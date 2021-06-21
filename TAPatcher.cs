using ColossalFramework;
using ColossalFramework.Plugins;
using HarmonyLib;
using TreeAnarchy.Patches;
using UnityEngine;

namespace TreeAnarchy {
    internal static class TAPatcher {
        internal const string HARMONYID = @"quistar.treeanarchy.mod";
        internal static readonly Harmony m_harmony = new Harmony(HARMONYID);
        static readonly TreeLimit m_treeLimit = new TreeLimit();
        static readonly TreeMovement m_treeMovement = new TreeMovement();
        static readonly TreeSnapping m_treeSnapping = new TreeSnapping();
        static readonly TreeManagerData m_treedata = new TreeManagerData();

        private static bool IsPluginExists(string name) {
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if (info.name.Contains(name)) {
                    return true;
                }
            }
            return false;
        }

        internal static void EnableCore() {
            m_treeLimit.Ensure(m_harmony);
            m_treeLimit.Enable(m_harmony);
            m_treedata.Enable(m_harmony);
        }

        internal static void LateEnable() {
            m_treeSnapping.Enable(m_harmony);
            if (!IsPluginExists("Random Tree Rotation")) {
                m_treeMovement.Enable(m_harmony);
            } else {
                Debug.Log($"Tree Anarchy: Found Random Tree Rotation mod, Disabling tree movement");
            }
            if (TAConfig.UseExperimental) {
                TATreeManager.Enable(m_harmony);
            }
        }

        internal static void DisableCore() {
            m_treeLimit.Disable(m_harmony);
        }
    }
}
