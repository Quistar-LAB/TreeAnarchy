using System;
using System.Diagnostics;
using HarmonyLib;
using ColossalFramework;
using UnityEngine;

namespace TreeAnarchy.Patches {
    static class TATreeManager {
        internal static void Enable(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TATreeManager), nameof(TATreeManager.EndRenderingImplPrefix))));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(TATreeManager), nameof(TATreeManager.EndRenderingImplPostfix))));
        }
        static Stopwatch timer = new Stopwatch();

        private static bool EndRenderingImplPrefix(RenderManager.CameraInfo cameraInfo) {
            timer.Reset();
            timer.Start();
            return true;
        }
        private static void EndRenderingImplPostfix(RenderManager.CameraInfo cameraInfo) {
            timer.Stop();
            UnityEngine.Debug.Log($"TreeAnarchy: EndRenderingImpl execution time: {timer.ElapsedMilliseconds}ms");
        }
    }
}
