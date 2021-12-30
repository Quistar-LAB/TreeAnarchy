using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TreeAnarchy.Patches {
    internal static class TreeInstancePatches {
        private const float errorMargin = 0.075f;
        private const ushort FixedHeightMask = unchecked((ushort)~TreeInstance.Flags.FixedHeight);
        private const ushort FixedHeightFlag = unchecked((ushort)TreeInstance.Flags.FixedHeight);

        /// <summary>
        /// Replace all Mathf to faster EMath
        /// </summary>
        internal static IEnumerable<CodeInstruction> ReplaceMath(IEnumerable<CodeInstruction> instructions) {
            MethodInfo max = AccessTools.Method(typeof(Mathf), nameof(Mathf.Max), new Type[] { typeof(int), typeof(int) });
            MethodInfo min = AccessTools.Method(typeof(Mathf), nameof(Mathf.Min), new Type[] { typeof(int), typeof(int) });
            MethodInfo maxf = AccessTools.Method(typeof(Mathf), nameof(Mathf.Max), new Type[] { typeof(float), typeof(float) });
            MethodInfo minf = AccessTools.Method(typeof(Mathf), nameof(Mathf.Min), new Type[] { typeof(float), typeof(float) });
            MethodInfo clamp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) });
            MethodInfo clampf = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), new Type[] { typeof(float), typeof(float), typeof(float) });
            MethodInfo roundToInt = AccessTools.Method(typeof(Mathf), nameof(Mathf.RoundToInt), new Type[] { typeof(float) });
            MethodInfo vector3Min = AccessTools.Method(typeof(Vector3), nameof(Vector3.Min));
            MethodInfo vector3Max = AccessTools.Method(typeof(Vector3), nameof(Vector3.Max));
            MethodInfo sin = AccessTools.Method(typeof(Mathf), nameof(Mathf.Sin));
            MethodInfo cos = AccessTools.Method(typeof(Mathf), nameof(Mathf.Cos));
            MethodInfo absi = AccessTools.Method(typeof(Mathf), nameof(Mathf.Abs), new Type[] { typeof(int) });
            MethodInfo absf = AccessTools.Method(typeof(Mathf), nameof(Mathf.Abs), new Type[] { typeof(float) });
            MethodInfo sqrt = AccessTools.Method(typeof(Mathf), nameof(Mathf.Sqrt));
            MethodInfo lerpFloat = AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp), new Type[] { typeof(float), typeof(float), typeof(float) });
            MethodInfo lerpVector = AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp), new Type[] { typeof(Vector3), typeof(Vector3), typeof(float) });
            MethodInfo getBlack = AccessTools.PropertyGetter(typeof(Color), nameof(Color.black));
            MethodInfo getMatrixIdentity = AccessTools.PropertyGetter(typeof(Matrix4x4), nameof(Matrix4x4.identity));
            MethodInfo getVector4Zero = AccessTools.PropertyGetter(typeof(Vector4), nameof(Vector4.zero));
            MethodInfo checkRenderDistance = AccessTools.Method(typeof(RenderManager.CameraInfo), nameof(RenderManager.CameraInfo.CheckRenderDistance));
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Call && code.operand == clamp) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Clamp), new Type[] { typeof(int), typeof(int), typeof(int) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == clampf) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Clamp), new Type[] { typeof(float), typeof(float), typeof(float) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == roundToInt) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.RoundToInt), new Type[] { typeof(float) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == vector3Min) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Min), new Type[] { typeof(Vector3), typeof(Vector3) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == min) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Min), new Type[] { typeof(int), typeof(int) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == minf) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Min), new Type[] { typeof(float), typeof(float) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == vector3Max) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Max), new Type[] { typeof(Vector3), typeof(Vector3) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == max) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Max), new Type[] { typeof(int), typeof(int) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == maxf) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Max), new Type[] { typeof(float), typeof(float) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == cos) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Cos));
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == sin) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Sin));
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == absi) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Abs), new Type[] { typeof(int) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == absf) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Abs), new Type[] { typeof(float) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == sqrt) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Sqrt));
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == lerpVector) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Lerp), new Type[] { typeof(Vector3), typeof(Vector3), typeof(float) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == lerpFloat) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Lerp), new Type[] { typeof(float), typeof(float), typeof(float) });
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == getBlack) {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.ColorBlack))).WithLabels(code.labels);
                } else if (code.opcode == OpCodes.Call && code.operand == getVector4Zero) {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.Vector4Zero))).WithLabels(code.labels);
                } else if (code.opcode == OpCodes.Call && code.operand == getMatrixIdentity) {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.matrix4Identity))).WithLabels(code.labels);
                } else if (code.opcode == OpCodes.Callvirt && code.operand == checkRenderDistance) {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EMath), nameof(EMath.CheckRenderDistance)));
                } else {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Replaces all calculation for scale into loading pre-calculated m_defScale[]
        /// </summary>
        /// <param name="treeIndexParamPos">TreeID parameter index</param>
        /// <returns></returns>
        private static IEnumerable<CodeInstruction> ReplaceScaleCalculator(IEnumerable<CodeInstruction> instructions, bool replaceBrightnessToo = false) {
            bool IsStloc(ref OpCode opcode) => opcode == OpCodes.Stloc || opcode == OpCodes.Stloc_0 || opcode == OpCodes.Stloc_1 || opcode == OpCodes.Stloc_2 || opcode == OpCodes.Stloc_3 || opcode == OpCodes.Stloc_S;
            bool IsLdloc(ref OpCode opcode) => opcode == OpCodes.Ldloc || opcode == OpCodes.Ldloc_0 || opcode == OpCodes.Ldloc_1 || opcode == OpCodes.Ldloc_2 || opcode == OpCodes.Ldloc_3 || opcode == OpCodes.Ldloc_S;
            FieldInfo minBrightness = AccessTools.Field(typeof(TreeInfo), nameof(TreeInfo.m_minBrightness));
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (cur.opcode == OpCodes.Ldloca_S && (cur.operand is LocalBuilder local) && local.LocalType == typeof(Randomizer) && codes.MoveNext()) {
                        List<Label> labels = cur.labels;
                        var storedLDarg = codes.Current;
                        while (codes.MoveNext()) {
                            cur = codes.Current;
                            if (IsStloc(ref cur.opcode)) break;
                        }
                        yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAManager), nameof(TAManager.m_extraTreeInfos))).WithLabels(labels);
                        yield return storedLDarg;
                        yield return new CodeInstruction(OpCodes.Ldelema, typeof(TAManager.ExtraTreeInfo));
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TAManager.ExtraTreeInfo), nameof(TAManager.ExtraTreeInfo.TreeScale)));
                        yield return cur;
                        if (replaceBrightnessToo && codes.MoveNext()) {
                            cur = codes.Current;
                            if (IsLdloc(ref cur.opcode) && codes.MoveNext()) {
                                var next = codes.Current;
                                if (next.opcode == OpCodes.Ldfld && next.operand == minBrightness) {
                                    while (codes.MoveNext()) {
                                        next = codes.Current;
                                        if (IsStloc(ref next.opcode)) break;
                                    }
                                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAManager), nameof(TAManager.m_extraTreeInfos)));
                                    yield return storedLDarg;
                                    yield return new CodeInstruction(OpCodes.Ldelema, typeof(TAManager.ExtraTreeInfo));
                                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TAManager.ExtraTreeInfo), nameof(TAManager.ExtraTreeInfo.m_brightness)));
                                    yield return next;
                                } else {
                                    yield return cur;
                                    yield return next;
                                }
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

        private static IEnumerable<CodeInstruction> ReplaceGetWindSpeedWithCustom(IEnumerable<CodeInstruction> instructions) {
            bool sigFound = false;
            MethodInfo weatherManager = AccessTools.PropertyGetter(typeof(Singleton<WeatherManager>), nameof(Singleton<WeatherManager>.instance));
            MethodInfo getWindSpeed = AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.GetWindSpeed), new Type[] { typeof(Vector3) });
            foreach (var code in instructions) {
                if (!sigFound && code.opcode == OpCodes.Call && code.operand == weatherManager) {
                    sigFound = true;
                } else if (sigFound && code.opcode == OpCodes.Callvirt && code.operand == getWindSpeed) {
                    sigFound = false;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.GetWindSpeed))).WithLabels(code.labels);
                } else {
                    yield return code;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> SetInfoTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceMath(instructions);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> SetPositionTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceMath(instructions);

        /// <summary>
        /// Used in Tree Anarchy state to get anarchy state set by user. This is specifically used in TreeInstance::set_GrowState
        /// </summary>
        /// <param name="val">GrowState</param>
        /// <returns>Returns true if set</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool GetAnarchyState(int growState) => TAMod.UseTreeAnarchy && growState == 0;

        private static IEnumerable<CodeInstruction> SetGrowStateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label valueNotZero = il.DefineLabel();
            int counter = 0;
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeInstancePatches), nameof(GetAnarchyState)));
            yield return new CodeInstruction(OpCodes.Brfalse_S, valueNotZero);
            yield return new CodeInstruction(OpCodes.Ret);
            foreach (var code in ReplaceMath(instructions)) {
                if (counter == 0) {
                    yield return code.WithLabels(valueNotZero);
                } else {
                    yield return code;
                }
                counter++;
            }
        }

        private static IEnumerable<CodeInstruction> AfterTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions) {
            bool skip = false;
            bool firstBeqFound = false;
            foreach (var code in ReplaceMath(instructions)) {
                if (!skip && code.opcode == OpCodes.Ldc_I4_3) {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_S, 0x23).WithLabels(code.labels);
                } else if (!skip && !firstBeqFound && code.opcode == OpCodes.Beq) {
                    firstBeqFound = true;
                    skip = true;
                } else if (skip && firstBeqFound && code.opcode == OpCodes.Brtrue) {
                    skip = false;
                    yield return new CodeInstruction(OpCodes.Bne_Un_S, code.operand);
                } else if (!skip) {
                    yield return code;
                }
            }
        }

        /// <summary>
        /// Helper function for Tree Snapping to remove fixedheight flag in m_flags if tree is close to terrain
        /// </summary>
        /// <param name="tree">Asset</param>
        /// <param name="position">Current position</param>
        /// <returns>Returns the terrain height if asset close to terrain, otherwise returns the asset current position y</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SampleSnapDetailHeight(ref TreeInstance tree, Vector3 position) {
            float terrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
            float positionY = position.y;
            if (TAMod.UseTreeSnapping && positionY < (terrainHeight - errorMargin) && positionY > (terrainHeight + errorMargin)) {
                return positionY;
            }
            tree.m_flags &= FixedHeightMask;
            return terrainHeight;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> CalculateTreeTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo terrainInstance = AccessTools.PropertyGetter(typeof(Singleton<TerrainManager>), nameof(Singleton<TerrainManager>.instance));
            MethodInfo sampleDetailHeight = AccessTools.Method(typeof(TerrainManager), nameof(TerrainManager.SampleDetailHeight), new Type[] { typeof(Vector3) });
            foreach (var code in ReplaceMath(instructions)) {
                if (code.opcode == OpCodes.Call && code.operand == terrainInstance) {
                    yield return new CodeInstruction(OpCodes.Ldarga_S, 0).WithLabels(code.labels);
                } else if (code.opcode == OpCodes.Callvirt && code.operand == sampleDetailHeight) {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeInstancePatches), nameof(TreeInstancePatches.SampleSnapDetailHeight))).WithLabels(code.labels);
                } else {
                    yield return code;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CheckAnarchyState(ref TreeInstance tree) {
            if (TAMod.UseTreeAnarchy) {
                ToolBase currentTool = ToolsModifierControl.GetCurrentTool<ToolBase>();
                if (!(currentTool is NetTool) && !(currentTool is BuildingTool) && !(currentTool is BulldozeTool) && tree.GrowState == 0) {
                    tree.GrowState = 1;
                    DistrictManager district = Singleton<DistrictManager>.instance;
                    district.m_parks.m_buffer[district.GetPark(tree.Position)].m_treeCount++;
                }
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CheckOverlapCoroutine(ref TreeInstance tree, uint treeID, Vector3 position) {
            if (tree.Info is TreeInfo info && !CheckAnarchyState(ref tree)) {
                Quad2 quad;
                ushort flags = tree.m_flags;
                ItemClass.CollisionType collisionType = (flags & (ushort)TreeInstance.Flags.FixedHeight) == 0 ? ItemClass.CollisionType.Terrain : ItemClass.CollisionType.Elevated;
                float scale = TAManager.m_extraTreeInfos[treeID].TreeScale;
                float height = info.m_generatedInfo.m_size.y * scale;
                float y = position.y;
                float maxY = position.y + height;
                float range = (flags & (ushort)TreeInstance.Flags.Single) == 0 ? 4.5f : 0.3f;
                Vector2 a = VectorUtils.XZ(position);
                quad.a = new Vector2(a.x - range, a.y - range);
                quad.b = new Vector2(a.x - range, a.y + range);
                quad.c = new Vector2(a.x + range, a.y + range);
                quad.d = new Vector2(a.x + range, a.y - range);
                bool flag = false;
                if (Singleton<NetManager>.instance.OverlapQuad(quad, y, maxY, collisionType, info.m_class.m_layer, 0, 0, 0) ||
                    Singleton<BuildingManager>.instance.OverlapQuad(quad, y, maxY, collisionType, info.m_class.m_layer, 0, 0, 0)) {
                    flag = true;
                }
                if (flag) {
                    if (tree.GrowState != 0) {
                        tree.GrowState = 0;
                        DistrictManager instance = Singleton<DistrictManager>.instance;
                        instance.m_parks.m_buffer[instance.GetPark(position)].m_treeCount--;
                        if (TAMod.DeleteOnOverlap) {
                            Singleton<SimulationManager>.instance.AddAction(() => Singleton<TreeManager>.instance.ReleaseTree(treeID));
                        }
                    }
                } else if (tree.GrowState == 0) {
                    tree.GrowState = 1;
                    DistrictManager instance = Singleton<DistrictManager>.instance;
                    instance.m_parks.m_buffer[instance.GetPark(position)].m_treeCount++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> CheckOverlapTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.Position)));
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeInstancePatches), nameof(TreeInstancePatches.CheckOverlapCoroutine)));
            yield return new CodeInstruction(OpCodes.Ret);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> OverlapQuadTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceScaleCalculator(instructions);

        /// <summary>
        /// Tree movement (tree sway) is also coded in this method
        /// Tree scaler is also coded in this method
        /// </summary>
        private static IEnumerable<CodeInstruction> PopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo getHidden = AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.Hidden));
            MethodInfo populateGroupData = AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(int).MakeByRefType(),
                             typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(),
                             typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() });
            foreach (var code in ReplaceScaleCalculator(instructions, true)) {
                if (code.opcode == OpCodes.Stloc_0 || code.opcode == OpCodes.Ldloc_0) {
                    // skip it
                } else if (code.opcode == OpCodes.Call && code.operand == getHidden) {
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))).WithLabels(code.labels);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                    yield return new CodeInstruction(OpCodes.And);
                } else if (code.opcode == OpCodes.Call && code.operand == populateGroupData) {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TreeInstancePatches), nameof(TreeInstancePatches.PopulateGroupData)));
                } else {
                    yield return code;
                }
            }
        }

        public static void PopulateGroupData(TreeInfo info, Vector3 position, float scale, float brightness, Vector4 objectIndex, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance) {
            float y = info.m_generatedInfo.m_size.y * scale;
            float num = EMath.Max(info.m_generatedInfo.m_size.x, info.m_generatedInfo.m_size.z) * scale * 0.5f;
            min = EMath.Min(min, position - new Vector3(num, 0f, num));
            max = EMath.Max(max, position + new Vector3(num, y, num));
            maxRenderDistance = EMath.Max(maxRenderDistance, 30000f);
            maxInstanceDistance = EMath.Max(maxInstanceDistance, 425f);
            Color32 color = (info.m_defaultColor * brightness).linear;
            color.a = (byte)EMath.Clamp(EMath.RoundToInt(TAManager.GetWindSpeed(position)), 0, 255);
            position -= groupPosition;
            position.y += info.m_generatedInfo.m_center.y * scale;
            data.m_vertices[vertexIndex] = position + new Vector3(info.m_renderUv0B.x * scale, info.m_renderUv0B.y * scale, 0f);
            data.m_normals[vertexIndex] = objectIndex;
            data.m_uvs[vertexIndex] = info.m_renderUv0;
            data.m_uvs2[vertexIndex] = info.m_renderUv0B * scale;
            data.m_colors[vertexIndex] = color;
            vertexIndex++;
            data.m_vertices[vertexIndex] = position + new Vector3(info.m_renderUv1B.x * scale, info.m_renderUv1B.y * scale, 0f);
            data.m_normals[vertexIndex] = objectIndex;
            data.m_uvs[vertexIndex] = info.m_renderUv1;
            data.m_uvs2[vertexIndex] = info.m_renderUv1B * scale;
            data.m_colors[vertexIndex] = color;
            vertexIndex++;
            data.m_vertices[vertexIndex] = position + new Vector3(info.m_renderUv2B.x * scale, info.m_renderUv2B.y * scale, 0f);
            data.m_normals[vertexIndex] = objectIndex;
            data.m_uvs[vertexIndex] = info.m_renderUv2;
            data.m_uvs2[vertexIndex] = info.m_renderUv2B * scale;
            data.m_colors[vertexIndex] = color;
            vertexIndex++;
            data.m_vertices[vertexIndex] = position + new Vector3(info.m_renderUv3B.x * scale, info.m_renderUv3B.y * scale, 0f);
            data.m_normals[vertexIndex] = objectIndex;
            data.m_uvs[vertexIndex] = info.m_renderUv3;
            data.m_uvs2[vertexIndex] = info.m_renderUv3B * scale;
            data.m_colors[vertexIndex] = color;
            vertexIndex++;
            data.m_triangles[triangleIndex++] = vertexIndex - 4;
            data.m_triangles[triangleIndex++] = vertexIndex - 3;
            data.m_triangles[triangleIndex++] = vertexIndex - 2;
            data.m_triangles[triangleIndex++] = vertexIndex - 2;
            data.m_triangles[triangleIndex++] = vertexIndex - 3;
            data.m_triangles[triangleIndex++] = vertexIndex - 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> PopulateGroupDataStaticTranspiler(IEnumerable<CodeInstruction> instructions) =>
            ReplaceGetWindSpeedWithCustom(ReplaceMath(instructions));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> RayCastTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceScaleCalculator(ReplaceMath(instructions));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IEnumerable<CodeInstruction> RenderInstanceTransplier(IEnumerable<CodeInstruction> instructions) => ReplaceScaleCalculator(instructions, true);

        private static IEnumerable<CodeInstruction> RenderInstanceStaticTransplier(IEnumerable<CodeInstruction> instructions) {
            MethodInfo identity = AccessTools.PropertyGetter(typeof(Quaternion), nameof(Quaternion.identity));
            foreach (var code in ReplaceGetWindSpeedWithCustom(ReplaceMath(instructions))) {
                if (code.opcode == OpCodes.Call && code.operand == identity) {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAManager), nameof(TAManager.m_treeQuaternions))).WithLabels(code.labels);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.x)));
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Mul);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.z)));
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Mul);
                    yield return new CodeInstruction(OpCodes.Add);
                    yield return new CodeInstruction(OpCodes.Conv_I4);
                    yield return new CodeInstruction(OpCodes.Ldc_I4, 359);
                    yield return new CodeInstruction(OpCodes.Rem);
                    yield return new CodeInstruction(OpCodes.Ldelem, typeof(Quaternion));
                } else {
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> RenderLODTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceMath(instructions);

        private static IEnumerable<CodeInstruction> TerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<Label> labels = instructions.Last().labels;
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Beq) {
                    yield return new CodeInstruction(OpCodes.Bne_Un_S, labels[0]);
                } else if (code.opcode == OpCodes.Brfalse) {
                    code.opcode = OpCodes.Brfalse_S;
                    yield return code;
                } else if (code.opcode == OpCodes.Ret && code.labels.Count == 0) {
                    /* skip code */
                } else {
                    if (code.opcode != OpCodes.Ret && code.labels.Count > 0) code.labels.Clear();
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> TerrainUpdatedVectorTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<Label> labels = instructions.Last().labels;
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Brtrue) {
                    code.opcode = OpCodes.Brfalse;
                    code.operand = labels[0];
                    yield return code;
                } else if (code.opcode == OpCodes.Ret && code.labels.Count == 0) {
                    /* skip code */
                } else {
                    if (code.opcode != OpCodes.Ret && code.labels.Count > 0) code.labels.Clear();
                    yield return code;
                }
            }
        }

        internal static void EnableTreeInstancePatch(Harmony harmony) {
            try {
                harmony.Patch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.Info)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(TreeInstancePatches.SetInfoTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::Info. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.Position)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(TreeInstancePatches.SetPositionTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::Position. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.GrowState)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(TreeInstancePatches.SetGrowStateTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::GrowState.");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.AfterTerrainUpdated)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(TreeInstancePatches.AfterTerrainUpdatedTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::AfterTerrainUpdated. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(CalculateTreeTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::CalculateTree");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), "CheckOverlap"),
                    transpiler: new HarmonyMethod(typeof(TreeInstancePatches), nameof(CheckOverlapTranspiler)));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::CheckOverlap");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.OverlapQuad)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(OverlapQuadTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::OverlapQuad");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                    new Type[] { typeof(uint), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData),
                             typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(PopulateGroupDataTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::PopulateGroupData. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                    new Type[] { typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(),
                                typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(PopulateGroupDataStaticTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch static TreeInstance::PopulateGroupData. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RayCast)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(RayCastTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::RayCast. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                    new Type[] { typeof(RenderManager.CameraInfo), typeof(uint), typeof(int) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(RenderInstanceTransplier))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::RenderInstance");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                    new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(RenderInstanceStaticTransplier))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch static TreeInstance::RenderInstance");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderLod)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(RenderLODTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::RenderLod");
                TAMod.TALog(e.Message);
                throw;
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.TerrainUpdated),
                    new Type[] { typeof(uint), typeof(float), typeof(float), typeof(float), typeof(float) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(TerrainUpdatedTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::TerrainUpdated. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.TerrainUpdated),
                    new Type[] { typeof(TreeInfo), typeof(Vector3) }),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TreeInstancePatches), nameof(TerrainUpdatedVectorTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeInstance::TerrainUpdated. This is non-Fatal");
                TAMod.TALog(e.Message);
            }
        }

        internal static void DisableTreeInstancePatch(Harmony harmony, string HARMONYID) {
            harmony.Unpatch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.Info)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.Position)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.GrowState)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.AfterTerrainUpdated)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), "CheckOverlap"), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.OverlapQuad)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(uint), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData),
                             typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(),
                                typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RayCast)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(uint), typeof(int) }), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderLod)), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.TerrainUpdated),
                new Type[] { typeof(uint), typeof(float), typeof(float), typeof(float), typeof(float) }), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.TerrainUpdated),
                new Type[] { typeof(TreeInfo), typeof(Vector3) }), HarmonyPatchType.Transpiler, HARMONYID);
        }
    }
}
