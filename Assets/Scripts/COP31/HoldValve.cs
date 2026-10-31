using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>Basılı tutuldukça dönen vana. Basınç mantığı DeskPuzzleController'dadır.</summary>
    public class HoldValve : Interactable
    {
        public Transform wheel;
        public float spinSpeed = 220f;

        public bool IsHeld { get; private set; }

        public void Setup(Transform wheelPivot, string label)
        {
            wheel = wheelPivot;
            hoverLabel = label;
        }

        public override void OnPressStart()
        {
            if (!isInteractable) return;
            IsHeld = true;
        }

        public override void OnRelease() => IsHeld = false;

        void OnDisable() => IsHeld = false;

        void Update()
        {
            if (IsHeld && wheel != null) wheel.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }
}
