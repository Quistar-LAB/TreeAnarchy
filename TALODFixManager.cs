using UnityEngine;

namespace TreeAnarchy {
    public static partial class TAManager {
        private const int LOD_LODRES = 512;
        private const int LOD_MEDIUMRES = 1024;
        private const int LOD_HIGHRES = 2048;
        private const int LOD_ULTRARES = 4096;
        public enum TreeLODResolution : int {
            Low,
            Medium,
            High,
            UltraHigh
        }

        public static void SetResolution(this TreeManager manager, TreeLODResolution resolution) {
            int res = LOD_ULTRARES;
            switch (resolution) {
            case TreeLODResolution.Low: res = LOD_LODRES; break; // This seems to break the game not sure why
            case TreeLODResolution.Medium: res = LOD_MEDIUMRES; break;
            case TreeLODResolution.High: res = LOD_HIGHRES; break;
            case TreeLODResolution.UltraHigh: res = LOD_ULTRARES; break;
            }
            manager.m_renderDiffuseTexture = new RenderTexture(res, res, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) {
                filterMode = FilterMode.Trilinear,
                autoGenerateMips = true
            };
            manager.m_renderXycaTexture = new RenderTexture(res, res, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) {
                filterMode = FilterMode.Trilinear,
                autoGenerateMips = true
            };
            manager.m_renderShadowTexture = new RenderTexture(res, res, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) {
                filterMode = FilterMode.Point,
                autoGenerateMips = false
            };
        }
    }
}
