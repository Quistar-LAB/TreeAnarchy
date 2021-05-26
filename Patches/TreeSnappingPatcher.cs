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
using HarmonyLib;
using ColossalFramework;
using UnityEngine;
using MoveIt;
using static TreeAnarchy.Patches.Patcher;
using static TreeAnarchy.TAConfig;

namespace TreeAnarchy.Patches
{
    internal class TreeSnappingPatcher
    {
        internal static void EnablePatches(Harmony harmony)
        {
            //harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.AfterTerrainUpdated)),
            //    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.AfterTerrainUpdatedTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.Transform)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeSnappingPatcher), nameof(TreeSnappingPatcher.TransformPrefix))));
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

        private static bool TransformPrefix(MoveableTree __instance, InstanceState state, ref Matrix4x4 matrix4x, float deltaHeight, Vector3 center, bool followTerrain)
        {
            /* I wanted to prevent trees from going underground, but the way moveit was written, and the way
             * deltaMove is used, there was no way to prevent deltaMove from increasing or decreasing when trees
             * go below the terrain.
            /* Basically the same as original Moveit::Transform, but the original never really had to consider
             * the possibility of trees going underground. With tree snapping enabled, this possibility has to
             * be considered. But for now...... just enabling tree.fixedheight.
             * Perhaps Moveit developers would be willing to change the way deltaMove is used to prevent things from
             * going below terrain... afterall... who needs their props below the terrain? just my thought
             */
            Vector3 newPosition = matrix4x.MultiplyPoint(state.position - center);
            float terrainHeight = TerrainManager.instance.SampleOriginalRawHeightSmooth(newPosition);

            newPosition.y = state.position.y + deltaHeight;
            if(newPosition.y > terrainHeight && UseTreeSnapping)
            {
                if (followTerrain)
                {
                    newPosition.y = newPosition.y + terrainHeight - state.position.y;
                }
                Singleton<TreeManager>.instance.m_trees.m_buffer[__instance.id.Tree].FixedHeight = true;
            }
            __instance.Move(newPosition, 0);
            return false;
        }
    }
}
