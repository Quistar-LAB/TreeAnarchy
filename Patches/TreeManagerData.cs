#define FULLVERBOSE
#define QUIETVERBOSE
#define SILENT
#undef FULLVERBOSE
#if SILENT
#undef DEBUG
#undef FULLVERBOSE
#undef QUIETVERBOSE
#endif

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy.Patches {
    internal static class TreeManagerData {
        internal static void Enable(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Deserialize)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerData), nameof(TreeManagerData.DeserializeTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Serialize)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerData), nameof(TreeManagerData.SerializeTranspiler))));
        }

        private static IEnumerable<CodeInstruction> DeserializeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            bool firstSig = false, secondSig = false;
            LocalBuilder maxLen = il.DeclareLocal(typeof(int)), num4;
            Label IsFixedHeight = il.DefineLabel();
            MethodInfo integratedDeserialize = AccessTools.Method(typeof(TASerializableDataExtension), nameof(TASerializableDataExtension.IntegratedDeserialize));

            var codes = instructions.ToList();
            codes.Insert(0, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeLimit), nameof(TreeLimit.EnsureCapacity))));
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_3 && !firstSig) {
                    firstSig = true;
                    var snippet = new CodeInstruction[] {

                        new CodeInstruction(OpCodes.Ldc_I4, DefaultTreeLimit),
                        new CodeInstruction(OpCodes.Stloc_S, maxLen),
                    };
                    codes.InsertRange(i + 1, snippet);
                }
                if (codes[i].opcode == OpCodes.Ldloc_3) {
                    if (codes[i - 1].operand != null) {
                        var local = codes[i - 1].operand as LocalBuilder;
                        if (local.LocalIndex != 19) {
                            codes[i] = new CodeInstruction(OpCodes.Ldloc_S, maxLen);
                        }
                    }
                }
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_nextGridTree))) && !secondSig) {
                    secondSig = true;
                    num4 = codes[i + 2].operand as LocalBuilder;
                    var snippet = new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_1),
                        new CodeInstruction(OpCodes.Ldloc_S, num4),
                        new CodeInstruction(OpCodes.Ldelema, typeof(TreeInstance)),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.FixedHeight))),
                        new CodeInstruction(OpCodes.Brtrue_S, IsFixedHeight),
                    };
                    codes.InsertRange(i + 1, snippet);
                    codes.Insert(i - 7, new CodeInstruction(OpCodes.Call, integratedDeserialize));
                }
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))) && secondSig) {
                    codes[i + 1] = codes[i + 1].WithLabels(IsFixedHeight);
                }
            }

            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> SerializeTranspiler(IEnumerable<CodeInstruction> instructions) {
            bool sigFound = false;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_2 && !sigFound) {
                    int index = i - 3;
                    codes.RemoveRange(index, 3);
                    codes.Insert(index, new CodeInstruction(OpCodes.Ldc_I4, DefaultTreeLimit));
                    sigFound = true;
                }
            }

            return codes.AsEnumerable();
        }
    }
}
