using System;
using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>İki konumlu şalter. AÇIK = kol ileri (yukarı), KAPALI = kol geri (aşağı).</summary>
    public class ToggleSwitch : Interactable
    {
        public Transform lever;
        public float onAngle = 35f;
        public float offAngle = -35f;
        public Color onColor = new Color(0.2f, 0.9f, 0.3f);
        public Color offColor = new Color(0.9f, 0.2f, 0.2f);
        public string onText = "YUKARI";
        public string offText = "AŞAĞI";

        public bool IsOn { get; private set; }
        public event Action<ToggleSwitch> Toggled;

        Material tipMaterial;

        public override string HoverLabel => $"{hoverLabel}: {(IsOn ? onText : offText)}";

        public void Setup(Transform leverPivot, Renderer tip, bool startOn, string label)
        {
            lever = leverPivot;
            hoverLabel = label;
            tipMaterial = tip != null ? tip.sharedMaterial : null;
            IsOn = startOn;
            UpdateTip();
            if (lever != null) lever.localRotation = TargetRotation;
        }

        Quaternion TargetRotation => Quaternion.Euler(IsOn ? onAngle : offAngle, 0f, 0f);

        public override void OnPressStart()
        {
            if (!isInteractable) return;
            IsOn = !IsOn;
            UpdateTip();
            Sfx.Play(SfxType.Click);
            Toggled?.Invoke(this);
        }

        void Update()
        {
            if (lever != null)
                lever.localRotation = Quaternion.Slerp(lever.localRotation, TargetRotation, Time.deltaTime * 18f);
        }

        void UpdateTip() => Greybox.SetColor(tipMaterial, IsOn ? onColor : offColor);
    }
}
