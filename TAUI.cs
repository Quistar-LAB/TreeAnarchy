using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal static class TAUI {
        private const float MaxScaleFactor = 8.0f;
        private const float MinScaleFactor = 1.5f;
        internal const float DefaultFontScale = 0.95f;
        internal const float SmallFontScale = 0.85f;
        private const string TREESNAPSPRITENAME = "TreeSnap";
        private const string LOCKFORESTRYSPRITENAME = "LockForestry";

        internal const string MainOptionPanelName = "MainOptionContainer";
        internal const string TreeSnapPanelName = "TreeSnapContainer";
        internal const string TreeSnapCBName = "TreeSnapCB";
        internal const string LockForestryCBName = "LockForestryCB";
        internal const uint TREESNAPID = 0x0000;
        internal const uint LOCKFORESTRYID = 0x0001;

        private static UITabstrip tabBar = default;
        private static UILabel MaxTreeLabel = default;
        public static UICheckBox TreeSnapCB = default;
        public static UICheckBox LockForestryCB = default;
        private static UISlider TreeScaleFactor;

        /* Main Indicator Panel data */
        internal static UIPanel mainUIPanel;
        private static UIPanel treeSnapIndicator;
        private static UIPanel treeAnarchyIndicator;
        private static UIPanel lockForestryIndicator;

        private static void OnTreeWindCheckChanged(UIComponent _, bool isChecked) {
            TreeEffectOnWind = isChecked;
            SaveSettings();
        }
        private static void OnTreeSnapCheckChanged(UIComponent _, bool isChecked) {
            UseTreeSnapping = isChecked;
            if (TAPatcher.MoveItUseTreeSnap != null) {
                TAPatcher.MoveItUseTreeSnap.SetValue(null, isChecked);
            }
            SaveSettings();
        }
        private static void OnTreeRotationCheckChanged(UIComponent _, bool isChecked) {
            RandomTreeRotation = isChecked;
            if (RandomTreeRotation) RandomTreeRotationFactor = 1000;
            else RandomTreeRotationFactor = 0;
            SaveSettings();
        }
        private static void OnTreeSwayFactorChanged(UIComponent _, float val) {
            TAMod.TreeSwayFactor = val;
            if (IsInGame) Patches.TreeMovement.UpdateTreeSway();
            SaveSettings();
        }
        private static void OnLockForestryCheckChanged(UIComponent _, bool isChecked) {
            UseLockForestry = isChecked;
            SaveSettings();
        }
        private static void OnPersistentLockCheckChanged(UIComponent _, bool isChecked) {
            PersistentLockForestry = isChecked;
            SaveSettings();
        }
        private static void OnTreeScaleFactorChanged(UIComponent _, float val) {
            TAMod.TreeScaleFactor = val;
            MaxTreeLabel.text = String.Format(SingletonLite<TALocale>.instance.GetLocale("MaxTreeLimit"), MaxTreeLimit);
            SaveSettings();
        }

        private static void OnReplaceRemoveKeepEventChanged(UIComponent _, int val) {
            RemoveReplaceOrKeep = val;
            SaveSettings();
        }

        private static void OnIndicatorCheckChanged(UIComponent _, bool isChecked) {
            ShowIndicators = isChecked;
            SaveSettings();
        }

        private static void SetIndicatorImpl(bool isEnabled, UIPanel indicator) {
            if (isEnabled) {
                indicator.backgroundSprite = indicator.stringUserData + "Enabled";
                return;
            }
            indicator.backgroundSprite = indicator.stringUserData + "Disabled";
        }

        public static void SetIndicator(bool isEnabled, uint typeID) {
            switch (typeID) {
            case TREESNAPID: SetIndicatorImpl(isEnabled, treeSnapIndicator); break;
            case LOCKFORESTRYID: SetIndicatorImpl(isEnabled, lockForestryIndicator); break;
            }
        }

        private static void Indicator_Clicked(UIComponent component, UIMouseEventParameter eventParam) {
            switch (component.objectUserData) {
            case TREESNAPID:
                UseTreeSnapping = !UseTreeSnapping;
                SetIndicatorImpl(UseTreeSnapping, component as UIPanel);
                break;
            case LOCKFORESTRYID:
                UseLockForestry = !UseLockForestry;
                SetIndicatorImpl(UseLockForestry, component as UIPanel);
                break;
            }
        }

        private static UIPanel CreateIndicator(UIPanel anchor, uint id, string sprite, string name) {
            UIPanel ind;
            ind = mainUIPanel.AddUIComponent<UIPanel>();
            ind.atlas = CreateTextureAtlas("TA" + name.Replace(" ", "") + "Atlas", new string[] { sprite + @"Enabled", sprite + @"Disabled" });
            ind.size = new Vector3(24, 24);
            ind.eventClicked += Indicator_Clicked;
            ind.playAudioEvents = true;
            ind.name = sprite;
            ind.stringUserData = sprite;
            ind.objectUserData = id;
            ind.relativePosition = anchor is null ? Vector3.zero : new Vector3(anchor.relativePosition.x + anchor.width, 0);
            return ind;
        }

        private const int MainPanelWidth = 80;
        private const int MainPanelHeight = 24;
        internal static void CreateMainUI() {
            mainUIPanel = UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            mainUIPanel.name = @"TreeAnarchyPanel";
            mainUIPanel.size = new Vector3(MainPanelWidth, MainPanelHeight);

            UIPanel refPanel = UIView.GetAView().FindUIComponent<UIPanel>(@"Demand");
            mainUIPanel.relativePosition = refPanel.absolutePosition + new Vector3(refPanel.width + 10, refPanel.height * 0.5f - 12);

            treeSnapIndicator = CreateIndicator(null, TREESNAPID, TREESNAPSPRITENAME, "Tree Snapping");
            SetIndicatorImpl(UseTreeSnapping, treeSnapIndicator);
            treeSnapIndicator.tooltip = "Tree Snapping";
            lockForestryIndicator = CreateIndicator(treeSnapIndicator, LOCKFORESTRYID, LOCKFORESTRYSPRITENAME, "Lock Forestry");
            SetIndicatorImpl(UseLockForestry, lockForestryIndicator);
            lockForestryIndicator.tooltip = "Lock Forestry";
        }


        private const int spriteMaxSize = 32;
        private static UITextureAtlas CreateTextureAtlas(string atlasName, string[] spriteNames) {
            Texture2D texture2D = new Texture2D(spriteMaxSize, spriteMaxSize, TextureFormat.ARGB32, false);
            Texture2D[] textures = new Texture2D[spriteNames.Length];

            for (int i = 0; i < spriteNames.Length; i++) {
                textures[i] = LoadTextureFromAssembly(spriteNames[i] + ".png");
            }

            Rect[] regions = texture2D.PackTextures(textures, 2, spriteMaxSize);

            UITextureAtlas textureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            Material material = UnityEngine.Object.Instantiate(UIView.GetAView().defaultAtlas.material);
            material.mainTexture = texture2D;
            textureAtlas.material = material;
            textureAtlas.name = atlasName;

            for (int i = 0; i < spriteNames.Length; i++) {
                UITextureAtlas.SpriteInfo item = new UITextureAtlas.SpriteInfo {
                    name = spriteNames[i],
                    texture = textures[i],
                    region = regions[i],
                };
                textureAtlas.AddSprite(item);
            }
            return textureAtlas;
        }

        private static Texture2D LoadTextureFromAssembly(string filename) {
            UnmanagedMemoryStream s = (UnmanagedMemoryStream)Assembly.GetExecutingAssembly().GetManifestResourceStream("TreeAnarchy.Resources." + filename);
            byte[] buf = new byte[s.Length];
            s.Read(buf, 0, buf.Length);
            Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            texture2D.LoadImage(buf);
            texture2D.Compress(true);
            return texture2D;
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
            TALocale locale = SingletonLite<TALocale>.instance;
            UIPanel rootPanel = (UIPanel)helper.self;
            if (!(tabBar is null)) {
                UnityEngine.Object.Destroy(tabBar);
            }
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
            ShowTreeAnarchyOptions(treesnapHelper);

            AddTab(tabBar, locale.GetLocale("KeyboardShortcutTab"), 2, true).gameObject.AddComponent<TAKeyBinding>();
        }

        private static void ShowStandardOptions(UIHelper option) {
            TALocale locale = SingletonLite<TALocale>.instance;
            UIPanel panel = (UIPanel)option.self;
            _ = AddCheckBox(panel, "Enable in game indicators", ShowIndicators, OnIndicatorCheckChanged);
            UICheckBox WindEffect = AddCheckBox(panel, locale.GetLocale("WindEffect"), TreeEffectOnWind, OnTreeWindCheckChanged);
            _ = AddLabel(panel, WindEffect, SmallFontScale, locale.GetLocale("WindEffectLabel"));
            UICheckBox TreeRotation = AddCheckBox(panel, locale.GetLocale("RandomTreeRotation"), RandomTreeRotation, OnTreeRotationCheckChanged);
            TreeRotation.width = 300;
            UIPanel SwayPanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            UILabel SwayFactorLabel = SwayPanel.Find<UILabel>("Label");
            SwayFactorLabel.text = locale.GetLocale("TreeSwayFactor");
            SwayFactorLabel.textScale = DefaultFontScale;
            SwayFactorLabel.width += 50;
            SwayFactorLabel.Disable();
            UISlider TreeSwayFactor = AddSlider(SwayPanel, 0f, 1f, 0.1f, TAMod.TreeSwayFactor, OnTreeSwayFactorChanged);
            SwayPanel.AlignTo(TreeRotation, UIAlignAnchor.TopRight);
            SwayPanel.relativePosition = new Vector3(320, 0);
            TreeSwayFactor.width += 40;
            LockForestryCB = AddCheckBox(panel, locale.GetLocale("LockForestry"), UseLockForestry, OnLockForestryCheckChanged);
            LockForestryCB.cachedName = LockForestryCBName;
            LockForestryCB.name = LockForestryCBName;
            LockForestryCB.width = 300;
            UICheckBox PersistentLock = AddCheckBox(panel, locale.GetLocale("PersistentLock"), PersistentLockForestry, OnPersistentLockCheckChanged);
            _ = AddLabel(panel, PersistentLock, SmallFontScale, locale.GetLocale("SwayLabel"));
            SwayPanel.BringToFront();
        }

        private static float ShowTreeLimitOption(UIHelper option) {
            float totalHeight = 0;
            UIPanel panel = (UIPanel)option.self;
            TALocale locale = SingletonLite<TALocale>.instance;

            UILabel MaxTreeTitle = panel.AddUIComponent<UILabel>();
            MaxTreeTitle.AlignTo(panel, UIAlignAnchor.BottomLeft);
            MaxTreeTitle.width = panel.width - 80;
            MaxTreeTitle.wordWrap = true;
            MaxTreeTitle.autoHeight = true;
            MaxTreeTitle.textScale = 1.15f;
            MaxTreeTitle.text = locale.GetLocale("MaxTreeLimitTitle");
            MaxTreeTitle.relativePosition = new Vector3(25, 25);
            totalHeight += MaxTreeTitle.height;
            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            MaxTreeLabel = ScalePanel.Find<UILabel>("Label");
            MaxTreeLabel.width = panel.width - 100;
            MaxTreeLabel.autoSize = false;
            MaxTreeLabel.autoHeight = true;
            MaxTreeLabel.text = String.Format(locale.GetLocale("MaxTreeLimit"), MaxTreeLimit);
            totalHeight += MaxTreeLabel.height;
            TreeScaleFactor = AddSlider(ScalePanel, MinScaleFactor, MaxScaleFactor, 0.5f, TAMod.TreeScaleFactor, OnTreeScaleFactorChanged);
            TreeScaleFactor.width = panel.width - 150;
            totalHeight += TreeScaleFactor.height;
            totalHeight += AddLabel(panel, ScalePanel, DefaultFontScale, locale.GetLocale("Important")).height;
            UIDropDown RemoveReplaceDropDown = AddDropdown(panel, locale.GetLocale("ReplaceRemoveName"),
                new string[] { locale.GetLocale("ReplaceRemoveDropdown0"), locale.GetLocale("ReplaceRemoveDropdown1"), locale.GetLocale("ReplaceRemoveDropdown2") },
                0, OnReplaceRemoveKeepEventChanged);
            totalHeight += RemoveReplaceDropDown.height;
            UILabel ReplaceRemoveLabel = AddLabel(panel, RemoveReplaceDropDown, SmallFontScale, locale.GetLocale("ReplaceRemoveDesc"));
            ReplaceRemoveLabel.relativePosition = new Vector3(0, RemoveReplaceDropDown.height + 5);
            totalHeight += ReplaceRemoveLabel.height;
            return totalHeight;
        }

        private static void ShowTreeAnarchyOptions(UIHelper option) {
            UIPanel panel = (UIPanel)option.self;
            TALocale locale = SingletonLite<TALocale>.instance;
            TreeSnapCB = AddCheckBox(panel, locale.GetLocale("TreeSnap"), UseTreeSnapping, OnTreeSnapCheckChanged);
            TreeSnapCB.cachedName = TreeSnapCBName;
            TreeSnapCB.name = TreeSnapCBName;
            _ = AddLabel(panel, TreeSnapCB, SmallFontScale, locale.GetLocale("TreeSnapLabel"));
        }

        private static void UpdateState(bool isInGame) {
            if (isInGame) {
                TreeScaleFactor.Disable();
                return;
            }
            TreeScaleFactor.Enable();
        }

        private static UICheckBox AddCheckBox(UIPanel panel, string name, bool defaultVal, PropertyChangedEventHandler<bool> callback) {
            UICheckBox cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate"));
            cb.isChecked = defaultVal;
            cb.autoSize = true;
            cb.text = name;
            cb.eventCheckChanged += new PropertyChangedEventHandler<bool>(callback);
            cb.height += 10;
            panel.height += cb.height;
            return cb;
        }

        private static UILabel AddLabel(UIPanel panel, UIComponent alignTo, float fontScale, string text) {
            UILabel label = panel.AddUIComponent<UILabel>();
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
            return label;
        }

        private static UISlider AddSlider(UIPanel panel, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            UISlider slider = panel.Find<UISlider>("Slider");
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += new PropertyChangedEventHandler<float>(callback);
            panel.height += slider.height;
            return slider;
        }

        private static UIDropDown AddDropdown(UIPanel panel, string text, string[] options, int defaultSelection, PropertyChangedEventHandler<int> callback) {
            UIPanel uiPanel = panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsDropdownTemplate")) as UIPanel;
            uiPanel.Find<UILabel>("Label").text = text;
            UIDropDown dropDown = uiPanel.Find<UIDropDown>("Dropdown");
            dropDown.width = 300;
            dropDown.items = options;
            dropDown.selectedIndex = defaultSelection;
            dropDown.eventSelectedIndexChanged += new PropertyChangedEventHandler<int>(callback);
            panel.height += dropDown.height;
            return dropDown;
        }

        internal static void SetTreeLimitSlider(float value) {
            TreeScaleFactor.value = value;
            SaveSettings();
        }
    }
}
