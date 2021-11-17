﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
		private static IEnumerable<CodeInstruction> InstallTreeGridBufferLocals(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			LocalBuilder treeGrid = il.DeclareLocal(typeof(uint[]));
			LocalBuilder treeBuf = il.DeclareLocal(typeof(TreeInstance[]));
			FieldInfo treeGridField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeGrid));
			FieldInfo treeArrayField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees));
			using (var codes = instructions.GetEnumerator()) {
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, treeGridField);
				yield return new CodeInstruction(OpCodes.Stloc_S, treeGrid);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, treeArrayField);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer)));
				yield return new CodeInstruction(OpCodes.Stloc_S, treeBuf);
				while (codes.MoveNext()) {
					var cur = codes.Current;
					if (cur.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == treeGridField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeGrid).WithLabels(cur.labels);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == treeArrayField && codes.MoveNext()) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeBuf).WithLabels(cur.labels);
						} else {
							yield return cur;
							yield return next;
						}
					} else if (cur.opcode == OpCodes.Conv_U) {
						// skip
					} else {
						yield return cur;
					}
				}
            }
		}

		private static IEnumerable<CodeInstruction> RemoveBoundaryCheck(IEnumerable<CodeInstruction> instructions) {
			using (var codes = instructions.GetEnumerator()) {
				while (codes.MoveNext()) {
					var cur = codes.Current;
					if ((cur.opcode == OpCodes.Ldloc_0 ||
						cur.opcode == OpCodes.Ldloc_1 ||
						cur.opcode == OpCodes.Ldloc_2 ||
						cur.opcode == OpCodes.Ldloc_3 ||
						cur.opcode == OpCodes.Ldloc_S) && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldc_I4_1 && codes.MoveNext()) {
							var next1 = codes.Current;
							if (next1.opcode == OpCodes.Add && codes.MoveNext()) {
								var next2 = codes.Current;
								if (next2.opcode == OpCodes.Dup && codes.MoveNext()) {
									var next3 = codes.Current;
									if ((next3.opcode == OpCodes.Stloc_0 ||
										next3.opcode == OpCodes.Stloc_1 ||
										next3.opcode == OpCodes.Stloc_2 ||
										next3.opcode == OpCodes.Stloc_3 ||
										next3.opcode == OpCodes.Stloc_S) && codes.MoveNext()) {
										var next4 = codes.Current;
										if (next4.LoadsConstant(262144)) {
											while (codes.MoveNext()) {
												var temp = codes.Current;
												if (temp.opcode == OpCodes.Br || temp.opcode == OpCodes.Br_S) break;
											}
										} else {
											yield return cur;
											yield return next;
											yield return next1;
											yield return next2;
											yield return next3;
											yield return next4;
										}
									} else {
										yield return cur;
										yield return next;
										yield return next1;
										yield return next2;
										yield return next3;
									}
								} else {
									yield return cur;
									yield return next;
									yield return next1;
									yield return next2;
								}
							} else {
								yield return cur;
								yield return next;
								yield return next1;
							}
						} else {
							yield return cur;
							yield return next;
						}
					} else {
						yield return cur;
					}
				}
			}
		}

		/// <summary>
		/// Major optimization to the routine, add local variables to treeGrid and treeBuffer, instead of using load field which is slow
		/// Also using new EMath optimized library
		/// </summary>
		private static IEnumerable<CodeInstruction> AfterTerrainUpdateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			int firstSigOccuranceCount = 0;
			int secondSigOccurancecount = 0;
			LocalBuilder treeGrid = il.DeclareLocal(typeof(uint[]));
			LocalBuilder treeBuffer = il.DeclareLocal(typeof(TreeInstance[]));
			MethodInfo mathMax = AccessTools.Method(typeof(EMath), nameof(EMath.Max), new Type[] { typeof(int), typeof(int) });
			MethodInfo mathMin = AccessTools.Method(typeof(EMath), nameof(EMath.Min), new Type[] { typeof(int), typeof(int) });
			FieldInfo treeGridField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeGrid));
			FieldInfo treeArrayField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees));
			using (var codes = ReplaceMath(instructions).GetEnumerator()) {
				while(codes.MoveNext()) {
					var cur = codes.Current;
					if (cur.LoadsConstant(270f) && codes.MoveNext()) {
						yield return cur;
						cur = codes.Current;
						if (cur.opcode == OpCodes.Add && codes.MoveNext()) {
							yield return cur;
							cur = codes.Current;
							if (cur.opcode == OpCodes.Conv_I4 && codes.MoveNext()) {
								yield return cur;
								cur = codes.Current;
								if (cur.opcode == OpCodes.Ldc_I4_0 && codes.MoveNext()) {
									yield return cur;
									cur = codes.Current;
									if (cur.opcode == OpCodes.Call && cur.operand == mathMax && codes.MoveNext()) {
										yield return cur;
										var next = codes.Current;
										if (++firstSigOccuranceCount == 2) {
											// skip the second stloc
										} else {
											yield return next;
										}
									}
								} else if (cur.LoadsConstant(539) && codes.MoveNext()) {
									yield return cur;
									cur = codes.Current;
									if (cur.opcode == OpCodes.Call && cur.operand == mathMin && codes.MoveNext()) {
										yield return cur;
										cur = codes.Current;
										if (++secondSigOccurancecount == 2 && codes.MoveNext()) {
											yield return cur;
											// skip the second ldloc and insert codes to initialize custom local variable
											yield return new CodeInstruction(OpCodes.Ldarg_0);
											yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeGrid)));
											yield return new CodeInstruction(OpCodes.Stloc_S, treeGrid);
											yield return new CodeInstruction(OpCodes.Ldarg_0);
											yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees)));
											yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer)));
											yield return new CodeInstruction(OpCodes.Stloc_S, treeBuffer);
										} else {
											yield return cur;
										}
									}
								}
							}
						}
					} else if (cur.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == treeGridField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeGrid).WithLabels(cur.labels);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == treeArrayField && codes.MoveNext()) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeBuffer).WithLabels(cur.labels);
						} else {
							yield return cur;
							yield return next;
						}
					} else if (cur.opcode == OpCodes.Conv_U) {
						// skip the code
					} else if ((cur.opcode == OpCodes.Stloc_S || cur.opcode == OpCodes.Ldloc_S) && (cur.operand as LocalBuilder).LocalIndex == 13) {
						// skip the code
					} else if (cur.opcode == OpCodes.Ldc_I4_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Stloc_S && (next.operand as LocalBuilder).LocalIndex == 11) {
							// skip codes
						} else {
							yield return cur;
							yield return next;
						}
					} else if (cur.opcode == OpCodes.Ldloc_S && (cur.operand as LocalBuilder).LocalIndex == 11 && codes.MoveNext()) {
						while(codes.MoveNext()) {
							if (codes.Current.opcode == OpCodes.Br) break;
                        }
					} else {
						yield return cur;
					}
				}
			}
		}

		private delegate T PrefabGetter<T>();
		private static PrefabGetter<FastList<PrefabCollection<TreeInfo>.PrefabData>> m_simulationPrefabs;

		private static PrefabGetter<T> CreateGetter<T>(FieldInfo field) {
			string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
			DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(T), new Type[1] { typeof(PrefabCollection<TreeInfo>) }, true);
			ILGenerator gen = setterMethod.GetILGenerator();
			if (field.IsStatic) {
				gen.Emit(OpCodes.Ldsfld, field);
			} else {
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(OpCodes.Ldfld, field);
			}
			gen.Emit(OpCodes.Ret);
			return (PrefabGetter<T>)setterMethod.CreateDelegate(typeof(PrefabGetter<T>));
		}
		public delegate void RenderFireEffectAPI(RenderManager.CameraInfo cameraInfo, uint treeID, ref TreeInstance data, float fireIntensity, float fireDamage);
		public static RenderFireEffectAPI delegatedRenderFireEffect;

		private static void CustomAwakeRoutine(TreeManager instance) {
			FieldInfo simPrefabs = typeof(PrefabCollection<TreeInfo>).GetField("m_simulationPrefabs", BindingFlags.NonPublic | BindingFlags.Static);
			m_simulationPrefabs = CreateGetter<FastList<PrefabCollection<TreeInfo>.PrefabData>>(simPrefabs);
			delegatedRenderFireEffect = instance.RenderFireEffect;
        }

		private static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions) {
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.CustomAwakeRoutine)));
			foreach (var code in instructions) {
				if (code.LoadsConstant(TAMod.DefaultTreeLimit)) {
					yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(TAMod.MaxTreeLimit)));
				} else if (code.LoadsConstant(TAMod.DefaultTreeUpdateCount)) {
					yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(TAMod.MaxTreeUpdateLimit)));
				} else yield return code;
			}
		}

		/// <summary>
		/// This custom routine is inserted and replaces the unoptimized codes in TreeManager::BeginRenderingImpl
		/// </summary>
		private unsafe static void BeginRenderingImplCoroutine(int treeLayer, int ID_XYCAMap, int ID_ShadowAMap, RenderTexture renderDiffuseTexture, RenderTexture renderXycaTexture, RenderTexture renderShadowTexture) {
			PrefabCollection<TreeInfo>.PrefabData[] prefabDatas = m_simulationPrefabs().m_buffer;
			int len = PrefabCollection<TreeInfo>.PrefabCount();
			for (int i = 0; i < len; i++) {
				TreeInfo prefab = prefabDatas[i].m_prefab;
				if (!(prefab is null)) {
					if (!(prefab.m_lodMeshData1 is null)) {
						RenderGroup.MeshData lodMeshData = prefab.m_lodMeshData1;
						prefab.m_lodMeshData1 = null;
						if (prefab.m_lodMesh1 is null) {
							prefab.m_lodMesh1 = new Mesh();
						}
						lodMeshData.PopulateMesh(prefab.m_lodMesh1);
					}
					if (!(prefab.m_lodMeshData4 is null)) {
						RenderGroup.MeshData lodMeshData = prefab.m_lodMeshData4;
						prefab.m_lodMeshData4 = null;
						if (prefab.m_lodMesh4 is null) {
							prefab.m_lodMesh4 = new Mesh();
						}
						lodMeshData.PopulateMesh(prefab.m_lodMesh4);
					}
					if (!(prefab.m_lodMeshData8 is null)) {
						RenderGroup.MeshData lodMeshData = prefab.m_lodMeshData8;
						prefab.m_lodMeshData8 = null;
						if (prefab.m_lodMesh8 is null) {
							prefab.m_lodMesh8 = new Mesh();
						}
						lodMeshData.PopulateMesh(prefab.m_lodMesh8);
					}
					if (!(prefab.m_lodMeshData16 is null)) {
						RenderGroup.MeshData lodMeshData = prefab.m_lodMeshData16;
						prefab.m_lodMeshData16 = null;
						if (prefab.m_lodMesh16 is null) {
							prefab.m_lodMesh16 = new Mesh();
						}
						lodMeshData.PopulateMesh(prefab.m_lodMesh16);
					}
					Material lodMaterial = prefab.m_lodMaterial;
					if (lodMaterial is null) {
						Shader shader = Singleton<RenderManager>.instance.m_properties.m_groupLayerShaders[treeLayer];
						lodMaterial = new Material(shader);
						lodMaterial.EnableKeyword("MULTI_INSTANCE");
					}
					lodMaterial.mainTexture = renderDiffuseTexture;
					lodMaterial.SetTexture(ID_XYCAMap, renderXycaTexture);
					lodMaterial.SetTexture(ID_ShadowAMap, renderShadowTexture);
					prefab.m_lodMaterial = lodMaterial;
				}
			}
		}

		private static IEnumerable<CodeInstruction> BeginRenderingImplTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			bool firstRenderCameraOccurance = false;
			bool firstgroupLayerOccurance = false;
			LocalBuilder treeLayer = il.DeclareLocal(typeof(int));
			LocalBuilder XYCAMap = il.DeclareLocal(typeof(int));
			LocalBuilder ShadowAMap = il.DeclareLocal(typeof(int));
			LocalBuilder renderShadowTexture = il.DeclareLocal(typeof(RenderTexture));
			LocalBuilder renderDiffuseTexture = il.DeclareLocal(typeof(RenderTexture));
			LocalBuilder renderXycaTexture = il.DeclareLocal(typeof(RenderTexture));
			MethodInfo prefabCount = AccessTools.Method(typeof(PrefabCollection<TreeInfo>), nameof(PrefabCollection<TreeInfo>.PrefabCount));
			FieldInfo cameraInfo = AccessTools.Field(typeof(TreeManager), "m_cameraInfo");
			FieldInfo treeLayerField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeLayer));
			FieldInfo XYCAMapField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_XYCAMap));
			FieldInfo ShadowAMapField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_ShadowAMap));
			FieldInfo renderShadowTextureField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_renderShadowTexture));
			FieldInfo renderDiffuseTextureField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_renderDiffuseTexture));
			FieldInfo renderXycaTextureField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_renderXycaTexture));
			MethodInfo renderManager = AccessTools.PropertyGetter(typeof(Singleton<RenderManager>), nameof(Singleton<RenderManager>.instance));
			FieldInfo groupLayerMaterials = AccessTools.Field(typeof(RenderManager), nameof(RenderManager.m_groupLayerMaterials));
			using (var codes = instructions.GetEnumerator()) {
				while(codes.MoveNext()) {
					var cur = codes.Current;
					if (!firstRenderCameraOccurance && cur.opcode == OpCodes.Stfld && cur.operand == cameraInfo) {
						firstRenderCameraOccurance = true;
						yield return cur;
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeLayer)));
						yield return new CodeInstruction(OpCodes.Stloc_S, treeLayer);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_XYCAMap)));
						yield return new CodeInstruction(OpCodes.Stloc_S, XYCAMap);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_ShadowAMap)));
						yield return new CodeInstruction(OpCodes.Stloc_S, ShadowAMap);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_renderDiffuseTexture)));
						yield return new CodeInstruction(OpCodes.Stloc_S, renderDiffuseTexture);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_renderShadowTexture)));
						yield return new CodeInstruction(OpCodes.Stloc_S, renderShadowTexture);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_renderXycaTexture)));
						yield return new CodeInstruction(OpCodes.Stloc_S, renderXycaTexture);
					} else if (cur.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == renderDiffuseTextureField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, renderDiffuseTexture);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == renderShadowTextureField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, renderShadowTexture);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == renderXycaTextureField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, renderXycaTexture);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == treeLayerField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeLayer);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == XYCAMapField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, XYCAMap);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == ShadowAMapField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, ShadowAMap);
						} else {
							yield return cur;
							yield return next;
						}
					} else if (cur.opcode == OpCodes.Call && cur.operand == renderManager && codes.MoveNext()) {
						var next = codes.Current;
						if(next.opcode == OpCodes.Ldfld && next.operand == groupLayerMaterials) {
							if(!firstgroupLayerOccurance) {
								firstgroupLayerOccurance = true;
								yield return cur;
								yield return next;
								yield return new CodeInstruction(OpCodes.Dup);
                            } else {
								yield return new CodeInstruction(OpCodes.Dup);
							}
						} else {
							yield return cur;
							yield return next;
                        }
					} else if (cur.opcode == OpCodes.Call && cur.operand == prefabCount) {
						yield return new CodeInstruction(OpCodes.Ldloc_S, treeLayer).WithLabels(cur.labels);
						yield return new CodeInstruction(OpCodes.Ldloc_S, XYCAMap);
						yield return new CodeInstruction(OpCodes.Ldloc_S, ShadowAMap);
						yield return new CodeInstruction(OpCodes.Ldloc_S, renderDiffuseTexture);
						yield return new CodeInstruction(OpCodes.Ldloc_S, renderXycaTexture);
						yield return new CodeInstruction(OpCodes.Ldloc_S, renderShadowTexture);
						yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.BeginRenderingImplCoroutine)));
						yield return new CodeInstruction(OpCodes.Ret);
						break;
					} else {
						yield return cur;
					}
				}
			}
        }

		private static IEnumerable<CodeInstruction> CalculateAreaHeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			LocalBuilder treeScales = il.DeclareLocal(typeof(float[]));
			using (var codes = RemoveBoundaryCheck(InstallTreeGridBufferLocals(ReplaceMath(instructions), il)).GetEnumerator()) {
				yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAManager), nameof(TAManager.m_defScales)));
				yield return new CodeInstruction(OpCodes.Stloc_S, treeScales);
				while (codes.MoveNext()) {
					var cur = codes.Current;
					if (cur.opcode == OpCodes.Ldloca_S && (cur.operand as LocalBuilder).LocalType == typeof(Randomizer)) {
						int varIndex = 0;
						List<Label> labels = cur.labels;
						while (codes.MoveNext()) {
							cur = codes.Current;
							if (cur.opcode == OpCodes.Stloc_S && (cur.operand as LocalBuilder).LocalType == typeof(float)) {
								varIndex = (cur.operand as LocalBuilder).LocalIndex;
								break;
							}
						}
						codes.MoveNext();
						yield return codes.Current.WithLabels(labels);
						while (codes.MoveNext()) {
							cur = codes.Current;
							if (cur.opcode == OpCodes.Ldloc_S && (cur.operand as LocalBuilder).LocalIndex == varIndex) {
								yield return new CodeInstruction(OpCodes.Ldloc_S, treeScales);
								yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
								yield return new CodeInstruction(OpCodes.Ldelem_R4);
								break;
							} else {
								yield return cur;
							}
						}
					} else {
						yield return cur;
					}
				}
			}
		}

		public unsafe static void EndRenderingImplCoroutine(RenderManager.CameraInfo cameraInfo, TreeInstance[] buffer, FastList<TreeManager.BurningTree> burningTrees,
															MaterialPropertyBlock materialBlock, int ID_TreeLocation, int ID_TreeColor, int ID_TreeObjectIndex, ref DrawCallData drawCallData) {
			PrefabCollection<TreeInfo>.PrefabData[] prefabDatas = m_simulationPrefabs().m_buffer;
			int len = PrefabCollection<TreeInfo>.PrefabCount();
			Color black = Color.black;
			Vector4 vecZero = EMath.Vector4Zero;
			Vector3 vec100 = new Vector3(100f, 100f, 100f);
			Vector3 vec100000 = new Vector3(100000f, 100000f, 100000f);
			Vector3 negvec100000 = new Vector3(-100000f, -100000f, -100000f);
			Vector3 camForward = cameraInfo.m_forward;
			Matrix4x4 identity = Matrix4x4.identity;
			materialBlock.Clear();
			Bounds bounds = default;
			for (int i = 0; i < len; i++) {
				if (prefabDatas[i].m_prefab is TreeInfo prefab && prefab.m_lodCount != 0) {
					Mesh mesh;
					int num;
					int lodCount = prefab.m_lodCount;
					Vector4[] lodLocations = prefab.m_lodLocations;
					Vector4[] lodColors = prefab.m_lodColors;
					Vector4[] lodObjectIndices = prefab.m_lodObjectIndices;
					if (lodCount <= 1) {
						mesh = prefab.m_lodMesh1;
						num = 1;
					} else if (lodCount <= 4) {
						mesh = prefab.m_lodMesh4;
						num = 4;
					} else if (lodCount <= 8) {
						mesh = prefab.m_lodMesh8;
						num = 8;
					} else {
						mesh = prefab.m_lodMesh16;
						num = 16;
					}
					for (int j = lodCount; j < num; j++) {
						lodLocations[j] = camForward * -100000f;
						lodColors[j] = black;
						lodObjectIndices[j] = vecZero;
					}
					materialBlock.SetVectorArray(ID_TreeLocation, lodLocations);
					materialBlock.SetVectorArray(ID_TreeColor, lodColors);
					materialBlock.SetVectorArray(ID_TreeObjectIndex, lodObjectIndices);
					bounds.SetMinMax(prefab.m_lodMin - vec100, prefab.m_lodMax + vec100);
					mesh.bounds = bounds;
					prefab.m_lodMin = vec100000;
					prefab.m_lodMax = negvec100000;
					drawCallData.m_lodCalls++;
					drawCallData.m_batchedCalls += (lodCount - 1);
					Graphics.DrawMesh(mesh, identity, prefab.m_lodMaterial, prefab.m_prefabDataLayer, null, 0, materialBlock);
					prefab.m_lodCount = 0;
				}
			}
			if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None) {
				len = burningTrees.m_size;
				TreeManager.BurningTree[] burningBuf = burningTrees.m_buffer;
				for (int m = 0; m < len; m++) {
					TreeManager.BurningTree burningTree = burningBuf[m];
					uint treeID = burningTree.m_treeIndex;
					if (treeID != 0u) {
						float fireIntensity = burningTree.m_fireIntensity * 0.003921569f;
						float fireDamage = burningTree.m_fireDamage * 0.003921569f;
						delegatedRenderFireEffect(cameraInfo, treeID, ref buffer[treeID], fireIntensity, fireDamage);
					}
				}
			}
		}

		/// <summary>
		/// Optimized version that replaces TreeInstance::RenderInstance. This is called directly from TreeManager::EndRenderingImpl
		/// </summary>
		public static void RenderInstanceEnchanced(RenderManager.CameraInfo cameraInfo, uint treeID, ref TreeInstance tree,
											   MaterialPropertyBlock materialBlock, int ID_Color, int ID_ObjectIndex, int ID_TreeLocation, int ID_TreeColor, int ID_TreeObjectIndex, ref DrawCallData drawCallData) {
			TreeInfo info = tree.Info;
			Vector3 position = tree.Position;
			if (cameraInfo.Intersect(position, info.m_generatedInfo.m_size.y * info.m_maxScale)) {
				float scale = TAManager.CalcTreeScale(treeID);
				float brightness = TAManager.m_brightness[treeID];
				Vector4 defaultColorLocation = RenderManager.DefaultColorLocation;
				defaultColorLocation.z = -1f;
				if (info.m_prefabInitialized) {
					if (cameraInfo is null || info.m_lodMesh1 is null || cameraInfo.CheckRenderDistance(position, info.m_lodRenderDistance)) {
						Color value = info.m_defaultColor * brightness;
						value.a = TAManager.GetWindSpeed(position);
						materialBlock.Clear();
						materialBlock.SetColor(ID_Color, value);
						materialBlock.SetVector(ID_ObjectIndex, defaultColorLocation);
						drawCallData.m_defaultCalls++;
						Graphics.DrawMesh(info.m_mesh,
							Matrix4x4.TRS(position, TAManager.m_treeQuaternions[(int)(position.x * position.x + position.z * position.z) % 359],
							new Vector3(scale, scale, scale)), info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
					} else {
						position.y += info.m_generatedInfo.m_center.y * (scale - 1f);
						Color color = info.m_defaultColor * brightness;
						color.a = TAManager.GetWindSpeed(position);
						info.m_lodLocations[info.m_lodCount] = new Vector4(position.x, position.y, position.z, scale);
						info.m_lodColors[info.m_lodCount] = color.linear;
						info.m_lodObjectIndices[info.m_lodCount] = defaultColorLocation;
						info.m_lodMin = EMath.Min(info.m_lodMin, position);
						info.m_lodMax = EMath.Max(info.m_lodMax, position);
						if (++info.m_lodCount == info.m_lodLocations.Length) {
							//TreeInstance.RenderLod(cameraInfo, info);
							Mesh mesh;
							int num;
							int lodCount = info.m_lodCount;
							Vector4[] lodLocations = info.m_lodLocations;
							Vector4[] lodColors = info.m_lodColors;
							Vector4[] lodObjectIndices = info.m_lodObjectIndices;
							if (lodCount <= 1) {
								mesh = info.m_lodMesh1;
								num = 1;
							} else if (lodCount <= 4) {
								mesh = info.m_lodMesh4;
								num = 4;
							} else if (lodCount <= 8) {
								mesh = info.m_lodMesh8;
								num = 8;
							} else {
								mesh = info.m_lodMesh16;
								num = 16;
							}
							for (int j = lodCount; j < num; j++) {
								lodLocations[j] = cameraInfo.m_forward * -100000f;
								lodColors[j] = EMath.ColorBlack;
								lodObjectIndices[j] = default;
							}
							materialBlock.SetVectorArray(ID_TreeLocation, lodLocations);
							materialBlock.SetVectorArray(ID_TreeColor, lodColors);
							materialBlock.SetVectorArray(ID_TreeObjectIndex, lodObjectIndices);
							Bounds bounds = default;
							bounds.SetMinMax(info.m_lodMin - EMath.DefaultLod100, info.m_lodMax + EMath.DefaultLod100);
							mesh.bounds = bounds;
							info.m_lodMin = EMath.DefaultLodMin;
							info.m_lodMax = EMath.DefaultLodMax;
							drawCallData.m_lodCalls++;
							drawCallData.m_batchedCalls += (lodCount - 1);
							Graphics.DrawMesh(mesh, EMath.MatrixIdentity, info.m_lodMaterial, info.m_prefabDataLayer, null, 0, materialBlock);
							info.m_lodCount = 0;

						}
					}
				}
			}
		}

		private static IEnumerable<CodeInstruction> EndRenderingImplTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			int treeBufIndex = -1;
			LocalBuilder treeLayer = il.DeclareLocal(typeof(int));
			LocalBuilder renderGroupLen = il.DeclareLocal(typeof(int));
			LocalBuilder renderGroups = il.DeclareLocal(typeof(RenderGroup[]));
			LocalBuilder materialBlock = il.DeclareLocal(typeof(MaterialPropertyBlock));
			LocalBuilder idColor = il.DeclareLocal(typeof(int));
			LocalBuilder idObjectIndex = il.DeclareLocal(typeof(int));
			LocalBuilder idTreeColor = il.DeclareLocal(typeof(int));
			LocalBuilder idTreeLocation = il.DeclareLocal(typeof(int));
			LocalBuilder idTreeObjectIndex = il.DeclareLocal(typeof(int));
			LocalBuilder drawCallData = il.DeclareLocal(typeof(DrawCallData).MakeByRefType());
			FieldInfo treeLayerField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeLayer));
			FieldInfo renderGroupsField = AccessTools.Field(typeof(FastList<RenderGroup>), nameof(FastList<RenderGroup>.m_buffer));
			FieldInfo renderGroupsSizeField = AccessTools.Field(typeof(FastList<RenderGroup>), nameof(FastList<RenderGroup>.m_size));
			FieldInfo instanceMask = AccessTools.Field(typeof(RenderGroup), nameof(RenderGroup.m_instanceMask));
			MethodInfo renderInstance = AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance), new Type[] { typeof(RenderManager.CameraInfo), typeof(uint), typeof(int) });
			MethodInfo prefabCount = AccessTools.Method(typeof(PrefabCollection<TreeInfo>), nameof(PrefabCollection<TreeInfo>.PrefabCount));
			using (var codes = InstallTreeGridBufferLocals(RemoveBoundaryCheck(instructions), il).GetEnumerator()) {
				while(codes.MoveNext()) {
					var cur = codes.Current;
					if(cur.opcode == OpCodes.Ldloc_S && cur.operand is LocalBuilder local && local.LocalType == typeof(TreeInstance[])) {
						treeBufIndex = local.LocalIndex;
					}
					if (cur.opcode == OpCodes.Stloc_0) {
						yield return cur;
						yield return new CodeInstruction(OpCodes.Ldloc_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FastList<RenderGroup>), nameof(FastList<RenderGroup>.m_size)));
						yield return new CodeInstruction(OpCodes.Stloc_S, renderGroupLen);
						yield return new CodeInstruction(OpCodes.Ldloc_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FastList<RenderGroup>), nameof(FastList<RenderGroup>.m_buffer)));
						yield return new CodeInstruction(OpCodes.Stloc_S, renderGroups);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeLayer)));
						yield return new CodeInstruction(OpCodes.Stloc_S, treeLayer);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_materialBlock)));
						yield return new CodeInstruction(OpCodes.Stloc_S, materialBlock);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_Color)));
						yield return new CodeInstruction(OpCodes.Stloc_S, idColor);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_ObjectIndex)));
						yield return new CodeInstruction(OpCodes.Stloc_S, idObjectIndex);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_TreeLocation)));
						yield return new CodeInstruction(OpCodes.Stloc_S, idTreeLocation);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_TreeColor)));
						yield return new CodeInstruction(OpCodes.Stloc_S, idTreeColor);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_TreeObjectIndex)));
						yield return new CodeInstruction(OpCodes.Stloc_S, idTreeObjectIndex);
						yield return new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_drawCallData)));
						yield return new CodeInstruction(OpCodes.Stloc_S, drawCallData);
					} else if (cur.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == treeLayerField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeLayer).WithLabels(cur.labels);
						} else {
							yield return cur;
							yield return next;
						}
					} else if (cur.opcode == OpCodes.Ldloc_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == renderGroupsField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, renderGroups).WithLabels(cur.labels);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == renderGroupsSizeField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, renderGroupLen).WithLabels(cur.labels);
						} else {
							yield return cur;
							yield return next;
						}
					} else if (treeBufIndex > 0 && cur.opcode == OpCodes.Ldloc_S && cur.operand is LocalBuilder l2 && l2.LocalIndex == treeBufIndex && codes.MoveNext()) {
						var next = codes.Current;
						if(next.opcode == OpCodes.Ldloc_S && codes.MoveNext()) {
							var next1 = codes.Current;
							if(next1.opcode == OpCodes.Ldelema && codes.MoveNext()) {
								var next2 = codes.Current;
								if(next2.opcode == OpCodes.Ldarg_1 && codes.MoveNext()) {
									var next3 = codes.Current;
									if(next3.opcode == OpCodes.Ldloc_S && codes.MoveNext()) {
										var next4 = codes.Current;
										if(next4.opcode == OpCodes.Ldloc_2 && codes.MoveNext()) {
											var next5 = codes.Current;
											if(next5.opcode == OpCodes.Ldfld && next5.operand == instanceMask && codes.MoveNext()) {
												yield return new CodeInstruction(OpCodes.Ldarg_1).WithLabels(cur.labels);
												yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
												yield return new CodeInstruction(OpCodes.Ldloc_S, treeBufIndex);
												yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
												yield return new CodeInstruction(OpCodes.Ldelema, typeof(TreeInstance));
												yield return new CodeInstruction(OpCodes.Ldloc_S, materialBlock);
												yield return new CodeInstruction(OpCodes.Ldloc_S, idColor);
												yield return new CodeInstruction(OpCodes.Ldloc_S, idObjectIndex);
												yield return new CodeInstruction(OpCodes.Ldloc_S, idTreeLocation);
												yield return new CodeInstruction(OpCodes.Ldloc_S, idTreeColor);
												yield return new CodeInstruction(OpCodes.Ldloc_S, idTreeObjectIndex);
												yield return new CodeInstruction(OpCodes.Ldloca_S, drawCallData);
												yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.RenderInstanceEnchanced)));
											} else {
												yield return cur;
												yield return next;
												yield return next1;
												yield return next2;
												yield return next3;
												yield return next4;
												yield return next5;
											}
										} else {
											yield return cur;
											yield return next;
											yield return next1;
											yield return next2;
											yield return next3;
											yield return next4;
										}
									} else {
										yield return cur;
										yield return next;
										yield return next1;
										yield return next2;
										yield return next3;
									}
								} else {
									yield return cur;
									yield return next;
									yield return next1;
									yield return next2;
                                }
                            } else {
								yield return cur;
								yield return next;
								yield return next1;
                            }
                        } else {
							yield return cur;
							yield return next;
                        }
					} else if (treeBufIndex > 0 && cur.opcode == OpCodes.Call && cur.operand == prefabCount) {
						yield return new CodeInstruction(OpCodes.Ldarg_1);
						yield return new CodeInstruction(OpCodes.Ldloc_S, treeBufIndex);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_burningTrees)));
						yield return new CodeInstruction(OpCodes.Ldloc_S, materialBlock);
						yield return new CodeInstruction(OpCodes.Ldloc_S, idTreeLocation);
						yield return new CodeInstruction(OpCodes.Ldloc_S, idTreeColor);
						yield return new CodeInstruction(OpCodes.Ldloc_S, idTreeObjectIndex);
						yield return new CodeInstruction(OpCodes.Ldloca_S, drawCallData);
						yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.EndRenderingImplCoroutine)));
						yield return instructions.Last();
						break;
					} else {
						yield return cur;
					}
                }
            }
        }

		private static IEnumerable<CodeInstruction> FinalizeTreeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			int treeGridOccuranceCount = 0;
			LocalBuilder treeGrid = il.DeclareLocal(typeof(uint[]));
			LocalBuilder treeBuffer = il.DeclareLocal(typeof(TreeInstance[]));
			FieldInfo treeGridField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeGrid));
			FieldInfo treeArrayField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees));
			using(var codes = ReplaceMath(instructions).GetEnumerator()) {
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, treeGridField);
				yield return new CodeInstruction(OpCodes.Stloc_S, treeGrid);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, treeArrayField);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer)));
				yield return new CodeInstruction(OpCodes.Stloc_S, treeBuffer);
				while (codes.MoveNext()) {
					var cur = codes.Current;
					if (cur.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == treeGridField) {
							switch (++treeGridOccuranceCount) {
							case 2:
							case 3:
								yield return new CodeInstruction(OpCodes.Ldloc_S, treeGrid).WithLabels(cur.labels);
								break;
							default:
								yield return cur;
								yield return next;
								break;
							}
						} else if (next.opcode == OpCodes.Ldfld && next.operand == treeArrayField && codes.MoveNext()) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeBuffer).WithLabels(cur.labels);
						} else {
							yield return cur;
							yield return next;
						}
					} else if (cur.opcode == OpCodes.Conv_U) {
						// skip
					} else if (cur.opcode == OpCodes.Ldloc_S && (cur.operand as LocalBuilder).LocalIndex == 5) {
						while(codes.MoveNext()) {
							if (codes.Current.opcode == OpCodes.Leave) break;
                        }
						yield return codes.Current;
					} else {
						yield return cur;
					}
                }
            }
		}

		private static IEnumerable<CodeInstruction> HandleFireSpreadTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			bool buildingValid = false;
			int buildingVarIndex = 0;
			LocalBuilder randomizer = il.DeclareLocal(typeof(Randomizer));
			LocalBuilder buildingGrid = il.DeclareLocal(typeof(ushort[]));
			LocalBuilder buildingBuf = il.DeclareLocal(typeof(Building[]));
			FieldInfo buildingGridField = AccessTools.Field(typeof(BuildingManager), nameof(BuildingManager.m_buildingGrid));
			FieldInfo buildingArrayField = AccessTools.Field(typeof(BuildingManager), nameof(BuildingManager.m_buildings));
			MethodInfo simManager = AccessTools.PropertyGetter(typeof(Singleton<SimulationManager>), nameof(Singleton<SimulationManager>.instance));
			using(var codes = InstallTreeGridBufferLocals(ReplaceMath(instructions), il).GetEnumerator()) {
				yield return new CodeInstruction(OpCodes.Call, simManager);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SimulationManager), nameof(SimulationManager.m_randomizer)));
				yield return new CodeInstruction(OpCodes.Stloc_S, randomizer);
				while(codes.MoveNext()) {
					var cur = codes.Current;
					if (cur.opcode == OpCodes.Stloc_S && (cur.operand as LocalBuilder).LocalIndex == 18 && codes.MoveNext()) {
						var next = codes.Current;
						if(next.opcode == OpCodes.Ldloc_S && (next.operand as LocalBuilder).LocalIndex == 18 && codes.MoveNext()) {
							var next1 = codes.Current;
							codes.MoveNext();
							yield return next1;
							yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Subtraction"));
							yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3), nameof(Vector3.SqrMagnitude)));
							yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EMath), nameof(EMath.Sqrt)));
						} else {
							yield return cur;
							yield return next;
                        }
					} else if (cur.opcode == OpCodes.Stloc_S && (cur.operand as LocalBuilder).LocalType == typeof(BuildingManager)) {
						buildingValid = true;
						buildingVarIndex = (cur.operand as LocalBuilder).LocalIndex;
						yield return cur;
						yield return new CodeInstruction(OpCodes.Ldloc_S, cur.operand);
						yield return new CodeInstruction(OpCodes.Ldfld, buildingGridField);
						yield return new CodeInstruction(OpCodes.Stloc_S, buildingGrid);
						yield return new CodeInstruction(OpCodes.Ldloc_S, cur.operand);
						yield return new CodeInstruction(OpCodes.Ldfld, buildingArrayField);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array16<Building>), nameof(Array16<Building>.m_buffer)));
						yield return new CodeInstruction(OpCodes.Stloc_S, buildingBuf);
					} else if (cur.opcode == OpCodes.Call && cur.operand == simManager && codes.MoveNext()) {
						yield return new CodeInstruction(OpCodes.Ldloca_S, randomizer);
					} else if (cur.opcode == OpCodes.Ldloc_S && (cur.operand as LocalBuilder).LocalIndex == 17) {
						while (codes.MoveNext()) {
							if (codes.Current.opcode == OpCodes.Br) break;
						}
					} else if (buildingValid && cur.opcode == OpCodes.Ldloc_S && (cur.operand as LocalBuilder).LocalIndex == buildingVarIndex && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == buildingGridField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, buildingGrid).WithLabels(cur.labels);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == buildingArrayField && codes.MoveNext()) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, buildingBuf).WithLabels(cur.labels);
						} else {
							yield return cur;
							yield return next;
						}
					} else {
						yield return cur;
					}
                }
            }
        }

		private static IEnumerable<CodeInstruction> CalculateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) => RemoveBoundaryCheck(InstallTreeGridBufferLocals(instructions, il));

		private static IEnumerable<CodeInstruction> CheckLimitsTranspiler(IEnumerable<CodeInstruction> instructions) {
			const int MAX_MAPEDITOR_TREES = 250000;
			const int MAX_MAP_TREES_CEILING = TAMod.DefaultTreeLimit - 5;
			foreach (var instruction in instructions) {
				if (instruction.Is(OpCodes.Ldc_I4, MAX_MAPEDITOR_TREES))
					yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(TAMod.CheckLowLimit)));
				else if (instruction.Is(OpCodes.Ldc_I4, MAX_MAP_TREES_CEILING))
					yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(TAMod.CheckHighLimit)));
				else
					yield return instruction;
			}
		}

		/// <summary>
		/// Helper function to initialize the treescale and brightness buffer
		/// </summary>
		/// <param name="treeID"></param>
		/// <param name="info"></param>
		private static void SetTreeScaleBrightness(int treeID, TreeInfo info) {
			EMath.SetRandomizerSeed(treeID);
			TAManager.m_defScales[treeID] = info.m_minScale + EMath.randomizer.Int32(10000u) * (info.m_maxScale - info.m_minScale) * 0.0001f;
			TAManager.m_brightness[treeID] = info.m_minBrightness + EMath.randomizer.Int32(10000u) * (info.m_maxBrightness - info.m_minBrightness) * 0.0001f;
		}

		private static IEnumerable<CodeInstruction> CreateTreeTranspiler(IEnumerable<CodeInstruction> instructions) {
			bool firstSigFound = false;
			bool installedNew = false;
			MethodInfo createItem = AccessTools.Method(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.CreateItem), new Type[] { typeof(uint).MakeByRefType(), typeof(Randomizer).MakeByRefType() });
			foreach(var code in instructions) {
				if (!firstSigFound && code.opcode == OpCodes.Callvirt && code.operand == createItem) {
					firstSigFound = true;
					yield return code;
				} else if (firstSigFound && !installedNew && code.opcode == OpCodes.Stind_I4) {
					installedNew = true;
					yield return code;
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldarg_3);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.SetTreeScaleBrightness)));
				} else {
					yield return code;
				}
            }
		}

		private static IEnumerable<CodeInstruction> InitializeTreeTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceMath(instructions);

		private unsafe static void OnPostRenderCoroutine(RenderManager.CameraInfo cameraInfo, ref Quaternion quaternion, int renderPass, int prefabCount, int sqrLen,
			float tangent, Shader renderShader, ref DrawCallData drawCallData, int ID_TileRect, int ID_ShadowAMap, int ID_ShadowMatrix, RenderTexture renderShadowTexture) {
			PrefabCollection<TreeInfo>.PrefabData[] prefabDatas = m_simulationPrefabs().m_buffer;
			Vector3 vOne = EMath.Vector3One;
			Matrix4x4 matrix;
			float halfSqrLen = 0.5f - sqrLen * 0.5f;
			float divSqrLen = 0.5f / sqrLen;
			float divTangent = 0.5f / tangent;
			for (int i = 0; i < prefabCount; i++) {
				if (prefabDatas[i].m_prefab is TreeInfo prefab) {
					if (prefab.m_renderMaterial is null) {
						prefab.m_renderMaterial = new Material(renderShader);
						prefab.m_renderMaterial.CopyPropertiesFromMaterial(prefab.m_material);
					}
					Vector3 vector = new Vector3((halfSqrLen + (i % sqrLen)) / sqrLen, (halfSqrLen + (i / sqrLen)) / sqrLen, divTangent);
					matrix = Matrix4x4.TRS(vector - quaternion * new Vector3(0f, prefab.m_renderOffset / (prefab.m_renderScale * sqrLen), 0f), quaternion, vOne / (prefab.m_renderScale * sqrLen));
					if (renderPass == 0) {
						Quaternion quaternion2 = Quaternion.Inverse(cameraInfo.m_shadowRotation);
						prefab.m_renderMaterial.SetTexture(ID_ShadowAMap, renderShadowTexture);
						prefab.m_renderMaterial.SetMatrix(ID_ShadowMatrix, Matrix4x4.TRS(new Vector3((0.5f + (i % sqrLen)) / sqrLen, (0.5f + (i / sqrLen)) / sqrLen, divTangent) -
							(quaternion2 * new Vector3(0f, prefab.m_renderOffset / (prefab.m_renderScale * sqrLen), 0f)), quaternion2, vOne / (prefab.m_renderScale * sqrLen)));
					}
					prefab.m_renderMaterial.SetVector(ID_TileRect, new Vector4(vector.x - divSqrLen, vector.y - divSqrLen, vector.x + divSqrLen, vector.y + divSqrLen));
					if (prefab.m_renderMaterial.SetPass(renderPass)) {
						drawCallData.m_defaultCalls++;
						Graphics.DrawMeshNow(prefab.m_mesh, matrix);
					}
					if ((renderPass == 1 || renderPass == 3) && prefab.m_renderMaterial.SetPass(2)) {
						Graphics.DrawMeshNow(prefab.m_mesh, matrix);
					}
				}
			}
		}

		private static IEnumerable<CodeInstruction> OnPostRenderTranspiler(IEnumerable<CodeInstruction> instructions) {
			bool sigFound = false;
			foreach(var code in instructions) {
				if (!sigFound && code.opcode == OpCodes.Stloc_3) {
					sigFound = true;
					yield return code;
				} else if (sigFound && code.opcode == OpCodes.Ldc_I4_0) {
					yield return new CodeInstruction(OpCodes.Ldarg_0).WithLabels(code.labels);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), "m_cameraInfo"));
					yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), "m_renderPass"));
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldloc_1);
					yield return new CodeInstruction(OpCodes.Ldloc_3);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_properties)));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeProperties), nameof(TreeProperties.m_renderShader)));
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_drawCallData)));
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_TileRect)));
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_ShadowAMap)));
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.ID_ShadowMatrix)));
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_renderShadowTexture)));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.OnPostRenderCoroutine)));
					yield return instructions.Last();
					break;
				} else {
					yield return code;
				}
            }
        }

		private static IEnumerable<CodeInstruction> TreeManagerOverlapQuadTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) => 
			 RemoveBoundaryCheck(InstallTreeGridBufferLocals(ReplaceMath(instructions), il));

		private static IEnumerable<CodeInstruction> TreeManagerPopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
			RemoveBoundaryCheck(InstallTreeGridBufferLocals(instructions, il));

		private static IEnumerable<CodeInstruction> TreeManagerRayCastTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
			RemoveBoundaryCheck(InstallTreeGridBufferLocals(ReplaceMath(instructions), il));

		private static IEnumerable<CodeInstruction> SampleSmoothHeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
			RemoveBoundaryCheck(InstallTreeGridBufferLocals(ReplaceMath(instructions), il));

		/// <summary>
		/// Did not take care of optimizing burning tree loop
		/// </summary>
		private static IEnumerable<CodeInstruction> SimulationStepImplTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			LocalBuilder treeBuf = il.DeclareLocal(typeof(TreeInstance[]));
			LocalBuilder updatedTrees = il.DeclareLocal(typeof(ulong[]));
			
			FieldInfo treeArrayField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees));
			FieldInfo updatedTreesField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_updatedTrees));
			using (var codes = instructions.GetEnumerator()) {
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, treeArrayField);
				yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer)));
				yield return new CodeInstruction(OpCodes.Stloc_S, treeBuf);
				yield return new CodeInstruction(OpCodes.Ldarg_0);
				yield return new CodeInstruction(OpCodes.Ldfld, updatedTreesField);
				yield return new CodeInstruction(OpCodes.Stloc_S, updatedTrees);
				while (codes.MoveNext()) {
					var cur = codes.Current;
					if (cur.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == treeArrayField && codes.MoveNext()) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeBuf).WithLabels(cur.labels);
						} else if (next.opcode == OpCodes.Ldfld && next.operand == updatedTreesField) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, updatedTrees).WithLabels(cur.labels);
						} else {
							yield return cur;
							yield return next;
						}
					} else if (cur.opcode == OpCodes.Conv_U) {
						// skip
					} else {
						yield return cur;
					}
				}
			}
		}

		private static IEnumerable<CodeInstruction> TreeManagerTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
			RemoveBoundaryCheck(InstallTreeGridBufferLocals(ReplaceMath(instructions), il));

		private static IEnumerable<CodeInstruction> UpdateDataTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			LocalBuilder bufferLimit = il.DeclareLocal(typeof(int));
			LocalBuilder treeBuffer = il.DeclareLocal(typeof(TreeInstance[]));
			FieldInfo treeArrayField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees));
			MethodInfo baseUpdateData = AccessTools.Method(typeof(SimulationManagerBase<TreeManager, TreeProperties>), nameof(SimulationManagerBase<TreeManager, TreeProperties>.UpdateData));
			using (var codes = instructions.GetEnumerator()) {
				while(codes.MoveNext()) {
					var cur = codes.Current;
					if (cur.opcode == OpCodes.Call && cur.operand == baseUpdateData) {
						yield return cur;
						yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(TAMod.MaxTreeLimit)));
						yield return new CodeInstruction(OpCodes.Stloc_S, bufferLimit);
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, treeArrayField);
						yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer)));
						yield return new CodeInstruction(OpCodes.Stloc_S, treeBuffer);
					} else if (cur.opcode == OpCodes.Ldarg_0 && codes.MoveNext()) {
						var next = codes.Current;
						if (next.opcode == OpCodes.Ldfld && next.operand == treeArrayField && codes.MoveNext()) {
							yield return new CodeInstruction(OpCodes.Ldloc_S, treeBuffer).WithLabels(cur.labels);
						} else {
							yield return cur;
							yield return next;
						}
					} else if (cur.LoadsConstant(262144)) {
						yield return new CodeInstruction(OpCodes.Ldloc_S, bufferLimit);
					} else {
						yield return cur;
					}
                }
            }
        }

		private static IEnumerable<CodeInstruction> UpdateTreeRendererTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceMath(instructions);

		private static IEnumerable<CodeInstruction> UpdateTreesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
			RemoveBoundaryCheck(InstallTreeGridBufferLocals(ReplaceMath(instructions), il));

		private static void CustomSetPosY(TreeInstance[] trees, int treeID) {
			if ((trees[treeID].m_flags & 32) == 0) {
				trees[treeID].m_posY = 0;
			}
		}

		private static IEnumerable<CodeInstruction> DeserializeTranspiler(IEnumerable<CodeInstruction> instructions) {
			bool firstSig = false, secondSig = false, thirdSig = false;
			MethodInfo integratedDeserialize = AccessTools.Method(typeof(TASerializableDataExtension), nameof(TASerializableDataExtension.IntegratedDeserialize));
			MethodInfo getTreeInstance = AccessTools.PropertyGetter(typeof(Singleton<TreeManager>), nameof(Singleton<TreeManager>.instance));
			MethodInfo getDataVersion = AccessTools.PropertyGetter(typeof(DataSerializer), nameof(DataSerializer.version));
			FieldInfo nextGridTree = AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_nextGridTree));
			using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
				while (codes.MoveNext()) {
					CodeInstruction cur = codes.Current;
					if (!firstSig && cur.opcode == OpCodes.Call && cur.operand == getTreeInstance) {
						firstSig = true;
						yield return cur;
						if (codes.MoveNext()) {
							yield return codes.Current;
							yield return new CodeInstruction(OpCodes.Ldloc_0);
							yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.EnsureCapacity)));
						}
					} else if (firstSig && !secondSig && cur.opcode == OpCodes.Ldloc_1) {
						secondSig = true;
						codes.MoveNext();
						CodeInstruction next = codes.Current;
						if (next.opcode == OpCodes.Ldlen) {
							codes.MoveNext();
							CodeInstruction next2 = codes.Current;
							codes.MoveNext();
							CodeInstruction next3 = codes.Current;
							if (next2.opcode == OpCodes.Conv_I4 && next3.opcode == OpCodes.Stloc_3) {
								yield return new CodeInstruction(OpCodes.Ldc_I4, TAMod.DefaultTreeLimit);
								yield return next3;
							} else {
								yield return cur;
								yield return next;
								yield return next2;
								yield return next3;
							}
						} else {
							yield return cur;
							yield return next;
						}
					} else if (firstSig && secondSig && !thirdSig && cur.Is(OpCodes.Callvirt, getDataVersion)) {
						yield return cur;
						while (codes.MoveNext()) {
							cur = codes.Current;
							if (cur.opcode == OpCodes.Ldc_I4_1 && codes.MoveNext()) {
								CodeInstruction next = codes.Current;
								if (next.opcode == OpCodes.Stloc_S && codes.MoveNext()) {
									CodeInstruction next2 = codes.Current;
									if (next2.opcode == OpCodes.Br) {
										yield return new CodeInstruction(OpCodes.Ldloc_1).WithLabels(cur.ExtractLabels());
										yield return new CodeInstruction(OpCodes.Call, integratedDeserialize);
										yield return new CodeInstruction(OpCodes.Ldloc_0);
										yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees)));
										yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer)));
										yield return new CodeInstruction(OpCodes.Stloc_1);
										yield return new CodeInstruction(OpCodes.Ldloc_1);
										yield return new CodeInstruction(OpCodes.Ldlen);
										yield return new CodeInstruction(OpCodes.Conv_I4);
										yield return new CodeInstruction(OpCodes.Stloc_3);
										yield return cur;
										yield return next;
										yield return next2;
									} else {
										yield return cur;
										yield return next;
										yield return next2;
									}
								} else {
									yield return cur;
									yield return next;
								}
							} else if (cur.opcode == OpCodes.Stfld && cur.operand == nextGridTree) {
								yield return cur;
								codes.MoveNext();
								yield return codes.Current;
								codes.MoveNext();
								yield return codes.Current;
								yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.CustomSetPosY)));
								codes.MoveNext(); codes.MoveNext(); codes.MoveNext();
							} else {
								yield return cur;
							}
						}
					} else {
						yield return cur;
					}
				}
			}
		}

		private static void RemoveOrReplaceTree(int treeID) {
			//const int KeepTree = 0;
			const int RemoveTree = 1;
			const int ReplaceTree = 2;
			switch (TAMod.RemoveReplaceOrKeep) {
			case RemoveTree:
				try {
					Singleton<TreeManager>.instance.ReleaseTree((uint)treeID);
				} catch {
					TAMod.TALog("Error occured releasing tree during prefab initialization");
				}
				break;
			case ReplaceTree:
				TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
				TreeInfo treeInfo = PrefabCollection<TreeInfo>.GetLoaded(0);
				buffer[treeID].Info = treeInfo;
				buffer[treeID].m_infoIndex = (ushort)treeInfo.m_prefabDataIndex;
				break;
			default:
				/* Keep missing tree */
				break;
			}
		}

		private static bool ValidateTreePrefab(TreeInfo treeInfo) {
			try {
				TreeInfo prefabInfo = PrefabCollection<TreeInfo>.GetLoaded((uint)treeInfo.m_prefabDataIndex);
				if (!(prefabInfo is null) && prefabInfo.m_prefabDataIndex != -1) {
					return true;
				}
			} catch {
				TAMod.TALog("Exception occured during valiidate tree prefab. This is harmless");
			}
			return false;
		}

		public static bool OldAfterDeserializeHandler() {
			if (!TAMod.OldFormatLoaded) return false;
			int maxLen = TAMod.MaxTreeLimit;
			TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
			for (int i = 1; i < maxLen; i++) {
				if (buffer[i].m_flags != 0) {
					if (buffer[i].m_infoIndex >= 0) {
						TreeInfo treeInfo = buffer[i].Info;
						if (treeInfo is null || treeInfo?.m_prefabDataIndex < 0) {
							RemoveOrReplaceTree(i);
						} else {
							if (ValidateTreePrefab(treeInfo)) {
								buffer[i].m_infoIndex = (ushort)buffer[i].Info.m_prefabDataIndex;
								SetTreeScaleBrightness(i, treeInfo);
							} else {
								RemoveOrReplaceTree(i);
							}
						}
					}
				}
			}
			return true;
		}

		private static IEnumerable<CodeInstruction> AfterDeserializeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			Label isOldFormatExit = il.DefineLabel();
			int cachedInfoIndex = 0;
			FieldInfo treeCount = AccessTools.Field(typeof(DistrictPark), nameof(DistrictPark.m_treeCount));
			MethodInfo getInfo = AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.Info));
			using(var codes = instructions.GetEnumerator()) {
				while(codes.MoveNext()) {
					var cur = codes.Current;
					if (cur.opcode == OpCodes.Stloc_2) {
						yield return cur;
						yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.OldAfterDeserializeHandler)));
						yield return new CodeInstruction(OpCodes.Brtrue, isOldFormatExit);
					} else if (cur.opcode == OpCodes.Blt && codes.MoveNext()) {
						yield return cur;
						yield return codes.Current.WithLabels(isOldFormatExit);
					} else if (cur.opcode == OpCodes.Call && cur.operand == getInfo && codes.MoveNext()) {
						var next = codes.Current;
						if(next.opcode == OpCodes.Stloc_S) {
							cachedInfoIndex = (next.operand as LocalBuilder).LocalIndex;
							yield return cur;
							yield return next;
						} else {
							yield return cur;
							yield return next;
                        }
					} else if (cachedInfoIndex > 0 && cur.opcode == OpCodes.Stfld && cur.operand == treeCount) {
						yield return cur;
						yield return new CodeInstruction(OpCodes.Ldloc_3);
						yield return new CodeInstruction(OpCodes.Ldloc_S, cachedInfoIndex);
						yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.SetTreeScaleBrightness)));
					} else {
						yield return cur;
					}
                }
            }
		}

		private static IEnumerable<CodeInstruction> SerializeTranspiler(IEnumerable<CodeInstruction> instructions) {
			bool sigFound = false;
			using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
				FieldInfo burningTrees = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_burningTrees));
				MethodInfo loadingManagerInstance = AccessTools.PropertyGetter(typeof(Singleton<LoadingManager>), nameof(Singleton<LoadingManager>.instance));
				while (codes.MoveNext()) {
					CodeInstruction cur = codes.Current;
					if (!sigFound && cur.opcode == OpCodes.Ldloc_1 && codes.MoveNext()) {
						sigFound = true;
						CodeInstruction next = codes.Current;
						if (next.opcode == OpCodes.Ldlen && codes.MoveNext()) {
							yield return new CodeInstruction(OpCodes.Ldc_I4, TAMod.DefaultTreeLimit);
						}
					} else if (sigFound && cur.opcode == OpCodes.Ldarg_1 && codes.MoveNext()) {
						CodeInstruction next = codes.Current;
						if (next.opcode == OpCodes.Ldloc_0 && codes.MoveNext()) {
							CodeInstruction next2 = codes.Current;
							if (next2.opcode == OpCodes.Ldfld && next2.operand == burningTrees && codes.MoveNext()) {
								yield return cur;
								yield return new CodeInstruction(OpCodes.Ldc_I4_0);
								codes.MoveNext();
								yield return codes.Current;
								while (codes.MoveNext()) {
									cur = codes.Current;
									if (cur.opcode == OpCodes.Call && cur.operand == loadingManagerInstance) {
										yield return cur;
										break;
									}
								}
							} else {
								yield return cur;
								yield return next;
								yield return next2;
							}
						} else {
							yield return cur;
							yield return next;
						}
					} else {
						yield return cur;
					}
				}
			}
		}

		private void EnableTreeManagerPatch(Harmony harmony) {
			harmony.Patch(AccessTools.Method(typeof(TreeManager), @"Awake"),
			    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(AwakeTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.AfterTerrainUpdate)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.AfterTerrainUpdateTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), @"BeginRenderingImpl"),
			    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(BeginRenderingImplTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateAreaHeight)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.CalculateAreaHeightTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateGroupData)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.CalculateGroupDataTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CheckLimits)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CheckLimitsTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CreateTree)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CreateTreeTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), @"EndRenderingImpl"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(EndRenderingImplTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), @"FinalizeTree"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.FinalizeTreeTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), @"HandleFireSpread"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.HandleFireSpreadTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), @"InitializeTree"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.InitializeTreeTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), @"OnPostRender"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.OnPostRenderTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.OverlapQuad)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.TreeManagerOverlapQuadTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.PopulateGroupData)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.TreeManagerPopulateGroupDataTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.RayCast)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.TreeManagerRayCastTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.SampleSmoothHeight)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.SampleSmoothHeightTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), @"SimulationStepImpl"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.SimulationStepImplTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.TerrainUpdated)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.TreeManagerTerrainUpdatedTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateData)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.UpdateDataTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTreeRenderer)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.UpdateTreeRendererTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTrees)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.UpdateTreesTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Deserialize)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(DeserializeTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Serialize)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(SerializeTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.AfterDeserialize)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(AfterDeserializeTranspiler))));
		}

		private void DisableTreeManagerPatch(Harmony harmony) {
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"Awake"), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.AfterTerrainUpdate)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"BeginRenderingImpl"), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateAreaHeight)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateGroupData)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CheckLimits)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CreateTree)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"EndRenderingImpl"), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"FinalizeTree"), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"HandleFireSpread"), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"InitializeTree"), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"OnPostRender"), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.OverlapQuad)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.PopulateGroupData)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.RayCast)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.SampleSmoothHeight)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"SimulationStepImpl"), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.TerrainUpdated)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateData)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTreeRenderer)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTrees)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Deserialize)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Serialize)), HarmonyPatchType.Transpiler, HARMONYID);
			harmony.Unpatch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.AfterDeserialize)), HarmonyPatchType.Transpiler, HARMONYID);
		}
	}
}