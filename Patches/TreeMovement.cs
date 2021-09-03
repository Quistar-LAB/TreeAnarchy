using ColossalFramework;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
#pragma warning disable IDE0044 // Add readonly modifier
        private static Quaternion[] treeQuaternion = new Quaternion[360];
        private static bool updateLODTreeSway = false;
        private static WeatherManager wmInstance;
#pragma warning restore IDE0044 // Add readonly modifier

        private void EnableTreeMovementPatches(Harmony harmony) {
            for (int i = 0; i < 360; i++) {
                treeQuaternion[i] = Quaternion.Euler(0, i, 0);
            }
            wmInstance = Singleton<WeatherManager>.instance;
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(RenderInstanceTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(),
                                typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(PopulateGroupDataTranspiler))));
            harmony.Patch(AccessTools.Method(typeof(OptionsMainPanel), nameof(OptionsMainPanel.OnClosed)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(OnOptionPanelClosed))));
        }

        private void DisableTreeMovementPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.RenderInstance),
                new Type[] { typeof(RenderManager.CameraInfo), typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4) }), HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(TreeInstance), nameof(TreeInstance.PopulateGroupData),
                new Type[] { typeof(TreeInfo), typeof(Vector3), typeof(float), typeof(float), typeof(Vector4), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(),
                                typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType() }),
                HarmonyPatchType.Transpiler, HARMONYID);
            harmony.Unpatch(AccessTools.Method(typeof(OptionsMainPanel), nameof(OptionsMainPanel.OnClosed)), HarmonyPatchType.Postfix, HARMONYID);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Quaternion GetRandomQuaternion(float magnitude) {
            int index = (int)((long)magnitude * RandomTreeRotationFactor) % 359;
            index = (index + (index >> 31)) ^ (index >> 31);
            return treeQuaternion[index];
        }

        public static float GetWindSpeed(ref Vector3 pos) {
            /* Apparently the lambda expression (a = (a ? > 127 : 127 : a) < 0 ? 0 : a) produces
             * unreliable results.. mono bug? Using local functions instead so they can be inlined
             * which shaved off ~5ms for this routine per 1 million calls */
            int clampi(int a) { a = a > 127 ? 127 : a; return a < 0 ? 0 : a; }
            float clampf(float f) { f = f > 2f ? 2f : f; return f < 0f ? 0f : f; }
            WeatherManager.WindCell[] windGrids = wmInstance.m_windGrid;
            int x = clampi((int)(pos.x * 0.0074074074f + 63.5f));
            int y = clampi((int)(pos.z * 0.0074074074f + 63.5f));
            return clampf((pos.y - windGrids[y * 128 + x].m_totalHeight * 0.015625f) * 0.02f + 1) * TreeSwayFactor;
        }


        private static void UpdateLODProc() {
            int layerID = Singleton<TreeManager>.instance.m_treeLayer;
            FastList<RenderGroup> renderedGroups = Singleton<RenderManager>.instance.m_renderedGroups;
            for (int i = 0; i < renderedGroups.m_size; i++) {
                RenderGroup renderGroup = renderedGroups.m_buffer[i];
                RenderGroup.MeshLayer layer = renderGroup.GetLayer(layerID);
                if (!(layer is null)) {
                    layer.m_dataDirty = true;
                }
                renderGroup.UpdateMeshData();
            }
        }

        public static void UpdateTreeSway() {
            updateLODTreeSway = true;
        }

        private static IEnumerable<CodeInstruction> RenderInstanceTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo qIdentity = AccessTools.PropertyGetter(typeof(Quaternion), nameof(Quaternion.identity));
            MethodInfo getWindSpeed = AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.GetWindSpeed), new Type[] { typeof(Vector3) });
            using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    CodeInstruction cur = codes.Current;
                    if (cur.opcode == OpCodes.Call && cur.operand == qIdentity) {
                        yield return new CodeInstruction(OpCodes.Ldarga_S, 2);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.sqrMagnitude)));
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(GetRandomQuaternion)));
                    } else if (cur.opcode == OpCodes.Call && codes.MoveNext()) {
                        CodeInstruction next = codes.Current;
                        if (next.opcode == OpCodes.Ldarg_2 && codes.MoveNext()) {
                            CodeInstruction next1 = codes.Current;
                            if (next1.opcode == OpCodes.Callvirt && next1.operand == getWindSpeed) {
                                yield return new CodeInstruction(OpCodes.Ldarga_S, 2);
                                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(GetWindSpeed)));
                            } else {
                                yield return cur;
                                yield return next;
                                yield return next1;
                            }
                        } else {
                            yield return cur;
                            yield return next;
                        }
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        /* TreeInstance::PopulateGroupData */
        private static IEnumerable<CodeInstruction> PopulateGroupDataTranspiler(IEnumerable<CodeInstruction> instructions) {
            MethodInfo getWindSpeed = AccessTools.Method(typeof(WeatherManager), nameof(WeatherManager.GetWindSpeed), new Type[] { typeof(Vector3) });
            using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
                while (codes.MoveNext()) {
                    CodeInstruction cur = codes.Current;
                    if (cur.opcode == OpCodes.Call && codes.MoveNext()) {
                        CodeInstruction next = codes.Current;
                        if (next.opcode == OpCodes.Ldarg_2 && codes.MoveNext()) {
                            CodeInstruction next1 = codes.Current;
                            if (next1.opcode == OpCodes.Callvirt && next1.operand == getWindSpeed) {
                                yield return new CodeInstruction(OpCodes.Ldarga_S, 1);
                                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(GetWindSpeed)));
                            } else {
                                yield return cur;
                                yield return next;
                                yield return next1;
                            }
                        } else {
                            yield return cur;
                            yield return next;
                        }
                    } else {
                        yield return cur;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void OnOptionPanelClosed() {
            if (updateLODTreeSway) {
                UpdateLODProc();
                updateLODTreeSway = false;
            }
        }
    }
}
