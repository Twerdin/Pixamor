using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>
    /// 7 Kıta Kriz Üretim ve Tetikleme Yöneticisi:
    /// - Yalnızca kara pikselleri üzerinde kriz oluşturur.
    /// - Okyanus tıklamalarını geçersiz sayar.
    /// - Kıtaların alarm nabzını (pulse) ve 3D kriz pinlerini yönetir.
    /// - Süre dolduğunda varsayılan seçeneği otomatik devreye alır.
    /// </summary>
    public class ContinentCrisisSpawner : MonoBehaviour
    {
        public static ContinentCrisisSpawner Instance { get; private set; }

        [Header("Zaman Ayarları")]
        public float initialCrisisDelay = 4.0f;
        public float crisisInterval = 28.0f;
        public float crisisDuration = 40.0f;

        [Header("Pin Referansları")]
        public WorldMapPin[] continentPins = new WorldMapPin[7];

        float nextSpawnTime;
        int questionsAskedCount;
        readonly HashSet<int> usedQuestionIds = new HashSet<int>();

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
            nextSpawnTime = Time.time + initialCrisisDelay;
        }

        void Update()
        {
            var mgr = ClimateOperatorManager.Instance;
            if (mgr != null && mgr.State != GameState.Playing) return;

            // 1. Yeni Kriz Üretme Döngüsü
            if (Time.time >= nextSpawnTime)
            {
                SpawnNextCrisis();
                nextSpawnTime = Time.time + crisisInterval;
            }

            // 2. Aktif Krizlerin Geri Sayımı
            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            for (int i = 0; i < 7; i++)
            {
                var cont = controller.continents[i];
                if (cont != null && cont.inCrisis)
                {
                    cont.crisisRemainingTime -= Time.deltaTime;

                    // Pini güncelle
                    if (continentPins != null && i < continentPins.Length && continentPins[i] != null)
                    {
                        continentPins[i].UpdateTimer(cont.crisisRemainingTime, crisisDuration);
                    }

                    // Süre dolduysa otomatik müdahalesizlik / erteleme seçeneği (3. seçenek) devreye girer
                    if (cont.crisisRemainingTime <= 0f)
                    {
                        HandleCrisisTimeout(cont);
                    }
                }
            }
        }

        public void RegisterPin(ContinentType continent, WorldMapPin pin)
        {
            int idx = (int)continent;
            if (idx >= 0 && idx < continentPins.Length)
            {
                continentPins[idx] = pin;
            }
        }

        /// <summary>
        /// Yalnızca kara pikselleri üzerinden kriz üretir.
        /// </summary>
        public void SpawnNextCrisis()
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            // Krizde olmayan kıtaları tespit et
            var availableContinents = new List<ContinentType>();
            for (int i = 0; i < 7; i++)
            {
                if (!controller.continents[i].inCrisis)
                {
                    availableContinents.Add(controller.continents[i].type);
                }
            }

            if (availableContinents.Count == 0) return; // Tüm kıtalar zaten krizde

            // Önceliklendirme: İklim sağlığı en düşük olan kıtaya daha yüksek kriz ihtimali ver
            availableContinents.Sort((a, b) =>
            {
                var sa = controller.GetContinent(a);
                var sb = controller.GetContinent(b);
                return sa.health.CompareTo(sb.health);
            });

            // En kritik ilk 3 kıtadan birini seç
            int pickRange = Mathf.Min(3, availableContinents.Count);
            ContinentType targetContinent = availableContinents[UnityEngine.Random.Range(0, pickRange)];
            var contState = controller.GetContinent(targetContinent);

            // Soru havuzundan uygun soruyu çek
            var pool = ContinentQuestionDatabase.GetQuestionsForContinent(targetContinent);
            if (pool.Count == 0) return;

            ContinentQuestion chosenQ = null;
            for (int i = 0; i < pool.Count; i++)
            {
                if (!usedQuestionIds.Contains(pool[i].id))
                {
                    chosenQ = pool[i];
                    break;
                }
            }

            // Hepsi kullanıldıysa havuzu tekrar aç
            if (chosenQ == null)
            {
                usedQuestionIds.Clear();
                chosenQ = pool[UnityEngine.Random.Range(0, pool.Count)];
            }

            usedQuestionIds.Add(chosenQ.id);

            // Kriz başlat
            controller.TriggerContinentCrisis(targetContinent, chosenQ, crisisDuration);

            // 3D Kriz Pinini aktif et
            int contIdx = (int)targetContinent;
            if (continentPins != null && contIdx < continentPins.Length && continentPins[contIdx] != null)
            {
                continentPins[contIdx].Activate(chosenQ.title, crisisDuration);
            }

            // Bildirim gönder
            ClimateOperatorManager.Instance?.Post($"[KRİZ ALARMI] {contState.name}: '{chosenQ.title}' patlak verdi! Kıtaya tıklayarak müdahale edin.");
            Sfx.Play(SfxType.Alarm);
        }

        /// <summary>
        /// Monitör ekranına tıklandığında çağrılır.
        /// Kesin Kural: Okyanus tıklamaları yoksayılır, yalnızca kara pikselleri yanıt verir.
        /// </summary>
        public void OnScreenClicked(Vector2 mapUV)
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            int continentIdx = controller.GetContinentAtUV(mapUV);

            // Okyanusa tıklandıysa kesinlikle hiçbir şey yapma!
            if (continentIdx < 0 || continentIdx >= 7)
            {
                return;
            }

            var cont = controller.continents[continentIdx];

            // Eğer kıtada kriz varsa Soru-Cevap modalını aç
            if (cont.inCrisis && cont.activeQuestion != null)
            {
                BlindChoiceDialogueUI.Instance?.OpenForContinent(cont);
            }
            else
            {
                // Kriz yoksa kıtanın güncel durumunu alt bilgi bandına yaz
                ClimateOperatorManager.Instance?.Post($"{cont.name} — Temsilci: {cont.representativeTitle} | İklim: %{cont.health:0} | Sinir: %{cont.anger:0} (Durum Stabil)");
                Sfx.Play(SfxType.Click);
            }
        }

        /// <summary>
        /// Bir 3D kriz pinine doğrudan tıklandığında çağrılır.
        /// </summary>
        public void OnPinClicked(int continentIdx)
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null || continentIdx < 0 || continentIdx >= 7) return;

            var cont = controller.continents[continentIdx];
            if (cont.inCrisis && cont.activeQuestion != null)
            {
                BlindChoiceDialogueUI.Instance?.OpenForContinent(cont);
            }
        }

        void HandleCrisisTimeout(WorldMapMonitorController.ContinentState cont)
        {
            if (cont.activeQuestion == null || cont.activeQuestion.choices.Length < 3) return;

            // 3. Seçenek (Müdahalesizlik / Erteleme) varsayılan olarak devreye girer
            var fallbackChoice = cont.activeQuestion.choices[2];

            ClimateOperatorManager.Instance?.Post($"[ZAMAN DOLDU] {cont.name} krizine müdahale gecikti! Gecikmeli sonuçlar uygulandı.");
            Sfx.Play(SfxType.Fail);

            ResolveCrisis(cont.type, fallbackChoice);
        }

        public void ResolveCrisis(ContinentType continent, BlindChoice choice)
        {
            var controller = WorldMapMonitorController.Instance;
            if (controller == null) return;

            int idx = (int)continent;

            // 3D Pini kapat
            if (continentPins != null && idx < continentPins.Length && continentPins[idx] != null)
            {
                continentPins[idx].Deactivate();
            }

            // Kıta durumunu ve harita dokusunu güncelle
            controller.ResolveContinentCrisis(continent, choice);

            // Küresel oyun dengesini (Para, Halk, İklim) güncelle
            var mgr = ClimateOperatorManager.Instance;
            if (mgr != null && choice != null)
            {
                mgr.ApplyChoiceEffects(choice.moneyDelta, choice.halkDelta, choice.iklimDelta, choice.outcomeSummary);
            }
        }
    }
}
