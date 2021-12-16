using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using HarmonyLib;
using System;
using System.Reflection;
using System.Text;
using System.Threading;
using TreeAnarchy.Patches;

namespace TreeAnarchy {
    internal static partial class TAPatcher {
        private const string HARMONYID = @"quistar.treeanarchy.mod";
        private const ulong PTAID = 593588108uL;
        internal static bool IsPTAPatched { get; set; } = false;
        internal static bool IsPTAInstalled { get; set; } = false;
        internal static FieldInfo MoveItUseTreeSnap { get; set; } = null;
        internal static bool IsMoveItInstalled { get; set; } = false;
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
            new ModInfo(2153618633, @"Prop Switcher", false),
            new ModInfo(1869561285, @"Prop Painter", false),
            new ModInfo(910440715, @"Plant Scaling", false),
            //new ModInfo(2584051448, @"Tree Anarchy Beta", false)
        };

        internal static bool IsPluginExists(ulong id, string name) {
            foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                if ((info.publishedFileID.AsUInt64 == id || info.ToString().Contains(name)) && info.isEnabled) return true;
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

        private static bool CheckMoveItTreeSnapSig() {
            FieldInfo treeSnapField = typeof(MoveIt.MoveItTool).GetField(@"treeSnapping", BindingFlags.GetField | BindingFlags.Static | BindingFlags.Public);
            if (treeSnapField != null) {
                MoveItUseTreeSnap = treeSnapField;
                MoveItUseTreeSnap.SetValue(null, TAMod.UseTreeSnapping);
                IsMoveItInstalled = true;
                return true;
            }
            return false;
        }

        internal static bool CheckIncompatibleMods() {
            StringBuilder errorMsg = new StringBuilder();
            foreach (var mod in PlatformService.workshop.GetSubscribedItems()) {
                for (int j = 0; j < IncompatibleMods.Length; j++) {
                    if (mod.AsUInt64 == IncompatibleMods[j].fileID) {
                        errorMsg.AppendLine('[' + IncompatibleMods[j].name + ']' + @"detected. " +
                            (IncompatibleMods[j].inclusive ? @"Tree Anarchy already includes the same functionality" : @"This mod is incompatible with Tree Anarchy"));
                        TAMod.TALog(@"Incompatible mod: [" + IncompatibleMods[j].name + @"] detected");
                    }
                }
                if (mod.AsUInt64 == PTAID) {
                    IsPTAInstalled = true;
                }
            }
            if (errorMsg.Length > 0) {
                UIView.ForwardException(new Exception(@"Tree Anarchy detected incompatible mods, please remove the following mentioned mods as the same functionality is already built into this mod", new Exception("\n" + errorMsg)));
                TAMod.TALog($"Tree Anarchy detected incompatible mods, please remove the following mentioned mods\n{errorMsg}");
                return false;
            }
            return true;
        }

        private static void PollForPTA(object _) {
            int counter = 0;
            Harmony harmony = new Harmony(HARMONYID);
            while (counter < 60) {
                Thread.Sleep(10000);
                foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                    if (info.publishedFileID.AsUInt64 == 593588108uL && info.isEnabled) {
                        TAMod.TALog($"Found PTA");
                        PatchPTA(harmony);
                        IsPTAPatched = true;
                        break;
                    }
                    counter++;
                }
            }
        }

        internal static void EnableCore() {
            Harmony harmony = new Harmony(HARMONYID);
            TreeInstancePatches.EnableTreeInstancePatch(harmony);
            TreeManagerPatches.EnableTreeManagerPatch(harmony);
            EnableTreeLimitPatches(harmony);
            EnableTreeSnappingPatches(harmony);
            EnableTreeVariationPatches(harmony);
            if (IsPTAInstalled) ThreadPool.QueueUserWorkItem(PollForPTA);
        }

        internal static void DisableCore() {
            Harmony harmony = new Harmony(HARMONYID);
            TreeInstancePatches.DisableTreeInstancePatch(harmony, HARMONYID);
            TreeManagerPatches.DisableTreeManagerPatch(harmony, HARMONYID);
            DisableTreeLimitPatches(harmony);
            DisableTreeSnappingPatches(harmony);
            DisableTreeVariationPatches(harmony);
        }

        internal static void LateEnable() {
            Harmony harmony = new Harmony(HARMONYID);
            EnableTreeMovementPatches(harmony);
            EnableTreeAnarchyPatches(harmony);
            if (IsPluginExists(1619685021, @"MoveIt") || IsPluginExists(2215771668, @"MoveIt")) {
                if (!CheckMoveItTreeSnapSig()) {
                    PatchMoveItSnapping(harmony);
                }
                PatchMoveItTreeVariation(harmony);
            }
        }

        internal static void DisableLatePatch() {
            Harmony harmony = new Harmony(HARMONYID);
            DisableTreeMovementPatches(harmony);
            if (IsPTAPatched) {
                UnpatchPTA(harmony);
                IsPTAPatched = false;
            }
            DisableTreeAnarchyPatches(harmony);
            if (IsMoveItInstalled) {
                DisableMoveItSnappingPatches(harmony);
            }
            DisableMoveItTreeVariationPatches(harmony);
        }
    }
}
