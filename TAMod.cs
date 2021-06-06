using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Plugins;
using ICities;
using static TreeAnarchy.TAConfig;


namespace TreeAnarchy {
    public class TAMod : ITerrainExtension, ILoadingExtension, IUserMod {
        private const string m_modName = "Unlimited Trees: Reboot";
        private const string m_modVersion = "0.6.2";
        private const string m_modDesc = "An improved Unlimited Trees Mod. Lets you plant more trees with tree snapping";

        #region OptionPanel
        private static class OptionPanel {
            static private UILabel MaxTreeLabel;
            static private UICheckBox WindEffect;
            static private UIButton PurgeSnappingData;
            static private UICheckBox TreeSnap;
            static private UICheckBox TreeRotation;
            static private UICheckBox Debug;
            static private UISlider ScaleFactor;

            internal static bool IsInGame = false;

            private static void TreeSwayHandler(UIComponent component, float value) {
                TreeSwayFactor = value;
                SaveSettings();
            }

            private static void DebugHandler(bool b) {
                DebugMode = b;
                if (DebugMode & Singleton<LoadingManager>.instance.m_loadingComplete) PurgeSnappingData.Show();
                else PurgeSnappingData.Hide();
            }

            private static void PurgeDataHandler() {
                //=> TASerializableDataExtension.PurgeData();
            }

            internal static void UpdateState() {
                if(isInGame) {
                    WindEffect.Disable();
                    ScaleFactor.Disable();
                    Debug.Enable();
                    return;
                }
                WindEffect.Enable();
                ScaleFactor.Enable();
                Debug.Disable();
            }

            internal static void CreatePanel(UIHelperBase helper) {
                UIPanel Space;
                UIHelper group = helper.AddGroup($"{m_modName} -- Version {m_modVersion}") as UIHelper;
                UIPanel panel = group.self as UIPanel;

                WindEffect = (UICheckBox)group.AddCheckbox(@"Enable Tree Effect on Wind [Original game default is enabled]", TreeEffectOnWind, (b) => {
                    TreeEffectOnWind = b;
                    SaveSettings();
                });
                WindEffect.tooltip = "Enable/Disable the normal game behavior of letting tree's height effect dilute the wind map.\nOption should be set before loading a map.";
                Space = (UIPanel)group.AddSpace(70);
                UILabel WindEffectLabel = (UILabel)Space.AddUIComponent<UILabel>();
                WindEffectLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                WindEffectLabel.width = 600;
                WindEffectLabel.height = 70;
                WindEffectLabel.autoSize = false;
                WindEffectLabel.relativePosition = new Vector3(25, 0);
                WindEffectLabel.wordWrap = true;
                WindEffectLabel.text = "Enable/Disable the normal game behavior of tree's height \ndiluting and weakening the wind on the map.\nOption should be set before loading a map.";

                TreeSnap = (UICheckBox)group.AddCheckbox(@"Tree Snapping/Anarchy Support", UseTreeSnapping, (b) => {
                    UseTreeSnapping = b;
                    SaveSettings();
                });
                TreeSnap.tooltip = @"Allows Trees to be placed anywhere if enabled";
                Space = (UIPanel)group.AddSpace(50);
                UILabel TreeSnapLabel = (UILabel)panel.AddUIComponent<UILabel>();
                TreeSnapLabel.AlignTo(Space, UIAlignAnchor.TopLeft);
                TreeSnapLabel.width = 600;
                TreeSnapLabel.height = 50;
                TreeSnapLabel.relativePosition = new Vector3(25, 0);
                TreeSnapLabel.wordWrap = true;
                TreeSnapLabel.autoSize = false;
                TreeSnapLabel.text = "Enables/Disable trees to be placed anywhere imaginable.\nFunctions like the original TreeSnapping Mod";

                TreeRotation = (UICheckBox)group.AddCheckbox(@"Random Tree Rotation", RandomTreeRotation, (b) => {
                    RandomTreeRotation = b;
                    SaveSettings();
                });
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
                ScaleFactor = (UISlider)ScaleGroup.AddSlider(@"Max Supported Tree", MinScaleFactor, MaxScaleFactor, 0.5f, TreeScaleFactor, (f) => {
                    TreeScaleFactor = f;
                    MaxTreeLabel.text = $"Maximum Supported Number of Trees: {(uint)(MaxTreeLimit)}";
                    Patches.TreeLimit.InjectResize();
                    SaveSettings();
                });
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
                UILabel ImportantLabel = (UILabel)panel.AddUIComponent<UILabel>();
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
        #endregion

        #region IUserMod
        string IUserMod.Name => $"{m_modName} {m_modVersion}";
        string IUserMod.Description => m_modDesc;
        public void OnEnabled() {
            LoadSettings();
            TAPatcher.Enable();
            TAWrapper wrapper = new TAWrapper("TreeAnarchy.FastCore.dll");
        }
        public void OnDisabled() {
            TAPatcher.Disable();
            SaveSettings();
        }

        public void OnSettingsUI(UIHelperBase helper) {
            OptionPanel.CreatePanel(helper);
            OptionPanel.UpdateState();
        }
        #endregion
        #region ILoadingExtension
        void ILoadingExtension.OnCreated(ILoading loading) {
        }

        void ILoadingExtension.OnReleased() {
        }

        void ILoadingExtension.OnLevelLoaded(LoadMode mode) {
            OptionPanel.UpdateState();
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
