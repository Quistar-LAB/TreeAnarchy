using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using System;
using System.Threading;
using TreeAnarchy.Patches;
using UnityEngine;
using static TreeAnarchy.TAConfig;
using static TreeAnarchy.Utils.XMLSetting;
using static TreeAnarchy.TASerializableDataExtension;

namespace TreeAnarchy
{
    public class TAMod : LoadingExtensionBase, IUserMod
    {
        private const string Version = @"0.2.0";
        private const string ModName = @"Unlimited Trees: Rebooted";
        public string Name => $"{ModName} - {Version}";
        public string Description => "Reboot of the Unlimited Trees Mod. Tree snapping is also enabled";

        internal class OptionPanel 
        {
            private UIHelper group;
            private UIPanel panel;
            private UILabel WindEffectLabel;
            private UILabel TreeSnapLabel;
            private UILabel ImportantLabel;
            private UILabel MaxTreeLabel;
            private UICheckBox WindEffect;
            private UIButton PurgeSnappingData;
            private UICheckBox TreeSnap;
            private UICheckBox Debug;
            private UISlider ScaleFactor;
            internal bool IsInGame { get; set; } = false;

            private void WindEffectHandler(bool b)
            {
                TreeEffectOnWind = b;
                SaveSettings();
            }

            private void TreeSnapHandler(bool b)
            {
                UseTreeSnapping = b;
                if (!UseTreeSnapping) TreeSnappingPatcher.DisablePatches(Patcher.m_Harmony, Patcher.HARMONYID);
                else TreeSnappingPatcher.EnablePatches(Patcher.m_Harmony);
                SaveSettings();
            }

            private void ScaleFactorHandler(float f)
            {
                TreeScaleFactor = f;
                MaxTreeLabel.text = $"Maximum Supported Number of Trees: {(uint)(MaxTreeLimit)}";
                SaveSettings();
            }

            private void DebugHandler(bool b)
            {
                DebugMode = b;
                if (DebugMode & Singleton<LoadingManager>.instance.m_loadingComplete) PurgeSnappingData.Show();
                else PurgeSnappingData.Hide();
            }

            private void PurgeDataHandler()
            {
                TASerializableDataExtension.PurgeData();
            }

            internal void UpdateState()
            {
                if (isInGame)
                {
                    UnityEngine.Debug.Log("TreeSnapping: Showing in-game options");
                    WindEffect.Disable();
                    TreeSnap.Disable();
                    ScaleFactor.Disable();
                    Debug.Enable();
                    return;
                }
                UnityEngine.Debug.Log("TreeSnapping: Showing initial game options");
                WindEffect.Enable();
                TreeSnap.Enable();
                ScaleFactor.Enable();
                Debug.Disable();
            }

            internal void CreatePanel(UIHelperBase helper)
            {
                UIPanel Space;
                group = helper.AddGroup($"{ModName} -- Version {Version}") as UIHelper;
                panel = group.self as UIPanel;

                WindEffect = (UICheckBox)group.AddCheckbox(@"Enable Tree Effect on Wind [Original game default is enabled]", TreeEffectOnWind, (b) => WindEffectHandler(b));
                WindEffect.tooltip = "Enable/Disable the normal game behavior of letting tree's height effect dilute the wind map.\nOption should be set before loading a map.";
                Space = (UIPanel)group.AddSpace(80);
                WindEffectLabel = (UILabel)Space.AddUIComponent<UILabel>();
                WindEffectLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                WindEffectLabel.width = 600;
                WindEffectLabel.height = 80;
                WindEffectLabel.autoSize = false;
                WindEffectLabel.relativePosition = new Vector3(25, 0);
                WindEffectLabel.wordWrap = true;
                WindEffectLabel.text = "Enable/Disable the normal game behavior of tree's height \ndiluting and weakening the wind on the map.\nOption should be set before loading a map.";
                group.AddSpace(10);

                TreeSnap = (UICheckBox)group.AddCheckbox(@"Tree Snapping/Anarchy Support", UseTreeSnapping, (b) => TreeSnapHandler(b));
                TreeSnap.tooltip = @"Allows Trees to be placed anywhere if enabled";
                Space = (UIPanel)group.AddSpace(50);
                TreeSnapLabel = (UILabel)panel.AddUIComponent<UILabel>();
                TreeSnapLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                TreeSnapLabel.width = 600;
                TreeSnapLabel.height = 80;
                TreeSnapLabel.relativePosition = new Vector3(25, 0);
                TreeSnapLabel.wordWrap = true;
                TreeSnapLabel.autoSize = false;
                TreeSnapLabel.text = "Enables/Disable trees to be placed anywhere imaginable.\nFunctions like the original TreeSnapping Mod";
                group.AddSpace(10);

                ScaleFactor = (UISlider)group.AddSlider(@"Max Supported Tree", MinScaleFactor, MaxScaleFactor, 0.5f, TreeScaleFactor, (f) => ScaleFactorHandler(f));
                ScaleFactor.tooltip = @"Increase or decrease supported number of trees";
                Space = (UIPanel)group.AddSpace(20);
                ScaleFactor.isVisible = true;
                MaxTreeLabel = (UILabel)Space.AddUIComponent<UILabel>();
                MaxTreeLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                MaxTreeLabel.relativePosition = new Vector3(0, 0);
                MaxTreeLabel.text = $"Maximum Supported Number of Trees: {(uint)MaxTreeLimit}";
                MaxTreeLabel.width = 500;
                MaxTreeLabel.autoSize = true;
                MaxTreeLabel.height = 50;
                MaxTreeLabel.Show();
                Space = (UIPanel)group.AddSpace(50);
                ImportantLabel = (UILabel)panel.AddUIComponent<UILabel>();
                ImportantLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                ImportantLabel.relativePosition = new Vector3(0, 0);
                ImportantLabel.text = @"Important! Must be set before entering map";
                group.AddSpace(20);

                PurgeSnappingData = (UIButton)group.AddButton(@"Purge TreeSnapping Data", () => PurgeDataHandler());

                Debug = (UICheckBox)group.AddCheckbox(@"Debug Mode - In-game usage", DebugMode, (b) => DebugHandler(b));
                Space = (UIPanel)group.AddSpace(20);
                PurgeSnappingData.AlignTo(Space, UIAlignAnchor.TopLeft);
                PurgeSnappingData.Hide();
            }
        }
        private OptionPanel optionPanel = null;

        #region IUserMod
        public void OnEnabled()
        {
            LoadSettings(); // Loading settings first
            Patcher.Setup();
        }

        public void OnDisabled()
        {
            SaveSettings();
            Patcher.Remove();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                if(optionPanel == null) optionPanel = new OptionPanel();
                optionPanel.CreatePanel(helper);
                optionPanel.UpdateState();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        #endregion

        #region LoadingExtensionBase
        public override void OnCreated(ILoading loading)
        {
            Debug.Log($"TreeSnapping:: Entering OnCreated(): loading={loading}");
            isInGame = true;

            base.OnCreated(loading);
        }

        public override void OnReleased()
        {
            isInGame = false;
            base.OnReleased();
        }
        #endregion
    }
}