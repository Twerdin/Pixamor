using System;
using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>Basılınca kapağı içeri gidip geri gelen (Lerp) fiziksel buton.</summary>
    public class PressButton : Interactable
    {
        public Transform cap;
        public Vector3 pressOffset = new Vector3(0f, -0.012f, 0f);
        public float returnSpeed = 5f;
        public bool playClick = true;

        public event Action Pressed;

        Vector3 capRest;
        float pressT;

        public void Setup(Transform capTransform, Vector3 offset, string label)
        {
            cap = capTransform;
            pressOffset = offset;
            hoverLabel = label;
            capRest = cap != null ? cap.localPosition : Vector3.zero;
        }

        public override void OnPressStart()
        {
            if (!isInteractable) return;
            pressT = 1f;
            if (cap != null) cap.localPosition = capRest + pressOffset;
            if (playClick) Sfx.Play(SfxType.Click);
            Pressed?.Invoke();
        }

        void Update()
        {
            if (cap == null || pressT <= 0f) return;
            pressT = Mathf.MoveTowards(pressT, 0f, Time.deltaTime * returnSpeed);
            cap.localPosition = capRest + pressOffset * pressT;
        }
    }
}
