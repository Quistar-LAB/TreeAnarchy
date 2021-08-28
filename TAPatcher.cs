using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using HarmonyLib;
using System;
using System.Reflection;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
        private const string HARMONYID = @"quistar.treeanarchy.mod";
        private Harmony m_harmony;
        internal Harmony CurrentHarmony {
            get => (m_harmony is null) ? m_harmony = new Harmony(HARMONYID) : m_harmony;
        }
        internal static FieldInfo MoveItUseTreeSnap = null;
        internal static bool isMoveItInstalled = false;
        private struct ModInfo {
            public readonly ulong fileID;
            public readonly string name;
            public ModInfo(ulong modID, string modName) {
                fileID = modID;
                name = modName;
            }
        }
        private static readonly ModInfo[] IncompatibleMods = new ModInfo[] {
            new ModInfo(455403039, "Unlimited Trees Mod"),
            new ModInfo(2378914031, "Unlimited Trees Revisited"),
            new ModInfo(869134690, "Tree Snapping"),
            new ModInfo(1637106958, "Lock Forestry"),
            new ModInfo(556784825, "Random Tree Rotation"),
            new ModInfo(1388613752, "Tree Movement Control"),
            new ModInfo(842981708, "Random Tree Rotation for Natural Disasters")
        };

        internal static bool IsPluginExists(ulong id, string name) {
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if (info.publishedFileID.AsUInt64 == id || info.ToString().Contains(name)) {
                    if (info.isEnabled) return true;
                }
            }
            foreach (var mod in PlatformService.workshop.GetSubscribedItems()) {
                for (int i = 0; i < IncompatibleMods.Length; i++) {
                    if (mod.AsUInt64 == id) {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckMoveItTreeSnapSig() {
            FieldInfo treeSnapField = typeof(MoveIt.MoveItTool).GetField("treeSnapping", BindingFlags.GetField | BindingFlags.Static | BindingFlags.Public);
            if (treeSnapField != null) {
                MoveItUseTreeSnap = treeSnapField;
                MoveItUseTreeSnap.SetValue(null, TAMod.UseTreeSnapping);
                isMoveItInstalled = true;
                return true;
            }
            return false;
        }

        internal bool CheckIncompatibleMods() {
            string errorMsg = "";
            foreach (var mod in PlatformService.workshop.GetSubscribedItems()) {
                for (int i = 0; i < IncompatibleMods.Length; i++) {
                    if (mod.AsUInt64 == IncompatibleMods[i].fileID) {
                        errorMsg += $"[{IncompatibleMods[i].name}] detected\n";
                        TAMod.TALog($"Incompatible mod: [{IncompatibleMods[i].name}] detected");
                    }
                }
            }
            if (errorMsg.Length > 0) {
                UIView.ForwardException(new Exception("Tree Anarchy detected incompatible mods, please remove the following mentioned mods", new Exception("\n" + errorMsg)));
                TAMod.TALog($"Tree Anarchy detected incompatible mods, please remove the following mentioned mods\n{errorMsg}");
                return false;
            }
            return true;
        }

        internal void EnableCore() {
            Harmony harmony = CurrentHarmony;
            EnableTreeLimitPatches(harmony);
            EnableTreeSnappingPatches(harmony);
            EnableTreeVariationPatches(harmony);
        }

        internal void DisableCore() {
            Harmony harmony = CurrentHarmony;
            DisableTreeLimitPatches(harmony);
            DisableTreeSnappingPatches(harmony);
            DisableTreeVariationPatches(harmony);
        }

        internal void LateEnable() {
            Harmony harmony = CurrentHarmony;
            EnableTreeMovementPatches(harmony);
#if ENABLETREEANARCHY
            PatchPTA(harmony);
            EnableTreeAnarchyPatches(harmony);
#endif
            if (IsPluginExists(1619685021, "MoveIt") || IsPluginExists(2215771668, "MoveIt")) {
                if (!CheckMoveItTreeSnapSig()) {
                    PatchMoveItSnapping(harmony);
                }
                PatchMoveItTreeVariation(harmony);
            }
        }

        internal void DisableLatePatch() {
            Harmony harmony = CurrentHarmony;
            DisableTreeMovementPatches(harmony);
#if ENABLETREEANARCHY
            UnpatchPTA(harmony);
            DisableTreeAnarchyPatches(harmony);
#endif
            if (isMoveItInstalled) {
                DisableMoveItSnappingPatches(harmony);
            }
            DisableMoveItTreeVariationPatches(harmony);
        }
    }
}
