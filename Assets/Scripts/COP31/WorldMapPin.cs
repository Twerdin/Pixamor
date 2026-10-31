using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// Dünya haritası üzerindeki tıklanabilir kriz pini.
    /// Kriz patlak verdiğinde kırmızı yanıp söner, uyarı ışığı saçar ve geri sayımı gösterir.
    /// </summary>
    public class WorldMapPin : Interactable
    {
        public CrisisEvent EventData { get; private set; }
        public float TimeRemaining { get; private set; }
        public float MaxDuration { get; private set; } = 40f;
        public bool IsActive { get; private set; }
        public int continentIndex = -1;

        public Renderer pinRenderer;
        public Light pinLight;
        public Text titleText;
        public Text timerText;
        public UIBar timeBar;
        public GameObject uiRoot;

        Material pinMat;
        static readonly Color NormalColor = new Color(0.95f, 0.2f, 0.15f);
        static readonly Color PulseColor = new Color(1f, 0.8f, 0.25f);

        void Awake()
        {
            EnsureMaterial();
        }

        void EnsureMaterial()
        {
            if (pinMat == null && pinRenderer != null)
            {
                pinMat = pinRenderer.material;
            }
        }

        public void Setup(CrisisEvent data)
        {
            EventData = data;
            hoverLabel = $"KRİZ: {data.title} ({data.locationName})";
            IsActive = false;
            if (pinLight != null) pinLight.enabled = false;
            if (uiRoot != null) uiRoot.SetActive(false);
            gameObject.SetActive(false);
        }

        public void SetupContinent(int contIdx, string contName)
        {
            continentIndex = contIdx;
            hoverLabel = $"KRİZ BÖLGESİ: {contName}";
            IsActive = false;
            if (pinLight != null) pinLight.enabled = false;
            if (uiRoot != null) uiRoot.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Activate(float duration)
        {
            TimeRemaining = duration;
            MaxDuration = duration;
            IsActive = true;
            gameObject.SetActive(true);
            if (pinLight != null) pinLight.enabled = true;
            if (uiRoot != null) uiRoot.SetActive(true);
            EnsureMaterial();

            if (titleText != null && EventData != null) titleText.text = EventData.title;
            UpdateVisuals();
        }

        public void Activate(string title, float duration)
        {
            TimeRemaining = duration;
            MaxDuration = duration;
            IsActive = true;
            gameObject.SetActive(true);
            if (pinLight != null) pinLight.enabled = true;
            if (uiRoot != null) uiRoot.SetActive(true);
            EnsureMaterial();

            if (titleText != null) titleText.text = title;
            UpdateVisuals();
        }

        public void UpdateTimer(float remaining, float maxDur)
        {
            if (!IsActive) return;
            TimeRemaining = Mathf.Max(0f, remaining);
            MaxDuration = maxDur;
            UpdateVisuals();
        }

        public void Deactivate()
        {
            IsActive = false;
            if (pinLight != null) pinLight.enabled = false;
            if (uiRoot != null) uiRoot.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (!IsActive) return;
            TimeRemaining = Mathf.Max(0f, TimeRemaining - dt);
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            EnsureMaterial();
            float pulse = (Mathf.Sin(Time.time * 9f) + 1f) * 0.5f;
            Color c = Color.Lerp(NormalColor, PulseColor, pulse);
            if (pinMat != null) pinMat.color = c;
            if (pinLight != null) pinLight.color = c;

            if (timerText != null)
                timerText.text = $"<color=#FF3333><b>{TimeRemaining:0.0}s</b></color>";

            if (timeBar != null && MaxDuration > 0f)
            {
                float t01 = Mathf.Clamp01(TimeRemaining / MaxDuration);
                Color barCol = t01 > 0.4f ? new Color(0.9f, 0.65f, 0.2f) : new Color(1f, 0.2f, 0.2f);
                timeBar.Set(t01, barCol);
            }
        }

        public override void OnPressStart()
        {
            if (!isInteractable || !IsActive) return;

            if (continentIndex >= 0)
            {
                ContinentCrisisSpawner.Instance?.OnPinClicked(continentIndex);
                Sfx.Play(SfxType.Click);
                return;
            }

            if (EventData != null)
            {
                ClimateOperatorManager.Instance?.OpenEventCard(EventData);
                Sfx.Play(SfxType.Click);
            }
        }
    }
}
