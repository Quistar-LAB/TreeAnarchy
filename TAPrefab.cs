using UnityEngine;

namespace TreeAnarchy {
    public static partial class TAManager {
        public static TreeInfo CreatePrefab() {
            int prefabCount = PrefabCollection<TreeInfo>.PrefabCount();

            for (uint i = 0; i < prefabCount; i++) {
                TreeInfo origPrefab = PrefabCollection<TreeInfo>.GetPrefab(i);
                MeshFilter filter = origPrefab.GetComponent<MeshFilter>();
                if (filter.sharedMesh.isReadable) {
                    GameObject go = Object.Instantiate(origPrefab.gameObject);
                    TreeInfo newPrefab = go.GetComponent<TreeInfo>();
                    newPrefab.m_lodMesh1 = null;
                    newPrefab.m_lodMesh4 = null;
                    newPrefab.m_lodMesh8 = null;
                    newPrefab.m_lodMesh16 = null;
                    newPrefab.m_lodMeshData1 = null;
                    newPrefab.m_lodMeshData4 = null;
                    newPrefab.m_lodMeshData8 = null;
                    newPrefab.m_lodMeshData16 = null;
                    newPrefab.m_generatedInfo = new TreeInfoGen();
                    return newPrefab;
                }
            }
            return null;
        }

        public static TreeInfo ClonePrefab(TreeInfo origPrefab) {
            GameObject go = Object.Instantiate(origPrefab.gameObject);
            go.GetComponent<PrefabInfo>().m_isCustomContent = true;
            TreeInfo clone = go.GetComponent<TreeInfo>();
            return clone;
        }
    }
}
