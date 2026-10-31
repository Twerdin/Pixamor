using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// COP31 İklim Düzeltme Birimi Arayüzü:
    /// FlavorCorp'un bağımlılık yaratan 4 istasyonlu üretim/paketleme/sevkiyat/bölge döngüsünü
    /// gerçekçi iklim restorasyonu ve devlet afet tasarrufu ekonomisine uyarlar.
    /// </summary>
    public class ClimateRestorationUI : MonoBehaviour
    {
        public static ClimateRestorationUI Instance { get; private set; }

        [Header("Sekme / Mod Yönetimi")]
        public GameObject worldMapContainer;
        public GameObject worldMap3DContainer;
        public GameObject restorationContainer;
        public GameObject restoration3DContainer;
        public PressButton tabMapButton;
        public PressButton tabRestorationButton;
        public Image tabMapBg;
        public Image tabRestorationBg;
        public Text tabMapText;
        public Text tabRestorationText;
        public bool isRestorationModeActive = false;

        [Header("Kök Panel")]
        public GameObject panelRoot;

        [Header("Üst Bilgi Bandı")]
        public Text budgetText;
        public Text savingsText;
        public Text rateText;
        public Text treeCountText;
        public Text carbonText;

        [Header("İstasyon 1: Biyo-Sera (Fidan Yetiştirici)")]
        public Text nurseryStatusText;
        public Text nurseryCostText;
        public PressButton growButton;
        public PressButton packageButton;

        [Header("İstasyon 2: Paketleme & Depo Rafı")]
        public Text storageStatusText;
        public PressButton loadTruckButton;

        [Header("İstasyon 3: Sevkiyat (Ağaçlandırma Kamyonu)")]
        public Text truckStatusText;
        public Text truckCargoText;
        public PressButton dispatchButton;

        [Header("İstasyon 4: Restorasyon Sahaları")]
        public Text[] regionNameTexts = new Text[4];
        public Text[] regionStatTexts = new Text[4];
        public PressButton[] regionSelectButtons = new PressButton[4];
        public UIBar[] regionProgressBars = new UIBar[4];
        public Image[] regionCardBgs = new Image[4];

        [Header("Devlet Sözleşmeleri")]
        public Text[] contractTitleTexts = new Text[3];
        public Text[] contractProgressTexts = new Text[3];
        public UIBar[] contractProgressBars = new UIBar[3];

        [Header("Görsel İstasyon Yuvaları & Göstergeleri")]
        public Text[] nurserySlotTexts = new Text[6];
        public Text nurseryLedDotsText;
        public Text[] storageSlotTexts = new Text[4];
        public Text[] truckCargoSlotTexts = new Text[3];
        public UIBar truckProgressBar;
        public Text truckTargetText;

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
            // Sekme Butonlarını Bağla
            if (tabMapButton != null)
                tabMapButton.Pressed += SwitchToMapMode;

            if (tabRestorationButton != null)
                tabRestorationButton.Pressed += SwitchToRestorationMode;

            // İstasyon Butonlarını Bağla
            if (growButton != null)
                growButton.Pressed += () => ClimateRestorationManager.Instance?.ProduceSapling();

            if (packageButton != null)
                packageButton.Pressed += () => ClimateRestorationManager.Instance?.PackageBox();

            if (loadTruckButton != null)
                loadTruckButton.Pressed += () => ClimateRestorationManager.Instance?.LoadBoxToTruck();

            if (dispatchButton != null)
                dispatchButton.Pressed += () => ClimateRestorationManager.Instance?.DispatchTruck();

            if (regionSelectButtons != null)
            {
                for (int i = 0; i < regionSelectButtons.Length; i++)
                {
                    int idx = i;
                    if (regionSelectButtons[i] != null)
                    {
                        regionSelectButtons[i].Pressed += () => OnRegionClicked(idx);
                    }
                }
            }

            var mgr = ClimateRestorationManager.Instance;
            if (mgr != null)
            {
                mgr.OnStateChanged += RefreshUI;
            }

            // Başlangıçta Dünya Haritası modu aktif olsun
            SwitchToMapMode();
            RefreshUI();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                // Sekme Kısayolları (1: Harita, 2: Fidan & Restorasyon, Tab: Değiştir)
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                {
                    SwitchToMapMode();
                }
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                {
                    SwitchToRestorationMode();
                }
                else if (kb.tabKey.wasPressedThisFrame)
                {
                    ToggleMode();
                }

                // Fidan ve Restorasyon Modu Açıkken Tuş Kısayolları
                if (isRestorationModeActive)
                {
                    // Boşluk tuşu ile seri fidan yetiştirme (Tıpkı FlavorCorp'un spacebar üretimi gibi!)
                    if (kb.spaceKey.wasPressedThisFrame)
                    {
                        ClimateRestorationManager.Instance?.ProduceSapling();
                    }
                    // P veya K: Paketle (3 fidanı koliye koy)
                    if (kb.pKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame)
                    {
                        ClimateRestorationManager.Instance?.PackageBox();
                    }
                    // L veya Y: Kamyona Yükle
                    if (kb.lKey.wasPressedThisFrame || kb.yKey.wasPressedThisFrame)
                    {
                        ClimateRestorationManager.Instance?.LoadBoxToTruck();
                    }
                    // G veya Enter: Sahaya Sevk Et (Kamyon yola çıksın)
                    if (kb.gKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                    {
                        ClimateRestorationManager.Instance?.DispatchTruck();
                    }
                    // 3..6: Bölge Seçimi / Kilit Açma
                    if (kb.digit3Key.wasPressedThisFrame) OnRegionClicked(0);
                    if (kb.digit4Key.wasPressedThisFrame) OnRegionClicked(1);
                    if (kb.digit5Key.wasPressedThisFrame) OnRegionClicked(2);
                    if (kb.digit6Key.wasPressedThisFrame) OnRegionClicked(3);
                }
            }

            // Sürekli akan gelir ve kamyon ilerlemesini her karede güncelle
            if (isRestorationModeActive || (panelRoot != null && panelRoot.activeInHierarchy))
            {
                RefreshLiveMetrics();
            }
        }

        public void SwitchToMapMode()
        {
            isRestorationModeActive = false;
            if (worldMapContainer != null) worldMapContainer.SetActive(true);
            if (worldMap3DContainer != null) worldMap3DContainer.SetActive(true);
            if (restorationContainer != null) restorationContainer.SetActive(false);
            if (restoration3DContainer != null) restoration3DContainer.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(false);
            UpdateTabVisuals();
            ClimateOperatorManager.Instance?.Post("DÜNYA İKLİM HARİTASI — KRİZLERE MÜDAHALE EDİN | [2] FİDAN BİRİMİ");
        }

        public void SwitchToRestorationMode()
        {
            isRestorationModeActive = true;
            if (worldMapContainer != null) worldMapContainer.SetActive(false);
            if (worldMap3DContainer != null) worldMap3DContainer.SetActive(false);
            if (restorationContainer != null) restorationContainer.SetActive(true);
            if (restoration3DContainer != null) restoration3DContainer.SetActive(true);
            if (panelRoot != null) panelRoot.SetActive(true);
            UpdateTabVisuals();
            RefreshUI();
            ClimateOperatorManager.Instance?.Post("FİDAN & RESTORASYON BİRİMİ — [BOŞLUK] FİDAN YETİŞTİR | DEVLET AFET TASARRUFU KAZAN");
        }

        public void ToggleMode()
        {
            if (isRestorationModeActive) SwitchToMapMode();
            else SwitchToRestorationMode();
        }

        void UpdateTabVisuals()
        {
            Color activeBg = new Color(0.12f, 0.32f, 0.48f, 0.95f);
            Color inactiveBg = new Color(0.06f, 0.08f, 0.12f, 0.85f);
            Color activeTxt = new Color(0.35f, 0.95f, 1f);
            Color inactiveTxt = new Color(0.60f, 0.70f, 0.80f);

            if (tabMapBg != null) tabMapBg.color = !isRestorationModeActive ? activeBg : inactiveBg;
            if (tabRestorationBg != null) tabRestorationBg.color = isRestorationModeActive ? activeBg : inactiveBg;

            if (tabMapText != null) tabMapText.color = !isRestorationModeActive ? activeTxt : inactiveTxt;
            if (tabRestorationText != null) tabRestorationText.color = isRestorationModeActive ? activeTxt : inactiveTxt;
        }

        void OnRegionClicked(int index)
        {
            var mgr = ClimateRestorationManager.Instance;
            if (mgr == null || index < 0 || index >= mgr.regions.Count) return;

            var reg = mgr.regions[index];
            if (!reg.isUnlocked)
            {
                mgr.UnlockRegion(index);
            }
            else
            {
                mgr.SelectRegion(index);
            }
        }

        public void RefreshUI()
        {
            var mgr = ClimateRestorationManager.Instance;
            if (mgr == null) return;

            // 1. Üretim İstasyonu (Fidanlık)
            if (nurseryStatusText != null)
            {
                string dots = "";
                for (int s = 0; s < ClimateRestorationManager.MaxTraySaplings; s++)
                {
                    dots += (s < mgr.nurseryTraySaplings ? "<color=#10B981>●</color> " : "<color=#475569>○</color> ");
                }
                nurseryStatusText.text = $"TEPSİ: <b><color=#10B981>{mgr.nurseryTraySaplings} / {ClimateRestorationManager.MaxTraySaplings} FİDAN</color></b> [ {dots.TrimEnd()} ]";
            }
            if (nurseryLedDotsText != null)
            {
                string dots = "";
                for (int s = 0; s < ClimateRestorationManager.MaxTraySaplings; s++)
                {
                    dots += (s < mgr.nurseryTraySaplings ? "<color=#10B981>●</color> " : "<color=#334155>○</color> ");
                }
                nurseryLedDotsText.text = dots.TrimEnd();
            }
            if (nurseryCostText != null)
            {
                nurseryCostText.text = $"Maliyet: <b>${ClimateRestorationManager.SeedCost:0.00}</b> / Fidan | [BOŞLUK / TIK]";
            }
            for (int s = 0; s < nurserySlotTexts.Length; s++)
            {
                if (nurserySlotTexts[s] != null)
                {
                    nurserySlotTexts[s].text = s < mgr.nurseryTraySaplings ? "<color=#10B981>🌱 FİDAN</color>" : "<color=#334155>· BOŞ</color>";
                }
            }

            // 2. Paketleme İstasyonu (Depo Rafı)
            if (storageStatusText != null)
            {
                string crates = "";
                for (int b = 0; b < ClimateRestorationManager.MaxBoxesInStorage; b++)
                {
                    crates += (b < mgr.packagedBoxes ? "<color=#F59E0B>📦</color> " : "<color=#475569>░░</color> ");
                }
                storageStatusText.text = $"DEPO RAFI: <b><color=#F59E0B>{mgr.packagedBoxes} / {ClimateRestorationManager.MaxBoxesInStorage} KOLİ</color></b> [ {crates.TrimEnd()} ]";
            }
            for (int b = 0; b < storageSlotTexts.Length; b++)
            {
                if (storageSlotTexts[b] != null)
                {
                    storageSlotTexts[b].text = b < mgr.packagedBoxes ? "<color=#F59E0B>📦 3x</color>" : "<color=#334155>░░</color>";
                }
            }

            // 3. Sevkiyat İstasyonu (Kamyon)
            string targetName = (mgr.selectedRegionIndex >= 0 && mgr.selectedRegionIndex < mgr.regions.Count)
                ? mgr.regions[mgr.selectedRegionIndex].name
                : "-";
            if (truckCargoText != null)
            {
                truckCargoText.text = $"YÜK: <b>{mgr.truckLoadedBoxes} / {ClimateRestorationManager.MaxTruckCapacity} KOLİ</b> | HEDEF: <color=#38BDF8>{targetName}</color>";
            }
            if (truckTargetText != null)
            {
                truckTargetText.text = $"Hedef: <color=#38BDF8><b>{targetName}</b></color>";
            }
            for (int c = 0; c < truckCargoSlotTexts.Length; c++)
            {
                if (truckCargoSlotTexts[c] != null)
                {
                    truckCargoSlotTexts[c].text = c < mgr.truckLoadedBoxes ? "<color=#06B6D4>📦</color>" : "<color=#334155>—</color>";
                }
            }

            // 4. Bölgeler
            for (int i = 0; i < mgr.regions.Count && i < regionNameTexts.Length; i++)
            {
                var reg = mgr.regions[i];
                bool isSelected = (i == mgr.selectedRegionIndex);

                if (regionCardBgs != null && i < regionCardBgs.Length && regionCardBgs[i] != null)
                {
                    regionCardBgs[i].color = isSelected ? new Color(0.08f, 0.18f, 0.28f, 0.98f) : new Color(0.06f, 0.08f, 0.12f, 0.90f);
                }

                if (regionNameTexts[i] != null)
                {
                    string prefix = isSelected ? "<color=#38BDF8>▶ </color>" : "  ";
                    string statusTag = reg.isUnlocked
                        ? "<color=#10B981>[AKTİF]</color>"
                        : $"<color=#F59E0B>[KİLİTLİ: ${reg.unlockCost:0}]</color>";
                    regionNameTexts[i].text = $"{prefix}<b>{reg.name}</b>  {statusTag}";
                }

                if (regionStatTexts[i] != null)
                {
                    if (reg.isUnlocked)
                    {
                        float rate = reg.CalculateRegionSavingsRate();
                        regionStatTexts[i].text = $"🌲 {reg.TotalTrees} Ağaç | <color=#10B981>+${rate:0.00}/sn</color> | <i>{reg.disasterRisk}</i>";
                    }
                    else
                    {
                        regionStatTexts[i].text = $"<i>{reg.disasterRisk} (Açmak için tıkla)</i>";
                    }
                }

                if (regionProgressBars != null && i < regionProgressBars.Length && regionProgressBars[i] != null)
                {
                    float p = reg.isUnlocked ? Mathf.Clamp01(reg.TotalTrees / 25f) : 0f;
                    regionProgressBars[i].Set(p);
                }
            }

            // 5. Sözleşmeler
            for (int i = 0; i < mgr.contracts.Count && i < contractTitleTexts.Length; i++)
            {
                var c = mgr.contracts[i];
                if (contractTitleTexts[i] != null)
                {
                    string status = c.isCompleted ? "<color=#10B981>✓</color> " : "• ";
                    string rewardTag = c.isCompleted ? "<color=#10B981>[TAMAMLANDI]</color>" : $"<color=#F59E0B>+${c.rewardGrant:0} HİBE</color>";
                    contractTitleTexts[i].text = $"{status}<b>{c.title}</b> ({rewardTag})";
                }

                if (contractProgressTexts[i] != null)
                {
                    if (c.isCompleted)
                    {
                        contractProgressTexts[i].text = $"<color=#10B981>Hazine Hibesi Alındı: +${c.rewardGrant:0}</color>";
                    }
                    else
                    {
                        float pct = Mathf.Clamp01((float)c.currentProgress / Mathf.Max(1, c.targetTreeCount)) * 100f;
                        contractProgressTexts[i].text = $"İlerleme: <b>{c.currentProgress} / {c.targetTreeCount}</b> (%{pct:0})";
                    }
                }

                if (contractProgressBars != null && i < contractProgressBars.Length && contractProgressBars[i] != null)
                {
                    float p = c.isCompleted ? 1f : Mathf.Clamp01((float)c.currentProgress / Mathf.Max(1, c.targetTreeCount));
                    contractProgressBars[i].Set(p, c.isCompleted ? new Color(0.10f, 0.85f, 0.45f) : new Color(0.20f, 0.70f, 0.95f));
                }
            }
        }

        void RefreshLiveMetrics()
        {
            var mgr = ClimateRestorationManager.Instance;
            if (mgr == null) return;

            // Üst Bilgi Bandı
            if (budgetText != null)
                budgetText.text = $"BÜTÇE: <b><color=#F59E0B>${mgr.budget:0.00}</color></b>";

            if (savingsText != null)
                savingsText.text = $"DEVLET TASARRUFU: <b><color=#10B981>${mgr.totalStateSavingsGenerated:0.00}</color></b>";

            float rate = mgr.GetTotalCurrentPassiveRate();
            if (rateText != null)
                rateText.text = $"HAZİNE AKIŞI: <b><color=#06B6D4>+${rate:0.00}/sn</color></b>";

            if (treeCountText != null)
                treeCountText.text = $"DİKİLEN AĞAÇ: <b>{mgr.totalTreesPlanted}</b>";

            if (carbonText != null)
                carbonText.text = $"KARBON TUTUMU: <b>{mgr.totalCarbonTonOffset:0.1} Ton</b>";

            // Kamyon Durumu
            if (truckStatusText != null)
            {
                switch (mgr.truckStatus)
                {
                    case TruckStatus.IdleInDepot:
                        truckStatusText.text = "DURUM: <color=#10B981><b>DEPOSUNDA HAZIR</b></color>";
                        break;
                    case TruckStatus.EnRouteToRegion:
                        float pct1 = Mathf.Clamp01(mgr.truckTimer / mgr.truckTripDuration) * 100f;
                        truckStatusText.text = $"DURUM: <color=#F59E0B><b>SAHAYA GİDİYOR... (%{pct1:0})</b></color>";
                        break;
                    case TruckStatus.PlantingOnSite:
                        truckStatusText.text = "DURUM: <color=#06B6D4><b>FİDANLAR DİKİLİYOR...</b></color>";
                        break;
                    case TruckStatus.ReturningToDepot:
                        float pct2 = Mathf.Clamp01(mgr.truckTimer / mgr.truckTripDuration) * 100f;
                        truckStatusText.text = $"DURUM: <color=#F59E0B><b>DEPOYA DÖNÜYOR... (%{pct2:0})</b></color>";
                        break;
                }
            }

            if (truckProgressBar != null)
            {
                if (mgr.truckStatus == TruckStatus.EnRouteToRegion || mgr.truckStatus == TruckStatus.ReturningToDepot)
                {
                    float p = Mathf.Clamp01(mgr.truckTimer / mgr.truckTripDuration);
                    truckProgressBar.Set(p, new Color(0.2f, 0.75f, 0.95f));
                }
                else if (mgr.truckStatus == TruckStatus.PlantingOnSite)
                {
                    truckProgressBar.Set(1f, new Color(0.1f, 0.9f, 0.4f));
                }
                else
                {
                    truckProgressBar.Set(0f);
                }
            }
        }
    }
}
