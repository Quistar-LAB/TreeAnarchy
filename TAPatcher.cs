using ColossalFramework;
using ColossalFramework.Plugins;
using HarmonyLib;
using System.Reflection;
using TreeAnarchy.Patches;

namespace TreeAnarchy {
    internal static class TAPatcher {
        private const string HARMONYID = @"quistar.treeanarchy.mod";
        internal static readonly Harmony m_harmony = new Harmony(HARMONYID);

        private static bool isCorePatched = false;
        private static bool isTreeSnapPatched = false;
        private static bool isTreeMovementPatched = false;
        private static bool isExperimentalPatched = false;
        private static bool isExperimentalTranspilerPatched = false;
        internal static FieldInfo MoveItUseTreeSnap = null;

        private static bool IsPluginExists(string id, string name) {
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if (info.name.Contains(id) || info.ToString().Contains(name)) {
                    if (info.isEnabled) return true;
                }
            }
            return false;
        }

        private static bool CheckMoveItTreeSnapSig() {
            FieldInfo treeSnapField = typeof(MoveIt.MoveItTool).GetField("treeSnapping", BindingFlags.GetField | BindingFlags.Static | BindingFlags.Public);
            if (treeSnapField != null) {
                MoveItUseTreeSnap = treeSnapField;
                MoveItUseTreeSnap.SetValue(null, TAMod.UseTreeSnapping);
                return true;
            }
            return false;
        }

        internal static void EnableCore() {
            if (!isCorePatched) {
                TreeLimit.Enable(m_harmony);
                isCorePatched = true;
            }
        }

        internal static void LateEnable() {
            EnableCore();
            if (IsPluginExists("1619685021", "MoveIt") || IsPluginExists("2215771668", "MoveIt")) {
                if (!isTreeSnapPatched) {
                    TreeSnapping.Enable(m_harmony);
                    if (!CheckMoveItTreeSnapSig()) {
                        TreeSnapping.PatchMoveIt(m_harmony);
                    }
                    isTreeSnapPatched = true;
                }
            }
            if (!IsPluginExists("1388613752", "Tree Movement Control") ||
                !IsPluginExists("556784825", "Random Tree Rotation") && !isTreeMovementPatched) {
                TreeMovement.Enable(m_harmony);
                isTreeMovementPatched = true;
            }

            if (TAMod.UseExperimental && !isExperimentalPatched) {
                if (TAMod.EnableProfiling) {
                    m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"),
                        prefix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.EndRenderingImplPrefixProfiled))),
                        postfix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.EndRenderingImplPostfix))));
                    if (!isExperimentalTranspilerPatched) {
                        m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"),
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplPrefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplPostfix))),
                            transpiler: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplTranspiler))));
                        isExperimentalTranspilerPatched = true;
                    } else {
                        m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"),
                            prefix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplPrefix))),
                            postfix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplPostfix))));
                    }
                } else {
                    m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"),
                        prefix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.EndRenderingImplPrefix))));
                    if (!isExperimentalTranspilerPatched) {
                        m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"),
                            transpiler: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplTranspiler))));
                        isExperimentalTranspilerPatched = true;
                    }
                }
                AccelLayer.SetupRenderingFramework(Singleton<TreeManager>.instance, Singleton<InfoManager>.instance, Singleton<TerrainManager>.instance, Singleton<RenderManager>.instance);
                isExperimentalPatched = true;
            } else if (TAMod.EnableProfiling) {
                m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.EndRenderingImplPrefixProfiledWithoutAccel))),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.EndRenderingImplPostfix))));
                m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplPrefix))),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplPostfix))));
            }
        }

        internal static void DisableLatePatch() {
            if (isExperimentalPatched) {
                m_harmony.Unpatch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"), HarmonyPatchType.All);
                m_harmony.Unpatch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"), HarmonyPatchType.All);
                isExperimentalPatched = false;
            }
        }

        internal static void DisableCore() { }
    }
}
