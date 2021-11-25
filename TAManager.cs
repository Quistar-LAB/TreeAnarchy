using ColossalFramework;
using System;
using System.Reflection;
using UnityEngine;
using System.Reflection.Emit;

namespace TreeAnarchy {
    public unsafe static partial class TAManager {
        public static MaterialPropertyBlock m_materialBlock;
        internal delegate U RefGetter<U>();
        internal static RefGetter<FastList<PrefabCollection<TreeInfo>.PrefabData>> GetSimPrefabs;
        internal static RefGetter<U> CreatePrefabRefGetter<U>(string s_field) {
            var prefab = typeof(PrefabCollection<TreeInfo>);
            var fi = prefab.GetField(s_field, BindingFlags.NonPublic | BindingFlags.Static);
            if (fi == null) throw new MissingFieldException(prefab.Name, s_field);
            var s_name = "__refget_" + prefab.Name + "_fi_" + fi.Name;
            var dm = new DynamicMethod(s_name, typeof(U), new[] { prefab }, prefab, true);
            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, fi);
            il.Emit(OpCodes.Ret);
            return (RefGetter<U>)dm.CreateDelegate(typeof(RefGetter<U>));
        }

        public static void Initialize() {
            m_materialBlock = Singleton<TreeManager>.instance.m_materialBlock;
            GetSimPrefabs = CreatePrefabRefGetter<FastList<PrefabCollection<TreeInfo>.PrefabData>>("m_simulationPrefabs");
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
