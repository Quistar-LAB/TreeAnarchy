#define FULLVERBOSE
#define QUIETVERBOSE
//#define SILENT
#undef FULLVERBOSE
#if SILENT
#undef DEBUG
#undef FULLVERBOSE
#undef QUIETVERBOSE
#endif

using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy.Patches {
    internal static class TreeSnapping {
        private static bool isTranspilerPatched = false;
        internal static void Enable(Harmony harmony) {
            if (!isTranspilerPatched) {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.CalculateTreeTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.SimulationStep)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.SimulationStepTranspiler))));
                isTranspilerPatched = true;
            }
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.TransformPrefix))));
        }

        internal static void Disable(Harmony harmony, string id) {
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)), HarmonyPatchType.Prefix, id);
        }

        private static IEnumerable<CodeInstruction> AfterTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label ExitLabel = il.DefineLabel();
            Label FirstLabel = il.DefineLabel();
            Label SecondLabel = il.DefineLabel();
            Label ThirdLabel = il.DefineLabel();

            var codes = new List<CodeInstruction>(instructions);
            // search for first occurance of growstate
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].Calls(AccessTools.DeclaredPropertyGetter(typeof(TreeInstance), nameof(TreeInstance.GrowState)))) {
                    i += 2; // increment by two instructions
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Brfalse_S, ThirdLabel));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_1));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Bgt_S, FirstLabel));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_1));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Br_S, SecondLabel));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0).WithLabels(new Label[] { FirstLabel }));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))).WithLabels(new Label[] { SecondLabel }));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Br_S, ExitLabel));
                    codes[i] = codes[i].WithLabels(ThirdLabel);
                    codes[i + 3] = codes[i + 3].WithLabels(ExitLabel);
                    break; // found first occurance, so insert codes and break out
                }
            }

            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> CalculateTreeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            int FirstIndex = 0, LastIndex = 0;
            Label UseTerrainHeight = il.DefineLabel(), ExitBranch = il.DefineLabel(), FirstJump = il.DefineLabel();
            LocalBuilder terrainHeight = il.DeclareLocal(typeof(float));
            CodeInstruction stlocTerrainHeight = default;
            CodeInstruction ldlocTerrainHeight = default;

            var codes = new List<CodeInstruction>(instructions);

            switch (terrainHeight.LocalIndex) {
                case 1:
                stlocTerrainHeight = new CodeInstruction(OpCodes.Stloc_1);
                ldlocTerrainHeight = new CodeInstruction(OpCodes.Ldloc_1);
                break;
                case 2:
                stlocTerrainHeight = new CodeInstruction(OpCodes.Stloc_2);
                ldlocTerrainHeight = new CodeInstruction(OpCodes.Ldloc_2);
                break;
                case 3:
                stlocTerrainHeight = new CodeInstruction(OpCodes.Stloc_3);
                ldlocTerrainHeight = new CodeInstruction(OpCodes.Ldloc_3);
                break;
            }

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Beq) {
                    FirstIndex = i;
                }
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY)))) {
                    LastIndex = i + 2;
                }
            }

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Beq_S, FirstJump),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(FirstJump),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))),
                new CodeInstruction(OpCodes.Ldc_I4_S, 32),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Brtrue_S, ExitBranch),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeInstance), "get_Position")),
                new CodeInstruction(OpCodes.Stloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ColossalFramework.Singleton<TerrainManager>), "get_instance")),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TerrainManager), nameof(TerrainManager.SampleDetailHeight), new Type[] { typeof(Vector3) })),
                stlocTerrainHeight,
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                ldlocTerrainHeight,
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Brfalse_S, UseTerrainHeight),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))),
                new CodeInstruction(OpCodes.Ldc_I4_S, 32),
                new CodeInstruction(OpCodes.Or),
                new CodeInstruction(OpCodes.Conv_U2),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                new CodeInstruction(OpCodes.Ldc_R4, 64f),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt), new Type[] { typeof(float) })),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4, 65535),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) })),
                new CodeInstruction(OpCodes.Conv_U2),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))),
                new CodeInstruction(OpCodes.Br_S, ExitBranch),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(UseTerrainHeight),
                ldlocTerrainHeight,
                new CodeInstruction(OpCodes.Ldc_R4, 64f),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt), new Type[] { typeof(float) })),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4, 65535),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) })),
                new CodeInstruction(OpCodes.Conv_U2),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(ExitBranch)
            };

            codes.RemoveRange(FirstIndex, LastIndex - FirstIndex);
            codes.InsertRange(FirstIndex, snippet);

            return codes.AsEnumerable();
        }

        /* Inefficient, but more readable, performance here is insignificant */
        private static List<CodeInstruction> InsertCodeSnippet1(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            int occuranceCount = 0;
            int insertIndex = 0;
            LocalBuilder RaycastInput = null;
            Label jumpLabel = il.DefineLabel();
            ConstructorInfo RaycastService = AccessTools.DeclaredConstructor(typeof(ToolBase.RaycastService),
                                             new Type[] { typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Layer) });

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            /* First make sure to catch the RaycastInput variable */
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].operand is LocalBuilder input && input.LocalType == typeof(ToolBase.RaycastInput)) {
                    RaycastInput = input;
                    break;
                }
            }

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].LoadsField(AccessTools.Field(typeof(ToolController), nameof(ToolController.m_mode)))) {
                    occuranceCount++;
                }
                if (codes[i].opcode == OpCodes.Brfalse && occuranceCount == 2) {
                    insertIndex = i;
                    break;
                }
            }

            /* The following IL represents the following c# codes
             * input.m_currentEditObject = true;
             * input.m_ignoreBuildingFlags = Building.Flags.None;
             * input.m_ignoreNodeFlags = NetNode.Flags.None;
             * input.m_ignoreSegmentFlags = NetSegment.Flags.None;
             * input.m_buildingService = new RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
             * input.m_netService = new RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
             * input.m_netService2 = new RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
             */
            CodeInstruction[] insertCodes = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput).WithLabels( new Label[] { jumpLabel }),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_currentEditObject))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreBuildingFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreNodeFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreSegmentFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignorePropFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, RaycastService),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_propService))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, RaycastService),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_buildingService))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, RaycastService),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_netService))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, RaycastService),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_netService2))),
            };

            codes.RemoveRange(insertIndex + 1, 3);
            codes.InsertRange(insertIndex + 1, insertCodes);
            codes.Insert(insertIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))));
            codes.Insert(insertIndex, new CodeInstruction(OpCodes.Brtrue_S, jumpLabel));

            return codes;
        }

        private static List<CodeInstruction> InsertCodeSnippet2(List<CodeInstruction> codes) {
            for (int i = 0; i < codes.Count; i++) {
                // First Insert
                if (codes[i].LoadsField(AccessTools.Field(typeof(TreeTool.RaycastOutput), nameof(TreeTool.RaycastOutput.m_currentEditObject)))) {
                    if (codes[i + 1].opcode == OpCodes.Brtrue && codes[i + 1].Branches(out Label? label)) {
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Brtrue, label.Value));
                    }
                }
                // Second Insert
                if (codes[i].LoadsField(AccessTools.Field(typeof(TreeTool.RaycastOutput), nameof(TreeTool.RaycastOutput.m_currentEditObject)))) {
                    if (codes[i - 2].opcode == OpCodes.Ldfld) {
                        codes.RemoveRange(i - 1, 2);
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ceq));
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldc_I4_0));
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))));
                        break; // can break out now
                    }
                }
            }
            return codes;
        }

        private static List<CodeInstruction> InsertCodeSnippet3(List<CodeInstruction> codes, ILGenerator il) {
            Label exitLabel = il.DefineLabel();
            List<Label> tempLabels = null;
            CodeInstruction leaveOp = null;

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeTool), "m_fixedHeight"))) {
                    if (codes[i - 1].opcode == OpCodes.Ldfld) {
                        if (codes[i + 1].Branches(out Label? label)) {
                            exitLabel = label.Value;
                            tempLabels = codes[i + 1].labels;
                        }
                    }
                }
                if (codes[i].opcode == OpCodes.Leave) {
                    leaveOp = new CodeInstruction(codes[i].opcode, codes[i].operand); // catch and store opcode
                }
            }
            CodeInstruction[] InsertCode = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseTreeSnapping))),
                new CodeInstruction(OpCodes.Brfalse_S, exitLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseTreeSnapping))),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeTool), "m_fixedHeight")),
                leaveOp.WithLabels(tempLabels)
            };
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeTool), "m_placementErrors"))) {
                    if (codes[i + 1].opcode == OpCodes.Ldarg_0 && codes[i - 1].opcode == OpCodes.Ldloc_S) {
                        codes.RemoveRange(i + 1, 5);
                        codes.InsertRange(i + 1, InsertCode);
                    }
                }
            }
            return codes;
        }

        private static IEnumerable<CodeInstruction> SimulationStepTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = InsertCodeSnippet1(instructions, il);
            codes = InsertCodeSnippet2(codes);
            codes = InsertCodeSnippet3(codes, il);

            return codes.AsEnumerable();
        }

        private static bool TransformPrefix(MoveableTree __instance, float deltaHeight, bool followTerrain) {
            if (!followTerrain) {
                if (!TreeManager.instance.m_trees.m_buffer[__instance.id.Tree].FixedHeight && deltaHeight != 0) {
                    TreeManager.instance.m_trees.m_buffer[__instance.id.Tree].FixedHeight = true;
                }
            }
            return true;
        }

        public static bool Transform(MoveableTree __instance, InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain) {
            Vector3 vector = matrix4x.MultiplyPoint(state.position - center);
            vector.y = state.position.y + deltaHeight;
            if (followTerrain) {
                vector.y = vector.y + ColossalFramework.Singleton<TerrainManager>.instance.SampleOriginalRawHeightSmooth(vector) - state.terrainHeight;
            } else if (!ColossalFramework.Singleton<TreeManager>.instance.m_trees.m_buffer[state.id].FixedHeight && deltaHeight > 0) {
                ColossalFramework.Singleton<TreeManager>.instance.m_trees.m_buffer[state.id].FixedHeight = true;
            }
            __instance.Move(vector, 0f);
            return false;
        }

        private static bool RayCast(ToolBase.RaycastInput input, out ToolBase.RaycastOutput output) {
            Vector3 origin = input.m_ray.origin;
            Vector3 normalized = input.m_ray.direction.normalized;
            Vector3 vector = input.m_ray.origin + normalized * input.m_length;
            Segment3 ray = new Segment3(origin, vector);
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
            float num = input.m_length;
            if (!input.m_ignoreTerrain && Singleton<TerrainManager>.instance.RayCast(ray, out Vector3 vector2)) {
                float num2 = Vector3.Distance(vector2, origin) + 100f;
                if (num2 < num) {
                    output.m_hitPos = vector2;
                    result = true;
                    num = num2;
                }
            }
            if ((input.m_ignoreNodeFlags != NetNode.Flags.All || input.m_ignoreSegmentFlags != NetSegment.Flags.All) && Singleton<NetManager>.instance.RayCast(input.m_buildObject as NetInfo, ray, input.m_netSnap, input.m_segmentNameOnly, input.m_netService.m_service, input.m_netService2.m_service, input.m_netService.m_subService, input.m_netService2.m_subService, input.m_netService.m_itemLayers, input.m_netService2.m_itemLayers, input.m_ignoreNodeFlags, input.m_ignoreSegmentFlags, out vector2, out output.m_netNode, out output.m_netSegment)) {
                float num3 = Vector3.Distance(vector2, origin);
                if (num3 < num) {
                    output.m_hitPos = vector2;
                    result = true;
                    num = num3;
                } else {
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                }
            }
            if (input.m_ignoreBuildingFlags != Building.Flags.All && Singleton<BuildingManager>.instance.RayCast(ray, input.m_buildingService.m_service, input.m_buildingService.m_subService, input.m_buildingService.m_itemLayers, input.m_ignoreBuildingFlags, out vector2, out output.m_building)) {
                float num4 = Vector3.Distance(vector2, origin);
                if (num4 < num) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    result = true;
                    num = num4;
                } else {
                    output.m_building = 0;
                }
            }
            if (input.m_currentEditObject && Singleton<ToolManager>.instance.m_properties.RaycastEditObject(ray, out vector2)) {
                float num6 = Vector3.Distance(vector2, origin);
                if (num6 < num) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_currentEditObject = true;
                    result = true;
                    num = num6;
                }
            }
            if (input.m_ignorePropFlags != PropInstance.Flags.All && Singleton<PropManager>.instance.RayCast(ray, input.m_propService.m_service, input.m_propService.m_subService, input.m_propService.m_itemLayers, input.m_ignorePropFlags, out vector2, out output.m_propInstance)) {
                float num7 = Vector3.Distance(vector2, origin) - 0.5f;
                if (num7 < num) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_currentEditObject = false;
                    result = true;
                    num = num7;
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
        private static void RenderCollision(MoveableTree tree, TreeState treeState, RenderManager.CameraInfo cameraInfo, TreeInfo info, Vector3 deltaPosition, Vector3 center, Vector3 position, float scale, float brightness, Vector4 objectIndex) {
            /*
                        Ray mouseRay;
                        float mouseRayLength;
                        Vector3 vector = position;

                        if (info.m_prefabInitialized && UseTreeSnapping) {
                            Vector3 mousePosition = Input.mousePosition;
                            //mouseRay = Camera.main.ScreenPointToRay(mousePosition);
                            mouseRay = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(position));
                            mouseRayLength = Camera.main.farClipPlane;
                            ToolBase.RaycastInput input = new ToolBase.RaycastInput(mouseRay, mouseRayLength) {
                                m_currentEditObject = true,
                                m_ignoreBuildingFlags = Building.Flags.None,
                                m_ignoreNodeFlags = NetNode.Flags.None,
                                m_ignoreSegmentFlags = NetSegment.Flags.None,
                                m_ignorePropFlags = PropInstance.Flags.None,
                                m_buildingService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default),
                                m_propService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default),
                                m_netService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default),
                                m_netService2 = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default)
                            };
                            if (RayCast(input, out ToolBase.RaycastOutput raycastOutput)) {
                                vector = raycastOutput.m_hitPos;
                                Matrix4x4 matrix4x = default;
                                matrix4x.SetTRS(center + deltaPosition, Quaternion.AngleAxis(0, Vector3.down), Vector3.one);
                                treeState.position = matrix4x.MultiplyPoint(position - vector) - deltaPosition;
                                treeState.position.y = vector.y;
                            }
                        }
            */
            TreeInstance.RenderInstance(cameraInfo, info, position, scale, brightness, objectIndex);
        }
    }
}
