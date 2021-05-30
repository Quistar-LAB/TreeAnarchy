// Originally written by algernon for Find It 2.

using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using static TreeAnarchy.TAConfig;

namespace TreeAnarchy.Utils
{
    public static class XMLSetting
    {
        private const string SettingsFileName = "TreeAnarchyConfig.xml";
        [XmlRoot(ElementName = "TreeAnarchySettings", Namespace = "TreeAnarchy", IsNullable = false)]
        public class SettingData
        {
            [XmlElement("TreeEffectOnWindEnabled")]
            public bool TreeEffectOnWind { get => TAConfig.TreeEffectOnWind; set => TAConfig.TreeEffectOnWind = value; }
            [XmlElement("TreeSnappingEnabled")]
            public bool TreeSnapping { get => TAConfig.UseTreeSnapping; set => TAConfig.UseTreeSnapping = value; }
            [XmlElement("ScaleFactor")]
            public float ScaleFactor { get => TreeScaleFactor; set => TreeScaleFactor = value; }
            [XmlElement("RandomRotation")]
            public bool RandomRotation { get => RandomTreeRotation; set => RandomTreeRotation = value; }
            [XmlElement("TreeSwayFactor")]
            public float TreeSwayScale { get => TreeSwayFactor; set => TreeSwayFactor = value; }
            [XmlElement("DistantTree")]
            public uint DistanctTree { get => StopDistantTree; set => StopDistantTree = value; }

        }

        internal static void LoadSettings()
        {
            try
            {
                if(!File.Exists(SettingsFileName))
                {
                    SaveSettings();
                }
                using (StreamReader reader = new StreamReader(SettingsFileName))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingData));
                    if (!(xmlSerializer.Deserialize(reader) is SettingData))
                    {
                        Debug.Log("TreeAnarchy: Couldn't deserialize settings file");
                    }
                }

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal static void SaveSettings()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(SettingsFileName))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingData));
                    xmlSerializer.Serialize(writer, new SettingData());
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}