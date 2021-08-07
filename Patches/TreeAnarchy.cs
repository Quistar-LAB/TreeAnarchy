using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace TreeAnarchy.Patches {
    class TreeAnarchy {
        internal void EnablePatches(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.CheckPlacementErrors)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeAnarchy), nameof(TreeToolCheckPlacementErrorsTranspiler))));
        }

        internal void DisablePatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.CheckPlacementErrors)), HarmonyPatchType.Transpiler, TAPatcher.HARMONYID);
        }

        private static IEnumerable<CodeInstruction> TreeToolCheckPlacementErrorsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label TreeAnarchyDisabled = il.DefineLabel();
            var codes = instructions.ToList();
            codes[0].WithLabels(TreeAnarchyDisabled);
            codes.InsertRange(0, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeAnarchy))),
                new CodeInstruction(OpCodes.Brfalse_S, TreeAnarchyDisabled),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Conv_I8),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes.AsEnumerable();
        }
    }
}
