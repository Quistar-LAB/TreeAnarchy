using ColossalFramework;
using ColossalFramework.Math;
using MoveIt;
using UnityEngine;

namespace TreeAnarchy {
    public static partial class TAManager {
        public const float minScale = 0.2f;
        public const float maxScale = 5.0f;
        public const float scaleStep = 0.2f;
        public static uint m_currentTreeID = 0;
        public static float[] m_treeScales;
        public static float[] m_defScales;
        public static float[] m_brightness;

        public static void SetScaleBuffer(int maxSize) {
            m_treeScales = new float[maxSize];
            m_defScales = new float[maxSize];
            m_brightness = new float[maxSize];
        }

        private static float CalculateCustomScale(float val, uint treeID) {
            float[] treeScales = m_treeScales;
            float scale = val + treeScales[treeID];
            if (scale > maxScale) treeScales[treeID] -= scaleStep;
            else if (scale < minScale) treeScales[treeID] += scaleStep;
            return val + treeScales[treeID];
        }

        public static float CalcTreeScale(uint treeID) => CalculateCustomScale(m_defScales[treeID], treeID);

        public static float GetSeedTreeScale(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) {
            if (treeInfo is null) return 0;
            m_currentTreeID = treeID;
            return CalculateCustomScale(treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f, treeID);
        }

        public static void IncrementTreeSize() {
            TreeTool treeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
            uint treeID = m_currentTreeID;
            if (treeTool != null && treeTool.m_mode == TreeTool.Mode.Single && Cursor.visible && treeID > 1) {
                m_treeScales[treeID] += scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default/* || MoveItTool.ToolState == MoveItTool.ToolStates.Cloning*/) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                TreeManager tmInstance = Singleton<TreeManager>.instance;
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 0) {
                        m_treeScales[instance.id.Tree] += scaleStep;
                        tmInstance.UpdateTree(instance.id.Tree);
                    }
                }
            }
        }

        public static void DecrementTreeSize() {
            TreeTool treeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
            uint treeID = m_currentTreeID;
            if (treeTool != null && treeTool.m_mode == TreeTool.Mode.Single && Cursor.visible && treeID > 1) {
                m_treeScales[treeID] -= scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default/* || MoveItTool.ToolState == MoveItTool.ToolStates.Cloning*/) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                TreeManager tmInstance = Singleton<TreeManager>.instance;
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 0) {
                        m_treeScales[instance.id.Tree] -= scaleStep;
                        tmInstance.UpdateTree(instance.id.Tree);
                    }
                }
            }
        }
    }
}
