using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// İklim Operatörü HUD Yöneticisi:
    /// - Para ($), Halk Mutluluğu (%0-100), İklim Dengesi (%0-100) ve 6 Dakikalık Vardiya Saati.
    /// - Nişangah (Crosshair) ve üzerine gelinen kriz/buton bilgisi (Hover tooltip).
    /// - Bilgi ve bildirim bandı (Maaş, kriz alarmları, sonuçlar).
    /// - Oyun sonu (Win/Loss) ekranı ve istatistik raporu.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        public static HudController Instance { get; private set; }

        [Header("Kaynak Barları ve Sayaçlar")]
        public UIBar climateBar;
        public Text climateText;

        public UIBar mentalBar; // Halk Mutluluğu
        public Text mentalText;

        public Text moneyText;
        public Text dateText; // Vardiya Saati
        public UIBar timeBar; // Vardiya İlerlemesi

        public Text crisisCountText;

        [Header("Etkileşim / Nişangah UI")]
        public PlayerInteractor interactor;
        public Text hoverLabelText;
        public Image crosshairImage;

        [Header("Bildirim / Mesaj Bandı")]
        public Text notificationText;

        [Header("Oyun Sonu (Win/Loss) Ekranı")]
        public GameObject endScreenRoot;
        public Text endTitleText;
        public Text endReasonText;

        float updateTimer;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var mgr = ClimateOperatorManager.Instance;
            if (mgr == null) return;

            // 1. Nişangah ve hover metni
            if (hoverLabelText != null && interactor != null)
            {
                var hovered = interactor.Hovered;
                if (hovered != null && !string.IsNullOrEmpty(hovered.HoverLabel))
                {
                    hoverLabelText.text = $"[ <b>{hovered.HoverLabel}</b> ]";
                    if (crosshairImage != null) crosshairImage.color = new Color(0.2f, 1f, 1f, 1f);
                }
                else
                {
                    hoverLabelText.text = "";
                    if (crosshairImage != null) crosshairImage.color = new Color(1f, 1f, 1f, 0.4f);
                }
            }

            // 2. Bildirim bandı (Son mesaj)
            if (notificationText != null)
            {
                if (Time.time - mgr.LastMessageTime < 6f)
                {
                    notificationText.text = mgr.LastMessage;
                }
                else
                {
                    notificationText.text = "İKLİM OPERATÖRÜ MERKEZİ — DÜNYA HARİTASINDAKİ KRİZLERE MÜDAHALE EDİN";
                }
            }

            // 3. Durum göstergelerini güncelle
            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0f)
            {
                updateTimer = 0.08f;
                RefreshHud(mgr);
            }

            // 4. Oyun Sonu Ekranı
            if (mgr.State != GameState.Playing)
            {
                if (endScreenRoot != null && !endScreenRoot.activeSelf)
                {
                    endScreenRoot.SetActive(true);
                    if (endTitleText != null)
                    {
                        endTitleText.text = mgr.State == GameState.Won
                            ? "<color=#55FF55>TEBRİKLER! DÜNYA KURTULDU!</color>\n(VARDİYA BAŞARIYLA TAMAMLANDI)"
                            : "<color=#FF4444>GÖREV BAŞARISIZ! DÜNYA ÇÖKTÜ!</color>\n(GAME OVER)";
                    }
                    if (endReasonText != null)
                    {
                        endReasonText.text =
                            $"{mgr.EndReason}\n\n" +
                            $"<b>İSTATİSTİKLER:</b>\n" +
                            $"• Müdahale Edilen Krizler : <color=#55FF55><b>{mgr.ResolvedCount}</b></color>\n" +
                            $"• Kaçırılan Krizler      : <color=#FF4444><b>{mgr.MissedCount}</b></color>\n" +
                            $"• Kalan Bütçe            : <color=#FFFF55><b>{mgr.Money}$</b></color>\n" +
                            $"• Nihai İklim Dengesi    : %{mgr.Iklim:0}\n" +
                            $"• Nihai Halk Mutluluğu   : %{mgr.Halk:0}\n\n" +
                            $"<color=#FFFF55><b>[ R ] TUŞUNA BASARAK YENİDEN BAŞLATIN</b></color>";
                    }
                }

                var kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }

        void RefreshHud(ClimateOperatorManager mgr)
        {
            // İklim Dengesi
            if (climateBar != null)
            {
                float c01 = mgr.Iklim / 100f;
                Color col = c01 < 0.25f ? new Color(0.95f, 0.15f, 0.15f) :
                            c01 < 0.50f ? new Color(1f, 0.65f, 0.1f) :
                                          new Color(0.2f, 0.9f, 0.4f);
                climateBar.Set(c01, col);
            }
            if (climateText != null)
            {
                string warn = mgr.Iklim < 25 ? " <color=#FF0000><b>[KRİTİK!]</b></color>" : "";
                climateText.text = $"İKLİM DENGESİ: %{mgr.Iklim:0}{warn}";
            }

            // Halk Mutluluğu
            if (mentalBar != null)
            {
                float m01 = mgr.Halk / 100f;
                Color col = m01 < 0.25f ? new Color(0.95f, 0.15f, 0.15f) :
                            m01 < 0.50f ? new Color(1f, 0.75f, 0.2f) :
                                          new Color(0.3f, 0.75f, 1f);
                mentalBar.Set(m01, col);
            }
            if (mentalText != null)
            {
                string warn = mgr.Halk < 25 ? " <color=#FF0000><b>[İSYAN EŞİĞİ!]</b></color>" : "";
                mentalText.text = $"HALK MUTLULUĞU: %{mgr.Halk:0}{warn}";
            }

            // Para / Bütçe
            if (moneyText != null)
                moneyText.text = $"BÜTÇE: <color=#55FF55>${mgr.Money}</color>";

            // Vardiya Saati
            if (dateText != null)
                dateText.text = $"VARDİYA: <b>{mgr.ShiftClockString}</b>";
            if (timeBar != null)
                timeBar.Set(mgr.ShiftProgress01, new Color(0.6f, 0.7f, 0.9f));

            // Kriz Sayaçları
            if (crisisCountText != null)
            {
                crisisCountText.text =
                    $"Müdahale: <color=#55FF55><b>{mgr.ResolvedCount}</b></color>  |  " +
                    $"Kaçırılan: <color=#FF4444><b>{mgr.MissedCount}</b></color>";
            }
        }
    }
}
