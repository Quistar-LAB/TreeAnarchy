using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using System;
using System.Diagnostics;
using System.Threading;
using TreeAnarchy.Patches;
using UnityEngine;
using Debug = UnityEngine.Debug;
using static TreeAnarchy.TAConfig;
using static TreeAnarchy.Utils.XMLSetting;
using static TreeAnarchy.TASerializableDataExtension;

namespace TreeAnarchy
{
    public class TAMod : LoadingExtensionBase, IUserMod
    {
        private const string Version = @"0.5.7";
        private const string ModName = @"Unlimited Trees: Rebooted";
        public string Name => $"{ModName} - {Version}";
        public string Description => @"This is a reboot of the original Unlimited Trees Mod. Let's you plant way more trees";

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
            private UICheckBox TreeRotation;
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
                SaveSettings();
            }
            
            private void TreeRotationHandler(bool b)
            {
                RandomTreeRotation = b;
                SaveSettings();
            }

            private void ScaleFactorHandler(float f)
            {
                TreeScaleFactor = f;
                MaxTreeLabel.text = $"Maximum Supported Number of Trees: {(uint)(MaxTreeLimit)}";
                SaveSettings();
            }

            private void TreeSwayHandler(UIComponent component, float value)
            {
                TreeSwayFactor = value;
                SaveSettings();
            }

            private void DebugHandler(bool b)
            {
                DebugMode = b;
                if (DebugMode & Singleton<LoadingManager>.instance.m_loadingComplete) PurgeSnappingData.Show();
                else PurgeSnappingData.Hide();
            }

            private void PurgeDataHandler() => TASerializableDataExtension.PurgeData();

            internal void UpdateState()
            {
                if (isInGame)
                {
                    WindEffect.Disable();
                    ScaleFactor.Disable();
                    Debug.Enable();
                    return;
                }
                WindEffect.Enable();
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
                Space = (UIPanel)group.AddSpace(70);
                WindEffectLabel = (UILabel)Space.AddUIComponent<UILabel>();
                WindEffectLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                WindEffectLabel.width = 600;
                WindEffectLabel.height = 70;
                WindEffectLabel.autoSize = false;
                WindEffectLabel.relativePosition = new Vector3(25, 0);
                WindEffectLabel.wordWrap = true;
                WindEffectLabel.text = "Enable/Disable the normal game behavior of tree's height \ndiluting and weakening the wind on the map.\nOption should be set before loading a map.";

                TreeSnap = (UICheckBox)group.AddCheckbox(@"Tree Snapping/Anarchy Support", UseTreeSnapping, (b) => TreeSnapHandler(b));
                TreeSnap.tooltip = @"Allows Trees to be placed anywhere if enabled";
                Space = (UIPanel)group.AddSpace(50);
                TreeSnapLabel = (UILabel)panel.AddUIComponent<UILabel>();
                TreeSnapLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                TreeSnapLabel.width = 600;
                TreeSnapLabel.height = 50;
                TreeSnapLabel.relativePosition = new Vector3(25, 0);
                TreeSnapLabel.wordWrap = true;
                TreeSnapLabel.autoSize = false;
                TreeSnapLabel.text = "Enables/Disable trees to be placed anywhere imaginable.\nFunctions like the original TreeSnapping Mod";

                TreeRotation = (UICheckBox)group.AddCheckbox(@"Random Tree Rotation", RandomTreeRotation, (b) => TreeRotationHandler(b));
                TreeRotation.tooltip = @"Enable/Disable random tree rotation during placement";
                TreeRotation.width = 400;
                UIPanel SwayPanel = panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate")) as UIPanel;
                SwayPanel.Find<UILabel>("Label").text = "Tree Sway Factor";
                UISlider uISlider = SwayPanel.Find<UISlider>("Slider");
                uISlider.minValue = 0f;
                uISlider.maxValue = 1f;
                uISlider.stepSize = 0.1f;
                uISlider.value = TreeSwayFactor;
                uISlider.eventValueChanged += TreeSwayHandler;
                SwayPanel.AlignTo(TreeRotation, UIAlignAnchor.TopRight);
                SwayPanel.relativePosition = new Vector3(300, -5);
                UILabel RotationLabel = (UILabel)panel.AddUIComponent<UILabel>();
                RotationLabel.AlignTo(TreeRotation, UIAlignAnchor.TopLeft);
                RotationLabel.width = 600;
                RotationLabel.height = 50;
                RotationLabel.relativePosition = new Vector3(25, 55);
                RotationLabel.wordWrap = true;
                RotationLabel.autoSize = false;
                RotationLabel.text = "Useful eyecandy for your pleasure. Reducing tree sway may improve FPS";
                group.AddSpace(20);

                UIHelper ScaleGroup = helper.AddGroup($"Configure Tree Limit") as UIHelper;
                ScaleFactor = (UISlider)ScaleGroup.AddSlider(@"Max Supported Tree", MinScaleFactor, MaxScaleFactor, 0.5f, TreeScaleFactor, (f) => ScaleFactorHandler(f));
                ScaleFactor.tooltip = @"Increase or decrease supported number of trees";
                Space = (UIPanel)ScaleGroup.AddSpace(20);
                ScaleFactor.isVisible = true;
                ScaleFactor.width = 500;
                MaxTreeLabel = (UILabel)Space.AddUIComponent<UILabel>();
                MaxTreeLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                MaxTreeLabel.relativePosition = new Vector3(0, 0);
                MaxTreeLabel.text = $"Maximum Supported Number of Trees: {(uint)MaxTreeLimit}";
                MaxTreeLabel.width = 500;
                MaxTreeLabel.autoSize = true;
                MaxTreeLabel.height = 50;
                MaxTreeLabel.Show();
                Space = (UIPanel)ScaleGroup.AddSpace(50);
                ImportantLabel = (UILabel)panel.AddUIComponent<UILabel>();
                ImportantLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                ImportantLabel.relativePosition = new Vector3(0, 0);
                ImportantLabel.text = @"Important! Must be set before entering map";
                group.AddSpace(20);

                PurgeSnappingData = (UIButton)ScaleGroup.AddButton(@"Purge TreeSnapping Data", () => PurgeDataHandler());

                Debug = (UICheckBox)ScaleGroup.AddCheckbox(@"Debug Mode - In-game usage", DebugMode, (b) => DebugHandler(b));
                Space = (UIPanel)ScaleGroup.AddSpace(20);
                PurgeSnappingData.AlignTo(Space, UIAlignAnchor.TopLeft);
                PurgeSnappingData.Hide();
            }
        }
        private OptionPanel optionPanel = null;

        #region IUserMod
        public void OnEnabled()
        {
            LoadSettings();
            Patcher.Setup();
        }

        public void OnDisabled()
        {
            Patcher.Remove();
            SaveSettings();
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
            isInGame = true;
            base.OnCreated(loading);
        }

        public override void OnReleased()
        {
            isInGame = false;
            base.OnReleased();
        }
        public override void OnLevelLoaded(LoadMode mode)
        {
            if (OldFormatLoaded)
            {
                /* When using original CO or Unlimited Trees Mod, the posY is never
                 * considered, and it could be any random number, usually 0. When
                 * saving into our new format. We need to actually store their posY
                 * so we have to make sure its initialized.
                 */
                TreeManager manager = Singleton<TreeManager>.instance;
                for (uint i = 1; i < MaxTreeLimit; i++)
                {
                    if (manager.m_trees.m_buffer[i].m_flags != 0)
                    {
                        if ((manager.m_trees.m_buffer[i].m_flags & 32) == 0)
                        {
                            Vector3 position = manager.m_trees.m_buffer[i].Position;
                            position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
                            ushort terrainHeight = (ushort)Mathf.Clamp(Mathf.RoundToInt(position.y * 64f), 0, 65535);
                            if(manager.m_trees.m_buffer[i].m_posY < terrainHeight)
                            {
                                manager.m_trees.m_buffer[i].m_posY = terrainHeight;
                            }
                        }
                    }
                }
                OldFormatLoaded = false;
            }

            base.OnLevelLoaded(mode);
        }
        #endregion
    }
}