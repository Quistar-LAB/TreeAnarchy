using ColossalFramework.UI;
using System;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal static class TAUI {
        private const float MaxScaleFactor = 8.0f;
        private const float MinScaleFactor = 1.5f;

        internal struct Msg {
            /* Key Binding */
            internal const string PressAnyKey = "Press any key";
            /* Tab Strips */
            internal const string MainOptionTab = "Main Options";
            internal const string TreeSnappingTab = "Tree Snapping";
            internal const string KeyboardShortcutTab = "Key Binding";
            /* Main options */
            internal const string MaxTreeLimitTitle = "Configure Custom Tree Limit";
            internal const string MaxTreeLimit = "Maximum Tree Limit:  {0} trees";
            internal const string WindEffect = @"Enable Tree Effect on Wind [Original game default is enabled]";
            internal const string WindEffectLabel = @"This option affects the normal game behavior of tree's height diluting and weakening the wind on the map.";
            internal const string RandomTreeRotation = @"Random Tree Rotation";
            internal const string TreeSwayFactor = @"Tree Sway Factor";
            internal const string LockForestry = @"Lock Forestry";
            internal const string PersistentLock = @"Persistent lock on forestry resources";
            internal const string SwayLabel = @"Useful eyecandy and tools. Lock forestry prevent trees from creating forestry resources and removing fertile land. " +
                                               @"Enabling Persistent Lock will set Lock Forestry to enabled on every game start. This is to prevent users from forgetting " +
                                               @"to turn on Lock Forestry when entering a game and destroying fertile land on a map";
            internal const string Important = "Important! Must be set before starting/loading a game\n\n" +
                                               "The following settings are only used when loading from Old Unlimited Trees Format";
            internal const string ReplaceRemoveDesc = @"The default behavior is set to keep the missing trees. If you select remove, then the trees will be removed " +
                                                        "from the saved game. If you choose to replace the trees, then the first vanilla tree is used to replace the missing trees";
            internal const string ReplaceRemoveName = @"Loading behavior for old unlimited trees game format";
            internal static readonly string[] ReplaceRemoveLabels = new string[] { "Keep missing trees", "Remove missing trees", "Replace missing trees" };
            /* Tree Snapping specific */
            internal const string TreeSnap = @"Tree Snapping/Anarchy Support";
            internal const string TreeSnapLabel = @"This option allows free movement of trees, and allows trees to be snapped to buildings and props";
            internal const string ExperimentalTreeSnap = @"Use Experimental Tree Snapping";
            internal const string ExperimentalTreeSnapLabel = @"Experimental tree snapping is a hack in MoveIt mod, allowing tree snapping to " +
                                                              @"Building, Networks, Props. Tree snapping works fine when moving trees, but when cloning, offsets will " +
                                                              @"occur after placing the clone. To resolve this, deselect the tree and reselect any tree to reset position. " +
                                                              @"Current I'm working within MoveIt internal framework during my free time to bring a better experience";
            internal const string TreeSnapToBuilding = @"Enable this option to allow trees to snap to buildings";
            internal const string TreeSnapToBuildingLabel = @"Tree snapping to sub-building does not work due to ray casting not recognizing sub-buildings";
            internal const string TreeSnapToNetwork = @"Enable this option to allow trees to snap to networks";
            internal const string TreeSnapToProp = @"Enable this option to allow trees to snap to props";
            internal const string TreeSnapToPropLabel = @"Tree snapping to props does not work 100% due to some props which doesn't have defined shape for ray casting. e.g. Ploppable Surfaces";
        }

        private static UITabstrip tabBar = default;
        private static UILabel MaxTreeLabel = default;
        private static UILabel WindEffectLabel = default;
        private static UILabel TreeSnapLabel = default;
        private static UICheckBox WindEffect = default;
        private static UICheckBox TreeSnap = default;
        private static UICheckBox ExperimentalTreeSnap = default;
        private static UIPanel TreeSnapOptionsPanel = default;
        private static UICheckBox TreeSnapToBuilding = default;
        private static UICheckBox TreeSnapToNetwork = default;
        private static UICheckBox TreeSnapToProp = default;
        private static UICheckBox TreeRotation = default;
        private static UICheckBox LockForestry = default;
        private static UICheckBox PersistentLock = default;
        private static UISlider TreeSwayFactor = default;
        private static UISlider TreeScaleFactor = default;
        private static UIDropDown RemoveReplaceDropDown = default;

        private static void OnTreeWindCheckChanged(UIComponent component, bool isChecked) {
            TreeEffectOnWind = isChecked;
            SaveSettings();
        }
        private static void OnTreeSnapCheckChanged(UIComponent component, bool isChecked) {
            UseTreeSnapping = isChecked;
            if (TAPatcher.MoveItUseTreeSnap != null) {
                TAPatcher.MoveItUseTreeSnap.SetValue(null, isChecked);
            }
            SaveSettings();
        }
        private static void OnExperimentalTreeSnapCheckChanged(UIComponent component, bool isChecked) {
            UseExperimentalTreeSnapping = isChecked;
            if (isChecked) {
                TreeSnapOptionsPanel.Show();
            } else {
                TreeSnapOptionsPanel.Hide();
            }
            SaveSettings();
        }
        private static void OnTreeSnapToBuildingCheckChanged(UIComponent component, bool isChecked) {
            UseTreeSnapToBuilding = isChecked;
            SaveSettings();
        }
        private static void OnTreeSnapToNetworkCheckChanged(UIComponent component, bool isChecked) {
            UseTreeSnapToNetwork = isChecked;
            SaveSettings();
        }
        private static void OnTreeSnapToPropCheckChanged(UIComponent component, bool isChecked) {
            UseTreeSnapToProp = isChecked;
            SaveSettings();
        }
        private static void OnTreeRotationCheckChanged(UIComponent component, bool isChecked) {
            RandomTreeRotation = isChecked;
            if (RandomTreeRotation) RandomTreeRotationFactor = 1000;
            else RandomTreeRotationFactor = 0;
            SaveSettings();
        }
        private static void OnTreeSwayFactorChanged(UIComponent component, float val) {
            TAMod.TreeSwayFactor = val;
            if (IsInGame) Patches.TreeMovement.UpdateTreeSway();
            SaveSettings();
        }
        private static void OnLockForestryCheckChanged(UIComponent component, bool isChecked) {
            TAMod.LockForestry = isChecked;
            SaveSettings();
        }
        private static void OnPersistentLockCheckChanged(UIComponent component, bool isChecked) {
            TAMod.PersistentLockForestry = isChecked;
            SaveSettings();
        }
        private static void OnTreeScaleFactorChanged(UIComponent component, float val) {
            TAMod.TreeScaleFactor = val;
            MaxTreeLabel.text = String.Format(Msg.MaxTreeLimit, MaxTreeLimit);
            SaveSettings();
        }

        private static void OnReplaceRemoveKeepEventChanged(UIComponent component, int val) {
            TAMod.RemoveReplaceOrKeep = val;
            SaveSettings();
        }

        internal static UIPanel AddTab(UITabstrip tabStrip, string tabName, int tabIndex, bool autoLayout) {
            UIButton tabButton = tabStrip.AddTab(tabName);

            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";
            tabButton.tooltip = tabName;
            tabButton.width = 175;
            tabButton.textScale = 0.9f;

            tabStrip.selectedIndex = tabIndex;

            UIPanel rootPanel = tabStrip.tabContainer.components[tabIndex] as UIPanel;
            rootPanel.autoLayout = autoLayout;
            if (autoLayout) {
                rootPanel.autoLayoutDirection = LayoutDirection.Vertical;
                rootPanel.autoLayoutPadding.top = 5;
                rootPanel.autoLayoutPadding.left = 10;
            }
            return rootPanel;
        }

        internal static void InitializeOptionPanel(UIHelper helper) {
            UIPanel rootPanel = (UIPanel)helper.self;
            tabBar = rootPanel.AddUIComponent<UITabstrip>();
            tabBar.relativePosition = new Vector3(0, 0);
            UITabContainer tabContainer = rootPanel.AddUIComponent<UITabContainer>();
            tabContainer.relativePosition = new Vector3(0, 50);
            tabContainer.size = new Vector3(rootPanel.width, rootPanel.height - 60);
            tabBar.tabPages = tabContainer;

            UIPanel mainPanel = AddTab(tabBar, Msg.MainOptionTab, 0, true);
            UIHelper mainHelper = new UIHelper(mainPanel);
            ShowStandardOptions(mainHelper);
            UIHelper treeLimitHelper = mainHelper.AddGroup(Msg.MaxTreeLimitTitle) as UIHelper;
            ShowTreeLimitOption(treeLimitHelper);
            tabContainer.height = mainPanel.height + (treeLimitHelper.self as UIPanel).height + 20;
            UpdateState(IsInGame);

            UIPanel treesnapPanel = AddTab(tabBar, Msg.TreeSnappingTab, 1, true);
            UIHelper treesnapHelper = new UIHelper(treesnapPanel);
            ShowTreeSnappingOptions(treesnapHelper);

            UIPanel keymappingPanel = AddTab(tabBar, Msg.KeyboardShortcutTab, 2, true);
            keymappingPanel.gameObject.AddComponent<TAKeyBinding>();
        }

        internal static void ShowStandardOptions(UIHelper option) {
            UILabel swayLabel = default;
            UIPanel panel = (UIPanel)option.self;
            AddCheckBox(ref panel, ref WindEffect, Msg.WindEffect, TAMod.TreeEffectOnWind, OnTreeWindCheckChanged);
            AddLabel(ref panel, WindEffect, ref WindEffectLabel, Msg.WindEffectLabel);
            option.AddSpace((int)WindEffectLabel.height);
            AddCheckBox(ref panel, ref TreeRotation, Msg.RandomTreeRotation, TAMod.RandomTreeRotation, OnTreeRotationCheckChanged);
            TreeRotation.width = 300;
            UIPanel SwayPanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            SwayPanel.Find<UILabel>("Label").text = Msg.TreeSwayFactor;
            AddSlider(ref SwayPanel, ref TreeSwayFactor, 0f, 1f, 0.1f, TAMod.TreeSwayFactor, OnTreeSwayFactorChanged);
            SwayPanel.AlignTo(TreeRotation, UIAlignAnchor.TopRight);
            SwayPanel.relativePosition = new Vector3(320, -5);
            AddCheckBox(ref panel, ref LockForestry, Msg.LockForestry, TAMod.LockForestry, OnLockForestryCheckChanged);
            LockForestry.width = 300;
            AddCheckBox(ref panel, ref PersistentLock, Msg.PersistentLock, TAMod.PersistentLockForestry, OnPersistentLockCheckChanged);
            AddLabel(ref panel, PersistentLock, ref swayLabel, Msg.SwayLabel);
            option.AddSpace((int)swayLabel.height + 20);
            SwayPanel.zOrder = TreeRotation.zOrder - 1;
        }

        internal static void ShowTreeLimitOption(UIHelper option) {
            UILabel ImportantMsg = default;
            UILabel ReplaceRemoveLabel = default;
            UIPanel panel = (UIPanel)option.self;
            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            MaxTreeLabel = ScalePanel.Find<UILabel>("Label");
            MaxTreeLabel.text = String.Format(Msg.MaxTreeLimit, TAMod.MaxTreeLimit);
            MaxTreeLabel.width = panel.width - 100;
            MaxTreeLabel.autoSize = false;
            MaxTreeLabel.autoHeight = true;
            AddSlider(ref ScalePanel, ref TreeScaleFactor, MinScaleFactor, MaxScaleFactor, 0.5f, TAMod.TreeScaleFactor, OnTreeScaleFactorChanged);
            TreeScaleFactor.width = panel.width - 150;
            AddLabel(ref panel, ScalePanel, ref ImportantMsg, Msg.Important);
            option.AddSpace((int)ImportantMsg.height);
            AddDropdown(ref panel, ref RemoveReplaceDropDown, Msg.ReplaceRemoveName, Msg.ReplaceRemoveLabels, 0, OnReplaceRemoveKeepEventChanged);
            AddLabel(ref panel, RemoveReplaceDropDown, ref ReplaceRemoveLabel, Msg.ReplaceRemoveDesc);
            ReplaceRemoveLabel.relativePosition = new Vector3(0, RemoveReplaceDropDown.height + 5);
            option.AddSpace((int)ReplaceRemoveLabel.height);
        }

        internal static void ShowTreeSnappingOptions(UIHelper option) {
            UILabel treeSnapToPropLabel = default;
            UILabel treeSnapToBuildingLabel = default;
            UILabel ExperimentalTreeSnapLabel = default;
            UIPanel panel = (UIPanel)option.self;
            AddCheckBox(ref panel, ref TreeSnap, Msg.TreeSnap, TAMod.UseTreeSnapping, OnTreeSnapCheckChanged);
            AddLabel(ref panel, TreeSnap, ref TreeSnapLabel, Msg.TreeSnapLabel);
            option.AddSpace((int)TreeSnapLabel.height);
            AddCheckBox(ref panel, ref ExperimentalTreeSnap, Msg.ExperimentalTreeSnap, TAMod.UseExperimentalTreeSnapping, OnExperimentalTreeSnapCheckChanged);
            AddLabel(ref panel, ExperimentalTreeSnap, ref ExperimentalTreeSnapLabel, Msg.ExperimentalTreeSnapLabel);
            option.AddSpace((int)ExperimentalTreeSnapLabel.height + 20);

            TreeSnapOptionsPanel = panel.AddUIComponent<UIPanel>();
            if (TAMod.UseExperimentalTreeSnapping) {
                TreeSnapOptionsPanel.Show();
            } else {
                TreeSnapOptionsPanel.Hide();
            }
            TreeSnapOptionsPanel.width = panel.width - 50;
            TreeSnapOptionsPanel.autoLayoutPadding.top = 5;
            TreeSnapOptionsPanel.autoLayoutPadding.left = 10;
            TreeSnapOptionsPanel.autoLayoutPadding.right = 10;
            TreeSnapOptionsPanel.autoLayout = true;
            TreeSnapOptionsPanel.autoFitChildrenVertically = true;
            TreeSnapOptionsPanel.autoLayoutDirection = LayoutDirection.Vertical;
            UIHelper treeSnapOptionsHelper = new UIHelper(TreeSnapOptionsPanel);
            AddCheckBox(ref TreeSnapOptionsPanel, ref TreeSnapToBuilding, Msg.TreeSnapToBuilding, TAMod.UseTreeSnapToBuilding, OnTreeSnapToBuildingCheckChanged);
            AddLabel(ref TreeSnapOptionsPanel, TreeSnapToBuilding, ref treeSnapToBuildingLabel, Msg.TreeSnapToBuildingLabel);
            treeSnapOptionsHelper.AddSpace((int)treeSnapToBuildingLabel.height);
            AddCheckBox(ref TreeSnapOptionsPanel, ref TreeSnapToNetwork, Msg.TreeSnapToNetwork, TAMod.UseTreeSnapToNetwork, OnTreeSnapToNetworkCheckChanged);
            AddCheckBox(ref TreeSnapOptionsPanel, ref TreeSnapToProp, Msg.TreeSnapToProp, TAMod.UseTreeSnapToProp, OnTreeSnapToPropCheckChanged);
            AddLabel(ref TreeSnapOptionsPanel, TreeSnapToProp, ref treeSnapToPropLabel, Msg.TreeSnapToPropLabel);
            treeSnapOptionsHelper.AddSpace((int)treeSnapToPropLabel.height);
        }

        internal static void UpdateState(bool isInGame) {
            if (isInGame) {
                TreeScaleFactor.Disable();
                return;
            }
            TreeScaleFactor.Enable();
        }

        private static void AddCheckBox(ref UIPanel panel, ref UICheckBox cb, string name, bool defaultVal, PropertyChangedEventHandler<bool> callback) {
            cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate"));
            cb.text = name;
            cb.isChecked = defaultVal;
            cb.autoSize = false;
            cb.eventCheckChanged += new PropertyChangedEventHandler<bool>(callback);
        }
        private static void AddLabel(ref UIPanel panel, UIComponent alignTo, ref UILabel label, string text) {
            label = panel.AddUIComponent<UILabel>();
            label.AlignTo(alignTo, UIAlignAnchor.BottomLeft);
            label.width = panel.width - 80;
            label.wordWrap = true;
            label.autoHeight = true;
            label.text = text;
            label.relativePosition = new Vector3(25, 25);
            panel.height += label.height;
        }
        private static void AddSlider(ref UIPanel panel, ref UISlider slider, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            slider = panel.Find<UISlider>("Slider");
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += new PropertyChangedEventHandler<float>(callback);
        }

        private static void AddDropdown(ref UIPanel panel, ref UIDropDown dropDown, string text, string[] options, int defaultSelection, PropertyChangedEventHandler<int> callback) {
            UIPanel uiPanel = panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsDropdownTemplate")) as UIPanel;
            uiPanel.Find<UILabel>("Label").text = text;
            dropDown = uiPanel.Find<UIDropDown>("Dropdown");
            dropDown.width = 300;
            dropDown.items = options;
            dropDown.selectedIndex = defaultSelection;
            dropDown.eventSelectedIndexChanged += new PropertyChangedEventHandler<int>(callback);
        }

        internal static void SetTreeLimitSlider(float value) {
            TreeScaleFactor.value = value;
            SaveSettings();
        }
    }
}
