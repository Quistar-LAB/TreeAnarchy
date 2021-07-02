using ColossalFramework;
using ICities;
using System;
using System.Globalization;
using System.IO;
using System.Xml;


namespace TreeAnarchy {
    public class TAMod : ILoadingExtension, IUserMod {
        internal const string m_modVersion = "0.8.9";
        internal const string m_assemblyVersion = m_modVersion + ".*";
        private const string m_modName = "Unlimited Trees: Reboot";
        private const string m_modDesc = "An improved Unlimited Trees Mod. Lets you plant more trees with tree snapping";

        /* Some standard constant definition for tree limits */
        private static float m_ScaleFactor = 4f;
        internal const int DefaultTreeLimit = 262144;
        internal const int DefaultTreeUpdateCount = 4096;
        /* Unlimited Trees Related */
        internal static bool OldFormatLoaded = false;
        internal static bool TreeEffectOnWind = true;
        internal static bool UseTreeSnapping = true;
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

        /* Experimental mode */
        internal static bool UseExperimental = false;
        internal static bool EnableProfiling = false;

        /* Lock Forestry */
        internal static bool LockForestry = false;
        internal static bool PersistentLockForestry = true;

        /* Tree Movement Releated */
        internal static float TreeSwayFactor = 1f;
        internal static bool RandomTreeRotation = true;
        internal static int RandomTreeRotationFactor = 1000;

        internal static bool UseModifiedTreeCap {
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

        internal static bool IsInGame = false;
        internal static bool Is64Bits = true;

        #region IUserMod
        string IUserMod.Name => $"{m_modName} {m_modVersion}";
        string IUserMod.Description => m_modDesc;
        public void OnEnabled() {
            LoadSettings();
            if (PersistentLockForestry) LockForestry = true;
            TAPatcher.EnableCore();
            if (IntPtr.Size == 8) {
                /* Make sure system is 64 bits */
                Is64Bits = true;
                AccelLayer.SetupCore();

            } else {
                Is64Bits = false;
            }
        }
        public void OnDisabled() {
            TAPatcher.DisableCore();
            SaveSettings();
        }

        public void OnSettingsUI(UIHelperBase helper) {
            TAUI.ShowStandardOptions(helper.AddGroup($"{m_modName} -- Version {m_modVersion}") as UIHelper);
            TAUI.ShowTreeLimitOption(helper.AddGroup("Configure Custom Tree Limit") as UIHelper);
            TAUI.UpdateState(IsInGame);
        }
        #endregion
        #region ILoadingExtension
        void ILoadingExtension.OnCreated(ILoading loading) {
            TAPatcher.LateEnable();
        }

        void ILoadingExtension.OnReleased() {
            TAPatcher.DisableLatePatch();
        }

        void ILoadingExtension.OnLevelLoaded(LoadMode mode) {
            IsInGame = true;
            TAUI.UpdateState(IsInGame);
        }

        void ILoadingExtension.OnLevelUnloading() {
            IsInGame = false;
            TAUI.UpdateState(IsInGame);
        }
        #endregion
        /*
                internal static Utils.MemoryModule setupMemModule(byte[] buf) {

                }
        */
        private const string SettingsFileName = "TreeAnarchyConfig.xml";
        internal static void LoadSettings() {
            try {
                if (!File.Exists(SettingsFileName)) {
                    SaveSettings();
                }
                XmlDocument xmlConfig = new XmlDocument();
                xmlConfig.Load(SettingsFileName);
                m_ScaleFactor = float.Parse(xmlConfig.DocumentElement.GetAttribute("ScaleFactor"), NumberStyles.Float, CultureInfo.CurrentCulture.NumberFormat);
                TreeEffectOnWind = bool.Parse(xmlConfig.DocumentElement.GetAttribute("TreeEffectOnWind"));
                UseTreeSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapping"));
                RandomTreeRotation = bool.Parse(xmlConfig.DocumentElement.GetAttribute("RandomTreeRotation"));
                TreeSwayFactor = float.Parse(xmlConfig.DocumentElement.GetAttribute("TreeSwayFactor"), NumberStyles.Float, CultureInfo.CurrentCulture.NumberFormat);
                LockForestry = bool.Parse(xmlConfig.DocumentElement.GetAttribute("LockForestry"));
                PersistentLockForestry = bool.Parse(xmlConfig.DocumentElement.GetAttribute("PersistentLock"));
                UseExperimental = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseExperimental"));
                EnableProfiling = bool.Parse(xmlConfig.DocumentElement.GetAttribute("EnableProfiling"));
            } catch {
                SaveSettings(); // Most likely a corrupted file if we enter here. Recreate the file
            }
        }

        internal static void SaveSettings() {
            XmlDocument xmlConfig = new XmlDocument();
            XmlElement root = xmlConfig.CreateElement("TreeAnarchyConfig");
            root.Attributes.Append(AddElement<float>(xmlConfig, "ScaleFactor", m_ScaleFactor));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "TreeEffectOnWind", TreeEffectOnWind));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "UseTreeSnapping", UseTreeSnapping));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "RandomTreeRotation", RandomTreeRotation));
            root.Attributes.Append(AddElement<float>(xmlConfig, "TreeSwayFactor", TreeSwayFactor));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "LockForestry", LockForestry));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "PersistentLock", PersistentLockForestry));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "UseExperimental", UseExperimental));
            root.Attributes.Append(AddElement<bool>(xmlConfig, "EnableProfiling", EnableProfiling));
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
