using ICities;
using static TreeAnarchy.TAConfig;


namespace TreeAnarchy {
    public class TAMod : ITerrainExtension, ILoadingExtension, IUserMod {
        private const string m_modName = "Unlimited Trees: Reboot";
        internal const string m_modVersion = "0.8.2";
        private const string m_modDesc = "An improved Unlimited Trees Mod. Lets you plant more trees with tree snapping";

        internal static bool IsInGame = false;

        #region IUserMod
        string IUserMod.Name => $"{m_modName} {m_modVersion}";
        string IUserMod.Description => m_modDesc;
        public void OnEnabled() {
            LoadSettings();
            TAPatcher.EnableCore();
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
            TAUI.UpdateState(true);
        }

        void ILoadingExtension.OnLevelUnloading() {
        }
        #endregion
        #region ITerrainExtension
        void ITerrainExtension.OnCreated(ITerrain terrain) {
        }

        void ITerrainExtension.OnReleased() {
        }

        void ITerrainExtension.OnAfterHeightsModified(float minX, float minZ, float maxX, float maxZ) {
        }
        #endregion
    }
}
