using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
        private void EnableTreeMovementPatches(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(OptionsMainPanel), nameof(OptionsMainPanel.OnClosed)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(TAManager), nameof(TAManager.OnOptionPanelClosed))));
        }

        private void DisableTreeMovementPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(OptionsMainPanel), nameof(OptionsMainPanel.OnClosed)), HarmonyPatchType.Postfix, HARMONYID);
        }
    }
}
