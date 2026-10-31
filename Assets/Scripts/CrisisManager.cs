using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixamor
{
    [RequireComponent(typeof(GameSession))]
    public class CrisisManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float firstCrisisTime = 5f;
        // Sabit prototip temposu eski sahnelerin kayıtlı 25 saniyesinden etkilenmez.
        private const float spawnInterval = 20f;
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
            catalog = CrisisCatalog.Create();

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
            active.Clear();
            OpenCard = null;
            nextIndex = 0;
            HandledCount = 0;
            MissedCount = 0;
            LastResult = "İlk kriz vardiyanın 5. saniyesinde gelecek.";
        }

        private void Advance(float step)
        {
            // Karar kartı ortak oyun saatini durdurmaz.
            float now = session.ElapsedTime;
            while (session.State == SessionState.Running)
            {
                float spawnAt = nextIndex < catalog.Length
                    ? Mathf.Max(0f, firstCrisisTime) + nextIndex * Mathf.Max(1f, spawnInterval)
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
                    var crisis = new ActiveCrisis(nextIndex, catalog[nextIndex], spawnAt);
                    if (nextIndex == 0) lastOpeningCrisis = crisis.Data.Title;
                    active.Add(crisis);
                    nextIndex++;
                    CrisisSpawned?.Invoke(crisis);
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
        }

        private void OnSessionEnded(SessionState state)
        {
            OpenCard = null;
            // Henüz süresi dolmamış krizler bitişte ek ceza uygulamaz.
        }
    }
}

