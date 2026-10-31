using System;
using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>Zaman Çizelgesi kolu: çekilince öne gelir, tepe noktasında Pulled tetiklenir, geri döner.</summary>
    public class TimeLever : Interactable
    {
        public Transform arm;
        public float restAngle = 30f;
        public float pulledAngle = -45f;
        public float duration = 0.6f;

        /// <summary>false dönerse kol çekilmez.</summary>
        public Func<bool> CanPull;
        public event Action Pulled;

        float anim = -1f;
        bool fired;

        public void Setup(Transform armPivot, string label)
        {
            arm = armPivot;
            hoverLabel = label;
            if (arm != null) arm.localRotation = Quaternion.Euler(restAngle, 0f, 0f);
        }

        public override void OnPressStart()
        {
            if (!isInteractable || anim >= 0f) return;
            if (CanPull != null && !CanPull())
            {
                Sfx.Play(SfxType.Fail, 0.6f);
                return;
            }
            anim = 0f;
            fired = false;
            Sfx.Play(SfxType.Lever);
        }

        void Update()
        {
            if (anim < 0f || arm == null) return;
            anim += Time.deltaTime / duration;
            float k = Mathf.Sin(Mathf.Clamp01(anim) * Mathf.PI);
            arm.localRotation = Quaternion.Euler(Mathf.Lerp(restAngle, pulledAngle, k), 0f, 0f);

            if (!fired && anim >= 0.5f)
            {
                fired = true;
                Pulled?.Invoke();
            }

            if (anim >= 1f)
            {
                anim = -1f;
                arm.localRotation = Quaternion.Euler(restAngle, 0f, 0f);
            }
        }
    }
}
