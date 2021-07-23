using ColossalFramework;
using ColossalFramework.Plugins;
using HarmonyLib;
using System.Reflection;
using TreeAnarchy.Patches;

namespace TreeAnarchy {
    internal class TAPatcher {
        private const string HARMONYID = @"quistar.treeanarchy.mod";

        private static bool isCorePatched = false;
        internal static FieldInfo MoveItUseTreeSnap = null;
        internal static bool isMoveItBeta = false;

        private bool IsPluginExists(string id, string name) {
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if (info.name.Contains(id) || info.ToString().Contains(name)) {
                    if (info.isEnabled) return true;
                }
            }
            return false;
        }

        private bool CheckMoveItTreeSnapSig() {
            FieldInfo treeSnapField = typeof(MoveIt.MoveItTool).GetField("treeSnapping", BindingFlags.GetField | BindingFlags.Static | BindingFlags.Public);
            if (treeSnapField != null) {
                MoveItUseTreeSnap = treeSnapField;
                MoveItUseTreeSnap.SetValue(null, TAMod.UseTreeSnapping);
                isMoveItBeta = true;
                return true;
            }
            return false;
        }

        internal void EnableCore() {
            Harmony harmony = new Harmony(HARMONYID);
            if (!isCorePatched) {
                TreeLimit treeLimit = new TreeLimit();
                treeLimit.Enable(harmony);
                TreeSnapping treeSnapping = new TreeSnapping();
                treeSnapping.Enable(harmony);
                TreeVariation treeVariation = new TreeVariation();
                treeVariation.EnablePatch(harmony);
                isCorePatched = true;
            }
        }

        internal void LateEnable() {
            Harmony harmony = new Harmony(HARMONYID);
            TreeVariation treeVariation = new TreeVariation();
            TreeMovement treeMovement = new TreeMovement();
            treeMovement.Enable(harmony);
            treeVariation.EnablePatch(harmony);

            if (IsPluginExists("1619685021", "MoveIt") || IsPluginExists("2215771668", "MoveIt")) {
                if (!CheckMoveItTreeSnapSig()) {
                    TreeSnapping treeSnapping = new TreeSnapping();
                    treeSnapping.PatchMoveIt(harmony);
                }
                treeVariation.PatchMoveIt(harmony);
            }
        }

        internal void DisableLatePatch() {
            Harmony harmony = new Harmony(HARMONYID);
            TreeVariation treeVariation = new TreeVariation();
            treeVariation.DisablePatch(harmony);
        }

        internal void DisableCore() { }
    }
}
