using ColossalFramework;
using ColossalFramework.UI;

namespace TreeAnarchy.UI
{
    class CustomSlider : UISlider
    {
        public override void OnDisable()
        {
            this.eventClick -= CustomSlider_eventClick;
        }

        public override void OnEnable()
        {
            this.eventClick += CustomSlider_eventClick;
        }

        private void CustomSlider_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            
        }
    }
}
