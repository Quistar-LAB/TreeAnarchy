#define FULLVERBOSE
#define QUIETVERBOSE
#define SILENT
#undef FULLVERBOSE
#if SILENT
#undef DEBUG
#undef FULLVERBOSE
#undef QUIETVERBOSE
#endif

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ColossalFramework;
using UnityEngine;
using MoveIt;
#if DEBUG
using static TreeAnarchy.Patches.Patcher;
#endif
using static TreeAnarchy.TAConfig;

namespace TreeAnarchy.Patches
{
    internal class TreeSnappingPatcher
    {
        internal static void EnablePatches(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.AfterTerrainUpdated)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.AfterTerrainUpdatedTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.CalculateTreePrefix))));
            harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.SimulationStep)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.SimulationStepTranspiler))));

#if FALSE
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.TransformPrefix))));
#endif
        }

        internal static void DisablePatches(Harmony harmony, string id)
        {
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)), HarmonyPatchType.Prefix, id);
        }

#if DEBUG
        private static IEnumerable<CodeInstruction> AfterTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase method)
#else
        private static IEnumerable<CodeInstruction> AfterTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
#endif
        {
            var codes = new List<CodeInstruction>(instructions);
            Label customExitLabel = il.DefineLabel();
            Label customFirstLabel = il.DefineLabel();
            Label customSecondLabel = il.DefineLabel();
            Label customThirdLabel = il.DefineLabel();
            // search for first occurance of growstate
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call)
                {
                    if (codes[i].ToString().Contains("get_GrowState"))
                    {
                        i += 2; // increment by two instructions
                        codes.Insert(i++, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToolsModifierControl), "GetCurrentTool", null, new Type[] { typeof(TerrainTool) })));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldnull));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality", new Type[] { typeof(UnityEngine.Object), typeof(UnityEngine.Object) })));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(TAConfig.UseTreeSnapping))));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Or));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Brfalse, customFirstLabel));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_1));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Bgt_S, customSecondLabel));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_1));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Br_S, customThirdLabel));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0).WithLabels(customSecondLabel));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))).WithLabels(customThirdLabel));
                        codes.Insert(i++, new CodeInstruction(OpCodes.Br_S, customExitLabel));
                        codes[i].labels.Add(customFirstLabel); i += 3;
                        codes[i].labels.Add(customExitLabel);
                        break; // found first occurance, so insert codes and break out
                    }
                }
            }
#if DEBUG
            PrintDebugIL(codes, method);
#endif
            return codes.AsEnumerable();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CalculateTreePrefix()
        {
            return false;
        }

        private static IEnumerable<CodeInstruction> SimulationStepTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase method)
        {
            LocalBuilder input = null;
            ConstructorInfo constructor = typeof(ToolBase.RaycastService).
                GetConstructor(BindingFlags.Instance | BindingFlags.Public, null,
                new Type[] { typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Layer) }, null);

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for(int i = 0; i < codes.Count; i++)
            {
                LocalBuilder local = codes[i].operand as LocalBuilder;
                if (local != null && local.LocalType == typeof(ToolBase.RaycastInput))
                {
                    input = local;
                    break;
                }
            }
            /* The following IL represents the following c# codes
             * input.m_ignoreBuildingFlags = Building.Flags.None;
             * input.m_ignoreNodeFlags = NetNode.Flags.None;
             * input.m_ignoreSegmentFlags = NetSegment.Flags.None;
             * input.m_buildingService = new RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
             * input.m_netService = new RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
             * input.m_netService2 = new RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
             */
            CodeInstruction[] insertCodes = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloca_S, input),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreBuildingFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, input),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreNodeFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, input),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreSegmentFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, input),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, constructor),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_buildingService))),
                new CodeInstruction(OpCodes.Ldloca_S, input),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, constructor),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_netService))),
                new CodeInstruction(OpCodes.Ldloca_S, input),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, constructor),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_netService2))),
            };

            int firstIndex = 0;
            int secondIndex = 0;
            bool firstSigFound = false;
            bool skippedFirst = false;
            
            for(int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(AccessTools.Method(typeof(Singleton<ToolManager>), "get_instance")) && !firstSigFound)
                {
                    if(skippedFirst == true)
                    {
                        firstIndex = i;
                        firstSigFound = true;
                    }
                    skippedFirst = true;
                }
                if(codes[i].StoresField(AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_currentEditObject))))
                {
                    secondIndex = i + 1;
                }
                if(codes[i].StoresField(AccessTools.Field(typeof(TreeTool), "m_fixedHeight")))
                {
                    codes.RemoveRange(i - 3, 4);
                }
            }
            codes.RemoveRange(firstIndex, secondIndex - firstIndex);
            codes.InsertRange(firstIndex, insertCodes);

            Debug.Log("TreeAnarchy: ------------- SimulationStep --------------------");
            foreach (var code in codes)
            {
                Debug.Log($"==> {code}");
            }
            Debug.Log("TreeAnarchy: -------------------------------------------------");
            return codes.AsEnumerable();
        }
    }
}
