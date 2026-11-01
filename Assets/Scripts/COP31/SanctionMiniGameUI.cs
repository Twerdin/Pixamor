using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// V3: Aqua-Modern Yaptırım Mini-Oyunları (In-OS Micro-Games):
    /// Doğru çelişki yakalandığında panel_minigame_frame.png içinde 6 saniyelik şu 2 mini-oyundan biri rastgele açılır:
    /// 1. Kanıt Frekansı Kilitleme (Timing Bar): Hızla sağa-sola kayan minigame_cursor_needle.png imlecini,
    ///    minigame_target_zone.png (su yeşili bölge) üzerindeyken SPACE veya Sol Tık ile 3 kez yakalama.
    /// 2. Veri Karartma Düğümlerini Temizleme (3x3 Grid Reflex): 3x3 ızgarada rastgele beliren 5 adet
    ///    icon_node_corrupt.png (bozuk veri) düğümüne süre bitmeden tıklayıp icon_node_verified.png (onaylı kanıt) haline getirme.
    /// </summary>
    public class SanctionMiniGameUI : MonoBehaviour
    {
        public static SanctionMiniGameUI Instance { get; private set; }

        public enum MiniGameMode
        {
            None = 0,
            TimingBar = 1,
            GridReflex = 2
        }

        [Header("Ana Mini-Oyun Penceresi (panel_minigame_frame.png)")]
        public GameObject modalRoot;
        public Text titleText;
        public Text subtitleText;
        public Text statusProgressText;
        public UIBar timerBar;

        [Header("Mod 1: Kanıt Frekansı Kilitleme (Timing Bar)")]
        public GameObject timingModeRoot;
        public GameObject timingTriggersRoot;
        public RectTransform timingTrackRt;
        public RectTransform timingTargetZoneRt;
        public RectTransform timingCursorRt;
        public PressButton timingLock3DButton;
        public Button timingLockUIButton;

        [Header("Mod 2: Veri Karartma Düğümlerini Temizleme (3x3 Grid Reflex)")]
        public GameObject gridModeRoot;
        public GameObject gridTriggersRoot;
        public Image[] gridNodeImages = new Image[9];
        public Image[] gridNodeIcons = new Image[9];
        public Text[] gridNodeTexts = new Text[9];
        public PressButton[] gridNode3DButtons = new PressButton[9];
        public Button[] gridNodeUIButtons = new Button[9];

        public bool IsActive => currentMode != MiniGameMode.None && modalRoot != null && modalRoot.activeSelf;

        MiniGameMode currentMode = MiniGameMode.None;
        Action<bool> onCompleteCallback;
        float timeRemaining;
        float maxDuration = 4.5f;

        // Mod 1 (Timing Bar) Değişkenleri
        float cursorPos01;
        float cursorSpeed = 1.75f;
        int cursorDir = 1;
        float targetMin01 = 0.35f;
        float targetMax01 = 0.65f;
        int timingHits;
        int timingMisses;
        const int RequiredTimingHits = 3;
        float inputCooldown;

        // Mod 2 (Grid Reflex) Değişkenleri
        readonly bool[] nodeIsCorrupted = new bool[9];
        int remainingCorruptedNodes;
        int cleanedNodesCount;
        const int TargetNodesToClean = 6;

        void OnEnable()
        {
            Instance = this;
        }

        void Awake()
        {
            Instance = this;
            EnsureUIBindings();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            EnsureUIBindings();

            if (timingLock3DButton != null)
                timingLock3DButton.Pressed += OnTimingLockAttempt;

            if (timingLockUIButton != null)
            {
                timingLockUIButton.onClick.RemoveAllListeners();
                timingLockUIButton.onClick.AddListener(OnTimingLockAttempt);
            }

            for (int i = 0; i < 9; i++)
            {
                int idx = i;
                if (gridNode3DButtons != null && idx < gridNode3DButtons.Length && gridNode3DButtons[idx] != null)
                {
                    gridNode3DButtons[idx].Pressed += () => OnGridNodeClicked(idx);
                }
                if (gridNodeUIButtons != null && idx < gridNodeUIButtons.Length && gridNodeUIButtons[idx] != null)
                {
                    gridNodeUIButtons[idx].onClick.RemoveAllListeners();
                    gridNodeUIButtons[idx].onClick.AddListener(() => OnGridNodeClicked(idx));
                }
            }

            if (modalRoot != null)
                modalRoot.SetActive(false);
        }

        /// <summary>
        /// KURAL 8: Sıfır Hata (Zero-Error) Çift Yönlü UI Bağlama Koruması
        /// </summary>
        public void EnsureUIBindings()
        {
            if (modalRoot == null)
            {
                var dossier = GameObject.Find("Ministry_Dossier_Root");
                if (dossier != null)
                {
                    var t = dossier.transform.Find("Sanction_MiniGame_Modal");
                    if (t != null) modalRoot = t.gameObject;
                }
            }

            if (modalRoot != null)
            {
                if (timingModeRoot == null)
                {
                    var t = modalRoot.transform.Find("MiniGame_Overlay_Canvas/MiniGame_Window/Timing_Container");
                    if (t != null) timingModeRoot = t.gameObject;
                }
                if (gridModeRoot == null)
                {
                    var t = modalRoot.transform.Find("MiniGame_Overlay_Canvas/MiniGame_Window/Grid_Container");
                    if (t != null) gridModeRoot = t.gameObject;
                }
            }
        }

        public void StartRandomMiniGame(Action<bool> callback)
        {
            EnsureUIBindings();
            onCompleteCallback = callback;
            maxDuration = 6.0f;
            timeRemaining = maxDuration;
            inputCooldown = 0.16f;

            if (modalRoot != null)
                modalRoot.SetActive(true);

            bool pickTiming = UnityEngine.Random.value < 0.5f;
            if (pickTiming)
                SetupTimingBarGame();
            else
                SetupGridReflexGame();
        }

        void SetupTimingBarGame()
        {
            currentMode = MiniGameMode.TimingBar;
            if (timingModeRoot != null) timingModeRoot.SetActive(true);
            if (timingTriggersRoot != null) timingTriggersRoot.SetActive(true);
            if (gridModeRoot != null) gridModeRoot.SetActive(false);
            if (gridTriggersRoot != null) gridTriggersRoot.SetActive(false);

            timingHits = 0;
            timingMisses = 0;
            cursorPos01 = 0.10f;
            cursorDir = 1;
            cursorSpeed = 1.75f;

            RandomizeTimingTargetZone();

            if (titleText != null)
                titleText.text = "<color=#EF4444>KANIT FREKANSI KİLİTLEME PROTOKOLÜ (4.5 SN)</color>";
            if (subtitleText != null)
                subtitleText.text = "İmleç dar hedef bölgeye girdiğinde SPACE veya Sol Tık ile 3 kez kilitleyin!";

            UpdateTimingStatusText();
            Sfx.Play(SfxType.Open);
        }

        void RandomizeTimingTargetZone()
        {
            // V4: Daraltılmış hedef bölge (0.13 - 0.18)
            float width01 = Mathf.Clamp(0.18f - timingHits * 0.025f, 0.13f, 0.18f);
            float center01 = UnityEngine.Random.Range(0.24f, 0.76f);
            targetMin01 = Mathf.Clamp01(center01 - width01 * 0.5f);
            targetMax01 = Mathf.Clamp01(center01 + width01 * 0.5f);

            if (timingTargetZoneRt != null && timingTrackRt != null)
            {
                float trackW = timingTrackRt.rect.width > 10f ? timingTrackRt.rect.width : 360f;
                float x = targetMin01 * trackW;
                float w = (targetMax01 - targetMin01) * trackW;
                Greybox.Place(timingTargetZoneRt, x, 2f, w, 24f);
            }
        }

        void SetupGridReflexGame()
        {
            currentMode = MiniGameMode.GridReflex;
            if (timingModeRoot != null) timingModeRoot.SetActive(false);
            if (timingTriggersRoot != null) timingTriggersRoot.SetActive(false);
            if (gridModeRoot != null) gridModeRoot.SetActive(true);
            if (gridTriggersRoot != null) gridTriggersRoot.SetActive(true);

            cleanedNodesCount = 0;
            remainingCorruptedNodes = 0;
            for (int i = 0; i < 9; i++)
            {
                nodeIsCorrupted[i] = false;
                RefreshGridNodeVisual(i);
            }

            // 5 adet bozuk veri düğümü (icon_node_corrupt.png) oluştur
            SpawnCorruptedNodes(TargetNodesToClean);

            if (titleText != null)
                titleText.text = "<color=#EF4444>VERİ KARARTMA DÜĞÜMLERİNİ TEMİZLEME (4.5 SN)</color>";
            if (subtitleText != null)
                subtitleText.text = "6 adet kırmızı bozuk düğüme hızla tıklayarak mavi 'Onaylı Kanıt' haline getirin!";

            UpdateGridStatusText();
            Sfx.Play(SfxType.Open);
        }

        void SpawnCorruptedNodes(int count)
        {
            int spawned = 0;
            int safety = 30;
            while (spawned < count && safety-- > 0)
            {
                int idx = UnityEngine.Random.Range(0, 9);
                if (!nodeIsCorrupted[idx])
                {
                    nodeIsCorrupted[idx] = true;
                    remainingCorruptedNodes++;
                    spawned++;
                    RefreshGridNodeVisual(idx);
                }
            }
        }

        void RefreshGridNodeVisual(int idx)
        {
            if (idx < 0 || idx >= 9) return;
            bool corrupted = nodeIsCorrupted[idx];

            if (gridNodeImages != null && idx < gridNodeImages.Length && gridNodeImages[idx] != null)
            {
                Greybox.ApplySprite(gridNodeImages[idx], corrupted ? "btn_sanction_strike" : "btn_choice_aqua", sliced: true);
            }

            if (gridNodeIcons != null && idx < gridNodeIcons.Length && gridNodeIcons[idx] != null)
            {
                Greybox.ApplySprite(gridNodeIcons[idx], corrupted ? "icon_node_corrupt" : "icon_node_verified", sliced: false);
            }

            if (gridNodeTexts != null && idx < gridNodeTexts.Length && gridNodeTexts[idx] != null)
            {
                gridNodeTexts[idx].text = corrupted
                    ? "<color=#FB7185><b>KARARTMA\nTEMİZLE</b></color>"
                    : "<color=#CCFBF1><b>ONAYLI\nKANIT</b></color>";
            }
        }

        void Update()
        {
            if (!IsActive) return;

            float dt = Time.deltaTime;
            if (inputCooldown > 0f) inputCooldown -= dt;

            timeRemaining -= dt;
            if (timerBar != null)
            {
                float t01 = Mathf.Clamp01(timeRemaining / maxDuration);
                Color barCol = t01 > 0.35f
                    ? Color.white
                    : new Color(0.98f, 0.44f, 0.52f, 1f); // #FB7185 Mercan
                timerBar.Set(t01, barCol);
            }

            if (timeRemaining <= 0f)
            {
                CompleteMiniGame(false);
                return;
            }

            if (currentMode == MiniGameMode.TimingBar)
            {
                TickTimingBar(dt);
            }
        }

        void TickTimingBar(float dt)
        {
            cursorPos01 += cursorDir * cursorSpeed * dt;
            if (cursorPos01 >= 1f)
            {
                cursorPos01 = 1f;
                cursorDir = -1;
            }
            else if (cursorPos01 <= 0f)
            {
                cursorPos01 = 0f;
                cursorDir = 1;
            }

            if (timingCursorRt != null && timingTrackRt != null)
            {
                float trackW = timingTrackRt.rect.width > 10f ? timingTrackRt.rect.width : 360f;
                float cx = Mathf.Clamp(cursorPos01 * (trackW - 14f), 0f, trackW - 14f);
                Greybox.Place(timingCursorRt, cx, -3f, 14f, 34f);
            }

            if (inputCooldown <= 0f)
            {
                bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
                bool leftClickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                if (spacePressed || leftClickPressed)
                {
                    OnTimingLockAttempt();
                }
            }
        }

        public void OnTimingLockAttempt()
        {
            if (currentMode != MiniGameMode.TimingBar || inputCooldown > 0f) return;
            inputCooldown = 0.14f;

            if (cursorPos01 >= (targetMin01 - 0.035f) && cursorPos01 <= (targetMax01 + 0.035f))
            {
                timingHits++;
                Sfx.Play(SfxType.Success);

                if (timingHits >= RequiredTimingHits)
                {
                    CompleteMiniGame(true);
                    return;
                }

                cursorSpeed += 0.24f;
                RandomizeTimingTargetZone();
                UpdateTimingStatusText();
            }
            else
            {
                timingMisses++;
                Sfx.Play(SfxType.Alarm);
                UpdateTimingStatusText();

                if (timingMisses >= 3)
                {
                    CompleteMiniGame(false);
                }
            }
        }

        void UpdateTimingStatusText()
        {
            if (statusProgressText != null)
            {
                statusProgressText.text =
                    $"Kilitlenen Kanıt: <color=#5EEAD4><b>{timingHits} / {RequiredTimingHits}</b></color>    " +
                    $"Hata Payı: <color=#FB7185><b>{timingMisses} / 3</b></color>";
            }
        }

        public void OnGridNodeClicked(int idx)
        {
            if (currentMode != MiniGameMode.GridReflex || idx < 0 || idx >= 9) return;

            if (nodeIsCorrupted[idx])
            {
                nodeIsCorrupted[idx] = false;
                remainingCorruptedNodes = Mathf.Max(0, remainingCorruptedNodes - 1);
                cleanedNodesCount++;
                Sfx.Play(SfxType.Click);
                RefreshGridNodeVisual(idx);
                UpdateGridStatusText();

                if (cleanedNodesCount >= TargetNodesToClean || remainingCorruptedNodes == 0)
                {
                    CompleteMiniGame(true);
                }
            }
        }

        void UpdateGridStatusText()
        {
            if (statusProgressText != null)
            {
                statusProgressText.text =
                    $"Onaylanan Kanıt: <color=#5EEAD4><b>{cleanedNodesCount} / {TargetNodesToClean}</b></color>    " +
                    $"Kalan Anomali: <color=#FB7185><b>{remainingCorruptedNodes}</b></color>";
            }
        }

        void CompleteMiniGame(bool success)
        {
            currentMode = MiniGameMode.None;
            if (modalRoot != null)
                modalRoot.SetActive(false);

            var cb = onCompleteCallback;
            onCompleteCallback = null;
            cb?.Invoke(success);
        }

        public void CancelMiniGame()
        {
            currentMode = MiniGameMode.None;
            onCompleteCallback = null;
            if (modalRoot != null)
                modalRoot.SetActive(false);
        }
    }
}
