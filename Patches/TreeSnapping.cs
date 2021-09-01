using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
        private const float errorMargin = 0.075f;
        private const ushort FixedHeightMask = unchecked((ushort)~TreeInstance.Flags.FixedHeight);
        private const ushort FixedHeightFlag = unchecked((ushort)TreeInstance.Flags.FixedHeight);
        private void EnableTreeSnappingPatches(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CalculateTreeTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.SimulationStep)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeToolSimulationStepTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeTool).GetNestedType("<CreateTree>c__Iterator0", BindingFlags.Instance | BindingFlags.NonPublic), "MoveNext"),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeToolCreateTreeTranspiler))));
        }

        private void PatchMoveItSnapping(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(MoveableTreeTransformPrefix))));
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneGeometry)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(RenderCloneGeometryPrefix))));
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneOverlay)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(RenderCloneOverlayPrefix))));
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Clone),
                new Type[] { typeof(InstanceState), typeof(Matrix4x4).MakeByRefType(),
                typeof(float), typeof(float), typeof(Vector3), typeof(bool), typeof(Dictionary<ushort, ushort>), typeof(MoveIt.Action) }),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(ClonePrefix))));
        }

        private void DisableTreeSnappingPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.SimulationStep)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool).GetNestedType("<CreateTree>c__Iterator0", BindingFlags.Instance | BindingFlags.NonPublic), "MoveNext"), HarmonyPatchType.Transpiler, HARMONYID);
        }

        private void DisableMoveItSnappingPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)), HarmonyPatchType.Prefix, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneGeometry)), HarmonyPatchType.Prefix, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneOverlay)), HarmonyPatchType.Prefix, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Clone),
                new Type[] { typeof(InstanceState), typeof(Matrix4x4).MakeByRefType(),
                typeof(float), typeof(float), typeof(Vector3), typeof(bool), typeof(Dictionary<ushort, ushort>), typeof(MoveIt.Action) }),
                HarmonyPatchType.Prefix, HARMONYID);
        }

        public static float SampleSnapDetailHeight(ref TreeInstance tree, Vector3 position) {
            float terrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
            if (UseTreeSnapping) {
                if (position.y < (terrainHeight - errorMargin) || position.y > terrainHeight + errorMargin) {
                    return position.y;
                }
            }
            tree.m_flags &= FixedHeightMask;
            return terrainHeight;
        }

        private static IEnumerable<CodeInstruction> CalculateTreeTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo terrainInstance = AccessTools.PropertyGetter(typeof(Singleton<TerrainManager>), nameof(Singleton<TerrainManager>.instance));
            using IEnumerator<CodeInstruction> codes = instructions.GetEnumerator();
            while (codes.MoveNext()) {
                CodeInstruction cur = codes.Current;
                if (cur.opcode == OpCodes.Call && cur.operand == terrainInstance) {
                    codes.MoveNext(); codes.MoveNext();
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(SampleSnapDetailHeight)));
                } else {
                    yield return cur;
                }
            }
        }

        private static ToolBase.RaycastService customServices = new(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
        public static void ConfigureRaycastInput(ref ToolBase.RaycastInput input) {
            if (UseTreeSnapping) {
                input.m_currentEditObject = false;
                input.m_ignoreTerrain = false;
                input.m_ignoreBuildingFlags = Building.Flags.None;
                input.m_ignoreNodeFlags = NetNode.Flags.None;
                input.m_ignoreSegmentFlags = NetSegment.Flags.None;
                input.m_ignorePropFlags = PropInstance.Flags.None;
                input.m_propService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
                input.m_buildingService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
                input.m_netService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
                input.m_netService2 = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
            }
        }

        public static void CalcFixedHeight(uint treeID) {
            TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;
            Vector3 position = trees[treeID].Position;
            float terrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
            if (position.y > terrainHeight + errorMargin) {
                trees[treeID].m_flags |= FixedHeightFlag;
            } else {
                trees[treeID].m_flags &= FixedHeightMask;
            }
        }

        private static IEnumerable<CodeInstruction> TreeToolCreateTreeTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo placementEffect = AccessTools.Method(typeof(TreeTool), nameof(TreeTool.DispatchPlacementEffect));
            using IEnumerator<CodeInstruction> codes = instructions.GetEnumerator();
            while (codes.MoveNext()) {
                CodeInstruction cur = codes.Current;
                if (cur.opcode == OpCodes.Call && cur.operand == placementEffect) {
                    yield return cur;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(CalcFixedHeight)));
                } else {
                    yield return cur;
                }
            }
        }

        private static IEnumerable<CodeInstruction> TreeToolSimulationStepTranspiler(IEnumerable<CodeInstruction> instructions) {
            var LoadFieldUseTreeSnapping = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseTreeSnapping)));
            FieldInfo rayOutputObject = AccessTools.Field(typeof(ToolBase.RaycastOutput), nameof(ToolBase.RaycastOutput.m_currentEditObject));
            FieldInfo hitPos = AccessTools.Field(typeof(ToolBase.RaycastOutput), nameof(ToolBase.RaycastOutput.m_hitPos));
            FieldInfo mouseRay = AccessTools.Field(typeof(TreeTool), "m_mouseRay");
            FieldInfo fixedHeight = AccessTools.Field(typeof(TreeTool), "m_fixedHeight");
            using IEnumerator<CodeInstruction> codes = instructions.GetEnumerator();
            while (codes.MoveNext()) {
                CodeInstruction cur = codes.Current;
                if (cur.opcode == OpCodes.Ldloca_S && codes.MoveNext()) {
                    CodeInstruction next = codes.Current;
                    if (next.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
                        CodeInstruction next1 = codes.Current;
                        if (next1.opcode == OpCodes.Ldfld && next1.operand == mouseRay && codes.MoveNext()) {
                            CodeInstruction next2 = codes.Current;
                            LocalBuilder input = cur.operand as LocalBuilder;
                            yield return cur;
                            yield return next;
                            yield return next1;
                            yield return next2;
                            codes.MoveNext();
                            yield return codes.Current;
                            codes.MoveNext();
                            yield return codes.Current;
                            yield return new CodeInstruction(OpCodes.Ldloca_S, input);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(ConfigureRaycastInput)));
                        } else {
                            yield return cur;
                            yield return next;
                            yield return next1;
                        }
                    } else if (next.opcode == OpCodes.Ldfld && next.operand == rayOutputObject && codes.MoveNext()) {
                        CodeInstruction next1 = codes.Current;
                        if (next1.opcode == OpCodes.Stfld && next1.operand == fixedHeight) {
                            yield return LoadFieldUseTreeSnapping;
                            yield return next1;
                        } else if (next1.opcode == OpCodes.Ldloc_S && (next1.operand as LocalBuilder).LocalIndex == 5) {
                            yield return cur;
                            yield return next;
                            yield return LoadFieldUseTreeSnapping;
                            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                            yield return new CodeInstruction(OpCodes.Ceq);
                            yield return new CodeInstruction(OpCodes.Or);
                            yield return next1;
                        } else if (next1.opcode == OpCodes.Brtrue) {
                            yield return cur;
                            yield return next;
                            yield return LoadFieldUseTreeSnapping;
                            yield return new CodeInstruction(OpCodes.Or);
                            yield return next1;
                        } else {
                            yield return cur;
                            yield return next;
                            yield return next1;
                        }
                    } else {
                        yield return cur;
                        yield return next;
                    }
                } else {
                    yield return cur;
                }
            }
        }

        /* If tree snapping is false => 
         * check if tree has fixedheight flag, if not then follow the terrain
         * 
         * If tree snapping is on =>
         * If following terrain is turned on, then just follow terrain no matter what.
         * If tree snapping is true, then check for deltaheight to see if user raised the tree or not
         * if deltaheight is 0, then raycast to see if it hit any object to snap to
         * If after raycast, no object is hit, then use default follow terrainheight
         */
        private static Vector3 SampleTreeSnapVector(MoveableTree instance, InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, Vector3 center, bool followTerrain) {
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            newPosition.y = state.position.y + deltaHeight;

            //float oldTerrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(instance.position);
            float newTerrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(newPosition);
            TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;
            uint treeID = instance.id.Tree;

            if (!UseTreeSnapping) {
                if ((trees[treeID].m_flags & FixedHeightFlag) == 0) {
                    newPosition.y = newTerrainHeight;
                }
            } else if (followTerrain) {
                trees[treeID].m_flags &= FixedHeightMask;
                newPosition.y = newTerrainHeight;
            } else {
                if (deltaHeight != 0) {
                    trees[treeID].m_flags |= FixedHeightFlag;
                } else {
                    if (!TreeSnapRayCast(newPosition, out newPosition)) {
                        newPosition.y = newTerrainHeight;
                    } else {
                        if (newPosition.y > newTerrainHeight + errorMargin || newPosition.y < newTerrainHeight - errorMargin) {
                            trees[treeID].m_flags |= FixedHeightFlag;
                        }
                        /* seems after snapping to a building with y position > 0, then tree position gets all messed up
                            * so we have to reset the position in the cases where position is back to terrain height +- 0.075f */
                        if (newPosition.y >= (newTerrainHeight - errorMargin) && newPosition.y <= (newTerrainHeight + errorMargin)) {
                            newPosition.y = newTerrainHeight;
                            trees[treeID].m_flags &= FixedHeightMask;
                            state.position.y = newTerrainHeight;
                        }
                    }
                }
            }

            return newPosition;
        }

        /* Must call method instead of adding in this routine. Harmony issue workaround */
        private static bool MoveableTreeTransformPrefix(MoveableTree __instance, InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, Vector3 center, bool followTerrain) {
            Vector3 vector = SampleTreeSnapVector(__instance, state, ref matrix4x, deltaHeight, center, followTerrain);
            __instance.Move(vector, 0f);
            return false;
        }

        private static bool RenderCloneGeometryPrefix(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo) {
            TreeState treeState = instanceState as TreeState;
            TreeInfo treeInfo = treeState.Info.Prefab as TreeInfo;
            Randomizer randomizer = new(treeState.instance.id.Tree);
            float scale = TAManager.CalcTreeScale(ref randomizer, treeState.instance.id.Tree, treeInfo);
            float brightness = treeInfo.m_minBrightness + (float)randomizer.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
            Vector3 vector = matrix4x.MultiplyPoint(treeState.position - center);
            vector.y = treeState.position.y + deltaPosition.y;
            vector = CalculateTreeVector(vector, deltaPosition.y, followTerrain);

            TreeInstance.RenderInstance(cameraInfo, treeInfo, vector, scale, brightness, RenderManager.DefaultColorLocation);
            return false;
        }

        private static bool RenderCloneOverlayPrefix(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor) {
            TreeState treeState = instanceState as TreeState;
            TreeInfo treeInfo = treeState.Info.Prefab as TreeInfo;
            Randomizer randomizer = new(treeState.instance.id.Tree);
            float scale = TAManager.CalcTreeScale(ref randomizer, treeState.instance.id.Tree, treeInfo);
            Vector3 vector = matrix4x.MultiplyPoint(treeState.position - center);
            vector.y = treeState.position.y + deltaPosition.y;
            vector = CalculateTreeVector(vector, deltaPosition.y, followTerrain);

            TreeTool.RenderOverlay(cameraInfo, treeInfo, vector, scale, toolColor);
            return false;
        }

        /* three situations where raycast position is used 
         * When deltaHeight is == 0
         * When position.y is at terrainHeight +- errorMargin
         * When raycastPosition.y > terrainHeight
         */
        private static Vector3 CalculateTreeVector(Vector3 position, float deltaHeight, bool followTerrain) {
            float terrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
            if (!UseTreeSnapping || followTerrain) {
                position.y = terrainHeight;
            } else if (TreeSnapRayCast(position, out Vector3 raycastPosition)) {
                position = raycastPosition;
            } else if (deltaHeight == 0) {
                position.y = terrainHeight;
            }
            return position;
        }

        private static bool ClonePrefix(ref Instance __result, InstanceState instanceState, ref Matrix4x4 matrix4x, float deltaHeight, Vector3 center, bool followTerrain) {
            TreeState treeState = instanceState as TreeState;
            Vector3 vector = matrix4x.MultiplyPoint(treeState.position - center);
            vector.y = treeState.position.y + deltaHeight;
            vector = CalculateTreeVector(vector, deltaHeight, followTerrain);
            __result = null;
            TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
            if (Singleton<TreeManager>.instance.CreateTree(out uint tree, ref Singleton<SimulationManager>.instance.m_randomizer, treeState.Info.Prefab as TreeInfo, vector, treeState.single)) {
                __result = new MoveableTree(new InstanceID {
                    Tree = tree
                }) {
                    position = vector
                };
                if (!followTerrain) {
                    if (deltaHeight != 0 || treeState.position.y > treeState.terrainHeight + errorMargin || treeState.position.y < treeState.terrainHeight - errorMargin) {
                        buffer[tree].m_flags |= FixedHeightFlag;
                    }
                }
            }
            //Debug.Log($"TreeAnarchy: terrainHeight={terrainHeight} __instance.position={__instance.position} vector={vector} treeState.position={treeState.position} deltaHeight={deltaHeight} cloned position={buffer[tree].Position}");

            return false;
        }

        private static bool RayCast(ToolBase.RaycastInput input, out ToolBase.RaycastOutput output) {
            float tempRayLength;
            Vector3 origin = input.m_ray.origin;
            Vector3 normalized = input.m_ray.direction.normalized;
            Vector3 vector = input.m_ray.origin + normalized * input.m_length;
            Segment3 ray = new(origin, vector);
            output.m_hitPos = vector;
            output.m_overlayButtonIndex = 0;
            output.m_netNode = 0;
            output.m_netSegment = 0;
            output.m_building = 0;
            output.m_propInstance = 0;
            output.m_treeInstance = 0u;
            output.m_vehicle = 0;
            output.m_parkedVehicle = 0;
            output.m_citizenInstance = 0;
            output.m_transportLine = 0;
            output.m_transportStopIndex = 0;
            output.m_transportSegmentIndex = 0;
            output.m_district = 0;
            output.m_park = 0;
            output.m_disaster = 0;
            output.m_currentEditObject = false;
            bool result = false;
            float mouseRayLength = input.m_length;
            if (!input.m_ignoreTerrain && Singleton<TerrainManager>.instance.RayCast(ray, out Vector3 vector2)) {
                float rayLength = Vector3.Distance(vector2, origin) + 100f;
                if (rayLength < mouseRayLength) {
                    output.m_hitPos = vector2;
                    result = true;
                    mouseRayLength = rayLength;
                }
            }
            if ((input.m_ignoreNodeFlags != NetNode.Flags.All ||
                 input.m_ignoreSegmentFlags != NetSegment.Flags.All) && Singleton<NetManager>.instance.RayCast(input.m_buildObject as NetInfo, ray, input.m_netSnap, input.m_segmentNameOnly, input.m_netService.m_service, input.m_netService2.m_service, input.m_netService.m_subService, input.m_netService2.m_subService, input.m_netService.m_itemLayers, input.m_netService2.m_itemLayers, input.m_ignoreNodeFlags, input.m_ignoreSegmentFlags, out vector2, out output.m_netNode, out output.m_netSegment)) {
                tempRayLength = Vector3.Distance(vector2, origin);
                if (tempRayLength < mouseRayLength) {
                    output.m_hitPos = vector2;
                    result = true;
                    mouseRayLength = tempRayLength;
                } else {
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                }
            }
            if (input.m_ignoreBuildingFlags != Building.Flags.All && Singleton<BuildingManager>.instance.RayCast(ray, input.m_buildingService.m_service, input.m_buildingService.m_subService, input.m_buildingService.m_itemLayers, input.m_ignoreBuildingFlags, out vector2, out output.m_building)) {
                tempRayLength = Vector3.Distance(vector2, origin);
                if (tempRayLength < mouseRayLength) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    result = true;
                    mouseRayLength = tempRayLength;
                } else {
                    output.m_building = 0;
                }
            }
            if (input.m_currentEditObject && Singleton<ToolManager>.instance.m_properties.RaycastEditObject(ray, out vector2)) {
                tempRayLength = Vector3.Distance(vector2, origin);
                if (tempRayLength < mouseRayLength) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_currentEditObject = true;
                    result = true;
                    mouseRayLength = tempRayLength;
                }
            }
            if (input.m_ignorePropFlags != PropInstance.Flags.All && Singleton<PropManager>.instance.RayCast(ray, input.m_propService.m_service, input.m_propService.m_subService, input.m_propService.m_itemLayers, input.m_ignorePropFlags, out vector2, out output.m_propInstance)) {
                if (Vector3.Distance(vector2, origin) - 0.5f < mouseRayLength) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_currentEditObject = false;
                    result = true;
                } else {
                    output.m_propInstance = 0;
                }
            }
            return result;
        }

        // These codes should really live in the MoveIt mod space and not here!!
        /* As you can see here, I tried to implement raycast for tree snapping to work with MoveIt mod,
         * but apparently MoveIt calculates position differently than CO Framework. If this was ever to 
         * be realized, I think it has to be done from within MoveIt mod.
         */
        private static bool TreeSnapRayCast(Vector3 position, out Vector3 vector) {
            Ray objRay;
            if (!UseExperimentalTreeSnapping) {
                vector = position;
                return false;
            }

            objRay = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(position));
            ToolBase.RaycastInput input = new(objRay, Camera.main.farClipPlane) {
                m_ignoreTerrain = true
            };
            if (UseTreeSnapToBuilding) {
                input.m_ignoreBuildingFlags = Building.Flags.None;
                input.m_buildingService = new(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
            }
            if (UseTreeSnapToNetwork) {
                input.m_ignoreNodeFlags = NetNode.Flags.None;
                input.m_ignoreSegmentFlags = NetSegment.Flags.None;
                input.m_netService = new(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
                input.m_netService2 = new(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
            }
            if (UseTreeSnapToProp) {
                input.m_ignorePropFlags = PropInstance.Flags.None;
                input.m_propService = new(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
            }
            if (RayCast(input, out ToolBase.RaycastOutput raycastOutput)) {
                vector = raycastOutput.m_hitPos;
                return true;
            }
            vector = position;
            return false;
        }
    }
}
