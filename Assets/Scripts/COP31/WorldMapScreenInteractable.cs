using System;
using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>
    /// Monitör ekranı yüzeyindeki 3D BoxCollider'a takılan etkileşim bileşeni.
    /// Oyuncunun birinci şahıs nişangahı veya serbest faresi ile monitöre tıkladığı
    /// noktanın UV koordinatını hesaplayıp WorldMapMonitorController ve ContinentCrisisSpawner'a iletir.
    /// </summary>
    public class WorldMapScreenInteractable : Interactable
    {
        public Vector2 screenPhysicalSize = new Vector2(1.56f, 0.86f);
        public Vector2 mapPixelOffset = new Vector2(8f, 56f); // Header ve kenar payı
        public Vector2 mapPixelSize = new Vector2(608f, 256f);
        public Vector2 canvasTotalSize = new Vector2(624f, 344f);

        Camera mainCam;

        public void SetHoverLabel(string label)
        {
            hoverLabel = label;
        }

        public override void OnPressStart()
        {
            base.OnPressStart();

            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            // Oyuncunun tıkladığı veya baktığı ışını çıkar
            Ray ray = PlayerInteractor.IsCursorLocked
                ? mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : mainCam.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                // Vurulan noktayı ekranın yerel koordinat sistemine dönüştür
                Vector3 localHit = transform.InverseTransformPoint(hit.point);

                // Ekran üzerindeki normalize UV (0..1)
                float screenU = Mathf.Clamp01((localHit.x / screenPhysicalSize.x) + 0.5f);
                float screenV = Mathf.Clamp01((localHit.y / screenPhysicalSize.y) + 0.5f);

                // Canvas piksel koordinatına dönüştür (0..624, 0..344)
                float canvasPxX = screenU * canvasTotalSize.x;
                float canvasPxY = screenV * canvasTotalSize.y;

                // Harita alanına düşüp düşmediğini kontrol et (x: 8..616, y: 56..312)
                if (canvasPxX >= mapPixelOffset.x && canvasPxX <= (mapPixelOffset.x + mapPixelSize.x) &&
                    canvasPxY >= mapPixelOffset.y && canvasPxY <= (mapPixelOffset.y + mapPixelSize.y))
                {
                    float mapU = (canvasPxX - mapPixelOffset.x) / mapPixelSize.x;
                    float mapV = (canvasPxY - mapPixelOffset.y) / mapPixelSize.y;

                    ContinentCrisisSpawner.Instance?.OnScreenClicked(new Vector2(mapU, mapV));
                }
            }
        }
    }
}
