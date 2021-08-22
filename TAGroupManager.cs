using ColossalFramework;
using ColossalFramework.Math;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace TreeAnarchy {
    public partial class TAManager : SingletonLite<TAManager> {
        public const ushort TreeGroupFlag = 0x0008;
        public const ushort TreeGroupMask = 0xfff7;
        public struct CustomTreeInstance {
            public uint oldTreeID;
            public Vector3 offset;
            public Vector3 position;
            public float scale;
            public TreeInstance tree;
        }
        public struct TreeGroupList {
            public ushort m_prefabIndex;
            public TreeInfo m_info;
            public FastList<CustomTreeInstance> m_groupList;
        }

        private Randomizer m_randomizer;
        public ushort m_defaultInfoIndex;
        public static Dictionary<uint, FastList<TreeGroupList>> m_groupedTrees;
        public int m_groupedCount;

        public TAManager() {
            m_groupedCount = 0;
            m_groupedTrees = new Dictionary<uint, FastList<TreeGroupList>>();
            m_randomizer = new Randomizer((int)DateTime.Now.Ticks);
        }

        public static void RenderGroupInstance(RenderManager.CameraInfo cameraInfo, uint treeID, Vector3 position, float brightness, Vector4 objectIndex) {
            if (cameraInfo == null || cameraInfo.CheckRenderDistance(position, RenderManager.LevelOfDetailFactor * 300f)) {
                TreeManager instance = Singleton<TreeManager>.instance;
                MaterialPropertyBlock materialBlock = instance.m_materialBlock;
                materialBlock.Clear();
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                instance.m_drawCallData.m_defaultCalls += 1;
                TreeGroupList[] treeGroup = m_groupedTrees[treeID].m_buffer;
                for (int i = 0; i < treeGroup.Length; i++) {
                    TreeInfo prefab = treeGroup[i].m_info;
                    CustomTreeInstance[] trees = treeGroup[i].m_groupList.m_buffer;
                    Matrix4x4[] matrices = new Matrix4x4[trees.Length];
                    Color value = treeGroup[i].m_info.m_defaultColor * brightness;
                    value.a = TAPatcher.GetWindSpeed(position);
                    materialBlock.SetColor(instance.ID_Color, value);
                    for (int j = 0; j < trees.Length; j++) {
                        Vector3 curPos = position + trees[j].offset;
                        float scale = trees[j].scale;
                        matrices[j].SetTRS(curPos, TAPatcher.GetRandomQuaternion(curPos.sqrMagnitude), new Vector3(scale, scale, scale));
                        //Graphics.DrawMesh(prefab.m_mesh, matrices[j], prefab.m_material, prefab.m_prefabDataLayer, null, 0, materialBlock);
                    }
                    Graphics.DrawMeshInstanced(prefab.m_mesh, 0, prefab.m_material, matrices, trees.Length, materialBlock, UnityEngine.Rendering.ShadowCastingMode.Off, false, prefab.m_prefabDataLayer);
                }
            }
            /*
            else {
                position.y += info.m_generatedInfo.m_center.y * (scale - 1f);
                Color color = info.m_defaultColor * brightness;
                color.a = Singleton<WeatherManager>.instance.GetWindSpeed(position);
                info.m_lodLocations[info.m_lodCount] = new Vector4(position.x, position.y, position.z, scale);
                info.m_lodColors[info.m_lodCount] = color.linear;
                info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
                info.m_lodMin = Vector3.Min(info.m_lodMin, position);
                info.m_lodMax = Vector3.Max(info.m_lodMax, position);
                if (++info.m_lodCount == info.m_lodLocations.Length) {
                    TreeInstance.RenderLod(cameraInfo, info);
                }
            }
            */
        }

        public static void RenderGroupLod() {

        }

        private ushort InsertPrefabCollection(TreeInfo treeInfo) {
            FastList<PrefabCollection<TreeInfo>.PrefabData> m_simulationPrefabs =
                typeof(PrefabCollection<TreeInfo>).GetField("m_simulationPrefabs", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).
                GetValue(null) as FastList<PrefabCollection<TreeInfo>.PrefabData>;
            m_simulationPrefabs.Add(new PrefabCollection<TreeInfo>.PrefabData {
                m_name = treeInfo.name,
                m_refcount = 1,
                m_replaced = false,
                m_prefab = treeInfo
            });
            return (ushort)(m_simulationPrefabs.m_size - 1);
        }

        public TreeInfo CreateTreePrefab(out ushort infoIndex, string name = null) {
            int prefabCount = PrefabCollection<TreeInfo>.PrefabCount();
            TreeInfo infoOrig = default;
            for(uint i = 0; i < prefabCount; i++) {
                infoOrig = PrefabCollection<TreeInfo>.GetPrefab(i);
                if (infoOrig.m_mesh.isReadable) break;
            }

            GameObject go = new GameObject(string.IsNullOrEmpty(name) ? name : "TATreePrefab");
            go.transform.parent = infoOrig.transform.parent;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.mesh = UnityEngine.Object.Instantiate(infoOrig.m_mesh);
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.material = UnityEngine.Object.Instantiate(infoOrig.m_material);
            TreeInfo prefab = go.AddComponent<TreeInfo>();
            prefab.m_generatedInfo = new TreeInfoGen {
                m_treeInfo = prefab,
            };
            prefab.m_class = new ItemClass();
            prefab.m_mesh = filter.sharedMesh;
            infoIndex = InsertPrefabCollection(prefab);
            prefab.m_generatedInfo.m_treeInfo.m_prefabDataIndex = infoIndex;
            prefab.m_prefabDataLayer = infoOrig.m_prefabDataLayer;
            prefab.m_prefabDataIndex = infoIndex;
            prefab.m_prefabInitialized = true;
            go.GetComponent<PrefabInfo>().m_isCustomContent = true;
            go.SetActive(false);
            return prefab;
        }

        private TreeInfo ClonePrefab(TreeInfo treeInfo = null, string name = null) {
            if (treeInfo is null) {
                treeInfo = PrefabCollection<TreeInfo>.GetPrefab(m_defaultInfoIndex);
            }
            GameObject go = UnityEngine.Object.Instantiate(treeInfo.gameObject);
            go.SetActive(false);
            go.GetComponent<PrefabInfo>().m_isCustomContent = true;
            if (!(name is null)) go.name = name;
            return go.GetComponent<TreeInfo>();
        }


        private bool PackTrees(TreeInstance[] trees, Instance[] selection, out int treeCount, out Vector3 center, out FastList<TreeGroupList> packedTrees) {
            treeCount = 0;
            center = Vector3.zero;
            packedTrees = null;
            Dictionary<ushort, FastList<CustomTreeInstance>> groupList = new Dictionary<ushort, FastList<CustomTreeInstance>>();
            for (int i = 0; i < selection.Length; i++) {
                Instance selected = selection[i];
                uint treeID = selected.id.Tree;
                if (selected is MoveableTree && (trees[treeID].m_flags & TreeGroupFlag) == 0) {
                    ushort infoIndex = trees[treeID].m_infoIndex;
                    Vector3 position = trees[treeID].Position;
                    if (!groupList.ContainsKey(infoIndex)) {
                        groupList.Add(infoIndex, new FastList<CustomTreeInstance>());
                    }
                    groupList[infoIndex].Add(new CustomTreeInstance {
                        oldTreeID = treeID,
                        scale = m_treeScales[treeID],
                        tree = trees[treeID],
                        position = trees[treeID].Position
                    });
                    center += position;
                    treeCount++;
                }
            }
            if (treeCount > 0) {
                center /= treeCount; /* Center is the sum of all position divided by tree count */
                packedTrees = new FastList<TreeGroupList>();
                foreach (KeyValuePair<ushort, FastList<CustomTreeInstance>> entry in groupList) {
                    ushort infoIndex = entry.Key;
                    entry.Value.Trim();
                    CustomTreeInstance[] treeList = entry.Value.m_buffer;
                    for (int i = 0; i < treeList.Length; i++) {
                        treeList[i].offset = center - treeList[i].position;
                    }
                    TreeInfo prefab = PrefabCollection<TreeInfo>.GetPrefab(infoIndex);
                    prefab.m_material.enableInstancing = true;
                    packedTrees.Add(new TreeGroupList {
                        m_prefabIndex = infoIndex,
                        m_info = prefab,
                        m_groupList = entry.Value
                    });
                }
                packedTrees.Trim();
                return true;
            }
            return false;
        }

        private void CalculateGeneratedInfo(TreeInfo prefab, TreeGroupList[] treeGroup) {
            float minX = 0f, maxX = 0f, maxY = 0f, minZ = 0f, maxZ = 0f;
            for (int i = 0; i < treeGroup.Length; i++) {
                CustomTreeInstance[] trees = treeGroup[i].m_groupList.m_buffer;
                MeshFilter[] filters = treeGroup[i].m_info.GetComponentsInChildren<MeshFilter>(true);
                for (int j = 0; j < trees.Length; j++) {
                    Vector3 offset = trees[j].offset;
                    for (int k = 0; k < filters.Length; k++) {
                        Vector3[] vertices = filters[k].sharedMesh.vertices;
                        for (int l = 0; l < vertices.Length; l++) {
                            minX = Mathf.Min(minX, vertices[l].x + offset.x);
                            maxX = Mathf.Max(maxX, vertices[l].x + offset.x);
                            maxY = Mathf.Max(maxY, vertices[l].y + offset.y);
                            minZ = Mathf.Min(minZ, vertices[l].z + offset.z);
                            maxZ = Mathf.Max(maxZ, vertices[l].z + offset.z);
                        }
                    }
                }
            }
            prefab.m_generatedInfo.m_center = new Vector3(0f, maxY * 0.5f, 0f);
            prefab.m_generatedInfo.m_size = new Vector3(Mathf.Max(-minX, maxX) * 2f, maxY, Mathf.Max(-minZ, maxZ) * 2f);
            prefab.m_generatedInfo.m_triangleArea = 0f;
            prefab.m_generatedInfo.m_uvmapArea = 0f;
            prefab.m_generatedInfo.m_treeInfo = prefab;
            prefab.m_generatedInfo.m_treeInfo.m_mesh = prefab.m_mesh;
            prefab.m_prefabInitialized = true;
        }

        private void ReleaseTrees(TreeManager tm, HashSet<Instance> selection) {
            List<Instance> removeList = new List<Instance>();
            foreach(var selected in selection) {
                if (selected is MoveableTree && !selected.id.IsEmpty && selected.id.Tree > 0) {
                    tm.ReleaseTree(selected.id.Tree);
                    removeList.Add(selected);
                }
            }
            foreach(var removeItem in removeList) {
                selection.Remove(removeItem);
            }
        }

        private bool CreateGroupedTrees(out uint groupTreeID, HashSet<Instance> selection) {
            groupTreeID = 0;
            TreeManager tmInstance = Singleton<TreeManager>.instance;
            TreeInstance[] trees = tmInstance.m_trees.m_buffer;
            if (PackTrees(trees, selection.ToArray(), out int treeCount, out Vector3 center, out FastList<TreeGroupList> packedTrees) && treeCount > 1) {
                TreeInfo prefabInfo = ClonePrefab(name: "TAGrouped" + m_groupedCount.ToString());
                CalculateGeneratedInfo(prefabInfo, packedTrees.m_buffer);
                if (tmInstance.CreateTree(out groupTreeID, ref m_randomizer, prefabInfo, center, true)) {
                    ReleaseTrees(tmInstance, selection);
                    trees[groupTreeID].m_flags |= TreeGroupFlag;
                    trees[groupTreeID].m_infoIndex = m_defaultInfoIndex;
                    trees[groupTreeID].Info = prefabInfo;
                    m_groupedTrees.Add(groupTreeID, packedTrees);
                    m_groupedCount++;
                    return true;
                }
            }
            return false;
        }

        public void GroupTrees() {
            if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) && UIToolOptionPanel.instance.isVisible && MoveIt.Action.selection.Count > 0) {
                if(CreateGroupedTrees(out uint treeID, MoveIt.Action.selection)) {
                    MoveIt.Action.selection.Add(new MoveableTree(new InstanceID { Tree = treeID }));
                }
            }
        }

        private Instance[] UngroupTrees(TreeManager tmInstance, TreeInstance[] treeBuffer, uint treeID) {
            Vector3 curPos = treeBuffer[treeID].Position;
            List<Instance> treeSelection = new List<Instance>();
            if (m_groupedTrees.ContainsKey(treeID)) {
                TreeGroupList[] groupList = m_groupedTrees[treeID].m_buffer;
                for (int i = 0; i < groupList.Length; i++) {
                    CustomTreeInstance[] trees = groupList[i].m_groupList.m_buffer;
                    for (int j = 0; j < trees.Length; j++) {
                        if (tmInstance.CreateTree(out uint tree, ref m_randomizer, groupList[i].m_info, curPos + trees[j].offset, true)) {
                            treeSelection.Add(new InstanceID { Tree = tree });
                        }
                    }
                }
                UnityEngine.Object.Destroy(treeBuffer[treeID].Info.gameObject);
                tmInstance.ReleaseTree(treeID);
                return treeSelection.ToArray();
            }
            return null;
        }

        public void UngroupTrees() {
            try {
                while (!Monitor.TryEnter(this, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
                if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) && UIToolOptionPanel.instance.isVisible && MoveIt.Action.selection.Count > 0) {
                    TreeManager tmInstance = Singleton<TreeManager>.instance;
                    TreeInstance[] trees = tmInstance.m_trees.m_buffer;
                    List<Instance> newSelections = new List<Instance>();
                    List<Instance> removeList = new List<Instance>();
                    foreach (Instance selected in MoveIt.Action.selection) {
                        if (selected is MoveableTree) {
                            uint treeID = selected.id.Tree;
                            if ((trees[treeID].m_flags & TreeGroupFlag) != 0) {
                                newSelections.AddRange(UngroupTrees(tmInstance, trees, treeID));
                                removeList.Add(selected);
                            }
                        }
                    }
                    if(removeList.Count > 0) {
                        for(int i = 0; i < removeList.Count; i++) {
                            MoveIt.Action.selection.Remove(removeList[i]);
                        }
                    }
                    if(newSelections.Count > 0) {
                        MoveIt.Action.selection.UnionWith(newSelections);
                    }
                }
            } finally {
                Monitor.Exit(this);
            }
        }

    }
}
