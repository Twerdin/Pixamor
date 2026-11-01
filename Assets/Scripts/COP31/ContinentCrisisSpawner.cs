using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// 7 Kıta Kriz, Dinamik Karar Süresi (10s / 20s / 30s) ve Günlük Görev Döngüsü Yöneticisi:
    /// - Her gün başında o güne ait 3 kriz/görev dosyası aktifleşir.
    /// - O güne ait tüm kriz/görev dosyaları tamamlandığında otomatik olarak Siyah Ekran ("GÜN X") geçişi tetiklenir.
    /// </summary>
    public class ContinentCrisisSpawner : MonoBehaviour
    {
        public static ContinentCrisisSpawner Instance { get; private set; }

        [Serializable]
        public class DeferredCrisisRecord
        {
            public ContinentType continent;
            public ContinentQuestion question;
            public int returnDay;
        }

        [Header("Pin Referansları")]
        public WorldMapPin[] continentPins = new WorldMapPin[7];

        [Header("Ertelenmiş Kriz Zinciri (3 Gün Sonra 2x Döner)")]
        public List<DeferredCrisisRecord> deferredCrises = new List<DeferredCrisisRecord>();

        readonly HashSet<int> usedQuestionIds = new HashSet<int>();

        void OnEnable()
        {
            Instance = this;
        }

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
            deferredCrises.Clear();
            SpawnNewDailyCrises(3);
        }

        public void RegisterPin(ContinentType continent, WorldMapPin pin)
        {
            int idx = (int)continent;
            if (idx >= 0 && idx < continentPins.Length)
            {
                continentPins[idx] = pin;
            }
        }

        public ContinentQuestion PickQuestionForContinent(ContinentType targetContinent)
        {
            var pool = ContinentQuestionDatabase.GetQuestionsForContinent(targetContinent);
            if (pool.Count == 0) return null;

            var unused = new List<ContinentQuestion>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (!usedQuestionIds.Contains(pool[i].id))
                {
                    unused.Add(pool[i]);
                }
            }

            ContinentQuestion chosenQ;
            if (unused.Count > 0)
            {
                chosenQ = unused[UnityEngine.Random.Range(0, unused.Count)];
            }
            else
            {
                usedQuestionIds.Clear();
                chosenQ = pool[UnityEngine.Random.Range(0, pool.Count)];
            }

            usedQuestionIds.Add(chosenQ.id);
            return chosenQ;
        }

        public int GetActiveCrisisCount()
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null || controller.continents == null) return 0;
            int count = 0;
            for (int i = 0; i < 7 && i < controller.continents.Length; i++)
            {
                if (controller.continents[i] != null && controller.continents[i].inCrisis)
                    count++;
            }
            return count;
        }

        public void ProcessEndOfDayCrises()
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            for (int i = 0; i < 7; i++)
            {
                var cont = controller.continents[i];
                if (cont != null && cont.inCrisis)
                {
                    ClearContinentCrisis(cont);
                }
            }
        }

        public void OnDayAdvanced(int newDay)
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            for (int i = deferredCrises.Count - 1; i >= 0; i--)
            {
                var rec = deferredCrises[i];
                if (newDay >= rec.returnDay)
                {
                    deferredCrises.RemoveAt(i);
                    var cont = controller.continents[(int)rec.continent];
                    ActivateCrisisOnContinent(cont, rec.question, isDeferredDoubled: true);
                    Sfx.Play(SfxType.Alarm);
                }
            }

            int activeCount = GetActiveCrisisCount();
            int toSpawn = Mathf.Clamp(3 - activeCount, 1, 3);
            SpawnNewDailyCrises(toSpawn);
        }

        public void SpawnNewDailyCrises(int count)
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            for (int n = 0; n < count; n++)
            {
                var available = new List<ContinentType>();
                for (int i = 0; i < 7; i++)
                {
                    if (!controller.continents[i].inCrisis)
                        available.Add((ContinentType)i);
                }

                if (available.Count == 0) break;

                ContinentType target = available[UnityEngine.Random.Range(0, available.Count)];
                ContinentQuestion q = PickQuestionForContinent(target);
                if (q != null)
                {
                    var cont = controller.continents[(int)target];
                    ActivateCrisisOnContinent(cont, q, isDeferredDoubled: false);
                }
            }
        }

        void ActivateCrisisOnContinent(WorldMapMonitorController.ContinentState cont, ContinentQuestion q, bool isDeferredDoubled)
        {
            if (cont == null || q == null) return;

            float duration = q.decisionTimeLimit > 1f ? q.decisionTimeLimit : 20f;
            cont.inCrisis = true;
            cont.isDeferredDoubled = isDeferredDoubled;
            cont.isVerified = false;
            cont.activeQuestion = q;
            cont.crisisRemainingTime = duration;

            WorldMapMonitorController.Instance?.SetContinentCrisisState(cont.type, true, q, duration);

            int idx = (int)cont.type;
            if (continentPins != null && idx >= 0 && idx < continentPins.Length && continentPins[idx] != null)
            {
                continentPins[idx].Activate(cont.name, isDeferredDoubled);
            }
        }

        public void DeferContinentCrisis(WorldMapMonitorController.ContinentState cont)
        {
            if (cont == null || cont.activeQuestion == null) return;

            int currentDay = ClimateOperatorManager.Instance != null ? ClimateOperatorManager.Instance.CurrentDay : 1;
            deferredCrises.Add(new DeferredCrisisRecord
            {
                continent = cont.type,
                question = cont.activeQuestion,
                returnDay = currentDay + 3
            });

            ClearContinentCrisis(cont);
            ClimateOperatorManager.Instance?.RegisterCrisisDeferred(cont);
        }

        public void ClearContinentCrisis(WorldMapMonitorController.ContinentState cont)
        {
            if (cont == null) return;

            cont.inCrisis = false;
            cont.isDeferredDoubled = false;
            cont.isVerified = false;
            cont.activeQuestion = null;
            cont.crisisRemainingTime = 0f;

            WorldMapMonitorController.Instance?.SetContinentCrisisState(cont.type, false, null, 0f);

            int idx = (int)cont.type;
            if (continentPins != null && idx >= 0 && idx < continentPins.Length && continentPins[idx] != null)
            {
                continentPins[idx].Deactivate();
            }
        }

        public void OnScreenClicked(Vector2 uv)
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            int clickedContIdx = controller.GetContinentAtUV(uv);
            if (clickedContIdx < 0 || clickedContIdx >= 7) return;

            OpenContinentDossier(clickedContIdx);
        }

        public void OnPinClicked(int continentIndex)
        {
            if (continentIndex < 0 || continentIndex >= 7) return;
            OpenContinentDossier(continentIndex);
        }

        public void OpenContinentDossier(int continentIndex)
        {
            if (ClimateOperatorManager.Instance != null && ClimateOperatorManager.Instance.IsDayTransitioning)
                return;

            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            var cont = controller.continents[continentIndex];
            if (cont == null) return;

            if (cont.activeQuestion == null)
            {
                cont.activeQuestion = PickQuestionForContinent(cont.type);
                cont.crisisRemainingTime = cont.activeQuestion != null ? cont.activeQuestion.decisionTimeLimit : 20f;
            }
            else if (cont.crisisRemainingTime <= 0.5f)
            {
                cont.crisisRemainingTime = cont.activeQuestion.decisionTimeLimit > 1f ? cont.activeQuestion.decisionTimeLimit : 20f;
            }

            Sfx.Play(SfxType.Open);
            BlindChoiceDialogueUI.Instance?.ShowForContinent(cont);
        }
    }
}
