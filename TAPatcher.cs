using ColossalFramework;
using ColossalFramework.Plugins;
using HarmonyLib;
using TreeAnarchy.Patches;

namespace TreeAnarchy {
    internal static class TAPatcher {
        internal const string HARMONYID = @"quistar.treeanarchy.mod";
        internal static readonly Harmony m_harmony = new Harmony(HARMONYID);
        static readonly TreeLimit m_treeLimit = new TreeLimit();
        static readonly TreeMovement m_treeMovement = new TreeMovement();
        static readonly TreeSnapping m_treeSnapping = new TreeSnapping();
        static readonly TreeManagerData m_treedata = new TreeManagerData();

        static bool isCorePatched = false;
        static bool isTreeSnapPatched = false;
        static bool isTreeMovementPatched = false;
        static bool isExperimentalPatched = false;
        static bool isExperimentalTranspilerPatched = false;

        private static bool IsPluginExists(string id, string name) {
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if (info.name.Contains(id) || info.ToString().Contains(name)) {
                    return true;
                }
            }
            return false;
        }

        internal static void EnableCore() {
            if (!isCorePatched) {
                m_treeLimit.Ensure(m_harmony);
                m_treeLimit.Enable(m_harmony);
                m_treedata.Enable(m_harmony);
                isCorePatched = true;
            }
        }

        internal static void LateEnable() {
            EnableCore();
            if (IsPluginExists("1619685021", "MoveIt")) {
                if (!isTreeSnapPatched) {
                    /* for tree snapping */
                    m_treeSnapping.Enable(m_harmony);
                    /* for tree rotation */
                    isTreeSnapPatched = true;
                }
                if (!IsPluginExists("1388613752", "Tree Movement Control") ||
                    !IsPluginExists("556784825", "Random Tree Rotation") && !isTreeMovementPatched) {
                    m_treeMovement.Enable(m_harmony);
                    isTreeMovementPatched = true;
                }
            }

            if (TAConfig.UseExperimental && !isExperimentalPatched) {
                if (TAConfig.EnableProfiling) {
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
                isExperimentalPatched = true;
            } else if(TAConfig.EnableProfiling) {
                m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.EndRenderingImplPrefixProfiledWithoutAccel))),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.EndRenderingImplPostfix))));
                m_harmony.Patch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplPrefix))),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(AccelLayer), nameof(AccelLayer.BeginRenderingImplPostfix))));
            }
        }

        internal static void DisableLatePatch() {
            if (isTreeSnapPatched) {
                m_treeSnapping.Disable(m_harmony, HARMONYID);
            }
            if (isTreeMovementPatched) {
                m_treeMovement.Disable(m_harmony);
            }
            if (isExperimentalPatched) {
                m_harmony.Unpatch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"), HarmonyPatchType.All);
                m_harmony.Unpatch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"), HarmonyPatchType.All);
                isExperimentalPatched = false;
            }
        }

        internal static void DisableCore() {
        }
    }
}
