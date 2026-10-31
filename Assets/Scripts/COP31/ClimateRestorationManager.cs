using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixamor.COP31
{
    public enum TreeType
    {
        Oak = 0,        // Dayanıklı Anadolu Meşesi (Dengeli, kuraklığa dirençli)
        Pine = 1,       // Hızlı Büyüyen Kızılçam (Hızlı getiri, erozyon önleyici)
        Cedar = 2       // Yüksek Karbonlu Toros Sediri (Geç büyür, devasa tasarruf)
    }

    public enum TreeGrowthStage
    {
        Sapling = 0,    // Fidan (0 - 25 sn) -> Düşük koruma, başlangıç tasarrufu
        YoungTree = 1,  // Genç Ağaç (25 - 65 sn) -> Kökler tutundu, orta tasarruf
        MatureForest = 2// Olgun Ağaç / Koruluk (65 sn+) -> Maksimum afet önleme tasarrufu!
    }

    [Serializable]
    public class PlantedTree
    {
        public TreeType type;
        public TreeGrowthStage stage;
        public float ageSeconds;

        public PlantedTree(TreeType type)
        {
            this.type = type;
            this.stage = TreeGrowthStage.Sapling;
            this.ageSeconds = 0f;
        }

        public void Tick(float dt)
        {
            ageSeconds += dt;
            if (ageSeconds >= 65f)
            {
                stage = TreeGrowthStage.MatureForest;
            }
            else if (ageSeconds >= 25f)
            {
                stage = TreeGrowthStage.YoungTree;
            }
            else
            {
                stage = TreeGrowthStage.Sapling;
            }
        }

        public float GetPassiveSavingsPerSecond()
        {
            float baseSavings = type == TreeType.Oak ? 0.12f : (type == TreeType.Pine ? 0.10f : 0.18f);
            switch (stage)
            {
                case TreeGrowthStage.YoungTree: return baseSavings * 2.8f;
                case TreeGrowthStage.MatureForest: return baseSavings * 7.5f;
                default: return baseSavings;
            }
        }
    }

    [Serializable]
    public class RestorationRegion
    {
        public string id;
        public string name;
        public string disasterRisk;         // "Şiddetli Toprak Erozyonu", "Sel & Heyelan", "Orman Yangını Külü"
        public bool isUnlocked;
        public float unlockCost;
        public float stateSavingsMultiplier; // Devletin bu bölgedeki afet hasar çarpanı
        public List<PlantedTree> plantedTrees = new List<PlantedTree>();

        public int TotalTrees => plantedTrees.Count;

        public float CalculateRegionSavingsRate()
        {
            float total = 0f;
            for (int i = 0; i < plantedTrees.Count; i++)
            {
                total += plantedTrees[i].GetPassiveSavingsPerSecond();
            }
            return total * stateSavingsMultiplier;
        }
    }

    [Serializable]
    public class StateContract
    {
        public string id;
        public string title;
        public string targetRegionId;
        public int targetTreeCount;
        public int currentProgress;
        public float rewardGrant;            // Devletten hibe / ödül para
        public bool isCompleted;
    }

    public enum TruckStatus
    {
        IdleInDepot,
        EnRouteToRegion,
        PlantingOnSite,
        ReturningToDepot
    }

    /// <summary>
    /// COP31 İklim Düzeltme Birimi — Ekolojik Restorasyon ve Ağaçlandırma Tycoon Motoru:
    /// - Üretim: Tohum ek, fidan yetiştir (Nursery / Sprout Station).
    /// - Paketleme: 3 Fidan = 1 Biyo-Koli, rafa diz.
    /// - Sevkiyat: Kolileri kamyona yükle, afet riskli bölgelere sevk et.
    /// - Pazar & Gelir Modeli: Devletin cebinden çıkan afet/hasar masrafını her azalttığımızda,
    ///   devlet bize düzenli afet önleme teşviki (kuruş kuruş pasif getiri) ve sözleşme hibesi öder!
    /// - Fidanlar zamanla Genç Ağaç ve Olgun Ormana dönüşerek devasa tasarruf üretir.
    /// </summary>
    public class ClimateRestorationManager : MonoBehaviour
    {
        public static ClimateRestorationManager Instance { get; private set; }

        [Header("Finansal ve Ekolojik Metrikler")]
        public float budget = 35.0f;                    // Operatör bütçesi ($)
        public float totalStateSavingsGenerated = 0.0f; // Devletin kasasında kalan toplam afet önleme tasarrufu ($)
        public int totalTreesPlanted = 0;               // Toplam dikilen ağaç sayısı
        public float totalCarbonTonOffset = 0.0f;       // Atmosferden temizlenen karbon miktarı

        [Header("İstasyon Stokları")]
        public int nurseryTraySaplings = 0;             // Fidanlık tepsisindeki hazır fidanlar (Maks: 6)
        public const int MaxTraySaplings = 6;
        public int packagedBoxes = 0;                   // Paketlenmiş koli sayısı (Maks: 4, her koli 3 fidan)
        public const int MaxBoxesInStorage = 4;
        public int truckLoadedBoxes = 0;                // Kamyondaki koli sayısı (Maks: 3)
        public const int MaxTruckCapacity = 3;

        [Header("Lojistik Kamyon Durumu")]
        public TruckStatus truckStatus = TruckStatus.IdleInDepot;
        public float truckTimer = 0f;
        public float truckTripDuration = 5.0f;
        public int selectedRegionIndex = 0;

        [Header("Bölgeler ve Sözleşmeler")]
        public List<RestorationRegion> regions = new List<RestorationRegion>();
        public List<StateContract> contracts = new List<StateContract>();

        // Maliyet ve Katsayılar
        public const float SeedCost = 1.20f;            // Fidan tohumu ve biyo-kapsül maliyeti ($)
        public const float BoxDispatchBounty = 8.50f;   // Koli tesliminde anında verilen acil devlet harcırahı ($)

        public event Action OnStateChanged;

        void Awake()
        {
            Instance = this;
            InitializeRegions();
            InitializeContracts();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void InitializeRegions()
        {
            regions.Clear();

            // 1. Bölge: İç Anadolu Bozkırı (Erozyon ve Toz Fırtınası Hattı)
            regions.Add(new RestorationRegion
            {
                id = "bozkir",
                name = "İç Anadolu Bozkırı",
                disasterRisk = "Toprak Erozyonu & Tarımsal Çölleşme",
                isUnlocked = true,
                unlockCost = 0f,
                stateSavingsMultiplier = 1.0f
            });

            // 2. Bölge: Akdeniz Kül Vadisi (Orman Yangın Sahası)
            regions.Add(new RestorationRegion
            {
                id = "akdeniz",
                name = "Akdeniz Yangın Sahası",
                disasterRisk = "Kül Taşması & Sel Baskını Riski",
                isUnlocked = true,
                unlockCost = 25f,
                stateSavingsMultiplier = 1.35f
            });

            // 3. Bölge: Karadeniz Heyelan Koridoru (Yol ve Altyapı Koruma)
            regions.Add(new RestorationRegion
            {
                id = "karadeniz",
                name = "Karadeniz Heyelan Kuşağı",
                disasterRisk = "Yol Çökmesi & Heyelan Hasarı",
                isUnlocked = false,
                unlockCost = 45f,
                stateSavingsMultiplier = 1.75f
            });

            // 4. Bölge: Güneydoğu Çölleşme Kalkanı (Yeşil Kuşak Duvarı)
            regions.Add(new RestorationRegion
            {
                id = "guneydogu",
                name = "Güneydoğu Çöl Kalkanı",
                disasterRisk = "Aşırı Sıcaklık & Su Akiferi Kaybı",
                isUnlocked = false,
                unlockCost = 80f,
                stateSavingsMultiplier = 2.40f
            });
        }

        void InitializeContracts()
        {
            contracts.Clear();

            contracts.Add(new StateContract
            {
                id = "c1",
                title = "İç Anadolu Bozkırına 6 Fidan Sevk Et",
                targetRegionId = "bozkir",
                targetTreeCount = 6,
                currentProgress = 0,
                rewardGrant = 30.0f,
                isCompleted = false
            });

            contracts.Add(new StateContract
            {
                id = "c2",
                title = "Akdeniz Yangın Sahasını Yeşert (9 Fidan)",
                targetRegionId = "akdeniz",
                targetTreeCount = 9,
                currentProgress = 0,
                rewardGrant = 55.0f,
                isCompleted = false
            });

            contracts.Add(new StateContract
            {
                id = "c3",
                title = "Devlete $50 Afet Önleme Tasarrufu Kazandır",
                targetRegionId = "all",
                targetTreeCount = 50, // Dolar bazında kontrol
                currentProgress = 0,
                rewardGrant = 75.0f,
                isCompleted = false
            });
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // 1. Sahalardaki Ağaçların Büyümesi ve Devlet Afet Önleme Tasarrufu Üretimi
            float currentSavingsRate = 0f;
            for (int r = 0; r < regions.Count; r++)
            {
                var reg = regions[r];
                for (int t = 0; t < reg.plantedTrees.Count; t++)
                {
                    reg.plantedTrees[t].Tick(dt);
                }
                currentSavingsRate += reg.CalculateRegionSavingsRate();
            }

            // "Devletin cebinden çıkan her kuruşu azalttığımızda bize para yolluyorlar"
            // Büyüyen ağaçlar devlete afet önleme tasarrufu sağladıkça, hazine bize düzenli pay aktarır:
            if (currentSavingsRate > 0f)
            {
                float savingsGenerated = currentSavingsRate * dt;
                totalStateSavingsGenerated += savingsGenerated;

                // Hazine tasarruf payı (Doğrudan bütçeye kuruş kuruş nakit akar!)
                budget += savingsGenerated;

                // Karbon tutumu
                totalCarbonTonOffset += (currentSavingsRate * 0.04f) * dt;

                // Sözleşme 3 kontrolü
                CheckSavingsContract();
            }

            // 2. Kamyon Sevkiyat Döngüsü
            UpdateTruckLogistics(dt);
        }

        void UpdateTruckLogistics(float dt)
        {
            if (truckStatus == TruckStatus.IdleInDepot) return;

            truckTimer += dt;

            if (truckStatus == TruckStatus.EnRouteToRegion)
            {
                if (truckTimer >= truckTripDuration)
                {
                    // Sahaya vardı, dikim aşamasına geç
                    truckStatus = TruckStatus.PlantingOnSite;
                    truckTimer = 0f;
                    PlantTruckCargoOnSite();
                }
            }
            else if (truckStatus == TruckStatus.PlantingOnSite)
            {
                if (truckTimer >= 2.0f) // 2 saniye dikim operasyonu
                {
                    // Depoya dönüşe geç
                    truckStatus = TruckStatus.ReturningToDepot;
                    truckTimer = 0f;
                    Sfx.Play(SfxType.Lever);
                }
            }
            else if (truckStatus == TruckStatus.ReturningToDepot)
            {
                if (truckTimer >= truckTripDuration)
                {
                    // Depoya ulaştı, kamyon hazır
                    truckStatus = TruckStatus.IdleInDepot;
                    truckTimer = 0f;
                    truckLoadedBoxes = 0;
                    ClimateOperatorManager.Instance?.Post("Ağaçlandırma kamyonu sahayı yeşertip depoya döndü. Yeni yükleme için hazır.");
                    Sfx.Play(SfxType.Success);
                    OnStateChanged?.Invoke();
                }
            }
        }

        void PlantTruckCargoOnSite()
        {
            if (selectedRegionIndex < 0 || selectedRegionIndex >= regions.Count) return;
            var reg = regions[selectedRegionIndex];

            int totalSaplingsToPlant = truckLoadedBoxes * 3;
            for (int i = 0; i < totalSaplingsToPlant; i++)
            {
                reg.plantedTrees.Add(new PlantedTree(TreeType.Oak));
            }

            totalTreesPlanted += totalSaplingsToPlant;

            // Koli başı anında devlet afet önleme harcırahı
            float dispatchBounty = truckLoadedBoxes * BoxDispatchBounty;
            budget += dispatchBounty;

            ClimateOperatorManager.Instance?.Post($"DİKİM BAŞARILI! {reg.name} sahasına {totalSaplingsToPlant} fidan dikildi. Devletten +${dispatchBounty:0.0} acil hibe alındı!");
            Sfx.Play(SfxType.Success);

            // Sözleşmeleri kontrol et
            CheckTreeCountContracts(reg.id, totalSaplingsToPlant);

            OnStateChanged?.Invoke();
        }

        // =========================================================================
        // OYUNCU ETKİLEŞİM METOTLARI (STATION ACTIONS)
        // =========================================================================

        /// <summary>
        /// 1. İstasyon: Biyo-Sera Tohum Ekimi / Fidan Yetiştirici (Nursery)
        /// Tıklanarak veya Boşluk tuşuyla tepsiye fidan üretir.
        /// </summary>
        public bool ProduceSapling()
        {
            if (nurseryTraySaplings >= MaxTraySaplings)
            {
                ClimateOperatorManager.Instance?.Post("Fidanlık tepsisi dolu (6/6)! Fidanları koliye aktarın.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            if (budget < SeedCost)
            {
                ClimateOperatorManager.Instance?.Post("Yetersiz bütçe! Tohum ve biyo-kapsül için $1.20 gerekli.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            budget -= SeedCost;
            nurseryTraySaplings++;
            Sfx.Play(SfxType.Click);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 2. İstasyon: Paketleme / Kolileme
        /// Tepsiden 3 fidan alıp 1 Biyo-Kök Kolisi yapar ve rafa yerleştirir.
        /// </summary>
        public bool PackageBox()
        {
            if (packagedBoxes >= MaxBoxesInStorage)
            {
                ClimateOperatorManager.Instance?.Post("Depo rafı tamamen dolu (4/4)! Kolileri kamyona yükleyin.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            if (nurseryTraySaplings < 3)
            {
                ClimateOperatorManager.Instance?.Post($"Kolilemek için en az 3 fidan gerekli! (Şu an: {nurseryTraySaplings}/3)");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            nurseryTraySaplings -= 3;
            packagedBoxes++;
            ClimateOperatorManager.Instance?.Post("3 Fidan koruyucu biyo-koliye paketlendi ve rafa dizildi.");
            Sfx.Play(SfxType.Open);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 3. İstasyon: Kamyon Yükleme
        /// Raftan 1 koli alıp kamyona koyar (0..3).
        /// </summary>
        public bool LoadBoxToTruck()
        {
            if (truckStatus != TruckStatus.IdleInDepot)
            {
                ClimateOperatorManager.Instance?.Post("Kamyon şu anda sahada sevkiyatta! Dönmesini bekleyin.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            if (truckLoadedBoxes >= MaxTruckCapacity)
            {
                ClimateOperatorManager.Instance?.Post("Kamyon kasası tamamen dolu (3/3 Koli)! Sahaya sevk edin.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            if (packagedBoxes <= 0)
            {
                ClimateOperatorManager.Instance?.Post("Rafta hazır kapalı koli yok! Önce fidan kolileyin.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            packagedBoxes--;
            truckLoadedBoxes++;
            Sfx.Play(SfxType.Click);
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 4. İstasyon: Kamyonu Sahaya Gönder (Sevkiyat Başlat)
        /// </summary>
        public bool DispatchTruck()
        {
            if (truckStatus != TruckStatus.IdleInDepot)
            {
                ClimateOperatorManager.Instance?.Post("Kamyon zaten görevde!");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            if (truckLoadedBoxes <= 0)
            {
                ClimateOperatorManager.Instance?.Post("Kamyon boş! Sahaya sevk etmek için en az 1 koli yükleyin.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            var reg = regions[selectedRegionIndex];
            if (!reg.isUnlocked)
            {
                ClimateOperatorManager.Instance?.Post($"{reg.name} henüz açılmadı! Önce bölgesel izin protokolünü tamamlayın.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            truckStatus = TruckStatus.EnRouteToRegion;
            truckTimer = 0f;
            ClimateOperatorManager.Instance?.Post($"Ağaçlandırma konvoyu yola çıktı -> Hedef: {reg.name} ({truckLoadedBoxes * 3} fidan)");
            Sfx.Play(SfxType.Lever);
            OnStateChanged?.Invoke();
            return true;
        }

        public void SelectRegion(int index)
        {
            if (index >= 0 && index < regions.Count)
            {
                selectedRegionIndex = index;
                Sfx.Play(SfxType.Click);
                OnStateChanged?.Invoke();
            }
        }

        public bool UnlockRegion(int index)
        {
            if (index < 0 || index >= regions.Count) return false;
            var reg = regions[index];
            if (reg.isUnlocked) return true;

            if (budget < reg.unlockCost)
            {
                ClimateOperatorManager.Instance?.Post($"Yetersiz bütçe! Bu bölgeyi açmak için ${reg.unlockCost:0} gerekli.");
                Sfx.Play(SfxType.Fail);
                return false;
            }

            budget -= reg.unlockCost;
            reg.isUnlocked = true;
            ClimateOperatorManager.Instance?.Post($"BÖLGE AÇILDI: {reg.name} restorasyon operasyonlarına dahil edildi!");
            Sfx.Play(SfxType.Success);
            OnStateChanged?.Invoke();
            return true;
        }

        // =========================================================================
        // DEVLET SÖZLEŞMELERİ KONTROLÜ
        // =========================================================================

        void CheckTreeCountContracts(string regionId, int newlyPlanted)
        {
            for (int i = 0; i < contracts.Count; i++)
            {
                var c = contracts[i];
                if (c.isCompleted) continue;

                if (c.targetRegionId == regionId || c.targetRegionId == "all")
                {
                    c.currentProgress += newlyPlanted;
                    if (c.currentProgress >= c.targetTreeCount)
                    {
                        CompleteContract(c);
                    }
                }
            }
        }

        void CheckSavingsContract()
        {
            for (int i = 0; i < contracts.Count; i++)
            {
                var c = contracts[i];
                if (c.isCompleted) continue;

                if (c.id == "c3")
                {
                    c.currentProgress = Mathf.FloorToInt(totalStateSavingsGenerated);
                    if (c.currentProgress >= c.targetTreeCount)
                    {
                        CompleteContract(c);
                    }
                }
            }
        }

        void CompleteContract(StateContract c)
        {
            c.isCompleted = true;
            budget += c.rewardGrant;
            ClimateOperatorManager.Instance?.Post($"[DEVLET SÖZLEŞMESİ TAMAMLANDI] '{c.title}' hedefine ulaşıldı! Hazine Hibesi: +${c.rewardGrant:0.0} aktarıldı!");
            Sfx.Play(SfxType.Success);
            OnStateChanged?.Invoke();
        }

        public float GetTotalCurrentPassiveRate()
        {
            float total = 0f;
            for (int i = 0; i < regions.Count; i++)
            {
                total += regions[i].CalculateRegionSavingsRate();
            }
            return total;
        }
    }
}
