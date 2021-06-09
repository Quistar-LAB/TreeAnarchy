using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TreeAnarchy.Patches;

namespace TreeAnarchy {
    internal static class TAPatcher {
        internal const string HARMONYID = @"quistar.treeanarchy.mod";
        internal static readonly Harmony m_harmony = new Harmony(HARMONYID);
        static readonly TreeLimit m_treeLimit = new TreeLimit();
        static readonly TreeMovement m_treeMovement = new TreeMovement();
        static readonly TreeSnapping m_treeSnapping = new TreeSnapping();
        static readonly TreeManagerData m_data = new TreeManagerData();

        internal static void Enable() {
            m_treeLimit.Ensure(m_harmony);
            m_treeLimit.Enable(m_harmony);
            m_treeMovement.Enable(m_harmony);
            m_treeSnapping.Enable(m_harmony);
            m_data.Enable(m_harmony);
        }

        internal static void EnableExperimental(bool enable) {
            if (enable) TATreeManager.Enable(m_harmony);
            else TATreeManager.Disable(m_harmony);
        }

        internal static void Disable() {
            m_treeLimit.Disable(m_harmony);
        }
    }
}
