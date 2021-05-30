using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using HarmonyLib;
using MoveIt;
using Random = UnityEngine.Random;
using static TreeAnarchy.TAConfig;

namespace TreeAnarchy.Patches
{
    internal class TreeMovementPatcher
    {
		internal static void EnablePatch(Harmony harmony)
        {
			harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.RenderGeometry)), 
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovementPatcher), nameof(RenderGeometryTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
				new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeMovementPatcher), nameof(RenderInstanceTranspiler))));
			harmony.Patch(AccessTools.Method(typeof(MoveableTree), nameof(MoveableTree.RenderCloneGeometry)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeMovementPatcher), nameof(RenderClonePrefix))));
        }

		private static IEnumerable<CodeInstruction> RenderGeometryTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			bool sigFound = false;
			Label branch = il.DefineLabel(), exit = il.DefineLabel();
			LocalBuilder brightness = default, scale = default;
			MethodInfo methodSig = AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
				new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) });

			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			for(int i = 0; i < codes.Count; i++)
            {
				if (codes[i].opcode == OpCodes.Stloc_S && ((LocalBuilder)codes[i].operand).LocalIndex == 4)
				{
					scale = codes[i].operand as LocalBuilder;
				}
				if (codes[i].opcode == OpCodes.Stloc_S && ((LocalBuilder)codes[i].operand).LocalIndex == 5 && !sigFound)
				{
					sigFound = true;
					brightness = codes[i].operand as LocalBuilder;
					CodeInstruction[] insertCodes = new CodeInstruction[]
					{
						new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(TAConfig.RandomTreeRotation))),
						new CodeInstruction(OpCodes.Brfalse_S, branch),
						new CodeInstruction(OpCodes.Ldnull),
						new CodeInstruction(OpCodes.Ldloc_0),
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeTool), "m_cachedPosition")),
						new CodeInstruction(OpCodes.Ldloc_S, scale),
						new CodeInstruction(OpCodes.Ldloc_S, brightness),
						new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(RenderManager), nameof(RenderManager.DefaultColorLocation))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeMovementPatcher), nameof(TreeMovementPatcher.RenderRotation))),
						new CodeInstruction(OpCodes.Br_S, exit),
					};
					codes[i + 1].WithLabels(branch);
					codes.InsertRange(i + 1, insertCodes);
				}
				if(codes[i].Calls(methodSig))
                {
					codes[i + 1].WithLabels(exit);
                }
			}
			return codes.AsEnumerable();
		}

		private static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			for(int i = 0; i < codes.Count; i++)
            {
				if (codes[i].opcode == OpCodes.Callvirt && codes[i].Calls(AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.GetWindSpeed), new Type[] { typeof(Vector3) })))
                {
					codes.Insert(i + 1, new CodeInstruction(OpCodes.Mul));
					codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAConfig), nameof(TAConfig.TreeSwayFactor))));
				}
			}

			foreach(var code in codes)
            {
				Debug.Log($"TreeAnarchy: ==> {code}");
            }

			return codes.AsEnumerable();
        }

		/* I'm making TreeTool::RenderGeometry and MoveIt RenderGeometryClone call this custom RenderInstance instead of the default CO RenderInstance
		 * which would only affect one tree, instead of the entire tree population on the map.
		 */
		private static void RenderRotation(RenderManager.CameraInfo _, TreeInfo info, Vector3 position, float scale, float brightness, Vector4 objectIndex)
		{
			if (info.m_prefabInitialized)
			{
				TreeManager instance = Singleton<TreeManager>.instance;
				MaterialPropertyBlock materialBlock = instance.m_materialBlock;
				Matrix4x4 matrix = default;
				matrix.SetTRS(position, Quaternion.Euler(0f, (((long)position.magnitude) << 5) % 360L, 0f), new Vector3(scale, scale, scale));
				Color value = info.m_defaultColor * brightness;
				value.a = Singleton<WeatherManager>.instance.GetWindSpeed(position) * TreeSwayFactor;
				materialBlock.Clear();
				materialBlock.SetColor(instance.ID_Color, value);
				materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
				instance.m_drawCallData.m_defaultCalls++;
				Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
			}
		}

		/* I wonder if this code can be implemented directly into MoveIt mod... or should it stay seperate
		 */
		private static bool RenderClonePrefix(InstanceState instanceState, ref Matrix4x4 matrix4x, Vector3 deltaPosition, Vector3 center, bool followTerrain, RenderManager.CameraInfo cameraInfo)
		{
			TreeState treeState = instanceState as TreeState;
			TreeInfo treeInfo = treeState.Info.Prefab as TreeInfo;
			Randomizer randomizer = new Randomizer(treeState.instance.id.Tree);
			float scale = treeInfo.m_minScale + (float)randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
			float brightness = treeInfo.m_minBrightness + (float)randomizer.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
			Vector3 vector = matrix4x.MultiplyPoint(treeState.position - center);
			vector.y = treeState.position.y + deltaPosition.y;
			if (followTerrain)
			{
				vector.y = vector.y - treeState.terrainHeight + Singleton<TerrainManager>.instance.SampleOriginalRawHeightSmooth(vector);
			}
			if (RandomTreeRotation)
			{
				RenderRotation(cameraInfo, treeInfo, vector, scale, brightness, RenderManager.DefaultColorLocation);
			}
			else
			{
				TreeInstance.RenderInstance(cameraInfo, treeInfo, vector, scale, brightness, RenderManager.DefaultColorLocation);
			}

			return false;
		}
	}
}
