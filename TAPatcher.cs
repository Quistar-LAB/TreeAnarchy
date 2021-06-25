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
            if (!isTreeSnapPatched) {
                /* for tree snapping */
                m_treeSnapping.Enable(m_harmony);
                /* for tree rotation */
                if (!IsPluginExists("1388613752", "Random Tree Rotation") && !isTreeMovementPatched) {
                    m_treeMovement.Enable(m_harmony);
                    isTreeMovementPatched = true;
                }
                isTreeSnapPatched = true;
            }

            if (TAConfig.UseExperimental && !isExperimentalPatched) {
                TATreeManager.Enable(m_harmony);
                isExperimentalPatched = true;
            }
        }

        internal static void DisableCore() {
            if (isCorePatched) {
                m_treeLimit.Disable(m_harmony);
                isCorePatched = false;
            }
            if (isTreeSnapPatched) {
                m_treeSnapping.Disable(m_harmony, HARMONYID);
            }
            if (isTreeMovementPatched) {
                m_treeMovement.Disable(m_harmony);
            }
        }
    }
}
