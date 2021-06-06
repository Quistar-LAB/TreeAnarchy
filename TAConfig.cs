using System;
using System.IO;
using System.Xml;
using ColossalFramework;
using UnityEngine;

namespace TreeAnarchy {
    public static class TAConfig
    {
        internal const float MaxScaleFactor = 6.0f;
        internal const float MinScaleFactor = 1.5f;
        internal const int DefaultTreeLimit = 262144;
        internal const int DefaultTreeUpdateCount = 4096;

        /* Lock Forestry */
        internal static bool LockForestry = false;

        /* Tree Movement Releated */
        internal static bool StopDistantTree = false;
        internal static float TreeSwayFactor = 1f;
        internal static bool RandomTreeRotation = true;

        /* Unlimited Trees Related */
        internal static bool isInGame = false;
        internal static bool OldFormatLoaded = false;
        private static float m_ScaleFactor = 4f;
        internal static bool TreeEffectOnWind = true;
        public static bool UseTreeSnapping = true;
        internal static bool DebugMode = false;
        internal static int LastMaxTreeLimit = DefaultTreeLimit;
        internal static int MaxTreeLimit {
            get => (int)(DefaultTreeLimit * TreeScaleFactor);
        }
        internal static float TreeScaleFactor {
            get => m_ScaleFactor;
            set {
                LastTreeScaleFactor = m_ScaleFactor;
                m_ScaleFactor = value;
            }
        }
        internal static float LastTreeScaleFactor { get; private set; } = m_ScaleFactor;
        internal static int MaxTreeUpdateLimit => (int)(DefaultTreeUpdateCount * TreeScaleFactor);
        internal static bool UseModifiedTreeCap
        {
            get {
                switch (Singleton<SimulationManager>.instance.m_metaData.m_updateMode) {
                case SimulationManager.UpdateMode.LoadGame:
                case SimulationManager.UpdateMode.LoadMap:
                case SimulationManager.UpdateMode.NewGameFromMap:
                case SimulationManager.UpdateMode.NewGameFromScenario:
                case SimulationManager.UpdateMode.NewMap:
                case SimulationManager.UpdateMode.LoadScenario:
                case SimulationManager.UpdateMode.NewScenarioFromGame:
                case SimulationManager.UpdateMode.NewScenarioFromMap:
                case SimulationManager.UpdateMode.UpdateScenarioFromGame:
                case SimulationManager.UpdateMode.UpdateScenarioFromMap:
                    return true;
                }
                return false;
            }
        }

        private const string SettingsFileName = "TreeAnarchyConfig.xml";
        internal static void LoadSettings()
        {
            try {
                if (!File.Exists(SettingsFileName)) {
                    SaveSettings();
                }
                XmlDocument xmlConfig = new XmlDocument();
                xmlConfig.Load(SettingsFileName);
                m_ScaleFactor = float.Parse(xmlConfig.DocumentElement.GetAttribute("ScaleFactor"));
                TreeEffectOnWind = bool.Parse(xmlConfig.DocumentElement.GetAttribute("TreeEffectOnWind"));
                UseTreeSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapping"));
                RandomTreeRotation = bool.Parse(xmlConfig.DocumentElement.GetAttribute("RandomTreeRotation"));
                TreeSwayFactor = float.Parse(xmlConfig.DocumentElement.GetAttribute("TreeSwayFactor"));
                StopDistantTree = bool.Parse(xmlConfig.DocumentElement.GetAttribute("StopDistantTree"));
                LockForestry = bool.Parse(xmlConfig.DocumentElement.GetAttribute("LockForestry"));
            } catch {
                SaveSettings(); // Create a new save
            }
        }

        internal static void SaveSettings()
        {
            XmlDocument xmlConfig = new XmlDocument();
            XmlElement root = xmlConfig.CreateElement("TreeAnarchyConfig");
            root.Attributes.Append(AddElement<float>(xmlConfig, "ScaleFactor", m_ScaleFactor));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "TreeEffectOnWind", TreeEffectOnWind));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "UseTreeSnapping", UseTreeSnapping));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "RandomTreeRotation", RandomTreeRotation));
            root.Attributes.Append(AddElement<float>(xmlConfig, "TreeSwayFactor", TreeSwayFactor));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "StopDistantTree", StopDistantTree));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "LockForestry", LockForestry));
            xmlConfig.AppendChild(root);
            xmlConfig.Save(SettingsFileName);
        }

        private static XmlAttribute AddElement<T>(XmlDocument doc, string name, T t) {
            XmlAttribute attr = doc.CreateAttribute(name);
            attr.Value = t.ToString();
            return attr;
        }

    }
}
