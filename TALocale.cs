using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.PlatformServices;
using System;
using System.Globalization;
using System.Xml;

namespace TreeAnarchy {
    internal class TALocale : SingletonLite<TALocale> {
        private const ulong m_thisModID = 2527486462;
        private const ulong m_betaModID = 2584051448;
        private const string m_defaultLocale = "en";
        private const string m_fileNameTemplate = @"TreeAnarchy.{0}.locale";
        private XmlDocument m_xmlLocale;
        private string m_directory;
        private bool isInitialized = false;

        public void OnLocaleChanged() {
            string locale = SingletonLite<LocaleManager>.instance.language;
            if (locale == "zh") {
                if (CultureInfo.InstalledUICulture.Name == "zh-TW") {
                    locale = "zh-TW";
                } else {
                    locale = "zh-CN";
                }
            } else if (locale == "pt") {
                if (CultureInfo.InstalledUICulture.Name == "pt-BR") {
                    locale = "pt-BR";
                }
            } else {
                switch (CultureInfo.InstalledUICulture.Name) {
                case "ms":
                case "ms-MY":
                    locale = "ms";
                    break;
                case "ja":
                case "ja-JP":
                    locale = "ja";
                    break;
                }
            }
            LoadLocale(locale);
            TAOptionPanel[] optionPanel = UnityEngine.Object.FindObjectsOfType<TAOptionPanel>();
            foreach (var panel in optionPanel) {
                panel.Invalidate();
            }
        }

        private void LoadLocale(string culture) {
            string localeFile = string.Format(m_directory + m_fileNameTemplate, culture);
            XmlDocument locale = new XmlDocument();
            try {
                locale.Load(localeFile);
            } catch {
                /* Load default english locale */
                localeFile = string.Format(m_directory + m_fileNameTemplate, m_defaultLocale);
                locale.Load(localeFile);
            }
            m_xmlLocale = locale;
        }

        internal void Init() {
            if (!isInitialized) {
                try {
                    foreach (PublishedFileId fileID in PlatformService.workshop.GetSubscribedItems()) {
                        if (fileID.AsUInt64 == m_thisModID) {
                            m_directory = PlatformService.workshop.GetSubscribedItemPath(fileID) + @"/Locale/";
                            break;
                        }
                        if (fileID.AsUInt64 == m_betaModID) {
                            m_directory = PlatformService.workshop.GetSubscribedItemPath(fileID) + @"/Locale/";
                            break;
                        }
                    }
                    if (m_directory is null) {
                        m_directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"/Colossal Order/Cities_Skylines/Addons/Mods/TreeAnarchy/Locale/";
                    }
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
                LocaleManager.eventLocaleChanged += OnLocaleChanged;
                OnLocaleChanged();
                isInitialized = true;
            }
        }

        internal void Destroy() {
            LocaleManager.eventLocaleChanged -= OnLocaleChanged;
        }

        internal string GetLocale(string name) => m_xmlLocale.GetElementById(name).InnerText;
    }
}
