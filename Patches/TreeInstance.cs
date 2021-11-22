using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
        /// <summary>
        /// Replace all Mathf to faster EMath
        /// </summary>
        private static IEnumerable<CodeInstruction> ReplaceMath(IEnumerable<CodeInstruction> instructions) {
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
            MethodInfo lerp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Lerp));
            MethodInfo getBlack = AccessTools.PropertyGetter(typeof(Color), nameof(Color.black));
            MethodInfo getMatrixIdentity = AccessTools.PropertyGetter(typeof(Matrix4x4), nameof(Matrix4x4.identity));
            MethodInfo getVector4Zero = AccessTools.PropertyGetter(typeof(Vector4), nameof(Vector4.zero));
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
                } else if (code.opcode == OpCodes.Call && code.operand == lerp) {
                    code.operand = AccessTools.Method(typeof(EMath), nameof(EMath.Lerp));
                    yield return code;
                } else if (code.opcode == OpCodes.Call && code.operand == getBlack) {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.ColorBlack))).WithLabels(code.labels);
                } else if (code.opcode == OpCodes.Call && code.operand == getVector4Zero) {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.Vector4Zero))).WithLabels(code.labels);
                } else if (code.opcode == OpCodes.Call && code.operand == getMatrixIdentity) {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.MatrixIdentity))).WithLabels(code.labels);
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
                        yield return storedLDarg.WithLabels(labels);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAManager), nameof(TAManager.CalcTreeScale)));
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
                                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAManager), nameof(TAManager.m_brightness))).WithLabels(cur.labels);
                                    yield return storedLDarg;
                                    yield return new CodeInstruction(OpCodes.Ldelem_R4);
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

        private static IEnumerable<CodeInstruction> SetInfoTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceMath(instructions);

        private static IEnumerable<CodeInstruction> SetPositionTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceMath(instructions);

#if ENABLETREEANARCHY
        /// <summary>
        /// Used in Tree Anarchy state to get anarchy state set by user. This is specifically used in TreeInstance::set_GrowState
        /// </summary>
        /// <param name="val">GrowState</param>
        /// <returns>Returns true if set</returns>
        public static bool GetAnarchyState(int growState) => TAMod.UseTreeAnarchy && growState == 0;
#endif
        private static IEnumerable<CodeInstruction> SetGrowStateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = __SetGrowStateTranspiler(instructions, il);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }

        private static IEnumerable<CodeInstruction> __SetGrowStateTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
#if ENABLETREEANARCHY
            Label valueNotZero = il.DefineLabel();
            int counter = 0;
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(GetAnarchyState)));
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
#else
            return ReplaceMath(instructions);
#endif
        }

        private static IEnumerable<CodeInstruction> AfterTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = __AfterTerrainUpdatedTranspiler(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }
        private static IEnumerable<CodeInstruction> __AfterTerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions) {
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
        public static float SampleSnapDetailHeight(ref TreeInstance tree, Vector3 position) {
            float terrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
            float positionY = position.y;
            if (TAMod.UseTreeSnapping && positionY < (terrainHeight - errorMargin) && positionY > (terrainHeight + errorMargin)) {
                return positionY;
            }
            tree.m_flags &= FixedHeightMask;
            return terrainHeight;
        }

        private static IEnumerable<CodeInstruction> CalculateTreeTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = __CalculateTreeTranspiler(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }
        private static IEnumerable<CodeInstruction> __CalculateTreeTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo terrainInstance = AccessTools.PropertyGetter(typeof(Singleton<TerrainManager>), nameof(Singleton<TerrainManager>.instance));
            MethodInfo sampleDetailHeight = AccessTools.Method(typeof(TerrainManager), nameof(TerrainManager.SampleDetailHeight), new Type[] { typeof(Vector3) });
            foreach (var code in ReplaceMath(instructions)) {
                if (code.opcode == OpCodes.Call && code.operand == terrainInstance) {
                    yield return new CodeInstruction(OpCodes.Ldarga_S, 0).WithLabels(code.labels);
                } else if (code.opcode == OpCodes.Callvirt && code.operand == sampleDetailHeight) {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.SampleSnapDetailHeight))).WithLabels(code.labels);
                } else {
                    yield return code;
                }
            }
        }

#if ENABLETREEANARCHY
        public static bool CheckAnarchyState(ref TreeInstance tree) {
            if (Singleton<LoadingManager>.instance.m_currentlyLoading) {
                return true;
            }
            if (TAMod.UseTreeAnarchy) {
                ToolBase currentTool = ToolsModifierControl.GetCurrentTool<ToolBase>();
                if (!(currentTool is NetTool) && !(currentTool is BuildingTool) && !(currentTool is BulldozeTool)) {
                    if (tree.GrowState == 0) {
                        tree.GrowState = 1;
                        DistrictManager district = Singleton<DistrictManager>.instance;
                        byte park = district.GetPark(tree.Position);
                        district.m_parks.m_buffer[park].m_treeCount++;
                    }
                }
                return true;
            }
            return false;
        }

        private static void QueuedAction(object treeID) => Singleton<TreeManager>.instance.ReleaseTree((uint)treeID);

        public static void ReleaseTreeQueue(uint treeID) => ThreadPool.QueueUserWorkItem(QueuedAction, treeID);

        private static IEnumerable<CodeInstruction> InstallCheckAnarchyInCheckOverlapTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            bool firstSig = false;
            Label exit = il.DefineLabel();
            instructions.Last().WithLabels(exit);
            using (var codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (!firstSig && cur.opcode == OpCodes.Brtrue && codes.MoveNext()) {
                        firstSig = true;
                        yield return new CodeInstruction(OpCodes.Brfalse, exit);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.CheckAnarchyState)));
                        yield return new CodeInstruction(OpCodes.Brtrue, exit);
                    } else {
                        yield return cur;
                    }
                }
            }
        }
#endif
        private static IEnumerable<CodeInstruction> CheckOverlapTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            var codes = __CheckOverlapTranspiler(instructions, il);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }

        private static IEnumerable<CodeInstruction> __CheckOverlapTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label exit = il.DefineLabel();
            bool firstSigFound = false;
            IEnumerable<CodeInstruction> codes_intermediate;
            FieldInfo treeCount = AccessTools.Field(typeof(DistrictPark), nameof(DistrictPark.m_treeCount));
#if ENABLETREEANARCHY
            codes_intermediate = ReplaceScaleCalculator(InstallCheckAnarchyInCheckOverlapTranspiler(instructions, il));
#else
            codes_intermediate = ReplaceScaleCalculator(instructions);
#endif
            codes_intermediate.Last().WithLabels(exit);
            using (var codes = codes_intermediate.GetEnumerator()) {
                while (codes.MoveNext()) {
                    var cur = codes.Current;
                    if (!firstSigFound && cur.opcode == OpCodes.Stfld && cur.operand == treeCount) {
                        firstSigFound = true;
                        yield return cur;
                        yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.DeleteOnOverlap)));
                        yield return new CodeInstruction(OpCodes.Brfalse_S, exit);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.ReleaseTreeQueue)));
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        private static IEnumerable<CodeInstruction> OverlapQuadTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = __OverlapQuadTranspiler(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }

        private static IEnumerable<CodeInstruction> __OverlapQuadTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceScaleCalculator(instructions);

        /// <summary>
        /// Tree movement (tree sway) is also coded in this method
        /// Tree scaler is also coded in this method
        /// </summary>
        private static IEnumerable<CodeInstruction> PopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo getHidden = AccessTools.PropertyGetter(typeof(TreeInstance), nameof(TreeInstance.Hidden));
            foreach (var code in ReplaceScaleCalculator(instructions, true)) {
                if (code.opcode == OpCodes.Stloc_0 || code.opcode == OpCodes.Ldloc_0) {
                    // skip it
                } else if (code.opcode == OpCodes.Call && code.operand == getHidden) {
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TreeInstance), nameof(TreeInstance.m_flags))).WithLabels(code.labels);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                    yield return new CodeInstruction(OpCodes.And);
                } else {
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> PopulateGroupDataStaticTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = __OverlapQuadTranspiler(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }
        private static IEnumerable<CodeInstruction> __PopulateGroupDataStaticTranspiler(IEnumerable<CodeInstruction> instructions) =>
            ReplaceGetWindSpeedWithCustom(ReplaceMath(instructions));

        private static IEnumerable<CodeInstruction> RayCastTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = __RayCastTranspiler(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }
        private static IEnumerable<CodeInstruction> __RayCastTranspiler(IEnumerable<CodeInstruction> instructions) => ReplaceScaleCalculator(ReplaceMath(instructions));

        private static IEnumerable<CodeInstruction> RenderInstanceTransplier(IEnumerable<CodeInstruction> instructions) {
            var codes = __RenderInstanceTransplier(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }
        private static IEnumerable<CodeInstruction> __RenderInstanceTransplier(IEnumerable<CodeInstruction> instructions) => ReplaceScaleCalculator(instructions, true);

        private static IEnumerable<CodeInstruction> RenderInstanceStaticTransplier(IEnumerable<CodeInstruction> instructions) {
            var codes = __RenderInstanceStaticTransplier(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }

        private static IEnumerable<CodeInstruction> __RenderInstanceStaticTransplier(IEnumerable<CodeInstruction> instructions) {
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

        private static IEnumerable<CodeInstruction> RenderLODTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = __RenderLODTranspiler(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }
        private static IEnumerable<CodeInstruction> __RenderLODTranspiler(IEnumerable<CodeInstruction> instructions) {
            const float lodMin = 100000f;
            const float lodMax = -lodMin;
            bool set100 = false;
            bool setlodMin = false;
            bool setlodMax = false;
            CodeInstruction storedCode = default;
            ConstructorInfo newVector3 = AccessTools.Constructor(typeof(Vector3), new Type[] { typeof(float), typeof(float), typeof(float) });
            foreach (var code in ReplaceMath(instructions)) {
                if (code.opcode == OpCodes.Ldelema && code.operand == typeof(Vector4)) {
                    // skip code
                } else if (code.opcode == OpCodes.Stobj && code.operand == typeof(Vector4)) {
                    code.opcode = OpCodes.Stelem;
                    yield return code;
                } else if (code.LoadsConstant(100f)) {
                    set100 = true;
                } else if (set100 && code.opcode == OpCodes.Newobj && code.operand == newVector3) {
                    set100 = false;
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.DefaultLod100))).WithLabels(code.labels);
                } else if (code.LoadsConstant(lodMin)) {
                    setlodMin = true;
                } else if (setlodMin && code.opcode == OpCodes.Newobj && code.operand == newVector3) {
                    setlodMin = false;
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.DefaultLodMin))).WithLabels(code.labels);
                } else if (code.LoadsConstant(lodMax)) {
                    storedCode = code;
                    setlodMax = true;
                } else if (setlodMax && (code.opcode != OpCodes.Ldc_R4 && code.opcode != OpCodes.Newobj)) {
                    setlodMax = false;
                    yield return storedCode;
                    yield return code;
                } else if (setlodMax && code.opcode == OpCodes.Newobj && code.operand == newVector3) {
                    setlodMax = false;
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(EMath), nameof(EMath.DefaultLodMax))).WithLabels(code.labels);
                } else {
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> TerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = __TerrainUpdatedTranspiler(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }
        private static IEnumerable<CodeInstruction> __TerrainUpdatedTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<Label> labels = instructions.Last().labels;
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Beq) {
                    yield return new CodeInstruction(OpCodes.Bne_Un_S, labels[0]);
                } else if (code.opcode == OpCodes.Brfalse) {
                    code.opcode = OpCodes.Brfalse_S;
                    yield return code;
                } else if (code.opcode == OpCodes.Ret && code.labels.Count == 0) {
                } else {
                    if (code.opcode != OpCodes.Ret && code.labels.Count > 0) code.labels.Clear();
                    yield return code;
                }
            }
        }

        private static IEnumerable<CodeInstruction> TerrainUpdatedVectorTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codes = __TerrainUpdatedVectorTranspiler(instructions);
            foreach (var code in codes) {
                TAMod.TALog(code.ToString());
            }
            return codes;
        }

        private static IEnumerable<CodeInstruction> __TerrainUpdatedVectorTranspiler(IEnumerable<CodeInstruction> instructions) {
            List<Label> labels = instructions.Last().labels;
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Brtrue) {
                    code.opcode = OpCodes.Brfalse;
                    code.operand = labels[0];
                    yield return code;
                } else if (code.opcode == OpCodes.Ret && code.labels.Count == 0) {
                } else {
                    if (code.opcode != OpCodes.Ret && code.labels.Count > 0) code.labels.Clear();
                    yield return code;
                }
            }
        }

        private void EnableTreeInstancePatch(Harmony harmony) {
            harmony.Patch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.Info)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.SetInfoTranspiler))));
            harmony.Patch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.Position)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.SetPositionTranspiler))));
            harmony.Patch(AccessTools.PropertySetter(typeof(TreeInstance), nameof(TreeInstance.GrowState)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.SetGrowStateTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.AfterTerrainUpdated)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.AfterTerrainUpdatedTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.CalculateTree)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(CalculateTreeTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), "CheckOverlap"),
                transpiler: new HarmonyMethod(typeof(TAPatcher), nameof(CheckOverlapTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.OverlapQuad)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(OverlapQuadTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(uint), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData),
                             typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(PopulateGroupDataTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(),
                                typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(PopulateGroupDataStaticTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RayCast)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(RayCastTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(uint), typeof(int) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(RenderInstanceTransplier))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(RenderInstanceStaticTransplier))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderLod)),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(RenderLODTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.TerrainUpdated),
                new Type[] { typeof(uint), typeof(float), typeof(float), typeof(float), typeof(float) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TerrainUpdatedTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.TerrainUpdated),
                new Type[] { typeof(TreeInfo), typeof(Vector3) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TerrainUpdatedVectorTranspiler))));
        }

        private void DisableTreeInstancePatch(Harmony harmony) {
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
