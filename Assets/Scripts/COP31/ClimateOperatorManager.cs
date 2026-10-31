using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Pixamor.COP31
{
    public sealed class ActiveCrisis
    {
        public CrisisEvent Data;
        public WorldMapPin Pin;
        public float TimeRemaining;

        public ActiveCrisis(CrisisEvent data, WorldMapPin pin)
        {
            Data = data;
            Pin = pin;
            TimeRemaining = data.duration;
        }
    }

    /// <summary>
    /// İklim Operatörü Oyun Yöneticisi:
    /// - 6 dakikalık vardiya (360 sn) sayacı.
    /// - Para (Başlangıç 60$), Halk (Başlangıç 70), İklim (Başlangıç 70).
    /// - Her 60 saniyede +30$ düzenli bütçe.
    /// - 12 somut kriz olayı, 40 saniye müdahale süresi (dolunca 3. seçenek otomatik uygulanır).
    /// - Kazanma: İklim = 100 veya 6 dk sonunda Halk > 0 ve İklim > 0.
    /// - Kaybetme: Halk = 0 veya İklim = 0.
    /// </summary>
    public class ClimateOperatorManager : MonoBehaviour
    {
        public static ClimateOperatorManager Instance { get; private set; }

        [Header("Başlangıç Kaynakları")]
        public int startMoney = 60;
        public int startHalk = 70;
        public int startIklim = 70;
        public float shiftDuration = 360f; // 6 dakika

        [Header("Düzenli Bütçe")]
        public int periodicIncomeAmount = 30;
        public float periodicIncomeInterval = 60f;
        public bool useLegacyEvents = false;

        // Durum Değişkenleri
        public int Money { get; private set; }
        public int Halk { get; private set; }
        public int Iklim { get; private set; }
        public float ShiftTimeRemaining { get; private set; }
        public GameState State { get; private set; } = GameState.Playing;
        public string EndReason { get; private set; } = "";

        public int ResolvedCount { get; private set; }
        public int MissedCount { get; private set; }
        public string LastMessage { get; private set; } = "";
        public float LastMessageTime { get; private set; } = -100f;

        readonly Dictionary<int, WorldMapPin> registeredPins = new Dictionary<int, WorldMapPin>();
        readonly Dictionary<int, ActiveCrisis> activeCrises = new Dictionary<int, ActiveCrisis>();
        CrisisEvent[] allEvents;
        readonly HashSet<int> spawnedEvents = new HashSet<int>();

        float nextIncomeTime;
        float shiftElapsed;
        bool lowHalkAlarmPlayed;
        bool lowIklimAlarmPlayed;

        public float ShiftProgress01 => Mathf.Clamp01(1f - (ShiftTimeRemaining / shiftDuration));
        public string ShiftClockString
        {
            get
            {
                int totalSec = Mathf.Max(0, Mathf.CeilToInt(ShiftTimeRemaining));
                int minutes = totalSec / 60;
                int seconds = totalSec % 60;
                return $"{minutes:00}:{seconds:00}";
            }
        }

        void Awake()
        {
            Instance = this;
            Money = startMoney;
            Halk = startHalk;
            Iklim = startIklim;
            ShiftTimeRemaining = shiftDuration;
            nextIncomeTime = periodicIncomeInterval;
            allEvents = CrisisDatabase.GetEvents();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterPin(int eventId, WorldMapPin pin)
        {
            registeredPins[eventId] = pin;
        }

        public void Post(string msg)
        {
            LastMessage = msg;
            LastMessageTime = Time.time;
            Debug.Log($"[İklim Operatörü] {msg}");
        }

        void Update()
        {
            if (State != GameState.Playing)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }

            float dt = Time.deltaTime;
            shiftElapsed += dt;
            ShiftTimeRemaining = Mathf.Max(0f, ShiftTimeRemaining - dt);

            // 1. Düzenli Bütçe Ödemesi (Her 60 saniyede bir +30$)
            if (shiftElapsed >= nextIncomeTime)
            {
                Money += periodicIncomeAmount;
                nextIncomeTime += periodicIncomeInterval;
                Post($"DÜZENLİ BÜTÇE: +{periodicIncomeAmount}$ aktarıldı. (Toplam: {Money}$)");
                Sfx.Play(SfxType.Success, 0.8f);
                EventCardUI.Instance?.RefreshChoices();
            }

            // 2. Kriz Olaylarının Çıkması (Yalnızca legacy mod aktifse)
            if (useLegacyEvents && allEvents != null)
            {
                foreach (var ev in allEvents)
                {
                    if (!spawnedEvents.Contains(ev.id) && shiftElapsed >= ev.spawnTime)
                    {
                        SpawnCrisis(ev);
                    }
                }
            }

            // 3. Aktif Krizlerin Geri Sayımı (40 sn)
            var toExpire = new List<ActiveCrisis>();
            foreach (var kvp in activeCrises)
            {
                var crisis = kvp.Value;
                crisis.TimeRemaining -= dt;
                crisis.Pin?.Tick(dt);

                if (EventCardUI.Instance != null && EventCardUI.Instance.IsOpen && EventCardUI.Instance.CurrentEventId == crisis.Data.id)
                {
                    EventCardUI.Instance.UpdateTimer(crisis.TimeRemaining);
                }

                if (crisis.TimeRemaining <= 0f)
                {
                    toExpire.Add(crisis);
                }
            }

            // Süresi dolan olaylar: Otomatik 3. seçenek (müdahale etmeme) uygulanır
            foreach (var exp in toExpire)
            {
                OnCrisisExpired(exp);
            }

            // 4. Kritik Eşik Uyarıları (< 25)
            CheckThresholdAlarms();

            // 5. Kazanma ve Kaybetme Kontrolü
            CheckGameOverConditions();
        }

        void SpawnCrisis(CrisisEvent ev)
        {
            spawnedEvents.Add(ev.id);
            if (registeredPins.TryGetValue(ev.id, out WorldMapPin pin))
            {
                pin.Activate(ev.duration);
                var active = new ActiveCrisis(ev, pin);
                activeCrises[ev.id] = active;
                Sfx.Play(SfxType.Alarm);
                Post($"KRİZ ALARMI! {ev.title} ({ev.locationName}) - 40s müdahale süresi başladı!");
            }
        }

        void OnCrisisExpired(ActiveCrisis crisis)
        {
            MissedCount++;
            int lastIdx = crisis.Data.choices.Length - 1; // 3. seçenek (müdahale etmeme)
            var autoChoice = crisis.Data.choices[lastIdx];

            // Etkileri uygula
            Halk = Mathf.Clamp(Halk + autoChoice.halkDelta, 0, 100);
            Iklim = Mathf.Clamp(Iklim + autoChoice.iklimDelta, 0, 100);

            if (crisis.Pin != null) crisis.Pin.Deactivate();
            activeCrises.Remove(crisis.Data.id);

            if (EventCardUI.Instance != null && EventCardUI.Instance.CurrentEventId == crisis.Data.id)
            {
                EventCardUI.Instance.Close();
            }

            Sfx.Play(SfxType.Fail);
            Post($"SÜRE DOLDU! {crisis.Data.title}: Müdahale edilmedi. (Halk: {autoChoice.halkDelta}, İklim: {autoChoice.iklimDelta})");
        }

        public void OpenEventCard(CrisisEvent data)
        {
            if (data == null || !activeCrises.TryGetValue(data.id, out ActiveCrisis active)) return;
            EventCardUI.Instance?.Open(data, active.TimeRemaining);
        }

        public void ApplyChoice(CrisisEvent data, int choiceIndex)
        {
            if (data == null || !activeCrises.TryGetValue(data.id, out ActiveCrisis active)) return;
            if (choiceIndex < 0 || choiceIndex >= data.choices.Length) return;

            var choice = data.choices[choiceIndex];

            // Bütçe kontrolü
            if (Money + choice.moneyDelta < 0 && choice.moneyDelta < 0)
            {
                Post("Yetersiz bütçe! Bu müdahale gerçekleştirilemedi.");
                Sfx.Play(SfxType.Fail);
                return;
            }

            // Kaynakları uygula
            Money += choice.moneyDelta;
            Halk = Mathf.Clamp(Halk + choice.halkDelta, 0, 100);
            Iklim = Mathf.Clamp(Iklim + choice.iklimDelta, 0, 100);

            ResolvedCount++;
            if (active.Pin != null) active.Pin.Deactivate();
            activeCrises.Remove(data.id);

            string mDelta = choice.moneyDelta != 0 ? $"{choice.moneyDelta}$" : "0$";
            string hDelta = (choice.halkDelta >= 0 ? "+" : "") + choice.halkDelta;
            string iDelta = (choice.iklimDelta >= 0 ? "+" : "") + choice.iklimDelta;

            Post($"{data.title}: '{choice.title}' uygulandı. (Para: {mDelta}, Halk: {hDelta}, İklim: {iDelta})");
            Sfx.Play(SfxType.Success);
        }

        public void AddFunds(int amount)
        {
            Money = Mathf.Max(0, Money + amount);
        }

        public void ApplyChoiceEffects(int moneyDelta, int halkDelta, int iklimDelta, string outcomeSummary)
        {
            Money += moneyDelta;
            Halk = Mathf.Clamp(Halk + halkDelta, 0, 100);
            Iklim = Mathf.Clamp(Iklim + iklimDelta, 0, 100);
            ResolvedCount++;
            if (!string.IsNullOrEmpty(outcomeSummary))
            {
                Post(outcomeSummary);
            }
            Sfx.Play(SfxType.Success);
        }

        void CheckThresholdAlarms()
        {
            if (Halk < 25 && !lowHalkAlarmPlayed)
            {
                lowHalkAlarmPlayed = true;
                Post("<color=#FF4444><b>DİKKAT: Halk memnuniyeti %25 altına düştü! Protestolar başladı!</b></color>");
                Sfx.Play(SfxType.Alarm);
            }
            else if (Halk >= 25)
            {
                lowHalkAlarmPlayed = false;
            }

            if (Iklim < 25 && !lowIklimAlarmPlayed)
            {
                lowIklimAlarmPlayed = true;
                Post("<color=#FF4444><b>DİKKAT: İklim dengesi %25 altına düştü! Ekolojik felaket eşikte!</b></color>");
                Sfx.Play(SfxType.Alarm);
            }
            else if (Iklim >= 25)
            {
                lowIklimAlarmPlayed = false;
            }
        }

        void CheckGameOverConditions()
        {
            if (State != GameState.Playing) return;

            // 1. İklim 100: Erken Zafer (Ütopya)
            if (Iklim >= 100)
            {
                EndGame(GameState.Won, "İKLİM DENGESİ %100! Küresel iklim krizi tamamen çözüldü, Ütopya kuruldu!");
                return;
            }

            // 2. Halk = 0: Kayıp (Halk İsyanı)
            if (Halk <= 0)
            {
                EndGame(GameState.Lost, "KİTLESEL İSYAN! Halkın güveni ve sabrı sıfırlandı. Yönetim alaşağı edildi!");
                return;
            }

            // 3. İklim = 0: Kayıp (Ekolojik Çöküş)
            if (Iklim <= 0)
            {
                EndGame(GameState.Lost, "EKOLOJİK ÇÖKÜŞ! İklim dengesi sıfırlandı. Dünya geri dönüşü olmayan kaosa sürüklendi!");
                return;
            }

            // 4. Vardiya Süresi Doldu (360 sn)
            if (ShiftTimeRemaining <= 0f)
            {
                if (Halk > 0 && Iklim > 0)
                {
                    EndGame(GameState.Won, "VARDİYA TAMAMLANDI! 6 dakika boyunca halkı ve iklim dengesini başarıyla korudunuz.");
                }
                else
                {
                    EndGame(GameState.Lost, "Vardiya bittiğinde kritik dengeler sağlanamadı.");
                }
            }
        }

        void EndGame(GameState result, string reason)
        {
            State = result;
            EndReason = reason;
            EventCardUI.Instance?.Close();
            Post(reason);
            Sfx.Play(result == GameState.Won ? SfxType.Success : SfxType.Fail);
        }
    }
}
