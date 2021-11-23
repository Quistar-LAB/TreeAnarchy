using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using HarmonyLib;
using System;
using System.Threading;
using System.Reflection;

namespace TreeAnarchy {
    internal partial class TAPatcher : SingletonLite<TAPatcher> {
        private const string HARMONYID = @"quistar.treeanarchy.mod";
        private const ulong PTAID = 593588108uL;
        private bool isPTAPatched = false;
        private bool isPTAInstalled = false;
        private Harmony m_harmony;
        internal Harmony CurrentHarmony {
            get => (m_harmony is null) ? m_harmony = new Harmony(HARMONYID) : m_harmony;
        }
        internal static FieldInfo MoveItUseTreeSnap = null;
        internal static bool isMoveItInstalled = false;
        private readonly struct ModInfo {
            public readonly ulong fileID;
            public readonly string name;
            public readonly bool inclusive;
            public ModInfo(ulong modID, string modName, bool isInclusive) {
                fileID = modID;
                name = modName;
                inclusive = isInclusive;
            }
        }
        private static readonly ModInfo[] IncompatibleMods = new ModInfo[] {
            new ModInfo(455403039, @"Unlimited Trees Mod", true),
            new ModInfo(2378914031, @"Unlimited Trees Revisited", true),
            new ModInfo(869134690, @"Tree Snapping", true),
            new ModInfo(1637106958, @"Lock Forestry", true),
            new ModInfo(556784825, @"Random Tree Rotation", true),
            new ModInfo(1388613752, @"Tree Movement Control", true),
            new ModInfo(842981708, @"Random Tree Rotation for Natural Disasters", true),
            new ModInfo(1349895184, @"Tree LOD Fix", true),
            new ModInfo(2153618633, @"Prop Switcher", false)
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
            FieldInfo treeSnapField = typeof(MoveIt.MoveItTool).GetField(@"treeSnapping", BindingFlags.GetField | BindingFlags.Static | BindingFlags.Public);
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
                        errorMsg += '[' + IncompatibleMods[i].name + ']' + @"detected. " +
                            (IncompatibleMods[i].inclusive ? @"Tree Anarchy already includes the same functionality" : @"This mod is incompatible with Tree Anarchy");
                        TAMod.TALog(@"Incompatible mod: [" + IncompatibleMods[i].name + @"] detected");
                    } else if(mod.AsUInt64 == PTAID) {
                        isPTAInstalled = true;
                    }
                }
            }
            if (errorMsg.Length > 0) {
                UIView.ForwardException(new Exception(@"Tree Anarchy detected incompatible mods, please remove the following mentioned mods as the same functionality is already built into this mod", new Exception("\n" + errorMsg)));
                TAMod.TALog($"Tree Anarchy detected incompatible mods, please remove the following mentioned mods\n{errorMsg}");
                return false;
            }
            return true;
        }

        private void PollForPTA(object _) {
            int counter = 0;
            Harmony harmony = CurrentHarmony;
            while(counter < 60) {
                Thread.Sleep(10000);
                foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                    if (info.publishedFileID.AsUInt64 == 593588108uL && info.isEnabled) {
                        TAMod.TALog($"Found PTA");
                        PatchPTA(harmony);
                        isPTAPatched = true;
                        break;
                    }
                    counter++;
                }
            }
        }

        internal void EnableCore() {
            Harmony harmony = CurrentHarmony;
            EnableTreeInstancePatch(harmony);
            EnableTreeManagerPatch(harmony);
            EnableTreeLimitPatches(harmony);
            EnableTreeSnappingPatches(harmony);
            EnableTreeVariationPatches(harmony);
            if(isPTAInstalled) ThreadPool.QueueUserWorkItem(PollForPTA);
        }

        internal void DisableCore() {
            Harmony harmony = CurrentHarmony;
            DisableTreeInstancePatch(harmony);
            DisableTreeManagerPatch(harmony);
            DisableTreeLimitPatches(harmony);
            DisableTreeSnappingPatches(harmony);
            DisableTreeVariationPatches(harmony);
        }

        internal void LateEnable() {
            Harmony harmony = CurrentHarmony;
            EnableTreeMovementPatches(harmony);
            EnableTreeAnarchyPatches(harmony);
            if (IsPluginExists(1619685021, @"MoveIt") || IsPluginExists(2215771668, @"MoveIt")) {
                if (!CheckMoveItTreeSnapSig()) {
                    PatchMoveItSnapping(harmony);
                }
                PatchMoveItTreeVariation(harmony);
            }
        }

        internal void DisableLatePatch() {
            Harmony harmony = CurrentHarmony;
            DisableTreeMovementPatches(harmony);
            if (isPTAPatched) {
                UnpatchPTA(harmony);
                isPTAPatched = false;
            }
            DisableTreeAnarchyPatches(harmony);
            if (isMoveItInstalled) {
                DisableMoveItSnappingPatches(harmony);
            }
            DisableMoveItTreeVariationPatches(harmony);
        }
    }
}
