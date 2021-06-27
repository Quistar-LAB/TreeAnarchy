#define FULLVERBOSE
#define QUIETVERBOSE
//#define SILENT
#define FULLVERBOSE
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
using System.Runtime.CompilerServices;
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
#if DEBUG
        private static IEnumerable<CodeInstruction> ReplaceLDCI4_MaxTreeLimit(IEnumerable<CodeInstruction> instructions, MethodBase method)
#else
        private static IEnumerable<CodeInstruction> ReplaceLDCI4_MaxTreeLimit(IEnumerable<CodeInstruction> instructions)
#endif
        {
#if DEBUG
            var codes = ReplaceLDCI4_Debug(instructions);
            PrintDebugIL(codes.ToList(), method);
            return codes;
#else
            foreach (var instruction in instructions) {
                if (instruction.LoadsConstant(LastMaxTreeLimit))
                    yield return new CodeInstruction(OpCodes.Ldc_I4, MaxTreeLimit);
                else
                    yield return instruction;
            }
#endif
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
            int CheckLowLimit = MaxTreeLimit - 12144;
            int CheckHighLimit = MaxTreeLimit - 5;
            foreach (var instruction in instructions) {
                if (instruction.Is(OpCodes.Ldc_I4, LastCheckLimitLowVal))
                    yield return new CodeInstruction(OpCodes.Ldc_I4, CheckLowLimit);
                else if (instruction.Is(OpCodes.Ldc_I4, LastCheckLimitHighVal))
                    yield return new CodeInstruction(OpCodes.Ldc_I4, CheckHighLimit);
                else
                    yield return instruction;
            }
            LastCheckLimitLowVal = CheckLowLimit;
            LastCheckLimitHighVal = CheckHighLimit;
#endif
        }
        private static int LastCheckLimitLowVal = 250000;
        private static int LastCheckLimitHighVal = 262139;

        /* For Forestry Lock */
        private static IEnumerable<CodeInstruction> NRMTreesModifiedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = ReplaceLDCI4_MaxTreeLimit(instructions).ToList();
            Label jump = il.DefineLabel();

            codes[0].labels.Add(jump);

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.LockForestry))),
                new CodeInstruction(OpCodes.Brfalse_S, jump),
                new CodeInstruction(OpCodes.Ret),
            };
            codes.InsertRange(0, snippet);

            return codes.AsEnumerable();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TrySpreadFirePrefix() {
            return false; // Skip the original codes completely
        }

        internal static void InjectResize() {
            Harmony harmony = TAPatcher.m_harmony;
            MethodInfo replaceLDCI4 = AccessTools.Method(typeof(TreeLimit), nameof(ReplaceLDCI4_MaxTreeLimit));

            harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.SaveProps)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.ClearDecorations)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(CommonBuildingAI), @"HandleFireSpread"), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(DisasterHelpers), nameof(DisasterHelpers.DestroyTrees)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(FireCopterAI), @"FindBurningTree"), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(ForestFireAI), @"FindClosestTree"), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(NaturalResourceManager), nameof(NaturalResourceManager.TreesModified)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeTool), @"ApplyBrush"), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.AfterTerrainUpdate)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateAreaHeight)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.CalculateGroupData)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), @"EndRenderingImpl"), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), @"HandleFireSpread"), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.OverlapQuad)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.PopulateGroupData)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.RayCast)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.SampleSmoothHeight)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.TerrainUpdated)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateData)), transpiler: new HarmonyMethod(replaceLDCI4));
            harmony.Patch(AccessTools.Method(typeof(TreeManager), nameof(TreeManager.UpdateTrees)), transpiler: new HarmonyMethod(replaceLDCI4));
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
                    isTranspilerPatched = true;
                }
                //m_Harmony.Patch(AccessTools.Method(typeof(TreeManager), "TrySpreadFire"), prefix: new HarmonyMethod(AccessTools.Method(typeof(Patcher), nameof(Patcher.TrySpreadFirePrefix))));
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        internal static void EnsureCapacity() {
            global::TreeManager manager = Singleton<global::TreeManager>.instance;
            TreeInstance[] oldbuf = manager.m_trees.m_buffer;

            if (manager.m_trees.m_buffer.Length != MaxTreeLimit) {

                Array32<TreeInstance> newBuffer = new Array32<TreeInstance>((uint)MaxTreeLimit);
                newBuffer.CreateItem(out uint _);
                uint itemCount = manager.m_trees.ItemCount();
                if (itemCount > 1) {
                    for (uint i = 1; i < itemCount; i++) {
                        if (newBuffer.CreateItem(out uint index)) {
                            newBuffer.m_buffer[index].m_flags = oldbuf[i].m_flags;
                            newBuffer.m_buffer[index].m_infoIndex = oldbuf[i].m_infoIndex;
                            newBuffer.m_buffer[index].m_nextGridTree = oldbuf[i].m_nextGridTree;
                            newBuffer.m_buffer[index].m_posX = oldbuf[i].m_posX;
                            newBuffer.m_buffer[index].m_posY = oldbuf[i].m_posY;
                            newBuffer.m_buffer[index].m_posZ = oldbuf[i].m_posZ;
                        }
                        manager.m_trees.ReleaseItem(i);
                    }
                }
                manager.m_trees = newBuffer;
                Array.Resize<ulong>(ref manager.m_updatedTrees, MaxTreeUpdateLimit);
            }
        }
    }
}
