using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// Ülkeye tıklandığında açılan Müzakere / Tercih ekranını ve
    /// seçilen tercihin hangi masaüstü bulmacasını tetikleyeceğini yöneten sınıf.
    /// </summary>
    public class DialogueAndChoiceSystem : MonoBehaviour
    {
        public static DialogueAndChoiceSystem Instance { get; private set; }

        [Header("Görsel Paneller (World-Space)")]
        public GameObject gridViewRoot;
        public GameObject negotiationViewRoot;
        public GameObject dialogue3DGroup;

        [Header("Müzakere UI Referansları")]
        public Text countryTitleText;
        public Text presidentText;
        public Text angerValueText;
        public UIBar angerBar;
        public Text crisisMessageText;
        public Text instructionText;

        [Header("6 Sorun Barı UI Referansları")]
        public Text[] problemLabels;
        public UIBar[] problemBars;

        [Header("3 Tercih Butonları / Kartları")]
        public PressButton choiceButton1; // Sert Yaptırım
        public PressButton choiceButton2; // Finansal Teşvik
        public PressButton choiceButton3; // Kısmi Taviz
        public PressButton backButton;    // Geri Dön

        public Text choice1Text;
        public Text choice2Text;
        public Text choice3Text;

        CountryNode currentCountry;
        PuzzleRequest pendingRequest;

        public CountryNode CurrentCountry => currentCountry;
        public PuzzleRequest PendingRequest => pendingRequest;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (choiceButton1 != null) choiceButton1.Pressed += () => OnChoiceSelected(ChoiceType.HardSanction);
            if (choiceButton2 != null) choiceButton2.Pressed += () => OnChoiceSelected(ChoiceType.FinancialIncentive);
            if (choiceButton3 != null) choiceButton3.Pressed += () => OnChoiceSelected(ChoiceType.PartialConcession);
            if (backButton != null) backButton.Pressed += ShowGrid;

            ShowGrid();
        }

        public void ShowGrid()
        {
            currentCountry = null;
            if (gridViewRoot != null) gridViewRoot.SetActive(true);
            if (negotiationViewRoot != null) negotiationViewRoot.SetActive(false);
            if (dialogue3DGroup != null) dialogue3DGroup.SetActive(false);
        }

        /// <summary>Ülke kutusuna tıklandığında müzakere ekranını açar.</summary>
        public void OpenCountry(CountryNode country)
        {
            if (country == null) return;
            currentCountry = country;

            if (gridViewRoot != null) gridViewRoot.SetActive(false);
            if (negotiationViewRoot != null) negotiationViewRoot.SetActive(true);
            if (dialogue3DGroup != null) dialogue3DGroup.SetActive(true);

            RefreshNegotiationView();
            Sfx.Play(SfxType.Click);
        }

        public void RefreshNegotiationView()
        {
            if (currentCountry == null) return;

            if (countryTitleText != null)
                countryTitleText.text = $"{currentCountry.CountryName.ToUpper()} MÜZAKERE MASASI";

            if (presidentText != null)
                presidentText.text = $"Devlet Başkanı: <b>{currentCountry.PresidentName}</b>";

            if (angerValueText != null)
                angerValueText.text = $"Sinir Seviyesi: %{currentCountry.Anger:0}";

            if (angerBar != null)
            {
                float a01 = currentCountry.Anger / 100f;
                Color c = Color.Lerp(new Color(0.2f, 0.8f, 0.3f), new Color(0.9f, 0.15f, 0.15f), a01);
                angerBar.Set(a01, c);
            }

            // Kriz mesajı
            if (crisisMessageText != null)
            {
                if (currentCountry.InCrisis)
                {
                    crisisMessageText.text =
                        $"<color=#FF4444><b>[KRİZ ALARMI - {ProblemInfo.Name(currentCountry.CrisisProblem).ToUpper()}]</b></color>\n" +
                        $"\"{currentCountry.CrisisMessage}\"";
                }
                else
                {
                    crisisMessageText.text =
                        "<color=#55FF88><b>[DURUM STABİL]</b></color>\n" +
                        "Aktif acil kriz bulunmuyor. Genel iklim politikalarını ve reformları müzakere edebilirsiniz.";
                }
            }

            // 6 Sorun Barı
            for (int i = 0; i < ProblemInfo.Count; i++)
            {
                var p = (ProblemType)i;
                float val = currentCountry.GetBar(p);
                if (problemLabels != null && i < problemLabels.Length && problemLabels[i] != null)
                {
                    string star = ProblemInfo.IsStarred(p) ? "<color=#FFD700>*</color>" : "";
                    problemLabels[i].text = $"{ProblemInfo.ShortName(p)}{star}: %{val:0}";
                }
                if (problemBars != null && i < problemBars.Length && problemBars[i] != null)
                {
                    float v01 = val / 100f;
                    Color barCol = v01 > 0.7f ? new Color(0.9f, 0.2f, 0.2f) :
                                   v01 > 0.4f ? new Color(0.9f, 0.7f, 0.2f) :
                                                new Color(0.2f, 0.8f, 0.4f);
                    problemBars[i].Set(v01, barCol);
                }
            }

            // Tercih Açıklamaları
            var mgr = ClimateStateManager.Instance;
            var prob = currentCountry.InCrisis ? currentCountry.CrisisProblem : currentCountry.WorstProblem;
            float finCost = mgr != null ? mgr.FinancialCost(currentCountry, prob) : 250f;

            if (choice1Text != null)
                choice1Text.text =
                    $"<b>[1] SERT YAPTIRIM</b>\n" +
                    $"Maliyet: 0$  |  {ProblemInfo.ShortName(prob)} -40\n" +
                    $"<color=#FF6666>Başkan Siniri +22, Mental -6</color>";

            if (choice2Text != null)
                choice2Text.text =
                    $"<b>[2] FİNANSAL TEŞVİK</b>\n" +
                    $"Maliyet: {finCost:0}$  |  {ProblemInfo.ShortName(prob)} -35\n" +
                    $"<color=#66FF99>Sinir -4, İklim Seviyesi Yükselir</color>";

            if (choice3Text != null)
                choice3Text.text =
                    $"<b>[3] KISMİ TAVİZ / ERTELEME</b>\n" +
                    $"Gelir: +120$  |  {ProblemInfo.ShortName(prob)} -8\n" +
                    $"<color=#FFFF66>Sinir -15 | Risk: %50 Çevre Kirliliği</color>";

            if (instructionText != null)
            {
                if (DeskPuzzleController.Instance != null && DeskPuzzleController.Instance.IsMainPuzzleActive)
                {
                    instructionText.text =
                        $"<color=#FFD24D><b>[BEKLEMEDE]</b></color> Masadaki aktif protokolü tamamlayın!";
                }
                else
                {
                    instructionText.text =
                        "Bir tercih seçtiğinizde masada fiziksel onay bulmacası açılacaktır.";
                }
            }
        }

        void OnChoiceSelected(ChoiceType choice)
        {
            if (currentCountry == null) return;
            var desk = DeskPuzzleController.Instance;
            if (desk != null && desk.IsMainPuzzleActive)
            {
                Sfx.Play(SfxType.Fail);
                if (instructionText != null)
                    instructionText.text = "<color=#FF4444>Masa meşgul! Önce açık protokolü çözün.</color>";
                return;
            }

            // Hangi sorun çözülecek?
            ProblemType targetProblem = currentCountry.InCrisis
                ? currentCountry.CrisisProblem
                : currentCountry.WorstProblem;

            // Hangi fiziksel bulmaca tetiklenecek?
            PuzzleType puzzle = DeterminePuzzle(targetProblem, choice);

            pendingRequest = new PuzzleRequest
            {
                Country = currentCountry,
                Problem = targetProblem,
                Choice = choice,
                Puzzle = puzzle
            };

            // DeskPuzzleController'a bulmacayı başlatması için emir ver
            if (desk != null)
            {
                desk.StartPuzzle(pendingRequest, OnPuzzleCompleted);
            }

            if (instructionText != null)
            {
                instructionText.text =
                    $"<color=#00FFFF><b>[FİZİKSEL ONAY GEREKİYOR]</b></color>\n" +
                    $"Lütfen masadaki <b>{ProblemInfo.PuzzleName(puzzle)}</b> modülünü tamamlayın!";
            }

            ClimateStateManager.Instance?.Post(
                $"Onay bekleniyor: {currentCountry.CountryName} için {ProblemInfo.ChoiceName(choice)} seçildi. {ProblemInfo.PuzzleName(puzzle)} devrede!");
        }

        /// <summary>
        /// Problem ve tercihe göre masadaki bulmaca modülü eşleşmesi:
        /// 1. Finansal Teşvik -> Bütçe Transferi Numpad
        /// 2. Yenilenebilir Enerji / Fabrika -> 4'lü Şalterler
        /// 3. Sıfır Atık -> Numpad Sıfır Atık Protokolü
        /// 4. Ağaç / Gıda Güvenliği -> Döner Vana & Basınç İbresi
        /// 5. Diğer -> Şalterler
        /// </summary>
        PuzzleType DeterminePuzzle(ProblemType problem, ChoiceType choice)
        {
            if (choice == ChoiceType.FinancialIncentive)
                return PuzzleType.Numpad;

            switch (problem)
            {
                case ProblemType.Renewable:
                case ProblemType.Factory:
                    return PuzzleType.Switches;

                case ProblemType.ZeroWaste:
                    return PuzzleType.Numpad;

                case ProblemType.Tree:
                case ProblemType.Food:
                    return PuzzleType.Valve;

                default:
                    return PuzzleType.Switches;
            }
        }

        void OnPuzzleCompleted(bool success)
        {
            var req = pendingRequest;
            pendingRequest = null;

            if (req != null)
            {
                ClimateStateManager.Instance?.ApplyChoiceResult(req, success);
            }

            if (currentCountry != null)
            {
                RefreshNegotiationView();
                // Eğer kriz çözüldüyse veya ülke durumu düzeldiyse
                if (!currentCountry.InCrisis)
                {
                    // Kısa süre sonra ana ızgaraya dönebilir veya açık kalabilir
                }
            }
        }
    }
}
