using ColossalFramework;
using System;
using System.Reflection;
using UnityEngine;

namespace TreeAnarchy {
    public unsafe static partial class TAManager {
        public static MaterialPropertyBlock m_materialBlock;
        public static PrefabCollection<TreeInfo>.PrefabData[] m_simulationPrefabs;

        public static void Initialize() {
            m_materialBlock = Singleton<TreeManager>.instance.m_materialBlock;
            m_simulationPrefabs = ((FastList<PrefabCollection<TreeInfo>.PrefabData>)typeof(PrefabCollection<TreeInfo>).GetField("m_simulationPrefabs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).m_buffer;
        }

        public static void EnsureCapacity(TreeManager manager) {
            if (manager.m_trees.m_buffer.Length != TAMod.MaxTreeLimit) {
                manager.m_trees = new Array32<TreeInstance>((uint)TAMod.MaxTreeLimit);
                manager.m_updatedTrees = new ulong[TAMod.MaxTreeUpdateLimit];
                Array.Clear(manager.m_trees.m_buffer, 0, manager.m_trees.m_buffer.Length);
                manager.m_trees.CreateItem(out uint _);
                SetScaleBuffer(TAMod.MaxTreeLimit);
#if ENABLETERRAINCOFNORM
                SingletonLite<TAManager>.instance.SetTCBuffer(MaxTreeLimit);
#endif
            }
            manager.SetResolution(TAMod.TreeLODSelectedResolution);
        }
    }
}
