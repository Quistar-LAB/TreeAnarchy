#if ENABLETREEANARCHY
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace TreeAnarchy {
    internal static partial class TAPatcher {
        private static Type m_redirectUtil = null;
        private static MethodInfo m_redirectMethod = null;

        private static void PatchPTA(Harmony harmony) {
            try {
                if (m_redirectUtil is null && (IsPluginExists(593588108, "Prop & Tree Anarchy") || IsPluginExists(2456344023, "Prop & Tree Anarchy"))) {
                    m_redirectUtil = Assembly.Load("PropAnarchy").GetType("PropAnarchy.Redirection.RedirectionUtil");
                    MethodInfo[] methods = m_redirectUtil.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
                    for (int i = 0; i < methods.Length; i++) {
                        if (methods[i].Name == "RedirectMethod" && methods[i].GetParameters()[2].ToString().Contains("Dictionary")) {
                            m_redirectMethod = methods[i];
                        }
                    }
                    harmony.Patch(AccessTools.Method(m_redirectUtil, "RedirectMethods"),
                        transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(PTARedirectMethodsTranspiler))));
                }
            } catch (Exception e) {
                TAMod.TALog("Found Prop & Tree Anarchy, but failed to patch it. Happily continuing and ignoring this error\n" + e.ToString());
            }
        }

        private static void UnpatchPTA(Harmony harmony) {
            if (!(m_redirectUtil is null)) {
                harmony.Unpatch(AccessTools.Method(m_redirectUtil, "RedirectMethods"), HarmonyPatchType.Transpiler, HARMONYID);
                m_redirectUtil = null;
            }
        }

        public static void CustomRedirect(Type targetType, MethodInfo method, object redirects, bool reverse = false) {
            switch (method.Name) {
            case @"set_GrowState":
            case @"CheckOverlap":
            case @"CheckPlacementErrors":
                Type type = method.GetParameters().First().ParameterType;
                if (type == typeof(PropInstance).MakeByRefType() || type == typeof(PropInfo)) {
                    goto runDefault;
                }
                TAMod.TALog($"Overriding Prop & Tree Anarchy Redirect: {method}");
                return;
            default:
runDefault:
                m_redirectMethod.Invoke(null, new object[] {
                    targetType,
                    method,
                    redirects,
                    reverse
                });
                break;
            }
        }

        /* A patch to the patcher of Prop Tree Anarchy */
        private static IEnumerable<CodeInstruction> PTARedirectMethodsTranspiler(IEnumerable<CodeInstruction> instructions) {
            foreach (var code in instructions) {
                if (code.opcode == OpCodes.Call && code.ToString().Contains("RedirectMethod")) {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TAPatcher), nameof(TAPatcher.CustomRedirect)));
                } else yield return code;
            }
        }

        private static void EnableTreeAnarchyPatches(Harmony harmony) {
            try {
                harmony.Patch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.CheckPlacementErrors)),
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(TAPatcher), nameof(TreeToolCheckPlacementErrorsTranspiler))));
            } catch (Exception e) {
                TAMod.TALog("Failed to patch TreeTool::CheckPlacementErrors. This is fatal for tree anarchy function.");
                TAMod.TALog(e.Message);
                throw;
            }
        }

        private static void DisableTreeAnarchyPatches(Harmony harmony) {
            harmony.Unpatch(AccessTools.Method(typeof(TreeTool), nameof(TreeTool.CheckPlacementErrors)), HarmonyPatchType.Transpiler, HARMONYID);
        }

        private static IEnumerable<CodeInstruction> TreeToolCheckPlacementErrorsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
            Label TreeAnarchyDisabled = il.DefineLabel();
            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(TAMod), nameof(TAMod.UseTreeAnarchy)));
            yield return new CodeInstruction(OpCodes.Brfalse_S, TreeAnarchyDisabled);
            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
            yield return new CodeInstruction(OpCodes.Conv_I8);
            yield return new CodeInstruction(OpCodes.Ret);
            using (IEnumerator<CodeInstruction> codes = instructions.GetEnumerator()) {
                if (codes.MoveNext()) codes.Current.WithLabels(TreeAnarchyDisabled);
                do {
                    yield return codes.Current;
                } while (codes.MoveNext());
            }
        }
    }
}
#endif