using System;
using UnityEngine;

namespace Pixamor
{
    public enum SessionState { Running, Won, Lost }

    // Vardiyanın süresini ve kaynaklarını tek noktadan yönetir.
    public class GameSession : MonoBehaviour
    {
        [Header("Başlangıç kaynakları")]
        [SerializeField, Min(0)] private int startingMoney = 60;
        [SerializeField, Range(0, 100)] private int startingHappiness = 70;
        [SerializeField, Range(0, 100)] private int startingClimate = 70;

        [Header("Vardiya ve bütçe (saniye)")]
        [SerializeField, Min(1f)] private float shiftDuration = 360f;
        [SerializeField, Min(1f)] private float incomeInterval = 60f;
        [SerializeField, Min(0)] private int incomeAmount = 30;

        public int Money { get; private set; }
        public int IncomeAmount => incomeAmount;
        public int Happiness { get; private set; }
        public int Climate { get; private set; }
        public float RemainingTime { get; private set; }
        public float ElapsedTime { get; private set; }
        public float TimeUntilIncome => Mathf.Max(0f, incomeInterval - incomeTimer);
        public bool IsPaused { get; private set; }
        public SessionState State { get; private set; }
        public string EndReason { get; private set; }
        public bool HasHappinessWarning => Happiness < 25;
        public bool HasClimateWarning => Climate < 25;

        // Gerçek arayüz daha sonra bu bildirimlere abone olabilir.
        public event Action Changed;
        public event Action<SessionState> Ended;
        public event Action Restarted;

        // Kriz sayaçları ileride bu ortak oyun zamanını kullanacak.
        // Duraklatıldığında bu bildirim gönderilmez.
        public event Action<float> TimeAdvanced;

        private float incomeTimer;

        private void Awake()
        {
            RestartSession();
        }

        public void RestartSession()
        {
            Money = Mathf.Max(0, startingMoney);
            Happiness = Mathf.Clamp(startingHappiness, 0, 100);
            Climate = Mathf.Clamp(startingClimate, 0, 100);
            RemainingTime = Mathf.Max(1f, shiftDuration);
            ElapsedTime = 0f;
            incomeTimer = 0f;
            IsPaused = false;
            State = SessionState.Running;
            EndReason = string.Empty;
            Restarted?.Invoke();
            CheckForLoss();
            Changed?.Invoke();
        }

        private void Update()
        {
            if (State != SessionState.Running || IsPaused || RemainingTime <= 0f)
                return;

            // Son karede vardiya süresinin dışına taşan zamanı saymayız.
            float step = Mathf.Min(Time.deltaTime, RemainingTime);
            RemainingTime = Mathf.Max(0f, RemainingTime - step);
            ElapsedTime += step;
            incomeTimer += step;

            // Uzun bir karede birden fazla ödeme zamanı geçerse hepsini uygula.
            float interval = Mathf.Max(1f, incomeInterval);
            while (incomeTimer >= interval)
            {
                incomeTimer -= interval;
                Money += incomeAmount;
            }

            TimeAdvanced?.Invoke(step);
            Changed?.Invoke();
        }

        private void LateUpdate()
        {
            if (State != SessionState.Running)
                return;

            // Aynı karede hem süre biter hem kaynak sıfırlanırsa kayıp önceliklidir.
            CheckForLoss();
            if (State == SessionState.Running && RemainingTime <= 0f)
                Finish(SessionState.Won, "Vardiya tamamlandı. Halk ve iklim korundu.");
        }

        public void SetPaused(bool paused)
        {
            if (State != SessionState.Running || IsPaused == paused)
                return;

            // Time.timeScale yerine ortak oyun saatini durduruyoruz.
            // Kart animasyonları ve arayüz çalışmaya devam edebilir.
            IsPaused = paused;
            Changed?.Invoke();
        }

        public bool CanAfford(int cost)
        {
            return cost >= 0 && Money >= cost;
        }

        // Maliyet pozitif verilir: 25, paradan 25 düşürür.
        // Kart açıkken de karar verilebilmesi için IsPaused kontrol edilmez.
        public bool TryApplyIntervention(int cost, int happinessChange, int climateChange)
        {
            if (State != SessionState.Running || !CanAfford(cost))
                return false;

            Money -= cost;
            ApplyResourceChanges(happinessChange, climateChange);
            return true;
        }

        // Kaçırılan kriz gibi para gerektirmeyen sonuçlar için de kullanılabilir.
        public void ApplyResourceChanges(int happinessChange, int climateChange)
        {
            if (State != SessionState.Running)
                return;

            Happiness = Mathf.Clamp(Happiness + happinessChange, 0, 100);
            Climate = Mathf.Clamp(Climate + climateChange, 0, 100);
            CheckForLoss();
            Changed?.Invoke();
        }

        private void CheckForLoss()
        {
            if (Happiness <= 0)
                Finish(SessionState.Lost, "Kitlesel isyan: halk mutluluğu sıfıra ulaştı.");
            else if (Climate <= 0)
                Finish(SessionState.Lost, "Krizler kontrolden çıktı: iklim dengesi sıfıra ulaştı.");
        }

        private void Finish(SessionState result, string reason)
        {
            // Bitiş yalnızca bir kez uygulanır.
            if (State != SessionState.Running)
                return;

            State = result;
            EndReason = reason;
            IsPaused = false;
            Ended?.Invoke(State);
            Changed?.Invoke();
        }
    }
}
