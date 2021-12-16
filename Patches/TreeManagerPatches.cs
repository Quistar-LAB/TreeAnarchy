using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TreeAnarchy.Patches {
    internal static class TreeManagerPatches {
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
            using (var codes = TreeInstancePatches.ReplaceMath(instructions).GetEnumerator()) {
                while (codes.MoveNext()) {
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
                        while (codes.MoveNext()) {
                            if (codes.Current.opcode == OpCodes.Br) break;
                        }
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        private delegate T PrefabGetter<out T>();
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
        private static RenderFireEffectAPI delegatedRenderFireEffect;

        private static void CustomAwakeRoutine(TreeManager instance) {
            FieldInfo simPrefabs = typeof(PrefabCollection<TreeInfo>).GetField("m_simulationPrefabs", BindingFlags.NonPublic | BindingFlags.Static);
            m_simulationPrefabs = CreateGetter<FastList<PrefabCollection<TreeInfo>.PrefabData>>(simPrefabs);
            delegatedRenderFireEffect = instance.RenderFireEffect;
            TAManager.m_extraTreeInfos = new TAManager.ExtraTreeInfo[TAMod.MaxTreeLimit];
            TAMod.TALog("Max tree limit set at: " + TAMod.MaxTreeLimit);
        }

        private static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions) {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.CustomAwakeRoutine)));
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
                while (codes.MoveNext()) {
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
                        if (next.opcode == OpCodes.Ldfld && next.operand == groupLayerMaterials) {
                            if (!firstgroupLayerOccurance) {
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
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.BeginRenderingImplCoroutine)));
                        yield return new CodeInstruction(OpCodes.Ret);
                        break;
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> CalculateAreaHeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            LocalBuilder treeScales = il.DeclareLocal(typeof(TAManager.ExtraTreeInfo[]));
            using (var codes = RemoveBoundaryCheck(InstallTreeGridBufferLocals(TreeInstancePatches.ReplaceMath(instructions), il)).GetEnumerator()) {
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAManager), nameof(TAManager.m_extraTreeInfos)));
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
                                yield return new CodeInstruction(OpCodes.Ldelema, typeof(TAManager.ExtraTreeInfo));
                                yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAManager.ExtraTreeInfo), nameof(TAManager.ExtraTreeInfo.TreeScale)));
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

        private unsafe static bool EndRenderingImplPrefix(TreeManager __instance, RenderManager.CameraInfo cameraInfo) {
            int treeLayer = __instance.m_treeLayer;
            ref DrawCallData drawCallData = ref __instance.m_drawCallData;
            RenderManager rmInstance = Singleton<RenderManager>.instance;
            RenderGroup[] renderedGroups = rmInstance.m_renderedGroups.m_buffer;
            Vector4 objectIndex = RenderManager.DefaultColorLocation;
            objectIndex.z = -1f;
            int ID_Color = __instance.ID_Color;
            int ID_ObjectIndex = __instance.ID_ObjectIndex;
            int ID_TreeColor = __instance.ID_TreeColor;
            int ID_TreeLocation = __instance.ID_TreeLocation;
            int ID_TreeObjectIndex = __instance.ID_TreeObjectIndex;
            MaterialPropertyBlock materialBlock = __instance.m_materialBlock;
            Matrix4x4 identity = EMath.matrix4Identity;
            Vector3 lodMin = EMath.DefaultLodMin;
            Vector3 lodMax = EMath.DefaultLodMax;
            Color black = EMath.ColorBlack;
            Vector4 v4zero = EMath.Vector4Zero;
            int len = rmInstance.m_renderedGroups.m_size;
            fixed (Quaternion* pQuaternions = &TAManager.m_treeQuaternions[0])
            fixed (TAManager.ExtraTreeInfo* pExtraInfos = &TAManager.m_extraTreeInfos[0])
            fixed (TreeInstance* pTrees = &__instance.m_trees.m_buffer[0])
            fixed (uint* pGrid = &__instance.m_treeGrid[0]) {
                for (int i = 0; i < len; i++) {
                    RenderGroup renderGroup = renderedGroups[i];
                    if ((renderGroup.m_instanceMask & 1 << treeLayer) != 0) {
                        int startX = renderGroup.m_x * (540 / 45);
                        int startZ = renderGroup.m_z * (540 / 45);
                        int endX = (renderGroup.m_x + 1) * (540 / 45) - 1;
                        int endZ = (renderGroup.m_z + 1) * (540 / 45) - 1;
                        for (int j = startZ; j <= endZ; j++) {
                            for (int k = startX; k <= endX; k++) {
                                uint treeID = *(pGrid + (j * 540 + k));
                                while (treeID != 0u) {
                                    TreeInstance* pTree = pTrees + treeID;
                                    ushort flags = pTree->m_flags;
                                    if ((flags & 0x0f00) != 0 && (flags & (ushort)TreeInstance.Flags.Hidden) == 0) {
                                        TreeInfo info = pTree->Info;
                                        Vector3 position = pTree->Position;
                                        if (info.m_prefabInitialized && cameraInfo.Intersect(position, info.m_generatedInfo.m_size.y * info.m_maxScale)) {
                                            TAManager.ExtraTreeInfo* extraTreeInfo = pExtraInfos + treeID;
                                            float scale = extraTreeInfo->TreeScale;
                                            float brightness = extraTreeInfo->m_brightness;
                                            if (cameraInfo is null || info.m_lodMesh1 is null || cameraInfo.ECheckRenderDistance(position, info.m_lodRenderDistance)) {
                                                Color value = info.m_defaultColor * brightness;
                                                value.a = TAManager.GetWindSpeed(position);
                                                materialBlock.Clear();
                                                materialBlock.SetColor(ID_Color, value);
                                                materialBlock.SetVector(ID_ObjectIndex, objectIndex);
                                                drawCallData.m_defaultCalls++;
                                                Graphics.DrawMesh(info.m_mesh,
                                                    Matrix4x4.TRS(position, *(pQuaternions + ((int)(position.x * position.x + position.z * position.z) % 359)), new Vector3(scale, scale, scale)),
                                                    info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
                                            } else {
                                                position.y += info.m_generatedInfo.m_center.y * (scale - 1f);
                                                Color color = info.m_defaultColor * brightness;
                                                color.a = TAManager.GetWindSpeed(position);
                                                info.m_lodLocations[info.m_lodCount] = new Vector4(position.x, position.y, position.z, scale);
                                                info.m_lodColors[info.m_lodCount] = color.linear;
                                                info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
                                                info.m_lodMin = EMath.Min(info.m_lodMin, position);
                                                info.m_lodMax = EMath.Max(info.m_lodMax, position);
                                                if (++info.m_lodCount == info.m_lodLocations.Length) {
                                                    Mesh mesh;
                                                    int num;
                                                    if (info.m_lodCount <= 1) {
                                                        mesh = info.m_lodMesh1;
                                                        num = 1;
                                                    } else if (info.m_lodCount <= 4) {
                                                        mesh = info.m_lodMesh4;
                                                        num = 4;
                                                    } else if (info.m_lodCount <= 8) {
                                                        mesh = info.m_lodMesh8;
                                                        num = 8;
                                                    } else {
                                                        mesh = info.m_lodMesh16;
                                                        num = 16;
                                                    }
                                                    for (int l = info.m_lodCount; l < num; l++) {
                                                        info.m_lodLocations[l] = cameraInfo.m_forward * -100000f;
                                                        info.m_lodColors[l] = black;
                                                        info.m_lodObjectIndices[i] = v4zero;
                                                    }
                                                    materialBlock.Clear();
                                                    materialBlock.SetVectorArray(ID_TreeLocation, info.m_lodLocations);
                                                    materialBlock.SetVectorArray(ID_TreeColor, info.m_lodColors);
                                                    materialBlock.SetVectorArray(ID_TreeObjectIndex, info.m_lodObjectIndices);
                                                    Bounds bounds = default;
                                                    bounds.SetMinMax(new Vector3(info.m_lodMin.x - 100f, info.m_lodMin.y - 100f, info.m_lodMin.z - 100f),
                                                                     new Vector3(info.m_lodMax.x + 100f, info.m_lodMax.y + 100f, info.m_lodMax.z + 100f));
                                                    mesh.bounds = bounds;
                                                    info.m_lodMin = lodMin;
                                                    info.m_lodMax = lodMax;
                                                    drawCallData.m_lodCalls++;
                                                    drawCallData.m_batchedCalls += (info.m_lodCount - 1);
                                                    Graphics.DrawMesh(mesh, identity, info.m_lodMaterial, info.m_prefabDataLayer, null, 0, materialBlock);
                                                    info.m_lodCount = 0;
                                                }
                                            }
                                        }
                                    }
                                    treeID = pTree->m_nextGridTree;
                                }
                            }
                        }
                    }
                }
                len = PrefabCollection<TreeInfo>.PrefabCount();
                PrefabCollection<TreeInfo>.PrefabData[] simPrefabs = m_simulationPrefabs().m_buffer;
                for (int l = 0; l < len; l++) {
                    if (simPrefabs[l].m_prefab is TreeInfo info && info.m_lodCount != 0) {
                        Mesh mesh;
                        int num;
                        if (info.m_lodCount <= 1) {
                            mesh = info.m_lodMesh1;
                            num = 1;
                        } else if (info.m_lodCount <= 4) {
                            mesh = info.m_lodMesh4;
                            num = 4;
                        } else if (info.m_lodCount <= 8) {
                            mesh = info.m_lodMesh8;
                            num = 8;
                        } else {
                            mesh = info.m_lodMesh16;
                            num = 16;
                        }
                        for (int i = info.m_lodCount; i < num; i++) {
                            info.m_lodLocations[i] = cameraInfo.m_forward * -100000f;
                            info.m_lodColors[i] = black;
                            info.m_lodObjectIndices[i] = v4zero;
                        }
                        materialBlock.Clear();
                        materialBlock.SetVectorArray(ID_TreeLocation, info.m_lodLocations);
                        materialBlock.SetVectorArray(ID_TreeColor, info.m_lodColors);
                        materialBlock.SetVectorArray(ID_TreeObjectIndex, info.m_lodObjectIndices);
                        Bounds bounds = default;
                        bounds.SetMinMax(new Vector3(info.m_lodMin.x - 100f, info.m_lodMin.y - 100f, info.m_lodMin.z - 100f),
                                         new Vector3(info.m_lodMax.x + 100f, info.m_lodMax.y + 100f, info.m_lodMax.z + 100f));
                        mesh.bounds = bounds;
                        info.m_lodMin = lodMin;
                        info.m_lodMax = lodMax;
                        drawCallData.m_lodCalls++;
                        drawCallData.m_batchedCalls += (info.m_lodCount - 1);
                        Graphics.DrawMesh(mesh, identity, info.m_lodMaterial, info.m_prefabDataLayer, null, 0, materialBlock);
                        info.m_lodCount = 0;
                    }
                }
                if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None) {
                    TreeManager.BurningTree[] burningTrees = __instance.m_burningTrees.m_buffer;
                    len = __instance.m_burningTrees.m_size;
                    for (int i = 0; i < len; i++) {
                        ref TreeManager.BurningTree burningTree = ref burningTrees[i];
                        if (burningTree.m_treeIndex != 0u) {
                            float fireIntensity = burningTree.m_fireIntensity * 0.003921569f;
                            float fireDamage = burningTree.m_fireDamage * 0.003921569f;
                            delegatedRenderFireEffect(cameraInfo, burningTree.m_treeIndex, ref *(pTrees + burningTree.m_treeIndex), fireIntensity, fireDamage);
                        }
                    }
                }
            }
            return false;
        }

        private static IEnumerable<CodeInstruction> FinalizeTreeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            int treeGridOccuranceCount = 0;
            LocalBuilder treeGrid = il.DeclareLocal(typeof(uint[]));
            LocalBuilder treeBuffer = il.DeclareLocal(typeof(TreeInstance[]));
            FieldInfo treeGridField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_treeGrid));
            FieldInfo treeArrayField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees));
            using (var codes = TreeInstancePatches.ReplaceMath(instructions).GetEnumerator()) {
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
                    } else if (cur.opcode == OpCodes.Ldloc_S && cur.operand is LocalBuilder l1 && l1.LocalIndex == 5) {
                        while (codes.MoveNext()) {
                            if (codes.Current.opcode == OpCodes.Leave) break;
                        }
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
            using (var codes = InstallTreeGridBufferLocals(TreeInstancePatches.ReplaceMath(instructions), il).GetEnumerator()) {
                yield return new CodeInstruction(OpCodes.Call, simManager);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SimulationManager), nameof(SimulationManager.m_randomizer)));
                yield return new CodeInstruction(OpCodes.Stloc_S, randomizer);
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Stloc_S && (cur.operand as LocalBuilder).LocalIndex == 18 && codes.MoveNext()) {
                        var next = codes.Current;
                        if (next.opcode == OpCodes.Ldloc_S && (next.operand as LocalBuilder).LocalIndex == 18 && codes.MoveNext()) {
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
            int random = new Randomizer(treeID).Int32(10000u);
            TAManager.m_extraTreeInfos[treeID].m_treeScale = info.m_minScale + random * (info.m_maxScale - info.m_minScale) * 0.0001f;
            TAManager.m_extraTreeInfos[treeID].m_brightness = info.m_minBrightness + random * (info.m_maxBrightness - info.m_minBrightness) * 0.0001f;
        }

        private static IEnumerable<CodeInstruction> CreateTreeTranspiler(IEnumerable<CodeInstruction> instructions) {
            bool firstSigFound = false;
            bool installedNew = false;
            MethodInfo createItem = AccessTools.Method(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.CreateItem), new Type[] { typeof(uint).MakeByRefType(), typeof(Randomizer).MakeByRefType() });
            foreach (var code in instructions) {
                if (!firstSigFound && code.opcode == OpCodes.Callvirt && code.operand == createItem) {
                    firstSigFound = true;
                    yield return code;
                } else if (firstSigFound && !installedNew && code.opcode == OpCodes.Stind_I4) {
                    installedNew = true;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.SetTreeScaleBrightness)));
                } else {
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> InitializeTreeTranspiler(IEnumerable<CodeInstruction> instructions) => TreeInstancePatches.ReplaceMath(instructions);

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
            foreach (var code in instructions) {
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
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.OnPostRenderCoroutine)));
                    yield return instructions.Last();
                    break;
                } else {
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> TreeManagerOverlapQuadTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
             RemoveBoundaryCheck(InstallTreeGridBufferLocals(TreeInstancePatches.ReplaceMath(instructions), il));

        private static IEnumerable<CodeInstruction> TreeManagerPopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
            RemoveBoundaryCheck(InstallTreeGridBufferLocals(instructions, il));

        private static IEnumerable<CodeInstruction> TreeManagerRayCastTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
            RemoveBoundaryCheck(InstallTreeGridBufferLocals(TreeInstancePatches.ReplaceMath(instructions), il));

        private static IEnumerable<CodeInstruction> SampleSmoothHeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
            RemoveBoundaryCheck(InstallTreeGridBufferLocals(TreeInstancePatches.ReplaceMath(instructions), il));

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
            RemoveBoundaryCheck(InstallTreeGridBufferLocals(TreeInstancePatches.ReplaceMath(instructions), il));

        private static IEnumerable<CodeInstruction> UpdateDataTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            LocalBuilder bufferLimit = il.DeclareLocal(typeof(int));
            LocalBuilder treeBuffer = il.DeclareLocal(typeof(TreeInstance[]));
            FieldInfo treeArrayField = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees));
            MethodInfo baseUpdateData = AccessTools.Method(typeof(SimulationManagerBase<TreeManager, TreeProperties>), nameof(SimulationManagerBase<TreeManager, TreeProperties>.UpdateData));
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
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

        private static IEnumerable<CodeInstruction> UpdateTreeRendererTranspiler(IEnumerable<CodeInstruction> instructions) => TreeInstancePatches.ReplaceMath(instructions);

        private static IEnumerable<CodeInstruction> UpdateTreesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) =>
            RemoveBoundaryCheck(InstallTreeGridBufferLocals(TreeInstancePatches.ReplaceMath(instructions), il));

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
                                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.CustomSetPosY)));
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
                if (buffer[i].m_flags != 0 && buffer[i].m_infoIndex >= 0) {
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
            return true;
        }

        private static IEnumerable<CodeInstruction> AfterDeserializeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label isOldFormatExit = il.DefineLabel();
            int cachedInfoIndex = 0;
            FieldInfo infoIndex = AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_infoIndex));
            MethodInfo getInfo = AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.Info));
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Stloc_2) {
                        yield return cur;
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.OldAfterDeserializeHandler)));
                        yield return new CodeInstruction(OpCodes.Brtrue, isOldFormatExit);
                    } else if (cur.opcode == OpCodes.Blt && codes.MoveNext()) {
                        yield return cur;
                        yield return codes.Current.WithLabels(isOldFormatExit);
                    } else if (cur.opcode == OpCodes.Call && cur.operand == getInfo && codes.MoveNext()) {
                        var next = codes.Current;
                        if (next.opcode == OpCodes.Stloc_S) {
                            cachedInfoIndex = (next.operand as LocalBuilder).LocalIndex;
                            yield return cur;
                            yield return next;
                        } else {
                            yield return cur;
                            yield return next;
                        }
                    } else if (cachedInfoIndex > 0 && cur.opcode == OpCodes.Stfld && cur.operand == infoIndex) {
                        yield return cur;
                        yield return new CodeInstruction(OpCodes.Ldloc_3);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, cachedInfoIndex);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.SetTreeScaleBrightness)));
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

        internal static void EnableTreeManagerPatch(Harmony harmony) {
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"Awake"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(AwakeTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::Awake. This is Fatal");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                //harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.AfterTerrainUpdate)),
                //    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.AfterTerrainUpdateTranspiler))));
                MethodInfo afterTerrainUpdate = AccessTools.Method(typeof(TreeManager), nameof(TreeManager.AfterTerrainUpdate));
                var patches = Harmony.GetPatchInfo(afterTerrainUpdate);
                if (!(patches is null) && !(patches.Prefixes is null)) {
                    harmony.Unpatch(afterTerrainUpdate, HarmonyPatchType.Prefix);
                }
                harmony.Patch(afterTerrainUpdate, transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.AfterTerrainUpdateTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::AfterTerrainUpdate with prefix");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"BeginRenderingImpl"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(BeginRenderingImplTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::BeginRenderingImpl");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateAreaHeight)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.CalculateAreaHeightTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::CalculateAreaHeight");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateGroupData)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.CalculateGroupDataTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::CalculateGroupData");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CheckLimits)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(CheckLimitsTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::CheckLimits");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CreateTree)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(CreateTreeTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::CreateTree. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"EndRenderingImpl"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(EndRenderingImplPrefix))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::EndRenderingImpl");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"FinalizeTree"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.FinalizeTreeTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::FinalizeTree");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"HandleFireSpread"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.HandleFireSpreadTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::HandleFireSpread. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"InitializeTree"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.InitializeTreeTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::InitializeTree. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"OnPostRender"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.OnPostRenderTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::OnPostRender. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.OverlapQuad)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.TreeManagerOverlapQuadTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::OverlapQuad");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.PopulateGroupData)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.TreeManagerPopulateGroupDataTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::PopulateGroupData");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.RayCast)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.TreeManagerRayCastTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::RayCast. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.SampleSmoothHeight)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.SampleSmoothHeightTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::SampleSmoothHeight");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"SimulationStepImpl"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.SimulationStepImplTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::SimulationStepImpl");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.TerrainUpdated)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.TreeManagerTerrainUpdatedTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::TerrainUpdated");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateData)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.UpdateDataTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::UpdateData. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTreeRenderer)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.UpdateTreeRendererTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::UpdateTreeRenderer. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTrees)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(TreeManagerPatches.UpdateTreesTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::UpdateTrees");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Deserialize)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(DeserializeTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::Data::Deserialize");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Serialize)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(SerializeTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::Data::Serialize");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.AfterDeserialize)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerPatches), nameof(AfterDeserializeTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeManager::Data::AfterDeserialize");
                TAMod.TALog(e.Message);
                throw;
            }
        }

        internal static void DisableTreeManagerPatch(Harmony harmony, string HARMONYID) {
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"Awake"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.AfterTerrainUpdate)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"BeginRenderingImpl"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateAreaHeight)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateGroupData)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CheckLimits)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CreateTree)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"EndRenderingImpl"), HarmonyPatchType.Prefix, HARMONYID);
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
