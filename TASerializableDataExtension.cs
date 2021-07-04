using ColossalFramework;
using ColossalFramework.IO;
using ICities;
using System;
using System.IO;
using System.Threading;
using UnityEngine;
using static TreeAnarchy.TAMod;
using static TreeAnarchy.TAOldDataSerializer;

namespace TreeAnarchy {
    public class TASerializableDataExtension : ISerializableDataExtension {
        private enum Format : uint {
            Version4 = 4,
            Version5 = 5,
        }

        static private ISerializableData m_Serializer = null;
        private const string TREE_ANARCHY_KEY = @"TreeAnarchy";
        internal class OldDataSerializer : TAOldDataSerializer {
            public OldDataSerializer(byte[] data) : base(data) { }
            public override void AfterDeserialize() {
            }
        }

        private class Data : IDataContainer {
            public void Deserialize(DataSerializer s) {
                ref TreeInstance[] trees = ref Singleton<TreeManager>.instance.m_trees.m_buffer;
                int maxLen = s.ReadInt32(); // Read in Max limit
                if (maxLen > MaxTreeLimit) {
                    maxLen = MaxTreeLimit;
                }
                EncodedArray.UShort uShort = EncodedArray.UShort.BeginRead(s);
                for (int i = DefaultTreeLimit; i < maxLen; i++) {
                    TreeInstance.Flags m_flags = (TreeInstance.Flags)uShort.Read();
                    m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                    trees[i].m_flags = (ushort)m_flags;
                }
                uShort.EndRead();
                int startIndex = 0;
                switch ((Format)s.version) {
                    case Format.Version4:
                    startIndex = DefaultTreeLimit;
                    break;
                    case Format.Version5:
                    startIndex = 1;
                    break;
                }
                PrefabCollection<TreeInfo>.BeginDeserialize(s);
                for (int i = startIndex; i < maxLen; i++) {
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
                EncodedArray.UShort uShort1 = EncodedArray.UShort.BeginRead(s);
                for (int i = 1; i < maxLen; i++) {
                    if ((trees[i].m_flags & (ushort)TreeInstance.Flags.FixedHeight) != 0) {
                        trees[i].m_posY = uShort1.Read();
                    }
                }
                uShort1.EndRead();
            }

            public void AfterDeserialize(DataSerializer s) {
            }

            public void Serialize(DataSerializer s) {
                int treeLimit = MaxTreeLimit;
                TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;

                // Important to save treelimit as it is an adjustable variable on every load
                s.WriteInt32(treeLimit);

                /* Apparently, the trees could be located anywhere in the buffer
                 * even if there's only 1 tree in the buffer. I'm assuming this is
                 * due to performance concerns.
                 * So have to look through the entire buffer.
                 */
                EncodedArray.UShort uShort = EncodedArray.UShort.BeginWrite(s);
                for (int i = DefaultTreeLimit; i < treeLimit; i++) {
                    uShort.Write(buffer[i].m_flags);
                }
                uShort.EndWrite();
                try {
                    PrefabCollection<TreeInfo>.BeginSerialize(s);
                    for (int i = 1; i < treeLimit; i++) {
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
                EncodedArray.UShort uShort1 = EncodedArray.UShort.BeginWrite(s);
                for (int i = 1; i < treeLimit; i++) {
                    if ((buffer[i].m_flags & (ushort)TreeInstance.Flags.FixedHeight) != 0) {
                        uShort1.Write(buffer[i].m_posY);
                    }
                }
                uShort1.EndWrite();
            }
        }

        public void OnCreated(ISerializableData s) => m_Serializer = s;

        public void OnReleased() => m_Serializer = null;

        public static void PurgeOldData() => m_Serializer?.EraseData(OldTreeUnlimiterKey);

        public void OnLoadData() {
        }

        public static void IntegratedDeserialize() {
            while (!Monitor.TryEnter(Singleton<SimulationManager>.instance.m_serializableDataStorage, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
            try { /* Try find old data version first */
                if (Singleton<SimulationManager>.instance.m_serializableDataStorage.ContainsKey(OldTreeUnlimiterKey)) {
                    byte[] oldData = Singleton<SimulationManager>.instance.m_serializableDataStorage[OldTreeUnlimiterKey];
                    if (oldData != null) {
                        if (oldData.Length < 2 || oldData.Length % 2 != 0) {
                            Debug.Log("TreeAnarchy: Invalid Old Data, Not Loading Tree Data");
                            return;
                        }
                        using OldDataSerializer oldSerializer = new OldDataSerializer(oldData);
                        if (oldSerializer.Deserialize(Singleton<TreeManager>.instance.m_trees.m_buffer)) {
                            Debug.Log("TreeAnarchy: Old Format Loaded");
                            OldFormatLoaded = true;
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
                if (OldFormatLoaded) m_Serializer?.EraseData(OldTreeUnlimiterKey);
                using (var stream = new MemoryStream()) {
                    DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, (uint)Format.Version5, new Data());
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
