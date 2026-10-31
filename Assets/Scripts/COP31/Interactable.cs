using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>
    /// Raycast ile tıklanabilen tüm 3D objelerin temel sınıfı.
    /// PlayerInteractor; collider'ın parent zincirinde ilk bulduğu Interactable'ı kullanır.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [SerializeField] protected string hoverLabel = "";
        public bool isInteractable = true;

        public virtual string HoverLabel => hoverLabel;

        public virtual void OnPressStart() { }
        public virtual void OnHold(float deltaTime) { }
        public virtual void OnRelease() { }
    }
}
