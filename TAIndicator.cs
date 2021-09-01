using ColossalFramework;
using ColossalFramework.UI;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace TreeAnarchy {
    public class TAIndicator : UIPanel {
        public static TAIndicator instance;
        private const float indicatorWidth = 24f;
        private const float indicatorHeight = 24f;
        private const int spriteMaxSize = 32;
        private const string IndicatorPanelName = "TreeAnarchyIndicatorPanel";
        private UIIndicator m_treeSnapIndicator;
        private UIIndicator m_lockForestryIndicator;
        public static UIIndicator TreeSnapIndicator { get => instance?.m_treeSnapIndicator; }
        public static UIIndicator LockForestryIndicator { get => instance?.m_lockForestryIndicator; }
#if ENABLETREEANARCHY
        private UIIndicator m_treeAnarchyIndicator;
        public static UIIndicator TreeAnarchyIndicator { get => instance?.m_treeAnarchyIndicator; }
#endif

        public TAIndicator() {
            instance = this;
        }

        public class UIIndicator : UIPanel {
            private bool state;
            public override void Awake() {
                base.Awake();
                isLocalized = true;
            }
            protected override void OnLocalizeTooltip() {
                if (!string.IsNullOrEmpty(name)) {
                    tooltip = SingletonLite<TALocale>.instance.GetLocale(state ? name + @"IsOn" : name + @"IsOff");
                }
            }
            public virtual void UpdateState() => backgroundSprite = state ? name + @"Enabled" : name + @"Disabled";
            public void SetState(bool isEnabled) {
                state = isEnabled;
                if (!(name is null)) {
                    backgroundSprite = state ? name + @"Enabled" : name + @"Disabled";
                    OnLocalizeTooltip();
                    RefreshTooltip();
                }
            }
        }

        protected override void OnResolutionChanged(Vector2 previousResolution, Vector2 currentResolution) {
            base.OnResolutionChanged(previousResolution, currentResolution);
            relativePosition = new Vector3(0f - width - 5f, 0f);
        }

        /* Hate this hack, but the only way I can think of
         * If anyone has a better solution, please don't hesitate to send a pull request
         * or email me. Thanks in advance */
        private void OnInitialFrame(object _) {
            while (UIView.GetAView().framesRendered < 10) { Thread.Sleep(100); }
            relativePosition = new Vector3(0f - width - 5f, 0f);
        }

        public override void OnEnable() {
            base.OnEnable();
            name = IndicatorPanelName;

            m_treeSnapIndicator = AddIndicator("TreeSnap", TAMod.UseTreeSnapping, (_, p) => {
                bool state = TAMod.UseTreeSnapping = !TAMod.UseTreeSnapping;
                m_treeSnapIndicator.SetState(state);
                TAOptionPanel.SetTreeSnapState(state);
                TAMod.SaveSettings();
            });
#if ENABLETREEANARCHY
            m_treeAnarchyIndicator = AddIndicator("TreeAnarchy", TAMod.UseTreeAnarchy, (_, p) => {
                bool state = TAMod.UseTreeAnarchy = !TAMod.UseTreeAnarchy;
                m_treeAnarchyIndicator.SetState(state);
                TAOptionPanel.SetTreeAnarchyState(state);
                TAMod.SaveSettings();
            }, m_treeSnapIndicator);
            m_lockForestryIndicator = AddIndicator("LockForestry", TAMod.UseLockForestry, (_, p) => {
                bool state = TAMod.UseLockForestry = !TAMod.UseLockForestry;
                m_lockForestryIndicator.SetState(state);
                TAOptionPanel.SetLockForestryState(state);
                TAMod.SaveSettings();
            }, m_treeAnarchyIndicator);
            size = new Vector2(indicatorWidth * 3, indicatorHeight);
#else
            m_lockForestryIndicator = AddIndicator("LockForestry", TAMod.UseLockForestry, (_, p) => {
                bool state = (TAMod.UseLockForestry = !TAMod.UseLockForestry);
                m_lockForestryIndicator.SetState(state);
                TAOptionPanel.SetLockForestryState(state);
                TAMod.SaveSettings();
            }, m_treeSnapIndicator);
            size = new Vector2(indicatorWidth * 2, indicatorHeight);
#endif
            UIButton uIButton = parent.Find<UIButton>("Heat'o'meter");
            if (uIButton is null) {
                uIButton = parent.Find<UIButton>("PopulationPanel");
            }
            if (!(uIButton is null)) {
                uIButton.relativePosition += new Vector3(10f, 0f);
                cachedTransform.parent = uIButton.cachedTransform;
                transform.parent = uIButton.transform;
                ThreadPool.QueueUserWorkItem(OnInitialFrame);
            }
        }

        public override void OnDisable() {
            base.OnDisable();
            RemoveUIComponent(m_treeSnapIndicator);
            RemoveUIComponent(m_lockForestryIndicator);
            Destroy(m_treeSnapIndicator.gameObject);
            Destroy(m_lockForestryIndicator.gameObject);
            m_treeSnapIndicator = null;
            m_lockForestryIndicator = null;
#if ENABLETREEANARCHY
            RemoveUIComponent(m_treeAnarchyIndicator);
            Destroy(m_treeAnarchyIndicator.gameObject);
            m_treeAnarchyIndicator = null;
#endif
        }

        public UIIndicator AddIndicator(string indicatorName, bool defState, MouseEventHandler callback, UIComponent anchor = null) {
            UIIndicator indicator = AddUIComponent<UIIndicator>();
            indicator.name = indicatorName;
            indicator.atlas = CreateTextureAtlas($"TA" + indicatorName + "Atlas", new string[] {
                    indicatorName + "Enabled",
                    indicatorName + "Disabled"
                });
            indicator.size = new Vector3(indicatorWidth, indicatorHeight);
            indicator.eventClicked += callback;
            indicator.playAudioEvents = true;
            indicator.relativePosition = (anchor is null) ? Vector3.zero : new Vector3(anchor.relativePosition.x + anchor.width, 0f);
            indicator.SetState(defState);
            return indicator;
        }

        private static UITextureAtlas CreateTextureAtlas(string atlasName, string[] spriteNames) {
            Texture2D texture2D = new(spriteMaxSize, spriteMaxSize, TextureFormat.ARGB32, false);
            Texture2D[] array = new Texture2D[spriteNames.Length];
            for (int i = 0; i < spriteNames.Length; i++) {
                array[i] = LoadTextureFromAssembly(spriteNames[i] + ".png");
            }
            Rect[] array2 = texture2D.PackTextures(array, 2, spriteMaxSize);
            UITextureAtlas uITextureAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            Material material = Instantiate<Material>(UIView.GetAView().defaultAtlas.material);
            material.mainTexture = texture2D;
            uITextureAtlas.material = material;
            uITextureAtlas.name = atlasName;
            for (int j = 0; j < spriteNames.Length; j++) {
                UITextureAtlas.SpriteInfo item = new() {
                    name = spriteNames[j],
                    texture = array[j],
                    region = array2[j]
                };
                uITextureAtlas.AddSprite(item);
            }
            return uITextureAtlas;
        }

        private static Texture2D LoadTextureFromAssembly(string filename) {
            UnmanagedMemoryStream s = (UnmanagedMemoryStream)Assembly.GetExecutingAssembly().GetManifestResourceStream("TreeAnarchy.Resources." + filename);
            byte[] array = new byte[s.Length];
            s.Read(array, 0, array.Length);
            Texture2D texture = new(2, 2, TextureFormat.ARGB32, false);
            texture.LoadImage(array);
            texture.Compress(false);
            return texture;
        }
    }
}
