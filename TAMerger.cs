using ColossalFramework;
using ColossalFramework.Math;
using MoveIt;
using TreeAnarchy.Patches;
using UnityEngine;

namespace TreeAnarchy {
    public class TAMerger : Singleton<TAMerger> {
        private const ushort TreeGroup = 0x0008;
        private const ushort TreeGroupMask = 0xfff7;
        private int GetTreeCountInSelection(Instance[] selection) {
            int treeCount = 0;
            for (int i = 0; i < selection.Length; i++) {
                if (selection[i] is MoveableTree) treeCount++;
            }
            return treeCount;
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, TreeInfo info, Vector3 position, float scale, float brightness, Vector4 objectIndex) {

        }

        public static void RenderLod() {

        }

        private void CreateGroupedTrees(Instance[] selection) {
            TreeManager tmInstance = Singleton<TreeManager>.instance;
            int treeCount = GetTreeCountInSelection(selection);
            int treeIndex = 0;
            if (treeCount > 0 && tmInstance.m_trees.CreateItem(out uint newTreeID)) {
                CombineInstance[] combinedMesh = new CombineInstance[treeCount];
                CombineInstance[] combinedLod1 = new CombineInstance[treeCount];
                CombineInstance[] combinedLod4 = new CombineInstance[treeCount];
                CombineInstance[] combinedLod8 = new CombineInstance[treeCount];
                CombineInstance[] combinedLod16 = new CombineInstance[treeCount];
                TreeInstance[] trees = tmInstance.m_trees.m_buffer;
                for (int i = 0; i < selection.Length; i++) {
                    Instance instance = selection[i];
                    uint treeID = instance.id.Tree;
                    if (instance is MoveableTree) {
                        Randomizer randomizer = new Randomizer(treeID);
                        TreeInfo info = PrefabCollection<TreeInfo>.GetPrefab(trees[treeID].m_infoIndex);
                        float scale = TreeVariation.GetTreeScale(ref randomizer, treeID, info);
                        Vector3 position = trees[treeID].Position;
                        Matrix4x4 matrix = default;
                        matrix.SetTRS(position, TreeMovement.GetRandomQuaternion(position.sqrMagnitude), new Vector3(scale, scale, scale));
                        combinedMesh[treeIndex].mesh = info.m_mesh;
                        combinedMesh[treeIndex].transform = matrix;
                        treeIndex++;
                    }
                }
                //trees[newTreeID].m_flags = 
            }
        }

        public int GroupTrees() {
            int treeCount = 0;

            if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default/* || MoveItTool.ToolState == MoveItTool.ToolStates.Cloning*/) &&
                       UIToolOptionPanel.instance.isVisible && MoveIt.Action.selection.Count > 0) {
                foreach (Instance instance in MoveIt.Action.selection) {
                    if (instance is MoveableTree && !instance.id.IsEmpty && instance.id.Tree > 0) {
                        // m_treeScales[instance.id.Tree] += scaleStep;
                    }
                }
            }
            return treeCount;
        }

        public int UngroupTrees() {
            int treeCount = 0;

            return treeCount;
        }
    }
}
