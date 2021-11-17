using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
        private static IEnumerable<CodeInstruction> ReplaceLDCI4_MaxTreeLimit(IEnumerable<CodeInstruction> instructions) {
            foreach (var code in instructions) {
                if (code.Is(OpCodes.Ldc_I4, LastMaxTreeLimit)) {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit);
                } else {
                    yield return code;
                }
            }
        }

        // Patch WeatherManager::CalculateSelfHeight()
        // Affects Tree on Wind Effect, stops tree from slowing wind
        private static IEnumerable<CodeInstruction> CalculateSelfHeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            bool sigFound = false;
            Label returnTreeManagerLabel = il.DefineLabel();
            MethodInfo getTreeInstance = AccessTools.PropertyGetter(typeof(Singleton<TreeManager>), nameof(Singleton<TreeManager>.instance));
            using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    CodeInstruction cur = codes.Current;
                    if (!sigFound && cur.opcode == OpCodes.Ldloca_S && codes.MoveNext()) {
                        CodeInstruction next = codes.Current;
                        if (next.opcode == OpCodes.Ldloca_S && codes.MoveNext()) {
                            CodeInstruction next1 = codes.Current;
                            if (next1.opcode == OpCodes.Ldloca_S && codes.MoveNext()) {
                                CodeInstruction next2 = codes.Current;
                                if (next2.opcode == OpCodes.Callvirt && codes.MoveNext()) {
                                    CodeInstruction next3 = codes.Current;
                                    if (next3.Is(OpCodes.Call, getTreeInstance)) {
                                        sigFound = true;
                                        LocalBuilder terrainAvg = next.operand as LocalBuilder;
                                        LocalBuilder terrainMax = next1.operand as LocalBuilder;
                                        yield return cur;
                                        yield return next;
                                        yield return next1;
                                        yield return next2;
                                        yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TreeEffectOnWind)));
                                        yield return new CodeInstruction(OpCodes.Brtrue_S, returnTreeManagerLabel);
                                        yield return new CodeInstruction(OpCodes.Ldloc_S, terrainAvg);
                                        yield return new CodeInstruction(OpCodes.Ldloc_S, terrainMax);
                                        yield return new CodeInstruction(OpCodes.Add);
                                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                                        yield return new CodeInstruction(OpCodes.Shr);
                                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                                        yield return new CodeInstruction(OpCodes.Ldc_I4, 65535);
                                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Clamp", new Type[] { typeof(int), typeof(int), typeof(int) }));
                                        yield return new CodeInstruction(OpCodes.Conv_U2);
                                        yield return new CodeInstruction(OpCodes.Ret);
                                        yield return next3.WithLabels(returnTreeManagerLabel);
                                    } else {
                                        yield return cur;
                                        yield return next;
                                        yield return next1;
                                        yield return next2;
                                        yield return next3;
                                    }
                                } else {
                                    yield return cur;
                                    yield return next;
                                    yield return next1;
                                    yield return next2;
                                }
                            } else {
                                yield return cur;
                                yield return next;
                                yield return next1;
                            }
                        } else {
                            yield return cur;
                            yield return next;
                        }
                    } else {
                        yield return cur;
                    }
                }
            }
        }


        /* For Forestry Lock */
        private static IEnumerable<CodeInstruction> NRMTreesModifiedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label jump = il.DefineLabel();
            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseLockForestry)));
            yield return new CodeInstruction(OpCodes.Brfalse_S, jump);
            yield return new CodeInstruction(OpCodes.Ret);
            using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
                if (codes.MoveNext()) codes.Current.WithLabels(jump);
                do {
                    if (codes.Current.Is(OpCodes.Ldc_I4, LastMaxTreeLimit)) {
                        yield return new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit);
                    } else {
                        yield return codes.Current;
                    }
                } while (codes.MoveNext());
            }
        }


        internal void InjectTreeLimit(Harmony harmony) {
            HarmonyMethod replaceLDCI4 = new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(ReplaceLDCI4_MaxTreeLimit)));
            harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.SaveProps)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.ClearDecorations)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(CommonBuildingAI), @"HandleFireSpread"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(DisasterHelpers), nameof(DisasterHelpers.DestroyTrees)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(FireCopterAI), @"FindBurningTree"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(ForestFireAI), @"FindClosestTree"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeTool), @"ApplyBrush"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(NRMTreesModifiedTranspiler))));
        }

        internal void RemoveTreeLimitPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.SaveProps)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.ClearDecorations)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(CommonBuildingAI), @"HandleFireSpread"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(DisasterHelpers), nameof(DisasterHelpers.DestroyTrees)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(FireCopterAI), @"FindBurningTree"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(ForestFireAI), @"FindClosestTree"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), @"ApplyBrush"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), HarmonyPatchType.Transpiler, HARMONYID);
        }

        private void EnableTreeLimitPatches(Harmony harmony) {
            try {
                InjectTreeLimit(harmony);
                harmony.Patch(AccessTools.Method(typeof(WeatherManager), @"CalculateSelfHeight"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CalculateSelfHeightTranspiler))));
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void DisableTreeLimitPatches(Harmony harmony) {
            RemoveTreeLimitPatches(harmony);
            harmony.Unpatch(AccessTools.Method(typeof(WeatherManager), @"CalculateSelfHeight"), HarmonyPatchType.Transpiler, HARMONYID);
        }

    }
}
