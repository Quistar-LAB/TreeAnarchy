using ColossalFramework;
using ColossalFramework.IO;
using ICities;
using System;
using System.IO;
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
            private static readonly uint[] limit = new uint[] { 393216, 524288, 655360, 786432, 917504, 1048576, 1179648, 1310720, 1441792, 1572864 };
            private static readonly float[] scale = new float[] { 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 4.5f, 5.0f, 5.5f, 6.0f };
            private static void UpdateTreeLimit(int newSize) {
                for (int i = 0; i < 10; i++) {
                    if (newSize == limit[i]) {
                        TreeScaleFactor = scale[i];
                        TAUI.SetTreeLimitSlider(TreeScaleFactor);
                        return;
                    }
                }
            }

            private static Array32<TreeInstance> ResizeTreeBuffer(uint treeCount) {
                for (int i = 0; i < 10; i++) {
                    if (treeCount < limit[i]) {
                        uint size = limit[i];
                        Array32<TreeInstance> newBuffer = new Array32<TreeInstance>(size);
                        TreeInstance[] oldBuf = Singleton<TreeManager>.instance.m_trees.m_buffer;
                        newBuffer.CreateItem(out uint _);
                        newBuffer.ClearUnused();
                        for (int j = 1; j < MaxTreeLimit; j++) {

                        }
                        /* Update mod parameters and UI */
                        TreeScaleFactor = scale[i];
                        TAUI.SetTreeLimitSlider(scale[i]);
                        return newBuffer;
                    }
                }
                return null;
            }

            public void Deserialize(DataSerializer s) {
                int maxLen = s.ReadInt32(); // Read in Max limit
                int startIndex = 1;
                int treeCount = 0;
                Array32<TreeInstance> newBuffer = default;
                TreeInstance[] trees;
                if (maxLen > MaxTreeLimit) {
                    newBuffer = new Array32<TreeInstance>((uint)maxLen);
                    newBuffer.CreateItem(out uint _);
                    newBuffer.ClearUnused();
                    trees = newBuffer.m_buffer;
                    TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                    for (int i = 1; i < DefaultTreeLimit; i++) {
                        if (buffer[i].m_flags != 0) {
                            trees[i].m_flags = buffer[i].m_flags;
                            trees[i].m_infoIndex = buffer[i].m_infoIndex;
                            trees[i].m_posX = buffer[i].m_posX;
                            trees[i].m_posZ = buffer[i].m_posZ;
                        }
                    }
                } else {
                    trees = Singleton<TreeManager>.instance.m_trees.m_buffer;
                }
                EncodedArray.UShort uShort = EncodedArray.UShort.BeginRead(s);
                for (int i = DefaultTreeLimit; i < maxLen; i++) {
                    TreeInstance.Flags m_flags = (TreeInstance.Flags)uShort.Read();
                    m_flags &= ~(TreeInstance.Flags.FireDamage | TreeInstance.Flags.Burning);
                    trees[i].m_flags = (ushort)m_flags;
                }
                uShort.EndRead();
                switch ((Format)s.version) {
                    case Format.Version4:
                    startIndex = DefaultTreeLimit;
                    break;
                }
                PrefabCollection<TreeInfo>.BeginDeserialize(s);
                for (int i = startIndex; i < maxLen; i++) {
                    if (trees[i].m_flags != 0) {
                        trees[i].m_infoIndex = (ushort)PrefabCollection<TreeInfo>.Deserialize(true);
                        treeCount++;
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
                /* Now Resize / Repack buffer if necessary */
                if (maxLen > MaxTreeLimit) {
                    if (treeCount > MaxTreeLimit) {
                        TreeManager treeManager = Singleton<TreeManager>.instance;
                        treeManager.m_trees = newBuffer;
                        UpdateTreeLimit(maxLen);
                        Array.Resize<ulong>(ref treeManager.m_updatedTrees, MaxTreeUpdateLimit);
                        return; /* Just return with this buffer */
                    }
                    /* Pack the result into existing buffer */
                    Array32<TreeInstance> buffer = Singleton<TreeManager>.instance.m_trees;
                    startIndex = 1;
                    /* update the Y position for trees between 1~262144 */
                    /* This needs to be done first */
                    for (int i = 1; i < DefaultTreeLimit; i++) {
                        if (trees[i].m_flags != 0) {
                            buffer.m_buffer[i].m_posY = trees[i].m_posY;
                        }
                    }
                    /* Add trees into empty nodes in buffer */
                    for (int i = DefaultTreeLimit; i < maxLen; i++) {
                        if (trees[i].m_flags != 0) {
                            /* Find available slot in buffer */
                            for (int j = startIndex; j < MaxTreeLimit; j++) {
                                if (buffer.m_buffer[j].m_flags == 0) {
                                    buffer.m_buffer[j].m_flags = trees[i].m_flags;
                                    buffer.m_buffer[j].m_infoIndex = trees[i].m_infoIndex;
                                    buffer.m_buffer[j].m_posX = trees[i].m_posX;
                                    buffer.m_buffer[j].m_posY = trees[i].m_posY;
                                    buffer.m_buffer[j].m_posZ = trees[i].m_posZ;
                                    startIndex = j;
                                    break;
                                }
                            }
                        }
                    }
                }
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
            //while (!Monitor.TryEnter(Singleton<SimulationManager>.instance.m_serializableDataStorage, SimulationManager.SYNCHRONIZE_TIMEOUT)) { }
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
