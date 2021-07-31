using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal class TAUI {
        private const float MaxScaleFactor = 8.0f;
        private const float MinScaleFactor = 1.5f;
        internal const float DefaultFontScale = 0.95f;
        internal const float SmallFontScale = 0.85f;

        internal const string MainOptionPanelName = "MainOptionContainer";
        internal const string TreeSnapPanelName = "TreeSnapContainer";
        internal const string TreeSnapCBName = "TreeSnapCB";
        internal const string LockForestryCBName = "LockForestryCB";

        private UITabstrip tabBar = default;
        private UILabel MaxTreeLabel = default;
        private UILabel WindEffectLabel = default;
        private UILabel TreeSnapLabel = default;
        private UICheckBox WindEffect = default;
        private UICheckBox TreeSnapCB = default;
        private UICheckBox ExperimentalTreeSnap = default;
        private UIPanel TreeSnapOptionsPanel = default;
        private UICheckBox TreeSnapToBuilding = default;
        private UICheckBox TreeSnapToNetwork = default;
        private UICheckBox TreeSnapToProp = default;
        private UICheckBox TreeRotation = default;
        private UICheckBox LockForestryCB = default;
        private UICheckBox PersistentLock = default;
        private UISlider TreeSwayFactor = default;
        private UISlider TreeScaleFactor = default;
        private UIDropDown RemoveReplaceDropDown = default;

        private void OnTreeWindCheckChanged(UIComponent _, bool isChecked) {
            TreeEffectOnWind = isChecked;
            SaveSettings();
        }
        private void OnTreeSnapCheckChanged(UIComponent _, bool isChecked) {
            UseTreeSnapping = isChecked;
            if (TAPatcher.MoveItUseTreeSnap != null) {
                TAPatcher.MoveItUseTreeSnap.SetValue(null, isChecked);
            }
            SaveSettings();
        }
        private void OnExperimentalTreeSnapCheckChanged(UIComponent _, bool isChecked) {
            UseExperimentalTreeSnapping = isChecked;
            if (isChecked) {
                TreeSnapOptionsPanel.Show();
            } else {
                TreeSnapOptionsPanel.Hide();
            }
            SaveSettings();
        }
        private void OnTreeSnapToBuildingCheckChanged(UIComponent _, bool isChecked) {
            UseTreeSnapToBuilding = isChecked;
            SaveSettings();
        }
        private void OnTreeSnapToNetworkCheckChanged(UIComponent _, bool isChecked) {
            UseTreeSnapToNetwork = isChecked;
            SaveSettings();
        }
        private void OnTreeSnapToPropCheckChanged(UIComponent _, bool isChecked) {
            UseTreeSnapToProp = isChecked;
            SaveSettings();
        }
        private void OnTreeRotationCheckChanged(UIComponent _, bool isChecked) {
            RandomTreeRotation = isChecked;
            if (RandomTreeRotation) RandomTreeRotationFactor = 1000;
            else RandomTreeRotationFactor = 0;
            SaveSettings();
        }
        private void OnTreeSwayFactorChanged(UIComponent _, float val) {
            TAMod.TreeSwayFactor = val;
            if (IsInGame) Patches.TreeMovement.UpdateTreeSway();
            SaveSettings();
        }
        private void OnLockForestryCheckChanged(UIComponent _, bool isChecked) {
            UseLockForestry = isChecked;
            SaveSettings();
        }
        private void OnPersistentLockCheckChanged(UIComponent _, bool isChecked) {
            PersistentLockForestry = isChecked;
            SaveSettings();
        }
        private void OnTreeScaleFactorChanged(UIComponent _, float val) {
            TAMod.TreeScaleFactor = val;
            MaxTreeLabel.text = String.Format(SingletonLite<TALocale>.instance.GetLocale("MaxTreeLimit"), MaxTreeLimit);
            SaveSettings();
        }

        private void OnReplaceRemoveKeepEventChanged(UIComponent _, int val) {
            RemoveReplaceOrKeep = val;
            SaveSettings();
        }

        internal UIPanel AddTab(UITabstrip tabStrip, string tabName, int tabIndex, bool autoLayout) {
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

        internal void InitializeOptionPanel(UIHelper helper) {
            TALocale locale = SingletonLite<TALocale>.instance;
            UIPanel rootPanel = (UIPanel)helper.self;
            tabBar = rootPanel.AddUIComponent<UITabstrip>();
            tabBar.relativePosition = new Vector3(0, 0);
            UITabContainer tabContainer = rootPanel.AddUIComponent<UITabContainer>();
            tabContainer.relativePosition = new Vector3(0, 50);
            tabContainer.size = new Vector3(rootPanel.width, rootPanel.height - 60);
            tabBar.tabPages = tabContainer;

            UIPanel mainPanel = AddTab(tabBar, locale.GetLocale("MainOptionTab"), 0, true);
            mainPanel.autoFitChildrenVertically = true;
            mainPanel.autoLayout = true;
            mainPanel.autoLayoutDirection = LayoutDirection.Vertical;
            mainPanel.autoSize = true;
            mainPanel.cachedName = MainOptionPanelName;
            UIHelper mainHelper = new UIHelper(mainPanel);

            ShowStandardOptions(mainHelper);
            float height = ShowTreeLimitOption(mainHelper);
            tabContainer.height = mainPanel.height + height + 120;
            UpdateState(IsInGame);

            UIPanel treesnapPanel = AddTab(tabBar, locale.GetLocale("TreeSnappingTab"), 1, true);
            treesnapPanel.cachedName = TreeSnapPanelName;
            UIHelper treesnapHelper = new UIHelper(treesnapPanel);
            ShowTreeSnappingOptions(treesnapHelper);

            AddTab(tabBar, locale.GetLocale("KeyboardShortcutTab"), 2, true).gameObject.AddComponent<TAKeyBinding>();
        }

        internal void ShowStandardOptions(UIHelper option) {
            TALocale locale = SingletonLite<TALocale>.instance;
            UILabel swayLabel = default;
            UIPanel panel = (UIPanel)option.self;
            AddCheckBox(ref panel, ref WindEffect, locale.GetLocale("WindEffect"), TreeEffectOnWind, OnTreeWindCheckChanged);
            AddLabel(ref panel, WindEffect, ref WindEffectLabel, SmallFontScale, locale.GetLocale("WindEffectLabel"));
            AddCheckBox(ref panel, ref TreeRotation, locale.GetLocale("RandomTreeRotation"), RandomTreeRotation, OnTreeRotationCheckChanged);
            TreeRotation.width = 300;
            UIPanel SwayPanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            UILabel SwayFactorLabel = SwayPanel.Find<UILabel>("Label");
            SwayFactorLabel.text = locale.GetLocale("TreeSwayFactor");
            SwayFactorLabel.width += 50;
            AddSlider(ref SwayPanel, ref TreeSwayFactor, 0f, 1f, 0.1f, TAMod.TreeSwayFactor, OnTreeSwayFactorChanged);
            SwayPanel.AlignTo(TreeRotation, UIAlignAnchor.TopRight);
            SwayPanel.relativePosition = new Vector3(320, -5);
            TreeSwayFactor.width += 40;
            AddCheckBox(ref panel, ref LockForestryCB, locale.GetLocale("LockForestry"), UseLockForestry, OnLockForestryCheckChanged);
            LockForestryCB.cachedName = LockForestryCBName;
            LockForestryCB.name = LockForestryCBName;
            LockForestryCB.width = 300;
            AddCheckBox(ref panel, ref PersistentLock, locale.GetLocale("PersistentLock"), PersistentLockForestry, OnPersistentLockCheckChanged);
            AddLabel(ref panel, PersistentLock, ref swayLabel, SmallFontScale, locale.GetLocale("SwayLabel"));
            SwayPanel.zOrder = TreeRotation.zOrder - 1;
        }

        internal float ShowTreeLimitOption(UIHelper option) {
            float totalHeight = 0;
            UILabel ImportantMsg = default;
            UILabel ReplaceRemoveLabel = default;
            UILabel MaxTreeTitle = default;
            UIPanel panel = (UIPanel)option.self;
            TALocale locale = SingletonLite<TALocale>.instance;

            MaxTreeTitle = panel.AddUIComponent<UILabel>();
            MaxTreeTitle.AlignTo(panel, UIAlignAnchor.BottomLeft);
            MaxTreeTitle.width = panel.width - 80;
            MaxTreeTitle.wordWrap = true;
            MaxTreeTitle.autoHeight = true;
            MaxTreeTitle.textScale = 1.15f;
            MaxTreeTitle.text = locale.GetLocale("MaxTreeLimitTitle");
            MaxTreeTitle.relativePosition = new Vector3(25, 25);

            //MaxTreeTitle.height -= 20;
            totalHeight += MaxTreeTitle.height;
            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            MaxTreeLabel = ScalePanel.Find<UILabel>("Label");
            MaxTreeLabel.width = panel.width - 100;
            MaxTreeLabel.autoSize = false;
            MaxTreeLabel.autoHeight = true;
            MaxTreeLabel.text = String.Format(locale.GetLocale("MaxTreeLimit"), MaxTreeLimit);
            totalHeight += MaxTreeLabel.height;
            AddSlider(ref ScalePanel, ref TreeScaleFactor, MinScaleFactor, MaxScaleFactor, 0.5f, TAMod.TreeScaleFactor, OnTreeScaleFactorChanged);
            TreeScaleFactor.width = panel.width - 150;
            totalHeight += TreeScaleFactor.height;
            AddLabel(ref panel, ScalePanel, ref ImportantMsg, DefaultFontScale, locale.GetLocale("Important"));
            totalHeight += ImportantMsg.height;
            AddDropdown(ref panel, ref RemoveReplaceDropDown, locale.GetLocale("ReplaceRemoveName"),
                new string[] { locale.GetLocale("ReplaceRemoveDropdown0"), locale.GetLocale("ReplaceRemoveDropdown1"), locale.GetLocale("ReplaceRemoveDropdown2") },
                0, OnReplaceRemoveKeepEventChanged);
            totalHeight += RemoveReplaceDropDown.height;
            AddLabel(ref panel, RemoveReplaceDropDown, ref ReplaceRemoveLabel, SmallFontScale, locale.GetLocale("ReplaceRemoveDesc"));
            ReplaceRemoveLabel.relativePosition = new Vector3(0, RemoveReplaceDropDown.height + 5);
            totalHeight += ReplaceRemoveLabel.height;
            return totalHeight;
        }

        internal void ShowTreeSnappingOptions(UIHelper option) {
            UILabel treeSnapToPropLabel = default;
            UILabel treeSnapToBuildingLabel = default;
            UILabel ExperimentalTreeSnapLabel = default;
            UIPanel panel = (UIPanel)option.self;
            TALocale locale = SingletonLite<TALocale>.instance;
            AddCheckBox(ref panel, ref TreeSnapCB, locale.GetLocale("TreeSnap"), UseTreeSnapping, OnTreeSnapCheckChanged);
            TreeSnapCB.cachedName = TreeSnapCBName;
            TreeSnapCB.name = TreeSnapCBName;
            AddLabel(ref panel, TreeSnapCB, ref TreeSnapLabel, SmallFontScale, locale.GetLocale("TreeSnapLabel"));
            AddCheckBox(ref panel, ref ExperimentalTreeSnap, locale.GetLocale("ExperimentalTreeSnap"), UseExperimentalTreeSnapping, OnExperimentalTreeSnapCheckChanged);
            AddLabel(ref panel, ExperimentalTreeSnap, ref ExperimentalTreeSnapLabel, SmallFontScale, locale.GetLocale("ExperimentalTreeSnapLabel"));

            TreeSnapOptionsPanel = panel.AddUIComponent<UIPanel>();
            if (UseExperimentalTreeSnapping) {
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
            AddCheckBox(ref TreeSnapOptionsPanel, ref TreeSnapToBuilding, locale.GetLocale("TreeSnapToBuilding"), UseTreeSnapToBuilding, OnTreeSnapToBuildingCheckChanged);
            AddLabel(ref TreeSnapOptionsPanel, TreeSnapToBuilding, ref treeSnapToBuildingLabel, SmallFontScale, locale.GetLocale("TreeSnapToBuildingLabel"));
            AddCheckBox(ref TreeSnapOptionsPanel, ref TreeSnapToNetwork, locale.GetLocale("TreeSnapToNetwork"), UseTreeSnapToNetwork, OnTreeSnapToNetworkCheckChanged);
            AddCheckBox(ref TreeSnapOptionsPanel, ref TreeSnapToProp, locale.GetLocale("TreeSnapToProp"), UseTreeSnapToProp, OnTreeSnapToPropCheckChanged);
            AddLabel(ref TreeSnapOptionsPanel, TreeSnapToProp, ref treeSnapToPropLabel, SmallFontScale, locale.GetLocale("TreeSnapToPropLabel"));
        }

        private void UpdateState(bool isInGame) {
            if (isInGame) {
                TreeScaleFactor.Disable();
                return;
            }
            TreeScaleFactor.Enable();
        }

        private void AddCheckBox(ref UIPanel panel, ref UICheckBox cb, string name, bool defaultVal, PropertyChangedEventHandler<bool> callback) {
            cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate"));
            cb.isChecked = defaultVal;
            cb.autoSize = true;
            cb.text = name;
            cb.eventCheckChanged += new PropertyChangedEventHandler<bool>(callback);
            cb.height += 10;
            panel.height += cb.height;
        }
        private void AddLabel(ref UIPanel panel, UIComponent alignTo, ref UILabel label, float fontScale, string text) {
            label = panel.AddUIComponent<UILabel>();
            label.AlignTo(alignTo, UIAlignAnchor.BottomLeft);
            label.width = panel.width - 80;
            label.wordWrap = true;
            label.autoHeight = true;
            label.textScale = fontScale;
            label.text = text;
            label.relativePosition = new Vector3(25, 25);
            UIPanel space = panel.AddUIComponent<UIPanel>();
            space.name = "Space";
            space.isInteractive = false;
            space.height = label.height;
            panel.height += label.height;
        }
        private void AddSlider(ref UIPanel panel, ref UISlider slider, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            slider = panel.Find<UISlider>("Slider");
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += new PropertyChangedEventHandler<float>(callback);
            panel.height += slider.height;
        }

        private void AddDropdown(ref UIPanel panel, ref UIDropDown dropDown, string text, string[] options, int defaultSelection, PropertyChangedEventHandler<int> callback) {
            UIPanel uiPanel = panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsDropdownTemplate")) as UIPanel;
            uiPanel.Find<UILabel>("Label").text = text;
            dropDown = uiPanel.Find<UIDropDown>("Dropdown");
            dropDown.width = 300;
            dropDown.items = options;
            dropDown.selectedIndex = defaultSelection;
            dropDown.eventSelectedIndexChanged += new PropertyChangedEventHandler<int>(callback);
            panel.height += dropDown.height;
        }

        internal void SetTreeLimitSlider(float value) {
            TreeScaleFactor.value = value;
            SaveSettings();
        }
    }
}
