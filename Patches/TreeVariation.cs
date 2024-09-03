using ColossalFramework.Math;
using HarmonyLib;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TreeAnarchy {
    internal static partial class TAPatcher {
        private static void EnableTreeVariationPatches(Harmony harmony) {
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderGeometry)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeToolRenderGeometryTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeTool::RenderGeometry");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderOverlay), new Type[] { typeof(RenderManager.CameraInfo) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeToolRenderOverlayTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed TreeTool::RenderOverlay");
                TAMod.TALog(e.Message);
                throw;
            }
        }

        private static void DisableTreeVariationPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderGeometry)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderOverlay), new Type[] { typeof(RenderManager.CameraInfo) }),
                HarmonyPatchType.Transpiler, HARMONYID);
        }

        private static void PatchMoveItTreeVariation(Harmony harmony) {
            try {
                harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderOverlay)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(MoveableTreeRenderOverlayTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch MoveIt::MoveableTree::RenderOverlay, this is non-Fatal");
                TAMod.TALog(e.Message);
            }
        }

        private static void DisableMoveItTreeVariationPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderOverlay)), HarmonyPatchType.Transpiler, HARMONYID);
        }

        private static IEnumerable<CodeInstruction> TreeInstancePopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions) {
            bool skip = false;
            ConstructorInfo randomizer = AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) });
            foreach (var code in instructions) {
                if (!skip && code.opcode == OpCodes.Call && code.operand == randomizer) {
                    skip = true;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 3);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.CalcTreeScale)));
                } else if (skip && code.opcode == OpCodes.Stloc_S && (code.operand as LocalBuilder).LocalIndex == 4) {
                    skip = false;
                    yield return code;
                } else if (!skip) {
                    yield return code;
                }
            }
        }

        /* This patch handles installing functions for Tree Grouping and Tree Terrain Conforming */
        private static IEnumerable<CodeInstruction> TreeInstanceRenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions) {
#if ENABLETERRAINCONFORM
            Label notTCTree = il.DefineLabel();
#endif
#if ENABLETREEGROUP
            Label notGroupedTree = il.DefineLabel();
#endif
            LocalBuilder brightness = default, defaultColorLocation = default;
            ConstructorInfo randomizer = AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) });
            FieldInfo vector4z = AccessTools.Field(typeof(Vector4), nameof(Vector4.z));
            using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    CodeInstruction cur = codes.Current;
                    if (cur.opcode == OpCodes.Call && cur.operand == randomizer) {
                        yield return cur;
                        yield return new CodeInstruction(OpCodes.Ldarg_2);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.CalcTreeScale)));
                        while (codes.MoveNext()) {
                            cur = codes.Current;
                            if (cur.opcode == OpCodes.Stloc_3) {
                                yield return cur;
                                break;
                            }
                        }
                    } else if (cur.opcode == OpCodes.Stloc_S && codes.MoveNext()) {
                        var next = codes.Current;
                        if (next.LoadsField(AccessTools.Field(typeof(RenderManager), nameof(RenderManager.DefaultColorLocation))) && codes.MoveNext()) {
                            var next1 = codes.Current;
                            brightness = cur.operand as LocalBuilder;
                            defaultColorLocation = next1.operand as LocalBuilder;
                            yield return cur;
                            yield return next;
                            yield return next1;
                        } else {
                            yield return cur;
                            yield return next;
                        }
                    } else if (cur.StoresField(vector4z) && codes.MoveNext()) {
                        yield return cur;
#if ENABLETREEGROUP
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags)));
                        yield return new CodeInstruction(OpCodes.Ldc_I4_8);
                        yield return new CodeInstruction(OpCodes.And);
                        yield return new CodeInstruction(OpCodes.Brfalse_S, notGroupedTree);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_2);
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, brightness);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, defaultColorLocation);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.RenderGroupInstance)));
                        yield return new CodeInstruction(OpCodes.Ret);
#endif
#if ENABLETERRAINCONFORM
#if ENABLETREEGROUP
                        yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(notGroupedTree);
#else
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
#endif
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags)));
                        yield return new CodeInstruction(OpCodes.Ldc_I4, (int)TAManager.TerrainConformFlag);
                        yield return new CodeInstruction(OpCodes.And);
                        yield return new CodeInstruction(OpCodes.Brfalse_S, notTCTree);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_2);
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        yield return new CodeInstruction(OpCodes.Ldloc_3);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, brightness);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, defaultColorLocation);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.RenderTCInstance)));
                        yield return new CodeInstruction(OpCodes.Ret);
                        yield return codes.Current.WithLabels(notTCTree);
#else
#if ENABLETREEGROUP
                        yield return codes.Current.WithLabels(notGroupedTree);
#else
                        yield return codes.Current;
#endif
#endif
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> TreeToolRenderGeometryTranspiler(IEnumerable<CodeInstruction> instructions) {
            bool skip = false;
            ConstructorInfo randomizer = AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) });
            foreach (var code in instructions) {
                if (!skip && code.opcode == OpCodes.Call && code.operand == randomizer) {
                    skip = true;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 3);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.GetSeedTreeScale)));
                } else if (skip && code.opcode == OpCodes.Stloc_S && (code.operand as LocalBuilder).LocalIndex == 4) {
                    skip = false;
                    yield return code;
                } else if (!skip) {
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> TreeToolRenderOverlayTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase method) {
            bool skipFirst = false;
            int randomizerLocalIndex = 3;
            ConstructorInfo randomizer = AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) });
            var variables = method.GetMethodBody().LocalVariables;
            foreach (var variable in variables) {
                if (!skipFirst && variable.LocalType == typeof(Randomizer)) {
                    skipFirst = true;
                } else if (skipFirst && variable.LocalType == typeof(Randomizer)) {
                    randomizerLocalIndex = variable.LocalIndex;
                }
            }
            using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Call && cur.operand == randomizer) {
                        yield return cur;
                        yield return new CodeInstruction(OpCodes.Ldloca_S, randomizerLocalIndex);
                        yield return new CodeInstruction(OpCodes.Ldloc_2);
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.GetSeedTreeScale)));
                        while (codes.MoveNext()) {
                            cur = codes.Current;
                            if (cur.opcode == OpCodes.Stloc_S) {
                                yield return cur;
                                break;
                            }
                        }
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> MoveableTreeRenderOverlayTranspiler(IEnumerable<CodeInstruction> instructions) {
            bool skip = false;
            ConstructorInfo randomizer = AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) });
            foreach (var code in instructions) {
                if (!skip && code.opcode == OpCodes.Call && code.operand == randomizer) {
                    skip = true;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.CalcTreeScale)));
                } else if (skip && code.opcode == OpCodes.Stloc_S && (code.operand as LocalBuilder).LocalIndex == 5) {
                    skip = false;
                    yield return code;
                } else if (!skip) {
                    yield return code;
                }
            }
        }
    }
}