using ColossalFramework;
using ColossalFramework.IO;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
        private static IEnumerable<CodeInstruction> ReplaceLDCI4_MaxTreeLimit(IEnumerable<CodeInstruction> instructions) {
            foreach (var code in instructions) {
                if (code.Is(OpCodes.Ldc_I4, LastMaxTreeLimit)) {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit);
                } else {
                    yield return code;
                }
            }
        }



        // Patch WeatherManager::CalculateSelfHeight()
        // Affects Tree on Wind Effect, stops tree from slowing wind
        private static IEnumerable<CodeInstruction> CalculateSelfHeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label returnTreeManagerLabel = il.DefineLabel();
            MethodInfo getTreeInstance = AccessTools.PropertyGetter(typeof(Singleton<TreeManager>), nameof(Singleton<TreeManager>.instance));
            using IEnumerator<CodeInstruction> codes = instructions.GetEnumerator();
            while (codes.MoveNext()) {
                CodeInstruction cur = codes.Current;
                if (cur.opcode == OpCodes.Ldloca_S && codes.MoveNext()) {
                    CodeInstruction next = codes.Current;
                    codes.MoveNext();
                    CodeInstruction inter = codes.Current;
                    if (next.opcode == OpCodes.Ldloca_S && inter.opcode == OpCodes.Ldloca_S && codes.MoveNext()) {
                        CodeInstruction next1 = codes.Current;
                        if (next1.opcode == OpCodes.Callvirt && codes.MoveNext()) {
                            CodeInstruction next2 = codes.Current;
                            if (next2.Is(OpCodes.Call, getTreeInstance)) {
                                LocalBuilder terrainAvg = cur.operand as LocalBuilder;
                                LocalBuilder terrainMax = next.operand as LocalBuilder;
                                yield return cur;
                                yield return next;
                                yield return inter;
                                yield return next1;
                                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TreeEffectOnWind)));
                                yield return new CodeInstruction(OpCodes.Brtrue_S, returnTreeManagerLabel);
                                yield return new CodeInstruction(OpCodes.Ldloc_S, terrainAvg);
                                yield return new CodeInstruction(OpCodes.Ldloc_S, terrainMax);
                                yield return new CodeInstruction(OpCodes.Add);
                                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                                yield return new CodeInstruction(OpCodes.Shr);
                                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                                yield return new CodeInstruction(OpCodes.Ldc_I4, 65535);
                                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Clamp", new Type[] { typeof(int), typeof(int), typeof(int) }));
                                yield return new CodeInstruction(OpCodes.Conv_U2);
                                yield return new CodeInstruction(OpCodes.Ret);
                                yield return next2.WithLabels(returnTreeManagerLabel);
                            } else {
                                yield return cur;
                                yield return next;
                                yield return inter;
                                yield return next1;
                                yield return next2;
                            }
                        } else {
                            yield return cur;
                            yield return next;
                            yield return inter;
                            yield return next1;
                        }
                    } else {
                        yield return cur;
                        yield return next;
                        yield return inter;
                    }
                } else {
                    yield return cur;
                }
            }
        }

        private const int MAX_MAPEDITOR_TREES = 250000;
        private const int MAX_MAP_TREES_CEILING = DefaultTreeLimit - 5;
        private static IEnumerable<CodeInstruction> CheckLimitsTranspiler(IEnumerable<CodeInstruction> instructions) {
            foreach (var instruction in instructions) {
                if (instruction.Is(OpCodes.Ldc_I4, MAX_MAPEDITOR_TREES))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(CheckLowLimit)));
                else if (instruction.Is(OpCodes.Ldc_I4, MAX_MAP_TREES_CEILING))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(CheckHighLimit)));
                else
                    yield return instruction;
            }
        }

        /* For Forestry Lock */
        private static IEnumerable<CodeInstruction> NRMTreesModifiedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label jump = il.DefineLabel();
            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseLockForestry)));
            yield return new CodeInstruction(OpCodes.Brfalse_S, jump);
            yield return new CodeInstruction(OpCodes.Ret);
            using IEnumerator<CodeInstruction> codes = instructions.GetEnumerator();
            if (codes.MoveNext()) codes.Current.WithLabels(jump);
            do {
                if (codes.Current.Is(OpCodes.Ldc_I4, LastMaxTreeLimit)) {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit);
                } else {
                    yield return codes.Current;
                }
            } while (codes.MoveNext());
        }

        public static void CustomSetPosY(TreeInstance[] trees, int treeID) {
            if ((trees[treeID].m_flags & 32) == 0) {
                trees[treeID].m_posY = 0;
            }
        }

        private static IEnumerable<CodeInstruction> DeserializeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            bool firstSig = false, secondSig = false, thirdSig = false;
            MethodInfo integratedDeserialize = AccessTools.Method(typeof(TASerializableDataExtension), nameof(TASerializableDataExtension.IntegratedDeserialize));
            MethodInfo getTreeInstance = AccessTools.PropertyGetter(typeof(Singleton<TreeManager>), nameof(Singleton<TreeManager>.instance));
            MethodInfo getDataVersion = AccessTools.PropertyGetter(typeof(DataSerializer), nameof(DataSerializer.version));
            FieldInfo nextGridTree = AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_nextGridTree));
            using IEnumerator<CodeInstruction> codes = instructions.GetEnumerator();
            while (codes.MoveNext()) {
                CodeInstruction cur = codes.Current;
                if (!firstSig && cur.opcode == OpCodes.Call && cur.operand == getTreeInstance) {
                    firstSig = true;
                    yield return cur;
                    codes.MoveNext();
                    yield return codes.Current;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.EnsureCapacity)));
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
                            yield return new CodeInstruction(OpCodes.Ldc_I4, DefaultTreeLimit);
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
                                    yield return cur;
                                    yield return next;
                                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                                    yield return new CodeInstruction(OpCodes.Call, integratedDeserialize);
                                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees)));
                                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer)));
                                    yield return new CodeInstruction(OpCodes.Stloc_1);
                                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                                    yield return new CodeInstruction(OpCodes.Ldlen);
                                    yield return new CodeInstruction(OpCodes.Conv_I4);
                                    yield return new CodeInstruction(OpCodes.Stloc_3);
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

        private const int KeepTree = 0;
        private const int RemoveTree = 1;
        private const int ReplaceTree = 2;
        private static void RemoveOrReplaceTree(uint treeID) {
            switch (RemoveReplaceOrKeep) {
            case RemoveTree:
                try {
                    Singleton<TreeManager>.instance.ReleaseTree(treeID);
                } catch {
                    TALog("Error occured releasing tree during prefab initialization");
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
                if (prefabInfo != null && prefabInfo.m_prefabDataIndex != -1) {
                    return true;
                }
            } catch {
                TALog("Exception occured during valiidate tree prefab. This is harmless");
            }
            return false;
        }

        public static bool OldAfterDeserializeHandler() {
            if (!OldFormatLoaded) return false;
            int maxLen = MaxTreeLimit;
            TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
            for (uint i = 1; i < maxLen; i++) {
                if (buffer[i].m_flags != 0) {
                    if (buffer[i].m_infoIndex >= 0) {
                        TreeInfo treeInfo = buffer[i].Info;
                        if (treeInfo == null || treeInfo?.m_prefabDataIndex < 0) {
                            RemoveOrReplaceTree(i);
                        } else {
                            if (ValidateTreePrefab(treeInfo)) {
                                buffer[i].m_infoIndex = (ushort)buffer[i].Info.m_prefabDataIndex;
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
            bool firstSig = false, secondSig = false;
            CodeInstruction prev = default, cur = default;
            Label isOldFormatExit = il.DefineLabel();
            foreach (var next in instructions) {
                if (!firstSig && prev?.opcode == OpCodes.Conv_I4 && cur?.opcode == OpCodes.Stloc_2) {
                    firstSig = true;
                    yield return prev;
                    yield return cur;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.OldAfterDeserializeHandler)));
                    yield return new CodeInstruction(OpCodes.Brtrue, isOldFormatExit);
                    prev = cur = null;
                } else if (firstSig && !secondSig && prev?.opcode == OpCodes.Blt && cur?.opcode == OpCodes.Ldloc_0 && next?.opcode == OpCodes.Ldloc_0) {
                    secondSig = true;
                    yield return prev;
                    yield return cur.WithLabels(isOldFormatExit);
                    prev = cur = null;
                } else if (prev is not null && cur is not null) {
                    yield return prev;
                    yield return cur;
                    prev = cur = null;
                }
                prev = cur;
                cur = next;
            }
            if (prev is not null) yield return prev;
            if (cur is not null) yield return cur;
        }

        private static IEnumerable<CodeInstruction> SerializeTranspiler(IEnumerable<CodeInstruction> instructions) {
            bool sigFound = false;
            using IEnumerator<CodeInstruction> codes = instructions.GetEnumerator();
            FieldInfo burningTrees = AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_burningTrees));
            MethodInfo loadingManagerInstance = AccessTools.PropertyGetter(typeof(Singleton<LoadingManager>), nameof(Singleton<LoadingManager>.instance));
            while (codes.MoveNext()) {
                CodeInstruction cur = codes.Current;
                if (!sigFound && cur.opcode == OpCodes.Ldloc_1 && codes.MoveNext()) {
                    sigFound = true;
                    CodeInstruction next = codes.Current;
                    if (next.opcode == OpCodes.Ldlen && codes.MoveNext()) {
                        yield return new CodeInstruction(OpCodes.Ldc_I4, DefaultTreeLimit);
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

        private static IEnumerable<CodeInstruction> AwakeTranspiler(IEnumerable<CodeInstruction> instructions) {
            foreach (var code in instructions) {
                if (code.LoadsConstant(DefaultTreeLimit)) {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(MaxTreeLimit)));
                } else if (code.LoadsConstant(DefaultTreeUpdateCount)) {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(MaxTreeUpdateLimit)));
                } else yield return code;
            }
        }

        public static int BeginRenderSkipCount = 0;
        private static IEnumerable<CodeInstruction> BeginRenderingImplTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            using IEnumerator<CodeInstruction> codes = instructions.GetEnumerator();
            Label CountExpired = il.DefineLabel();
            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAPatcher), nameof(BeginRenderSkipCount)));
            yield return new CodeInstruction(OpCodes.Dup);
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Add);
            yield return new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(TAPatcher), nameof(BeginRenderSkipCount)));
            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(BeginSkipFrameCount)));
            yield return new CodeInstruction(OpCodes.Rem);
            yield return new CodeInstruction(OpCodes.Brfalse_S, CountExpired);
            yield return new CodeInstruction(OpCodes.Ret);
            if (codes.MoveNext()) codes.Current.WithLabels(CountExpired);
            do {
                yield return codes.Current;
            } while (codes.MoveNext());
        }

        internal void InjectTreeLimit(Harmony harmony) {
            HarmonyMethod replaceLDCI4 = new(AccessTools.Method(typeof(TAPatcher), nameof(ReplaceLDCI4_MaxTreeLimit)));
            harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.SaveProps)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.ClearDecorations)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(CommonBuildingAI), @"HandleFireSpread"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(DisasterHelpers), nameof(DisasterHelpers.DestroyTrees)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(FireCopterAI), @"FindBurningTree"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(ForestFireAI), @"FindClosestTree"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeTool), @"ApplyBrush"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.AfterTerrainUpdate)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateAreaHeight)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateGroupData)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), @"EndRenderingImpl"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), @"HandleFireSpread"), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.OverlapQuad)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.PopulateGroupData)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.RayCast)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.SampleSmoothHeight)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.TerrainUpdated)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateData)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTrees)), transpiler: replaceLDCI4);
            harmony.Patch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(NRMTreesModifiedTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CheckLimits)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CheckLimitsTranspiler))));
        }

        internal void RemoveTreeLimitPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.SaveProps)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.ClearDecorations)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(CommonBuildingAI), @"HandleFireSpread"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(DisasterHelpers), nameof(DisasterHelpers.DestroyTrees)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(FireCopterAI), @"FindBurningTree"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(ForestFireAI), @"FindClosestTree"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), @"ApplyBrush"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.AfterTerrainUpdate)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateAreaHeight)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateGroupData)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"EndRenderingImpl"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"HandleFireSpread"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.OverlapQuad)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.PopulateGroupData)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.RayCast)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.SampleSmoothHeight)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.TerrainUpdated)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateData)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTrees)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CheckLimits)), HarmonyPatchType.Transpiler, HARMONYID);
        }

        private void EnableTreeLimitPatches(Harmony harmony) {
            try {
                InjectTreeLimit(harmony);
                harmony.Patch(AccessTools.Method(typeof(TreeManager), @"Awake"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(AwakeTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(WeatherManager), @"CalculateSelfHeight"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CalculateSelfHeightTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Deserialize)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(DeserializeTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Serialize)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(SerializeTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.AfterDeserialize)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(AfterDeserializeTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(BeginRenderingImplTranspiler))));
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void DisableTreeLimitPatches(Harmony harmony) {
            RemoveTreeLimitPatches(harmony);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), @"Awake"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(WeatherManager), @"CalculateSelfHeight"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Deserialize)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Serialize)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.AfterDeserialize)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeManager), "BeginRenderingImpl"), HarmonyPatchType.Transpiler, HARMONYID);
        }

        public static void EnsureCapacity(TreeManager manager) {
            if (manager.m_trees.m_buffer.Length != MaxTreeLimit) {
                manager.m_trees = new Array32<TreeInstance>((uint)MaxTreeLimit);
                manager.m_updatedTrees = new ulong[MaxTreeUpdateLimit];
                Array.Clear(manager.m_trees.m_buffer, 0, manager.m_trees.m_buffer.Length);
                manager.m_trees.CreateItem(out uint _);
                SingletonLite<TAManager>.instance.SetScaleBuffer(MaxTreeLimit);
            }
        }
    }
}
