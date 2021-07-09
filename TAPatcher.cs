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
        private static bool isTreeMovementPatched = false;
        internal static FieldInfo MoveItUseTreeSnap = null;
        internal static bool isMoveItBeta = false;

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
                isMoveItBeta = true;
                return true;
            }
            return false;
        }

        internal static void EnableCore() {
            if (!isCorePatched) {
                TreeLimit.Enable(m_harmony);
                TreeSnapping.Enable(m_harmony);
                isCorePatched = true;
            }
        }

        internal static void LateEnable() {
            if (IsPluginExists("1619685021", "MoveIt") || IsPluginExists("2215771668", "MoveIt")) {
                if (!CheckMoveItTreeSnapSig()) {
                    TreeSnapping.PatchMoveIt(m_harmony);
                }
                TreeSnapping.PatchMoveItRayCast(m_harmony);
            }
            if (!IsPluginExists("1388613752", "Tree Movement Control") ||
                !IsPluginExists("556784825", "Random Tree Rotation") && !isTreeMovementPatched) {
                TreeMovement.Enable(m_harmony);
                isTreeMovementPatched = true;
            }
        }

        internal static void DisableLatePatch() {
        }

        internal static void DisableCore() { }
    }
}
