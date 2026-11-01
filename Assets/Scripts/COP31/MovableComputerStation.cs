using UnityEngine;

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// Sahne içinde (Scene View / Edit Mode) istenilen yere taşınabilen ve döndürülebilen
    /// bağımsız Bilgisayar İstasyonu (Computer_Workstation) kök bileşeni.
    /// Altındaki 3D Monitör Modeli, World-Space Monitor_OS_Canvas, Etkileşim Collider'ı
    /// ve CameraFocusPoint birlikte hareket eder.
    /// </summary>
    [ExecuteAlways]
    public class MovableComputerStation : Interactable
    {
        public static MovableComputerStation Instance { get; private set; }

        [Header("İstasyon Alt Bileşenleri (Birlikte Taşınır)")]
        public Transform monitor3DModel;
        public Transform monitorHead;
        public Canvas monitorOSCanvas;
        public Transform cameraFocusPoint;
        public BoxCollider interactionTrigger;

        void OnEnable()
        {
            Instance = this;
            hoverLabel = "";
        }

        void Awake()
        {
            Instance = this;
            hoverLabel = "";
        }

        public override void OnPressStart()
        {
            if (!isInteractable) return;
            if (PlayerInteractor.Instance != null && !PlayerInteractor.Instance.isFullscreen)
            {
                PlayerInteractor.Instance.EnterComputerMode();
            }
        }

        void OnDrawGizmos()
        {
            if (cameraFocusPoint != null && monitorHead != null)
            {
                Gizmos.color = new Color(0.06f, 0.73f, 0.51f, 0.75f);
                Gizmos.DrawWireSphere(cameraFocusPoint.position, 0.045f);
                Gizmos.DrawLine(cameraFocusPoint.position, monitorHead.position);
            }
        }
    }
}
