using ColossalFramework;
using ColossalFramework.IO;
using ICities;
using System;
using System.IO;
using System.Threading;
using TreeAnarchy.Patches;
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

        private const string TREE_ANARCHY_KEY = @"TreeAnarchy";
        private class OldDataSerializer : TAOldDataSerializer {
            public OldDataSerializer(byte[] data) : base(data) { }
            public override void AfterDeserialize() {
            }
        }

        private class Data : IDataContainer {
#pragma warning disable IDE0044 // Add readonly modifier
            private uint[] limit = new uint[] { 393216, 524288, 655360, 786432, 917504, 1048576, 1179648, 1310720, 1441792, 1572864, 1703936, 1835008, 1966080, 2097152 };
            private float[] scale = new float[] { 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 4.5f, 5.0f, 5.5f, 6.0f, 6.5f, 7.0f, 7.5f, 8.0f };
#pragma warning restore IDE0044 // Add readonly modifier
            private const ushort fireDamageBurningMask = unchecked((ushort)~(TreeInstance.Flags.Burning | TreeInstance.Flags.FireDamage));
            private void UpdateTreeLimit(int newSize) {
                for (int i = 0; i < limit.Length; i++) {
                    if (newSize == limit[i]) {
                        TreeScaleFactor = scale[i];
                        SaveSettings();
                        return;
                    }
                }
            }

            private void EnsureCapacity(int maxLimit, out Array32<TreeInstance> newArray, out TreeInstance[] treeBuffer, out float[] treeScale) {
                TreeManager tmInstance = Singleton<TreeManager>.instance;
                if (maxLimit > MaxTreeLimit) {
                    TreeInstance[] oldBuffer = tmInstance.m_trees.m_buffer;
                    Array32<TreeInstance> newTreeArray = new Array32<TreeInstance>((uint)maxLimit);
                    float[] newTreeScaleBuffer = new float[maxLimit];
                    newTreeArray.CreateItem(out uint _);
                    newTreeArray.ClearUnused();
                    TreeInstance[] newBuffer = newTreeArray.m_buffer;

                    for (int i = 1; i < DefaultTreeLimit; i++) {
                        if (oldBuffer[i].m_flags != 0) {
                            newBuffer[i].m_flags = oldBuffer[i].m_flags;
                            newBuffer[i].m_infoIndex = oldBuffer[i].m_flags;
                            newBuffer[i].m_posX = oldBuffer[i].m_posX;
                            newBuffer[i].m_posZ = oldBuffer[i].m_posZ;
                        }
                    }
                    newArray = newTreeArray;
                    treeBuffer = newBuffer;
                    treeScale = newTreeScaleBuffer;
                    return;
                }
                newArray = tmInstance.m_trees;
                treeBuffer = tmInstance.m_trees.m_buffer;
                treeScale = Singleton<TreeScaleManager>.instance.m_treeScales;
            }

            private void RepackBuffer(int maxLimit, int treeCount, Format version, Array32<TreeInstance> existingTreeBuffer, float[] existingTreeScaleBuffer) {
                if (maxLimit > MaxTreeLimit) {
                    if (treeCount > MaxTreeLimit) {
                        TreeManager tmInstance = Singleton<TreeManager>.instance;
                        tmInstance.m_trees = existingTreeBuffer;
                        UpdateTreeLimit(maxLimit);
                        /* UpdateTreeLimit first so TreeScaleFactor is updated for next statement */
                        tmInstance.m_updatedTrees = new ulong[MaxTreeUpdateLimit];
                        if (version >= Format.Version6) {
                            SingletonLite<TreeScaleManager>.instance.m_treeScales = existingTreeScaleBuffer;
                        }
                        return; /* Just return with existing buffers */
                    }
                    /* Pack the result into old buffer as we are sure there are enough space to fit in buffer */
                    TreeInstance[] existingBuffer = existingTreeBuffer.m_buffer;
                    TreeInstance[] oldBuffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                    float[] existingScales = existingTreeScaleBuffer;
                    float[] oldScales = Singleton<TreeScaleManager>.instance.m_treeScales;
                    /* make sure to fill in 1~262144 trees first */
                    for (int i = 1; i < DefaultTreeLimit; i++) {
                        if (existingBuffer[i].m_flags != 0) {
                            oldBuffer[i].m_posY = existingBuffer[i].m_posY;
                            oldScales[i] = existingScales[i];
                        }
                    }

                    for (int i = DefaultTreeLimit, offsetIndex = 1; i < existingBuffer.Length; i++) {
                        if (existingBuffer[i].m_flags != 0) {
                            while (oldBuffer[offsetIndex].m_flags != 0) { offsetIndex++; } /* Find available slot in old buffer */
                            oldBuffer[offsetIndex].m_flags = existingBuffer[i].m_flags;
                            oldBuffer[offsetIndex].m_infoIndex = existingBuffer[i].m_infoIndex;
                            oldBuffer[offsetIndex].m_posX = existingBuffer[i].m_posX;
                            oldBuffer[offsetIndex].m_posZ = existingBuffer[i].m_posZ;
                            oldBuffer[offsetIndex].m_posY = existingBuffer[i].m_posY;
                            if (version > Format.Version6) {
                                oldScales[offsetIndex] = existingScales[i];
                            }
                        }
                    }
                }
            }

            public void Deserialize(DataSerializer s) {
                int maxLen = s.ReadInt32(); // Read in Max limit
                int treeCount = 0;
                EnsureCapacity(maxLen, out Array32<TreeInstance> newBuffer, out TreeInstance[] trees, out float[] treeScaleBuffer);
                EncodedArray.UShort uShort = EncodedArray.UShort.BeginRead(s);
                for (int i = DefaultTreeLimit; i < maxLen; i++) {
                    trees[i].m_flags = (ushort)(uShort.Read() & fireDamageBurningMask);
                }
                uShort.EndRead();
                PrefabCollection<TreeInfo>.BeginDeserialize(s);
                for (int i = 1; i < maxLen; i++) {
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
                RepackBuffer(maxLen, treeCount, (Format)s.version, newBuffer, treeScaleBuffer);
            }

            public void AfterDeserialize(DataSerializer s) { }

            public void Serialize(DataSerializer s) {
                int treeLimit = MaxTreeLimit;
                TreeInstance[] buffer = Singleton<TreeManager>.instance.m_trees.m_buffer;
                float[] treeScaleBuffer = Singleton<TreeScaleManager>.instance.m_treeScales;

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

        public void OnCreated(ISerializableData s) { }

        public void OnReleased() { }

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
                if (OldFormatLoaded) EraseData(OldTreeUnlimiterKey);
                using (var stream = new MemoryStream()) {
                    DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, (uint)Format.Version6, new Data());
                    data = stream.ToArray();
                }
                SaveData(TREE_ANARCHY_KEY, data);
                Debug.Log($"TreeAnarchy: Saved {data.Length} bytes of data");
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private void SaveData(string id, byte[] data) {
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            while (!Monitor.TryEnter(smInstance.m_serializableDataStorage, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
            try {
                smInstance.m_serializableDataStorage[id] = data;
            } finally {
                Monitor.Exit(smInstance.m_serializableDataStorage);
            }
        }

        private void EraseData(string id) {
            SimulationManager smInstance = Singleton<SimulationManager>.instance;
            while (!Monitor.TryEnter(smInstance.m_serializableDataStorage, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
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
