using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.PlatformServices;
using System;
using System.Globalization;
using System.Xml;

namespace TreeAnarchy {
    internal class TALocale : SingletonLite<TALocale> {
        private const UInt64 m_thisModID = 2527486462;
        private const string m_defaultLocale = "en";
        private const string m_fileNameTemplate = @"TreeAnarchy.{0}.locale";
        private XmlDocument m_xmlLocale;
        private string m_directory;

        public void OnLocaleChanged() {
            string locale = SingletonLite<LocaleManager>.instance.language;
            if (locale == "zh") {
                if (CultureInfo.InstalledUICulture.Name == "zh-TW") {
                    locale = "zh-TW";
                } else {
                    locale = "zh-CN";
                }
            }
            LoadLocale(CultureInfo.GetCultureInfo(locale));
        }

        private void LoadLocale(CultureInfo culture) {
            string localeFile = String.Format(m_directory + m_fileNameTemplate, culture.Name);
            XmlDocument locale = new XmlDocument();
            try {
                locale.Load(localeFile);
            } catch {
                /* Load default english locale */
                localeFile = String.Format(m_directory + m_fileNameTemplate, m_defaultLocale);
                locale.Load(localeFile);
            }
            m_xmlLocale = locale;
        }

        internal void Init() {
            foreach (PublishedFileId fileID in PlatformService.workshop.GetSubscribedItems()) {
                if (fileID.AsUInt64 == m_thisModID) {
                    m_directory = PlatformService.workshop.GetSubscribedItemPath(fileID) + @"/Locale/";
                    break;
                }
            }
            OnLocaleChanged();
        }

        internal string GetLocale(string name) => m_xmlLocale.GetElementById(name).InnerText;
    }
}
