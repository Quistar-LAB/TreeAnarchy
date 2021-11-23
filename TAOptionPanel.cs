using ColossalFramework;
using ColossalFramework.UI;
using System.Threading;
using UI;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    public class TAOptionPanel : UIPanel {
        private const string m_optionPanelName = @"TreeAnarchyOptionPanel";
        private const float MaxScaleFactor = 20.0f;
        private const float MinScaleFactor = 1.5f;
        public const float DefaultFontScale = 0.95f;
        public const float SmallFontScale = 0.85f;
        public const float TabFontScale = 0.9f;
        private const string LockForestryCBName = @"LockForestryCB";
        private const string TreeSnapCBName = @"TreeSnapCB";
        private const string TreeAnarchyCBName = @"TreeAnarchyCB";
        private const string TreeLODFixCBName = @"TreeLODFixCB";
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

            UIPanel mainPanel = AddTab(tabBar, TALocale.GetLocale(@"MainOptionTab"), 0, true);
            mainPanel.autoLayout = false;
            mainPanel.autoSize = false;
            ShowStandardOptions(mainPanel);
            UpdateState(IsInGame);

            UIPanel treeAnarchyPanel = AddTab(tabBar, TALocale.GetLocale(@"TreeAnarchyTab"), 1, true);
            treeAnarchyPanel.autoLayout = false;
            treeAnarchyPanel.autoSize = false;
            ShowTreeAnarchyOptions(treeAnarchyPanel);

            AddTab(tabBar, TALocale.GetLocale(@"KeyboardShortcutTab"), 2, true).gameObject.AddComponent<TAKeyBinding>();
        }

        public static void UpdateState(bool isInGame) {
            if (isInGame) {
                TreeScaleFactorSlider.Disable();
                return;
            }
            TreeScaleFactorSlider.Enable();
        }

        private void ShowStandardOptions(UIPanel panel) {
            UICheckBox indicatorCB = AddCheckBox(panel, TALocale.GetLocale(@"EnableIndicator"), ShowIndicators, (_, isChecked) => {
                ShowIndicators = isChecked;
                ThreadPool.QueueUserWorkItem(SaveSettings);
            });
            indicatorCB.AlignTo(panel, UIAlignAnchor.TopLeft);
            indicatorCB.relativePosition = new Vector3(2, 5);
            UICheckBox WindEffectCB = AddCheckBox(panel, TALocale.GetLocale(@"WindEffect"), TreeEffectOnWind, (_, isChecked) => {
                TreeEffectOnWind = isChecked;
                ThreadPool.QueueUserWorkItem(SaveSettings);
            });
            WindEffectCB.AlignTo(indicatorCB, UIAlignAnchor.BottomLeft);
            WindEffectCB.relativePosition += new Vector3(0, indicatorCB.height);
            UILabel WindEffectLabel = AddDescription(panel, @"WindEffectLabel", WindEffectCB.label, SmallFontScale, TALocale.GetLocale(@"WindEffectLabel"));
            LockForestryCB = AddCheckBox(panel, TALocale.GetLocale(@"LockForestry"), UseLockForestry, (_, isChecked) => {
                UseLockForestry = isChecked;
                UIIndicator.LockForestryIndicator?.SetState(isChecked);
                ThreadPool.QueueUserWorkItem(SaveSettings);
            });
            LockForestryCB.AlignTo(WindEffectCB, UIAlignAnchor.BottomLeft);
            LockForestryCB.relativePosition = new Vector3(0, WindEffectCB.height + WindEffectLabel.height);
            LockForestryCB.cachedName = LockForestryCBName;
            LockForestryCB.name = LockForestryCBName;
            LockForestryCB.width = 300;

            UIPanel SwayPanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsSliderTemplate"));
            UILabel SwayFactorLabel = SwayPanel.Find<UILabel>(@"Label");
            SwayFactorLabel.text = TALocale.GetLocale(@"TreeSwayFactor");
            SwayFactorLabel.textScale = DefaultFontScale;
            SwayFactorLabel.width += 80;
            SwayFactorLabel.Disable();
            AddSlider(SwayPanel, 0f, 1f, 0.1f, TreeSwayFactor, (_, val) => {
                TreeSwayFactor = val;
                if (IsInGame) TAManager.UpdateTreeSway();
                ThreadPool.QueueUserWorkItem(SaveSettings);
            }).width -= 10;
            SwayPanel.AlignTo(LockForestryCB, UIAlignAnchor.TopRight);
            SwayPanel.relativePosition = new Vector3(380, 0);
            UICheckBox PersistentLockCB = AddCheckBox(panel, TALocale.GetLocale(@"PersistentLock"), PersistentLockForestry, (_, isChecked) => {
                PersistentLockForestry = isChecked;
                ThreadPool.QueueUserWorkItem(SaveSettings);
            });
            PersistentLockCB.AlignTo(LockForestryCB, UIAlignAnchor.BottomLeft);
            PersistentLockCB.relativePosition = new Vector3(0, LockForestryCB.height);
            UILabel SwayLabel = AddDescription(panel, @"SwayLabel", PersistentLockCB.label, SmallFontScale, TALocale.GetLocale(@"SwayLabel"));
            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsSliderTemplate"));
            MaxTreeLabel = ScalePanel.Find<UILabel>(@"Label");
            MaxTreeLabel.width = panel.width - 100;
            MaxTreeLabel.textScale = 1.1f;
            MaxTreeLabel.text = string.Format(TALocale.GetLocale(@"MaxTreeLimit"), MaxTreeLimit);
            TreeScaleFactorSlider = AddSlider(ScalePanel, MinScaleFactor, MaxScaleFactor, 0.5f, TreeScaleFactor, (_, val) => {
                TreeScaleFactor = val;
                MaxTreeLabel.text = string.Format(TALocale.GetLocale(@"MaxTreeLimit"), MaxTreeLimit);
                ThreadPool.QueueUserWorkItem(SaveSettings);
            });
            ScalePanel.AlignTo(PersistentLockCB, UIAlignAnchor.BottomLeft);
            ScalePanel.relativePosition = new Vector3(0, PersistentLockCB.height + SwayLabel.height + 15);
            TreeScaleFactorSlider.width = panel.width - 150;
            UILabel ImportantLabel = AddDescription(panel, @"ImportantLabel", ScalePanel, DefaultFontScale, TALocale.GetLocale(@"Important"));
            UIDropDown RemoveReplaceDropDown = AddDropdown(panel, ScalePanel, TALocale.GetLocale(@"ReplaceRemoveName"),
                new string[] { TALocale.GetLocale(@"ReplaceRemoveDropdown0"), TALocale.GetLocale(@"ReplaceRemoveDropdown1"), TALocale.GetLocale(@"ReplaceRemoveDropdown2") },
                0, (c, selectIndex) => {
                    RemoveReplaceOrKeep = selectIndex;
                    ThreadPool.QueueUserWorkItem(SaveSettings);
                });
            UILabel RRDLabel = AddDescription(panel, @"RRDLabel", RemoveReplaceDropDown, SmallFontScale, TALocale.GetLocale(@"ReplaceRemoveDesc"));
            RRDLabel.relativePosition = new Vector3(1, 45);
        }

        private void ShowTreeAnarchyOptions(UIPanel panel) {
            TreeSnapCB = AddCheckBox(panel, TALocale.GetLocale(@"TreeSnap"), UseTreeSnapping, (_, isChecked) => {
                UseTreeSnapping = isChecked;
                if (TAPatcher.isMoveItInstalled && TAPatcher.MoveItUseTreeSnap != null) {
                    TAPatcher.MoveItUseTreeSnap.SetValue(null, isChecked);
                }
                TAPatcher.MoveItUseTreeSnap?.SetValue(null, isChecked);
                UIIndicator.SnapIndicator?.SetState(isChecked);
                ThreadPool.QueueUserWorkItem(SaveSettings);
            });
            TreeSnapCB.cachedName = TreeSnapCBName;
            TreeSnapCB.name = TreeSnapCBName;
            TreeSnapCB.AlignTo(panel, UIAlignAnchor.TopLeft);
            TreeSnapCB.relativePosition = new Vector3(2, 5);
            UILabel TreeSnapDesc = AddDescription(panel, @"TreeSnapDesc", TreeSnapCB.label, SmallFontScale, TALocale.GetLocale(@"TreeSnapLabel"));
            TreeAnarchyCB = AddCheckBox(panel, TALocale.GetLocale(@"TreeAnarchy"), UseTreeAnarchy, (_, isChecked) => {
                UseTreeAnarchy = isChecked;
                UIIndicator.AnarchyIndicator?.SetState(isChecked);
                ThreadPool.QueueUserWorkItem(SaveSettings);
            });
            TreeAnarchyCB.cachedName = TreeAnarchyCBName;
            TreeAnarchyCB.name = TreeAnarchyCBName;
            TreeAnarchyCB.AlignTo(TreeSnapCB, UIAlignAnchor.BottomLeft);
            TreeAnarchyCB.relativePosition = new Vector3(0, TreeSnapCB.height + TreeSnapDesc.height);
            UILabel TreeAnarchyDesc = AddDescription(panel, @"TreeAnarchyDesc", TreeAnarchyCB.label, SmallFontScale, TALocale.GetLocale(@"TreeAnarchyDesc"));
            UIDropDown TreeBehaviourDD = AddDropdown(panel, panel, TALocale.GetLocale(@"TreeAnarchyBehaviour"),
                            new string[] { TALocale.GetLocale(@"TreeAnarchyHideTree"), TALocale.GetLocale(@"TreeAnarchyDeleteTree") },
                            DeleteOnOverlap ? 1 : 0, (_, val) => {
                                DeleteOnOverlap = val != 0;
                                ThreadPool.QueueUserWorkItem(SaveSettings);
                            });
            TreeBehaviourDD.parent.relativePosition = new Vector3(27, TreeSnapCB.height + TreeSnapDesc.height + TreeAnarchyCB.height + TreeAnarchyDesc.height);
            UIDropDown TreeLODFixDD = AddDropdown(panel, panel, null,
                            new string[] { TALocale.GetLocale(@"TreeLODLow"), TALocale.GetLocale(@"TreeLODMedium"), TALocale.GetLocale(@"TreeLODHigh"), TALocale.GetLocale(@"TreeLODUltraHigh") },
                            (int)TreeLODSelectedResolution, (_, val) => {
                                TreeLODSelectedResolution = (TAManager.TreeLODResolution)val;
                                if (IsInGame) {
                                    Singleton<TreeManager>.instance.SetResolution((TAManager.TreeLODResolution)val);
                                }
                                ThreadPool.QueueUserWorkItem(SaveSettings);
                            });
            TreeLODFixDD.disabledColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            TreeLODFixDD.disabledTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            TreeLODFixDD.autoListWidth = true;
            TreeLODFixDD.width = 200f;
            TreeLODFixDD.textScale = 0.95f;
            TreeLODFixDD.height = 28;
            TreeLODFixDD.itemHeight = 22;
            string treeLODFixName = TALocale.GetLocale(@"TreeLODFix");
            UICheckBox TreeLodFixCB = AddCheckBox(panel, treeLODFixName, UseTreeLODFix, (_, isChecked) => {
                UseTreeLODFix = isChecked;
                if (isChecked) TreeLODFixDD.Enable();
                else TreeLODFixDD.Disable();
                ThreadPool.QueueUserWorkItem(SaveSettings);
            });
            TreeLodFixCB.cachedName = TreeLODFixCBName;
            TreeLodFixCB.name = TreeLODFixCBName;
            TreeLodFixCB.AlignTo(TreeAnarchyCB, UIAlignAnchor.BottomLeft);
            UILabel label = TreeLodFixCB.Find<UILabel>(@"Label");
            UIFontRenderer renderer = label.ObtainRenderer();
            Vector2 size = renderer.MeasureString(treeLODFixName);
            TreeLodFixCB.relativePosition = new Vector3(0, TreeAnarchyCB.height + TreeAnarchyDesc.height + TreeBehaviourDD.parent.height);
            TreeLODFixDD.parent.relativePosition = new Vector3(35 + size.x, TreeSnapCB.height + TreeSnapDesc.height + TreeAnarchyCB.height + TreeAnarchyDesc.height + TreeBehaviourDD.parent.height + 3);
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

        private static UICheckBox AddCheckBox(UIPanel panel, string name, bool defaultVal, PropertyChangedEventHandler<bool> callback) {
            UICheckBox cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsCheckBoxTemplate"));
            cb.eventCheckChanged += callback;
            cb.text = name;
            cb.height += 10;
            cb.isChecked = defaultVal;
            return cb;
        }

        private static void AddSpace(UIPanel panel, float height) {
            UIPanel space = panel.AddUIComponent<UIPanel>();
            space.name = @"Space";
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
            uiPanel.AlignTo(alignTo, UIAlignAnchor.BottomLeft);
            UILabel label = uiPanel.Find<UILabel>(@"Label");
            if (text.IsNullOrWhiteSpace()) {
                label.Hide();
            } else {
                label.text = text;
            }
            UIDropDown dropDown = uiPanel.Find<UIDropDown>(@"Dropdown");
            dropDown.width = 380;
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
