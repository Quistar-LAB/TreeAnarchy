using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    public class TAOptionPanel : UIPanel {
        private const string m_optionPanelName = "TreeAnarchyOptionPanel";
        private const float MaxScaleFactor = 8.0f;
        private const float MinScaleFactor = 1.5f;
        public const float DefaultFontScale = 0.95f;
        public const float SmallFontScale = 0.85f;
        public const float TabFontScale = 0.9f;
        private const string LockForestryCBName = "LockForestryCB";
        private const string TreeSnapCBName = "TreeSnapCB";
        private const string TreeAnarchyCBName = "TreeAnarchyCB";
        private static UICheckBox LockForestryCB;
        private static UICheckBox TreeSnapCB;
        private static UICheckBox TreeAnarchyCB;
        private static UISlider TreeScaleFactorSlider;
        private UILabel MaxTreeLabel;

        protected TAOptionPanel() {
            gameObject.name = m_optionPanelName;
            name = m_optionPanelName;
        }

        public override void Awake() {
            base.OnEnable();
            FitTo(m_Parent);
            isLocalized = true;
            m_AutoLayoutDirection = LayoutDirection.Vertical;
            m_AutoLayout = true;
            UITabstrip tabBar = AddUIComponent<UITabstrip>();
            UITabContainer tabContainer = AddUIComponent<UITabContainer>();
            tabBar.tabPages = tabContainer;
            tabContainer.FitTo(m_Parent);

            TALocale locale = SingletonLite<TALocale>.instance;

            UIPanel mainPanel = AddTab(tabBar, locale.GetLocale("MainOptionTab"), 0, true);
            mainPanel.autoLayout = false;
            mainPanel.autoSize = false;
            ShowStandardOptions(mainPanel);
            UpdateState(IsInGame);

            UIPanel treeAnarchyPanel = AddTab(tabBar, locale.GetLocale("TreeAnarchyTab"), 1, true);
            treeAnarchyPanel.autoLayout = false;
            treeAnarchyPanel.autoSize = false;
            ShowTreeAnarchyOptions(treeAnarchyPanel);

            AddTab(tabBar, locale.GetLocale("KeyboardShortcutTab"), 2, true).gameObject.AddComponent<TAKeyBinding>();
        }

        private static void UpdateState(bool isInGame) {
            if (isInGame) {
                TreeScaleFactorSlider.Disable();
                return;
            }
            TreeScaleFactorSlider.Enable();
        }

        private void ShowStandardOptions(UIPanel panel) {
            TALocale locale = SingletonLite<TALocale>.instance;
            UICheckBox indicatorCB = AddCheckBox(panel, locale.GetLocale("EnableIndicator"), ShowIndicators, (_, isChecked) => {
                ShowIndicators = isChecked;
                SaveSettings();
            });
            indicatorCB.AlignTo(panel, UIAlignAnchor.TopLeft);
            indicatorCB.relativePosition = new Vector3(2, 5);
            UICheckBox WindEffectCB = AddCheckBox(panel, locale.GetLocale("WindEffect"), TreeEffectOnWind, (_, isChecked) => {
                TreeEffectOnWind = isChecked;
                SaveSettings();
            });
            WindEffectCB.AlignTo(indicatorCB, UIAlignAnchor.BottomLeft);
            WindEffectCB.relativePosition += new Vector3(0, indicatorCB.height);
            UILabel WindEffectLabel = AddDescription(panel, "WindEffectLabel", WindEffectCB.label, SmallFontScale, locale.GetLocale("WindEffectLabel"));

            UICheckBox TreeRotationCB = AddCheckBox(panel, locale.GetLocale("RandomTreeRotation"), RandomTreeRotation, (_, isChecked) => {
                RandomTreeRotation = isChecked;
                if (RandomTreeRotation) RandomTreeRotationFactor = 1000;
                else RandomTreeRotationFactor = 0;
                SaveSettings();
            });
            TreeRotationCB.AlignTo(WindEffectCB, UIAlignAnchor.BottomLeft);
            TreeRotationCB.relativePosition = new Vector3(0, WindEffectCB.height + WindEffectLabel.height);
            TreeRotationCB.width = 300;
            LockForestryCB = AddCheckBox(panel, locale.GetLocale("LockForestry"), UseLockForestry, (_, isChecked) => {
                UseLockForestry = isChecked;
                TAIndicator.LockForestryIndicator?.SetState(isChecked);
                SaveSettings();
            });
            LockForestryCB.AlignTo(TreeRotationCB, UIAlignAnchor.BottomLeft);
            LockForestryCB.relativePosition = new Vector3(0, TreeRotationCB.height);
            LockForestryCB.cachedName = LockForestryCBName;
            LockForestryCB.name = LockForestryCBName;
            LockForestryCB.width = 300;

            UIPanel SwayPanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            UILabel SwayFactorLabel = SwayPanel.Find<UILabel>("Label");
            SwayFactorLabel.text = locale.GetLocale("TreeSwayFactor");
            SwayFactorLabel.textScale = DefaultFontScale;
            SwayFactorLabel.width += 50;
            SwayFactorLabel.Disable();
            AddSlider(SwayPanel, 0f, 1f, 0.1f, TreeSwayFactor, (_, val) => {
                TreeSwayFactor = val;
                if (IsInGame) TAPatcher.UpdateTreeSway();
                SaveSettings();
            }).width += 40;
            SwayPanel.AlignTo(TreeRotationCB, UIAlignAnchor.TopRight);
            SwayPanel.relativePosition = new Vector3(320, 0);
            UICheckBox PersistentLockCB = AddCheckBox(panel, locale.GetLocale("PersistentLock"), PersistentLockForestry, (_, isChecked) => {
                PersistentLockForestry = isChecked;
                SaveSettings();
            });
            PersistentLockCB.AlignTo(LockForestryCB, UIAlignAnchor.BottomLeft);
            PersistentLockCB.relativePosition = new Vector3(0, LockForestryCB.height);
            UILabel SwayLabel = AddDescription(panel, "SwayLabel", PersistentLockCB.label, SmallFontScale, locale.GetLocale("SwayLabel"));
            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            MaxTreeLabel = ScalePanel.Find<UILabel>("Label");
            MaxTreeLabel.width = panel.width - 100;
            MaxTreeLabel.textScale = 1.1f;
            MaxTreeLabel.text = string.Format(locale.GetLocale("MaxTreeLimit"), MaxTreeLimit);
            TreeScaleFactorSlider = AddSlider(ScalePanel, MinScaleFactor, MaxScaleFactor, 0.5f, TreeScaleFactor, (_, val) => {
                TreeScaleFactor = val;
                MaxTreeLabel.text = string.Format(SingletonLite<TALocale>.instance.GetLocale("MaxTreeLimit"), MaxTreeLimit);
                SaveSettings();
            });
            ScalePanel.AlignTo(PersistentLockCB, UIAlignAnchor.BottomLeft);
            ScalePanel.relativePosition = new Vector3(0, PersistentLockCB.height + SwayLabel.height + 15);
            TreeScaleFactorSlider.width = panel.width - 150;
            UILabel ImportantLabel = AddDescription(panel, "ImportantLabel", ScalePanel, DefaultFontScale, locale.GetLocale("Important"));
            UIDropDown RemoveReplaceDropDown = AddDropdown(panel, ScalePanel, locale.GetLocale("ReplaceRemoveName"),
                new string[] { locale.GetLocale("ReplaceRemoveDropdown0"), locale.GetLocale("ReplaceRemoveDropdown1"), locale.GetLocale("ReplaceRemoveDropdown2") },
                0, (c, selectIndex) => {
                    RemoveReplaceOrKeep = selectIndex;
                    SaveSettings();
                });
            UILabel RRDLabel = AddDescription(panel, "RRDLabel", RemoveReplaceDropDown, SmallFontScale, locale.GetLocale("ReplaceRemoveDesc"));
            RRDLabel.relativePosition = new Vector3(1, 45);
        }

        private void ShowTreeAnarchyOptions(UIPanel panel) {
            TALocale locale = SingletonLite<TALocale>.instance;
            TreeSnapCB = AddCheckBox(panel, locale.GetLocale("TreeSnap"), UseTreeSnapping, (_, isChecked) => {
                UseTreeSnapping = isChecked;
                if (TAPatcher.MoveItUseTreeSnap != null) {
                    TAPatcher.MoveItUseTreeSnap.SetValue(null, isChecked);
                }
                TAIndicator.TreeSnapIndicator?.SetState(isChecked);
                SaveSettings();
            });
            TreeSnapCB.cachedName = TreeSnapCBName;
            TreeSnapCB.name = TreeSnapCBName;
            TreeSnapCB.AlignTo(panel, UIAlignAnchor.TopLeft);
            TreeSnapCB.relativePosition = new Vector3(2, 5);
            UILabel TreeSnapDesc = AddDescription(panel, "TreeSnapDesc", TreeSnapCB.label, SmallFontScale, locale.GetLocale("TreeSnapLabel"));
#if ENABLETREEANARCHY
            TreeAnarchyCB = AddCheckBox(panel, locale.GetLocale("TreeAnarchy"), UseTreeAnarchy, (_, isChecked) => {
                UseTreeAnarchy = isChecked;
                TAIndicator.TreeAnarchyIndicator?.SetState(isChecked);
                SaveSettings();
            });
            TreeAnarchyCB.cachedName = TreeAnarchyCBName;
            TreeAnarchyCB.name = TreeAnarchyCBName;
            TreeAnarchyCB.AlignTo(TreeSnapCB, UIAlignAnchor.BottomLeft);
            TreeAnarchyCB.relativePosition = new Vector3(0, TreeSnapCB.height + TreeSnapDesc.height);
            UILabel TreeAnarchyDesc = AddDescription(panel, "TreeAnarchyDesc", TreeAnarchyCB.label, SmallFontScale, locale.GetLocale("TreeAnarchyDesc"));
            UIDropDown TreeBehaviourDD = AddDropdown(panel, TreeAnarchyDesc, locale.GetLocale("TreeAnarchyBehaviour"),
                            new string[] { locale.GetLocale("TreeAnarchyHideTree"), locale.GetLocale("TreeAnarchyDeleteTree") },
                            DeleteOnOverlap ? 1 : 0, (_, val) => {
                                DeleteOnOverlap = val != 0;
                                SaveSettings();
                            });
            TreeBehaviourDD.parent.relativePosition = new Vector3(0, TreeAnarchyDesc.height);
#endif
        }

        private static UIPanel AddTab(UITabstrip tabStrip, string tabName, int tabIndex, bool autoLayout) {
            UIButton tabButton = tabStrip.AddTab(tabName);

            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";
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

        private static UICheckBox AddCheckBox(UIPanel panel, string name, bool defaultVal, PropertyChangedEventHandler<bool> callback) {
            UICheckBox cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate"));
            cb.eventCheckChanged += callback;
            cb.text = name;
            cb.height += 10;
            cb.isChecked = defaultVal;
            return cb;
        }

        private static void AddSpace(UIPanel panel, float height) {
            UIPanel space = panel.AddUIComponent<UIPanel>();
            space.name = "Space";
            space.isInteractive = false;
            space.height = height;
        }

        private static UILabel AddDescription(UIPanel panel, string name, UIComponent alignTo, float fontScale, string text) {
            UILabel desc = panel.AddUIComponent<UILabel>();
            if (!(alignTo is null)) desc.AlignTo(alignTo, UIAlignAnchor.BottomLeft);
            desc.name = name;
            desc.width = panel.width - 80;
            desc.wordWrap = true;
            desc.autoHeight = true;
            desc.textScale = fontScale;
            desc.text = text;
            desc.relativePosition = new Vector3(1, 23);
            AddSpace(panel, desc.height);
            return desc;
        }

        private static UISlider AddSlider(UIPanel panel, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            UISlider slider = panel.Find<UISlider>("Slider");
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += callback;
            return slider;
        }

        private static UIDropDown AddDropdown(UIPanel panel, UIComponent alignTo, string text, string[] options, int defaultSelection, PropertyChangedEventHandler<int> callback) {
            UIPanel uiPanel = panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsDropdownTemplate")) as UIPanel;
            uiPanel.AlignTo(alignTo, UIAlignAnchor.BottomLeft);
            UILabel label = uiPanel.Find<UILabel>("Label");
            label.text = text;
            UIDropDown dropDown = uiPanel.Find<UIDropDown>("Dropdown");
            dropDown.width = 340;
            dropDown.items = options;
            dropDown.selectedIndex = defaultSelection;
            dropDown.eventSelectedIndexChanged += callback;
            return dropDown;
        }

        public static void SetTreeLimitSlider(float value) {
            TreeScaleFactorSlider.value = value;
            SaveSettings();
        }

        public static void SetTreeSnapState(bool isChecked) {
            TreeSnapCB.isChecked = isChecked;
        }

        public static void SetLockForestryState(bool isChecked) {
            LockForestryCB.isChecked = isChecked;
        }

        public static void SetTreeAnarchyState(bool isChecked) {
            TreeAnarchyCB.isChecked = isChecked;
        }
    }
}
