using UnityEngine;

namespace TreeAnarchy {
    public partial class TAManager {
        public TreeInfo CreatePrefab() {
            TreeInfo origPrefab = default;
            int prefabCount = PrefabCollection<TreeInfo>.PrefabCount();

            for (uint i = 0; i < prefabCount; i++) {
                origPrefab = PrefabCollection<TreeInfo>.GetPrefab(i);
                MeshFilter filter = origPrefab.GetComponent<MeshFilter>();
                if (filter.sharedMesh.isReadable) {
                    break;
                }
            }
            GameObject go = UnityEngine.Object.Instantiate(origPrefab.gameObject);
            TreeInfo newPrefab = go.GetComponent<TreeInfo>();
            newPrefab.m_lodMesh1 =
            newPrefab.m_lodMesh4 =
            newPrefab.m_lodMesh8 =
            newPrefab.m_lodMesh16 = null;
            newPrefab.m_lodMeshData1 =
            newPrefab.m_lodMeshData4 =
            newPrefab.m_lodMeshData8 =
            newPrefab.m_lodMeshData16 = null;
            newPrefab.m_generatedInfo = new TreeInfoGen();
            return newPrefab;
        }

        public TreeInfo ClonePrefab(TreeInfo origPrefab) {
            GameObject go = UnityEngine.Object.Instantiate(origPrefab.gameObject);
            go.GetComponent<PrefabInfo>().m_isCustomContent = true;
            TreeInfo clone = go.GetComponent<TreeInfo>();
            return clone;
        }
    }
}
