using ColossalFramework.UI;
using UnityEngine;

namespace TreeAnarchy.UI {
    public class UIFancySlider : UIComponent {
        private const float HEIGHT = 35f;
        private const float SLIDERWIDTH = 200f;
        private const float LEFTSPRITEWIDTH = 50f;
        private const float RIGHTSPRITEWIDTH = 50f;
        private UISlider m_slider;
        private UILabel m_sliderLabel;
        private UIPanel m_sliderPanel;
        private UILabel m_leftLabel;
        private UILabel m_rightLabel;

        public string text {
            get => m_sliderLabel.text;
            set => m_sliderLabel.text = value;
        }

        public float value {
            get => m_slider.value;
            set => m_slider.value = value;
        }

        public override void Awake() {
            UITextureAtlas atlas = UIUtils.CreateTextureAtlas(@"FancySliderAtlas", new string[] { @"leftSprite", @"midSprite", @"rightSprite", @"slider" });
            UIPanel sliderPanel = (UIPanel)AttachUIComponent(UITemplateManager.GetAsGameObject(@"OptionsSliderTemplate"));
            sliderPanel.atlas = atlas;
            sliderPanel.autoLayout = false;
            sliderPanel.autoSize = false;
            m_sliderPanel = sliderPanel;
            UILabel sliderLabel = sliderPanel.Find<UILabel>(@"Label");
            sliderLabel.autoSize = true;
            sliderLabel.verticalAlignment = UIVerticalAlignment.Middle;
            sliderLabel.textAlignment = UIHorizontalAlignment.Center;
            sliderLabel.textScale = 0.85f;
            sliderLabel.padding = new RectOffset(0, 0, 4, 0);
            sliderLabel.textColor = new Color32(0x00, 0x00, 0x00, 0x50);
            //sliderLabel.useDropShadow = true;
            sliderLabel.wordWrap = false;
            m_sliderLabel = sliderLabel;
            UISlider slider = sliderPanel.Find<UISlider>(@"Slider");
            slider.atlas = atlas;
            slider.backgroundSprite = @"midSprite";
            UISprite fillIndicator = slider.AddUIComponent<UISprite>();
            fillIndicator.atlas = atlas;
            fillIndicator.spriteName = @"slider";
            slider.fillIndicatorObject = fillIndicator;
            UISprite thumb = slider.thumbObject as UISprite;
            thumb.spriteName = @"";
            m_slider = slider;
            sliderLabel.AlignTo(slider, UIAlignAnchor.TopLeft);
            UILabel leftLabel = AddUIComponent<UILabel>();
            leftLabel.atlas = atlas;
            leftLabel.backgroundSprite = @"leftSprite";
            leftLabel.autoSize = false;
            leftLabel.autoHeight = false;
            leftLabel.textAlignment = UIHorizontalAlignment.Center;
            leftLabel.verticalAlignment = UIVerticalAlignment.Middle;
            leftLabel.textColor = new Color32(0xee, 0xee, 0xee, 0x50);
            leftLabel.textScale = 0.60f;
            leftLabel.dropShadowOffset = new Vector2(-1.2f, 1.2f);
            leftLabel.dropShadowColor = new Color32(0x00, 0x00, 0x00, 0x40);
            leftLabel.useDropShadow = true;
            leftLabel.padding = new RectOffset(0, 0, 3, 0);
            leftLabel.outlineSize = 0;
            leftLabel.text = "No\nSway";
            leftLabel.size = new Vector2(LEFTSPRITEWIDTH, HEIGHT);
            m_leftLabel = leftLabel;
            UILabel rightLabel = AddUIComponent<UILabel>();
            rightLabel.atlas = atlas;
            rightLabel.backgroundSprite = @"rightSprite";
            rightLabel.autoSize = false;
            rightLabel.autoHeight = false;
            rightLabel.textAlignment = UIHorizontalAlignment.Center;
            rightLabel.verticalAlignment = UIVerticalAlignment.Middle;
            rightLabel.textColor = new Color32(0xee, 0xee, 0xee, 0x50);
            rightLabel.textScale = 0.60f;
            rightLabel.dropShadowOffset = new Vector2(-1.2f, 1.2f);
            rightLabel.dropShadowColor = new Color32(0x00, 0x00, 0x00, 0x40);
            rightLabel.useDropShadow = true;
            rightLabel.padding = new RectOffset(0, 0, 3, 0);
            rightLabel.text = "Full\nSway";
            rightLabel.size = new Vector2(RIGHTSPRITEWIDTH, HEIGHT);
            m_rightLabel = rightLabel;
        }

        public override void Start() {
            base.Start();
        }

        public void Initialize(string text, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            UISlider slider = m_slider;
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += callback;
            UILabel label = m_sliderLabel;
            label.text = text;
            UIFontRenderer fontRenderer = label.ObtainRenderer();
            Vector2 size = fontRenderer.MeasureString(text);
            UIPanel sliderPanel = m_sliderPanel;
            size = new Vector2(EMath.Max(size.x + 20f, SLIDERWIDTH), HEIGHT);
            sliderPanel.size = size;
            label.size = size;
            slider.size = size;
            UILabel leftLabel = m_leftLabel;
            UILabel rightLabel = m_rightLabel;
            leftLabel.relativePosition = new Vector3(0f, 0f);
            sliderPanel.relativePosition = new Vector3(leftLabel.size.x - 1f, 0f);
            slider.relativePosition = new Vector3(0f, 0f);
            label.relativePosition = new Vector3((slider.size.x - label.size.x) / 2f, (slider.size.y - label.size.y) / 2f);
            rightLabel.relativePosition = new Vector3(sliderPanel.relativePosition.x + sliderPanel.size.x - 1f, 0f);
            this.size = new Vector2(leftLabel.size.x + sliderPanel.size.x + rightLabel.size.x, HEIGHT);

        }

        public override void OnDestroy() {
            base.OnDestroy();
            m_sliderLabel = null;
            m_slider = null;
            m_sliderPanel = null;
            m_leftLabel = null;
            m_rightLabel = null;
        }
    }
}
