using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy.Patches {
    internal static class TreeMovement {
        private static bool transpilerPatched = false;
        private static readonly Quaternion[] treeQuaternion = new Quaternion[360];
        private static bool updateLODTreeSway = false;
        private static WeatherManager.WindCell[] windGrids;

        internal static void Enable(Harmony harmony) {
            for (int i = 0; i < 360; i++) {
                treeQuaternion[i] = Quaternion.Euler(0f, i, 0f);
            }
            windGrids = Singleton<WeatherManager>.instance.m_windGrid;
            if (!transpilerPatched) {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                    new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(RenderInstanceTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                    new Type[] { typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(),
                                 typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(PopulateGroupDataTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(OptionsMainPanel), nameof(OptionsMainPanel.OnClosed)),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(OnOptionPanelClosed))));
                transpilerPatched = true;
            }
        }

        private static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].Calls(AccessTools.PropertyGetter(typeof(Quaternion), nameof(Quaternion.identity)))) {
                    var snippet = new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarga_S, 2),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.sqrMagnitude)))
                    };
                    codes[i].operand = AccessTools.Method(typeof(TreeMovement), nameof(TreeMovement.GetRandomQuaternion));
                    codes.InsertRange(i, snippet);
                }
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].Calls(AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.GetWindSpeed), new Type[] { typeof(Vector3) }))) {
                    codes.RemoveRange(i - 2, 3);
                    codes.Insert(i - 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeMovement), nameof(TreeMovement.GetWindSpeed))));
                    codes.Insert(i - 2, new CodeInstruction(OpCodes.Ldarg_2));
                }
            }
            return codes.AsEnumerable();
        }

        /* TreeInstance::PopulateGroupData */
        private static IEnumerable<CodeInstruction> PopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].Calls(AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.GetWindSpeed), new Type[] { typeof(Vector3) }))) {
                    codes.RemoveRange(i - 2, 3);
                    codes.Insert(i - 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeMovement), nameof(TreeMovement.GetWindSpeed))));
                    codes.Insert(i - 2, new CodeInstruction(OpCodes.Ldarg_1));
                }
            }
            return codes.AsEnumerable();
        }

        private static void OnOptionPanelClosed() {
            if (updateLODTreeSway) {
                UpdateLODProc();
                updateLODTreeSway = false;
            }
        }



        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Quaternion GetRandomQuaternion(float magnitude) {
            int index = (int)((long)magnitude * RandomTreeRotationFactor) % 359;
            index = (index + (index >> 31)) ^ (index >> 31);
            return treeQuaternion[index];
        }

        public static float GetWindSpeed(Vector3 pos) {
            int tempX = (int)(pos.x * 0.0074074074f + 63.5f);
            int tempY = (int)(pos.z * 0.0074074074f + 63.5f);
            tempX = tempX > 127 ? 127 : tempX;
            int x = tempX < 0 ? 0 : tempX;
            //int x = Mathf.Clamp(Mathf.FloorToInt(pos.x / 135f + 64f - 0.5f), 0, 127);

            tempY = tempY > 127 ? 127 : tempY;
            int y = tempY < 0 ? 0 : tempY;
            //int y = Mathf.Clamp(Mathf.FloorToInt(pos.z / 135f + 64f - 0.5f), 0, 127);

            int totalHeight = (int)windGrids[y * 128 + x].m_totalHeight;
            float windHeight = ((pos.y - (float)totalHeight * 0.015625f) * 0.02f + 1);
            windHeight = windHeight > 2f ? 2f : windHeight;
            return windHeight < 0 ? 0 : windHeight * TreeSwayFactor;
            //return Mathf.Clamp(windHeight * 0.02f + 1, 0f, 2f) * TreeSwayFactor;
        }


        private static void UpdateLODProc() {
            int layerID = Singleton<TreeManager>.instance.m_treeLayer;
            FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
            for (int i = 0; i < renderedGroups.m_size; i++) {
                RenderGroup renderGroup = renderedGroups.m_buffer[i];
                RenderGroup.MeshLayer layer = renderGroup.GetLayer(layerID);
                if (layer != null) {
                    layer.m_dataDirty = true;
                }
                renderGroup.UpdateMeshData();
            }
        }

        public static void UpdateTreeSway() {
            updateLODTreeSway = true;
        }
    }
}
