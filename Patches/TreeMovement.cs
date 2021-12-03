using ColossalFramework;
using HarmonyLib;

namespace TreeAnarchy {
    internal static partial class TAPatcher {
        private static void EnableTreeMovementPatches(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(OptionsMainPanel), nameof(OptionsMainPanel.OnClosed)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(TAManager), nameof(TAManager.OnOptionPanelClosed))));
        }

        private static void DisableTreeMovementPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(OptionsMainPanel), nameof(OptionsMainPanel.OnClosed)), HarmonyPatchType.Postfix, HARMONYID);
        }
    }
}
