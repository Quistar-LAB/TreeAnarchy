using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using ColossalFramework;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy.Patches {
    internal static class TreeMovement {
        private static bool transpilerPatched = false;
        private static readonly Quaternion[] treeQuaternion = new Quaternion[360];

        internal static void Enable(Harmony harmony) {
            for (int i = 0; i < 360; i++) {
                treeQuaternion[i] = Quaternion.Euler(0f, i, 0f);
            }
            if (!transpilerPatched) {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                    new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(RenderInstanceTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                    new Type[] { typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(),
                                 typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(PopulateGroupDataPrefix))),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(PopulateGroupDataTranspiler))));

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

            for(int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].Calls(AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.GetWindSpeed), new Type[] { typeof(Vector3) }))) {
                    codes.RemoveRange(i - 2, 3);
                    codes.Insert(i - 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeMovement), nameof(TreeMovement.GetWindSpeed))));
                    codes.Insert(i - 2, new CodeInstruction(OpCodes.Ldarg_1));
                }
            }
            return codes.AsEnumerable();
        }

        private static bool PopulateGroupDataPrefix(TreeInfo info, Vector3 position, float scale, float brightness, Vector4 objectIndex, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance) {
            Debug.Log($"TreeAnarchy: PopulateGroupData ==> vertexIndex: {vertexIndex}");
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Quaternion GetRandomQuaternion(float magnitude) {
            int index = (int)((long)magnitude * 1000) % 359;
            index = (index + (index >> 31)) ^ (index >> 31);
            return treeQuaternion[index];
        }

        public static float GetWindSpeed(Vector3 pos) {
            int x = Mathf.Clamp(Mathf.FloorToInt(pos.x / 135f + 64f - 0.5f), 0, 127);
            int y = Mathf.Clamp(Mathf.FloorToInt(pos.z / 135f + 64f - 0.5f), 0, 127);
            int totalHeight = (int)WeatherManager.instance.m_windGrid[y * 128 + x].m_totalHeight;
            float windHeight = pos.y - (float)totalHeight * 0.015625f;
            return Mathf.Clamp(windHeight * 0.02f + TreeSwayFactor, 0f, 2f);
        }

        public static void UpdateTreeSway() {
            TreeManager tm = Singleton<TreeManager>.instance;
            FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
            for (int i = 0; i < renderedGroups.m_size; i++) {
                RenderGroup renderGroup = renderedGroups.m_buffer[i];
                RenderGroup.MeshLayer meshLayer = renderGroup.GetLayer(tm.m_treeLayer);
                if(meshLayer != null) {
                    if(meshLayer.m_tempData != null) {
                        byte alpha = (byte)GetWindSpeed(renderGroup.m_position);
                        for (int j = 0; j < meshLayer.m_tempData.m_colors.Length; j++) {
                            meshLayer.m_tempData.m_colors[j].a = alpha;
                        }
                    }
                }
            }
        }
    }
}
