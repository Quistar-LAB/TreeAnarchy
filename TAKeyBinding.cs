﻿using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using System;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    public class TAKeyBinding : UICustomControl {
        private SavedInputKey m_EditingBinding;
        private const string KeybindingConfig = "TreeAnarchyKeyBindSetting";

        private static readonly SavedInputKey treeSnapping = new SavedInputKey("toggleTreeSnapping", KeybindingConfig, SavedInputKey.Encode(KeyCode.S, false, false, true), true);
        private static readonly SavedInputKey lockForestry = new SavedInputKey("toggleForestry", KeybindingConfig, SavedInputKey.Encode(KeyCode.F, false, false, true), true);
        private static readonly SavedInputKey incrTreeVariation = new SavedInputKey("incrTreeVariation", KeybindingConfig, SavedInputKey.Encode(KeyCode.KeypadPlus, false, false, false), true);
        private static readonly SavedInputKey decrTreeVariation = new SavedInputKey("decrTreeVariation", KeybindingConfig, SavedInputKey.Encode(KeyCode.KeypadMinus, false, false, false), true);

        protected void Update() {
            Event e = Event.current;
            if (treeSnapping.IsPressed(e)) {
                if (UseTreeSnapping) UseTreeSnapping = false;
                else UseTreeSnapping = true;
            }
            if (lockForestry.IsPressed(e)) {
                if (LockForestry) LockForestry = false;
                else LockForestry = true;
            }
            if (incrTreeVariation.IsPressed(e)) {
                Debug.Log($"Tree Anarchy: Increment tree variation HotKey pressed");
            }
            if (decrTreeVariation.IsPressed(e)) {
                Debug.Log($"Tree Anarchy: Decrement tree variation HotKey pressed");
            }
        }

        protected void OnEnable() {
            LocaleManager.eventLocaleChanged += new LocaleManager.LocaleChangedHandler(OnLocaleChanged);
        }

        protected void OnDisable() {
            LocaleManager.eventLocaleChanged -= new LocaleManager.LocaleChangedHandler(OnLocaleChanged);
        }

        protected void Awake() {
            try {
                if (GameSettings.FindSettingsFileByName(KeybindingConfig) == null) {
                    GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = KeybindingConfig } });
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }

            AddKeymapping("Tree Snapping", treeSnapping);
            AddKeymapping("Lock Forestry", lockForestry);
            AddKeymapping("Increase Tree Size", incrTreeVariation);
            AddKeymapping("Decrease Tree Size", decrTreeVariation);
        }

        private int listCount = 0;
        public void AddKeymapping(string label, SavedInputKey savedInputKey) {
            UIPanel uIPanel = component.AttachUIComponent(UITemplateManager.GetAsGameObject("KeyBindingTemplate")) as UIPanel;
            if (listCount++ % 2 == 1) uIPanel.backgroundSprite = null;

            UILabel uILabel = uIPanel.Find<UILabel>("Name");
            UIButton uIButton = uIPanel.Find<UIButton>("Binding");
            uIButton.eventKeyDown += new KeyPressHandler(OnBindingKeyDown);
            uIButton.eventMouseDown += new MouseEventHandler(OnBindingMouseDown);

            uILabel.text = label;
            uIButton.text = savedInputKey.ToLocalizedString("KEYNAME");
            uIButton.objectUserData = savedInputKey;
        }

        private void OnLocaleChanged() {
            RefreshBindableInputs();
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
                uIButton.text = TAUI.Msg.PressAnyKey;
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

        private void RefreshBindableInputs() {
            foreach (UIComponent current in component.GetComponentsInChildren<UIComponent>()) {
                UITextComponent uITextComponent = current.Find<UITextComponent>("Binding");
                if (uITextComponent != null) {
                    SavedInputKey savedInputKey = uITextComponent.objectUserData as SavedInputKey;
                    if (savedInputKey != null) {
                        uITextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                    }
                }
                UILabel uILabel = current.Find<UILabel>("Name");
                if (uILabel != null) {
                    uILabel.text = Locale.Get("KEYMAPPING", uILabel.stringUserData);
                }
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
