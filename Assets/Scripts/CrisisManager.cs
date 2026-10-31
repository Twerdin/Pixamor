using System.Collections.Generic;
using UnityEngine;

namespace Pixamor
{
    [RequireComponent(typeof(GameSession))]
    public class CrisisManager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float firstCrisisTime = 5f;
        [SerializeField, Min(1f)] private float spawnInterval = 25f;

        private GameSession session;
        private CrisisData[] catalog;
        private readonly List<ActiveCrisis> active = new List<ActiveCrisis>();
        private int nextIndex;

        public IReadOnlyList<ActiveCrisis> Active => active;
        public ActiveCrisis OpenCard { get; private set; }
        public int HandledCount { get; private set; }
        public int MissedCount { get; private set; }
        public string LastResult { get; private set; }

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
            active.Clear();
            OpenCard = null;
            nextIndex = 0;
            HandledCount = 0;
            MissedCount = 0;
            LastResult = "İlk kriz vardiyanın 5. saniyesinde gelecek.";
        }

        private void Advance(float step)
        {
            // Kendi Update sayacımız yok: kart açılınca ortak saatle birlikte dururuz.
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
                    active.Add(new ActiveCrisis(nextIndex, catalog[nextIndex], spawnAt));
                    nextIndex++;
                }
            }
        }

        public float SecondsLeft(ActiveCrisis crisis)
        {
            return Mathf.Max(0f, crisis.Deadline - session.ElapsedTime);
        }

        public bool Open(ActiveCrisis crisis)
        {
            if (session.State != SessionState.Running || OpenCard != null ||
                crisis == null || crisis.IsResolved || !active.Contains(crisis))
                return false;

            OpenCard = crisis;
            session.SetPaused(true);
            return true;
        }

        public void CloseCard()
        {
            if (OpenCard == null) return;
            OpenCard = null;
            session.SetPaused(false);
        }

        public bool Choose(int optionIndex)
        {
            var crisis = OpenCard;
            if (session.State != SessionState.Running || crisis == null ||
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
            CloseCard();
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
