using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal class TAKeyBinding : UICustomControl {
        private const string thisCategory = "TreeAnarchy";
        private SavedInputKey m_EditingBinding;

        [RebindableKey("TreeAnarchy")]
        private static readonly string toggleTreeSnapping = "toggleTreeSnapping";
        [RebindableKey("TreeAnarchy")]
        private static readonly string toggleLockForestry = "toggleLockForestry";
#if ENABLETREEANARCHY
        [RebindableKey("TreeAnarchy")]
        private static readonly string toggleTreeAnarchy = "toggleTreeAnarchy";
#endif
        [RebindableKey("TreeAnarchy")]
        private static readonly string groupTrees = "groupTrees";
        [RebindableKey("TreeAnarchy")]
        private static readonly string ungroupTrees = "ungroupTrees";
        [RebindableKey("TreeAnarchy")]
        private static readonly string terrainConformTrees = "terrainConformTrees";
        [RebindableKey("TreeAnarchy")]
        private static readonly string incrementTreeSize = "incrTreeVariation";
        [RebindableKey("TreeAnarchy")]
        private static readonly string decrementTreeSize = "decrTreeVariation";

        private static readonly InputKey defaultToggleTreeSnappingKey = SavedInputKey.Encode(KeyCode.S, false, false, true);
        private static readonly InputKey defaultToggleLockForestryKey = SavedInputKey.Encode(KeyCode.F, false, false, true);
#if ENABLETREEANARCHY
        private static readonly InputKey defaultToggleTreeAnarchyKey = SavedInputKey.Encode(KeyCode.A, false, false, true);
#endif
        private static readonly InputKey defaultGroupTreeKey = SavedInputKey.Encode(KeyCode.G, true, false, false);
        private static readonly InputKey defaultUngroupTreeKey = SavedInputKey.Encode(KeyCode.U, true, false, false);
        private static readonly InputKey defaultTerrainConformTrees = SavedInputKey.Encode(KeyCode.T, false, false, true);
        private static readonly InputKey defaultIncrementTreeSizeKey = SavedInputKey.Encode(KeyCode.Period, false, false, false);
        private static readonly InputKey defaultDecrementTreeSizeKey = SavedInputKey.Encode(KeyCode.Comma, false, false, false);

        private static readonly SavedInputKey m_treeSnapping = new SavedInputKey(toggleTreeSnapping, KeybindingConfigFile, defaultToggleTreeSnappingKey, true);
        private static readonly SavedInputKey m_lockForestry = new SavedInputKey(toggleLockForestry, KeybindingConfigFile, defaultToggleLockForestryKey, true);
#if ENABLETREEANARCHY
        private static readonly SavedInputKey m_treeAnarchy = new SavedInputKey(toggleTreeAnarchy, KeybindingConfigFile, defaultToggleTreeAnarchyKey, true);
#endif
        private static readonly SavedInputKey m_groupTrees = new SavedInputKey(groupTrees, KeybindingConfigFile, defaultGroupTreeKey, true);
        private static readonly SavedInputKey m_ungroupTrees = new SavedInputKey(ungroupTrees, KeybindingConfigFile, defaultUngroupTreeKey, true);
        private static readonly SavedInputKey m_terrainConformTrees = new SavedInputKey(terrainConformTrees, KeybindingConfigFile, defaultTerrainConformTrees, true);
        private static readonly SavedInputKey m_incrTreeVariation = new SavedInputKey(incrementTreeSize, KeybindingConfigFile, defaultIncrementTreeSizeKey, true);
        private static readonly SavedInputKey m_decrTreeVariation = new SavedInputKey(decrementTreeSize, KeybindingConfigFile, defaultDecrementTreeSizeKey, true);

        protected void Update() {
            if (!UIView.HasModalInput() && !UIView.HasInputFocus()) {
                Event e = Event.current;
                if (m_treeSnapping.IsPressed(e)) {
                    bool state = UseTreeSnapping = !UseTreeSnapping;
                    if (TAPatcher.isMoveItInstalled && TAPatcher.MoveItUseTreeSnap != null) {
                        TAPatcher.MoveItUseTreeSnap.SetValue(null, state);
                    }
                    TAOptionPanel.SetTreeSnapState(state);
                    TAIndicator.TreeSnapIndicator.SetState(state);
                    SaveSettings();
                } else if (m_lockForestry.IsPressed(e)) {
                    bool state = UseLockForestry = !UseLockForestry;
                    TAOptionPanel.SetLockForestryState(state);
                    TAIndicator.LockForestryIndicator.SetState(state);
                    SaveSettings();
                }
#if ENABLETREEANARCHY
                else if (m_treeAnarchy.IsPressed(e)) {
                    bool state = UseTreeAnarchy = !UseTreeAnarchy;
                    TAOptionPanel.SetTreeAnarchyState(state);
                    TAIndicator.TreeAnarchyIndicator.SetState(state);
                    SaveSettings();
                }
#endif
                else if (m_groupTrees.IsPressed(e)) {
                    SingletonLite<TAManager>.instance.GroupTrees();
                } else if (m_ungroupTrees.IsPressed(e)) {
                    SingletonLite<TAManager>.instance.UngroupTrees();
                } else if (m_terrainConformTrees.IsPressed(e)) {
                    SingletonLite<TAManager>.instance.TerrainConformTrees();
                } else if (IsCustomPressed(m_incrTreeVariation, e)) {
                    SingletonLite<TAManager>.instance.IncrementTreeSize();
                } else if (IsCustomPressed(m_decrTreeVariation, e)) {
                    SingletonLite<TAManager>.instance.DecrementTreeSize();
                }
            }
        }

        protected void Awake() {
            UILabel desc = component.AddUIComponent<UILabel>();
            desc.padding.top = 10;
            desc.width = component.width - 50;
            desc.autoHeight = true;
            desc.wordWrap = true;
            desc.textScale = TAOptionPanel.SmallFontScale;
            desc.text = SingletonLite<TALocale>.instance.GetLocale("KeyBindDescription");
            AddKeymapping("TreeSnap", m_treeSnapping);
            AddKeymapping("LockForestry", m_lockForestry);
#if ENABLETREEANARCHY
            AddKeymapping("TreeAnarchy", m_treeAnarchy);
#endif
            AddKeymapping("TerrainConformTrees", m_terrainConformTrees);
            AddKeymapping("GroupTrees", m_groupTrees);
            AddKeymapping("UngroupTrees", m_ungroupTrees);
            AddKeymapping("IncreaseTreeSize", m_incrTreeVariation);
            AddKeymapping("DecreaseTreeSize", m_decrTreeVariation);
        }

        private bool IsCustomPressed(SavedInputKey inputKey, Event e) {
            if (e.type != EventType.KeyDown) return false;
            return Input.GetKey(inputKey.Key) &&
                (e.modifiers & EventModifiers.Control) == EventModifiers.Control == inputKey.Control &&
                (e.modifiers & EventModifiers.Shift) == EventModifiers.Shift == inputKey.Shift &&
                (e.modifiers & EventModifiers.Alt) == EventModifiers.Alt == inputKey.Alt;
        }

        private int listCount = 0;
        private void AddKeymapping(string key, SavedInputKey savedInputKey) {
            TALocale locale = SingletonLite<TALocale>.instance;
            UIPanel uIPanel = component.AttachUIComponent(UITemplateManager.GetAsGameObject("KeyBindingTemplate")) as UIPanel;
            if (listCount++ % 2 == 1) uIPanel.backgroundSprite = null;

            UILabel uILabel = uIPanel.Find<UILabel>("Name");
            UIButton uIButton = uIPanel.Find<UIButton>("Binding");

            uIButton.eventKeyDown += new KeyPressHandler(OnBindingKeyDown);
            uIButton.eventMouseDown += new MouseEventHandler(OnBindingMouseDown);
            uILabel.objectUserData = locale;
            uILabel.stringUserData = key;
            uILabel.text = locale.GetLocale(key);
            uIButton.text = savedInputKey.ToLocalizedString("KEYNAME");
            uIButton.objectUserData = savedInputKey;
            uIButton.stringUserData = thisCategory; // used for localization TODO:
        }

        private void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p) {
            if (m_EditingBinding != null && !IsModifierKey(p.keycode)) {
                p.Use();
                UIView.PopModal();
                KeyCode keycode = p.keycode;
                InputKey inputKey = (p.keycode == KeyCode.Escape) ? m_EditingBinding.value : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
                if (p.keycode == KeyCode.Backspace) {
                    inputKey = SavedInputKey.Empty;
                }
                m_EditingBinding.value = inputKey;
                UITextComponent uITextComponent = p.source as UITextComponent;
                uITextComponent.text = m_EditingBinding.ToLocalizedString("KEYNAME");
                m_EditingBinding = null;
            }
        }

        private void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p) {
            if (m_EditingBinding == null) {
                p.Use();
                m_EditingBinding = (SavedInputKey)p.source.objectUserData;
                UIButton uIButton = p.source as UIButton;
                uIButton.buttonsMask = (UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3);
                uIButton.text = SingletonLite<TALocale>.instance.GetLocale("PressAnyKey");
                p.source.Focus();
                UIView.PushModal(p.source);
            } else if (!IsUnbindableMouseButton(p.buttons)) {
                p.Use();
                UIView.PopModal();
                InputKey inputKey = SavedInputKey.Encode(ButtonToKeycode(p.buttons), IsControlDown(), IsShiftDown(), IsAltDown());
                m_EditingBinding.value = inputKey;
                UIButton uIButton2 = p.source as UIButton;
                uIButton2.text = m_EditingBinding.ToLocalizedString("KEYNAME");
                uIButton2.buttonsMask = UIMouseButton.Left;
                m_EditingBinding = null;
            }
        }

        private KeyCode ButtonToKeycode(UIMouseButton button) => button switch {
            UIMouseButton.Left => KeyCode.Mouse0,
            UIMouseButton.Right => KeyCode.Mouse1,
            UIMouseButton.Middle => KeyCode.Mouse2,
            UIMouseButton.Special0 => KeyCode.Mouse3,
            UIMouseButton.Special1 => KeyCode.Mouse4,
            UIMouseButton.Special2 => KeyCode.Mouse5,
            UIMouseButton.Special3 => KeyCode.Mouse6,
            _ => KeyCode.None,
        };

        private bool IsUnbindableMouseButton(UIMouseButton code) => (code == UIMouseButton.Left || code == UIMouseButton.Right);
        private bool IsModifierKey(KeyCode code) => code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift ||
                                                    code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        private bool IsControlDown() => (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        private bool IsShiftDown() => (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        private bool IsAltDown() => (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
    }
}
