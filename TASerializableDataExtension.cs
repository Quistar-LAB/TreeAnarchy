using System;
using System.IO;
using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.IO;
using static TreeAnarchy.TAConfig;
using static TreeAnarchy.TAOldDataSerializer;

namespace TreeAnarchy {
    public class TASerializableDataExtension : ISerializableDataExtension {
        private enum Format : uint {
            Version4 = 4
        }

        static private ISerializableData m_Serializer = null;
        private const string TREE_ANARCHY_KEY = @"TreeAnarchy";
        internal class OldDataSerializer : TAOldDataSerializer {
            public OldDataSerializer(byte[] data) : base(data) {}
            public override void AfterDeserialize() {
            }
        }

        public class Data : IDataContainer {
            public void Deserialize(DataSerializer s) {
                TreeInstance[] trees = Singleton<TreeManager>.instance.m_trees.m_buffer;
                int maxLen = s.ReadInt32(); // Read in Max limit
                if(maxLen > MaxTreeLimit) {
                    maxLen = MaxTreeLimit;
                }
                EncodedArray.UShort uShort = EncodedArray.UShort.BeginRead(s);
                for (int i = DefaultTreeLimit; i < maxLen; i++) {
                    TreeInstance.Flags m_flags = (TreeInstance.Flags)uShort.Read();
                    m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                    trees[i].m_flags = (ushort)m_flags;
                }
                uShort.EndRead();
                PrefabCollection<TreeInfo>.BeginDeserialize(s);
                for (int i = DefaultTreeLimit; i < maxLen; i++) {
                    if (trees[i].m_flags != 0) {
                        trees[i].m_infoIndex = (ushort)PrefabCollection<TreeInfo>.Deserialize(true);
                    }
                }
                PrefabCollection<TreeInfo>.EndDeserialize(s);
                EncodedArray.Short @short = EncodedArray.Short.BeginRead(s);
                for (int i = DefaultTreeLimit; i < maxLen; i++) {
                    if (trees[i].m_flags != 0) {
                        trees[i].m_posX = @short.Read();
                    } else {
                        trees[i].m_posX = 0;
                    }
                }
                @short.EndRead();
                EncodedArray.Short @short1 = EncodedArray.Short.BeginRead(s);
                for (int i = DefaultTreeLimit; i < maxLen; i++) {
                    if (trees[i].m_flags != 0) {
                        trees[i].m_posZ = @short1.Read();
                    } else {
                        trees[i].m_posZ = 0;
                    }
                }
                @short1.EndRead();
                uShort = EncodedArray.UShort.BeginRead(s);
                for (int i = 1; i < maxLen; i++) {
                    if ((trees[i].m_flags & (ushort)TreeInstance.Flags.FixedHeight) != 0) {
                        trees[i].m_posY = uShort.Read();
                    } else {
                        trees[i].m_posY = 0;
                    }
                }
                uShort.EndRead();
            }

            public void AfterDeserialize(DataSerializer s) {
            }

            public void Serialize(DataSerializer s) {
                int treeLimit = MaxTreeLimit;
                TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;

                // Set header information now. Header is saved now for compatibility reasons
                s.WriteInt32(treeLimit);

                /* Apparently, the trees could be located anywhere in the buffer
                 * even if there's only 1 tree in the buffer. I'm assuming this is
                 * due to performance concerns.
                 * So have to save the entire buffer.
                 */
                EncodedArray.UShort uShort = EncodedArray.UShort.BeginWrite(s);
                for (int i = DefaultTreeLimit; i < treeLimit; i++) {
                    uShort.Write(buffer[i].m_flags);
                }
                uShort.EndWrite();
                try {
                    PrefabCollection<TreeInfo>.BeginSerialize(s);
                    for (int i = DefaultTreeLimit; i < treeLimit; i++) {
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
                EncodedArray.Short @short1 = EncodedArray.Short.BeginWrite(s);
                for (int i = DefaultTreeLimit; i < treeLimit; i++) {
                    if (buffer[i].m_flags != 0) {
                        @short1.Write(buffer[i].m_posZ);
                    }
                }
                @short1.EndWrite();
                uShort = EncodedArray.UShort.BeginWrite(s);
                for (int i = 1; i < treeLimit; i++) {
                    if ((buffer[i].m_flags & (ushort)TreeInstance.Flags.FixedHeight) != 0) {
                        uShort.Write(buffer[i].m_posY);
                    }
                }
                uShort.EndWrite();

#if FALSE // Burning tree handled in prefix method
            // Let original codes handle the extra burning tree list. Hope it works
            WriteUInt24(s, (uint)instance.m_burningTrees.m_size);
            for (int m = 0; m < instance.m_burningTrees.m_size; m++)
            {
                WriteUInt24(s, instance.m_burningTrees.m_buffer[m].m_treeIndex);
                WriteUShort(s, instance.m_burningTrees.m_buffer[m].m_fireIntensity);
                WriteUShort(s, instance.m_burningTrees.m_buffer[m].m_fireDamage);
            }
#endif
            }
        }

        public void OnCreated(ISerializableData s) => m_Serializer = s;

        public void OnReleased() => m_Serializer = null;

        public static void PurgeOldData() => m_Serializer?.EraseData(OldTreeUnlimiterKey);

        public void OnLoadData() {
        }

        public static void IntegratedDeserialize() {
            try { /* Try find old data version first */
                if (Singleton<SimulationManager>.instance.m_serializableDataStorage.ContainsKey(OldTreeUnlimiterKey)) {
                    byte[] oldData = Singleton<SimulationManager>.instance.m_serializableDataStorage[OldTreeUnlimiterKey];
                    if (oldData != null) {
                        if (oldData.Length < 2 || oldData.Length % 2 != 0) {
                            Debug.Log("TreeAnarchy: Invalid Old Data, Not Loading Tree Data");
                            return;
                        }
                        using OldDataSerializer oldSerializer = new OldDataSerializer(oldData);
                        if (oldSerializer.Deserialize(Singleton<TreeManager>.instance.m_trees.m_buffer, out ErrorFlags errors)) {
                            if ((errors & ErrorFlags.OLDFORMAT) != ErrorFlags.NONE) {
                                Debug.Log("TreeAnarchy: Old Format Loaded");
                                OldFormatLoaded = true;
                            }
                        } else {
                            Debug.Log("TreeAnarchy: Invalid Data Format");
                        }
                        return;
                    }
                }
                // Work on our new data format
                if (Singleton<SimulationManager>.instance.m_serializableDataStorage.TryGetValue(TREE_ANARCHY_KEY, out byte[] data)) {
                    if (data is null) {
                        Debug.Log("TreeAnarchy: No extra trees to load");
                        return;
                    }
                    using (var stream = new MemoryStream(data)) {
                        DataSerializer.Deserialize<Data>(stream, DataSerializer.Mode.Memory);
                    }
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public void OnSaveData() {
            try {
                byte[] data;
                m_Serializer?.EraseData(OldTreeUnlimiterKey);
                using (var stream = new MemoryStream()) {
                    DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, (uint)Format.Version4, new Data());
                    data = stream.ToArray();
                }
                m_Serializer.SaveData(TREE_ANARCHY_KEY, data);
                Debug.Log($"TreeAnarchy: Saved {data.Length} bytes of data");
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }
    }
}
