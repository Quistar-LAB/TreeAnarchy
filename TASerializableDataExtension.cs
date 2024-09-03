using ColossalFramework;
using ColossalFramework.IO;
using HarmonyLib;
using ICities;
using System;
using System.IO;
using System.Threading;
using UnityEngine;
using static TreeAnarchy.TAMod;
using static TreeManager;
using PrefabData = PrefabCollection<TreeInfo>.PrefabData;

namespace TreeAnarchy {
    public class TASerializableDataExtension : ISerializableDataExtension {
        private enum Format : uint {
            Version4 = 4,
            Version5,
            Version6,
            Version7,
        }

        private const string TREE_ANARCHY_KEY = @"TreeAnarchy";

        private class Data : IDataContainer {
            private const ushort fireDamageBurningMask = unchecked((ushort)~(TreeInstance.Flags.Burning | TreeInstance.Flags.FireDamage));
            private static EncodedArray.UShort m_encodedArray;
            private static FastList<PrefabData> m_simulationPrefabs;

            private void UpdateTreeLimit(int newSize) {
                TreeScaleFactor = newSize / DefaultTreeLimit;
                SaveSettings();
            }

            private void BeginDeserializeInfos(DataSerializer serializer) {
                // Set read array.
                m_encodedArray = EncodedArray.UShort.BeginRead(serializer);

                // Get simulationPrefabs fastlist.
                m_simulationPrefabs = AccessTools.Field(typeof(PrefabCollection<TreeInfo>), "m_simulationPrefabs").GetValue(null) as FastList<PrefabCollection<TreeInfo>.PrefabData>;
            }

            private ushort DeserializeInfo() {
                // Read prefab index.
                uint prefabIndex = m_encodedArray.Read();

                // Check for new index.
                if ((int)prefabIndex >= m_simulationPrefabs.m_size) {
                    int simPrefabsLength = 0;
                    if (m_simulationPrefabs.m_buffer != null) {
                        simPrefabsLength = m_simulationPrefabs.m_buffer.Length;
                    }

                    // Expand simulation prefab fastlist length if required.
                    if ((int)prefabIndex >= simPrefabsLength) {
                        int capacity = Mathf.Max(Mathf.Max((int)(prefabIndex + 1), 32), simPrefabsLength << 1);
                        m_simulationPrefabs.SetCapacity(capacity);
                    }

                    m_simulationPrefabs.m_size = (int)(prefabIndex + 1);
                }

                // Update simulation prefab reference count.
                m_simulationPrefabs.m_buffer[prefabIndex].m_refcount++;

                return (ushort)prefabIndex;
            }

            private void EndDeserializeInfos(DataSerializer serializer) {
                // Close off array reading.
                m_encodedArray.EndRead();
                m_encodedArray = null;

                // Read prefab names.
                int numEncodedNames = (int)serializer.ReadUInt16();
                PrefabData item = default;
                for (int i = 0; i < numEncodedNames; ++i) {
                    // Check for existing info reference.
                    if (i < m_simulationPrefabs.m_size) {
                        // Existing info reference - populate the name, but only if it hasn't already been populated (don't overwrite).
                        string prefabName = serializer.ReadUniqueString();
                        if (m_simulationPrefabs.m_buffer[i].m_name == null) {
                            m_simulationPrefabs.m_buffer[i].m_name = prefabName;
                        }

                        continue;
                    }

                    // New reference.
                    item.m_name = serializer.ReadUniqueString();
                    item.m_refcount = 0;
                    item.m_prefab = null;
                    item.m_replaced = false;
                    m_simulationPrefabs.Add(item);
                }
            }

            public void Deserialize(DataSerializer s) {
                TreeManager treeManager = Singleton<TreeManager>.instance;
                int savedBufferSize = s.ReadInt32(); // Read in Max limit

                if (savedBufferSize <= MAX_TREE_COUNT) {
                    TALog("Invalid extended tree buffer size detected; aborting");
                    return;
                }
                uint newBufferSize = (uint)Math.Max(savedBufferSize, MaxTreeLimit);

                Array32<TreeInstance> newTreeArray = new Array32<TreeInstance>(newBufferSize);
                TreeInstance[] newTreeBuffer = newTreeArray.m_buffer;

                // Initialize Array32 by creating zero (null) item and resetting unused count to zero (unused count will be recalculated after data population).
                newTreeArray.CreateItem(out uint _);
                newTreeArray.ClearUnused();

                EncodedArray.UShort uShort = EncodedArray.UShort.BeginRead(s);
                for (int i = DefaultTreeLimit; i < savedBufferSize; ++i) {
                    TreeInstance.Flags flag = (TreeInstance.Flags)uShort.Read();
                    flag &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                    newTreeBuffer[i].m_flags = (ushort)flag;
                }
                uShort.EndRead();

                // Tree prefab indexes.
                BeginDeserializeInfos(s);
                for (int i = DefaultTreeLimit; i < savedBufferSize; ++i) {
                    if (newTreeBuffer[i].m_flags != 0) {
                        newTreeBuffer[i].m_infoIndex = DeserializeInfo();
                    }
                }
                EndDeserializeInfos(s);

                EncodedArray.Short @short = EncodedArray.Short.BeginRead(s);
                for (int i = DefaultTreeLimit; i < savedBufferSize; i++) {
                    if (newTreeBuffer[i].m_flags != 0) {
                        newTreeBuffer[i].m_posX = @short.Read();
                    } else {
                        newTreeBuffer[i].m_posX = 0;
                    }
                }
                @short.EndRead();
                EncodedArray.Short @short1 = EncodedArray.Short.BeginRead(s);
                for (int i = DefaultTreeLimit; i < savedBufferSize; i++) {
                    if (newTreeBuffer[i].m_flags != 0) {
                        newTreeBuffer[i].m_posZ = @short1.Read();
                    } else {
                        newTreeBuffer[i].m_posZ = 0;
                    }
                }
                @short1.EndRead();
                EncodedArray.UShort uShort1 = EncodedArray.UShort.BeginRead(s);
                for (int i = 1; i < savedBufferSize; i++) {
                    if ((newTreeBuffer[i].m_flags & (ushort)TreeInstance.Flags.FixedHeight) != 0) {
                        newTreeBuffer[i].m_posY = uShort1.Read();
                    }
                }
                uShort1.EndRead();

                FastList<BurningTree> burningTrees = Singleton<TreeManager>.instance.m_burningTrees;
                burningTrees.Clear();

                BurningTree burningTree = default;
                int burningListSize = (int)s.ReadUInt24();
                treeManager.m_burningTrees.EnsureCapacity(burningListSize);
                for (int i = 0; i < burningListSize; ++i) {
                    burningTree.m_treeIndex = s.ReadUInt24();
                    burningTree.m_fireIntensity = (byte)s.ReadUInt8();
                    burningTree.m_fireDamage = (byte)s.ReadUInt8();
                    uint treeIndex = burningTree.m_treeIndex;
                    if (treeIndex != 0u) {
                        burningTrees.Add(burningTree);
                        newTreeBuffer[treeIndex].m_flags |= 64;
                        if (burningTree.m_fireIntensity != 0) {
                            newTreeBuffer[treeIndex].m_flags |= 128;
                        }
                    }
                }

                // copy over default tree buffer to new buffer
                TreeInstance[] oldTreeBuffer = treeManager.m_trees.m_buffer;
                for(int i = 1; i < DefaultTreeLimit; ++i) {
                    ushort flags = oldTreeBuffer[i].m_flags;
                    if(flags != 0) {
                        newTreeBuffer[i].m_flags = flags;
                        newTreeBuffer[i].m_infoIndex = oldTreeBuffer[i].m_infoIndex;
                        newTreeBuffer[i].m_posX = oldTreeBuffer[i].m_posX;
                        newTreeBuffer[i].m_posZ = oldTreeBuffer[i].m_posZ;
                    }
                }
                // Assign new array and create new updated tree array.
                treeManager.m_trees = newTreeArray;
                int arraySize = newTreeArray.m_buffer.Length;
                treeManager.m_updatedTrees = new ulong[arraySize >> 6];
            }

            public void AfterDeserialize(DataSerializer s) { }

            public void Serialize(DataSerializer s) {
                int treeLimit = MaxTreeLimit;
                TreeManager treeManager = Singleton<TreeManager>.instance;
                TreeInstance[] buffer = treeManager.m_trees.m_buffer;
                TAManager.ExtraTreeInfo[] extraInfos = TAManager.m_extraTreeInfos;

                // Important to save treelimit as it is an adjustable variable on every load
                s.WriteInt32(treeLimit);

                /* Apparently, the trees could be located anywhere in the buffer
                 * even if there's only 1 tree in the buffer. I'm assuming this is
                 * due to performance concerns.
                 * So have to iterate through the entire buffer.
                 */
                EncodedArray.UShort uShort = EncodedArray.UShort.BeginWrite(s);
                for (int i = DefaultTreeLimit; i < treeLimit; ++i) {
                    uShort.Write(buffer[i].m_flags);
                }
                uShort.EndWrite();
                try {
                    PrefabCollection<TreeInfo>.BeginSerialize(s);
                    for (int i = DefaultTreeLimit; i < treeLimit; ++i) {
                        if (buffer[i].m_flags != 0) {
                            PrefabCollection<TreeInfo>.Serialize(buffer[i].m_infoIndex);
                        }
                    }
                } finally {
                    PrefabCollection<TreeInfo>.EndSerialize(s);
                }
                EncodedArray.Short @short = EncodedArray.Short.BeginWrite(s);
                for (int i = DefaultTreeLimit; i < treeLimit; i++) {
                    if (buffer[i].m_flags != 0) {
                        @short.Write(buffer[i].m_posX);
                    }
                }
                @short.EndWrite();
                @short = EncodedArray.Short.BeginWrite(s);
                for (int i = DefaultTreeLimit; i < treeLimit; i++) {
                    if (buffer[i].m_flags != 0) {
                        @short.Write(buffer[i].m_posZ);
                    }
                }
                @short.EndWrite();
                uShort = EncodedArray.UShort.BeginWrite(s);
                for (int i = 1; i < treeLimit; i++) {
                    if ((buffer[i].m_flags & (ushort)TreeInstance.Flags.FixedHeight) != 0) {
                        uShort.Write(buffer[i].m_posY);
                    }
                }
                uShort.EndWrite();
                EncodedArray.Float @float = EncodedArray.Float.BeginWrite(s);
                for (int i = 1; i < treeLimit; i++) {
                    if (buffer[i].m_flags != 0) {
                        @float.Write(extraInfos[i].m_extraScale);
                    }
                }
                @float.EndWrite();

                // Burning trees.
                FastList<BurningTree> burningTrees = treeManager.m_burningTrees;
                uint burningTreesSize = (uint)burningTrees.m_size;
                BurningTree[] burningTreeBuffer = burningTrees.m_buffer;

                s.WriteUInt24(burningTreesSize);
                for (int i = 0; i < burningTreesSize; ++i) {
                    s.WriteUInt24(burningTreeBuffer[i].m_treeIndex);
                    s.WriteUInt8(burningTreeBuffer[i].m_fireIntensity);
                    s.WriteUInt8(burningTreeBuffer[i].m_fireDamage);
                }
            }
        }

        public void OnCreated(ISerializableData s) { }

        public void OnReleased() { }

        public void OnLoadData() { }

        private static void ClearBurningTrees() {
            Singleton<TreeManager>.instance.m_burningTrees.Clear();
        }

        public static void IntegratedDeserialize(TreeInstance[] trees) {
            try {
                if (Singleton<SimulationManager>.instance.m_serializableDataStorage.TryGetValue(TREE_ANARCHY_KEY, out byte[] data)) {
                    if (data is null) {
                        TALog("No extra trees to load");
                        return;
                    }
                    using (MemoryStream stream = new MemoryStream(data)) {
                        DataSerializer.Deserialize<Data>(stream, DataSerializer.Mode.Memory);
                    }
                } else {
                    //for (int i = DefaultTreeLimit; i < trees.Length; i++) {
                    //    trees[i].m_flags = 0;
                    //}
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public void OnSaveData() {
            try {
                byte[] data;
                using (var stream = new MemoryStream()) {
                    DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, (uint)Format.Version7, new Data());
                    data = stream.ToArray();
                }
                SaveData(TREE_ANARCHY_KEY, data);
                TALog($"Saved {data.Length} bytes of data");
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void SaveData(string id, byte[] data) {
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            while (!Monitor.TryEnter(smInstance.m_serializableDataStorage, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
            try {
                smInstance.m_serializableDataStorage[id] = data;
            } finally {
                Monitor.Exit(smInstance.m_serializableDataStorage);
            }
        }

        private void EraseData(string id) {
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            while (!Monitor.TryEnter(smInstance.m_serializableDataStorage, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
            try {
                if (smInstance.m_serializableDataStorage.ContainsKey(id)) {
                    smInstance.m_serializableDataStorage.Remove(id);
                }
            } finally {
                Monitor.Exit(smInstance.m_serializableDataStorage);
            }
        }
    }
}
