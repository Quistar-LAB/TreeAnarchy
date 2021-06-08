using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using ColossalFramework;
using UnityEngine;

namespace TreeAnarchy.Patches {
	static class TATreeManager {
		internal static void Enable(Harmony harmony) {
			harmony.Patch(AccessTools.Method(typeof(TreeManager), "EndRenderingImpl"),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(TATreeManager), nameof(TATreeManager.EndRenderingImplPrefix))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TATreeManager), nameof(TATreeManager.BeginRenderingImplTranspiler))));
		}
		static Stopwatch timer = new Stopwatch();
		static Stopwatch Endtimer = new Stopwatch();

		private unsafe static void BeginRedenderingLoopOpt(TreeManager instance) {
			unchecked {
				void refreshLod(ref RenderGroup.MeshData data, ref Mesh mesh) {
					if (data != null) {
						mesh ??= new Mesh();
						data.PopulateMesh(mesh);
						data = null;
					}
				}
				void setTexture(TreeInfo prefab) {
					if (prefab != null) {
						refreshLod(ref prefab.m_lodMeshData1, ref prefab.m_lodMesh1);
						refreshLod(ref prefab.m_lodMeshData4, ref prefab.m_lodMesh4);
						refreshLod(ref prefab.m_lodMeshData8, ref prefab.m_lodMesh8);
						refreshLod(ref prefab.m_lodMeshData16, ref prefab.m_lodMesh16);
						if (prefab.m_lodMaterial == null) {
							Shader shader = Singleton<RenderManager>.instance.m_properties.m_groupLayerShaders[instance.m_treeLayer];
							prefab.m_lodMaterial = new Material(shader);
							prefab.m_lodMaterial.EnableKeyword("MULTI_INSTANCE");
						}
						prefab.m_lodMaterial.mainTexture = instance.m_renderDiffuseTexture;
						prefab.m_lodMaterial.SetTexture(instance.ID_XYCAMap, instance.m_renderXycaTexture);
						prefab.m_lodMaterial.SetTexture(instance.ID_ShadowAMap, instance.m_renderShadowTexture);
					}
				}
				// unroll loop
				uint maxCount = (uint)PrefabCollection<TreeInfo>.PrefabCount();
				uint remainder = maxCount % 5;
				uint prefabCount = maxCount - remainder;
				for (uint i = 0; i < prefabCount; i++) {
					setTexture(PrefabCollection<TreeInfo>.GetPrefab(i++));
					setTexture(PrefabCollection<TreeInfo>.GetPrefab(i++));
					setTexture(PrefabCollection<TreeInfo>.GetPrefab(i++));
					setTexture(PrefabCollection<TreeInfo>.GetPrefab(i++));
                    setTexture(PrefabCollection<TreeInfo>.GetPrefab(i));
				}
				for(uint i = prefabCount; i < maxCount; i++) {
					setTexture(PrefabCollection<TreeInfo>.GetPrefab(i));
				}
			}
		}

		private static IEnumerable<CodeInstruction> BeginRenderingImplTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			int firstIndex = 0;
			int lastIndex = 0;
			Label lb = default;

			var codes = new List<CodeInstruction>(instructions);
			for(int i = 0; i < codes.Count; i++) {
				if(codes[i].opcode == OpCodes.Endfinally) {
					firstIndex = i + 1;
					lb = codes[i + 1].labels[0];
                }
				if(codes[i].opcode == OpCodes.Ret) {
					lastIndex = i;
                }
            }
			codes.RemoveRange(firstIndex, lastIndex - firstIndex);

			codes.Insert(firstIndex, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TATreeManager), nameof(TATreeManager.BeginRedenderingLoopOpt))));
			codes.Insert(firstIndex, new CodeInstruction(OpCodes.Ldarg_0).WithLabels(lb));

			return codes.AsEnumerable();
		}

		private unsafe static bool EndRenderingImplPrefix(TreeManager __instance, RenderManager.CameraInfo cameraInfo) {
			FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
			unchecked {
				int layerMask = 1 << __instance.m_treeLayer;
				uint[] treeGrid = __instance.m_treeGrid;
				fixed (TreeInstance* pBuffer = &__instance.m_trees.m_buffer[0]) {
					int maxSize = renderedGroups.m_size;
					for (int i = 0; i < maxSize; i++) {
						RenderGroup renderGroup = renderedGroups.m_buffer[i];
						if ((renderGroup.m_instanceMask & layerMask) != 0) {
							int minX = renderGroup.m_x * 12; /* 540 / 45; Avoid division */
							int minZ = renderGroup.m_z * 12; /* 540 / 45; Avoid division */
							int maxX = (renderGroup.m_x + 1) * 12 - 1; // 540 / 45 - 1; avoid division
							int maxZ = (renderGroup.m_z + 1) * 12 - 1; // 540 / 45 - 1; avoid division
							for (int j = minZ; j <= maxZ; j++) {
								for (int k = minX; k <= maxX; k++) {
									uint treeID = treeGrid[j * 540 + k];
									while (treeID != 0u) {
										(pBuffer + treeID)->RenderInstance(cameraInfo, treeID, renderGroup.m_instanceMask);
										treeID = (pBuffer + treeID)->m_nextGridTree;
										// removed bound check here
									}
								}
							}
						}
					}
				}
				// unroll this loop
				uint prefabCount = (uint)PrefabCollection<TreeInfo>.PrefabCount();
				uint remainer = prefabCount % 5;
				uint maxLen = prefabCount - remainer;
				void RenderLod(TreeInfo prefab) {
					if (prefab?.m_lodCount != 0) TreeInstance.RenderLod(cameraInfo, prefab);
                }
				for (uint i = 0; i < maxLen; i++) {
					RenderLod(PrefabCollection<TreeInfo>.GetPrefab(i++));
					RenderLod(PrefabCollection<TreeInfo>.GetPrefab(i++));
					RenderLod(PrefabCollection<TreeInfo>.GetPrefab(i++));
					RenderLod(PrefabCollection<TreeInfo>.GetPrefab(i++));
					RenderLod(PrefabCollection<TreeInfo>.GetPrefab(i));
				}
				for(uint i = maxLen; i < prefabCount; i++) { // process the remaining prefabs
					RenderLod(PrefabCollection<TreeInfo>.GetPrefab(i));
				}
				if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None) {
					int size = __instance.m_burningTrees.m_size;
					for (int m = 0; m < size; m++) {
						TreeManager.BurningTree burningTree = __instance.m_burningTrees.m_buffer[m];
						if (burningTree.m_treeIndex != 0) {
							float fireIntensity = (float)burningTree.m_fireIntensity * 0.003921569f;
							float fireDamage = (float)burningTree.m_fireDamage * 0.003921569f;
							__instance.RenderFireEffect(cameraInfo, burningTree.m_treeIndex, ref __instance.m_trees.m_buffer[burningTree.m_treeIndex], fireIntensity, fireDamage);
						}
					}
				}
			}
			return false; // replace the original method
		}
	}
}
