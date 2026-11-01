using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// Üst Durum Çubuğu, "E" Tuşu Etkileşim İkonu (Key Prompt Badge), Siyah Ekran ("GÜN 2") Geçişi
    /// ve Dönem Sonu Raporu Yöneticisi:
    /// - Uzun etkileşim yazısı kaldırılmıştır; sadece monitöre bakarken ekran ortasında 'E' ikonu (icon_key_e.png) belirir.
    /// - GÜN 1 göstergesi tıklanamaz; gün yalnızca tüm görevler bittiğinde Siyah Ekran (#000000) Fade-In / 2.5sn / Fade-Out ile ilerler.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        public static HudController Instance { get; private set; }

        [Header("Üst Durum Çubuğu (3 Gösterge + 15 Sn Bütçe Sayacı + Gün Göstergesi + Masadan Kalk)")]
        public UIBar climateBar;
        public Text climateText;

        public UIBar mentalBar;
        public Text mentalText;

        public Text moneyText;
        public Text authorityText;
        public UIBar fundingTimerBar;
        public Text floatingBudgetText;

        public Text dayText;
        public PressButton nextDayButton;
        public PressButton fullscreenButton;

        [Header("Serbest Mod Etkileşim: Sadece 'E' İkonu (icon_key_e.png)")]
        public PlayerInteractor interactor;
        public GameObject keyPromptBadgeRoot;
        public Text hoverLabelText;
        public Image crosshairImage;

        [Header("KURAL 5: Siyah Ekran ('GÜN 2') Geçişi (CanvasGroup Fade-In / 2.5s / Fade-Out)")]
        public CanvasGroup dayTransitionCanvasGroup;
        public Text dayTransitionTitleText;
        public CanvasGroup monitorDayTransitionGroup;
        public Text monitorDayTransitionTitleText;

        [Header("Modern Dönem Sonu Raporu (2x3 Stat Widgets + Tıklanabilir Buton)")]
        public GameObject endScreenRoot;
        public Text endTitleText;
        public Text endSubtitleText;
        public Text[] endStatValueTexts = new Text[6];
        public Button restartButton;
        public PressButton restart3DButton;

        float updateTimer;
        float floatingPopupTimer;
        const float FloatingPopupDuration = 2.0f;
        Vector2 floatingPopupStartPos = new Vector2(305f, -44f);
        Color floatingPopupBaseColor = new Color(0.06f, 0.73f, 0.51f, 1f); // #10B981
        Coroutine activeDayTransitionCoroutine;

        void OnEnable()
        {
            Instance = this;
        }

        void Awake()
        {
            Instance = this;
            EnsureUIBindings();
        }

        void Start()
        {
            EnsureUIBindings();

            if (fullscreenButton != null)
            {
                fullscreenButton.Pressed += () =>
                {
                    if (interactor != null) interactor.ToggleFullscreen();
                    else PlayerInteractor.Instance?.ToggleFullscreen();
                };
            }

            // KURAL 5: GÜN 1 kutusuna tıklayarak gün atlama özelliği TAMAMEN KALDIRILDI.
            if (nextDayButton != null)
            {
                nextDayButton.isInteractable = false;
                nextDayButton.gameObject.SetActive(false);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartGamePeriod);
            }

            if (restart3DButton != null)
            {
                restart3DButton.Pressed += RestartGamePeriod;
            }

            if (floatingBudgetText != null)
            {
                floatingPopupStartPos = floatingBudgetText.rectTransform.anchoredPosition;
                floatingBudgetText.gameObject.SetActive(false);
            }

            if (hoverLabelText != null)
            {
                hoverLabelText.text = "";
                hoverLabelText.gameObject.SetActive(false);
            }

            if (keyPromptBadgeRoot != null)
            {
                keyPromptBadgeRoot.SetActive(false);
            }

            if (dayTransitionCanvasGroup != null)
            {
                dayTransitionCanvasGroup.alpha = 0f;
                dayTransitionCanvasGroup.blocksRaycasts = false;
                dayTransitionCanvasGroup.interactable = false;
                dayTransitionCanvasGroup.gameObject.SetActive(false);
            }

            if (monitorDayTransitionGroup != null)
            {
                monitorDayTransitionGroup.alpha = 0f;
                monitorDayTransitionGroup.blocksRaycasts = false;
                monitorDayTransitionGroup.interactable = false;
                monitorDayTransitionGroup.gameObject.SetActive(false);
            }
        }

        public void EnsureUIBindings()
        {
            if (interactor == null) interactor = PlayerInteractor.Instance;

            var hudCanvas = GameObject.Find("Player_HUD_Canvas");
            if (hudCanvas != null)
            {
                if (endScreenRoot == null)
                {
                    var endTr = hudCanvas.transform.Find("EndScreen");
                    if (endTr != null) endScreenRoot = endTr.gameObject;
                }

                if (keyPromptBadgeRoot == null)
                {
                    var kpTr = hudCanvas.transform.Find("KeyPrompt_E_Badge");
                    if (kpTr != null) keyPromptBadgeRoot = kpTr.gameObject;
                }

                if (dayTransitionCanvasGroup == null)
                {
                    var dtTr = hudCanvas.transform.Find("DayTransition_BlackScreen");
                    if (dtTr != null)
                    {
                        dayTransitionCanvasGroup = dtTr.GetComponent<CanvasGroup>();
                        var titleTr = dtTr.Find("DayTransition_Title");
                        if (titleTr != null) dayTransitionTitleText = titleTr.GetComponent<Text>();
                    }
                }
            }

            if (floatingBudgetText == null)
            {
                var fp = GameObject.Find("Floating_Budget_Popup");
                if (fp != null) floatingBudgetText = fp.GetComponent<Text>();
            }
        }

        /// <summary>
        /// KURAL 5: O günün tüm görevleri bittiği anda ekran yumuşak bir geçişle (CanvasGroup Fade-In)
        /// tamamen Siyah Ekran (#000000) olur, tam ortada büyük ve modern bir tipografiyle "GÜN 2"
        /// yazar, 2.5 saniye bekledikten sonra siyah ekran açılarak (Fade-Out) yeni günün görevleri başlar.
        /// </summary>
        public void PlayDayTransition(int newDay, Action onBlackScreenMidpoint, Action onTransitionFinished)
        {
            EnsureUIBindings();
            if (activeDayTransitionCoroutine != null)
            {
                StopCoroutine(activeDayTransitionCoroutine);
            }
            activeDayTransitionCoroutine = StartCoroutine(DayTransitionRoutine(newDay, onBlackScreenMidpoint, onTransitionFinished));
        }

        IEnumerator DayTransitionRoutine(int newDay, Action onBlackScreenMidpoint, Action onTransitionFinished)
        {
            string label = $"GÜN {newDay}";

            if (dayTransitionTitleText != null)
                dayTransitionTitleText.text = label;
            if (monitorDayTransitionTitleText != null)
                monitorDayTransitionTitleText.text = label;

            if (dayTransitionCanvasGroup != null)
            {
                dayTransitionCanvasGroup.gameObject.SetActive(true);
                dayTransitionCanvasGroup.blocksRaycasts = true;
                dayTransitionCanvasGroup.alpha = 0f;
            }

            if (monitorDayTransitionGroup != null)
            {
                monitorDayTransitionGroup.gameObject.SetActive(true);
                monitorDayTransitionGroup.blocksRaycasts = true;
                monitorDayTransitionGroup.alpha = 0f;
            }

            // 1. Yumuşak Siyah Ekran Geçişi (Fade-In: 0.45 sn)
            const float fadeInDuration = 0.45f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Clamp01(elapsed / fadeInDuration);
                if (dayTransitionCanvasGroup != null) dayTransitionCanvasGroup.alpha = a;
                if (monitorDayTransitionGroup != null) monitorDayTransitionGroup.alpha = a;
                yield return null;
            }

            if (dayTransitionCanvasGroup != null) dayTransitionCanvasGroup.alpha = 1f;
            if (monitorDayTransitionGroup != null) monitorDayTransitionGroup.alpha = 1f;

            // Yeni günün verilerini ve görevlerini hazırla
            onBlackScreenMidpoint?.Invoke();
            if (ClimateOperatorManager.Instance != null)
            {
                RefreshHud(ClimateOperatorManager.Instance);
            }

            // 2. Siyah Ekranda "GÜN 2" ile 2.5 Saniye Bekleme
            yield return new WaitForSeconds(2.5f);

            // 3. Siyah Ekranın Açılması (Fade-Out: 0.45 sn)
            const float fadeOutDuration = 0.45f;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                if (dayTransitionCanvasGroup != null) dayTransitionCanvasGroup.alpha = a;
                if (monitorDayTransitionGroup != null) monitorDayTransitionGroup.alpha = a;
                yield return null;
            }

            if (dayTransitionCanvasGroup != null)
            {
                dayTransitionCanvasGroup.alpha = 0f;
                dayTransitionCanvasGroup.blocksRaycasts = false;
                dayTransitionCanvasGroup.gameObject.SetActive(false);
            }

            if (monitorDayTransitionGroup != null)
            {
                monitorDayTransitionGroup.alpha = 0f;
                monitorDayTransitionGroup.blocksRaycasts = false;
                monitorDayTransitionGroup.gameObject.SetActive(false);
            }

            activeDayTransitionCoroutine = null;
            onTransitionFinished?.Invoke();
        }

        public void ShowFloatingBudgetPopup(string message, bool isNegative = false)
        {
            EnsureUIBindings();
            if (floatingBudgetText == null) return;

            floatingBudgetText.gameObject.SetActive(true);
            floatingBudgetText.text = $"<b>{message}</b>";
            floatingPopupBaseColor = isNegative
                ? new Color(0.98f, 0.44f, 0.52f, 1f)   // #FB7185 Mercan
                : new Color(0.06f, 0.73f, 0.51f, 1f);  // #10B981 Zümrüt
            floatingBudgetText.color = floatingPopupBaseColor;
            floatingBudgetText.rectTransform.anchoredPosition = floatingPopupStartPos;
            floatingPopupTimer = FloatingPopupDuration;
        }

        public void RestartGamePeriod()
        {
            Sfx.Play(SfxType.Click);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var mgr = ClimateOperatorManager.Instance;
            if (mgr == null) return;

            if (interactor == null) interactor = PlayerInteractor.Instance;

            // 15 Saniyelik Otomatik Bütçe Akışı Göstergesi
            if (fundingTimerBar != null)
            {
                fundingTimerBar.Set(mgr.FundingProgress01, Color.white);
            }

            // Floating Kamuoyu Fonu Animasyonu (Üst barın üzerine binmeden aşağıda belirip kaybolur)
            if (floatingPopupTimer > 0f && floatingBudgetText != null)
            {
                floatingPopupTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(floatingPopupTimer / FloatingPopupDuration);
                var rt = floatingBudgetText.rectTransform;
                rt.anchoredPosition = floatingPopupStartPos - new Vector2(0f, t * 8f);

                float alpha = t < 0.7f ? 1f : Mathf.Clamp01((1f - t) / 0.3f);
                floatingBudgetText.color = new Color(
                    floatingPopupBaseColor.r,
                    floatingPopupBaseColor.g,
                    floatingPopupBaseColor.b,
                    alpha);

                if (floatingPopupTimer <= 0f)
                {
                    floatingBudgetText.gameObject.SetActive(false);
                }
            }

            // KURAL 1: Uzun etkileşim yazısı yok! Sadece monitöre yaklaşıp doğrudan baktığında
            // ekranın ortasında minimalist 'E' Tuşu İkonu (KeyPrompt_E_Badge) belirir;
            // bilgisayara oturulduğu anda tamamen gizlenir (SetActive(false)).
            if (interactor != null)
            {
                bool inMonitorMode = interactor.isFullscreen;
                bool showKeyE = !inMonitorMode && mgr.State == GameState.Playing && !mgr.IsDayTransitioning && interactor.isNearWorkstation;

                if (keyPromptBadgeRoot != null && keyPromptBadgeRoot.activeSelf != showKeyE)
                {
                    keyPromptBadgeRoot.SetActive(showKeyE);
                }

                if (crosshairImage != null)
                {
                    crosshairImage.enabled = !inMonitorMode && !showKeyE && mgr.State == GameState.Playing && !mgr.IsDayTransitioning;
                }
            }

            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0f)
            {
                updateTimer = 0.06f;
                RefreshHud(mgr);
            }

            if (mgr.State != GameState.Playing)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (keyPromptBadgeRoot != null && keyPromptBadgeRoot.activeSelf)
                {
                    keyPromptBadgeRoot.SetActive(false);
                }

                if (endScreenRoot != null && !endScreenRoot.activeSelf)
                {
                    endScreenRoot.SetActive(true);

                    if (endTitleText != null)
                    {
                        endTitleText.text = mgr.State == GameState.Won
                            ? "<color=#10B981>DÖNEM BAŞARIYLA TAMAMLANDI</color>"
                            : "<color=#FB7185>BAKANLIK DÖNEMİ SONA ERDİ</color>";
                    }

                    if (endSubtitleText != null)
                    {
                        endSubtitleText.text = mgr.EndReason;
                    }

                    if (endStatValueTexts != null && endStatValueTexts.Length >= 6)
                    {
                        if (endStatValueTexts[0] != null) endStatValueTexts[0].text = $"Gün {Mathf.Min(mgr.CurrentDay, mgr.targetDays)} / {mgr.targetDays}";
                        if (endStatValueTexts[1] != null) endStatValueTexts[1].text = $"{mgr.ResolvedCount} Karar";
                        if (endStatValueTexts[2] != null) endStatValueTexts[2].text = $"%{mgr.Iklim:0}";
                        if (endStatValueTexts[3] != null) endStatValueTexts[3].text = $"%{mgr.Halk:0}";
                        if (endStatValueTexts[4] != null) endStatValueTexts[4].text = $"${mgr.Money}";
                        if (endStatValueTexts[5] != null) endStatValueTexts[5].text = $"{mgr.Authority} Puan";
                    }
                }
            }
        }

        void RefreshHud(ClimateOperatorManager mgr)
        {
            if (climateBar != null)
            {
                float c01 = mgr.Iklim / 100f;
                climateBar.Set(c01, Color.white);
            }
            if (climateText != null)
            {
                string valCol = mgr.Iklim < 30f ? "#EF4444" : "#10B981";
                climateText.text = $"Küresel İklim  <color={valCol}>%{mgr.Iklim:0}</color>";
            }

            if (mentalBar != null)
            {
                float m01 = mgr.Halk / 100f;
                mentalBar.Set(m01, Color.white);
            }
            if (mentalText != null)
            {
                string valCol = mgr.Halk < 30f ? "#EF4444" : "#38BDF8";
                mentalText.text = $"Kamuoyu Desteği  <color={valCol}>%{mgr.Halk:0}</color>";
            }

            if (moneyText != null)
            {
                moneyText.text = $"Bütçe <color=#F59E0B>${mgr.Money}</color>  <size=6><color=#94A3B8>(+${mgr.NextFundingAmount}/15sn)</color></size>";
            }

            if (authorityText != null)
            {
                authorityText.text = $"Otorite <color=#A855F7><b>{mgr.Authority}</b></color>   İmza <color=#E2E8F0><b>{mgr.RemainingSignatures}/{mgr.maxDailySignatures}</b></color>";
            }

            if (dayText != null)
            {
                dayText.text = $"GÜN {mgr.CurrentDay}";
            }
        }
    }
}
