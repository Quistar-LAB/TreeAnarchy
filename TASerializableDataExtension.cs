using System;
using System.IO;
using System.Threading;
using ColossalFramework;
using ICities;
using UnityEngine;
using static TreeAnarchy.TAConfig;

namespace TreeAnarchy
{
    public class TASerializableDataExtension : ISerializableDataExtension
    {
        static private ISerializableData m_Serializer = null;
        private class DataSerializer : TADataSerializer
        {
            // Used for saving trees indexed > 262144
            public override void FinalizeSave()
            {

            }

            public override void AfterDeserialize(Stream s)
            {

            }
        }

        public const string OldTreeUnlimiterKey = @"mabako/unlimiter";

        public void OnCreated(ISerializableData s)
        {
            m_Serializer = s;
        }

        public void OnLoadData()
        {
        }

        public void OnSaveData()
        {
            const int headerSize = 100; // over estimate header size
            const int eachTreeByteCount = 5;

            PurgeData(); // remove old data if it exists
            try
            {
                using (DataSerializer serializer = new DataSerializer())
                using (MemoryStream stream = new MemoryStream((MaxTreeLimit * eachTreeByteCount) + headerSize))
                {
                    serializer.Serialize(stream);
                    if(m_Serializer != null) m_Serializer.SaveData(OldTreeUnlimiterKey, stream.ToArray());
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public void OnReleased()
        {
            m_Serializer = null;
        }

        public static void PurgeData()
        {
            if (m_Serializer != null) m_Serializer.EraseData(OldTreeUnlimiterKey);
        }

        public static void Deserialize()
        {
            if (Singleton<SimulationManager>.instance.m_serializableDataStorage.TryGetValue(OldTreeUnlimiterKey, out byte[] data))
            {
                if (data.Length < 2 || data.Length % 2 != 0)
                {
                    Debug.Log("TreeAnarchy: Invalid Data");
                    return;
                }
                using (DataSerializer serializer = new DataSerializer())
                using (MemoryStream stream = new MemoryStream(data))
                {
                    serializer.Deserialize(stream);
                }
                return;
            }
            Debug.Log("TreeAnarchy: No extra tree data saved or found in this savegame or map");
        }

    }
}
