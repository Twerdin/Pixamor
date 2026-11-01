using UnityEngine;
using UnityEngine.UI;

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// V3: Aqua-Modern (60-30-10 Su Yeşili) İklim Bakanlığı Kriz, Gizli Çelişki Tespiti ve Dinamik Karar Süresi Dosyası:
    /// - EnsureUIBindings() ile sıfır NullReferenceException garantisi sağlar.
    /// - Tüm kartlar 25+ PNG Sprite setini (panel_main_aqua, panel_telemetry_glass, card_dialogue_citizen,
    ///   card_dialogue_president, badge_speaker_tag, btn_choice_aqua, btn_sanction_strike, btn_bribe_lobby, bar_timer_urgent)
    ///   kullanır ve Image.color = Color.white korunur.
    /// </summary>
    public class BlindChoiceDialogueUI : MonoBehaviour
    {
        public static BlindChoiceDialogueUI Instance { get; private set; }

        [Header("Ana Kök ve Arka Plan Gizleme")]
        public GameObject rootObject;
        public GameObject backgroundMap2D;
        public GameObject backgroundMap3D;

        [Header("Üst Zaman Çubuğu (10s / 20s / 30s Eriyen Bar - bar_timer_urgent.png)")]
        public UIBar decisionTimerBar;

        [Header("Sol/Orta Kısım: Muhatap Kartı, Etiket, Ham Beyan & Sağ Uydu Telemetrisi")]
        public Text continentTitleText;
        public Text representativeText;
        public Text statusBadgeText;
        public Image statusBadgeBg;

        public Image statementCardBg;
        public Text officialStatementText;
        public Image telemetryCardBg;
        public Text satelliteSensorText;

        public PressButton verifyReportButton;
        public Button verifyReportUIButton;
        public Text verifyReportButtonText;
        public Image verifyReportButtonBg;

        [Header("Orta/Sağ Kısım: Kriz & 3 Ferah Seçim Kartı (btn_choice_aqua.png)")]
        public Text crisisTitleText;
        public Text descriptionText;
        public PressButton[] choiceButtons = new PressButton[3];
        public Text[] choiceTexts = new Text[3];
        public Image[] choiceBgImages = new Image[3];

        [Header("Alt Kısım: Bakanlık Müdahalesi (btn_bribe_lobby.png & btn_header_utility.png)")]
        public PressButton budgetSupportButton;
        public Text budgetSupportText;
        public PressButton auditPressureButton;
        public Text auditPressureText;

        [Header("Sağ Üst Köşe Kapatma İkonu (✕)")]
        public PressButton closeButton;

        WorldMapMonitorController.ContinentState currentContinent;
        float activeDecisionMaxTime = 20f;
        float activeDecisionTimeLeft = 20f;
        bool falseAccusationTriggeredOnCurrent;

        void OnEnable()
        {
            Instance = this;
        }

        void Awake()
        {
            Instance = this;
            EnsureUIBindings();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            EnsureUIBindings();

            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    int idx = i;
                    if (choiceButtons[i] != null)
                        choiceButtons[i].Pressed += () => OnChoiceSelected(idx);
                }
            }

            if (verifyReportButton != null)
                verifyReportButton.Pressed += OnSanctionAuditClicked;

            if (verifyReportUIButton != null)
            {
                verifyReportUIButton.onClick.RemoveAllListeners();
                verifyReportUIButton.onClick.AddListener(OnSanctionAuditClicked);
            }

            if (budgetSupportButton != null)
                budgetSupportButton.Pressed += OnBudgetSupportClicked;

            if (auditPressureButton != null)
                auditPressureButton.Pressed += OnAuditPressureClicked;

            if (closeButton != null)
                closeButton.Pressed += Close;

            Close();
        }

        /// <summary>
        /// KURAL 8: Sıfır Hata (Zero-Error) Çift Yönlü UI Bağlama Koruması
        /// </summary>
        public void EnsureUIBindings()
        {
            if (rootObject == null)
            {
                var found = GameObject.Find("Ministry_Dossier_Root");
                if (found != null) rootObject = found;
            }

            if (backgroundMap2D == null)
            {
                var found = GameObject.Find("World_Map_2D_Group");
                if (found != null) backgroundMap2D = found;
            }

            if (backgroundMap3D == null)
            {
                var found = GameObject.Find("World_Map_3D_Group");
                if (found != null) backgroundMap3D = found;
            }
        }

        void Update()
        {
            if (rootObject == null || !rootObject.activeSelf || currentContinent == null) return;

            var mgr = ClimateOperatorManager.Instance;
            if (mgr != null && mgr.State != GameState.Playing)
            {
                Close();
                return;
            }

            if (SanctionMiniGameUI.Instance != null && SanctionMiniGameUI.Instance.IsActive)
                return;

            // KURAL 6: 10 sn / 20 sn / 30 sn hızla eriyen karar süresi çubuğu (bar_timer_urgent.png)
            activeDecisionTimeLeft -= Time.deltaTime;
            currentContinent.crisisRemainingTime = activeDecisionTimeLeft;

            if (decisionTimerBar != null && activeDecisionMaxTime > 0.1f)
            {
                float t01 = Mathf.Clamp01(activeDecisionTimeLeft / activeDecisionMaxTime);
                Color barCol = t01 > 0.35f
                    ? Color.white
                    : new Color(0.98f, 0.44f, 0.52f, 1f); // #FB7185 Mercan
                decisionTimerBar.Set(t01, barCol);
            }

            if (activeDecisionTimeLeft <= 0f)
            {
                var expiredCont = currentContinent;
                string qTitle = expiredCont.activeQuestion != null ? expiredCont.activeQuestion.title : "Kriz Dosyası";
                bool wasDoubled = expiredCont.isDeferredDoubled;

                ContinentCrisisSpawner.Instance?.ClearContinentCrisis(expiredCont);
                Close();
                mgr?.ApplyUnresolvedCrisisPenalty(expiredCont.name, qTitle, wasDoubled);
            }
        }

        public void ShowForContinent(WorldMapMonitorController.ContinentState cont)
        {
            EnsureUIBindings();
            if (cont == null || rootObject == null) return;

            currentContinent = cont;
            falseAccusationTriggeredOnCurrent = false;

            if (currentContinent.activeQuestion == null && ContinentCrisisSpawner.Instance != null)
            {
                currentContinent.activeQuestion = ContinentCrisisSpawner.Instance.PickQuestionForContinent(cont.type);
            }

            var q = currentContinent.activeQuestion;
            activeDecisionMaxTime = (q != null && q.decisionTimeLimit > 1f) ? q.decisionTimeLimit : 20f;
            activeDecisionTimeLeft = (currentContinent.crisisRemainingTime > 1f && currentContinent.crisisRemainingTime <= activeDecisionMaxTime)
                ? currentContinent.crisisRemainingTime
                : activeDecisionMaxTime;
            currentContinent.crisisRemainingTime = activeDecisionTimeLeft;

            if (decisionTimerBar != null)
            {
                decisionTimerBar.Set(Mathf.Clamp01(activeDecisionTimeLeft / activeDecisionMaxTime), Color.white);
            }

            if (backgroundMap2D != null) backgroundMap2D.SetActive(false);
            if (backgroundMap3D != null) backgroundMap3D.SetActive(false);

            rootObject.SetActive(true);
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (currentContinent == null) return;

            var q = currentContinent.activeQuestion;
            string category = (q != null && !string.IsNullOrEmpty(q.speakerCategory))
                ? q.speakerCategory
                : "Kıta Başkanı";
            bool isExecutive = category.Contains("Başkan") || category.Contains("CEO");

            if (continentTitleText != null)
            {
                continentTitleText.text = currentContinent.name.ToUpperInvariant();
            }

            if (representativeText != null)
            {
                string speaker = (q != null && !string.IsNullOrEmpty(q.speakerName))
                    ? q.speakerName
                    : currentContinent.representativeName;
                representativeText.text = speaker;
            }

            if (statusBadgeText != null)
            {
                statusBadgeText.text = $"{category}  |  Uyum %{currentContinent.loyalty:0}";
                if (statusBadgeBg != null)
                {
                    Greybox.ApplySprite(statusBadgeBg, "badge_speaker_tag", sliced: true);
                }
            }

            // Muhatap Beyanı Kartı: Halk ise card_dialogue_citizen.png, Başkan/CEO ise card_dialogue_president.png
            if (statementCardBg != null)
            {
                Greybox.ApplySprite(statementCardBg, isExecutive ? "card_dialogue_president" : "card_dialogue_citizen", sliced: true);
            }

            if (telemetryCardBg != null)
            {
                Greybox.ApplySprite(telemetryCardBg, "panel_telemetry_glass", sliced: true);
            }

            // KURAL 4: GİZLİ ÇELİŞKİ TESPİTİ (Sadece 2 Ham Bilgi Alanı + Standart "Denetim & Yaptırım Başlat" Butonu)
            if (q != null)
            {
                if (officialStatementText != null)
                    officialStatementText.text = q.GetOfficialStatement(currentContinent.representativeName);

                if (satelliteSensorText != null)
                    satelliteSensorText.text = q.GetSatelliteSensorData();
            }

            if (verifyReportButtonText != null)
            {
                if (falseAccusationTriggeredOnCurrent)
                {
                    verifyReportButtonText.text = "<color=#FB7185><b>Asılsız Denetim Skandalı (-%12 Kamuoyu)</b></color>";
                    if (verifyReportButtonBg != null)
                        Greybox.ApplySprite(verifyReportButtonBg, "btn_sanction_strike", sliced: true);
                }
                else
                {
                    verifyReportButtonText.text = "<color=#F0FDFA><b>Denetim & Yaptırım Başlat</b></color>";
                    if (verifyReportButtonBg != null)
                        Greybox.ApplySprite(verifyReportButtonBg, "btn_sanction_strike", sliced: true);
                }
            }

            // Orta/Sağ Kısım: Kriz Başlığı, Açıklama ve 3 Seçim Kartı (btn_choice_aqua.png)
            if (q != null)
            {
                if (crisisTitleText != null)
                {
                    crisisTitleText.text = currentContinent.isDeferredDoubled
                        ? $"<color=#F59E0B>ERTELENMİŞ KRİZ (2X ETKİ)</color>  {q.title}"
                        : q.title;
                }

                if (descriptionText != null)
                {
                    descriptionText.text = q.description;
                }

                // KURAL 4: Karar/seçim butonlarında (1, 2, 3) maliyet, kazanç ve sonuç açıklamaları TAMAMEN GİZLİ;
                // sadece oyuncunun vereceği kararın saf diyalog/eylem cümlesi yazar.
                if (choiceTexts != null && q.choices != null)
                {
                    if (choiceTexts.Length > 0 && choiceTexts[0] != null && q.choices.Length > 0)
                    {
                        choiceTexts[0].text = q.choices[0].decisionText;
                        if (choiceBgImages != null && choiceBgImages.Length > 0 && choiceBgImages[0] != null)
                            Greybox.ApplySprite(choiceBgImages[0], "btn_choice_aqua", sliced: true);
                    }

                    if (choiceTexts.Length > 1 && choiceTexts[1] != null && q.choices.Length > 1)
                    {
                        choiceTexts[1].text = q.choices[1].decisionText;
                        if (choiceBgImages != null && choiceBgImages.Length > 1 && choiceBgImages[1] != null)
                            Greybox.ApplySprite(choiceBgImages[1], "btn_choice_aqua", sliced: true);
                    }

                    if (choiceTexts.Length > 2 && choiceTexts[2] != null && q.choices.Length > 2)
                    {
                        choiceTexts[2].text = q.choices[2].decisionText;
                        if (choiceBgImages != null && choiceBgImages.Length > 2 && choiceBgImages[2] != null)
                            Greybox.ApplySprite(choiceBgImages[2], "btn_choice_aqua", sliced: true);
                    }
                }
            }

            // Alt Kısım: 2 Sade Bakanlık Müdahalesi Butonu (Maliyetsiz saf başlık)
            if (budgetSupportText != null)
            {
                budgetSupportText.text = "<b>Bütçe Desteği Sağla</b>";
            }

            if (auditPressureText != null)
            {
                auditPressureText.text = currentContinent.IsPuppet
                    ? "<b>Denetim Raporu Uygulandı</b>"
                    : "<b>Denetim Raporuyla Baskı Kur</b>";
            }
        }

        void OnSanctionAuditClicked()
        {
            if (currentContinent == null || currentContinent.activeQuestion == null) return;
            if (SanctionMiniGameUI.Instance != null && SanctionMiniGameUI.Instance.IsActive) return;

            var q = currentContinent.activeQuestion;

            if (q.hasContradiction)
            {
                if (SanctionMiniGameUI.Instance != null)
                {
                    SanctionMiniGameUI.Instance.StartRandomMiniGame(OnSanctionMiniGameCompleted);
                }
                else
                {
                    OnSanctionMiniGameCompleted(true);
                }
            }
            else
            {
                if (!falseAccusationTriggeredOnCurrent)
                {
                    falseAccusationTriggeredOnCurrent = true;
                    ClimateOperatorManager.Instance?.ApplyFalseAccusationPenalty(currentContinent);
                    RefreshDisplay();
                }
            }
        }

        void OnSanctionMiniGameCompleted(bool wonMiniGame)
        {
            if (currentContinent == null) return;

            var targetCont = currentContinent;
            var mgr = ClimateOperatorManager.Instance;
            bool wasDoubled = targetCont.isDeferredDoubled;

            ContinentCrisisSpawner.Instance?.ClearContinentCrisis(targetCont);
            Close();

            if (wonMiniGame)
            {
                mgr?.ApplyContradictionSanction(targetCont, wasDoubled);
            }
            else
            {
                mgr?.ApplyFailedMiniGamePenalty(targetCont);
            }
        }

        void OnBudgetSupportClicked()
        {
            if (currentContinent == null) return;
            if (SanctionMiniGameUI.Instance != null && SanctionMiniGameUI.Instance.IsActive) return;

            if (ClimateOperatorManager.Instance != null && ClimateOperatorManager.Instance.TryProvideBudgetSupport(currentContinent))
            {
                RefreshDisplay();
            }
        }

        void OnAuditPressureClicked()
        {
            if (currentContinent == null || currentContinent.IsPuppet) return;
            if (SanctionMiniGameUI.Instance != null && SanctionMiniGameUI.Instance.IsActive) return;

            if (ClimateOperatorManager.Instance != null && ClimateOperatorManager.Instance.TryEnforceAuditPressure(currentContinent))
            {
                RefreshDisplay();
            }
        }

        void OnChoiceSelected(int choiceIndex)
        {
            if (SanctionMiniGameUI.Instance != null && SanctionMiniGameUI.Instance.IsActive) return;

            if (currentContinent == null || currentContinent.activeQuestion == null)
            {
                Close();
                return;
            }

            var targetCont = currentContinent;
            var mgr = ClimateOperatorManager.Instance;
            if (mgr == null) return;

            var q = targetCont.activeQuestion;
            bool wasDoubled = targetCont.isDeferredDoubled;

            if (choiceIndex == 2 && targetCont.inCrisis && !targetCont.isDeferredDoubled)
            {
                Close();
                ContinentCrisisSpawner.Instance?.DeferContinentCrisis(targetCont);
                return;
            }

            if (q.choices != null && choiceIndex >= 0 && choiceIndex < q.choices.Length)
            {
                var chosen = q.choices[choiceIndex];
                if (mgr.CanAffordContinentChoice(chosen, choiceIndex))
                {
                    ContinentCrisisSpawner.Instance?.ClearContinentCrisis(targetCont);
                    Close();
                    mgr.ApplyContinentChoice(targetCont, chosen, choiceIndex, wasDoubled);
                }
            }
        }

        public void Close()
        {
            if (SanctionMiniGameUI.Instance != null && SanctionMiniGameUI.Instance.IsActive)
            {
                SanctionMiniGameUI.Instance.CancelMiniGame();
            }

            currentContinent = null;
            if (rootObject != null)
            {
                rootObject.SetActive(false);
            }
            if (backgroundMap2D != null) backgroundMap2D.SetActive(true);
            if (backgroundMap3D != null) backgroundMap3D.SetActive(true);
        }
    }
}
