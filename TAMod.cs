using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using UI;
using UnityEngine;

namespace TreeAnarchy {
    public class TAMod : ILoadingExtension, IUserMod {
        internal const string m_modVersion = @"1.2.9";
        internal const string m_assemblyVersion = m_modVersion + @".*";
        private const string m_modName = @"Tree Anarchy";
        private const string m_modDesc = @"Lets you plant more trees with tree snapping";
        private const string m_debugLogFile = @"00TreeAnarchyDebug.log";
        internal const string KeybindingConfigFile = @"TreeAnarchyKeyBindSetting";

        /* Some standard constant definition for tree limits */
        private static float m_ScaleFactor = 4f;
        public const int DefaultTreeLimit = 262144;
        public const int DefaultTreeUpdateCount = 4096;
        /* Unlimited Trees Related */
        internal static int RemoveReplaceOrKeep = 0;
        internal static bool OldFormatLoaded = false;
        private static bool m_treeEffectOnWind = true;
        internal static bool TreeEffectOnWind {
            get => m_treeEffectOnWind;
            set {
                if (m_treeEffectOnWind != value) {
                    m_treeEffectOnWind = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }
        internal static int LastMaxTreeLimit = DefaultTreeLimit;
        internal static float TreeScaleFactor {
            get => m_ScaleFactor;
            set {
                LastTreeScaleFactor = m_ScaleFactor;
                m_ScaleFactor = value;
                ThreadPool.QueueUserWorkItem(SaveSettings);
            }
        }
        internal static float LastTreeScaleFactor = m_ScaleFactor;
        internal static int MaxTreeLimit => (int)(DefaultTreeLimit * TreeScaleFactor);
        internal static int MaxTreeUpdateLimit => (int)(DefaultTreeUpdateCount * TreeScaleFactor);
        internal static int CheckLowLimit => MaxTreeLimit - 12144;
        internal static int CheckHighLimit => MaxTreeLimit - 5;

        /* Tree Snapping */
        private static bool m_useTreeSnapping = false;
        internal static bool UseTreeSnapping {
            get => m_useTreeSnapping;
            set {
                if (m_useTreeSnapping != value) {
                    m_useTreeSnapping = value;
                    if (TAOptionPanel.TreeSnapCB) TAOptionPanel.TreeSnapCB.isChecked = value;
                    UIIndicator.SnapIndicator?.SetState(value);
                    MoveIt.MoveItTool.treeSnapping = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }
        internal static bool UseExperimentalTreeSnapping = false;
        internal static bool UseTreeSnapToBuilding = true;
        internal static bool UseTreeSnapToNetwork = true;
        internal static bool UseTreeSnapToProp = true;

        /* Lock Forestry */
        private static bool m_useLockForestry = false;
        internal static bool UseLockForestry {
            get => m_useLockForestry;
            set {
                if (m_useLockForestry != value) {
                    m_useLockForestry = value;
                    if (TAOptionPanel.TreeSnapCB) TAOptionPanel.LockForestryCB.isChecked = value;
                    UIIndicator.LockForestryIndicator?.SetState(value);
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }
        private static bool m_persistentLockForestry = true;
        internal static bool PersistentLockForestry {
            get => m_persistentLockForestry;
            set {
                if (m_persistentLockForestry != value) {
                    m_persistentLockForestry = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }

        /* Tree Movement Related */
        private static float m_treeSwayFactor = 1f;
        internal static float TreeSwayFactor {
            get => m_treeSwayFactor;
            set {
                if (m_treeSwayFactor != value) {
                    m_treeSwayFactor = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }

        /* Tree Anarchy Related */
        private static bool m_useTreeAnarchy = true;
        internal static bool UseTreeAnarchy {
            get => m_useTreeAnarchy;
            set {
                if (m_useTreeAnarchy != value) {
                    m_useTreeAnarchy = value;
                    if (TAOptionPanel.TreeAnarchyCB) TAOptionPanel.TreeAnarchyCB.isChecked = value;
                    UIIndicator.AnarchyIndicator?.SetState(value);
                }
            }
        }

        private static bool m_hideTreeOnLoad = false;
        internal static bool HideTreeOnLoad {
            get => m_hideTreeOnLoad;
            set {
                if(m_hideTreeOnLoad != value) {
                    m_hideTreeOnLoad = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }

        private static bool m_deleteOnOverlap = false;
        internal static bool DeleteOnOverlap {
            get => m_deleteOnOverlap;
            set {
                if (m_deleteOnOverlap != value) {
                    m_deleteOnOverlap = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }

        /* Tree LOD Fix Related */
        private static bool m_useTreeLODFix = true;
        internal static bool UseTreeLODFix {
            get => m_useTreeLODFix;
            set {
                if (m_useTreeLODFix != value) {
                    m_useTreeLODFix = value;
                    UseTreeAnarchy = !value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }
        private static TAManager.TreeLODResolution m_treeLODResolution = TAManager.TreeLODResolution.Medium;
        internal static TAManager.TreeLODResolution TreeLODSelectedResolution {
            get => m_treeLODResolution;
            set {
                if (m_treeLODResolution != value) {
                    m_treeLODResolution = value;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                }
            }
        }

        internal static bool IsInGame { get; private set; } = false;

        #region IUserMod
        public string Name => m_modName + " " + m_modVersion;
        public string Description => m_modDesc;

        public TAMod() {
            try {
                CreateDebugFile();
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        public void OnEnabled() {
            GameSettings.AddSettingsFile(new SettingsFile[] {
                new SettingsFile() { fileName = KeybindingConfigFile }
            });
            TALocale.Init();
            TAPatcher.CheckIncompatibleMods();
            for (int loadTries = 0; loadTries < 2; loadTries++) {
                if (LoadSettings()) break; // Try 2 times, and if still fails, then use default settings
            }
            if (PersistentLockForestry) UseLockForestry = true;
            if (HideTreeOnLoad) UseTreeAnarchy = false;
            HarmonyHelper.DoOnHarmonyReady(TAPatcher.EnableCore);
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) {
                TAPatcher.DisableCore();
            }
            TALocale.Destroy();
            SaveSettings();
        }

        public void OnSettingsUI(UIHelperBase helper) {
            TALocale.OnLocaleChanged();
            LocaleManager.eventLocaleChanged += TALocale.OnLocaleChanged;
            TAOptionPanel.SetupPanel((helper.AddGroup(m_modName + @" -- Version " + m_modVersion) as UIHelper).self as UIPanel);
        }
        #endregion
        #region ILoadingExtension
        public void OnCreated(ILoading loading) {
            CheckAllPlugins();
            TAManager.Initialize();
            if (HarmonyHelper.IsHarmonyInstalled) {
                TAPatcher.LateEnable();
            }
        }

        public void OnReleased() {
            if (HarmonyHelper.IsHarmonyInstalled) {
                TAPatcher.DisableLatePatch();
            }
        }

        public void OnLevelLoaded(LoadMode mode) {
            IsInGame = true;
            if (Singleton<ToolManager>.instance.m_properties.m_mode != ItemClass.Availability.AssetEditor) {
                UIIndicator indicatorPanel = UIIndicator.Setup();
                if (indicatorPanel) {
                    UIIndicator.UIIcon treeSnap = default;
                    treeSnap = indicatorPanel.AddSnappingIcon(TALocale.GetLocale(@"TreeSnapIsOn"), TALocale.GetLocale(@"TreeSnapIsOff"), UseTreeSnapping, (_, p) => {
                        UseTreeSnapping = !UseTreeSnapping;
                    }, out bool finalState);
                    if (finalState != UseTreeSnapping) {
                        UseTreeSnapping = finalState;
                    }
                    UIIndicator.UIIcon treeAnarchy = default;
                    treeAnarchy = indicatorPanel.AddAnarchyIcon(TALocale.GetLocale(@"TreeAnarchyIsOn"), TALocale.GetLocale(@"TreeAnarchyIsOff"), UseTreeAnarchy, (_, p) => {
                        UseTreeAnarchy = !UseTreeAnarchy;
                    }, out finalState);
                    if (finalState != UseTreeAnarchy) {
                        UseTreeAnarchy = finalState;
                    }
                    UIIndicator.UIIcon lockForestry = default;
                    lockForestry = indicatorPanel.AddLockForestryIcon(TALocale.GetLocale(@"LockForestryIsOn"), TALocale.GetLocale(@"LockForestryIsOff"), UseLockForestry, (_, p) => {
                        UseLockForestry = !UseLockForestry;
                    });
                }
            }
            TAOptionPanel.UpdateState(true);
        }

        void ILoadingExtension.OnLevelUnloading() {
            IsInGame = false;
        }
        #endregion

        private const string SettingsFileName = @"TreeAnarchyConfig.xml";
        internal static bool LoadSettings() {
            try {
                if (!File.Exists(SettingsFileName)) {
                    SaveSettings();
                }
                XmlDocument xmlConfig = new XmlDocument {
                    XmlResolver = null
                };
                xmlConfig.Load(SettingsFileName);
                m_ScaleFactor = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"ScaleFactor"), NumberStyles.Float, CultureInfo.CurrentCulture.NumberFormat);
                m_treeEffectOnWind = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"TreeEffectOnWind"));
                m_useTreeSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseTreeSnapping"));
                UseExperimentalTreeSnapping = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseExperimentalTreeSnapping"));
                UseTreeSnapToBuilding = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseTreeSnapToBuilding"));
                UseTreeSnapToNetwork = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseTreeSnapToNetwork"));
                UseTreeSnapToProp = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseTreeSnapToProp"));
                m_treeSwayFactor = float.Parse(xmlConfig.DocumentElement.GetAttribute(@"TreeSwayFactor"), NumberStyles.Float, CultureInfo.CurrentCulture.NumberFormat);
                m_useLockForestry = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"LockForestry"));
                PersistentLockForestry = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"PersistentLock"));
                m_deleteOnOverlap = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"DeleteOnOverlap"));
                m_useTreeLODFix = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"UseTreeLODFix"));
                m_treeLODResolution = (TAManager.TreeLODResolution)int.Parse(xmlConfig.DocumentElement.GetAttribute(@"TreeLODSelectedResolution"));
                try {
                    m_hideTreeOnLoad = bool.Parse(xmlConfig.DocumentElement.GetAttribute(@"HideTreeOnLoad"));
                } catch {
                    m_hideTreeOnLoad = false;
                }
            } catch {
                SaveSettings(); // Most likely a corrupted file if we enter here. Recreate the file
                return false;
            }
            return true;
        }

        private static readonly object settingsLock = new object();
        internal static void SaveSettings(object _ = null) {
            Monitor.Enter(settingsLock);
            try {
                XmlDocument xmlConfig = new XmlDocument {
                    XmlResolver = null
                };
                XmlElement root = xmlConfig.CreateElement(@"TreeAnarchyConfig");
                root.Attributes.Append(AddElement(xmlConfig, @"ScaleFactor", m_ScaleFactor));
                root.Attributes.Append(AddElement(xmlConfig, @"TreeEffectOnWind", m_treeEffectOnWind));
                root.Attributes.Append(AddElement(xmlConfig, @"UseTreeSnapping", m_useTreeSnapping));
                root.Attributes.Append(AddElement(xmlConfig, @"UseExperimentalTreeSnapping", UseExperimentalTreeSnapping));
                root.Attributes.Append(AddElement(xmlConfig, @"UseTreeSnapToBuilding", UseTreeSnapToBuilding));
                root.Attributes.Append(AddElement(xmlConfig, @"UseTreeSnapToNetwork", UseTreeSnapToNetwork));
                root.Attributes.Append(AddElement(xmlConfig, @"UseTreeSnapToProp", UseTreeSnapToProp));
                root.Attributes.Append(AddElement(xmlConfig, @"TreeSwayFactor", m_treeSwayFactor));
                root.Attributes.Append(AddElement(xmlConfig, @"LockForestry", m_useLockForestry));
                root.Attributes.Append(AddElement(xmlConfig, @"PersistentLock", PersistentLockForestry));
                root.Attributes.Append(AddElement(xmlConfig, @"DeleteOnOverlap", m_deleteOnOverlap));
                root.Attributes.Append(AddElement(xmlConfig, @"UseTreeLODFix", m_useTreeLODFix));
                root.Attributes.Append(AddElement(xmlConfig, @"TreeLODSelectedResolution", (int)m_treeLODResolution));
                root.Attributes.Append(AddElement(xmlConfig, @"HideTreeOnLoad", m_hideTreeOnLoad));
                xmlConfig.AppendChild(root);
                xmlConfig.Save(SettingsFileName);
            } finally {
                Monitor.Exit(settingsLock);
            }
        }

        private static XmlAttribute AddElement<T>(XmlDocument doc, string name, T t) {
            XmlAttribute attr = doc.CreateAttribute(name);
            attr.Value = t.ToString();
            return attr;
        }

        private static readonly Stopwatch profiler = new Stopwatch();
        private static readonly object fileLock = new object();
        private void CreateDebugFile() {
            profiler.Start();
            /* Create Debug Log File */
            string path = Path.Combine(Application.dataPath, m_debugLogFile);
            using (FileStream debugFile = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (StreamWriter sw = new StreamWriter(debugFile)) {
                sw.WriteLine(@"--- " + m_modName + ' ' + m_modVersion + " Debug File ---");
                sw.WriteLine(Environment.OSVersion);
                sw.WriteLine(@"C# CLR Version " + Environment.Version);
                sw.WriteLine(@"Unity Version " + Application.unityVersion);
                sw.WriteLine(@"-------------------------------------");
            }
        }

        private void CheckAllPlugins() {
            Monitor.Enter(fileLock);
            try {
                using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append, FileAccess.Write, FileShare.None))
                using (StreamWriter sw = new StreamWriter(debugFile)) {
                    sw.WriteLine(@"Mods Installed are:");
                    foreach (PluginManager.PluginInfo info in Singleton<PluginManager>.instance.GetPluginsInfo()) {
                        sw.WriteLine($"=> {info.name}-{(info.userModInstance as IUserMod).Name} {(info.isEnabled ? @"** Enabled **" : @"** Disabled **")}");
                    }
                    sw.WriteLine(@"-------------------------------------");
                }
            } finally {
                Monitor.Exit(fileLock);
            }
        }

        internal static void TALog(string msg) {
            var ticks = profiler.ElapsedTicks;
            Monitor.Enter(fileLock);
            try {
                using (FileStream debugFile = new FileStream(Path.Combine(Application.dataPath, m_debugLogFile), FileMode.Append))
                using (StreamWriter sw = new StreamWriter(debugFile)) {
                    sw.WriteLine($"{(ticks / Stopwatch.Frequency):n0}:{(ticks % Stopwatch.Frequency):D7}-{new StackFrame(1, true).GetMethod().Name} ==> {msg}");
                }
            } finally {
                Monitor.Exit(fileLock);
            }
        }
    }
}
