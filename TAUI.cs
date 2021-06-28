using ColossalFramework.UI;
using System;
using UnityEngine;
using static TreeAnarchy.TAMod;

namespace TreeAnarchy {
    internal static class TAUI {
        private const float MaxScaleFactor = 6.0f;
        private const float MinScaleFactor = 1.5f;

        private struct Msg {
            internal static string MaxTreeLimit = "Maximum Tree Limit:  {0} trees";
            internal static string WindEffect = @"Enable Tree Effect on Wind [Original game default is enabled]";
            internal static string WindEffectLabel = @"This option affects the normal game behavior of tree's height diluting and weakening the wind on the map.";
            internal static string TreeSnap = @"Tree Snapping/Anarchy Support";
            internal static string TreeSnapLabel = @"This option allows free movement of trees, and allows trees to be snapped to buildings and props";
            internal static string RandomTreeRotation = @"Random Tree Rotation";
            internal static string TreeSwayFactor = @"Tree Sway Factor";
            internal static string LockForestry = @"Lock Forestry";
            internal static string SwayLabel = @"Useful eyecandy and tools. Lock forestry prevent trees from creating forestry resources and removing fertile land";
            internal static string Important = @"Important! Must be set before starting/loading a game";
            internal static string Experimental = @"Experimental Tree Rendering Acceleration";
            internal static string ExperimentalLabel = @"Experimental acceleration is achieved by disabling (assumed) redundant checks and " +
                                                       @"unrolling loops within potential bottelnecks when rendering trees. This is considered " +
                                                       @"unsafe, thus use at your own risk";
            internal static string EnableProfiling = @"Enable Profiling";
            internal static string EnableProfilingLabel = @"Will profile TreeManager::BeginRenderingImpl and TreeManager::EndRenderingImpl, saving the result in TAProfile.txt";
        }

        private static UILabel MaxTreeLabel = default;
        private static UILabel WindEffectLabel = default;
        private static UILabel TreeSnapLabel = default;
        private static UICheckBox WindEffect = default;
        private static UICheckBox TreeSnap = default;
        private static UICheckBox TreeRotation = default;
        private static UICheckBox LockForestry = default;
        private static UICheckBox Experimental = default;
        private static UICheckBox EnableProfiling = default;
        private static UISlider TreeSwayFactor = default;
        private static UISlider TreeScaleFactor = default;

        private static void OnTreeWindCheckChanged(UIComponent component, bool isChecked) {
            TreeEffectOnWind = isChecked;
            SaveSettings();
        }
        private static void OnTreeSnapCheckChanged(UIComponent component, bool isChecked) {
            UseTreeSnapping = isChecked;
            SaveSettings();
        }
        private static void OnTreeRotationCheckChanged(UIComponent component, bool isChecked) {
            RandomTreeRotation = isChecked;
            SaveSettings();
        }
        private static void OnTreeSwayFactorChanged(UIComponent component, float val) {
            TAMod.TreeSwayFactor = val;
            if(IsInGame) Patches.TreeMovement.UpdateTreeSway();
            SaveSettings();
        }
        private static void OnLockForestryCheckChanged(UIComponent component, bool isChecked) {
            TAMod.LockForestry = isChecked;
            SaveSettings();
        }
        private static void OnTreeScaleFactorChanged(UIComponent component, float val) {
            TAMod.TreeScaleFactor = val;
            MaxTreeLabel.text = String.Format(Msg.MaxTreeLimit, MaxTreeLimit);
            Patches.TreeLimit.InjectResize();
            SaveSettings();
        }
        private static void OnExperimentalCheckChanged(UIComponent component, bool isChecked) {
            UseExperimental = isChecked;
            SaveSettings();
        }

        private static void OnEnableProfilingCheckChanged(UIComponent component, bool isChecked) {
            TAMod.EnableProfiling = isChecked;
            SaveSettings();
        }

        internal static void ShowStandardOptions(UIHelper option) {
            UILabel swayLabel = default;
            UIPanel panel = (UIPanel)option.self;
            AddCheckBox(ref panel, ref WindEffect, Msg.WindEffect, TAMod.TreeEffectOnWind, OnTreeWindCheckChanged);
            AddLabel(ref panel, WindEffect, ref WindEffectLabel, Msg.WindEffectLabel);
            option.AddSpace((int)WindEffectLabel.height);
            AddCheckBox(ref panel, ref TreeSnap, Msg.TreeSnap, TAMod.UseTreeSnapping, OnTreeSnapCheckChanged);
            AddLabel(ref panel, TreeSnap, ref TreeSnapLabel, Msg.TreeSnapLabel);
            option.AddSpace((int)TreeSnapLabel.height);
            AddCheckBox(ref panel, ref TreeRotation, Msg.RandomTreeRotation, TAMod.RandomTreeRotation, OnTreeRotationCheckChanged);
            TreeRotation.width = 300;
            UIPanel SwayPanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            SwayPanel.Find<UILabel>("Label").text = Msg.TreeSwayFactor;
            AddSlider(ref SwayPanel, ref TreeSwayFactor, 0f, 1f, 0.1f, TAMod.TreeSwayFactor, OnTreeSwayFactorChanged);
            SwayPanel.AlignTo(TreeRotation, UIAlignAnchor.TopRight);
            SwayPanel.relativePosition = new Vector3(320, -5);
            AddCheckBox(ref panel, ref LockForestry, Msg.LockForestry, TAMod.LockForestry, OnLockForestryCheckChanged);
            LockForestry.width = 300;
            AddLabel(ref panel, LockForestry, ref swayLabel, Msg.SwayLabel);
            option.AddSpace((int)SwayPanel.height);
            SwayPanel.zOrder = TreeRotation.zOrder - 1;
        }

        internal static void ShowTreeLimitOption(UIHelper option) {
            UILabel ImportantMsg = default;
            UILabel ExperimentalLabel = default;
            UILabel EnableProfilingLabel = default;
            UIPanel panel = (UIPanel)option.self;
            UIPanel ScalePanel = (UIPanel)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsSliderTemplate"));
            MaxTreeLabel = ScalePanel.Find<UILabel>("Label");
            MaxTreeLabel.text = String.Format(Msg.MaxTreeLimit, TAMod.MaxTreeLimit);
            MaxTreeLabel.width = panel.width - 100;
            MaxTreeLabel.autoSize = false;
            MaxTreeLabel.autoHeight = true;
            AddSlider(ref ScalePanel, ref TreeScaleFactor, MinScaleFactor, MaxScaleFactor, 0.5f, TAMod.TreeScaleFactor, OnTreeScaleFactorChanged);
            TreeScaleFactor.width = panel.width - 150;
            AddLabel(ref panel, ScalePanel, ref ImportantMsg, Msg.Important);
            option.AddSpace((int)ImportantMsg.height);
            AddCheckBox(ref panel, ref Experimental, Msg.Experimental, TAMod.UseExperimental, OnExperimentalCheckChanged);
            AddLabel(ref panel, Experimental, ref ExperimentalLabel, Msg.ExperimentalLabel);
            option.AddSpace((int)ExperimentalLabel.height);
            AddCheckBox(ref panel, ref EnableProfiling, Msg.EnableProfiling, TAMod.EnableProfiling, OnEnableProfilingCheckChanged);
            AddLabel(ref panel, EnableProfiling, ref EnableProfilingLabel, Msg.EnableProfilingLabel);
            option.AddSpace((int)EnableProfilingLabel.height);
        }

        internal static void UpdateState(bool isInGame) {
            if (isInGame) {
                WindEffect.Disable();
                TreeScaleFactor.Disable();
                Experimental.Disable();
                EnableProfiling.Disable();
                return;
            }
            EnableProfiling.Enable();
            Experimental.Enable();
            WindEffect.Enable();
            TreeScaleFactor.Enable();
        }

        private static void AddCheckBox(ref UIPanel panel, ref UICheckBox cb, string name, bool defaultVal, PropertyChangedEventHandler<bool> callback) {
            cb = (UICheckBox)panel.AttachUIComponent(UITemplateManager.GetAsGameObject("OptionsCheckBoxTemplate"));
            cb.text = name;
            cb.isChecked = defaultVal;
            cb.autoSize = false;
            cb.eventCheckChanged += new PropertyChangedEventHandler<bool>(callback);
        }
        private static void AddLabel(ref UIPanel panel, UIComponent alignTo, ref UILabel label, string text) {
            label = panel.AddUIComponent<UILabel>();
            label.AlignTo(alignTo, UIAlignAnchor.BottomLeft);
            label.width = panel.width - 80;
            label.wordWrap = true;
            label.autoHeight = true;
            label.text = text;
            label.relativePosition = new Vector3(25, 25);
            panel.height += label.height;
        }
        private static void AddSlider(ref UIPanel panel, ref UISlider slider, float min, float max, float step, float defaultVal, PropertyChangedEventHandler<float> callback) {
            slider = panel.Find<UISlider>("Slider");
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = defaultVal;
            slider.eventValueChanged += new PropertyChangedEventHandler<float>(callback);
        }


    }
}
