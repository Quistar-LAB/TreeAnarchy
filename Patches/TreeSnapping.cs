#define FULLVERBOSE
#define QUIETVERBOSE
//#define SILENT
#undef FULLVERBOSE
#if SILENT
#undef DEBUG
#undef FULLVERBOSE
#undef QUIETVERBOSE
#endif
using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using MoveIt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy.Patches {
    internal static class TreeSnapping {
        private static bool isTranspilerPatched = false;
        private static bool isMoveItPatched = false;
        internal static void Enable(Harmony harmony) {
            if (!isTranspilerPatched) {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.CalculateTreeTranspiler))));
                //                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
                //                    prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.CalculateTreePrefix))));
                harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.SimulationStep)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.SimulationStepTranspiler))));
                isTranspilerPatched = true;
            }
            //            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneGeometry)),
            //                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.RenderCloneGeometryPrefix))));
            //            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneOverlay)),
            //                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.RenderCloneOverlayPrefix))));
            //            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Clone),
            //                new Type[] { typeof(InstanceState), typeof(Matrix4x4).MakeByRefType(), typeof(float), typeof(float), typeof(Vector3), typeof(bool), typeof(Dictionary<ushort, ushort>), typeof(MoveIt.Action) }),
            //                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.ClonePrefix))));
        }

        internal static void PatchMoveIt(Harmony harmony) {
            if (!isMoveItPatched) {
                //                harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)),
                //                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.MoveableTreeTransformTranspiler))));
                harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnapping), nameof(TreeSnapping.MoveableTreeTransformPrefix))));
                isMoveItPatched = true;
            }
        }

        private static IEnumerable<CodeInstruction> CalculateTreeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            int FirstIndex = 0, LastIndex = 0;
            bool firstSig = false;
            bool secondSig = false;
            Label useTerrainHeight = il.DefineLabel();
            Label hasFixedHeight = il.DefineLabel();
            Label exitLabel = default;
            LocalBuilder terrainHeight = il.DeclareLocal(typeof(float));
            CodeInstruction stlocTerrainHeight = default;
            CodeInstruction ldlocTerrainHeight = default;

            var codes = new List<CodeInstruction>(instructions);

            switch (terrainHeight.LocalIndex) {
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
                if (codes[i].opcode == OpCodes.Ret && !firstSig) {
                    firstSig = true;
                    var snippet = new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.Position))),
                        new CodeInstruction(OpCodes.Stloc_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ColossalFramework.Singleton<TerrainManager>), "get_instance")),
                        new CodeInstruction(OpCodes.Ldloc_0),
                        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TerrainManager), nameof(TerrainManager.SampleDetailHeight), new Type[] { typeof(Vector3) })),
                        stlocTerrainHeight,
                        new CodeInstruction(OpCodes.Ldarg_0)
                    };
                    codes.InsertRange(i + 2, snippet);
                }
            }

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].opcode == OpCodes.Brtrue && !secondSig) {
                    secondSig = true;
                    codes[i] = new CodeInstruction(OpCodes.Brtrue, hasFixedHeight);
                    FirstIndex = i + 1;
                } else if (codes[i].StoresField(AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY)))) {
                    LastIndex = i + 1;
                    exitLabel = codes[i + 1].labels[0];
                    break;
                }
            }

            var snippet1 = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))),
                new CodeInstruction(OpCodes.Brfalse_S, useTerrainHeight),

                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                ldlocTerrainHeight,
                new CodeInstruction(OpCodes.Ldc_R4, 0.075f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Ble_Un_S, useTerrainHeight),

                new CodeInstruction(OpCodes.Ldarg_0), /* Use TreeInstance position.y */
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
                new CodeInstruction(OpCodes.Br_S, exitLabel),

                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(useTerrainHeight),
                ldlocTerrainHeight,
                new CodeInstruction(OpCodes.Ldc_R4, 64f),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt), new Type[] { typeof(float) })),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4, 65535),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) })),
                new CodeInstruction(OpCodes.Conv_U2),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))),
                new CodeInstruction(OpCodes.Br_S, exitLabel),

                new CodeInstruction(OpCodes.Ldloc_0).WithLabels(hasFixedHeight),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                ldlocTerrainHeight,
                new CodeInstruction(OpCodes.Bge_Un_S, exitLabel),

                new CodeInstruction(OpCodes.Ldarg_0),
                ldlocTerrainHeight,
                new CodeInstruction(OpCodes.Ldc_R4, 64f),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt), new Type[] { typeof(float) })),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4, 65535),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) })),
                new CodeInstruction(OpCodes.Conv_U2),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_posY))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.FixedHeight)))
            };

            codes.RemoveRange(FirstIndex, LastIndex - FirstIndex);
            codes.InsertRange(FirstIndex, snippet1);

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
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].operand is LocalBuilder input && input.LocalType == typeof(ToolBase.RaycastInput)) {
                    RaycastInput = input;
                    break;
                }
            }

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].LoadsField(AccessTools.Field(typeof(ToolController), nameof(ToolController.m_mode)))) {
                    occuranceCount++;
                }
                if (codes[i].opcode == OpCodes.Brfalse && occuranceCount == 2) {
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
            codes.Insert(insertIndex, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))));
            codes.Insert(insertIndex, new CodeInstruction(OpCodes.Brtrue_S, jumpLabel));

            return codes;
        }

        private static List<CodeInstruction> InsertCodeSnippet2(List<CodeInstruction> codes) {
            for (int i = 0; i < codes.Count; i++) {
                // First Insert
                if (codes[i].LoadsField(AccessTools.Field(typeof(TreeTool.RaycastOutput), nameof(TreeTool.RaycastOutput.m_currentEditObject)))) {
                    if (codes[i + 1].opcode == OpCodes.Brtrue && codes[i + 1].Branches(out Label? label)) {
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Brtrue, label.Value));
                    }
                }
                // Second Insert
                if (codes[i].LoadsField(AccessTools.Field(typeof(TreeTool.RaycastOutput), nameof(TreeTool.RaycastOutput.m_currentEditObject)))) {
                    if (codes[i - 2].opcode == OpCodes.Ldfld) {
                        codes.RemoveRange(i - 1, 2);
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ceq));
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldc_I4_0));
                        codes.Insert(i - 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeSnapping))));
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

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeTool), "m_fixedHeight"))) {
                    if (codes[i - 1].opcode == OpCodes.Ldfld) {
                        if (codes[i + 1].Branches(out Label? label)) {
                            exitLabel = label.Value;
                            tempLabels = codes[i + 1].labels;
                        }
                    }
                }
                if (codes[i].opcode == OpCodes.Leave) {
                    leaveOp = new CodeInstruction(codes[i].opcode, codes[i].operand); // catch and store opcode
                }
            }
            CodeInstruction[] InsertCode = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseTreeSnapping))),
                new CodeInstruction(OpCodes.Brfalse_S, exitLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseTreeSnapping))),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(TreeTool), "m_fixedHeight")),
                leaveOp.WithLabels(tempLabels)
            };
            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].StoresField(AccessTools.Field(typeof(TreeTool), "m_placementErrors"))) {
                    if (codes[i + 1].opcode == OpCodes.Ldarg_0 && codes[i - 1].opcode == OpCodes.Ldloc_S) {
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


        private static bool MoveableTreeTransformPrefix(MoveableTree __instance, InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain) {
            if (deltaHeight != 0) Singleton<TreeManager>.instance.m_trees.m_buffer[__instance.id.Tree].FixedHeight = true;
            return true;
        }

        private static IEnumerable<CodeInstruction> MoveableTreeTransformTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            const int followTerrainParIndex = 6;
            int firstIndex = 0, lastIndex = 0;
            Label hasTreeSnapEnabled = il.DefineLabel();
            Label Exit1 = il.DefineLabel();
            Label deltaHeightIsGreater = il.DefineLabel();
            Label isFollowTerrain = il.DefineLabel();
            Label isFollowTerrain2 = il.DefineLabel();
            Label isFixedHeight = il.DefineLabel();
            Label Exit = default;
            LocalBuilder oldTerrainHeight = il.DeclareLocal(typeof(float));
            LocalBuilder newTerrainHeight = il.DeclareLocal(typeof(float));
            LocalBuilder trees = il.DeclareLocal(typeof(TreeInstance[]));
            LocalBuilder treeID = il.DeclareLocal(typeof(uint));
            LocalBuilder instanceID = il.DeclareLocal(typeof(InstanceID));
            LocalBuilder vector = default;

            CodeInstruction stloc_oldTerrainHeight = default;
            CodeInstruction ldloc_oldTerrainHeight = default;
            CodeInstruction stloc_newTerrainHeight = default;
            CodeInstruction ldloc_newTerrainHeight = default;
            CodeInstruction stloc_trees = default;
            CodeInstruction ldloc_trees = default;
            CodeInstruction stloc_treeID = default;
            CodeInstruction ldloc_treeID = default;

            CodeInstruction[] stlocs = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Stloc_0),
                new CodeInstruction(OpCodes.Stloc_1),
                new CodeInstruction(OpCodes.Stloc_2),
                new CodeInstruction(OpCodes.Stloc_3),
            };
            CodeInstruction[] ldlocs = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldloc_3),
            };


            for (int i = 0; i < 4; i++) {
                if (oldTerrainHeight.LocalIndex == i) {
                    stloc_oldTerrainHeight = stlocs[i];
                    ldloc_oldTerrainHeight = ldlocs[i];
                }
                if (newTerrainHeight.LocalIndex == i) {
                    stloc_newTerrainHeight = stlocs[i];
                    ldloc_newTerrainHeight = ldlocs[i];
                }
                if (trees.LocalIndex == i) {
                    stloc_trees = stlocs[i];
                    ldloc_trees = ldlocs[i];
                }
            }
            if (newTerrainHeight.LocalIndex > 3) {
                stloc_newTerrainHeight = new CodeInstruction(OpCodes.Stloc_S, newTerrainHeight);
                ldloc_newTerrainHeight = new CodeInstruction(OpCodes.Ldloc_S, newTerrainHeight);
            }
            if (trees.LocalIndex > 3) {
                stloc_trees = new CodeInstruction(OpCodes.Stloc_S, trees);
                ldloc_trees = new CodeInstruction(OpCodes.Ldloc_S, trees);
            }
            stloc_treeID = new CodeInstruction(OpCodes.Stloc_S, trees);
            ldloc_treeID = new CodeInstruction(OpCodes.Ldloc_S, trees);

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++) {
                if (codes[i].StoresField(AccessTools.Field(typeof(Vector3), nameof(Vector3.y))) && firstIndex == 0) {
                    firstIndex = i + 1;
                }
                if (codes[i].opcode == OpCodes.Ldloca_S) {
                    vector = codes[i].operand as LocalBuilder;
                }
                if (codes[i].opcode == OpCodes.Ldarg_0) {
                    lastIndex = i;
                    Exit = codes[i].labels[0];
                    break;
                }
            }

            var snippet = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Singleton<TerrainManager>), nameof(Singleton<TerrainManager>.instance))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Instance), nameof(Instance.position))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TerrainManager), nameof(TerrainManager.SampleDetailHeight), new Type[] { typeof(Vector3) })),
                stloc_oldTerrainHeight,
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Singleton<TerrainManager>), nameof(Singleton<TerrainManager>.instance))),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(TerrainManager), nameof(TerrainManager.SampleDetailHeight), new Type[] { typeof(Vector3) })),
                stloc_newTerrainHeight,
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Singleton<TreeManager>), nameof(Singleton<TreeManager>.instance))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeManager), nameof(TreeManager.m_trees))),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Array32<TreeInstance>), nameof(Array32<TreeInstance>.m_buffer))),
                stloc_trees,
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Instance), nameof(Instance.id))),
                new CodeInstruction(OpCodes.Stloc_S, instanceID),
                new CodeInstruction(OpCodes.Ldloca_S, instanceID),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(InstanceID), nameof(InstanceID.Tree))),
                new CodeInstruction(OpCodes.Stloc_S, treeID),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(UseTreeSnapping))),
                new CodeInstruction(OpCodes.Brtrue_S, hasTreeSnapEnabled),

                ldloc_trees,
                ldloc_treeID,
                new CodeInstruction(OpCodes.Ldelema, typeof(TreeInstance)),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.FixedHeight))),
                new CodeInstruction(OpCodes.Ldloca_S, 0),
                ldloc_newTerrainHeight,
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                new CodeInstruction(OpCodes.Br, Exit),

                ldloc_trees.WithLabels(hasTreeSnapEnabled),
                ldloc_treeID,
                new CodeInstruction(OpCodes.Ldelema, typeof(TreeInstance)),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.FixedHeight))),
                new CodeInstruction(OpCodes.Brfalse_S, isFixedHeight),

                new CodeInstruction(OpCodes.Ldarg_S, followTerrainParIndex),
                new CodeInstruction(OpCodes.Brfalse_S, isFollowTerrain),

                new CodeInstruction(OpCodes.Ldloca_S, 0),
                new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldind_R4),
                ldloc_newTerrainHeight,
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(InstanceState), nameof(InstanceState.terrainHeight))),
                new CodeInstruction(OpCodes.Sub),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stind_R4),

                new CodeInstruction(OpCodes.Ldloc_0).WithLabels(isFollowTerrain),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                ldloc_newTerrainHeight,
                new CodeInstruction(OpCodes.Ldc_R4, 0.075f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Bge_Un_S, Exit),

                new CodeInstruction(OpCodes.Ldloca_S, 0),
                ldloc_newTerrainHeight,
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                new CodeInstruction(OpCodes.Br_S, Exit),

                new CodeInstruction(OpCodes.Ldarg_3).WithLabels(isFixedHeight),
                new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
                new CodeInstruction(OpCodes.Ble_Un_S, deltaHeightIsGreater),

                ldloc_trees,
                ldloc_treeID,
                new CodeInstruction(OpCodes.Ldelema, typeof(TreeInstance)),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.FixedHeight))),
                new CodeInstruction(OpCodes.Ldarg_S, followTerrainParIndex).WithLabels(deltaHeightIsGreater),
                new CodeInstruction(OpCodes.Brfalse_S, isFollowTerrain2),

                new CodeInstruction(OpCodes.Ldloca_S, 0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldind_R4),
                ldloc_newTerrainHeight,
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(InstanceState), nameof(InstanceState.terrainHeight))),
                new CodeInstruction(OpCodes.Sub),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stind_R4),
                new CodeInstruction(OpCodes.Ldloc_0).WithLabels(isFollowTerrain2),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
                ldloc_newTerrainHeight,
                new CodeInstruction(OpCodes.Ldc_R4, 0.075f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Bge_Un_S, Exit),

                new CodeInstruction(OpCodes.Ldloca_S, 0),
                ldloc_newTerrainHeight,
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.y))),
            };

            codes.RemoveRange(firstIndex, lastIndex - firstIndex);
            codes.InsertRange(firstIndex, snippet);

            return codes;
        }

        private static bool RenderCloneGeometryPrefix(MoveableTree __instance, InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor) {
            TreeState treeState = instanceState as TreeState;
            TreeInfo treeInfo = treeState.Info.Prefab as TreeInfo;
            Randomizer randomizer = new Randomizer(treeState.instance.id.Tree);
            float scale = treeInfo.m_minScale + (float)randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
            float brightness = treeInfo.m_minBrightness + (float)randomizer.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
            Vector3 vector = matrix4x.MultiplyPoint(treeState.position - center);
            vector.y = treeState.position.y + deltaPosition.y;
            float terrainHeight = vector.y - treeState.terrainHeight + Singleton<TerrainManager>.instance.SampleOriginalRawHeightSmooth(vector);
            if (!UseTreeSnapping) {
                vector.y = terrainHeight;
            } else {
                vector = TreeSnapRayCast(vector, terrainHeight, treeState);
            }
            TreeInstance.RenderInstance(cameraInfo, treeInfo, vector, scale, brightness, RenderManager.DefaultColorLocation);
            return false;
        }

        private static bool RenderCloneOverlayPrefix(MoveableTree __instance, InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, float deltaAngle, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo, Color toolColor) {
            //            if (MoveItTool.m_isLowSensitivity) {
            //                return false;
            //            }
            TreeState treeState = instanceState as TreeState;
            TreeInfo treeInfo = treeState.Info.Prefab as TreeInfo;
            Randomizer randomizer = new Randomizer(treeState.instance.id.Tree);
            float scale = treeInfo.m_minScale + (float)randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
            Vector3 vector = matrix4x.MultiplyPoint(treeState.position - center);
            vector.y = treeState.position.y + deltaPosition.y;
            float terrainHeight = vector.y - treeState.terrainHeight + Singleton<TerrainManager>.instance.SampleOriginalRawHeightSmooth(vector);
            if (!UseTreeSnapping) {
                vector.y = terrainHeight;
            } else {
                vector = TreeSnapRayCast(vector, terrainHeight, treeState);
            }
            TreeTool.RenderOverlay(cameraInfo, treeInfo, vector, scale, toolColor);
            return false;
        }

        private static bool ClonePrefix(MoveableTree __instance, ref Instance __result, InstanceState instanceState, ref Matrix4x4 matrix4x, float deltaHeight, float deltaAngle, Vector3 center, bool followTerrain, Dictionary<ushort, ushort> clonedNodes, MoveIt.Action action) {
            TreeState treeState = instanceState as TreeState;
            Vector3 vector = matrix4x.MultiplyPoint(treeState.position - center);
            vector.y = treeState.position.y + deltaHeight;
            float terrainHeight = vector.y - treeState.terrainHeight + Singleton<TerrainManager>.instance.SampleOriginalRawHeightSmooth(vector);
            if (!UseTreeSnapping) {
                vector.y = terrainHeight;
            } else {
                vector = TreeSnapRayCast(vector, terrainHeight, treeState);
            }
            __result = null;
            if (Singleton<TreeManager>.instance.CreateTree(out uint tree, ref Singleton<SimulationManager>.instance.m_randomizer, treeState.Info.Prefab as TreeInfo, vector, treeState.single)) {
                __result = new MoveableTree(new InstanceID {
                    Tree = tree
                });
            }
            return false;
        }


        private static bool RayCast(ToolBase.RaycastInput input, out ToolBase.RaycastOutput output) {
            float tempRayLength;
            Vector3 origin = input.m_ray.origin;
            Vector3 normalized = input.m_ray.direction.normalized;
            Vector3 vector = input.m_ray.origin + normalized * input.m_length;
            Segment3 ray = new Segment3(origin, vector);
            output.m_hitPos = vector;
            output.m_overlayButtonIndex = 0;
            output.m_netNode = 0;
            output.m_netSegment = 0;
            output.m_building = 0;
            output.m_propInstance = 0;
            output.m_treeInstance = 0u;
            output.m_vehicle = 0;
            output.m_parkedVehicle = 0;
            output.m_citizenInstance = 0;
            output.m_transportLine = 0;
            output.m_transportStopIndex = 0;
            output.m_transportSegmentIndex = 0;
            output.m_district = 0;
            output.m_park = 0;
            output.m_disaster = 0;
            output.m_currentEditObject = false;
            bool result = false;
            float mouseRayLength = input.m_length;
            if (!input.m_ignoreTerrain && Singleton<TerrainManager>.instance.RayCast(ray, out Vector3 vector2)) {
                float rayLength = Vector3.Distance(vector2, origin) + 100f;
                if (rayLength < mouseRayLength) {
                    output.m_hitPos = vector2;
                    result = true;
                    mouseRayLength = rayLength;
                }
            }
            if ((input.m_ignoreNodeFlags != NetNode.Flags.All ||
                 input.m_ignoreSegmentFlags != NetSegment.Flags.All) && Singleton<NetManager>.instance.RayCast(input.m_buildObject as NetInfo, ray, input.m_netSnap, input.m_segmentNameOnly, input.m_netService.m_service, input.m_netService2.m_service, input.m_netService.m_subService, input.m_netService2.m_subService, input.m_netService.m_itemLayers, input.m_netService2.m_itemLayers, input.m_ignoreNodeFlags, input.m_ignoreSegmentFlags, out vector2, out output.m_netNode, out output.m_netSegment)) {
                tempRayLength = Vector3.Distance(vector2, origin);
                if (tempRayLength < mouseRayLength) {
                    output.m_hitPos = vector2;
                    result = true;
                    mouseRayLength = tempRayLength;
                } else {
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                }
            }
            if (input.m_ignoreBuildingFlags != Building.Flags.All && Singleton<BuildingManager>.instance.RayCast(ray, input.m_buildingService.m_service, input.m_buildingService.m_subService, input.m_buildingService.m_itemLayers, input.m_ignoreBuildingFlags, out vector2, out output.m_building)) {
                tempRayLength = Vector3.Distance(vector2, origin);
                if (tempRayLength < mouseRayLength) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    result = true;
                    mouseRayLength = tempRayLength;
                } else {
                    output.m_building = 0;
                }
            }
            if (input.m_currentEditObject && Singleton<ToolManager>.instance.m_properties.RaycastEditObject(ray, out vector2)) {
                tempRayLength = Vector3.Distance(vector2, origin);
                if (tempRayLength < mouseRayLength) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_currentEditObject = true;
                    result = true;
                    mouseRayLength = tempRayLength;
                }
            }
            if (input.m_ignorePropFlags != PropInstance.Flags.All && Singleton<PropManager>.instance.RayCast(ray, input.m_propService.m_service, input.m_propService.m_subService, input.m_propService.m_itemLayers, input.m_ignorePropFlags, out vector2, out output.m_propInstance)) {
                if (Vector3.Distance(vector2, origin) - 0.5f < mouseRayLength) {
                    output.m_hitPos = vector2;
                    output.m_netNode = 0;
                    output.m_netSegment = 0;
                    output.m_building = 0;
                    output.m_disaster = 0;
                    output.m_currentEditObject = false;
                    result = true;
                } else {
                    output.m_propInstance = 0;
                }
            }
            return result;
        }

        // These codes should really live in the MoveIt mod space and not here!!
        /* As you can see here, I tried to implement raycast for tree snapping to work with MoveIt mod,
         * but apparently MoveIt calculates position differently than CO Framework. If this was ever to 
         * be realized, I think it has to be done from within MoveIt mod.
         */
        private static Vector3 TreeSnapRayCast(Vector3 position, float terrainHeight, TreeState state) {
            Ray objRay;
            float objRayLength;

            objRay = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(position));
            objRayLength = Camera.main.farClipPlane;
            ToolBase.RaycastInput input = new ToolBase.RaycastInput(objRay, objRayLength) {
                m_currentEditObject = true,
                m_ignoreTerrain = false,
                m_ignoreBuildingFlags = Building.Flags.None,
                m_ignoreNodeFlags = NetNode.Flags.None,
                m_ignoreSegmentFlags = NetSegment.Flags.None,
                m_ignorePropFlags = PropInstance.Flags.None,
                m_buildingService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default),
                m_netService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default),
                m_netService2 = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default),
                m_propService = new ToolBase.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default)
            };
            if (RayCast(input, out ToolBase.RaycastOutput raycastOutput)) {
                return raycastOutput.m_hitPos;
            }
            position.y = position.y + terrainHeight - state.terrainHeight;
            return position;
        }

        private static Vector3 SnaptToTerrainHeight(Vector3 position, float terrainHeight, TreeState state) {
            position.y = position.y - state.terrainHeight + terrainHeight;
            return position;
        }

        internal static Vector3 RaycastMouseLocation() {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            ToolBase.RaycastInput input = new ToolBase.RaycastInput(mouseRay, Camera.main.farClipPlane) {
                m_ignoreTerrain = false
            };
            RayCast(input, out ToolBase.RaycastOutput output);
            return output.m_hitPos;
        }
    }
}
