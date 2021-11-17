﻿using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

namespace TreeAnarchy {
    public class TAMod : ILoadingExtension, IUserMod {
        internal const string m_modVersion = "1.0.9";
        internal const string m_assemblyVersion = m_modVersion + ".*";
        private const string m_modName = "Tree Anarchy";
        private const string m_modDesc = "Lets you plant more trees with tree snapping";
        private const string m_debugLogFile = "00TreeAnarchyDebug.log";

        internal const string KeybindingConfigFile = "TreeAnarchyKeyBindSetting";

        /* Some standard constant definition for tree limits */
        private static float m_ScaleFactor = 4f;
        public const int DefaultTreeLimit = 262144;
        public const int DefaultTreeUpdateCount = 4096;
        /* Unlimited Trees Related */
        public static int RemoveReplaceOrKeep = 0;
        public static bool OldFormatLoaded = false;
        public static bool TreeEffectOnWind = true;
        public static int LastMaxTreeLimit = DefaultTreeLimit;
        public static int CheckLowLimit {
            get => MaxTreeLimit - 12144;
        }
        public static int CheckHighLimit {
            get => MaxTreeLimit - 5;
        }

        public static int MaxTreeLimit {
            get => (int)(DefaultTreeLimit * TreeScaleFactor);
        }
        public static float TreeScaleFactor {
            get => m_ScaleFactor;
            set {
                LastTreeScaleFactor = m_ScaleFactor;
                m_ScaleFactor = value;
            }
        }
        public static float LastTreeScaleFactor { get; private set; } = m_ScaleFactor;
        public static int MaxTreeUpdateLimit => (int)(DefaultTreeUpdateCount * TreeScaleFactor);

        /* Experimental mode */
        public static bool UseExperimental = false;
        internal static bool EnableProfiling = false;
        /* Tree Performance Related */
        public static int BeginSkipFrameCount = 12;

        /* Tree Snapping */
        public static bool UseTreeSnapping = false;
        public static bool UseExperimentalTreeSnapping = false;
        public static bool UseTreeSnapToBuilding = true;
        public static bool UseTreeSnapToNetwork = true;
        public static bool UseTreeSnapToProp = true;

        /* Lock Forestry */
        public static bool UseLockForestry = false;
        internal static bool PersistentLockForestry = true;

        /* Tree Movement Related */
        public static float TreeSwayFactor = 1f;
        public static bool RandomTreeRotation = true;
        public static int RandomTreeRotationFactor = 1000;

        /* Tree Anarchy Related */
        public static bool UseTreeAnarchy = false;
        public static bool DeleteOnOverlap = false;

        /* Indicators */
        public static bool ShowIndicators = true;

        internal static bool IsInGame = false;

        #region IUserMod
        string IUserMod.Name => m_modName + " " + m_modVersion;
        string IUserMod.Description => m_modDesc;

        public TAMod() {
            try {
                if (GameSettings.FindSettingsFileByName(KeybindingConfigFile) == null) {
                    GameSettings.AddSettingsFile(new SettingsFile[] {
                        new SettingsFile() { fileName = KeybindingConfigFile }
                    });
                }
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        public void OnEnabled() {
            CreateDebugFile();
            SingletonLite<TAPatcher>.instance.CheckIncompatibleMods();
            for (int loadTries = 0; loadTries < 2; loadTries++) {
                if (LoadSettings()) break; // Try 2 times, and if still fails, then use default settings
            }
            if (PersistentLockForestry) UseLockForestry = true;
            TAManager.SetScaleBuffer(MaxTreeLimit);
            HarmonyHelper.DoOnHarmonyReady(() => SingletonLite<TAPatcher>.instance.EnableCore());
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) {
                SingletonLite<TAPatcher>.instance.DisableCore();
            }
            SingletonLite<TALocale>.instance.Destroy();
            SaveSettings();
        }

        public void OnSettingsUI(UIHelperBase helper) {
            SingletonLite<TALocale>.instance.Init();
            ((helper.AddGroup($"{m_modName} -- Version {m_modVersion}") as UIHelper).self as UIPanel).AddUIComponent<TAOptionPanel>();
        }
        #endregion
        #region ILoadingExtension
        void ILoadingExtension.OnCreated(ILoading loading) {
            OutputPluginsList();
            TAManager.Initialize();
            TAManager.InitializeSwayManager();
            if (HarmonyHelper.IsHarmonyInstalled) {
                SingletonLite<TAPatcher>.instance.LateEnable();
            }
        }

        void ILoadingExtension.OnReleased() {
            if (HarmonyHelper.IsHarmonyInstalled) {
                SingletonLite<TAPatcher>.instance.DisableLatePatch();
            }
        }

        void ILoadingExtension.OnLevelLoaded(LoadMode mode) {
            if (ShowIndicators && (Singleton<ToolManager>.instance.m_properties.m_mode & ItemClass.Availability.MapEditor) == ItemClass.Availability.None) {
                UIView.GetAView().FindUIComponent<UIPanel>("InfoPanel").AddUIComponent<TAIndicator>();
            }
            TAOptionPanel.UpdateState(true);
            IsInGame = true;
        }

        void ILoadingExtension.OnLevelUnloading() {
            IsInGame = false;
        }
        #endregion

        private const string SettingsFileName = "TreeAnarchyConfig.xml";
        internal static bool LoadSettings() {
            try {
                if (!File.Exists(SettingsFileName)) {
                    SaveSettings();
                }
                XmlDocument xmlConfig = new XmlDocument();
                xmlConfig.Load(SettingsFileName);
                m_ScaleFactor = float.Parse(xmlConfig.DocumentElement.GetAttribute("ScaleFactor"), NumberStyles.Float, CultureInfo.CurrentCulture.NumberFormat);
                TreeEffectOnWind = bool.Parse(xmlConfig.DocumentElement.GetAttribute("TreeEffectOnWind"));
                UseTreeSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapping"));
                UseExperimentalTreeSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseExperimentalTreeSnapping"));
                UseTreeSnapToBuilding = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapToBuilding"));
                UseTreeSnapToNetwork = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapToNetwork"));
                UseTreeSnapToProp = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeSnapToProp"));
                RandomTreeRotation = bool.Parse(xmlConfig.DocumentElement.GetAttribute("RandomTreeRotation"));
                TreeSwayFactor = float.Parse(xmlConfig.DocumentElement.GetAttribute("TreeSwayFactor"), NumberStyles.Float, CultureInfo.CurrentCulture.NumberFormat);
                UseLockForestry = bool.Parse(xmlConfig.DocumentElement.GetAttribute("LockForestry"));
                PersistentLockForestry = bool.Parse(xmlConfig.DocumentElement.GetAttribute("PersistentLock"));
                ShowIndicators = bool.Parse(xmlConfig.DocumentElement.GetAttribute("ShowIndicators"));
                UseTreeAnarchy = bool.Parse(xmlConfig.DocumentElement.GetAttribute("UseTreeAnarchy"));
                DeleteOnOverlap = bool.Parse(xmlConfig.DocumentElement.GetAttribute("DeleteOnOverlap"));
            } catch {
                SaveSettings(); // Most likely a corrupted file if we enter here. Recreate the file
                return false;
            }
            return true;
        }

        internal static void SaveSettings() {
            XmlDocument xmlConfig = new XmlDocument();
            XmlElement root = xmlConfig.CreateElement("TreeAnarchyConfig");
            _ = root.Attributes.Append(AddElement(xmlConfig, "ScaleFactor", m_ScaleFactor));
            _ = root.Attributes.Append(AddElement(xmlConfig, "TreeEffectOnWind", TreeEffectOnWind));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UseTreeSnapping", UseTreeSnapping));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UseExperimentalTreeSnapping", UseExperimentalTreeSnapping));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UseTreeSnapToBuilding", UseTreeSnapToBuilding));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UseTreeSnapToNetwork", UseTreeSnapToNetwork));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UseTreeSnapToProp", UseTreeSnapToProp));
            _ = root.Attributes.Append(AddElement(xmlConfig, "RandomTreeRotation", RandomTreeRotation));
            _ = root.Attributes.Append(AddElement(xmlConfig, "TreeSwayFactor", TreeSwayFactor));
            _ = root.Attributes.Append(AddElement(xmlConfig, "LockForestry", UseLockForestry));
            _ = root.Attributes.Append(AddElement(xmlConfig, "PersistentLock", PersistentLockForestry));
            _ = root.Attributes.Append(AddElement(xmlConfig, "ShowIndicators", ShowIndicators));
            _ = root.Attributes.Append(AddElement(xmlConfig, "UseTreeAnarchy", UseTreeAnarchy));
            _ = root.Attributes.Append(AddElement(xmlConfig, "DeleteOnOverlap", DeleteOnOverlap));
            xmlConfig.AppendChild(root);
            xmlConfig.Save(SettingsFileName);
        }

        private static XmlAttribute AddElement<T>(XmlDocument doc, string name, T t) {
            XmlAttribute attr = doc.CreateAttribute(name);
            attr.Value = t.ToString();
            return attr;
        }

        private static readonly Stopwatch profiler = new Stopwatch();
        private void CreateDebugFile() {
            profiler.Start();
            /* Create Debug Log File */
            string path = Path.Combine(Application.dataPath, m_debugLogFile);
            using (FileStream debugFile = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine($"--- {m_modName} {m_modVersion} Debug File ---");
                sw.WriteLine(Environment.OSVersion);
                sw.WriteLine($"C# CLR Version {Environment.Version}");
                sw.WriteLine($"Unity Version {Application.unityVersion}");
                sw.WriteLine("-------------------------------------");
            }
        }

        private void OutputPluginsList() {
            using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append, FileAccess.Write, FileShare.None))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine("Mods Installed are:");
                foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                    sw.WriteLine($"=> {info.name}-{(info.userModInstance as IUserMod).Name} {(info.isEnabled ? "** Enabled **" : "** Disabled **")}");
                }
                sw.WriteLine("-------------------------------------");
            }
        }

        internal static void TALog(string msg) {
            var ticks = profiler.ElapsedTicks;
            using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine($"{(ticks / Stopwatch.Frequency):n0}:{(ticks % Stopwatch.Frequency):D7}-{new StackFrame(1, true).GetMethod().Name} ==> {msg}");
            }
        }
    }
}
