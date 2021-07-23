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
    public class TreeScaleManager : Singleton<TreeScaleManager> {
        public const float minScale = 0.2f;
        public const float maxScale = 5.0f;
        public const float scaleStep = 0.2f;
        public uint currentTreeID = 0;
        public float[] m_treeScales;

        protected void Awake() {
            m_treeScales = new float[TAMod.MaxTreeLimit];
        }

        public void ResizeBuffer(int maxSize) {
            m_treeScales = new float[maxSize];
        }

        public void IncrementTreeSize() {
            TreeTool treeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
            uint treeID = currentTreeID;
            if (treeTool != null && treeTool.m_mode == TreeTool.Mode.Single && Cursor.visible && treeID > 1) {
                m_treeScales[treeID] += scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default/* || MoveItTool.ToolState == MoveItTool.ToolStates.Cloning*/) &&
                       UIToolOptionPanel.instance.isVisible && MoveIt.Action.selection.Count > 0) {
                foreach (Instance instance in MoveIt.Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 0) {
                        m_treeScales[instance.id.Tree] += scaleStep;
                    }
                }
            }
        }

        public void DecrementTreeSize() {
            TreeTool treeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
            uint treeID = currentTreeID;
            if (treeTool != null && treeTool.m_mode == TreeTool.Mode.Single && Cursor.visible && treeID > 1) {
                m_treeScales[treeID] -= scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default/* || MoveItTool.ToolState == MoveItTool.ToolStates.Cloning*/) &&
                       UIToolOptionPanel.instance.isVisible && MoveIt.Action.selection.Count > 0) {
                foreach (Instance instance in MoveIt.Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 0) {
                        m_treeScales[instance.id.Tree] -= scaleStep;
                    }
                }
            }
        }
    }

    public class TreeVariation {
        private static bool isTreeVariationPatched = false;
        private static bool isLatePatched = false;

        internal void EnablePatch(Harmony harmony) {
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

        internal void PatchMoveIt(Harmony harmony) {
            if (!isLatePatched) {
                harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderOverlay)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.MoveableTreeRenderOverlayTranspiler))));
                isLatePatched = true;
            }
        }

        internal void DisablePatch(Harmony harmony) {

        }

        private static float CalculateCustomScale(float val, uint treeID) {
            float[] treeScales = Singleton<TreeScaleManager>.instance.m_treeScales;
            float scale = val + treeScales[treeID];
            if (scale > TreeScaleManager.maxScale) {
                scale = val + (treeScales[treeID] -= TreeScaleManager.scaleStep);
            } else if (scale < TreeScaleManager.minScale) {
                scale = val + (treeScales[treeID] += TreeScaleManager.scaleStep);
            }
            return scale;
        }

        public static float GetTreeScale(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) {
            float scale = CalculateCustomScale(treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f, treeID);
            Singleton<TreeManager>.instance.UpdateTreeRenderer(treeID, true);
            return scale;
        }

        public static float GetSeedTreeScale(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) {
            Singleton<TreeScaleManager>.instance.currentTreeID = treeID;
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
            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 3),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetTreeScale)))
            });

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

            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 2),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetTreeScale)))
            });

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

            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 3),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetSeedTreeScale)))
            });

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

            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 4),
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetSeedTreeScale)))
            });

            return codes;
        }

        private static IEnumerable<CodeInstruction> MoveableTreeRenderOverlayTranspiler(IEnumerable<CodeInstruction> instructions) {
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

            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloca_S, 4),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeVariation), nameof(TreeVariation.GetTreeScale)))
            });

            return codes;
        }
    }
}