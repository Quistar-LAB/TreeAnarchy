using ColossalFramework;
using MoveIt;
using System.Linq;
using UnityEngine;

namespace TreeAnarchy {
    public partial class TAManager : SingletonLite<TAManager> {
        public const ushort TerrainConformFlag = 0x1000;
        public const ushort TerrainConformMask = 0xefff;
        private static readonly Shader defTCShader = Shader.Find("Custom/Props/Prop/Fence");

        private TreeInfo ClonePrefab(TreeInfo origPrefab) {
            TreeInfo newPrefab = CreateTreePrefab(origPrefab.name + "_TC");
            newPrefab.m_mesh = origPrefab.m_mesh;
            newPrefab.m_material = origPrefab.m_material;
            newPrefab.m_material.shader = defTCShader;
            newPrefab.m_defaultColor = origPrefab.m_defaultColor;
            newPrefab.m_material.SetColor("_Color", newPrefab.m_defaultColor);
            newPrefab.m_Thumbnail = origPrefab.m_Thumbnail;
            newPrefab.m_InfoTooltipThumbnail = origPrefab.m_InfoTooltipThumbnail;
            newPrefab.m_InfoTooltipAtlas = origPrefab.m_InfoTooltipAtlas;
            newPrefab.m_Atlas = origPrefab.m_Atlas;
            newPrefab.m_generatedInfo.m_center = origPrefab.m_generatedInfo.m_center;
            newPrefab.m_generatedInfo.m_uvmapArea = origPrefab.m_generatedInfo.m_uvmapArea;
            newPrefab.m_generatedInfo.m_size = origPrefab.m_generatedInfo.m_size;
            newPrefab.m_generatedInfo.m_triangleArea = origPrefab.m_generatedInfo.m_triangleArea;
            newPrefab.m_maxScale = origPrefab.m_maxScale;
            newPrefab.m_minScale = origPrefab.m_minScale;
            newPrefab.m_renderScale = origPrefab.m_renderScale;
            newPrefab.m_prefabInitialized = true;
            return newPrefab;
        }

        private int CreateTerrainConformedTree(Instance[] selection) {
            TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;
            for (int i = 0; i < selection.Length; i++) {
                Instance selected = selection[i];
                uint treeID = selected.id.Tree;
                if (selected is MoveableTree && !selected.id.IsEmpty && treeID > 0) {
                    TreeInfo newPrefab = ClonePrefab(PrefabCollection<TreeInfo>.GetPrefab(trees[treeID].m_infoIndex));
                    if (Singleton<TreeManager>.instance.CreateTree(out uint newTreeID, ref Singleton<SimulationManager>.instance.m_randomizer, newPrefab, selected.position, true)) {
                        Singleton<TreeManager>.instance.ReleaseTree(treeID);
                        trees[newTreeID].m_flags |= TerrainConformFlag;
                        trees[newTreeID].Info = newPrefab;
                        selection[i] = new MoveableTree(new InstanceID { Tree = newTreeID });
                        //trees[treeID].m_infoIndex = infoIndex;
                    }
                }
            }
            return 0;
        }

        public int TerrainConformTrees() {
            if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) && UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                return CreateTerrainConformedTree(Action.selection.ToArray());
            }
            return 0;
        }

        public int UnConformTrees() {
            return 0;
        }
    }
}
