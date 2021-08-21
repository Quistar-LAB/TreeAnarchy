using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
        private void EnableTreeVariationPatches(Harmony harmony) {
            //                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CreateTree)),
            //                    postfix: new HarmonyMethod(AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.CreateTreePostfix))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(uint), typeof(int) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeInstanceRenderInstanceTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(uint), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData),
                            typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeInstancePopulateGroupDataTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderGeometry)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeToolRenderGeometryTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderOverlay), new Type[] { typeof(RenderManager.CameraInfo) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeToolRenderOverlayTranspiler))));
        }

        private void DisableTreeVariationPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(uint), typeof(int) }),
                HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(uint), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData),
                            typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderGeometry)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderOverlay), new Type[] { typeof(RenderManager.CameraInfo) }),
                HarmonyPatchType.Transpiler, HARMONYID);
        }

        private void PatchMoveItTreeVariation(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderOverlay)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(MoveableTreeRenderOverlayTranspiler))));
        }

        private void DisableMoveItTreeVariationPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderOverlay)), HarmonyPatchType.Transpiler, HARMONYID);
        }

        public static void CreateTreePostfix(uint tree) {
            SingletonLite<TAManager>.instance.m_treeScales[tree] = 0;
        }

        private static IEnumerable<CodeInstruction> TreeInstancePopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions) {
            int firstIndex = 0, lastIndex = 0;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (firstIndex == 0 && codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) }))) {
                    firstIndex = i + 1;
                } else if (codes[i].opcode == OpCodes.Stloc_S && (codes[i].operand as LocalBuilder).LocalIndex == 4 && lastIndex == 0) {
                    lastIndex = i;
                    break;
                }
            }
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 3),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.CalcTreeScale)))
            });

            return codes;
        }

        private static IEnumerable<CodeInstruction> TreeInstanceRenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            int firstIndex = 0, lastIndex = 0;
            Label isGroupedTree = il.DefineLabel();
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (firstIndex == 0 && codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) }))) {
                    firstIndex = i + 1;
                } else if (lastIndex == 0 && codes[i].opcode == OpCodes.Stloc_3) {
                    lastIndex = i;
                    codes.RemoveRange(firstIndex, lastIndex - firstIndex);
                    codes.InsertRange(firstIndex, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloca_S, 2),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.CalcTreeScale)))
                    });
                } else if (codes[i].opcode == OpCodes.Ldarg_1 && codes[i + 1].opcode == OpCodes.Ldloc_0 && codes[i - 1].StoresField(AccessTools.Field(typeof(Vector4), nameof(Vector4.z)))) {
                    codes.InsertRange(i, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))),
                        new CodeInstruction(OpCodes.Ldc_I4_8),
                        new CodeInstruction(OpCodes.And),
                        new CodeInstruction(OpCodes.Brtrue_S, isGroupedTree)
                    });
                }
            }
            codes.AddRange(new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldarg_1).WithLabels(isGroupedTree),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldloc_S, 4),
                new CodeInstruction(OpCodes.Ldloc_S, 5),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.RenderGroupInstance))),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> TreeToolRenderGeometryTranspiler(IEnumerable<CodeInstruction> instructions) {
            int firstIndex = 0, lastIndex = 0;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (firstIndex == 0 && codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) }))) {
                    firstIndex = i + 1;
                } else if (codes[i].opcode == OpCodes.Stloc_S && (codes[i].operand as LocalBuilder).LocalIndex == 4 && lastIndex == 0) {
                    lastIndex = i;
                    break;
                }
            }
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 3),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.GetSeedTreeScale)))
            });

            return codes;
        }

        private static IEnumerable<CodeInstruction> TreeToolRenderOverlayTranspiler(IEnumerable<CodeInstruction> instructions) {
            int firstIndex = 0, lastIndex = 0;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (firstIndex == 0 && codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) }))) {
                    firstIndex = i + 1;
                } else if (codes[i].opcode == OpCodes.Stloc_S && (codes[i].operand as LocalBuilder).LocalIndex == 5 && lastIndex == 0) {
                    lastIndex = i;
                    break;
                }
            }
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 4),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.GetSeedTreeScale)))
            });

            return codes;
        }

        private static IEnumerable<CodeInstruction> MoveableTreeRenderOverlayTranspiler(IEnumerable<CodeInstruction> instructions) {
            int firstIndex = 0, lastIndex = 0;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (firstIndex == 0 && codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) }))) {
                    firstIndex = i + 1;
                } else if (codes[i].opcode == OpCodes.Stloc_S && (codes[i].operand as LocalBuilder).LocalIndex == 5 && lastIndex == 0) {
                    lastIndex = i;
                    break;
                }
            }
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 4),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.CalcTreeScale)))
            });

            return codes;
        }
    }
}