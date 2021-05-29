#define FULLVERBOSE
#define QUIETVERBOSE
#define SILENT
#undef FULLVERBOSE
#if SILENT
#undef DEBUG
#undef FULLVERBOSE
#undef QUIETVERBOSE
#endif

using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.IO;
using static TreeAnarchy.TAConfig;
using static TreeAnarchy.Patches.Patcher;

namespace TreeAnarchy.Patches
{
    internal static class TreeManagerDataPatcher
    {
        internal static void PatchTreeManagerData(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Deserialize)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerDataPatcher), nameof(TreeManagerDataPatcher.DataDeserializePrefix))));
//            harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.AfterDeserialize)),
//                postfix: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerDataPatcher), nameof(TreeManagerDataPatcher.AfterDeserializePostfix))));
            harmony.Patch(AccessTools.Method(typeof(TreeManager.Data), nameof(TreeManager.Data.Serialize)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(TreeManagerDataPatcher), nameof(TreeManagerDataPatcher.SerializePrefix))));
        }

        private static bool DataDeserializePrefix(DataSerializer s)
        {
            EnsureCapacity();
            TreeManager treeManager = Singleton<TreeManager>.instance;
            TreeInstance[] buffer = treeManager.m_trees.m_buffer;
            uint[] treeGrid = treeManager.m_treeGrid;
            int buflen = DefaultTreeLimit;
            int gridlen = treeGrid.Length;

            Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginDeserialize(s, "TreeManager");
            treeManager.m_trees.ClearUnused();
            treeManager.m_burningTrees.Clear();
            if ((treeManager.m_burningTrees.m_buffer == null) == false && treeManager.m_burningTrees.m_buffer.Length > 128)
            {
                treeManager.m_burningTrees.Trim();
            }
            SimulationManager.UpdateMode updateMode = Singleton<SimulationManager>.instance.m_metaData.m_updateMode;
            bool assetEditor = updateMode == SimulationManager.UpdateMode.NewAsset || updateMode == SimulationManager.UpdateMode.LoadAsset;
            for (int i = 0; i < gridlen; i++)
            {
                treeGrid[i] = 0u;
            }
            EncodedArray.UShort uShort = EncodedArray.UShort.BeginRead(s);
            for (int i = 1; i < buflen; i++)
            {
                TreeInstance.Flags flags = (TreeInstance.Flags)uShort.Read();
                flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                buffer[i].m_flags = (ushort)flags;
            }
            uShort.EndRead();
            PrefabCollection<TreeInfo>.BeginDeserialize(s);
            for (int i = 1; i < buflen; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    buffer[i].m_infoIndex = (ushort)PrefabCollection<TreeInfo>.Deserialize(true);
                }
            }
            PrefabCollection<TreeInfo>.EndDeserialize(s);
            EncodedArray.Short @short = EncodedArray.Short.BeginRead(s);
            for (int i = 1; i < buflen; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    buffer[i].m_posX = @short.Read();
                }
                else
                {
                    buffer[i].m_posX = 0;
                }
            }
            @short.EndRead();
            EncodedArray.Short short2 = EncodedArray.Short.BeginRead(s);
            for (int i = 1; i < buflen; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    buffer[i].m_posZ = short2.Read();
                }
                else
                {
                    buffer[i].m_posZ = 0;
                }
                buffer[i].m_posY = 0; // Set posY to 0 to prevent trees from floating
            }
            short2.EndRead();
            if (s.version >= 266u)
            {
                int burntreelen = (int)s.ReadUInt24();
                treeManager.m_burningTrees.EnsureCapacity(burntreelen);
                for (int n = 0; n < burntreelen; n++)
                {
                    TreeManager.BurningTree item;
                    item.m_treeIndex = s.ReadUInt24();
                    item.m_fireIntensity = (byte)s.ReadUInt8();
                    item.m_fireDamage = (byte)s.ReadUInt8();
                    if (item.m_treeIndex != 0u)
                    {
                        treeManager.m_burningTrees.Add(item);
                        buffer[item.m_treeIndex].m_flags = (ushort)(buffer[item.m_treeIndex].m_flags | 64);
                        if (item.m_fireIntensity != 0)
                        {
                            buffer[item.m_treeIndex].m_flags = (ushort)(buffer[item.m_treeIndex].m_flags | 128);
                        }
                    }
                }
            }

            if(UseModifiedTreeCap) TASerializableDataExtension.Deserialize();

            int MaxTreeLen = MaxTreeLimit;
            for (uint i = 1; i < MaxTreeLen; i++)
            {
                buffer[i].m_nextGridTree = 0u;
                if (buffer[i].m_flags != 0)
                {
                    InitializeTree(Singleton<TreeManager>.instance, i, ref buffer[i], assetEditor);
                }
                else
                {
                    Singleton<TreeManager>.instance.m_trees.ReleaseItem(i);
                }
            }
            Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndDeserialize(s, "TreeManager");
            return false;
        }

        private static void AfterDeserializePostfix(DataSerializer s)
        {
            using(TASerializableDataExtension.DataSerializer serializer = new TASerializableDataExtension.DataSerializer())
            {
                serializer.AfterDeserialize(s);
            }
        }

        private static bool SerializePrefix(DataSerializer s)
        {
            Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.BeginSerialize(s, "TreeManager");
            TreeManager instance = Singleton<TreeManager>.instance;
            TreeInstance[] buffer = instance.m_trees.m_buffer;
            uint maxTrees = DefaultTreeLimit;
            EncodedArray.UShort uShort = EncodedArray.UShort.BeginWrite(s);
            for (int i = 1; i < maxTrees; i++)
            {
                uShort.Write(buffer[i].m_flags);
            }
            uShort.EndWrite();
            try
            {
                PrefabCollection<TreeInfo>.BeginSerialize(s);
                for (int i = 1; i < maxTrees; i++)
                {
                    if (buffer[i].m_flags != 0)
                    {
                        PrefabCollection<TreeInfo>.Serialize((uint)buffer[i].m_infoIndex);
                    }
                }
            }
            finally
            {
                PrefabCollection<TreeInfo>.EndSerialize(s);
            }
            EncodedArray.Short @short = EncodedArray.Short.BeginWrite(s);
            for (int i = 1; i < maxTrees; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    @short.Write(buffer[i].m_posX);
                }
            }
            @short.EndWrite();
            EncodedArray.Short short2 = EncodedArray.Short.BeginWrite(s);
            for (int i = 1; i < maxTrees; i++)
            {
                if (buffer[i].m_flags != 0)
                {
                    short2.Write(buffer[i].m_posZ);
                }
            }
            short2.EndWrite();
            s.WriteUInt24((uint)instance.m_burningTrees.m_size);
            for (int m = 0; m < instance.m_burningTrees.m_size; m++)
            {
                s.WriteUInt24(instance.m_burningTrees.m_buffer[m].m_treeIndex);
                s.WriteUInt8((uint)instance.m_burningTrees.m_buffer[m].m_fireIntensity);
                s.WriteUInt8((uint)instance.m_burningTrees.m_buffer[m].m_fireDamage);
            }
            Singleton<LoadingManager>.instance.m_loadingProfilerSimulation.EndSerialize(s, "TreeManager");
            return false; // don't run original codes
        }

        /* A copy of TreeManager::InitializeTree() so it can be called locally since it was declared
         * private in TreeManager. This is called inside a loop and would be expensive to use the Invoke
         * method
         */
        private static void InitializeTree(TreeManager tm, uint tree, ref global::TreeInstance data, bool assetEditor)
        {
            int num;
            int num2;
            if (assetEditor)
            {
                num = Mathf.Clamp(((int)(data.m_posX / 16) + 32768) * 540 / 65536, 0, 539);
                num2 = Mathf.Clamp(((int)(data.m_posZ / 16) + 32768) * 540 / 65536, 0, 539);
            }
            else
            {
                num = Mathf.Clamp(((int)data.m_posX + 32768) * 540 / 65536, 0, 539);
                num2 = Mathf.Clamp(((int)data.m_posZ + 32768) * 540 / 65536, 0, 539);
            }
            int num3 = num2 * 540 + num;
            while (!Monitor.TryEnter(tm.m_treeGrid, SimulationManager.SYNCHRONIZE_TIMEOUT))
            {
            }
            try
            {
                tm.m_trees.m_buffer[tree].m_nextGridTree = tm.m_treeGrid[num3];
                tm.m_treeGrid[num3] = tree;
            }
            finally
            {
                Monitor.Exit(tm.m_treeGrid);
            }
        }

    }
}
