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
        private static bool transpilerPatched = false;
        private static readonly Quaternion[] treeQuaternion = new Quaternion[360];

        internal void Enable(Harmony harmony) {
            for(int i = 0; i < 360; i++) {
                treeQuaternion[i] = Quaternion.Euler(0f, i, 0f);
            }
            if (!transpilerPatched) {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                    new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovement), nameof(RenderInstanceTranspiler))));
                transpilerPatched = true;
            }
        }

        internal void Disable(Harmony harmony) {}

        public static Quaternion GetRandomQuaternion(float magnitude) {
            return treeQuaternion[Math.Abs((int)((long)magnitude * 1000) % 359)];
        }

        private static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase method) {
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

            foreach(var code in codes) {
                Debug.Log($"TreeAnarchy: =>> {code}");
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

    }
}
