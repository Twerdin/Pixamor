using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Pixamor.COP31
{
    /// <summary>
    /// Masa (Çevre Paneli) üzerindeki tüm fiziksel 3D bulmaca modüllerini,
    /// şalterleri, numpad tuşlarını, vanayı ve acil durum butonunu yöneten sınıf.
    /// Her modül hem serbest test modunda hem de kriz protokolü sırasında %100 çalışır.
    /// </summary>
    public class DeskPuzzleController : MonoBehaviour
    {
        public static DeskPuzzleController Instance { get; private set; }

        [Header("Zaman Çizelgesi")]
        public TimeLever timeLever;
        public ToggleSwitch timeModeSwitch;
        public Text timeModeText;

        [Header("Modül 1: 4'lü Şalterler")]
        public HatchModule hatchSwitches;
        public ToggleSwitch[] switches = new ToggleSwitch[4];
        public PressButton switchConfirmButton;
        public Text switchTargetText;
        public Image[] switchTargetLamps;

        [Header("Modül 2: Numpad (Tuş Takımı)")]
        public HatchModule hatchNumpad;
        public PressButton[] numpadDigits = new PressButton[10];
        public PressButton numpadClear;
        public PressButton numpadOk;
        public Text numpadDisplay;
        public Text numpadProtocolLabel;

        [Header("Modül 3: Döner Vana & Basınç İbresi")]
        public HatchModule hatchValve;
        public HoldValve valve;
        public PressButton valveConfirmButton;
        public UIBar pressureBar;
        public Text pressureText;
        public float pressureFillSpeed = 0.35f;
        public float pressureLeakSpeed = 0.18f;
        public float greenMin = 0.60f;
        public float greenMax = 0.85f;

        [Header("Modül 4: Acil Durum / Sinir Yatıştırma")]
        public HatchModule hatchEmergency;
        public PressButton emergencyButton;
        public Text emergencyTimerText;
        public Light emergencyLight;
        public float emergencyDuration = 10f;

        [Header("Masa Bilgi Ekranı")]
        public Text deskStatusText;

        // Durum Değişkenleri
        PuzzleRequest currentRequest;
        Action<bool> onMainPuzzleComplete;

        bool[] targetSwitchPattern = new bool[4];
        string targetNumpadCode = "";
        string enteredNumpadCode = "";
        float currentPressure = 0.1f;

        bool isEmergencyActive;
        float emergencyTimer;
        Action<bool> onEmergencyComplete;
        CountryNode emergencyCountry;

        public bool IsMainPuzzleActive => currentRequest != null;
        public bool CanStartEmergency => !isEmergencyActive;
        public bool IsEmergencyActive => isEmergencyActive;

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
            // Zaman kolu bağlama
            if (timeLever != null)
            {
                timeLever.CanPull = () =>
                {
                    var mgr = ClimateStateManager.Instance;
                    return mgr != null && mgr.CanAdvanceTime(true);
                };
                timeLever.Pulled += () =>
                {
                    ClimateStateManager.Instance?.AdvanceTime();
                };
            }

            // Zaman modu (Ay / Yıl) anahtarı
            if (timeModeSwitch != null)
            {
                timeModeSwitch.Toggled += sw =>
                {
                    ClimateStateManager.Instance?.SetYearMode(sw.IsOn);
                    if (timeModeText != null)
                        timeModeText.text = sw.IsOn ? "MOD: <color=#FFD24D>YIL (12 Ay)</color>" : "MOD: <color=#00FFFF>AY (1 Ay)</color>";
                };
            }

            // Modül 1 butonları
            if (switchConfirmButton != null)
                switchConfirmButton.Pressed += EvaluateSwitches;

            // Modül 2 butonları
            for (int i = 0; i < numpadDigits.Length; i++)
            {
                int digit = i;
                if (numpadDigits[i] != null)
                    numpadDigits[i].Pressed += () => OnNumpadDigitPressed(digit);
            }
            if (numpadClear != null) numpadClear.Pressed += OnNumpadClear;
            if (numpadOk != null) numpadOk.Pressed += OnNumpadOk;

            // Modül 3 butonu
            if (valveConfirmButton != null)
                valveConfirmButton.Pressed += EvaluateValve;

            // Modül 4 butonu
            if (emergencyButton != null)
                emergencyButton.Pressed += OnEmergencyButtonPressed;

            // Başlangıçta tüm modülleri açık ve erişilebilir yap
            OpenAllHatches();
            RefreshNumpadDisplay();
            UpdateDeskStatus("ÇEVRE PANELİ HAZIR. Masadaki modülleri kullanabilir veya krizdeki bir ülkeye tıklayabilirsiniz.");
        }

        void Update()
        {
            // Vana basınç simülasyonu (Her zaman çalışır!)
            if (valve != null)
            {
                if (valve.IsHeld)
                {
                    currentPressure = Mathf.Clamp01(currentPressure + pressureFillSpeed * Time.deltaTime);
                }
                else
                {
                    currentPressure = Mathf.Clamp01(currentPressure - pressureLeakSpeed * Time.deltaTime);
                }

                if (pressureBar != null)
                {
                    bool inGreen = currentPressure >= greenMin && currentPressure <= greenMax;
                    Color c = inGreen ? new Color(0.2f, 1f, 0.4f) : new Color(0.9f, 0.3f, 0.2f);
                    pressureBar.Set(currentPressure, c);
                }
                if (pressureText != null)
                {
                    bool inGreen = currentPressure >= greenMin && currentPressure <= greenMax;
                    string status = inGreen ? "<color=#55FF55>HEDEF ARALIKTA! ONAYLA!</color>" : "Basılı Tutup Dengele";
                    pressureText.text = $"Basınç: %{currentPressure * 100:0}\n{status}";
                }
            }

            // Acil durum geri sayımı
            if (isEmergencyActive)
            {
                emergencyTimer -= Time.deltaTime;

                if (emergencyLight != null)
                {
                    emergencyLight.enabled = ((int)(emergencyTimer * 4f)) % 2 == 0;
                }

                if (emergencyTimerText != null)
                {
                    emergencyTimerText.text = $"ACİL DURUM!\nKALAN SÜRE: <color=#FF3333>{Mathf.Max(0f, emergencyTimer):0.0}s</color>";
                }

                if (emergencyTimer <= 0f)
                {
                    FinishEmergency(false);
                }
            }
        }

        // ============================================================ Bulmaca Başlatma / Bitirme

        public void StartPuzzle(PuzzleRequest req, Action<bool> onComplete)
        {
            currentRequest = req;
            onMainPuzzleComplete = onComplete;

            OpenAllHatches();

            switch (req.Puzzle)
            {
                case PuzzleType.Switches:
                    SetupSwitchesModule();
                    break;
                case PuzzleType.Numpad:
                    SetupNumpadModule();
                    break;
                case PuzzleType.Valve:
                    SetupValveModule();
                    break;
            }
        }

        void FinishMainPuzzle(bool success)
        {
            var cb = onMainPuzzleComplete;
            currentRequest = null;
            onMainPuzzleComplete = null;

            UpdateDeskStatus(success ? "<color=#55FF55>PROTOKOL ONAYLANDI. Kriz çözüldü.</color>" : "<color=#FF5555>PROTOKOL HATASI! Kriz devam ediyor.</color>");

            cb?.Invoke(success);
        }

        public void AbortAll()
        {
            currentRequest = null;
            onMainPuzzleComplete = null;
            isEmergencyActive = false;
            emergencyCountry = null;
            onEmergencyComplete = null;

            if (emergencyLight != null) emergencyLight.enabled = false;
            UpdateDeskStatus("Tüm işlemler durduruldu.");
        }

        public void OpenAllHatches()
        {
            if (hatchSwitches != null) hatchSwitches.SetOpen(true);
            if (hatchNumpad != null) hatchNumpad.SetOpen(true);
            if (hatchValve != null) hatchValve.SetOpen(true);
            if (hatchEmergency != null) hatchEmergency.SetOpen(true);
        }

        public void CloseAllHatches()
        {
            if (hatchSwitches != null) hatchSwitches.SetOpen(false);
            if (hatchNumpad != null) hatchNumpad.SetOpen(false);
            if (hatchValve != null) hatchValve.SetOpen(false);
            if (!isEmergencyActive && hatchEmergency != null) hatchEmergency.SetOpen(false);
        }

        void UpdateDeskStatus(string msg)
        {
            if (deskStatusText != null) deskStatusText.text = msg;
        }

        // ============================================================ MODÜL 1: ŞALTERLER

        void SetupSwitchesModule()
        {
            if (hatchSwitches != null) hatchSwitches.SetOpen(true);

            // Rastgele 4'lü hedef kombinasyon
            for (int i = 0; i < 4; i++)
            {
                targetSwitchPattern[i] = Random.value > 0.5f;
            }

            // Hedef kombinasyon metnini göster
            string targetStr = "";
            for (int i = 0; i < 4; i++)
            {
                targetStr += targetSwitchPattern[i] ? "<color=#55FF55>[▲ AÇIK]</color> " : "<color=#FF5555>[▼ KAPALI]</color> ";
            }

            if (switchTargetText != null)
                switchTargetText.text = $"HEDEF KOMBİNASYON:\n{targetStr}\nŞalterleri bu desene getirip ONAY'a basın!";

            if (switchTargetLamps != null)
            {
                for (int i = 0; i < switchTargetLamps.Length && i < 4; i++)
                {
                    if (switchTargetLamps[i] != null)
                        switchTargetLamps[i].color = targetSwitchPattern[i] ? new Color(0.2f, 1f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
                }
            }

            UpdateDeskStatus("MODÜL 1: Şalterleri ekranda belirtilen hedef desene göre dizin ve ONAY'a basın.");
        }

        void EvaluateSwitches()
        {
            if (currentRequest != null && currentRequest.Puzzle == PuzzleType.Switches)
            {
                bool allMatch = true;
                for (int i = 0; i < 4; i++)
                {
                    if (switches != null && i < switches.Length && switches[i] != null)
                    {
                        if (switches[i].IsOn != targetSwitchPattern[i])
                        {
                            allMatch = false;
                            break;
                        }
                    }
                }

                FinishMainPuzzle(allMatch);
            }
            else
            {
                Sfx.Play(SfxType.Click);
                string cur = "";
                for (int i = 0; i < 4; i++)
                {
                    if (switches != null && i < switches.Length && switches[i] != null)
                        cur += (switches[i].IsOn ? "[▲]" : "[▼]") + " ";
                }
                UpdateDeskStatus($"ŞALTER TESTİ: {cur.Trim()} (Aktif protokol bekleniyor)");
            }
        }

        // ============================================================ MODÜL 2: NUMPAD

        void SetupNumpadModule()
        {
            if (hatchNumpad != null) hatchNumpad.SetOpen(true);

            enteredNumpadCode = "";

            if (currentRequest.Choice == ChoiceType.FinancialIncentive)
            {
                targetNumpadCode = "4821";
                if (numpadProtocolLabel != null)
                    numpadProtocolLabel.text = "PROTOKOL: BÜTÇE TRANSFERİ\n(Kod: 4821)";
            }
            else
            {
                targetNumpadCode = "7395";
                if (numpadProtocolLabel != null)
                    numpadProtocolLabel.text = "PROTOKOL: SIFIR ATIK\n(Kod: 7395)";
            }

            RefreshNumpadDisplay();
            UpdateDeskStatus("MODÜL 2: 4 haneli protokol kodunu tuşlayın ve OK'e basın.");
        }

        void OnNumpadDigitPressed(int digit)
        {
            if (enteredNumpadCode.Length < 4)
            {
                enteredNumpadCode += digit.ToString();
                RefreshNumpadDisplay();
                Sfx.Play(SfxType.Click);
            }
        }

        void OnNumpadClear()
        {
            enteredNumpadCode = "";
            RefreshNumpadDisplay();
            Sfx.Play(SfxType.Click);
        }

        void OnNumpadOk()
        {
            if (currentRequest != null && currentRequest.Puzzle == PuzzleType.Numpad)
            {
                bool match = (enteredNumpadCode == targetNumpadCode);
                FinishMainPuzzle(match);
            }
            else
            {
                if (enteredNumpadCode == "4821")
                {
                    UpdateDeskStatus("<color=#55FF55>KOD [4821] DOĞRULANDI: Bütçe Transfer Protokolü Hazır.</color>");
                    Sfx.Play(SfxType.Success);
                }
                else if (enteredNumpadCode == "7395")
                {
                    UpdateDeskStatus("<color=#55FF55>KOD [7395] DOĞRULANDI: Sıfır Atık Protokolü Hazır.</color>");
                    Sfx.Play(SfxType.Success);
                }
                else
                {
                    UpdateDeskStatus($"<color=#FF5555>KOD [{enteredNumpadCode}] GEÇERSİZ. (Masadaki plakete bakın: 4821 / 7395)</color>");
                    Sfx.Play(SfxType.Fail);
                }
            }
        }

        void RefreshNumpadDisplay()
        {
            if (numpadDisplay == null) return;
            string display = "";
            for (int i = 0; i < 4; i++)
            {
                if (i < enteredNumpadCode.Length)
                    display += enteredNumpadCode[i] + " ";
                else
                    display += "_ ";
            }
            numpadDisplay.text = $"[ {display.Trim()} ]";
        }

        // ============================================================ MODÜL 3: VANA & BASINÇ

        void SetupValveModule()
        {
            if (hatchValve != null) hatchValve.SetOpen(true);
            currentPressure = 0.1f;
            UpdateDeskStatus("MODÜL 3: Vanayı basılı tutarak ibreyi YEŞİL BÖLGEDE (%60-%85) tutun ve ONAY'a basın!");
        }

        void EvaluateValve()
        {
            bool inRange = currentPressure >= greenMin && currentPressure <= greenMax;
            if (currentRequest != null && currentRequest.Puzzle == PuzzleType.Valve)
            {
                FinishMainPuzzle(inRange);
            }
            else
            {
                if (inRange)
                {
                    UpdateDeskStatus("<color=#55FF55>VANA TESTİ: Basınç yeşil aralıkta sabitlendi! (Sistem stabil)</color>");
                    Sfx.Play(SfxType.Success);
                }
                else
                {
                    UpdateDeskStatus("<color=#FF5555>VANA TESTİ: Basınç yeşil aralıkta (%60-%85) değil!</color>");
                    Sfx.Play(SfxType.Fail);
                }
            }
        }

        // ============================================================ MODÜL 4: ACİL DURUM

        public bool StartEmergency(CountryNode country, Action<bool> callback)
        {
            if (isEmergencyActive) return false;

            isEmergencyActive = true;
            emergencyCountry = country;
            onEmergencyComplete = callback;
            emergencyTimer = emergencyDuration;

            if (hatchEmergency != null) hatchEmergency.SetOpen(true);
            if (emergencyLight != null) emergencyLight.enabled = true;

            Sfx.Play(SfxType.Alarm);
            return true;
        }

        void OnEmergencyButtonPressed()
        {
            if (isEmergencyActive)
            {
                FinishEmergency(true);
            }
            else
            {
                UpdateDeskStatus("ACİL DURUM BUTONU: Test edildi. (Aktif kriz yok)");
                Sfx.Play(SfxType.Click);
            }
        }

        void FinishEmergency(bool success)
        {
            var cb = onEmergencyComplete;
            isEmergencyActive = false;
            emergencyCountry = null;
            onEmergencyComplete = null;

            if (emergencyLight != null) emergencyLight.enabled = false;

            cb?.Invoke(success);
        }
    }
}
