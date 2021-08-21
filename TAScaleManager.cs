using ColossalFramework;
using ColossalFramework.Math;
using MoveIt;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TreeAnarchy {
    public partial class TAManager : SingletonLite<TAManager> {
        public const float minScale = 0.2f;
        public const float maxScale = 5.0f;
        public const float scaleStep = 0.2f;
        public uint m_currentTreeID = 0;
        public float[] m_treeScales;

        public void SetScaleBuffer(int maxSize) {
            m_treeScales = new float[maxSize];
        }

        private float CalculateCustomScale(float val, uint treeID) {
            float[] treeScales = m_treeScales;
            float scale = val + treeScales[treeID];
            if (scale > maxScale) treeScales[treeID] -= scaleStep;
            else if (scale < minScale) treeScales[treeID] += scaleStep;
            return val + treeScales[treeID];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float CalcTreeScale(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) => instance.CalcTreeScaleImpl(ref randomizer, treeID, treeInfo);

        private float CalcTreeScaleImpl(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) =>
            CalculateCustomScale(treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f, treeID);

        public static float GetSeedTreeScale(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) {
            if (treeInfo == null) return 0;
            instance.m_currentTreeID = treeID;
            return instance.CalculateCustomScale(treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f, treeID);
        }

        public void IncrementTreeSize() {
            TreeTool treeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
            uint treeID = m_currentTreeID;
            if (treeTool != null && treeTool.m_mode == TreeTool.Mode.Single && Cursor.visible && treeID > 1) {
                m_treeScales[treeID] += scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default/* || MoveItTool.ToolState == MoveItTool.ToolStates.Cloning*/) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 0) {
                        m_treeScales[instance.id.Tree] += scaleStep;
                    }
                }
            }
        }

        public void DecrementTreeSize() {
            TreeTool treeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
            uint treeID = m_currentTreeID;
            if (treeTool != null && treeTool.m_mode == TreeTool.Mode.Single && Cursor.visible && treeID > 1) {
                m_treeScales[treeID] -= scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default/* || MoveItTool.ToolState == MoveItTool.ToolStates.Cloning*/) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                foreach (Instance instance in Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 0) {
                        m_treeScales[instance.id.Tree] -= scaleStep;
                    }
                }
            }
        }
    }
}
