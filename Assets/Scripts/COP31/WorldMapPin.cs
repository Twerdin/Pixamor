using UnityEngine;
using UnityEngine.UI;

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// Dünya haritası üzerindeki tıklanabilir kıta kriz pini.
    /// KURAL 2: 3D Işık (PointLight/SpotLight) KESİNLİKLE KULLANILMAZ; durumlar yalnızca
    /// UI elemanlarının ve materyalin kendi renk değerleriyle (Image.color / Material.color) gösterilir.
    /// </summary>
    public class WorldMapPin : Interactable
    {
        public bool IsActive { get; private set; }
        public bool IsDeferredDoubled { get; private set; }
        public int continentIndex = -1;

        public Renderer pinRenderer;
        public Image pinIconImage;
        public Text titleText;
        public GameObject uiRoot;

        Material pinMat;
        static readonly Color NormalColor = new Color(0.94f, 0.27f, 0.27f, 1f);    // #EF4444 Parlak Kızıl Kriz
        static readonly Color PulseColor = new Color(0.98f, 0.98f, 0.99f, 1f);     // #F8FAFC Beyaz Vurgu
        static readonly Color DoubledColor = new Color(0.96f, 0.62f, 0.04f, 1f);   // #F59E0B Kehribar (2x Kriz)

        void Awake()
        {
            RemoveAnyLegacy3DLights();
            EnsureMaterial();
        }

        void RemoveAnyLegacy3DLights()
        {
            var lights = GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null) Greybox.Kill(lights[i]);
            }
            if (transform.parent != null)
            {
                var parentLights = transform.parent.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < parentLights.Length; i++)
                {
                    if (parentLights[i] != null) Greybox.Kill(parentLights[i].gameObject);
                }
            }
        }

        void EnsureMaterial()
        {
            if (pinMat == null && pinRenderer != null)
            {
                pinMat = pinRenderer.material;
            }
        }

        public void SetupContinent(int contIdx, string contName)
        {
            continentIndex = contIdx;
            hoverLabel = "";
            IsActive = false;
            IsDeferredDoubled = false;
            RemoveAnyLegacy3DLights();
            if (uiRoot != null) uiRoot.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Activate(string contName, bool isDeferredDoubled = false)
        {
            IsActive = true;
            IsDeferredDoubled = isDeferredDoubled;
            gameObject.SetActive(true);
            if (uiRoot != null) uiRoot.SetActive(true);
            EnsureMaterial();

            if (titleText != null)
            {
                titleText.text = isDeferredDoubled
                    ? $"<color=#F59E0B><b>{contName} (2X)</b></color>"
                    : $"<b>{contName}</b>";
            }
            UpdateVisuals();
        }

        public void Deactivate()
        {
            IsActive = false;
            IsDeferredDoubled = false;
            if (uiRoot != null) uiRoot.SetActive(false);
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!IsActive) return;
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            EnsureMaterial();
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            Color targetA = IsDeferredDoubled ? DoubledColor : NormalColor;
            Color c = Color.Lerp(targetA, PulseColor, pulse);
            if (pinMat != null) pinMat.color = c;
            if (pinIconImage != null) pinIconImage.color = c;
        }

        public override void OnPressStart()
        {
            if (!isInteractable || !IsActive) return;

            if (continentIndex >= 0)
            {
                ContinentCrisisSpawner.Instance?.OnPinClicked(continentIndex);
                Sfx.Play(SfxType.Click);
            }
        }
    }
}
