using System;
using UnityEngine;

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// İklim Bakanlığı Ana Karar, Sürekli İklim Erimesi, 15 Sn Otomatik Bütçe ve Otomatik Gün Geçiş Yöneticisi:
    /// - Gün ilerlemesi SADECE o güne ait tüm kriz/görev dosyaları tamamlandığında otomatik olarak gerçekleşir.
    /// - Gün bittiğinde Siyah Ekran (#000000) "GÜN 2" CanvasGroup Fade-In / 2.5sn Bekleme / Fade-Out geçişi oynatılır.
    /// </summary>
    public class ClimateOperatorManager : MonoBehaviour
    {
        public static ClimateOperatorManager Instance { get; private set; }

        [Header("V4 Zorlu Başlangıç Bakanlık Değerleri")]
        public int startMoney = 55;
        public float startHalk = 48f;
        public float startIklim = 42f;
        public int startAuthority = 3;

        [Header("V4 Hızlanan İklim Çöküşü (Gün 1: 0.70%/s -> Gün 12: 1.80%/s)")]
        public float baseClimateDecayPerSecond = 0.70f;
        public float dailyDecayEscalation = 0.10f;
        public float extraDecayPerActiveCrisis = 0.15f;

        [Header("15 Saniyede Bir Kısıtlı Bütçe Akışı ($8 + Kamuoyu * 0.18)")]
        public float fundingInterval = 15f;

        [Header("Gün / Tur ve İmza Yetkisi")]
        public int targetDays = 12;
        public int maxDailySignatures = 3;

        [Header("Bakanlık Müdahale Maliyetleri")]
        public int budgetSupportCost = 22;
        public int auditAuthorityCost = 2;

        // 3 Ana Üst Bar Göstergesi + Otorite & Gün
        public int Money { get; private set; }
        public float Halk { get; private set; }
        public float Iklim { get; private set; }
        public int Authority { get; private set; }

        public int CurrentDay { get; private set; } = 1;
        public int RemainingSignatures { get; private set; } = 3;
        public int DailyTasksTarget { get; private set; } = 3;
        public int DailyTasksCompleted { get; private set; } = 0;
        public bool IsDayTransitioning { get; private set; }

        public GameState State { get; private set; } = GameState.Playing;
        public string EndReason { get; private set; } = "";

        public int ResolvedCount { get; private set; }
        public int DeferredCount { get; private set; }
        public int MissedCount { get; private set; }
        public string LastMessage { get; private set; } = "";
        public float LastMessageTime { get; private set; } = -100f;

        float fundingTimer;

        public float FundingProgress01 => fundingInterval > 0.01f ? Mathf.Clamp01(fundingTimer / fundingInterval) : 0f;
        public float FundingSecondsLeft => Mathf.Max(0f, fundingInterval - fundingTimer);
        public int NextFundingAmount => Mathf.RoundToInt(8f + (Halk * 0.18f));

        void OnEnable()
        {
            Instance = this;
        }

        void Awake()
        {
            Instance = this;
            ResetMinistryState();
        }

        public void ResetMinistryState()
        {
            Money = startMoney;
            Halk = startHalk;
            Iklim = startIklim;
            Authority = startAuthority;
            CurrentDay = 1;
            RemainingSignatures = maxDailySignatures;
            DailyTasksTarget = 3;
            DailyTasksCompleted = 0;
            IsDayTransitioning = false;
            State = GameState.Playing;
            EndReason = "";
            ResolvedCount = 0;
            DeferredCount = 0;
            MissedCount = 0;
            fundingTimer = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            Post("GÜN 1 — İklim Bakanlığı sistemi aktif. Beyanları ve uydu telemetrisini karşılaştırarak karar verin.");
        }

        void Update()
        {
            if (State != GameState.Playing || IsDayTransitioning) return;

            float dt = Time.deltaTime;

            // 1. V4 HIZLANAN KÜRESEL ÇÖKÜŞ VE ÇOKLU KRİZ BASKISI
            int activeCrises = ContinentCrisisSpawner.Instance != null
                ? ContinentCrisisSpawner.Instance.GetActiveCrisisCount()
                : 0;

            float dayDecayBase = baseClimateDecayPerSecond + (CurrentDay - 1) * dailyDecayEscalation;
            float decayPerSec = dayDecayBase + activeCrises * extraDecayPerActiveCrisis;
            Iklim = Mathf.Clamp(Iklim - decayPerSec * dt, 0f, 100f);

            // Çözülmeden bekleyen her kriz kamuoyu desteğini de zamanla eritir
            if (activeCrises > 0)
            {
                Halk = Mathf.Clamp(Halk - (activeCrises * 0.10f) * dt, 0f, 100f);
            }

            // 2. HER 15 SANİYEDE BİR OTOMATİK BÜTÇE AKIŞI (TIMED FUNDING TICK)
            fundingTimer += dt;
            if (fundingTimer >= fundingInterval)
            {
                fundingTimer -= fundingInterval;
                int payout = NextFundingAmount;
                Money += payout;
                Sfx.Play(SfxType.Click);
                HudController.Instance?.ShowFloatingBudgetPopup($"+${payout} Kamuoyu Fonu", isNegative: false);
            }

            if (Iklim <= 0f || Halk <= 0f)
            {
                CheckEndConditions();
            }
        }

        public void Post(string msg)
        {
            LastMessage = msg;
            LastMessageTime = Time.time;
            Debug.Log($"[İKLİM BAKANLIĞI] {msg}");
        }

        // ============================================================ 1. OTOMATİK GÜN SONU VE SİYAH EKRAN GEÇİŞİ

        public void ConsumeSignature()
        {
            if (State != GameState.Playing) return;
            RemainingSignatures = Mathf.Max(0, RemainingSignatures - 1);
        }

        public void CheckAndTriggerEndOfDayIfAllResolved()
        {
            if (State != GameState.Playing || IsDayTransitioning) return;

            int activeCrises = ContinentCrisisSpawner.Instance != null
                ? ContinentCrisisSpawner.Instance.GetActiveCrisisCount()
                : 0;

            // KURAL 5: Bir günün ilerlemesi SADECE o güne ait tüm (3) kriz/görev dosyaları tamamlandığında gerçekleşir.
            // Görevler bitmeden ASLA diğer güne geçilmez!
            if (DailyTasksCompleted >= DailyTasksTarget && activeCrises <= 0)
            {
                AdvanceDay();
            }
        }

        public void AdvanceDay()
        {
            if (State != GameState.Playing || IsDayTransitioning) return;

            CheckEndConditions();
            if (State != GameState.Playing) return;

            int nextDay = CurrentDay + 1;
            if (nextDay > targetDays)
            {
                CurrentDay = nextDay;
                State = GameState.Won;
                EndReason =
                    $"TEBRİKLER SAYIN BAKAN! {targetDays} günlük görev sürenizi Küresel İklimi (%{Iklim:0}) ve " +
                    $"Kamuoyu Desteğini (%{Halk:0}) koruyarak başarıyla tamamladınız.";
                Sfx.Play(SfxType.Success);
                PlayerInteractor.LockCursor(false);
                return;
            }

            IsDayTransitioning = true;
            BlindChoiceDialogueUI.Instance?.Close();

            if (HudController.Instance != null)
            {
                HudController.Instance.PlayDayTransition(
                    nextDay,
                    onBlackScreenMidpoint: () => ApplyNewDayState(nextDay),
                    onTransitionFinished: () =>
                    {
                        IsDayTransitioning = false;
                        CheckEndConditions();
                    });
            }
            else
            {
                ApplyNewDayState(nextDay);
                IsDayTransitioning = false;
            }
        }

        void ApplyNewDayState(int newDay)
        {
            if (ContinentCrisisSpawner.Instance != null)
            {
                ContinentCrisisSpawner.Instance.ProcessEndOfDayCrises();
            }

            CurrentDay = newDay;
            RemainingSignatures = maxDailySignatures;
            DailyTasksTarget = 3;
            DailyTasksCompleted = 0;
            Authority = Mathf.Clamp(Authority + 1, 0, 15);

            Sfx.Play(SfxType.Click);
            Post($"GÜN {CurrentDay} BAŞLADI — Yeni günün görev dosyaları masanıza iletildi.");

            if (ContinentCrisisSpawner.Instance != null)
            {
                ContinentCrisisSpawner.Instance.OnDayAdvanced(CurrentDay);
            }
        }

        // ============================================================ 2. SEÇİM VE KARAR

        public bool CanAffordContinentChoice(BlindChoice choice, int choiceIndex)
        {
            if (State != GameState.Playing || choice == null) return false;

            if (choiceIndex == 0 && choice.iklimDelta > 0 && Authority < 1)
            {
                Sfx.Play(SfxType.Fail);
                Post("Yetersiz Siyasi Otorite! Bu karar için en az 1 Otorite gerekiyor.");
                return false;
            }

            int cost = choice.moneyDelta < 0 ? -choice.moneyDelta : 0;
            if (cost > 0 && Money < cost)
            {
                Sfx.Play(SfxType.Fail);
                Post($"Yetersiz Bakanlık Bütçesi! Bu karar için ${cost} gerekiyor (Mevcut: ${Money}).");
                return false;
            }

            return true;
        }

        public bool ApplyContinentChoice(
            WorldMapMonitorController.ContinentState cont,
            BlindChoice choice,
            int choiceIndex,
            bool wasDeferredDoubled)
        {
            if (!CanAffordContinentChoice(choice, choiceIndex) || cont == null) return false;

            float mult = wasDeferredDoubled ? 2.0f : 1.0f;

            int authDelta = 0;
            if (choiceIndex == 0 && choice.iklimDelta > 0)
            {
                authDelta = -1;
            }
            else if (choiceIndex == 2 && choice.iklimDelta < 0)
            {
                authDelta = +1;
            }

            Authority = Mathf.Clamp(Authority + authDelta, 0, 15);
            Money = Mathf.Max(0, Money + choice.moneyDelta);
            Halk = Mathf.Clamp(Halk + choice.halkDelta * mult, 0f, 100f);
            Iklim = Mathf.Clamp(Iklim + choice.iklimDelta * mult, 0f, 100f);

            cont.health = Mathf.Clamp(cont.health + choice.continentHealthDelta * mult, 0f, 100f);
            cont.anger = Mathf.Clamp(cont.anger + choice.angerDelta, 0f, 100f);

            if (choice.angerDelta < 0f)
                cont.loyalty = Mathf.Clamp(cont.loyalty + Mathf.Abs(choice.angerDelta) * 0.8f, 0f, 100f);
            else if (choice.angerDelta > 0f && !cont.isPuppet)
                cont.loyalty = Mathf.Clamp(cont.loyalty - choice.angerDelta * 0.4f, 0f, 100f);

            ResolvedCount++;
            DailyTasksCompleted++;
            ConsumeSignature();

            Sfx.Play(choice.iklimDelta >= 0 ? SfxType.Success : SfxType.Alarm);
            Post(choice.outcomeSummary);

            WorldMapMonitorController.Instance?.RefreshContinentVisuals(cont.type);
            CheckEndConditions();
            CheckAndTriggerEndOfDayIfAllResolved();

            return true;
        }

        // ============================================================ 3. GİZLİ ÇELİŞKİ & YAPTIRIM MİNİ-OYUNU SONUÇLARI

        public bool ApplyContradictionSanction(WorldMapMonitorController.ContinentState cont, bool wasDeferredDoubled)
        {
            if (State != GameState.Playing || cont == null) return false;

            float mult = wasDeferredDoubled ? 1.5f : 1.0f;

            Authority = Mathf.Clamp(Authority + 1, 0, 15);
            Money += 40;
            Iklim = Mathf.Clamp(Iklim + 10f * mult, 0f, 100f);
            Halk = Mathf.Clamp(Halk + 8f * mult, 0f, 100f);

            cont.health = Mathf.Clamp(cont.health + 22f, 0f, 100f);
            cont.loyalty = Mathf.Clamp(cont.loyalty + 30f, 0f, 100f);
            cont.anger = Mathf.Clamp(cont.anger - 20f, 0f, 100f);

            ResolvedCount++;
            DailyTasksCompleted++;
            ConsumeSignature();

            Sfx.Play(SfxType.Success);
            HudController.Instance?.ShowFloatingBudgetPopup("+$40 Tazminat Geliri", isNegative: false);
            Post($"YAPTIRIM BAŞARILI — {cont.name}: Yalan beyan kanıtlandı! İklim +%10, Kamuoyu +%8 ve +$40 Tazminat kazanıldı.");

            WorldMapMonitorController.Instance?.RefreshContinentVisuals(cont.type);
            CheckEndConditions();
            CheckAndTriggerEndOfDayIfAllResolved();

            return true;
        }

        public void ApplyFailedMiniGamePenalty(WorldMapMonitorController.ContinentState cont)
        {
            if (State != GameState.Playing) return;

            MissedCount++;
            Iklim = Mathf.Clamp(Iklim - 8f, 0f, 100f);
            Halk = Mathf.Clamp(Halk - 8f, 0f, 100f);

            if (cont != null)
            {
                cont.health = Mathf.Clamp(cont.health - 12f, 0f, 100f);
                cont.anger = Mathf.Clamp(cont.anger + 15f, 0f, 100f);
                WorldMapMonitorController.Instance?.RefreshContinentVisuals(cont.type);
            }

            DailyTasksCompleted++;
            ConsumeSignature();
            Sfx.Play(SfxType.Fail);
            Post($"KANIT KARARTILDI — {cont?.name}: Lobi avukatları dosyayı kapattı! İklim ve Kamuoyu düştü.");
            CheckEndConditions();
            CheckAndTriggerEndOfDayIfAllResolved();
        }

        public void ApplyFalseAccusationPenalty(WorldMapMonitorController.ContinentState cont)
        {
            if (State != GameState.Playing) return;

            Halk = Mathf.Clamp(Halk - 12f, 0f, 100f);
            Money = Mathf.Max(0, Money - 25);
            Authority = Mathf.Max(0, Authority - 1);

            if (cont != null)
            {
                cont.anger = Mathf.Clamp(cont.anger + 20f, 0f, 100f);
                cont.loyalty = Mathf.Clamp(cont.loyalty - 15f, 0f, 100f);
            }

            Sfx.Play(SfxType.Fail);
            HudController.Instance?.ShowFloatingBudgetPopup("-$25 Asılsız Denetim Cezası", isNegative: true);
            Post($"ASILSIZ DENETİM SKANDALI — {cont?.name}: Uydu verileri beyanın doğru olduğunu kanıtladı! Kamuoyu -%12, Bütçe -$25.");
            CheckEndConditions();
        }

        // ============================================================ 4. KRİZİ ERTELE (3 GÜN SONRA 2X DÖNER)

        public void RegisterCrisisDeferred(WorldMapMonitorController.ContinentState cont)
        {
            if (State != GameState.Playing || cont == null) return;

            DeferredCount++;
            DailyTasksCompleted++;
            ConsumeSignature();

            Sfx.Play(SfxType.Lever);
            Post($"{cont.name}: Kriz dosyası ertelendi. 3 gün sonra (GÜN {CurrentDay + 3}) iki katı etkiyle Bakanlık gündemine dönecek!");

            CheckAndTriggerEndOfDayIfAllResolved();
        }

        // ============================================================ 5. ALT BAKANLIK MÜDAHALELERİ

        public bool TryProvideBudgetSupport(WorldMapMonitorController.ContinentState cont)
        {
            if (State != GameState.Playing || cont == null) return false;

            if (Money < budgetSupportCost)
            {
                Sfx.Play(SfxType.Fail);
                Post($"Yetersiz Bakanlık Bütçesi! Bütçe desteği için ${budgetSupportCost} gerekiyor.");
                return false;
            }

            Money -= budgetSupportCost;
            cont.anger = Mathf.Clamp(cont.anger - 35f, 0f, 100f);
            cont.loyalty = Mathf.Clamp(cont.loyalty + 30f, 0f, 100f);
            cont.health = Mathf.Clamp(cont.health + 8f, 0f, 100f);
            Iklim = Mathf.Clamp(Iklim + 5f, 0f, 100f);
            Halk = Mathf.Clamp(Halk + 4f, 0f, 100f);

            Sfx.Play(SfxType.Success);
            Post($"{cont.name} yönetimine ${budgetSupportCost} Bakanlık bütçe desteği sağlandı; uyum arttı.");
            WorldMapMonitorController.Instance?.RefreshContinentVisuals(cont.type);
            CheckEndConditions();
            return true;
        }

        public bool TryEnforceAuditPressure(WorldMapMonitorController.ContinentState cont)
        {
            if (State != GameState.Playing || cont == null) return false;

            if (Authority < auditAuthorityCost)
            {
                Sfx.Play(SfxType.Fail);
                Post($"Yetersiz Siyasi Otorite! Denetim baskısı için {auditAuthorityCost} Otorite gerekiyor (Mevcut: {Authority}).");
                return false;
            }

            Authority -= auditAuthorityCost;
            cont.isPuppet = true;
            cont.loyalty = 100f;
            cont.anger = 0f;
            cont.health = Mathf.Clamp(cont.health + 15f, 0f, 100f);
            Iklim = Mathf.Clamp(Iklim + 7f, 0f, 100f);
            Halk = Mathf.Clamp(Halk + 5f, 0f, 100f);

            Sfx.Play(SfxType.Success);
            Post($"{cont.name}: Bakanlık Denetim Raporu ile siyasi baskı kuruldu! Tam uyuma geçildi.");
            WorldMapMonitorController.Instance?.RefreshContinentVisuals(cont.type);
            CheckEndConditions();
            return true;
        }

        public void ApplyUnresolvedCrisisPenalty(string continentName, string crisisTitle, bool wasDeferredDoubled)
        {
            if (State != GameState.Playing) return;

            float mult = wasDeferredDoubled ? 2.0f : 1.0f;
            MissedCount++;
            Halk = Mathf.Clamp(Halk - 10f * mult, 0f, 100f);
            Iklim = Mathf.Clamp(Iklim - 11f * mult, 0f, 100f);

            ConsumeSignature();
            Sfx.Play(SfxType.Fail);
            Post($"KARAR SÜRESİ DOLDU — {continentName} '{crisisTitle}' zamanında çözülmedi!");
            CheckEndConditions();
            CheckAndTriggerEndOfDayIfAllResolved();
        }

        void CheckEndConditions()
        {
            if (State != GameState.Playing) return;

            if (Halk <= 0f)
            {
                State = GameState.Lost;
                EndReason = "KAMUOYU DESTEĞİ ÇÖKTÜ (%0): Halk protestoları ve güvenoyu kaybı sonucu İklim Bakanlığı kabinesi düştü.";
                Sfx.Play(SfxType.Fail);
                PlayerInteractor.LockCursor(false);
            }
            else if (Iklim <= 0f)
            {
                State = GameState.Lost;
                EndReason = "EKOLOJİK ÇÖKÜŞ (%0): Küresel iklim dengesi sıfırlandı; geri döndürülemez biyosfer felaketi gerçekleşti.";
                Sfx.Play(SfxType.Fail);
                PlayerInteractor.LockCursor(false);
            }
            // V4 KURAL 3.1: Erken zafer İPTAL! Zafer SADECE 12. Gün sonunda AdvanceDay() ile kazanılabilir.
        }
    }
}
