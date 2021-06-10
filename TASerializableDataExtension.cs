using System;
using ICities;
using UnityEngine;
using ColossalFramework;
using static TreeAnarchy.TAConfig;
using static TreeAnarchy.TADataSerializer;

namespace TreeAnarchy {
    public class TASerializableDataExtension : ISerializableDataExtension {
        static private ISerializableData m_Serializer = null;
        internal class DataSerializer : TADataSerializer {
            public DataSerializer() : base() {}
            public DataSerializer(int size) : base(size) {}
            public DataSerializer(byte[] data) : base(data) {}
            public override void FinalizeSave() {
            }
            public override void AfterDeserialize(ColossalFramework.IO.DataSerializer s) {
            }
        }

        public const string OldTreeUnlimiterKey = @"mabako/unlimiter";

        public void OnCreated(ISerializableData s) => m_Serializer = s;

        public void OnLoadData() {
        }

        public void OnSaveData() {
            //PurgeData(); // remove old data if it exists
            EnsurePosY();
            try {
                // hmmmm assuming 1 million trees... this is around ~10mb of data
                // lets see if compression helps improve File IO
                // Use dynamic memory allocation, to possibly save ram during saving
                using DataSerializer serializer = new DataSerializer();
                m_Serializer?.SaveData(OldTreeUnlimiterKey, serializer.Serialize());
            } catch(Exception e) {
                Debug.LogException(e);
            }
        }
        public void OnReleased() => m_Serializer = null;

        public static void PurgeData() => m_Serializer?.EraseData(OldTreeUnlimiterKey);

        public static void Deserialize() {
            if(Singleton<SimulationManager>.instance.m_serializableDataStorage.TryGetValue(OldTreeUnlimiterKey, out byte[] data)) {
                if(data.Length < 2 || data.Length % 2 != 0) {
                    Debug.Log("TreeAnarchy: Invalid Data");
                    return;
                }
                using DataSerializer serializer = new DataSerializer(data);
                if(serializer.Deserialize(Singleton<TreeManager>.instance.m_trees.m_buffer, out ErrorFlags errors)) {
                    if((errors & ErrorFlags.OLDFORMAT) != ErrorFlags.NONE) {
                        Debug.Log("TreeAnarchy: Old Format Detected");
                        OldFormatLoaded = true;
                    }
                } else {
                    Debug.Log("TreeAnarchy: Invalid Data Format");
                }
                return;
            }
            Debug.Log("TreeAnarchy: No extra tree data saved or found in this savegame or map");
        }

        public static void EnsurePosY() {
            if (OldFormatLoaded) {
                Debug.Log("TreeAnarchy: Old format detected. Repositioning Y");
                /* When using original CO or Unlimited Trees Mod, the posY is never
                 * considered, and it could be any random number, usually 0. When
                 * saving into our new format. We need to actually store their posY
                 * so we have to make sure its initialized.
                 */
                TreeManager manager = Singleton<TreeManager>.instance;
                for (uint i = 1; i < MaxTreeLimit; i++) {
                    if ((manager.m_trees.m_buffer[i].m_flags) != 0) {
                        Vector3 position = manager.m_trees.m_buffer[i].Position;
                        float terrainHeight = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
                        if(position.y < terrainHeight) {
                            position.y = terrainHeight;
                            manager.m_trees.m_buffer[i].Position = position;
                        }
                    }
                }
            }
        }
    }
}
