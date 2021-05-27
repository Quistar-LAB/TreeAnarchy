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
using ColossalFramework.Math;
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
//            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.AfterTerrainUpdated)),
//                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.AfterTerrainUpdatedPrefix))));
//            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.AfterTerrainUpdated)),
//                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.AfterTerrainUpdatedTranspiler))));
//            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
//                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.CalculateTreePrefix))));
            harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.SimulationStep)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.SimulationStepTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.TransformPrefix))));
        }

        internal static void DisablePatches(Harmony harmony, string id)
        {
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)), HarmonyPatchType.Prefix, id);
        }

        // TreeInstance
        internal static bool AfterTerrainUpdatedPrefix(TreeInstance __instance, uint treeID, float minX, float minZ, float maxX, float maxZ)
        {
            if ((__instance.m_flags & 3) != 1)
            {
                return false;
            }
            if ((__instance.m_flags & 32) == 0)
            {
                Vector3 position = __instance.Position;
                position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
                ushort num = (ushort)Mathf.Clamp(Mathf.RoundToInt(position.y * 64f), 0, 65535);
                if (num != __instance.m_posY)
                {
                    int growState = __instance.GrowState;
                    if(UseTreeSnapping)
                    {
                        if (__instance.m_posY < num) __instance.m_posY = num;
                    }
                    else
                    {
                        __instance.m_posY = num;
                    }
                    //__instance.CheckOverlap(treeID);
                    int growState2 = __instance.GrowState;
                    if (growState2 != growState)
                    {
                        Singleton<TreeManager>.instance.UpdateTree(treeID);
                    }
                    else if (growState2 != 0)
                    {
                        Singleton<TreeManager>.instance.UpdateTreeRenderer(treeID, true);
                    }
                }
            }
            return false;
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


        private static IEnumerable<CodeInstruction> SimulationStepTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            LocalBuilder input = null;
            ConstructorInfo constructor = typeof(ToolBase.RaycastService).
                GetConstructor(BindingFlags.Instance | BindingFlags.Public, null,
                new Type[] { typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Layer) }, null);

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for(int i = 0; i < codes.Count; i++)
            {
                if (codes[i].operand is LocalBuilder local && local.LocalType == typeof(ToolBase.RaycastInput))
                {
                    input = local;
                    break;
                }
            }
        /* The following IL represents the following c# codes
         * input.m_currentEditObject = true;
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
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_currentEditObject))),
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
            Label label5 = new Label();
            
            for(int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(AccessTools.Method(typeof(Singleton<ToolManager>), "get_instance")) && !firstSigFound)
                {
                    if(skippedFirst == true)
                    {
                        firstIndex = i;
                        if(codes[i - 1].Branches(out Label? label))
                        {
                            label5 = (Label)label;
                        }
                        firstSigFound = true;
                    }
                    skippedFirst = true;
                }
                if (firstSigFound
                    && codes[i].opcode == OpCodes.Callvirt
                    && codes[i].operand == AccessTools.Method(typeof(ToolController), nameof(ToolController.BeginColliding)))
                {
                    secondIndex = i + 1;

                }
            }
            Debug.Log($"TreeAnarchy: First Index at {firstIndex} Found second index at {secondIndex}");
            codes.RemoveRange(firstIndex, secondIndex - firstIndex);
            codes.InsertRange(firstIndex, insertCodes);
            codes[firstIndex + insertCodes.Length] = codes[firstIndex + insertCodes.Length].WithLabels(new Label[] { label5 });

            Debug.Log("TreeAnarchy: ------------- SimulationStep --------------------");
            foreach (var code in codes)
            {
                Debug.Log($"==> {code}");
            }
            Debug.Log("TreeAnarchy: -------------------------------------------------");
            return codes.AsEnumerable();
        }

        private static bool TransformPrefix(MoveableTree __instance, InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain)
        {
            if (!TreeManager.instance.m_trees.m_buffer[__instance.id.Tree].FixedHeight && deltaHeight != 0)
            {
                TreeManager.instance.m_trees.m_buffer[__instance.id.Tree].FixedHeight = true;
            }
            return true;
        }
    }
}
