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
            Version6 = 6,
        }

        static private ISerializableData m_Serializer = null;
        private const string TREE_ANARCHY_KEY = @"TreeAnarchy";
        internal class OldDataSerializer : TAOldDataSerializer {
            public OldDataSerializer(byte[] data) : base(data) { }
            public override void AfterDeserialize() {
            }
        }

        private class Data : IDataContainer {
#pragma warning disable IDE0044 // Add readonly modifier
            private static uint[] limit = new uint[] { 393216, 524288, 655360, 786432, 917504, 1048576, 1179648, 1310720, 1441792, 1572864, 1703936, 1835008, 1966080, 2097152 };
            private static float[] scale = new float[] { 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 4.5f, 5.0f, 5.5f, 6.0f, 6.5f, 7.0f, 7.5f, 8.0f };
#pragma warning restore IDE0044 // Add readonly modifier
            private const ushort fireDamageBurningMask = unchecked((ushort)~(TreeInstance.Flags.Burning | TreeInstance.Flags.FireDamage));
            private static void UpdateTreeLimit(int newSize) {
                for (int i = 0; i < limit.Length; i++) {
                    if (newSize == limit[i]) {
                        TreeScaleFactor = scale[i];
                        TAUI.SetTreeLimitSlider(TreeScaleFactor);
                        return;
                    }
                }
            }

            public void Deserialize(DataSerializer s) {
                int maxLen = s.ReadInt32(); // Read in Max limit
                int startIndex = 1;
                int treeCount = 0;
                Array32<TreeInstance> newBuffer = null;
                TreeInstance[] trees;
                float[] treeScaleBuffer;
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
                    treeScaleBuffer = new float[maxLen];
                    for (int i = 0; i < treeScaleBuffer.Length; i++) {
                        treeScaleBuffer[i] = 0;
                    }
                } else {
                    trees = Singleton<TreeManager>.instance.m_trees.m_buffer;
                    treeScaleBuffer = Patches.TreeVariation.m_treeScale;
                }
                EncodedArray.UShort uShort = EncodedArray.UShort.BeginRead(s);
                for (int i = DefaultTreeLimit; i < maxLen; i++) {
                    trees[i].m_flags = (ushort)(uShort.Read() & fireDamageBurningMask);
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
                if ((Format)s.version >= Format.Version6) {
                    EncodedArray.Float @float = EncodedArray.Float.BeginRead(s);
                    for (int i = 1; i < maxLen; i++) {
                        if (trees[i].m_flags != 0) {
                            treeScaleBuffer[i] = @float.Read();
                        }
                    }
                    @float.EndRead();
                }
                /* Now Resize / Repack buffer if necessary */
                if (maxLen > MaxTreeLimit) {
                    if (treeCount > MaxTreeLimit) {
                        TreeManager treeManager = Singleton<TreeManager>.instance;
                        treeManager.m_trees = newBuffer;
                        UpdateTreeLimit(maxLen);
                        /* UpdateTreeLimit first so TreeScaleFactor is updated for next statement */
                        treeManager.m_updatedTrees = new ulong[MaxTreeUpdateLimit];
                        if ((Format)s.version >= Format.Version6) {
                            Patches.TreeVariation.m_treeScale = treeScaleBuffer;
                        }
                        return; /* Just return with this buffer */
                    }
                    /* Pack the result into existing buffer */
                    TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                    float[] scaleBuffer = Patches.TreeVariation.m_treeScale;
                    startIndex = 1;
                    /* update the Y position for trees between 1~262144 */
                    /* This needs to be done first */
                    for (int i = 1; i < DefaultTreeLimit; i++) {
                        if (trees[i].m_flags != 0) {
                            buffer[i].m_posY = trees[i].m_posY;
                        }
                    }
                    /* Add trees into empty nodes in buffer */
                    for (int i = DefaultTreeLimit; i < maxLen; i++) {
                        if (trees[i].m_flags != 0) {
                            /* Find available slot in buffer */
                            for (int j = startIndex; j < buffer.Length; j++) {
                                if (buffer[j].m_flags == 0) {
                                    buffer[j].m_flags = trees[i].m_flags;
                                    buffer[j].m_infoIndex = trees[i].m_infoIndex;
                                    buffer[j].m_posX = trees[i].m_posX;
                                    buffer[j].m_posY = trees[i].m_posY;
                                    buffer[j].m_posZ = trees[i].m_posZ;
                                    if ((Format)s.version >= Format.Version6) {
                                        scaleBuffer[j] = treeScaleBuffer[i];
                                    }
                                    startIndex = j;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            public void AfterDeserialize(DataSerializer s) { }

            public void Serialize(DataSerializer s) {
                int treeLimit = MaxTreeLimit;
                TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                float[] treeScaleBuffer = Patches.TreeVariation.m_treeScale;

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
                EncodedArray.Float @float = EncodedArray.Float.BeginWrite(s);
                for (int i = 1; i < treeLimit; i++) {
                    if (buffer[i].m_flags != 0) {
                        @float.Write(treeScaleBuffer[i]);
                    }
                }
                @float.EndWrite();
            }
        }

        public void OnCreated(ISerializableData s) => m_Serializer = s;

        public void OnReleased() => m_Serializer = null;

        public static void PurgeOldData() => m_Serializer?.EraseData(OldTreeUnlimiterKey);

        public void OnLoadData() { }

        public static void IntegratedDeserialize() {
            try { /* Try find old data version first */
                if (Singleton<SimulationManager>.instance.m_serializableDataStorage.ContainsKey(OldTreeUnlimiterKey)) {
                    byte[] oldData = Singleton<SimulationManager>.instance.m_serializableDataStorage[OldTreeUnlimiterKey];
                    if (oldData != null) {
                        if (oldData.Length < 2 || oldData.Length % 2 != 0) {
                            Debug.Log("TreeAnarchy: Invalid Old Data, Not Loading Tree Data");
                            return;
                        }
                        OldDataSerializer oldSerializer = new OldDataSerializer(oldData);
                        if (oldSerializer.Deserialize()) {
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
                    using var stream = new MemoryStream(data); DataSerializer.Deserialize<Data>(stream, DataSerializer.Mode.Memory);
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
                    DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, (uint)Format.Version6, new Data());
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
