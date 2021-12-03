using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.PlatformServices;
using System;
using System.Globalization;
using System.IO;
using System.Xml;
using TreeAnarchy.Localization;

namespace TreeAnarchy {
    internal static class TALocale {
        private const ulong m_thisModID = 2527486462;
        private const ulong m_betaModID = 2584051448;
        private const string DefaultEnLocale = @"TreeAnarchy.en.locale";
        private static XmlDocument m_xmlLocale;
        private static string m_directory;
        private static bool isInitialized = false;

        public static void OnLocaleChanged() {
            string locale = SingletonLite<LocaleManager>.instance.language;
            if (locale == @"zh") {
                if (CultureInfo.InstalledUICulture.Name == @"zh-TW") {
                    locale = @"zh-TW";
                } else {
                    locale = @"zh-CN";
                }
            } else if (locale == @"pt") {
                if (CultureInfo.InstalledUICulture.Name == @"pt-BR") {
                    locale = @"pt-BR";
                }
            } else {
                switch (CultureInfo.InstalledUICulture.Name) {
                case @"ms":
                case @"ms-MY":
                    locale = @"ms";
                    break;
                case @"ja":
                case @"ja-JP":
                    locale = @"ja";
                    break;
                }
            }
            LoadLocale(locale);
            TAOptionPanel[] optionPanel = UnityEngine.Object.FindObjectsOfType<TAOptionPanel>();
            foreach (var panel in optionPanel) {
                panel.Invalidate();
            }
        }

        private static void LoadLocale(string culture) {
            XmlDocument locale = new XmlDocument {
                XmlResolver = null
            };
            try {
                string localeFile = m_directory + @"TreeAnarchy." + culture + @".locale";
                locale.Load(localeFile);
            } catch {
                /* Load default english locale embedded in assembly */
                using (MemoryStream s = new MemoryStream(DefaultLocale.TreeAnarchy_en)) {
                    locale.Load(s);
                }
            } finally {
                m_xmlLocale = locale;
            }
        }

        internal static void Init() {
            if (!isInitialized) {
                try {
                    foreach (PublishedFileId fileID in PlatformService.workshop.GetSubscribedItems()) {
                        if (fileID.AsUInt64 == m_thisModID) {
                            string dir = PlatformService.workshop.GetSubscribedItemPath(fileID) + @"/Locale/";
                            if (Directory.Exists(dir) && File.Exists(dir + DefaultEnLocale)) {
                                m_directory = dir;
                                break;
                            }
                        }
                        if (fileID.AsUInt64 == m_betaModID) {
                            string dir = PlatformService.workshop.GetSubscribedItemPath(fileID) + @"/Locale/";
                            if (Directory.Exists(dir) && File.Exists(dir + DefaultEnLocale)) {
                                m_directory = dir;
                                break;
                            }
                        }
                    }
                    if (m_directory is null) {
                        string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"/Colossal Order/Cities_Skylines/Addons/Mods/TreeAnarchy/Locale/";
                        if (Directory.Exists(dir) && File.Exists(dir + DefaultEnLocale)) {
                            m_directory = dir;
                        }
                    }
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
                isInitialized = true;
            }
        }

        internal static void Destroy() {
            LocaleManager.eventLocaleChanged -= OnLocaleChanged;
        }

        internal static string GetLocale(string name) => m_xmlLocale.GetElementById(name).InnerText;
    }
}
