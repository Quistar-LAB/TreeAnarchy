using ColossalFramework;
using ColossalFramework.Math;
using MoveIt;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Action = MoveIt.Action;

namespace TreeAnarchy {
    public unsafe static partial class TAManager {
        public struct ExtraTreeInfo {
            public float m_treeScale;
            public float m_extraScale;
            public float m_brightness;
            public float TreeScale => m_treeScale + m_extraScale;
        }
        public const float minScale = 0.2f;
        public const float maxScale = 5.0f;
        public const float scaleStep = 0.2f;
        private static uint m_currentTreeID = 0;
        public static readonly Quaternion[] m_treeQuaternions = new Quaternion[360];
        public static ExtraTreeInfo[] m_extraTreeInfos;
        private static bool m_updateLODTreeSway = false;
        private static WeatherManager.WindCell[] m_windGrid;

        public static void Initialize() {
            for (int i = 0; i < 360; i++) {
                m_treeQuaternions[i] = Quaternion.Euler(0, i, 0);
            }
            m_windGrid = Singleton<WeatherManager>.instance.m_windGrid;

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void EnsureCapacity(TreeManager manager) {
            if (manager.m_trees.m_buffer.Length != TAMod.MaxTreeLimit) {
                manager.m_trees = new Array32<TreeInstance>((uint)TAMod.MaxTreeLimit);
                manager.m_updatedTrees = new ulong[TAMod.MaxTreeUpdateLimit];
                Array.Clear(manager.m_trees.m_buffer, 0, manager.m_trees.m_buffer.Length);
                manager.m_trees.CreateItem(out uint _);
                m_extraTreeInfos = new ExtraTreeInfo[TAMod.MaxTreeLimit];
#if ENABLETERRAINCOFNORM
                SingletonLite<TAManager>.instance.SetTCBuffer(MaxTreeLimit);
#endif
            }
            manager.SetResolution(TAMod.TreeLODSelectedResolution);
            TAMod.TALog("Setting Tree LOD to " + TAMod.TreeLODSelectedResolution);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe static float GetWindSpeed(Vector3 pos) {
            /* Apparently the lambda expression (a = (a ? > 127 : 127 : a) < 0 ? 0 : a) produces
             * unreliable results.. mono bug? Using local functions instead so they can be inlined
             * which shaved off ~5ms for this routine per 1 million calls */
            WeatherManager.WindCell[] windCells = m_windGrid;
            int x = (int)(pos.x * 0.0074074074f + 63.5f);
            x = x > 127 ? 127 : (x < 0 ? 0 : x);
            int z = (int)(pos.z * 0.0074074074f + 63.5f);
            z = z > 127 ? 127 : (z < 0 ? 0 : z);
            float windSpeed = (pos.y - windCells[z * 128 + x].m_totalHeight * 0.015625f) * 0.02f + 1;
            return (windSpeed > 2f ? 2f : (windSpeed < 0f ? 0f : windSpeed)) * TAMod.TreeSwayFactor;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UpdateLODProc() {
            int layerID = Singleton<TreeManager>.instance.m_treeLayer;
            FastList<RenderGroup> renderedGroupsList = Singleton<RenderManager>.instance.m_renderedGroups;
            RenderGroup[] renderedGroups = renderedGroupsList.m_buffer;
            int len = renderedGroupsList.m_size;
            for (int i = 0; i < len; i++) {
                RenderGroup renderGroup = renderedGroups[i];
                RenderGroup.MeshLayer layer = renderGroup.GetLayer(layerID);
                if (!(layer is null)) {
                    layer.m_dataDirty = true;
                }
                renderGroup.UpdateMeshData();
            }
        }

        public static void UpdateTreeSway() => m_updateLODTreeSway = true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OnOptionPanelClosed() {
            if (m_updateLODTreeSway) {
                UpdateLODProc();
                m_updateLODTreeSway = false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static float CalcTreeScale(uint treeID) {
            ExtraTreeInfo[] extraInfos = m_extraTreeInfos;
            float scale = extraInfos[treeID].TreeScale;
            if (scale > maxScale) extraInfos[treeID].m_extraScale -= scaleStep;
            else if (scale < minScale) extraInfos[treeID].m_extraScale += scaleStep;
            return extraInfos[treeID].TreeScale;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float GetSeedTreeScale(ref Randomizer randomizer, uint treeID, TreeInfo treeInfo) {
            if (treeInfo is null) return 0;
            m_currentTreeID = treeID;
            ExtraTreeInfo[] extraInfos = m_extraTreeInfos;
            float scale = treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
            if (scale > maxScale) extraInfos[treeID].m_extraScale -= scaleStep;
            else if (scale < minScale) extraInfos[treeID].m_extraScale += scaleStep;
            return scale + extraInfos[treeID].m_extraScale;
        }

        public static void IncrementTreeSize() {
            TreeTool treeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
            uint treeID = m_currentTreeID;
            if (!(treeTool is null) && treeTool.m_mode == TreeTool.Mode.Single && Cursor.visible && treeID > 0) {
                m_extraTreeInfos[treeID].m_extraScale += scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                TreeManager tmInstance = Singleton<TreeManager>.instance;
                foreach (Instance instance in Action.selection) {
                    treeID = instance.id.Tree;
                    if (instance is MoveableTree && treeID > 0) {
                        m_extraTreeInfos[treeID].m_extraScale += scaleStep;
                        tmInstance.UpdateTree(treeID);
                    }
                }
            }
        }

        public static void DecrementTreeSize() {
            TreeTool treeTool = ToolsModifierControl.GetCurrentTool<TreeTool>();
            uint treeID = m_currentTreeID;
            if (!(treeTool is null) && treeTool.m_mode == TreeTool.Mode.Single && Cursor.visible && treeID > 0) {
                m_extraTreeInfos[treeID].m_extraScale -= scaleStep;
            } else if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) &&
                       UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                TreeManager tmInstance = Singleton<TreeManager>.instance;
                foreach (Instance instance in Action.selection) {
                    treeID = instance.id.Tree;
                    if (instance is MoveableTree && treeID > 0) {
                        m_extraTreeInfos[treeID].m_extraScale -= scaleStep;
                        tmInstance.UpdateTree(treeID);
                    }
                }
            }
        }
    }
}
