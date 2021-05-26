using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace TreeAnarchy.Patches
{
    internal static class BuildingDecorationPatcher
    {
        internal static void PatchBuildingDecoration(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.SaveProps)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(BuildingDecorationPatcher), nameof(BuildingDecorationPatcher.SavePropsPostfix))));
            harmony.Patch(AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.LoadProps)),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(BuildingDecorationPatcher), nameof(BuildingDecorationPatcher.LoadPropsPostfix))));
        }

        private static void SavePropsPostfix(BuildingInfo info, ushort buildingID, ref Building data)
        {
            Debug.Log("TreeAnarchy: SaveProps called!!!!");
        }

        private static void LoadPropsPostfix(BuildingInfo info, ushort buildingID, ref Building data)
        {
            Debug.Log("TreeAnarchy: LoadProps called!!!!");
        }
    }
}
