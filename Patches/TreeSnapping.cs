#define FULLVERBOSE
#define QUIETVERBOSE
//#define SILENT
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
using ColossalFramework;
using HarmonyLib;
using UnityEngine;
using MoveIt;
using static TreeAnarchy.TAConfig;

namespace TreeAnarchy.Patches {
    internal class TreeSnapping {
        internal void Enable(Harmony harmony) {
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.AfterTerrainUpdated)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.AfterTerrainUpdatedTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.CalculateTreeTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.SimulationStep)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.SimulationStepTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.TransformPrefix))));
        }

        internal void Disable(Harmony harmony, string id) {
            harmony.Unpatch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)), HarmonyPatchType.Prefix, id);
        }

        private static IEnumerable<CodeInstruction> AfterTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label ExitLabel = il.DefineLabel();
            Label FirstLabel = il.DefineLabel();
            Label SecondLabel = il.DefineLabel();
            Label ThirdLabel = il.DefineLabel();

            var codes = new List<CodeInstruction>(instructions);
            // search for first occurance of growstate
            for(int i = 0; i < codes.Count; i++) {
                if(codes[i].Calls(AccessTools.DeclaredPropertyGetter(typeof(TreeInstance), nameof(TreeInstance.GrowState)))) {
                    i += 2; // increment by two instructions
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(TAConfig.UseTreeSnapping))));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Brfalse_S, ThirdLabel));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_1));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Bgt_S, FirstLabel));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_1));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Br_S, SecondLabel));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0).WithLabels(new Label[] { FirstLabel }));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))).WithLabels(new Label[] { SecondLabel }));
                    codes.Insert(i++, new CodeInstruction(OpCodes.Br_S, ExitLabel));
                    codes[i] = codes[i].WithLabels(ThirdLabel);
                    codes[i + 3] = codes[i + 3].WithLabels(ExitLabel);
                    break; // found first occurance, so insert codes and break out
                }
            }

            return codes.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> CalculateTreeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase method) {
            int FirstIndex = 0, LastIndex = 0;
            Label UseTerrainHeight = il.DefineLabel(), ExitBranch = il.DefineLabel(), FirstJump = il.DefineLabel();
            LocalBuilder terrainHeight = il.DeclareLocal(typeof(float));
            CodeInstruction stlocTerrainHeight = default;
            CodeInstruction ldlocTerrainHeight = default;
            
            var codes = new List<CodeInstruction>(instructions);

            switch(terrainHeight.LocalIndex) {
                case 1: 
                stlocTerrainHeight = new CodeInstruction(OpCodes.Stloc_1);
                ldlocTerrainHeight = new CodeInstruction(OpCodes.Ldloc_1);
                break;
                case 2:
                stlocTerrainHeight = new CodeInstruction(OpCodes.Stloc_2);
                ldlocTerrainHeight = new CodeInstruction(OpCodes.Ldloc_2);
                break;
                case 3:
                stlocTerrainHeight = new CodeInstruction(OpCodes.Stloc_3);
                ldlocTerrainHeight = new CodeInstruction(OpCodes.Ldloc_3);
                break;
            }

            for (int i = 0; i < codes.Count; i++) {
                if(codes[i].opcode == OpCodes.Beq) {
                    FirstIndex = i;
                }
                if(codes[i].StoresField(AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY)))) {
                    LastIndex = i + 2;
                }
            }

           var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Beq_S, FirstJump),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(FirstJump),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))),
                new CodeInstruction(OpCodes.Ldc_I4_S, 32),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Brtrue_S, ExitBranch),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeInstance), "get_Position")),
                new CodeInstruction(OpCodes.Stloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ColossalFramework.Singleton<TerrainManager>), "get_instance")),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TerrainManager), nameof(TerrainManager.SampleDetailHeight), new Type[] { typeof(Vector3) })),
                stlocTerrainHeight,
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(UseTreeSnapping))),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                ldlocTerrainHeight,
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Brfalse_S, UseTerrainHeight),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))),
                new CodeInstruction(OpCodes.Ldc_I4_S, 32),
                new CodeInstruction(OpCodes.Or),
                new CodeInstruction(OpCodes.Conv_U2),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                new CodeInstruction(OpCodes.Ldc_R4, 64f),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt), new Type[] { typeof(float) })),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4, 65535),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) })),
                new CodeInstruction(OpCodes.Conv_U2),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))),
                new CodeInstruction(OpCodes.Br_S, ExitBranch),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(UseTerrainHeight),
                ldlocTerrainHeight,
                new CodeInstruction(OpCodes.Ldc_R4, 64f),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt), new Type[] { typeof(float) })),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4, 65535),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) })),
                new CodeInstruction(OpCodes.Conv_U2),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(ExitBranch)
            };

            codes.RemoveRange(FirstIndex, LastIndex - FirstIndex);
            codes.InsertRange(FirstIndex, snippet);

            return codes.AsEnumerable();
        }

        /* Inefficient, but more readable, performance here is insignificant */
        private static List<CodeInstruction> InsertCodeSnippet1(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            int occuranceCount = 0;
            int insertIndex = 0;
            LocalBuilder RaycastInput = null;
            Label jumpLabel = il.DefineLabel();
            ConstructorInfo RaycastService = AccessTools.DeclaredConstructor(typeof(ToolBase.RaycastService),
                                             new Type[] { typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Layer) });

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            /* First make sure to catch the RaycastInput variable */
            for(int i = 0; i < codes.Count; i++) {
                if(codes[i].operand is LocalBuilder input && input.LocalType == typeof(ToolBase.RaycastInput)) {
                    RaycastInput = input;
                    break;
                }
            }

            for(int i = 0; i < codes.Count; i++) {
                if(codes[i].LoadsField(AccessTools.Field(typeof(ToolController), nameof(ToolController.m_mode)))) {
                    occuranceCount++;
                }
                if(codes[i].opcode == OpCodes.Brfalse && occuranceCount == 2) {
                    insertIndex = i;
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
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput).WithLabels( new Label[] { jumpLabel }),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_currentEditObject))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreBuildingFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreNodeFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignoreSegmentFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_ignorePropFlags))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, RaycastService),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_propService))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, RaycastService),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_buildingService))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, RaycastService),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_netService))),
                new CodeInstruction(OpCodes.Ldloca_S, RaycastInput),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Newobj, RaycastService),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(ToolBase.RaycastInput), nameof(ToolBase.RaycastInput.m_netService2))),
            };

            codes.RemoveRange(insertIndex + 1, 3);
            codes.InsertRange(insertIndex + 1, insertCodes);
            codes.Insert(insertIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(TAConfig.UseTreeSnapping))));
            codes.Insert(insertIndex, new CodeInstruction(OpCodes.Brtrue_S, jumpLabel));

            return codes;
        }

        private static List<CodeInstruction> InsertCodeSnippet2(List<CodeInstruction> codes) {
            for(int i = 0; i < codes.Count; i++) {
                // First Insert
                if(codes[i].LoadsField(AccessTools.Field(typeof(TreeTool.RaycastOutput), nameof(TreeTool.RaycastOutput.m_currentEditObject)))) {
                    if(codes[i + 1].opcode == OpCodes.Brtrue && codes[i + 1].Branches(out Label? label)) {
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(TAConfig.UseTreeSnapping))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Brtrue, label.Value));
                    }
                }
                // Second Insert
                if(codes[i].LoadsField(AccessTools.Field(typeof(TreeTool.RaycastOutput), nameof(TreeTool.RaycastOutput.m_currentEditObject)))) {
                    if(codes[i - 2].opcode == OpCodes.Ldfld) {
                        codes.RemoveRange(i - 1, 2);
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ceq));
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldc_I4_0));
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(TAConfig.UseTreeSnapping))));
                        break; // can break out now
                    }
                }
            }
            return codes;
        }

        private static List<CodeInstruction> InsertCodeSnippet3(List<CodeInstruction> codes, ILGenerator il) {
            Label exitLabel = il.DefineLabel();
            List<Label> tempLabels = null;
            CodeInstruction leaveOp = null;

            for(int i = 0; i < codes.Count; i++) {
                if(codes[i].StoresField(AccessTools.Field(typeof(TreeTool), "m_fixedHeight"))) {
                    if(codes[i - 1].opcode == OpCodes.Ldfld) {
                        if(codes[i + 1].Branches(out Label? label)) {
                            exitLabel = label.Value;
                            tempLabels = codes[i + 1].labels;
                        }
                    }
                }
                if(codes[i].opcode == OpCodes.Leave) {
                    leaveOp = new CodeInstruction(codes[i].opcode, codes[i].operand); // catch and store opcode
                }
            }
            CodeInstruction[] InsertCode = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(UseTreeSnapping))),
                new CodeInstruction(OpCodes.Brfalse_S, exitLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(UseTreeSnapping))),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeTool), "m_fixedHeight")),
                leaveOp.WithLabels(tempLabels)
            };
            for(int i = 0; i < codes.Count; i++) {
                if(codes[i].StoresField(AccessTools.Field(typeof(TreeTool), "m_placementErrors"))) {
                    if(codes[i + 1].opcode == OpCodes.Ldarg_0 && codes[i - 1].opcode == OpCodes.Ldloc_S) {
                        codes.RemoveRange(i + 1, 5);
                        codes.InsertRange(i + 1, InsertCode);
                    }
                }
            }
            return codes;
        }

        private static IEnumerable<CodeInstruction> SimulationStepTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = InsertCodeSnippet1(instructions, il);
            codes = InsertCodeSnippet2(codes);
            codes = InsertCodeSnippet3(codes, il);

            return codes.AsEnumerable();
        }

        private static bool TransformPrefix(MoveableTree __instance, float deltaHeight) {
            if(!global::TreeManager.instance.m_trees.m_buffer[__instance.id.Tree].FixedHeight && deltaHeight != 0) {
                global::TreeManager.instance.m_trees.m_buffer[__instance.id.Tree].FixedHeight = true;
            }
            return true;
        }
    }
}
