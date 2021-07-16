using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace TreeAnarchy.Patches {
    public class TreeVariation {
        private const float minScale = 0.5f;
        private const float maxScale = 5.0f;
        private const float scaleStep = 0.2f;
        public static float[] m_treeScale = null;
        private static uint currentTreeID = 0;
        private static bool isTreeVariationPatched = false;

        internal static void EnablePatch(Harmony harmony) {
            InitTreeScaleCapacity();
            if (!isTreeVariationPatched) {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(uint), typeof(int) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.TreeInstanceRenderInstanceTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                    new Type[] { typeof(uint), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData),
                             typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.TreeInstancePopulateGroupDataTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderGeometry)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.TreeToolRenderGeometryTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderOverlay), new Type[] { typeof(RenderManager.CameraInfo) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.TreeToolRenderOverlayTranspiler))));
                isTreeVariationPatched = true;
            }
        }

        internal static void DisablePatch(Harmony harmony) {
        }

        internal static void IncrementScaler() {
            TreeTool treeTool = Singleton<TreeTool>.instance;
            uint treeID = currentTreeID;
            if (treeTool.m_mode == TreeTool.Mode.Single && treeTool.isActiveAndEnabled && Cursor.visible && treeID > 1) {
                m_treeScale[treeID] += scaleStep;
            } else if (MoveItTool.ToolState == MoveItTool.ToolStates.Default && MoveItTool.instance.isActiveAndEnabled && ActionQueue.instance.current is SelectAction) {
                foreach (Instance instance in MoveIt.Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 1) {
                        m_treeScale[instance.id.Tree] += scaleStep;
                    }
                }
            }
        }

        internal static void DecrementScaler() {
            TreeTool treeTool = Singleton<TreeTool>.instance;
            uint treeID = currentTreeID;
            if (treeTool.m_mode == TreeTool.Mode.Single && treeTool.isActiveAndEnabled && Cursor.visible && treeID > 1) {
                m_treeScale[treeID] -= scaleStep;
            } else if (MoveItTool.ToolState == MoveItTool.ToolStates.Default && MoveItTool.instance.isActiveAndEnabled && ActionQueue.instance.current is SelectAction) {
                foreach (Instance instance in MoveIt.Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 1) {
                        m_treeScale[instance.id.Tree] -= scaleStep;
                    }
                }
            }
        }

        private static void InitTreeScaleCapacity() {
            if (m_treeScale == null) {
                m_treeScale = new float[TAMod.MaxTreeLimit];
                for (int i = 0; i < m_treeScale.Length; i++) {
                    m_treeScale[i] = 0;
                }
            }
        }

        private static float CalculateCustomScale(float val, uint treeID) {
            float scale = val + m_treeScale[treeID];
            if (scale > maxScale) {
                scale = val + (m_treeScale[treeID] -= scaleStep);
            } else if (scale < minScale) {
                scale = val + (m_treeScale[treeID] += scaleStep);
            }
            return scale;
        }

        public static float GetTreeScale(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) {
            float scale = CalculateCustomScale(treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f, treeID);
            Singleton<TreeManager>.instance.UpdateTreeRenderer(treeID, true);
            return scale;
        }

        public static float GetSeedTreeScale(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) {
            currentTreeID = treeID;
            return CalculateCustomScale(treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f, treeID);
        }

        private static IEnumerable<CodeInstruction> TreeInstancePopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions) {
            int firstIndex = 0, lastIndex = 0;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) })) && firstIndex == 0) {
                    firstIndex = i + 1;
                }
                if (codes[i].opcode == OpCodes.Stloc_S && (codes[i].operand as LocalBuilder).LocalIndex == 4 && lastIndex == 0) {
                    lastIndex = i;
                    break;
                }
            }

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 3),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetTreeScale)))
            };
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, snippet);

            return codes;
        }

        private static IEnumerable<CodeInstruction> TreeInstanceRenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions) {
            int firstIndex = 0, lastIndex = 0;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) })) && firstIndex == 0) {
                    firstIndex = i + 1;
                }
                if (codes[i].opcode == OpCodes.Stloc_3 && lastIndex == 0) {
                    lastIndex = i;
                    break;
                }
            }

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 2),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetTreeScale)))
            };
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, snippet);

            return codes;
        }

        private static IEnumerable<CodeInstruction> TreeToolRenderGeometryTranspiler(IEnumerable<CodeInstruction> instructions) {
            int firstIndex = 0, lastIndex = 0;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) })) && firstIndex == 0) {
                    firstIndex = i + 1;
                }
                if (codes[i].opcode == OpCodes.Stloc_S && (codes[i].operand as LocalBuilder).LocalIndex == 4 && lastIndex == 0) {
                    lastIndex = i;
                    break;
                }
            }

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 3),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetSeedTreeScale)))
            };
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, snippet);

            return codes;
        }

        private static IEnumerable<CodeInstruction> TreeToolRenderOverlayTranspiler(IEnumerable<CodeInstruction> instructions) {
            int firstIndex = 0, lastIndex = 0;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Call && codes[i].OperandIs(AccessTools.Constructor(typeof(Randomizer), new Type[] { typeof(uint) })) && firstIndex == 0) {
                    firstIndex = i + 1;
                }
                if (codes[i].opcode == OpCodes.Stloc_S && (codes[i].operand as LocalBuilder).LocalIndex == 5 && lastIndex == 0) {
                    lastIndex = i;
                    break;
                }
            }

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 4),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetSeedTreeScale)))
            };
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, snippet);

            return codes;
        }
    }
}