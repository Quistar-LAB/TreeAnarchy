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

        internal static void Enable(Harmony harmony) {
            for (int i = 0; i < 360; i++) {
                treeQuaternion[i] = Quaternion.Euler(0f, i, 0f);
            }
            if (!transpilerPatched) {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                    new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(RenderInstanceTranspiler))));
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Quaternion GetRandomQuaternion(float magnitude) {
            int index = (int)((long)magnitude * 1000) % 359;
            index = (index + (index >> 31)) ^ (index >> 31);
            return treeQuaternion[index];
        }

        public static float GetWindSpeed(Vector3 pos) {
            int num = Mathf.Clamp(Mathf.FloorToInt(pos.x / 135f + 64f - 0.5f), 0, 127);
            int num2 = Mathf.Clamp(Mathf.FloorToInt(pos.z / 135f + 64f - 0.5f), 0, 127);
            int totalHeight = (int)WeatherManager.instance.m_windGrid[num2 * 128 + num].m_totalHeight;
            float num3 = pos.y - (float)totalHeight * 0.015625f;
            return Mathf.Clamp(num3 * 0.02f + TreeSwayFactor, 0f, 2f);
        }
    }
}
