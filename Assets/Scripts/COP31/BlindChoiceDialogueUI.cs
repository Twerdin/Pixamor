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
        int lastKnownMoney = -1;
        int lastKnownAuthority = -1;

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

            if (choiceTexts == null || choiceTexts.Length < 3 || choiceTexts[0] == null)
            {
                if (choiceTexts == null || choiceTexts.Length < 3) choiceTexts = new UnityEngine.UI.Text[3];
                for (int i = 0; i < 3; i++)
                {
                    if (choiceTexts[i] == null && rootObject != null)
                    {
                        var cardT = rootObject.transform.Find($"Choice_Card_{i + 1}");
                        if (cardT != null)
                        {
                            var t = cardT.GetComponentInChildren<UnityEngine.UI.Text>();
                            if (t != null) choiceTexts[i] = t;
                        }
                    }
                }
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

            if (mgr != null && (mgr.Money != lastKnownMoney || mgr.Authority != lastKnownAuthority))
            {
                lastKnownMoney = mgr.Money;
                lastKnownAuthority = mgr.Authority;
                RefreshDisplay();
            }

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

            if (ClimateOperatorManager.Instance != null)
            {
                lastKnownMoney = ClimateOperatorManager.Instance.Money;
                lastKnownAuthority = ClimateOperatorManager.Instance.Authority;
            }

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

                // Karar/seçim butonlarında (1, 2, 3) eylem metni ve net Para/Otorite gereksinimleri belirtilir.
                if (choiceTexts != null && q.choices != null)
                {
                    for (int i = 0; i < choiceTexts.Length && i < q.choices.Length; i++)
                    {
                        if (choiceTexts[i] == null) continue;

                        choiceTexts[i].verticalOverflow = VerticalWrapMode.Overflow;
                        choiceTexts[i].text = FormatChoiceText(q.choices[i], i, currentContinent.isDeferredDoubled);

                        if (choiceBgImages != null && i < choiceBgImages.Length && choiceBgImages[i] != null)
                            Greybox.ApplySprite(choiceBgImages[i], "btn_choice_aqua", sliced: true);
                    }
                }
            }

            // Alt Kısım: 2 Bakanlık Müdahalesi Butonu (Net Para ve Otorite Maliyeti)
            var operatorMgr = ClimateOperatorManager.Instance;
            int bCost = operatorMgr != null ? operatorMgr.budgetSupportCost : 22;
            int aCost = operatorMgr != null ? operatorMgr.auditAuthorityCost : 2;
            bool canAffordB = operatorMgr == null || operatorMgr.Money >= bCost;
            bool canAffordA = operatorMgr == null || operatorMgr.Authority >= aCost;

            if (budgetSupportText != null)
            {
                string bColor = canAffordB ? "#F59E0B" : "#EF4444";
                string bWarn = canAffordB ? "" : " (Yetersiz)";
                budgetSupportText.text = $"<b>Bütçe Desteği Sağla</b>  <size=8><color={bColor}>(-${bCost} Bütçe{bWarn})</color></size>";
            }

            if (auditPressureText != null)
            {
                if (currentContinent.IsPuppet)
                {
                    auditPressureText.text = "<color=#94A3B8><b>Denetim Raporu Uygulandı</b></color>";
                }
                else
                {
                    string aColor = canAffordA ? "#C084FC" : "#EF4444";
                    string aWarn = canAffordA ? "" : " (Yetersiz)";
                    auditPressureText.text = $"<b>Denetim Raporuyla Baskı Kur</b>  <size=8><color={aColor}>({aCost} Otorite{aWarn})</color></size>";
                }
            }
        }

        string FormatChoiceText(BlindChoice choice, int choiceIndex, bool isDeferredDoubled)
        {
            if (choice == null) return "";

            var mgr = ClimateOperatorManager.Instance;
            string cleanText = choice.decisionText ?? "";
            cleanText = cleanText.Replace("(1 Otorite)", "").Replace("( 1 Otorite)", "").Trim();

            string moneyStr;
            string authStr;

            if (choiceIndex == 0)
            {
                // Seçim 1: Tavizsiz Yaptırım / Acil Saha Müdahalesi (Bütçe harcar + 1 Otorite gerekir)
                int cost = choice.moneyDelta < 0 ? -choice.moneyDelta : 0;
                bool hasMoney = mgr == null || mgr.Money >= cost;
                bool hasAuth = mgr == null || mgr.Authority >= 1;

                moneyStr = cost > 0
                    ? (hasMoney ? $"<color=#F59E0B>-${cost} Bütçe</color>" : $"<color=#EF4444>-${cost} Bütçe (Yetersiz)</color>")
                    : "<color=#94A3B8>$0 Bütçe</color>";

                authStr = hasAuth
                    ? "<color=#C084FC>1 Otorite</color>"
                    : "<color=#EF4444>1 Otorite (Yetersiz)</color>";

                return $"<size=8>{cleanText}</size>\n<size=7><color=#64748B>Gereksinim:</color>  {moneyStr}   <color=#475569>•</color>   {authStr}</size>";
            }
            else if (choiceIndex == 1)
            {
                // Seçim 2: Kademeli Teknoloji & Yeşil Dönüşüm Teşviği (Bütçe harcar, 0 Otorite)
                int cost = choice.moneyDelta < 0 ? -choice.moneyDelta : 0;
                bool hasMoney = mgr == null || mgr.Money >= cost;

                moneyStr = cost > 0
                    ? (hasMoney ? $"<color=#F59E0B>-${cost} Bütçe</color>" : $"<color=#EF4444>-${cost} Bütçe (Yetersiz)</color>")
                    : (choice.moneyDelta > 0 ? $"<color=#10B981>+${choice.moneyDelta} Bütçe</color>" : "<color=#94A3B8>$0 Bütçe</color>");

                authStr = "<color=#94A3B8>0 Otorite</color>";

                return $"<size=8>{cleanText}</size>\n<size=7><color=#64748B>Gereksinim:</color>  {moneyStr}   <color=#475569>•</color>   {authStr}</size>";
            }
            else
            {
                // Seçim 3: Ertelenmiş veya Normal Erteleme
                if (!isDeferredDoubled)
                {
                    // İlk kriz anında: 3 gün sonra 2x etkiyle dönmek üzere erteler (Maliyetsiz)
                    moneyStr = "<color=#94A3B8>$0 Bütçe</color>";
                    authStr = "<color=#94A3B8>0 Otorite</color>";
                    return $"<size=8>{cleanText}</size>\n<size=7><color=#64748B>Gereksinim:</color>  {moneyStr}   <color=#475569>•</color>   {authStr}   <color=#475569>•</color>   <color=#F59E0B>(3 Gün Ertele)</color></size>";
                }
                else
                {
                    // 2x Ertelenmiş krizde: Sanayi tavizi kabul edilir (+Para ve +1 Otorite kazandırır)
                    int gain = choice.moneyDelta > 0 ? choice.moneyDelta : 20;
                    moneyStr = $"<color=#10B981>+${gain} Bütçe</color>";
                    authStr = "<color=#10B981>+1 Otorite Kazandırır</color>";
                    return $"<size=8>{cleanText}</size>\n<size=7><color=#64748B>Gereksinim:</color>  {moneyStr}   <color=#475569>•</color>   {authStr}</size>";
                }
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
