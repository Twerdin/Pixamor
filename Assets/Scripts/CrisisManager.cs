using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixamor
{
    [RequireComponent(typeof(GameSession))]
    public class CrisisManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float firstCrisisTime = 5f;
        // Pozitif değer yalnızca sabit zaman çizelgeli regresyon testlerinde kullanılır.
        private float spawnInterval = 0f;
        private bool repeatCrises = true;
        private float cycleStart;
        private int nextId;
        private bool previousBurst;
        private float[] spawnOffsets;
        public void ConfigureTempo() { firstCrisisTime=5f; }

        private GameSession session;
        private CrisisData[] catalog;
        private bool shuffleOnRestart = true;
        private readonly System.Random random = new System.Random();
        private string lastOpeningCrisis;
        private readonly List<ActiveCrisis> active = new List<ActiveCrisis>();
        private int nextIndex;

        public IReadOnlyList<ActiveCrisis> Active => active;
        public ActiveCrisis OpenCard { get; private set; }
        public int HandledCount { get; private set; }
        public int MissedCount { get; private set; }
        public string LastResult { get; private set; }
        public event Action<ActiveCrisis> CrisisSpawned;
        public event Action<bool> CrisisResolved;

        private void Awake()
        {
            session = GetComponent<GameSession>();
            catalog = CrisisCatalog.Create();
        }

        private void OnEnable()
        {
            session.TimeAdvanced += Advance;
            session.Restarted += ResetCrises;
            session.Ended += OnSessionEnded;
            ResetCrises();
        }

        private void OnDisable()
        {
            session.TimeAdvanced -= Advance;
            session.Restarted -= ResetCrises;
            session.Ended -= OnSessionEnded;
            CloseCard();
        }

        private void ResetCrises()
        {
            cycleStart = 0f;
            nextId = 0;
            previousBurst = false;
            PrepareCycle();
            active.Clear();
            OpenCard = null;
            nextIndex = 0;
            HandledCount = 0;
            MissedCount = 0;
            LastResult = "İlk kriz vardiyanın 5. saniyesinde gelecek.";
        }

        // Yeni tur, devam eden afetleri veya kaynakları sıfırlamaz.
        private void PrepareCycle()
        {
            catalog = CrisisCatalog.Create();
            foreach (var data in catalog) data.ResponseDuration = session.ResponseDuration;
            // Her yeni vardiyada olayları kayıpsız karıştır; aynı afet iki kez eklenmez.
            if (shuffleOnRestart) for (int i = catalog.Length - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                var swap = catalog[i]; catalog[i] = catalog[other]; catalog[other] = swap;
            }
            // Art arda oynanan vardiyalar aynı afetle başlamasın.
            if (shuffleOnRestart && catalog.Length > 1 && catalog[0].Title == lastOpeningCrisis)
            {
                int other = random.Next(1, catalog.Length);
                var swap = catalog[0]; catalog[0] = catalog[other]; catalog[other] = swap;
            }
            spawnOffsets=new float[catalog.Length];
        }

        private float NextInterval()
        {
            // İki kısa aralık art arda üretilmez; tur değişiminde de ritim korunur.
            bool burst = spawnInterval <= 0 && !previousBurst && random.NextDouble() < .25;
            previousBurst = burst;
            return spawnInterval > 0 ? spawnInterval : burst
                ? 1f + (float)random.NextDouble() * 2f
                : 8f + (float)random.NextDouble() * 4f;
        }

        private void Advance(float step)
        {
            // Karar kartı ortak oyun saatini durdurmaz.
            float now = session.ElapsedTime;
            while (session.State == SessionState.Running)
            {
                float spawnAt = nextIndex < catalog.Length
                    ? Mathf.Max(0f, firstCrisisTime) + cycleStart + spawnOffsets[nextIndex]
                    : float.PositiveInfinity;
                ActiveCrisis expired = null;
                foreach (var crisis in active)
                    if (expired == null || crisis.Deadline < expired.Deadline)
                        expired = crisis;

                float deadline = expired == null ? float.PositiveInfinity : expired.Deadline;
                if (Mathf.Min(spawnAt, deadline) > now)
                    break;

                // Düşük kare hızında bile olayları tarih sırasıyla işleriz.
                if (deadline <= spawnAt)
                    Resolve(expired, expired.Data.TimeoutResult, true);
                else
                {
                    var crisis = new ActiveCrisis(nextId++, catalog[nextIndex], spawnAt);
                    lastOpeningCrisis = crisis.Data.Title;
                    active.Add(crisis);
                    nextIndex++;
                    if (nextIndex < catalog.Length)
                        spawnOffsets[nextIndex] = spawnOffsets[nextIndex - 1] + NextInterval();
                    CrisisSpawned?.Invoke(crisis);
                    if (nextIndex == catalog.Length && repeatCrises)
                    {
                        cycleStart += spawnOffsets[catalog.Length - 1] + NextInterval();
                        PrepareCycle();
                        nextIndex = 0;
                    }
                }
            }
        }

        public float SecondsLeft(ActiveCrisis crisis)
        {
            return Mathf.Max(0f, crisis.Deadline - session.ElapsedTime);
        }

        public bool Open(ActiveCrisis crisis)
        {
            if (session.State != SessionState.Running || session.IsMenuPaused || OpenCard != null ||
                crisis == null || crisis.IsResolved || !active.Contains(crisis))
                return false;

            OpenCard = crisis;

            return true;
        }

        public void CloseCard()
        {
            if (OpenCard == null) return;
            OpenCard = null;

        }

        public bool Choose(int optionIndex)
        {
            var crisis = OpenCard;
            if (session.State != SessionState.Running || session.IsMenuPaused || crisis == null ||
                crisis.IsResolved || !active.Contains(crisis) ||
                optionIndex < 0 || optionIndex >= crisis.Data.Options.Length)
                return false;

            var option = crisis.Data.Options[optionIndex];
            if (!session.CanAfford(option.Cost)) return false;
            Resolve(crisis, option, false);
            return true;
        }

        private void Resolve(ActiveCrisis crisis, InterventionData result, bool timedOut)
        {
            // Listeden çıkarıp kilitliyoruz: aynı kriz ikinci kez uygulanamaz.
            if (crisis.IsResolved) return;
            crisis.IsResolved = true;
            active.Remove(crisis);
            if (result.IsIntervention && !timedOut) HandledCount++;
            else MissedCount++;

            LastResult = (timedOut ? "Süre doldu! " : "") + result.Result;
            if (OpenCard == crisis) CloseCard();
            // Sayaç ve sayılar, bitiş bildirimi gönderilmeden önce güncellenir.
            session.TryApplyIntervention(result.Cost, result.HappinessChange, result.ClimateChange);
            CrisisResolved?.Invoke(timedOut || !result.IsIntervention);
        }

        private void OnSessionEnded(SessionState state)
        {
            OpenCard = null;
            // Henüz süresi dolmamış krizler bitişte ek ceza uygulamaz.
        }
    }
}


