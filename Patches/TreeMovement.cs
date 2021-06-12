using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using HarmonyLib;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static TreeAnarchy.TAConfig;

namespace TreeAnarchy.Patches {
    internal class TreeMovement {
        private bool IsMoveItExists() {
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if (info.name.Contains("Move It")) {
                    return true;
                }
            }
            return false;
        }

        internal void Enable(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderGeometry)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(RenderGeometryTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(RenderInstanceTranspiler))));
            if (IsMoveItExists()) {
                harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneGeometry)),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(RenderClonePrefix))));
            }
        }

        internal void Disable(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneGeometry)), HarmonyPatchType.Prefix, TAPatcher.HARMONYID);
        }

        private static IEnumerable<CodeInstruction> RenderGeometryTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            bool sigFound = false;
            Label branch = il.DefineLabel(), exit = il.DefineLabel();
            LocalBuilder brightness = default, scale = default;
            MethodInfo methodSig = AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) });

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_S && ((LocalBuilder)codes[i].operand).LocalIndex == 4) {
                    scale = codes[i].operand as LocalBuilder;
                }
                if (codes[i].opcode == OpCodes.Stloc_S && ((LocalBuilder)codes[i].operand).LocalIndex == 5 && !sigFound) {
                    sigFound = true;
                    brightness = codes[i].operand as LocalBuilder;
                    CodeInstruction[] insertCodes = new CodeInstruction[]
                    {
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(RandomTreeRotation))),
                        new CodeInstruction(OpCodes.Brfalse_S, branch),
                        new CodeInstruction(OpCodes.Ldnull),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeTool), "m_cachedPosition")),
                        new CodeInstruction(OpCodes.Ldloc_S, scale),
                        new CodeInstruction(OpCodes.Ldloc_S, brightness),
                        new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(RenderManager), nameof(RenderManager.DefaultColorLocation))),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeMovement), nameof(TreeMovement.RenderRotation))),
                        new CodeInstruction(OpCodes.Br_S, exit),
                    };
                    codes[i + 1].WithLabels(branch);
                    codes.InsertRange(i + 1, insertCodes);
                }
                if (codes[i].Calls(methodSig)) {
                    codes[i + 1].WithLabels(exit);
                }
            }
            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].Calls(AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.GetWindSpeed), new Type[] { typeof(Vector3) }))) {
                    codes.RemoveRange(i - 2, 3);
                    codes.Insert(i - 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeMovement), nameof(TreeMovement.GetWindSpeed))));
                    codes.Insert(i - 2, new CodeInstruction(OpCodes.Ldarg_2));
                }
            }

            return codes.AsEnumerable();
        }

        private static float GetWindSpeed(Vector3 pos) {
            int num = Mathf.Clamp(Mathf.FloorToInt(pos.x / 135f + 64f - 0.5f), 0, 127);
            int num2 = Mathf.Clamp(Mathf.FloorToInt(pos.z / 135f + 64f - 0.5f), 0, 127);
            int totalHeight = (int)WeatherManager.instance.m_windGrid[num2 * 128 + num].m_totalHeight;
            float num3 = pos.y - (float)totalHeight * 0.015625f;
            return Mathf.Clamp(num3 * 0.02f + TreeSwayFactor, 0f, 2f);
        }

        /* I'm making TreeTool::RenderGeometry and MoveIt RenderGeometryClone call this custom RenderInstance instead of the default CO RenderInstance
		 * which would only affect one tree, instead of the entire tree population on the map.
		 */
        private static void RenderRotation(RenderManager.CameraInfo _, TreeInfo info, Vector3 position, float scale, float brightness, Vector4 objectIndex) {
            if (info.m_prefabInitialized) {
                TreeManager instance = Singleton<TreeManager>.instance;
                MaterialPropertyBlock materialBlock = instance.m_materialBlock;
                Matrix4x4 matrix = default;
                matrix.SetTRS(position, Quaternion.Euler(0f, (((long)position.magnitude) << 5) % 360L, 0f), new Vector3(scale, scale, scale));
                Color value = info.m_defaultColor * brightness;
                value.a = Singleton<WeatherManager>.instance.GetWindSpeed(position) * TreeSwayFactor;
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, value);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
            }
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
        private static void RenderCollision(TreeState treeState, RenderManager.CameraInfo cameraInfo, TreeInfo info, Vector3 position, float scale, float brightness, Vector4 objectIndex) {
            Ray mouseRay;
            float mouseRayLength;

            if (info.m_prefabInitialized && UseTreeSnapping) {
                /*
				Vector3 mousePosition = Input.mousePosition;
				mouseRay = Camera.main.ScreenPointToRay(mousePosition);
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
					position = raycastOutput.m_hitPos;
					treeState.position.y = position.y;
				}
				*/
                if (RandomTreeRotation) RenderRotation(cameraInfo, info, position, scale, brightness, objectIndex);
                else TreeInstance.RenderInstance(cameraInfo, info, position, scale, brightness, objectIndex);
            }
        }

        /* I wonder if this code can be implemented directly into MoveIt mod... or should it stay seperate
		 */
        private static bool RenderClonePrefix(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo) {
            TreeState treeState = instanceState as TreeState;
            TreeInfo treeInfo = treeState.Info.Prefab as TreeInfo;
            Randomizer randomizer = new Randomizer(treeState.instance.id.Tree);
            float scale = treeInfo.m_minScale + (float)randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
            float brightness = treeInfo.m_minBrightness + (float)randomizer.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
            Vector3 vector = matrix4x.MultiplyPoint(treeState.position - center);
            vector.y = treeState.position.y + deltaPosition.y;
            if (followTerrain) {
                vector.y = vector.y - treeState.terrainHeight + Singleton<TerrainManager>.instance.SampleOriginalRawHeightSmooth(vector);
            }
            RenderCollision(treeState, cameraInfo, treeInfo, vector, scale, brightness, RenderManager.DefaultColorLocation);

            return false;
        }
    }
}
