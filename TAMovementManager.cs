using ColossalFramework;
using UnityEngine;

namespace TreeAnarchy {
    public static partial class TAManager {
        public static readonly Quaternion[] m_treeQuaternions = new Quaternion[360];
        private static bool m_updateLODTreeSway = false;
        private static WeatherManager.WindCell[] m_windGrid;

        public static void InitializeSwayManager() {
            for (int i = 0; i < 360; i++) {
                m_treeQuaternions[i] = Quaternion.Euler(0, i, 0);
            }
            m_windGrid = Singleton<WeatherManager>.instance.m_windGrid;
        }

        public static Quaternion GetRandomQuaternion(float magnitude) => m_treeQuaternions[EMath.Abs((int)(((long)magnitude * TAMod.RandomTreeRotationFactor) >> 2) % 359)];

        public static float GetWindSpeed(Vector3 pos) {
            /* Apparently the lambda expression (a = (a ? > 127 : 127 : a) < 0 ? 0 : a) produces
             * unreliable results.. mono bug? Using local functions instead so they can be inlined
             * which shaved off ~5ms for this routine per 1 million calls */
            int clampi(int a) { a = a > 127 ? 127 : a; return a < 0 ? 0 : a; }
            float clampf(float f) { f = f > 2f ? 2f : f; return f < 0f ? 0f : f; }
            int x = clampi((int)(pos.x * 0.0074074074f + 63.5f));
            int y = clampi((int)(pos.z * 0.0074074074f + 63.5f));
            return clampf((pos.y - m_windGrid[y * 128 + x].m_totalHeight * 0.015625f) * 0.02f + 1) * TAMod.TreeSwayFactor;
        }

        private static void UpdateLODProc() {
            int layerID = Singleton<TreeManager>.instance.m_treeLayer;
            FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
            for (int i = 0; i < renderedGroups.m_size; i++) {
                RenderGroup renderGroup = renderedGroups.m_buffer[i];
                RenderGroup.MeshLayer layer = renderGroup.GetLayer(layerID);
                if (!(layer is null)) {
                    layer.m_dataDirty = true;
                }
                renderGroup.UpdateMeshData();
            }
        }

        public static void UpdateTreeSway() => m_updateLODTreeSway = true;

        public static void OnOptionPanelClosed() {
            if (m_updateLODTreeSway) {
                UpdateLODProc();
                m_updateLODTreeSway = false;
            }
        }
    }
}
