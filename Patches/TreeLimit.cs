using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal static partial class TAPatcher {
        private static IEnumerable<CodeInstruction> Replace262144Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            LocalBuilder treeLimit = il.DeclareLocal(typeof(int));
            MethodInfo tmInstance = AccessTools.PropertyGetter(typeof(Singleton<TreeManager>), nameof(Singleton<TreeManager>.instance));
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Call && cur.operand == tmInstance && codes.MoveNext()) {
                        var next = codes.Current;
                        if (next.opcode == OpCodes.Stloc_S) {
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(MaxTreeLimit)));
                            yield return new CodeInstruction(OpCodes.Stloc_S, treeLimit);
                            yield return cur;
                            yield return next;
                        } else {
                            yield return cur;
                            yield return next;
                        }
                    } else if (cur.Is(OpCodes.Ldc_I4, DefaultTreeLimit)) {
                        yield return new CodeInstruction(OpCodes.Ldloc_S, treeLimit);
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> RemoveBoundaryCheckTranspiler(IEnumerable<CodeInstruction> instructions) {
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Ldloc_S && cur.operand is LocalBuilder treeLimit && treeLimit.LocalType == typeof(int) && codes.MoveNext()) {
                        var next = codes.Current;
                        if (next.opcode == OpCodes.Ldc_I4_1 && codes.MoveNext()) {
                            var next1 = codes.Current;
                            if (next1.opcode == OpCodes.Add && codes.MoveNext()) {
                                var next2 = codes.Current;
                                if (next2.opcode == OpCodes.Dup && codes.MoveNext()) {
                                    var next3 = codes.Current;
                                    if (next3.opcode == OpCodes.Stloc_S && next3.operand is LocalBuilder treeLimit2 && treeLimit2.LocalIndex == treeLimit.LocalIndex && codes.MoveNext()) {
                                        var next4 = codes.Current;
                                        if (next4.LoadsConstant(DefaultTreeLimit)) {
                                            while (codes.MoveNext()) {
                                                if (codes.Current.opcode == OpCodes.Br) break;
                                            }
                                        } else {
                                            yield return cur;
                                            yield return next;
                                            yield return next1;
                                            yield return next2;
                                            yield return next3;
                                            yield return next4;
                                        }
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
                                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EMath), "Clamp", new Type[] { typeof(int), typeof(int), typeof(int) }));
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

        private unsafe static void TreesModifiedCoroutine(NaturalResourceManager nrmInstance, Vector3 position) {
            if (!UseLockForestry) {
                int x = EMath.Clamp((int)(position.x / 33.75f + 256f), 0, 511);
                int y = EMath.Clamp((int)(position.z / 33.75f + 256f), 0, 511);
                float num3 = (x - 256f) * 33.75f;
                float num4 = (y - 256f) * 33.75f;
                float num5 = (x + 1 - 256f) * 33.75f;
                float num6 = (y + 1 - 256f) * 33.75f;
                int startX = EMath.Max((int)((x - 256f) * (33.75f / 32f) + 270f), 0);
                int startZ = EMath.Max((int)((y - 256f) * (33.75f / 32f) + 270f), 0);
                int endX = EMath.Min((int)((x + 1 - 256f) * (33.75f / 32f) + 270f), 539);
                int endZ = EMath.Min((int)((y + 1 - 256f) * (33.75f / 32f) + 270f), 539);
                TreeManager instance = Singleton<TreeManager>.instance;
                int num11 = 0;
                int num12 = 0;
                fixed (uint* pGrid = &instance.m_treeGrid[0])
                fixed (TreeInstance* pTree = &instance.m_trees.m_buffer[0]) {
                    for (int i = startZ; i <= endZ; i++) {
                        for (int j = startX; j <= endX; j++) {
                            uint treeID = *(pGrid + (i * 540 + j));
                            while (treeID != 0u) {
                                TreeInstance* tree = pTree + treeID;
                                if ((tree->m_flags & 3) == 1) {
                                    Vector3 position2 = tree->Position;
                                    if (position2.x >= num3 && position2.z >= num4 && position2.x <= num5 && position2.z <= num6) {
                                        num11 += 15;
                                        num12 += tree->GrowState;
                                    }
                                }
                                treeID = tree->m_nextGridTree;
                            }
                        }
                    }
                }
                byte b = (byte)EMath.Min(num11 * 4, 255);
                byte b2 = (byte)EMath.Min(num12 * 4, 255);
                NaturalResourceManager.ResourceCell resourceCell = nrmInstance.m_naturalResources[y * 512 + x];
                if (b != resourceCell.m_forest || b2 != resourceCell.m_tree) {
                    bool flag = b != resourceCell.m_forest;
                    resourceCell.m_forest = b;
                    resourceCell.m_tree = b2;
                    if (b > 0) {
                        resourceCell.m_fertility = 0;
                    }
                    nrmInstance.m_naturalResources[y * 512 + x] = resourceCell;
                    if (flag) {
                        nrmInstance.AreaModified(x, y, x, y);
                    }
                }
            }
        }

        /* For Forestry Lock */
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> NRMTreesModifiedTranspiler(IEnumerable<CodeInstruction> instructions) {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.TreesModifiedCoroutine)));
            yield return new CodeInstruction(OpCodes.Ret);
        }

        private static void EnableTreeLimitPatches(Harmony harmony) {
            HarmonyMethod replace262144 = new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(Replace262144Transpiler)));
            HarmonyMethod removeBoundaryCheck = new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(RemoveBoundaryCheckTranspiler)));
            try {
                harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.SaveProps)), transpiler: replace262144);
            } catch (Exception e) {
                TALog("Failed to patch BuildingDecoration::SaveProps");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.ClearDecorations)), transpiler: replace262144);
            } catch (Exception e) {
                TALog("Failed to patch BuildingDecoration::ClearDecoration");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(CommonBuildingAI), @"HandleFireSpread"), transpiler: removeBoundaryCheck);
            } catch (Exception e) {
                TALog("Failed to patch CommonBuildingAI::HandleFireSpread");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(DisasterHelpers), nameof(DisasterHelpers.DestroyTrees)), transpiler: removeBoundaryCheck);
            } catch (Exception e) {
                TALog("Failed to patch DisasterHelpers::DestroyTrees");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(DistrictManager), @"MoveParkTrees"), transpiler: removeBoundaryCheck);
            } catch (Exception e) {
                TALog("Failed to patch DistrictManager::MoveParkTrees");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(FireCopterAI), @"FindBurningTree"), transpiler: removeBoundaryCheck);
            } catch (Exception e) {
                TALog("Failed to patch FireCopterAI::FindBurningTree");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(ForestFireAI), @"FindClosestTree"), transpiler: removeBoundaryCheck);
            } catch (Exception e) {
                TALog("Failed to patch ForestFireAI::FindClosestTree");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(NRMTreesModifiedTranspiler))));
            } catch (Exception e) {
                TALog("Failed to patch NaturalResourceManager::TreesModified");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeTool), @"ApplyBrush"), transpiler: removeBoundaryCheck);
            } catch (Exception e) {
                TALog("Failed to patch TreeTool::ApplyBrush");
                TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(WeatherManager), @"CalculateSelfHeight"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CalculateSelfHeightTranspiler))));
            } catch (Exception e) {
                TALog("Failed to patch WeatherManager::CalculateSelfHeight. This is non-Fatal");
                TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(WeatherManager), @"FindStrikeTarget"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CalculateSelfHeightTranspiler))));
            } catch (Exception e) {
                TALog("Failed to patch WeatherManager::FindStrikeTarget");
                TALog(e.Message);
                throw;
            }
        }

        private static void DisableTreeLimitPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.SaveProps)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.ClearDecorations)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(CommonBuildingAI), @"HandleFireSpread"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(DisasterHelpers), nameof(DisasterHelpers.DestroyTrees)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(DistrictManager), @"MoveParkTrees"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(FireCopterAI), @"FindBurningTree"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(ForestFireAI), @"FindClosestTree"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), @"ApplyBrush"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(WeatherManager), @"CalculateSelfHeight"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(WeatherManager), @"FindStrikeTarget"), HarmonyPatchType.Transpiler, HARMONYID);
        }
    }
}
