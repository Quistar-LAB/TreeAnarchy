#if ENABLETERRAINCONFORM
using ColossalFramework;
using MoveIt;
using UnityEngine;

namespace TreeAnarchy {
    public partial class TAManager : SingletonLite<TAManager> {
        public const ushort TerrainConformFlag = 0x1000;
        public const ushort TerrainConformMask = 0xefff;
        private static readonly Shader defTCShader = Shader.Find("Custom/Props/Prop/Fence");
        private static Material[] m_TCMaterial = default;
        private static int ID_HeightMap = Shader.PropertyToID("_HeightMap");
        private static int ID_HeightMapping = Shader.PropertyToID("_HeightMapping");
        private static int ID_SurfaceMapping = Shader.PropertyToID("_SurfaceMapping");

        internal void SetTCBuffer(int maxSize) {
            if (!(m_TCMaterial is null) && m_TCMaterial.Length != maxSize) {
                m_TCMaterial = new Material[maxSize];
            } else {
                m_TCMaterial = new Material[maxSize];
            }
        }

        public static void RenderTCInstance(RenderManager.CameraInfo cameraInfo, uint treeID, TreeInfo info, Vector3 position, float scale, float brightness, Vector4 objectIndex) {
            if (info.m_prefabInitialized) {
                if (cameraInfo is null || info.m_lodMesh1 is null || cameraInfo.CheckRenderDistance(position, info.m_lodRenderDistance)) {
                    TreeManager instance = Singleton<TreeManager>.instance;
                    MaterialPropertyBlock materialBlock = instance.m_materialBlock;
                    Matrix4x4 matrix = default;
                    matrix.SetTRS(position, TAPatcher.GetRandomQuaternion(position.sqrMagnitude), new Vector3(scale, scale, scale));
                    Color value = info.m_defaultColor * brightness;
                    value.a = Singleton<WeatherManager>.instance.GetWindSpeed(position);
                    Singleton<TerrainManager>.instance.GetHeightMapping(position, out Texture heightMap, out Vector4 heightMapping, out Vector4 surfaceMapping);
                    materialBlock.Clear();
                    materialBlock.SetColor(instance.ID_Color, value);
                    materialBlock.SetTexture(ID_HeightMap, heightMap);
                    materialBlock.SetVector(ID_HeightMapping, heightMapping);
                    materialBlock.SetVector(ID_SurfaceMapping, surfaceMapping);
                    materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                    instance.m_drawCallData.m_defaultCalls++;
                    info.m_material.shader = defTCShader;
                    Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
                } else {
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
            }
        }

        public void TerrainConformTrees() {
            if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) && UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;
                foreach (var selected in Action.selection) {
                    uint treeID = selected.id.Tree;
                    if (selected is MoveableTree && !selected.id.IsEmpty && treeID > 0) {
                        if ((trees[treeID].m_flags & TerrainConformFlag) == 0) {
                            Material origMat = trees[treeID].Info.m_material;
                            Material material = new Material(origMat) {
                                shader = defTCShader,
                                mainTexture = origMat.mainTexture,
                                mainTextureScale = origMat.mainTextureScale,
                                mainTextureOffset = origMat.mainTextureOffset,
                                shaderKeywords = origMat.shaderKeywords,
                                color = origMat.color,
                                doubleSidedGI = origMat.doubleSidedGI,
                                globalIlluminationFlags = origMat.globalIlluminationFlags,
                                renderQueue = origMat.renderQueue,
                                hideFlags = origMat.hideFlags,
                                name = origMat.name
                            };
                            if (!(material is null)) {
                                trees[treeID].m_flags |= TerrainConformFlag;
                                m_TCMaterial[treeID] = material;
                            }
                        }
                    }
                }
            }
        }

        public void UnConformTrees() {
            if ((MoveItTool.ToolState == MoveItTool.ToolStates.Default) && UIToolOptionPanel.instance.isVisible && Action.selection.Count > 0) {
                TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;
                foreach (var selected in Action.selection) {
                    uint treeID = selected.id.Tree;
                    if (selected is MoveableTree && !selected.id.IsEmpty && treeID > 0) {
                        if ((trees[treeID].m_flags & TerrainConformFlag) != 0) {
                            trees[treeID].m_flags &= TerrainConformMask;
                            Object.Destroy(m_TCMaterial[treeID]);
                        }
                    }
                }
            }
        }
    }
}
#endif