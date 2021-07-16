#define FULLVERBOSE
#define QUIETVERBOSE
#define SILENT
//#define FULLVERBOSE
//#define DEBUG
#if SILENT
#undef DEBUG
#undef FULLVERBOSE
#undef QUIETVERBOSE
#endif

using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy.Patches {
    internal static class TreeLimit {
#if DEBUG
        internal static void PrintDebugIL(List<CodeInstruction> codes, MethodBase method)
        {
#if QUIETVERBOSE
            Debug.Log($"TreeAnarchy: Harmony Transpiler Patched for method: [{method.Name}] ==> Set MaxTreeLimit to {MaxTreeLimit}");
#if FULLVERBOSE
            foreach (var code in codes)
            {
                Debug.Log($"====   IL: {code}");
            }
            Debug.Log("------------------------------------------------------------------");
#endif
#endif
        }
#endif
#if DEBUG
        private static IEnumerable<CodeInstruction> ReplaceLDCI4_Debug(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.LoadsConstant(LastMaxTreeLimit)) yield return new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit);
                else yield return instruction;
            }
        }
#endif
        private static IEnumerable<CodeInstruction> ReplaceLDCI4_MaxTreeLimit(IEnumerable<CodeInstruction> instructions) {
            foreach (var instruction in instructions) {
                if (instruction.LoadsConstant(LastMaxTreeLimit))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(MaxTreeLimit))); //new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit);
                else
                    yield return instruction;
            }
        }

        // Patch WeatherManager::CalculateSelfHeight()
        // Affects Tree on Wind Effect, stops tree from slowing wind
#if DEBUG
        private static IEnumerable<CodeInstruction> CalculateSelfHeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase method)
#else
        private static IEnumerable<CodeInstruction> CalculateSelfHeightTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
#endif
        {
            var codes = new List<CodeInstruction>(instructions);

            int insertionIndex = -1;
            Label returnTreeManagerLabel = il.DefineLabel();
            LocalBuilder num2 = null, a = null; // local variables in WeatherManager::CalculateSelfHeight()

            // extract two important variables
            for (int i = 0; i < codes.Count - 1; i++) // -1 since we will be checking i + 1
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].ToString().Contains("TreeManager")) {
                    insertionIndex = i;
                    // rewind and find num2 and a
                    int k = i - 10; // should be within 10 instructions
                    for (int j = i; j > k; j--) {
                        if (codes[j].opcode == OpCodes.Callvirt) {
                            num2 = (LocalBuilder)codes[j - 2].operand;
                            a = (LocalBuilder)codes[j - 1].operand;
                            break;
                        }
                    }
                    codes[i].labels.Add(returnTreeManagerLabel);
                }
            }

            if (insertionIndex != -1) {
                var instructionsToInsert = new List<CodeInstruction>
                {
                    /*
                     * The following instructions injects the following snippet into WeatherManager::CalculateSelfHeight()
                    if (TreeAnarchyConfig.TreeEffectOnWind)   //My Additions to overide tree effects.
                    {
                        return (ushort)Mathf.Clamp(num1 + num2 >> 1, 0, 65535);
                    }
                    */
                    new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TreeEffectOnWind))),
                    new CodeInstruction(OpCodes.Brfalse_S, returnTreeManagerLabel),
                    new CodeInstruction(OpCodes.Ldloc_S, num2),
                    new CodeInstruction(OpCodes.Ldloc_S, a),
                    new CodeInstruction(OpCodes.Add),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Shr),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Ldc_I4, 65535),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "Clamp", new Type[] { typeof(int), typeof(int), typeof(int) })),
                    new CodeInstruction(OpCodes.Conv_U2),
                    new CodeInstruction(OpCodes.Ret)
                };

                codes.InsertRange(insertionIndex, instructionsToInsert);
            }
#if DEBUG
            PrintDebugIL(codes, method);
#endif
            return codes.AsEnumerable();
        }

#if DEBUG
        private static IEnumerable<CodeInstruction> CheckLimitsDebug(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase method)
        {
            CodeInstruction insertion = new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit - 5);
            foreach (var instruction in instructions)
            {
                if (instruction.Is(OpCodes.Ldc_I4, 250000)) yield return insertion;
                else if (instruction.Is(OpCodes.Ldc_I4, 262139)) yield return insertion;
                else yield return instruction;
            }
        }
#endif
#if DEBUG
        private static IEnumerable<CodeInstruction> CheckLimitsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase method)
#else
        private static IEnumerable<CodeInstruction> CheckLimitsTranspiler(IEnumerable<CodeInstruction> instructions)
#endif
        {
#if DEBUG
            var codes = CheckLimitsDebug(instructions, il, method);
            PrintDebugIL(codes.ToList(), method);
            return codes.AsEnumerable();
#else
            foreach (var instruction in instructions) {
                if (instruction.Is(OpCodes.Ldc_I4, 250000))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(CheckLowLimit))); //new CodeInstruction(OpCodes.Ldc_I4, CheckLowLimit);
                else if (instruction.Is(OpCodes.Ldc_I4, 262139))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAMod), nameof(CheckHighLimit))); // new CodeInstruction(OpCodes.Ldc_I4, CheckHighLimit);
                else
                    yield return instruction;
            }
#endif
        }

        /* For Forestry Lock */
        private static IEnumerable<CodeInstruction> NRMTreesModifiedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = ReplaceLDCI4_MaxTreeLimit(instructions).ToList();
            Label jump = il.DefineLabel();

            codes[0].labels.Add(jump);

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseLockForestry))),
                new CodeInstruction(OpCodes.Brfalse_S, jump),
                new CodeInstruction(OpCodes.Ret),
            };
            codes.InsertRange(0, snippet);

            return codes.AsEnumerable();
        }

        /* This patches the TreeManager::Data::Deserialize method */
        private static IEnumerable<CodeInstruction> DeserializeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            bool firstSig = false, secondSig = false;
            LocalBuilder maxLen = il.DeclareLocal(typeof(int)), num4;
            Label IsFixedHeight = il.DefineLabel();
            MethodInfo integratedDeserialize = AccessTools.Method(typeof(TASerializableDataExtension), nameof(TASerializableDataExtension.IntegratedDeserialize));

            var codes = instructions.ToList();
            codes.Insert(0, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeLimit), nameof(TreeLimit.EnsureCapacity))));
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_3 && !firstSig) {
                    firstSig = true;
                    var snippet = new CodeInstruction[] {

                        new CodeInstruction(OpCodes.Ldc_I4, DefaultTreeLimit),
                        new CodeInstruction(OpCodes.Stloc_S, maxLen),
                    };
                    codes.InsertRange(i + 1, snippet);
                }
                if (codes[i].opcode == OpCodes.Ldloc_3) {
                    if (codes[i - 1].operand != null) {
                        var local = codes[i - 1].operand as LocalBuilder;
                        if (local.LocalIndex != 19) {
                            codes[i] = new CodeInstruction(OpCodes.Ldloc_S, maxLen);
                        }
                    }
                }
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_nextGridTree))) && !secondSig) {
                    secondSig = true;
                    num4 = codes[i + 2].operand as LocalBuilder;
                    var snippet = new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Ldloc_1),
                        new CodeInstruction(OpCodes.Ldloc_S, num4),
                        new CodeInstruction(OpCodes.Ldelema, typeof(TreeInstance)),
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.FixedHeight))),
                        new CodeInstruction(OpCodes.Brtrue_S, IsFixedHeight),
                    };
                    codes.InsertRange(i + 1, snippet);
                    var snippet2 = new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Call, integratedDeserialize),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees))),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer))),
                        new CodeInstruction(OpCodes.Stloc_1),
                        new CodeInstruction(OpCodes.Ldloc_1),
                        new CodeInstruction(OpCodes.Ldlen),
                        new CodeInstruction(OpCodes.Conv_I4),
                        new CodeInstruction(OpCodes.Stloc_3)
                    };
                    codes.InsertRange(i - 7, snippet2);
                }
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))) && secondSig) {
                    codes[i + 1] = codes[i + 1].WithLabels(IsFixedHeight);
                }
            }

            return codes.AsEnumerable();
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
                Debug.Log("TreeAnarchy: Exception occured during valiidate tree prefab. This is harmless");
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
            int insertIndex = -1;
            bool firstSig = false;
            bool secondSig = false;
            Label isOldFormatExit = il.DefineLabel();
            var codes = instructions.ToList();

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeLimit), nameof(TreeLimit.OldAfterDeserializeHandler))),
                new CodeInstruction(OpCodes.Brtrue, isOldFormatExit)
            };

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Ldc_I4_1 && codes[i + 2].opcode == OpCodes.Br && !firstSig) {
                    insertIndex = i;
                    firstSig = true;
                }
                if (codes[i].opcode == OpCodes.Ldloc_0 && codes[i + 1].opcode == OpCodes.Ldloc_0 && !secondSig) {
                    codes[i].WithLabels(isOldFormatExit);
                    secondSig = true;
                    break;
                }
            }
            if (insertIndex > 0) {
                codes.InsertRange(insertIndex, snippet);
            }

            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> SerializeTranspiler(IEnumerable<CodeInstruction> instructions) {
            bool sigFound = false;
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Stloc_2 && !sigFound) {
                    int index = i - 3;
                    codes.RemoveRange(index, 3);
                    codes.Insert(index, new CodeInstruction(OpCodes.Ldc_I4, DefaultTreeLimit));
                    sigFound = true;
                }
            }

            return codes.AsEnumerable();
        }

        internal static void InjectResize() {
            Harmony harmony = TAPatcher.harmony;
            HarmonyMethod replaceLDCI4 = new HarmonyMethod(AccessTools.Method(typeof(TreeLimit), nameof(ReplaceLDCI4_MaxTreeLimit)));

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
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeLimit), nameof(NRMTreesModifiedTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CheckLimits)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeLimit), nameof(CheckLimitsTranspiler))));
        }

        static bool isTranspilerPatched = false;
        internal static void Enable(Harmony harmony) {
            try {
                if (!isTranspilerPatched) {
                    InjectResize();
                    harmony.Patch(AccessTools.Method(typeof(WeatherManager), @"CalculateSelfHeight"), transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeLimit), @"CalculateSelfHeightTranspiler")));
                    harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Deserialize)),
                        transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeLimit), nameof(TreeLimit.DeserializeTranspiler))));
                    harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Serialize)),
                        transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeLimit), nameof(TreeLimit.SerializeTranspiler))));
                    harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.AfterDeserialize)),
                        transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeLimit), nameof(TreeLimit.AfterDeserializeTranspiler))));
                    isTranspilerPatched = true;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        internal static void EnsureCapacity() {
            TreeManager manager = Singleton<TreeManager>.instance;
            if (manager.m_trees.m_buffer.Length != MaxTreeLimit) {
                manager.m_trees = new Array32<TreeInstance>((uint)MaxTreeLimit);
                manager.m_updatedTrees = new ulong[MaxTreeUpdateLimit];
                manager.m_trees.CreateItem(out uint _);
            }
        }
    }
}
