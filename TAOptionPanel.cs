using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal static class TAOptionPanel {
        private const float MaxScaleFactor = 20.0f;
        private const float MinScaleFactor = 1.5f;
        internal const float DefaultFontScale = 0.95f;
        internal const float SmallFontScale = 0.85f;
        internal const float TabFontScale = 0.9f;
        private static readonly Color32 m_greyColor = new Color32(0xe6, 0xe6, 0xe6, 0xee);
        private static readonly Color32 m_greenColor = new Color32(0xcf, 0xf9, 0x8f, 0xff);
        private static readonly Color32 m_orangeColor = new Color32(0xfe, 0xd8, 0x8b, 0xff);
        private const string LockForestryCBName = @"LockForestryCB";
        private const string TreeSnapCBName = @"TreeSnapCB";
        private const string TreeAnarchyCBName = @"TreeAnarchyCB";
        private const string TreeLODFixCBName = @"TreeLODFixCB";
        internal static UICheckBox LockForestryCB;
        internal static UICheckBox TreeSnapCB;
        internal static UICheckBox TreeAnarchyCB;
        internal static UISlider TreeScaleFactorSlider;
        internal static UILabel MaxTreeLabel;

        internal static void UpdateState(bool isInGame) {
            if (isInGame) {
                TreeScaleFactorSlider?.Disable();
                return;
            }
            TreeScaleFactorSlider?.Enable();
        }

        internal static void SetupPanel(UIPanel root) {
            UITabstrip tabBar = root.AddUIComponent<UITabstrip>();
            UITabContainer tabContainer = root.AddUIComponent<UITabContainer>();
            tabBar.tabPages = tabContainer;
            tabContainer.width = root.width;
            tabContainer.height = 520f;

            UIPanel mainPanel = AddTab(tabBar, TALocale.GetLocale(@"MainOptionTab"), 0, true);
            mainPanel.autoLayout = false;
            mainPanel.autoSize = false;
            ShowStandardOptions(mainPanel);
            UpdateState(IsInGame);

            UIPanel treeAnarchyPanel = AddTab(tabBar, TALocale.GetLocale(@"TreeAnarchyTab"), 1, true);
            treeAnarchyPanel.autoLayout = false;
            treeAnarchyPanel.autoSize = false;
            treeAnarchyPanel.FitTo(root);
            ShowTreeAnarchyOptions(treeAnarchyPanel);

            AddTab(tabBar, TALocale.GetLocale(@"KeyboardShortcutTab"), 2, true).gameObject.AddComponent<TAKeyBinding>();
        }

        private const float OFFSETX = 10f;
        private static void ShowStandardOptions(UIPanel panel) {
            string windEffectText = TALocale.GetLocale(@"WindEffect");
            UICheckBox WindEffectCB = AddCheckBox(panel, windEffectText, TreeEffectOnWind);
            WindEffectCB.eventCheckChanged += (_, isChecked) => TreeEffectOnWind = isChecked;
            WindEffectCB.relativePosition = new Vector3(OFFSETX, 0f);
            UIFontRenderer fontRenderer = WindEffectCB.label.ObtainRenderer();
            WindEffectCB.width = fontRenderer.MeasureString(windEffectText).x + 5f;
            UILabel WindEffectLabel = AddDescription(panel, @"WindEffectLabel", WindEffectCB, SmallFontScale, TALocale.GetLocale(@"WindEffectLabel"));
            string lockForestryText = TALocale.GetLocale(@"LockForestry");
            LockForestryCB = AddCheckBox(panel, lockForestryText, UseLockForestry);
            LockForestryCB.eventClicked += (c, p) => UseLockForestry = (c as UICheckBox).isChecked;
            LockForestryCB.cachedName = LockForestryCBName;
            LockForestryCB.name = LockForestryCBName;
            LockForestryCB.relativePosition = new Vector3(OFFSETX, WindEffectCB.relativePosition.y + WindEffectCB.height + WindEffectLabel.height);
            fontRenderer = LockForestryCB.label.ObtainRenderer();
            LockForestryCB.width = fontRenderer.MeasureString(lockForestryText).x + 5f;

            UI.UIFancySlider swayPanel = panel.AddUIComponent<UI.UIFancySlider>();
            swayPanel.Initialize(TALocale.GetLocale("TreeSwayFactor"), 0f, 1f, 0.1f, TreeSwayFactor, (_, val) => {
                TreeSwayFactor = val;
                if (IsInGame) TAManager.UpdateTreeSway();
            });
            swayPanel.relativePosition = new Vector3(panel.width - 60f - swayPanel.size.x, LockForestryCB.relativePosition.y + 5f);
            string persistentLockText = TALocale.GetLocale(@"PersistentLock");
            UICheckBox PersistentLockCB = AddCheckBox(panel, persistentLockText, PersistentLockForestry);
            PersistentLockCB.eventCheckChanged += (_, isChecked) => PersistentLockForestry = isChecked;
            PersistentLockCB.relativePosition = new Vector3(OFFSETX, LockForestryCB.relativePosition.y + LockForestryCB.height);
            fontRenderer = PersistentLockCB.label.ObtainRenderer();
            PersistentLockCB.width = fontRenderer.MeasureString(persistentLockText).x + 5f;
            UILabel SwayLabel = AddDescription(panel, @"SwayLabel", PersistentLockCB, SmallFontScale, TALocale.GetLocale(@"SwayLabel"));
            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsSliderTemplate"));
            MaxTreeLabel = ScalePanel.Find<UILabel>(@"Label");
            MaxTreeLabel.width = panel.width - 70f;
            MaxTreeLabel.textScale = 1.1f;
            ScalePanel.color = m_orangeColor;
            MaxTreeLabel.text = string.Format(TALocale.GetLocale(@"MaxTreeLimit"), MaxTreeLimit);
            TreeScaleFactorSlider = AddSlider(ScalePanel, MinScaleFactor, MaxScaleFactor, 0.5f, TreeScaleFactor, (_, val) => {
                TreeScaleFactor = val;
                MaxTreeLabel.text = string.Format(TALocale.GetLocale(@"MaxTreeLimit"), MaxTreeLimit);
            });
            UITextureAtlas atlas = UI.UIUtils.CreateTextureAtlas(@"TAOptionAtlas", new string[] { @"thumb", @"treelimitbg" });
            ScalePanel.relativePosition = new Vector3(OFFSETX, PersistentLockCB.relativePosition.y + PersistentLockCB.height + SwayLabel.height + 15);
            TreeScaleFactorSlider.atlas = atlas;
            TreeScaleFactorSlider.backgroundSprite = @"treelimitbg";
            TreeScaleFactorSlider.size = new Vector2(panel.width - 70f, 21f);
            UISprite sliderThumb = TreeScaleFactorSlider.thumbObject as UISprite;
            sliderThumb.atlas = atlas;
            sliderThumb.spriteName = @"thumb";
            sliderThumb.height = 21f;

            UILabel ImportantLabel = AddDescription(panel, @"ImportantLabel", ScalePanel, DefaultFontScale, TALocale.GetLocale(@"Important"));
            ImportantLabel.relativePosition = new Vector3(OFFSETX, ImportantLabel.relativePosition.y + 13f);

            UIDropDown RemoveReplaceDropDown = AddDropdown(panel, ImportantLabel, TALocale.GetLocale(@"ReplaceRemoveName"),
                new string[] { TALocale.GetLocale(@"ReplaceRemoveDropdown0"), TALocale.GetLocale(@"ReplaceRemoveDropdown1"), TALocale.GetLocale(@"ReplaceRemoveDropdown2") },
                0, (c, selectIndex) => RemoveReplaceOrKeep = selectIndex);
            UILabel RRDLabel = AddDescription(panel, @"RRDLabel", RemoveReplaceDropDown.parent, SmallFontScale, TALocale.GetLocale(@"ReplaceRemoveDesc"));
            RRDLabel.relativePosition = new Vector3(OFFSETX, RRDLabel.relativePosition.y);
        }

        private static void ShowTreeAnarchyOptions(UIPanel panel) {
            TreeSnapCB = AddCheckBox(panel, TALocale.GetLocale(@"TreeSnap"), UseTreeSnapping);
            TreeSnapCB.eventClicked += (c, p) => {
                UseTreeSnapping = (c as UICheckBox).isChecked;
            };
            TreeSnapCB.cachedName = TreeSnapCBName;
            TreeSnapCB.name = TreeSnapCBName;
            TreeSnapCB.relativePosition = new Vector3(OFFSETX, 0f);
            UILabel TreeSnapDesc = AddDescription(panel, @"TreeSnapDesc", TreeSnapCB, SmallFontScale, TALocale.GetLocale(@"TreeSnapLabel"));
            TreeAnarchyCB = AddCheckBox(panel, TALocale.GetLocale(@"TreeAnarchy"), UseTreeAnarchy);
            TreeAnarchyCB.eventClicked += (c, p) => {
                UseTreeAnarchy = (c as UICheckBox).isChecked;
            };
            TreeAnarchyCB.cachedName = TreeAnarchyCBName;
            TreeAnarchyCB.name = TreeAnarchyCBName;
            TreeAnarchyCB.relativePosition = new Vector3(OFFSETX, TreeSnapCB.height + TreeSnapDesc.height);
            UILabel TreeAnarchyDesc = AddDescription(panel, @"TreeAnarchyDesc", TreeAnarchyCB, SmallFontScale, TALocale.GetLocale(@"TreeAnarchyDesc"));
            UICheckBox hideTreeOnLoadCB = AddCheckBox(panel, TALocale.GetLocale(@"HideTreeOnLoad"), HideTreeOnLoad);
            hideTreeOnLoadCB.eventClicked += (c, p) => {
                HideTreeOnLoad = (c as UICheckBox).isChecked;
            };
            hideTreeOnLoadCB.relativePosition = new Vector3(OFFSETX + 25f, TreeAnarchyDesc.relativePosition.y + TreeAnarchyDesc.height);
            UILabel hideTreeDesc = AddDescription(panel, @"HideTreeDesc", hideTreeOnLoadCB, SmallFontScale, TALocale.GetLocale(@"HideTreeDesc"));
            hideTreeDesc.relativePosition = new Vector3(OFFSETX + 25f, hideTreeDesc.relativePosition.y);
            UIDropDown TreeBehaviourDD = AddDropdown(panel, panel, TALocale.GetLocale(@"TreeAnarchyBehaviour"),
                            new string[] { TALocale.GetLocale(@"TreeAnarchyHideTree"), TALocale.GetLocale(@"TreeAnarchyDeleteTree") },
                            DeleteOnOverlap ? 1 : 0, (_, val) => DeleteOnOverlap = val != 0);
            TreeBehaviourDD.parent.relativePosition = new Vector3(OFFSETX + 25f, hideTreeDesc.relativePosition.y + hideTreeDesc.height + 3f);
            UIDropDown TreeLODFixDD = AddDropdown(panel, panel, null,
                            new string[] { TALocale.GetLocale(@"TreeLODLow"), TALocale.GetLocale(@"TreeLODMedium"), TALocale.GetLocale(@"TreeLODHigh"), TALocale.GetLocale(@"TreeLODUltraHigh") },
                            (int)TreeLODSelectedResolution, (_, val) => {
                                TreeLODSelectedResolution = (TAManager.TreeLODResolution)val;
                                if (IsInGame) {
                                    Singleton<TreeManager>.instance.SetResolution((TAManager.TreeLODResolution)val);
                                }
                            });
            TreeLODFixDD.disabledColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            TreeLODFixDD.disabledTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            TreeLODFixDD.autoListWidth = true;
            TreeLODFixDD.width = 200f;
            TreeLODFixDD.textScale = 0.95f;
            TreeLODFixDD.height = 28f;
            TreeLODFixDD.itemHeight = 22;
            TreeLODFixDD.itemPadding = new RectOffset(5, 0, 3, 0);
            string treeLODFixName = TALocale.GetLocale(@"TreeLODFix");
            UICheckBox TreeLodFixCB = AddCheckBox(panel, treeLODFixName, UseTreeLODFix);
            TreeLodFixCB.eventCheckChanged += (_, isChecked) => {
                UseTreeLODFix = isChecked;
                if (isChecked) {
                    TreeLODFixDD.Enable();
                } else {
                    TreeLODFixDD.Disable();
                }
            };
            TreeLodFixCB.cachedName = TreeLODFixCBName;
            TreeLodFixCB.name = TreeLODFixCBName;
            UILabel label = TreeLodFixCB.Find<UILabel>(@"Label");
            UIFontRenderer renderer = label.ObtainRenderer();
            Vector2 size = renderer.MeasureString(treeLODFixName);
            TreeLodFixCB.relativePosition = new Vector3(OFFSETX, TreeBehaviourDD.parent.relativePosition.y + TreeBehaviourDD.parent.height);
            TreeLodFixCB.width = size.x + 2f;
            TreeLODFixDD.parent.relativePosition = new Vector3(35f + size.x, TreeBehaviourDD.parent.relativePosition.y + TreeBehaviourDD.parent.height + 5f);
        }

        private static UIPanel AddTab(UITabstrip tabStrip, string tabName, int tabIndex, bool autoLayout) {
            UIButton tabButton = tabStrip.AddTab(tabName);

            tabButton.normalBgSprite = @"SubBarButtonBase";
            tabButton.disabledBgSprite = @"SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = @"SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = @"SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = @"SubBarButtonBasePressed";
            tabButton.tooltip = tabName;
            tabButton.width = 175;
            tabButton.textScale = TabFontScale;

            tabStrip.selectedIndex = tabIndex;

            UIPanel rootPanel = tabStrip.tabContainer.components[tabIndex] as UIPanel;
            rootPanel.autoLayout = autoLayout;
            if (autoLayout) {
                rootPanel.autoLayoutDirection = LayoutDirection.Vertical;
                rootPanel.autoLayoutPadding.top = 0;
                rootPanel.autoLayoutPadding.bottom = 0;
                rootPanel.autoLayoutPadding.left = 5;
            }
            return rootPanel;
        }

        private static UICheckBox AddCheckBox(UIPanel panel, string name, bool defaultVal) {
            UICheckBox cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsCheckBoxTemplate"));
            cb.autoSize = true;
            cb.isLocalized = true;
            cb.label.textScale = 0.95f;
            cb.label.padding = new RectOffset(0, 0, 3, 0);
            cb.label.textColor = m_greenColor;
            cb.text = name;
            cb.height += 20f;
            cb.isChecked = defaultVal;
            return cb;
        }

        private static UILabel AddDescription(UIPanel panel, string name, UIComponent alignTo, float fontScale, string text) {
            UILabel desc = panel.AddUIComponent<UILabel>();
            desc.name = name;
            desc.width = panel.width - 80;
            desc.wordWrap = true;
            desc.autoHeight = true;
            desc.textScale = fontScale;
            desc.textColor = m_greyColor;
            desc.text = text;
            desc.relativePosition = new Vector3(alignTo.relativePosition.x + 26f, alignTo.relativePosition.y + alignTo.height - 5f);
            return desc;
        }

        private static UISlider AddSlider(UIPanel panel, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            UISlider slider = panel.Find<UISlider>(@"Slider");
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += callback;
            return slider;
        }

        private static UIDropDown AddDropdown(UIPanel panel, UIComponent alignTo, string text, string[] options, int defaultSelection, PropertyChangedEventHandler<int> callback) {
            UIPanel uiPanel = panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsDropdownTemplate")) as UIPanel;
            UILabel label = uiPanel.Find<UILabel>(@"Label");
            if (text.IsNullOrWhiteSpace()) {
                label.Hide();
            } else {
                label.autoSize = true;
                label.textScale = 0.95f;
                label.textColor = m_greenColor;
                label.text = text;
            }
            UIDropDown dropDown = uiPanel.Find<UIDropDown>(@"Dropdown");
            dropDown.width = 380;
            dropDown.items = options;
            dropDown.selectedIndex = defaultSelection;
            dropDown.eventSelectedIndexChanged += callback;
            uiPanel.relativePosition = new Vector3(alignTo.relativePosition.x, alignTo.relativePosition.y + alignTo.height);
            return dropDown;
        }
    }
}
