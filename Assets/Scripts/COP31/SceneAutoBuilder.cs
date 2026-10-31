using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pixamor.COP31
{
    /// <summary>
    /// İklim Operatörü Sahne Mimarı (Minimalist & Ultra Sade Düzen):
    /// - Masanın üzerinde SADECE taktik operatör monitörü bulunur; tüm gereksiz masa aletleri kaldırılmıştır.
    /// - Monitörün üzerinde aşırı sade ve düzenli bir arayüz yer alır:
    ///   * Üst İnce Band: İklim Dengesi, Halk Mutluluğu, Bütçe ve Vardiya Saati.
    ///   * Ana Alan: Birebir gerçek dünya haritası ve gerçek koordinatlı 12 kriz pini.
    ///   * Alt İnce Band: Canlı bildirim/durum şeridi.
    ///   * Olay Kartı: Kriz tıklandığında açılan sade, modern, 3 tercihi şeffaf sunan brifing paneli.
    /// - Tüm metin ve ekranlar oyuncunun bakış açısına tam dik (12° eğimli), net ve unmirrored'dır.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneAutoBuilder : MonoBehaviour
    {
        [Header("Otomatik Kurulum")]
        public bool buildOnAwake = true;

        const string RootName = "=== COP31_ENVIRONMENT ===";

        void Awake()
        {
            if (buildOnAwake && GameObject.Find(RootName) == null)
            {
                BuildScene();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeBootstrap()
        {
            if (GameObject.Find(RootName) == null)
            {
                var existing = Object.FindAnyObjectByType<SceneAutoBuilder>();
                if (existing != null)
                {
                    existing.BuildScene();
                }
                else
                {
                    var go = new GameObject("SceneAutoBuilder_Runtime");
                    var builder = go.AddComponent<SceneAutoBuilder>();
                    builder.BuildScene();
                }
            }
        }

#if UNITY_EDITOR
        [MenuItem("COP31/Sahneyi Yeniden Kur (Build Scene)", false, 10)]
        public static void BuildSceneMenuItem()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            var builder = FindAnyObjectByType<SceneAutoBuilder>();
            if (builder == null)
            {
                var go = new GameObject("SceneAutoBuilder");
                builder = go.AddComponent<SceneAutoBuilder>();
                Undo.RegisterCreatedObjectUndo(go, "Create SceneAutoBuilder");
            }

            builder.BuildScene();
            Debug.Log("<color=#55FF55>[COP31] Sahne başarıyla sıfırdan inşa edildi!</color>");
        }
#endif

        [ContextMenu("Sahneyi Sıfırdan İnşa Et")]
        public void BuildScene()
        {
            var old = GameObject.Find(RootName);
            if (old != null) Greybox.Kill(old);

            var root = new GameObject(RootName).transform;
            root.position = Vector3.zero;
            root.rotation = Quaternion.identity;

            // 1. Ses Yöneticisi
            var sfxGo = new GameObject("SFX_Manager");
            sfxGo.transform.SetParent(root);
            sfxGo.AddComponent<Sfx>();

            // 2. Kamera ve Oyuncu Etkileşimi
            Camera cam = SetupCamera(root);
            var interactor = cam.gameObject.AddComponent<PlayerInteractor>();
            interactor.Setup(cam, 12f);

            // 3. Sade Oda Mimarisi
            BuildRoom(root);

            // 4. Oyun ve Arayüz Yöneticileri
            var mgr = root.gameObject.AddComponent<ClimateOperatorManager>();
            var hud = root.gameObject.AddComponent<HudController>();
            hud.interactor = interactor;
            var cardUi = root.gameObject.AddComponent<EventCardUI>();
            var mapController = root.gameObject.AddComponent<WorldMapMonitorController>();
            var crisisSpawner = root.gameObject.AddComponent<ContinentCrisisSpawner>();
            var blindUi = root.gameObject.AddComponent<BlindChoiceDialogueUI>();
            var restorationMgr = root.gameObject.AddComponent<ClimateRestorationManager>();
            var restorationUi = root.gameObject.AddComponent<ClimateRestorationUI>();

            // 5. Sade Masa ve Monitör (Masa üzerinde SADECE monitör vardır!)
            BuildMinimalDeskAndMonitor(root, mgr, hud, cardUi, mapController, crisisSpawner, blindUi, restorationMgr, restorationUi);

            // 6. Ekran Nişangahı ve Oyun Sonu (GameOver/Win) Arayüzü
            BuildCrosshairCanvas(cam.transform, hud);
        }

        // ============================================================ 1. KAMERA

        Camera SetupCamera(Transform root)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }

            cam.transform.SetParent(root, false);
            // Oyuncunun koltuktaki göz hizası
            cam.transform.localPosition = new Vector3(0f, 1.45f, -1.20f);
            cam.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.09f);

            return cam;
        }

        // ============================================================ 2. SADE ODA MİMARİSİ

        void BuildRoom(Transform root)
        {
            var room = Greybox.Empty("Room_Geometry", root, Vector3.zero);

            Color floorCol = new Color(0.09f, 0.10f, 0.12f);
            Color wallCol = new Color(0.12f, 0.14f, 0.17f);
            Color frontWallCol = new Color(0.11f, 0.13f, 0.15f);

            // Zemin (9m x 8m)
            Greybox.Box("Floor", room, new Vector3(0f, -0.05f, 0.8f), new Vector3(9f, 0.1f, 8f), floorCol);
            // Tavan
            Greybox.Box("Ceiling", room, new Vector3(0f, 4.0f, 0.8f), new Vector3(9f, 0.1f, 8f), wallCol);
            // Duvarlar
            Greybox.Box("FrontWall", room, new Vector3(0f, 2.0f, 4.2f), new Vector3(9f, 4.0f, 0.2f), frontWallCol);
            Greybox.Box("BackWall", room, new Vector3(0f, 2.0f, -2.8f), new Vector3(9f, 4.0f, 0.2f), wallCol);
            Greybox.Box("LeftWall", room, new Vector3(-4.5f, 2.0f, 0.8f), new Vector3(0.2f, 4.0f, 8f), wallCol);
            Greybox.Box("RightWall", room, new Vector3(4.5f, 2.0f, 0.8f), new Vector3(0.2f, 4.0f, 8f), wallCol);

            // Sade Koltuk (Dekoratif - oyuncu hareketini engellemez)
            var chair = Greybox.Empty("Minister_Chair", room, new Vector3(0f, 0.5f, -1.35f));
            Greybox.Box("Chair_Seat", chair, Vector3.zero, new Vector3(0.65f, 0.12f, 0.65f), new Color(0.08f, 0.09f, 0.11f), unlit: false, collider: false);
            Greybox.Box("Chair_Back", chair, new Vector3(0f, 0.45f, -0.3f), new Vector3(0.65f, 0.8f, 0.12f), new Color(0.08f, 0.09f, 0.11f), unlit: false, collider: false);

            // Yumuşak Işıklandırma
            var lights = Greybox.Empty("Room_Lighting", root, Vector3.zero);
            Greybox.PointLight("Ceiling_Soft", lights, new Vector3(0f, 3.6f, 0.5f), new Color(0.88f, 0.92f, 1f), 1.2f, 12f);
            Greybox.PointLight("Desk_Soft", lights, new Vector3(0f, 2.3f, -0.1f), new Color(0.95f, 0.98f, 1f), 1.0f, 4.5f);
        }

        // ============================================================ 3. SADE MASA VE MONİTÖR

        void BuildMinimalDeskAndMonitor(Transform root, ClimateOperatorManager mgr, HudController hud, EventCardUI cardUi, WorldMapMonitorController mapController, ContinentCrisisSpawner crisisSpawner, BlindChoiceDialogueUI blindUi, ClimateRestorationManager restorationMgr, ClimateRestorationUI restorationUi)
        {
            var deskRoot = Greybox.Empty("Desk_Environment", root, new Vector3(0f, 0.85f, -0.25f));

            // Sade Masa Üst Tablası (Genişlik: 2.4m, Derinlik: 1.0m)
            Color deskCol = new Color(0.10f, 0.12f, 0.14f);
            Greybox.Box("Desk_Top", deskRoot, Vector3.zero, new Vector3(2.4f, 0.08f, 1.0f), deskCol, unlit: false, collider: false);

            // Minimalist Masa Ayakları
            Greybox.Box("Desk_Leg_L", deskRoot, new Vector3(-1.1f, -0.42f, 0f), new Vector3(0.08f, 0.76f, 0.8f), new Color(0.06f, 0.07f, 0.09f));
            Greybox.Box("Desk_Leg_R", deskRoot, new Vector3(1.1f, -0.42f, 0f), new Vector3(0.08f, 0.76f, 0.8f), new Color(0.06f, 0.07f, 0.09f));

            // ------------------------------------------------------------ SADECE MONİTÖR (12° Eğimli)
            var monitorRoot = Greybox.Empty("Tactical_Monitor", deskRoot, new Vector3(0f, 0.46f, 0.12f), new Vector3(12f, 0f, 0f));
            if (hud != null && hud.interactor != null)
            {
                hud.interactor.monitorTarget = monitorRoot;
            }

            // Monitör Ayağı (Masaya bağlanan şık metal ayak)
            Greybox.Box("Monitor_Stand_Base", monitorRoot, new Vector3(0f, -0.44f, 0f), new Vector3(0.40f, 0.02f, 0.22f), new Color(0.08f, 0.09f, 0.11f), unlit: false, collider: false);
            Greybox.Box("Monitor_Stand_Pillar", monitorRoot, new Vector3(0f, -0.22f, 0.04f), new Vector3(0.08f, 0.42f, 0.06f), new Color(0.12f, 0.14f, 0.16f), unlit: false, collider: false);

            // Monitör Kasası ve İnce Çerçeve (1.60m x 0.90m)
            Greybox.Box("Monitor_Frame", monitorRoot, Vector3.zero, new Vector3(1.60f, 0.90f, 0.04f), new Color(0.06f, 0.07f, 0.09f), unlit: false, collider: false);

            // World-Space Canvas (1.56m x 0.86m, 400 PPM -> 624 x 344 piksel)
            float ppm = 400f;
            var canvasRt = Greybox.WorldCanvas("Monitor_Canvas", monitorRoot, new Vector3(0f, 0f, -0.022f), Vector3.zero, new Vector2(1.56f, 0.86f), ppm);

            var mCanvas = canvasRt.GetComponent<Canvas>();
            if (mCanvas != null)
            {
                mCanvas.overrideSorting = true;
                mCanvas.sortingOrder = 1;
            }

            var screenBg = Greybox.Child(canvasRt, "Screen_BG", new Color(0.03f, 0.04f, 0.07f, 1f));
            Greybox.Stretch(screenBg);

            // ------------------------------------------------------------ A. AŞIRI SADE ÜST GÖSTERGE BANDI
            BuildMinimalHeader(screenBg, monitorRoot, hud, ppm);

            // ------------------------------------------------------------ B. MOD SEÇİM SEKME BANDI (HARİTA VS RESTORASYON)
            BuildModeTabBar(screenBg, monitorRoot, restorationUi, ppm);

            // ------------------------------------------------------------ C. GERÇEK DÜNYA HARİTASI (7 KITA & DİNAMİK RENKLER - TAB 1)
            BuildWorldMap(screenBg, monitorRoot, mgr, mapController, crisisSpawner, restorationUi, ppm);

            // ------------------------------------------------------------ D. FİDAN VE RESTORASYON BİRİMİ (4 İSTASYONLU TYCOON - TAB 2)
            BuildRestorationTycoonUI(screenBg, monitorRoot, restorationUi, ppm);

            // ------------------------------------------------------------ E. ALT SADE BİLDİRİM BANDI
            BuildMinimalFooter(screenBg, hud);

            // ------------------------------------------------------------ F. 7 KITA KÖR SEÇENEK BRİFİNG PANELİ (BLIND CHOICE DOSSIER)
            BuildBlindChoiceDossier(monitorRoot, blindUi, ppm);

            // Eski olay kartı (Gerekirse pasif olarak hazır bekler)
            if (cardUi != null)
            {
                BuildMinimalEventCard(monitorRoot, cardUi, ppm);
            }
        }

        // ============================================================ A. AŞIRI SADE ÜST GÖSTERGE BANDI

        void BuildMinimalHeader(RectTransform screenBg, Transform monitorRoot, HudController hud, float ppm)
        {
            // Tek parça ultra sade ince başlık bandı (Yükseklik: 38px)
            var headerRt = Greybox.Child(screenBg, "Header_Panel", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            Greybox.Place(headerRt, 8f, 6f, 608f, 38f);

            // 1. İklim Dengesi
            hud.climateText = Greybox.Label(headerRt, "İKLİM %70", 11, new Color(0.25f, 0.95f, 0.45f), TextAnchor.MiddleLeft, 12f, 5f, 115f, 15f, FontStyle.Bold);
            hud.climateBar = Greybox.Bar(headerRt, 12f, 22f, 110f, 6f, new Color(0.12f, 0.16f, 0.12f), new Color(0.2f, 0.9f, 0.4f));

            // 2. Halk Mutluluğu
            hud.mentalText = Greybox.Label(headerRt, "HALK %70", 11, new Color(0.25f, 0.80f, 1f), TextAnchor.MiddleLeft, 135f, 5f, 115f, 15f, FontStyle.Bold);
            hud.mentalBar = Greybox.Bar(headerRt, 135f, 22f, 110f, 6f, new Color(0.12f, 0.14f, 0.18f), new Color(0.3f, 0.75f, 1f));

            // 3. Bütçe
            hud.moneyText = Greybox.Label(headerRt, "BÜTÇE $60", 12, new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleLeft, 260f, 5f, 115f, 16f, FontStyle.Bold);
            Greybox.Label(headerRt, "(+30$/dk)", 9, new Color(0.6f, 0.7f, 0.6f), TextAnchor.MiddleLeft, 260f, 21f, 115f, 12f);

            // 4. Vardiya Saati
            hud.dateText = Greybox.Label(headerRt, "06:00", 13, Color.white, TextAnchor.MiddleRight, 380f, 5f, 115f, 16f, FontStyle.Bold);
            hud.timeBar = Greybox.Bar(headerRt, 395f, 22f, 100f, 6f, new Color(0.15f, 0.15f, 0.20f), new Color(0.7f, 0.6f, 0.9f));

            // 5. Tam Ekran Butonu [E]
            var fsBox = Greybox.Child(headerRt, "Btn_Fullscreen_Visual", new Color(0.10f, 0.22f, 0.36f, 0.95f));
            Greybox.Place(fsBox, 506f, 6f, 94f, 26f);
            Greybox.Label(fsBox, "<b>⛶ TAM EKRAN [E]</b>", 8, new Color(0.4f, 0.9f, 1f), TextAnchor.MiddleCenter, 0f, 0f, 94f, 26f);

            float fsPx = 8f + 506f + 47f;
            float fsPy = 6f + 6f + 13f;
            float fsmx = (fsPx - 312f) / ppm;
            float fsmy = -(fsPy - 172f) / ppm;
            var fsGo = Greybox.TriggerBox("Btn_Fullscreen_Trigger", monitorRoot, new Vector3(fsmx, fsmy, -0.024f), new Vector3(94f / ppm, 26f / ppm, 0.006f));
            var fsBtn = fsGo.AddComponent<PressButton>();
            fsBtn.Setup(fsGo.transform, new Vector3(0f, 0f, 0.003f), "TAM EKRAN ODAKLAN / ÇIK [E TUŞU]");
            fsBtn.Pressed += () => hud.interactor?.ToggleFullscreen();
        }

        // ============================================================ B. MOD SEÇİM SEKME BANDI

        void BuildModeTabBar(RectTransform screenBg, Transform monitorRoot, ClimateRestorationUI ui, float ppm)
        {
            var tabRt = Greybox.Child(screenBg, "Tab_Bar_Panel", new Color(0.05f, 0.07f, 0.10f, 0.95f));
            Greybox.Place(tabRt, 8f, 48f, 608f, 22f);

            // Sekme 1: Dünya İklim Haritası
            var tab1Panel = Greybox.Child(tabRt, "Tab1_Bg", new Color(0.12f, 0.32f, 0.48f, 0.95f));
            Greybox.Place(tab1Panel, 0f, 0f, 300f, 22f);
            ui.tabMapBg = tab1Panel.GetComponent<Image>();
            ui.tabMapText = Greybox.Label(tab1Panel, "<b>[ 🌍 1: DÜNYA İKLİM HARİTASI ]</b>", 10, new Color(0.35f, 0.95f, 1f), TextAnchor.MiddleCenter, 0f, 0f, 300f, 22f);

            // Sekme 2: Fidan & Restorasyon Birimi
            var tab2Panel = Greybox.Child(tabRt, "Tab2_Bg", new Color(0.06f, 0.08f, 0.12f, 0.85f));
            Greybox.Place(tab2Panel, 304f, 0f, 304f, 22f);
            ui.tabRestorationBg = tab2Panel.GetComponent<Image>();
            ui.tabRestorationText = Greybox.Label(tab2Panel, "<b>[ 🌱 2: FİDAN & RESTORASYON BİRİMİ ]</b>", 10, new Color(0.60f, 0.70f, 0.80f), TextAnchor.MiddleCenter, 0f, 0f, 304f, 22f);

            // 3D Tıklanabilir Görünmez Butonlar (SIFIR SİYAH KUTU!)
            var tabBtnRoot = Greybox.Empty("Tab_3D_Buttons", monitorRoot, Vector3.zero);

            float t1x = (158f - 312f) / ppm;
            float t1y = -(59f - 172f) / ppm;
            var t1Go = Greybox.TriggerBox("Btn_Tab_Map", tabBtnRoot, new Vector3(t1x, t1y, -0.024f), new Vector3(300f / ppm, 22f / ppm, 0.006f));
            var t1Btn = t1Go.AddComponent<PressButton>();
            t1Btn.Setup(t1Go.transform, new Vector3(0f, 0f, 0.003f), "[1] DÜNYA İKLİM HARİTASI");
            ui.tabMapButton = t1Btn;

            float t2x = (464f - 312f) / ppm;
            float t2y = -(59f - 172f) / ppm;
            var t2Go = Greybox.TriggerBox("Btn_Tab_Restoration", tabBtnRoot, new Vector3(t2x, t2y, -0.024f), new Vector3(304f / ppm, 22f / ppm, 0.006f));
            var t2Btn = t2Go.AddComponent<PressButton>();
            t2Btn.Setup(t2Go.transform, new Vector3(0f, 0f, 0.003f), "[2] FİDAN & RESTORASYON BİRİMİ");
            ui.tabRestorationButton = t2Btn;
        }

        // ============================================================ C. GERÇEK DÜNYA HARİTASI (7 KITA & DİNAMİK RENKLER - TAB 1)

        void BuildWorldMap(RectTransform screenBg, Transform monitorRoot, ClimateOperatorManager mgr, WorldMapMonitorController controller, ContinentCrisisSpawner spawner, ClimateRestorationUI restorationUi, float ppm)
        {
            // 2D Kapsayıcı (Sekme geçişinde açılıp kapanır)
            var worldMap2D = Greybox.Child(screenBg, "World_Map_2D_Group", Color.clear);
            Greybox.Place(worldMap2D, 8f, 70f, 608f, 246f);
            restorationUi.worldMapContainer = worldMap2D.gameObject;

            // Üst Taktik Başlık Şeridi
            var topBar = Greybox.Child(worldMap2D, "Map_Top_Bar", new Color(0.06f, 0.09f, 0.14f, 0.96f));
            Greybox.Place(topBar, 0f, 0f, 608f, 16f);
            Greybox.Label(topBar, "<b>🌍 DÜNYA İKLİM SAĞLIĞI & KRİZ RADARI</b> — [MÜDAHALE ETMEK İÇİN KITAYA TIKLAYIN]", 8, new Color(0.35f, 0.90f, 1f), TextAnchor.MiddleLeft, 8f, 1f, 592f, 14f);

            // Harita Dokusu (world_map.png tabanlı 7 kıta dinamik dokusu)
            var mapBox = Greybox.Child(worldMap2D, "Map_Box", new Color(0.04f, 0.07f, 0.12f, 1f));
            Greybox.Place(mapBox, 0f, 18f, 608f, 212f);

            var mapGo = new GameObject("World_Map_Image", typeof(RectTransform));
            mapGo.transform.SetParent(mapBox, false);
            var mapRt = (RectTransform)mapGo.transform;
            Greybox.Stretch(mapRt);

            var rawImg = mapGo.AddComponent<RawImage>();
            rawImg.raycastTarget = false;
            controller.BindRawImage(rawImg);

            // İnce Taktik Çerçeve
            var borderRt = Greybox.Child(mapBox, "Map_Border", new Color(0.14f, 0.24f, 0.36f, 0.60f));
            Greybox.Stretch(borderRt);

            // Alt Kıta Renk Lejantı
            var legendBar = Greybox.Child(worldMap2D, "Map_Legend_Bar", new Color(0.05f, 0.07f, 0.10f, 0.95f));
            Greybox.Place(legendBar, 0f, 232f, 608f, 14f);
            Greybox.Label(legendBar, "<color=#6E0000>■ %0 Kriz (Koyu Kırmızı)</color>   <color=#B86B00>■ %50 Kritik (Kehribar)</color>   <color=#0A5C1A>■ %100 Denge (Koyu Yeşil)</color>   <color=#FF2222>⚠️ Yanıp Sönen: AKTİF KRİZ ALARMI</color>", 7, new Color(0.80f, 0.85f, 0.90f), TextAnchor.MiddleCenter, 0f, 0f, 608f, 14f);

            // 3D Kapsayıcı (Kriz pinleri ve tıklama yüzeyi)
            var worldMap3D = Greybox.Empty("World_Map_3D_Group", monitorRoot, Vector3.zero);
            restorationUi.worldMap3DContainer = worldMap3D.gameObject;

            // Monitör Yüzey Tıklama Etkileşimi (Piksel-Kusursuz Kara/Okyanus Algılama)
            var screenHitGo = Greybox.Empty("Screen_Raycast_Surface", worldMap3D, new Vector3(0f, 0f, -0.024f));
            var boxCol = screenHitGo.gameObject.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(1.56f, 0.86f, 0.01f);
            var screenInt = screenHitGo.gameObject.AddComponent<WorldMapScreenInteractable>();
            screenInt.SetHoverLabel("DÜNYA İKLİM HARİTASI (MÜDAHALE İÇİN KITAYA TIKLAYIN)");
            screenInt.screenPhysicalSize = new Vector2(1.56f, 0.86f);
            screenInt.mapPixelOffset = new Vector2(8f, 88f);
            screenInt.mapPixelSize = new Vector2(608f, 212f);
            screenInt.canvasTotalSize = new Vector2(624f, 344f);

            // 7 Kıtanın 3D Kriz Pinleri (Yalnızca Kara Pikselleri Üzerinde)
            var pinContainer = Greybox.Empty("Continent_Pin_Container", worldMap3D, Vector3.zero);

            for (int i = 0; i < 7; i++)
            {
                var cont = controller.continents[i];
                Vector2 uv = cont.primaryLandUV;

                // Harita piksel konumu (x: 8..616, y: 88..300)
                float pixelX = 8f + uv.x * 608f;
                float pixelY = 88f + (1f - uv.y) * 212f;

                // Monitör merkezine (312px, 172px) göre metre konumu
                float localMetersX = (pixelX - 312f) / ppm;
                float localMetersY = -(pixelY - 172f) / ppm;

                var pin = CreateMinimalContinentPin(pinContainer, i, cont.name, localMetersX, localMetersY, ppm);
                spawner.RegisterPin(cont.type, pin);
            }
        }

        // ============================================================ D. FİDAN VE RESTORASYON BİRİMİ (TAB 2)

        void BuildRestorationTycoonUI(RectTransform screenBg, Transform monitorRoot, ClimateRestorationUI ui, float ppm)
        {
            // 1. Ana Panel (608 x 248 piksel)
            var panelRt = Greybox.Child(screenBg, "Restoration_Tycoon_Panel", new Color(0.04f, 0.05f, 0.08f, 1f));
            Greybox.Place(panelRt, 8f, 70f, 608f, 246f);
            ui.panelRoot = panelRt.gameObject;
            ui.restorationContainer = panelRt.gameObject;

            // 3D Görünmez Butonlar Kapsayıcısı (SIFIR SİYAH KUTU!)
            var r3d = Greybox.Empty("Restoration_3D_Buttons", monitorRoot, Vector3.zero);
            ui.restoration3DContainer = r3d.gameObject;

            // ---------------------------------------------------------------- ÜST İNCE METRİK BİLGİ ŞERİDİ
            var bannerRt = Greybox.Child(panelRt, "Top_Banner", new Color(0.06f, 0.09f, 0.14f, 0.96f));
            Greybox.Place(bannerRt, 2f, 2f, 604f, 20f);

            Greybox.Label(bannerRt, "<b>🌱 DEVLET AFET ÖNLEME & RESTORASYON BİRİMİ</b>", 8, new Color(0.35f, 0.95f, 0.55f), TextAnchor.MiddleLeft, 6f, 2f, 230f, 16f);
            ui.budgetText = Greybox.Label(bannerRt, "BÜTÇE: $35.00", 9, new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleLeft, 240f, 2f, 100f, 16f, FontStyle.Bold);
            ui.savingsText = Greybox.Label(bannerRt, "TASARRUF: $0.00", 8, new Color(0.3f, 0.9f, 0.4f), TextAnchor.MiddleLeft, 345f, 2f, 135f, 16f, FontStyle.Bold);
            ui.rateText = Greybox.Label(bannerRt, "HAZİNE: +$0.00/sn", 8, new Color(0.3f, 0.85f, 1f), TextAnchor.MiddleRight, 485f, 2f, 112f, 16f, FontStyle.Bold);

            // ---------------------------------------------------------------- SOL SÜTUN: RESTORASYON SAHALARI (Width 236)
            var leftCol = Greybox.Child(panelRt, "Left_Col", new Color(0.05f, 0.07f, 0.11f, 0.96f));
            Greybox.Place(leftCol, 2f, 24f, 236f, 220f);

            Greybox.Label(leftCol, "<b>🌲 RESTORASYON SAHALARI (Pazar & Teşvik)</b>", 8, new Color(0.4f, 0.85f, 1f), TextAnchor.MiddleLeft, 6f, 2f, 224f, 13f);

            // 4 Restorasyon Bölgesi (Zengin tasarımlı kartlar)
            for (int i = 0; i < 4; i++)
            {
                float rY = 16f + i * 26f;
                var rBox = Greybox.Child(leftCol, $"Region_{i}", new Color(0.07f, 0.10f, 0.15f, 0.95f));
                Greybox.Place(rBox, 3f, rY, 230f, 25f);
                ui.regionCardBgs[i] = rBox.GetComponent<Image>();

                // Sol durum şeridi
                var strip = Greybox.Child(rBox, "Strip", i == 0 ? new Color(0.1f, 0.85f, 0.45f) : new Color(0.95f, 0.65f, 0.20f));
                Greybox.Place(strip, 0f, 0f, 3f, 25f);

                ui.regionNameTexts[i] = Greybox.Label(rBox, $"Bölge #{i + 1}", 8, Color.white, TextAnchor.MiddleLeft, 6f, 1f, 220f, 11f, FontStyle.Bold);
                ui.regionStatTexts[i] = Greybox.Label(rBox, "Yükleniyor...", 7, new Color(0.75f, 0.85f, 0.95f), TextAnchor.MiddleLeft, 6f, 11f, 142f, 11f);

                // Mini Ağaçlandırma İlerleme Barı
                ui.regionProgressBars[i] = Greybox.Bar(rBox, 152f, 13f, 72f, 5f, new Color(0.12f, 0.16f, 0.22f), new Color(0.10f, 0.85f, 0.45f));

                // 3D Görünmez Tıklama Kutusu
                float px = 8f + 2f + 3f;
                float py = 70f + 24f + rY;
                float pw = 230f;
                float ph = 25f;
                float mx = (px + pw * 0.5f - 312f) / ppm;
                float my = -(py + ph * 0.5f - 172f) / ppm;

                var bGo = Greybox.TriggerBox($"Btn_Region_{i}", r3d, new Vector3(mx, my, -0.024f), new Vector3(pw / ppm, ph / ppm, 0.006f));
                var btn = bGo.AddComponent<PressButton>();
                btn.Setup(bGo.transform, new Vector3(0f, 0f, 0.003f), $"BÖLGE #{i + 1} SEÇ / KİLİDİ AÇ [Tuş: {i + 3}]");
                ui.regionSelectButtons[i] = btn;
            }

            // Alt Kısım: Devlet Sözleşmeleri (Zengin Kart)
            var contractBox = Greybox.Child(leftCol, "Contracts_Panel", new Color(0.06f, 0.09f, 0.14f, 0.95f));
            Greybox.Place(contractBox, 3f, 122f, 230f, 96f);

            Greybox.Label(contractBox, "<b>🏛 DEVLET AFET SÖZLEŞMELERİ (Hazine Hibeleri)</b>", 8, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleLeft, 6f, 2f, 220f, 12f);

            for (int c = 0; c < 3; c++)
            {
                float cY = 16f + c * 26f;
                var cCard = Greybox.Child(contractBox, $"Contract_{c}", new Color(0.08f, 0.12f, 0.18f, 0.90f));
                Greybox.Place(cCard, 3f, cY, 224f, 24f);

                ui.contractTitleTexts[c] = Greybox.Label(cCard, $"Sözleşme #{c + 1}", 7, Color.white, TextAnchor.MiddleLeft, 4f, 1f, 216f, 10f, FontStyle.Bold);
                ui.contractProgressTexts[c] = Greybox.Label(cCard, "0 / 0", 7, new Color(0.7f, 0.9f, 0.7f), TextAnchor.MiddleLeft, 4f, 11f, 140f, 11f);

                // Mini İlerleme Barı
                ui.contractProgressBars[c] = Greybox.Bar(cCard, 146f, 13f, 72f, 5f, new Color(0.12f, 0.16f, 0.22f), new Color(0.20f, 0.70f, 0.95f));
            }

            // ---------------------------------------------------------------- ORTA SÜTUN: DEPO & SEVKİYAT LOJİSTİĞİ (Width 204)
            var midCol = Greybox.Child(panelRt, "Mid_Col", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            Greybox.Place(midCol, 240f, 24f, 206f, 220f);

            // 1. İstasyon: Paketleme & Depo Rafı (Görsel Depo Çerçevesi)
            Greybox.Label(midCol, "<b>📦 BİYO-KÖK KOLİLEME (Depo Rafı)</b>", 8, new Color(1f, 0.65f, 0.25f), TextAnchor.MiddleLeft, 6f, 2f, 194f, 13f);
            ui.storageStatusText = Greybox.Label(midCol, "DEPO RAFI: 0/4 KOLİ", 8, Color.white, TextAnchor.MiddleLeft, 6f, 14f, 194f, 12f);

            // Görsel Depo Rafı Yuvaları
            var shelfBox = Greybox.Child(midCol, "Shelf_Frame", new Color(0.08f, 0.11f, 0.16f, 0.95f));
            Greybox.Place(shelfBox, 4f, 27f, 198f, 32f);

            for (int b = 0; b < 4; b++)
            {
                var slotBg = Greybox.Child(shelfBox, $"Shelf_Slot_{b}", new Color(0.10f, 0.14f, 0.20f, 0.90f));
                Greybox.Place(slotBg, 4f + b * 48f, 3f, 44f, 26f);
                ui.storageSlotTexts[b] = Greybox.Label(slotBg, "░░", 8, new Color(0.6f, 0.7f, 0.8f), TextAnchor.MiddleCenter, 0f, 0f, 44f, 26f, FontStyle.Bold);
            }

            // Buton: Kolile (3 Fidan -> 1 Koli)
            var packBox = Greybox.Child(midCol, "Pack_Btn_Visual", new Color(0.06f, 0.32f, 0.18f, 0.95f));
            Greybox.Place(packBox, 4f, 62f, 198f, 28f);
            Greybox.Label(packBox, "<b>📦 3 FİDANI KOLİLE [P]</b>\n<size=7>Haznedeki fidanları biyo-koliye dönüştürür</size>", 8, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 198f, 28f);

            float ppx = 8f + 240f + 4f; float ppy = 70f + 24f + 62f;
            float pmx = (ppx + 198f * 0.5f - 312f) / ppm;
            float pmy = -(ppy + 28f * 0.5f - 172f) / ppm;
            var packGo = Greybox.TriggerBox("Btn_Package", r3d, new Vector3(pmx, pmy, -0.024f), new Vector3(198f / ppm, 28f / ppm, 0.006f));
            var packBtn = packGo.AddComponent<PressButton>();
            packBtn.Setup(packGo.transform, new Vector3(0f, 0f, 0.003f), "3 FİDANI BİYO-KOLİYE PAKETLE [P]");
            ui.packageButton = packBtn;

            // 2. İstasyon: Sevkiyat Kamyonu & Lojistik
            var truckDivider = Greybox.Child(midCol, "Truck_Divider", new Color(0.14f, 0.20f, 0.30f));
            Greybox.Place(truckDivider, 4f, 93f, 198f, 1f);

            Greybox.Label(midCol, "<b>🚚 SEVKİYAT KAMYONU (Lojistik)</b>", 8, new Color(0.2f, 0.9f, 0.7f), TextAnchor.MiddleLeft, 6f, 96f, 194f, 13f);

            // Görsel Kamyon Yükleme Alanı
            var truckDock = Greybox.Child(midCol, "Truck_Dock_Frame", new Color(0.08f, 0.12f, 0.18f, 0.95f));
            Greybox.Place(truckDock, 4f, 110f, 198f, 48f);

            ui.truckTargetText = Greybox.Label(truckDock, "Hedef: -", 7, new Color(0.4f, 0.85f, 1f), TextAnchor.MiddleLeft, 4f, 2f, 190f, 11f);
            ui.truckStatusText = Greybox.Label(truckDock, "DURUM: DEPOSUNDA HAZIR", 7, new Color(0.4f, 1f, 0.6f), TextAnchor.MiddleLeft, 4f, 12f, 190f, 11f);
            ui.truckProgressBar = Greybox.Bar(truckDock, 4f, 23f, 190f, 4f, new Color(0.12f, 0.15f, 0.20f), new Color(0.20f, 0.75f, 0.95f));

            // Kamyon Kasa Yuvaları (3 Koli Kapasite)
            ui.truckCargoText = Greybox.Label(truckDock, "Kasa: [3 Koli]", 7, new Color(0.7f, 0.8f, 0.9f), TextAnchor.MiddleLeft, 4f, 30f, 60f, 14f);
            for (int c = 0; c < 3; c++)
            {
                var cSlot = Greybox.Child(truckDock, $"Truck_Slot_{c}", new Color(0.12f, 0.16f, 0.24f));
                Greybox.Place(cSlot, 70f + c * 40f, 29f, 36f, 16f);
                ui.truckCargoSlotTexts[c] = Greybox.Label(cSlot, "—", 7, new Color(0.4f, 0.9f, 1f), TextAnchor.MiddleCenter, 0f, 0f, 36f, 16f, FontStyle.Bold);
            }

            // Buton: Kamyona Yükle
            var loadBox = Greybox.Child(midCol, "Load_Btn_Visual", new Color(0.08f, 0.24f, 0.40f, 0.95f));
            Greybox.Place(loadBox, 4f, 161f, 198f, 22f);
            Greybox.Label(loadBox, "<b>⬆️ KAMYONA YÜKLE [L]</b>", 8, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 198f, 22f);

            float lpx = 8f + 240f + 4f; float lpy = 70f + 24f + 161f;
            float lmx = (lpx + 198f * 0.5f - 312f) / ppm;
            float lmy = -(lpy + 22f * 0.5f - 172f) / ppm;
            var loadGo = Greybox.TriggerBox("Btn_LoadTruck", r3d, new Vector3(lmx, lmy, -0.024f), new Vector3(198f / ppm, 22f / ppm, 0.006f));
            var loadBtn = loadGo.AddComponent<PressButton>();
            loadBtn.Setup(loadGo.transform, new Vector3(0f, 0f, 0.003f), "KOLİYİ KAMYONA YÜKLE [L]");
            ui.loadTruckButton = loadBtn;

            // Büyük Gönder Butonu
            var dispBox = Greybox.Child(midCol, "Dispatch_Btn_Visual", new Color(0.06f, 0.45f, 0.38f, 0.95f));
            Greybox.Place(dispBox, 4f, 186f, 198f, 30f);
            Greybox.Label(dispBox, "<b>🚀 SAHAYA GÖNDER [G / ENTER]</b>", 10, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 198f, 30f);

            float dpx = 8f + 240f + 4f; float dpy = 70f + 24f + 186f;
            float dmx = (dpx + 198f * 0.5f - 312f) / ppm;
            float dmy = -(dpy + 30f * 0.5f - 172f) / ppm;
            var dispGo = Greybox.TriggerBox("Btn_Dispatch", r3d, new Vector3(dmx, dmy, -0.024f), new Vector3(198f / ppm, 30f / ppm, 0.006f));
            var dispBtn = dispGo.AddComponent<PressButton>();
            dispBtn.Setup(dispGo.transform, new Vector3(0f, 0f, 0.003f), "KAMYONU SAHAYA SEVK ET [G / ENTER]");
            ui.dispatchButton = dispBtn;

            // ---------------------------------------------------------------- SAĞ SÜTUN: BİYO-SERA FİDANLIĞI (Width 156)
            var rightCol = Greybox.Child(panelRt, "Right_Col", new Color(0.06f, 0.09f, 0.14f, 0.98f));
            Greybox.Place(rightCol, 448f, 24f, 156f, 220f);

            Greybox.Label(rightCol, "<b>🌱 BİYO-SERA KONSOLU</b>", 9, new Color(0.3f, 0.95f, 0.5f), TextAnchor.MiddleCenter, 4f, 2f, 148f, 14f, FontStyle.Bold);
            ui.nurseryLedDotsText = Greybox.Label(rightCol, "● ● ● ● ● ●", 8, new Color(0.10f, 0.85f, 0.45f), TextAnchor.MiddleCenter, 4f, 15f, 148f, 10f);

            // Fidan Tanıtım Kartı
            var prodBox = Greybox.Child(rightCol, "Product_Card", new Color(0.08f, 0.12f, 0.18f, 0.95f));
            Greybox.Place(prodBox, 4f, 26f, 148f, 40f);
            Greybox.Label(prodBox, "🌲 <b>KARAÇAM BİYO-TÜPÜ</b>", 8, Color.white, TextAnchor.MiddleLeft, 6f, 2f, 136f, 11f, FontStyle.Bold);
            ui.nurseryCostText = Greybox.Label(prodBox, "Tohum & Besin: $1.20 | [TIK]", 7, new Color(0.8f, 0.9f, 0.7f), TextAnchor.MiddleLeft, 6f, 13f, 136f, 11f);
            Greybox.Label(prodBox, "Karbon: 1.4x | Hazine Tasarrufu: Yüksek", 7, new Color(0.35f, 0.85f, 1f), TextAnchor.MiddleLeft, 6f, 24f, 136f, 12f);

            // Görsel Fidan Tepsisi (6 Yuva)
            var trayBox = Greybox.Child(rightCol, "Tray_Frame", new Color(0.08f, 0.11f, 0.16f, 0.95f));
            Greybox.Place(trayBox, 4f, 68f, 148f, 38f);
            ui.nurseryStatusText = Greybox.Label(trayBox, "TEPSİ: 0/6 FİDAN", 8, Color.white, TextAnchor.MiddleLeft, 4f, 1f, 140f, 11f, FontStyle.Bold);

            for (int s = 0; s < 6; s++)
            {
                float sx = 4f + (s % 3) * 46f;
                float sy = 13f + (s / 3) * 11f;
                ui.nurserySlotTexts[s] = Greybox.Label(trayBox, "· BOŞ", 7, new Color(0.5f, 0.6f, 0.7f), TextAnchor.MiddleLeft, sx, sy, 44f, 11f);
            }

            // Dev Fidan Yetiştir Butonu
            var growBox = Greybox.Child(rightCol, "Grow_Btn_Visual", new Color(0.06f, 0.48f, 0.24f, 0.95f));
            Greybox.Place(growBox, 4f, 108f, 148f, 46f);
            Greybox.Label(growBox, "<b>🌱 FİDAN YETİŞTİR</b>\n<size=8>[BOŞLUK / FAREYLE TIKLA]</size>\n<size=7>Maliyet: -$1.20 | +1 Fidan</size>", 10, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 148f, 46f);

            float gpx = 8f + 448f + 4f; float gpy = 70f + 24f + 108f;
            float gmx = (gpx + 148f * 0.5f - 312f) / ppm;
            float gmy = -(gpy + 46f * 0.5f - 172f) / ppm;
            var growGo = Greybox.TriggerBox("Btn_Grow", r3d, new Vector3(gmx, gmy, -0.024f), new Vector3(148f / ppm, 46f / ppm, 0.006f));
            var growBtn = growGo.AddComponent<PressButton>();
            growBtn.Setup(growGo.transform, new Vector3(0f, 0f, 0.003f), "FİDAN YETİŞTİR ($1.20) [BOŞLUK / TIK]");
            ui.growButton = growBtn;

            // Ekolojik Bilgi Kartı
            var infoBox = Greybox.Child(rightCol, "Info_Panel", new Color(0.04f, 0.07f, 0.11f, 0.95f));
            Greybox.Place(infoBox, 4f, 156f, 148f, 60f);

            ui.treeCountText = Greybox.Label(infoBox, "Dikilen Ağaç: 0", 7, Color.white, TextAnchor.MiddleLeft, 6f, 2f, 136f, 11f);
            ui.carbonText = Greybox.Label(infoBox, "Karbon: 0.0 Ton", 7, new Color(0.3f, 0.85f, 1f), TextAnchor.MiddleLeft, 6f, 13f, 136f, 11f);
            Greybox.Label(infoBox, "<i>Devletin afet masrafını azalttıkça hazineden kuruş kuruş fon akar. Ağaçlar büyüdükçe getiri katlanır!</i>", 6, new Color(0.7f, 0.75f, 0.8f), TextAnchor.UpperLeft, 6f, 24f, 136f, 34f);

            // Başlangıçta pasif (World Map aktifken gizli)
            panelRt.gameObject.SetActive(false);
            r3d.gameObject.SetActive(false);
        }

        WorldMapPin CreateMinimalContinentPin(Transform parent, int contIndex, string contName, float localX, float localY, float ppm)
        {
            var pinRoot = Greybox.Empty($"Pin_{contIndex}_{contName}", parent, new Vector3(localX, localY, -0.035f));

            // Sade İstasyon Halkası
            Greybox.Prim(PrimitiveType.Cylinder, "Ring", pinRoot, Vector3.zero, new Vector3(0.032f, 0.003f, 0.032f), new Color(0.12f, 0.25f, 0.38f), unlit: true, collider: false);

            // Tıklanabilir 3D Küre Pin
            var sphereGo = Greybox.Prim(PrimitiveType.Sphere, "Beacon", pinRoot, new Vector3(0f, 0.012f, 0f), new Vector3(0.034f, 0.034f, 0.034f), new Color(0.95f, 0.2f, 0.15f), unlit: true, collider: true);

            // Uyarı Işığı
            var light = Greybox.PointLight("Light", pinRoot, new Vector3(0f, 0.025f, -0.04f), new Color(1f, 0.25f, 0.15f), 1.0f, 0.45f);
            light.enabled = false;

            // Sade Pin Üstü Etiketi (Pill Badge)
            var pinCanvas = Greybox.WorldCanvas("Canvas", pinRoot, new Vector3(0f, 0.042f, 0f), Vector3.zero, new Vector2(0.18f, 0.065f), 500f);
            var pCanvas = pinCanvas.GetComponent<Canvas>();
            if (pCanvas != null)
            {
                pCanvas.overrideSorting = true;
                pCanvas.sortingOrder = 10;
            }
            var pinBadge = Greybox.Child(pinCanvas, "Badge", new Color(0.07f, 0.04f, 0.04f, 0.95f));
            Greybox.Stretch(pinBadge);

            var titleTxt = Greybox.Label(pinBadge, contName, 9, Color.white, TextAnchor.MiddleCenter, 2f, 2f, 86f, 14f, FontStyle.Bold);
            var timerTxt = Greybox.Label(pinBadge, "40.0s", 11, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter, 2f, 15f, 86f, 13f, FontStyle.Bold);
            var bar = Greybox.Bar(pinBadge, 4f, 27f, 82f, 3f, new Color(0.2f, 0.1f, 0.1f), new Color(1f, 0.2f, 0.2f));

            var pin = sphereGo.AddComponent<WorldMapPin>();
            pin.pinRenderer = sphereGo.GetComponent<Renderer>();
            pin.pinLight = light;
            pin.titleText = titleTxt;
            pin.timerText = timerTxt;
            pin.timeBar = bar;
            pin.uiRoot = pinCanvas.gameObject;

            pin.SetupContinent(contIndex, contName);
            return pin;
        }

        WorldMapPin CreateMinimalPin(Transform parent, CrisisEvent ev, float ppm)
        {
            Vector2 geo = GetCrisisGeoCoordinates(ev.id);
            float u = (geo.x + 180f) / 360f;
            float v = (geo.y + 90f) / 180f;

            // Harita piksel konumu (x: 8..616, y: 56..312)
            float pixelX = 8f + u * 608f;
            float pixelY = 56f + (1f - v) * 256f;

            // Monitör merkezine (312px, 172px) göre metre konumu
            float localMetersX = (pixelX - 312f) / ppm;
            float localMetersY = -(pixelY - 172f) / ppm;

            var pinRoot = Greybox.Empty($"Pin_{ev.id}_{ev.locationName}", parent, new Vector3(localMetersX, localMetersY, -0.035f));

            // Sade İstasyon Halkası
            Greybox.Prim(PrimitiveType.Cylinder, "Ring", pinRoot, Vector3.zero, new Vector3(0.030f, 0.003f, 0.030f), new Color(0.12f, 0.25f, 0.38f), unlit: true, collider: false);

            // Tıklanabilir 3D Küre Pin
            var sphereGo = Greybox.Prim(PrimitiveType.Sphere, "Beacon", pinRoot, new Vector3(0f, 0.012f, 0f), new Vector3(0.032f, 0.032f, 0.032f), new Color(0.95f, 0.2f, 0.15f), unlit: true, collider: true);

            // Uyarı Işığı (Varsayılan kapalı)
            var light = Greybox.PointLight("Light", pinRoot, new Vector3(0f, 0.025f, -0.04f), new Color(1f, 0.25f, 0.15f), 1.0f, 0.45f);
            light.enabled = false;

            // Sade Pin Üstü Etiketi (Pill Badge)
            var pinCanvas = Greybox.WorldCanvas("Canvas", pinRoot, new Vector3(0f, 0.040f, 0f), Vector3.zero, new Vector2(0.16f, 0.065f), 500f);
            var pCanvas = pinCanvas.GetComponent<Canvas>();
            if (pCanvas != null)
            {
                pCanvas.overrideSorting = true;
                pCanvas.sortingOrder = 10;
            }
            var pinBadge = Greybox.Child(pinCanvas, "Badge", new Color(0.07f, 0.04f, 0.04f, 0.95f));
            Greybox.Stretch(pinBadge);

            var titleTxt = Greybox.Label(pinBadge, ev.title, 9, Color.white, TextAnchor.MiddleCenter, 2f, 2f, 76f, 14f, FontStyle.Bold);
            var timerTxt = Greybox.Label(pinBadge, "40.0s", 11, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter, 2f, 15f, 76f, 13f, FontStyle.Bold);
            var bar = Greybox.Bar(pinBadge, 4f, 27f, 72f, 3f, new Color(0.2f, 0.1f, 0.1f), new Color(1f, 0.2f, 0.2f));

            var pin = sphereGo.AddComponent<WorldMapPin>();
            pin.pinRenderer = sphereGo.GetComponent<Renderer>();
            pin.pinLight = light;
            pin.titleText = titleTxt;
            pin.timerText = timerTxt;
            pin.timeBar = bar;
            pin.uiRoot = pinCanvas.gameObject;

            pin.Setup(ev);
            return pin;
        }

        static Vector2 GetCrisisGeoCoordinates(int id)
        {
            switch (id)
            {
                case 1: return new Vector2(21.5f, 38.2f);   // Akdeniz / Atina-İzmir
                case 2: return new Vector2(106.8f, -6.2f);  // Güneydoğu Asya / Jakarta
                case 3: return new Vector2(-119.8f, 36.8f); // Kuzey Amerika / Kaliforniya
                case 4: return new Vector2(44.2f, 33.3f);   // Orta Doğu / Bağdat-Basra
                case 5: return new Vector2(76.5f, 28.6f);   // Hint Yarımadası / Yeni Delhi
                case 6: return new Vector2(-41.0f, 72.0f);  // Grönland Buz Tabakası
                case 7: return new Vector2(116.4f, 39.9f);  // Doğu Asya / Pekin
                case 8: return new Vector2(90.4f, 23.8f);   // Bangladeş / Dakka
                case 9: return new Vector2(147.5f, -19.5f); // Okyanusya / Büyük Set Resifi
                case 10: return new Vector2(-75.0f, 16.0f); // Karayipler
                case 11: return new Vector2(-60.0f, -4.0f); // Amazon Havzası / Brezilya
                case 12: return new Vector2(108.0f, 62.0f); // Sibirya / Rusya
                default: return Vector2.zero;
            }
        }

        // ============================================================ C. ALT SADE BİLDİRİM BANDI

        void BuildMinimalFooter(RectTransform screenBg, HudController hud)
        {
            var footerRt = Greybox.Child(screenBg, "Footer_Panel", new Color(0.05f, 0.07f, 0.10f, 0.94f));
            Greybox.Place(footerRt, 8f, 316f, 608f, 20f);

            hud.notificationText = Greybox.Label(footerRt, "DÜNYA İKLİM OPERASYON ODASI HAZIR — KRİZLERİ SEÇİP MÜDAHALE EDİN", 10, new Color(0.75f, 0.85f, 0.95f), TextAnchor.MiddleLeft, 8f, 2f, 592f, 16f);
        }

        // ============================================================ D. 7 KITA KÖR SEÇENEK BRİFİNG PANELİ (BLIND CHOICE DOSSIER)

        void BuildBlindChoiceDossier(Transform monitorRoot, BlindChoiceDialogueUI blindUi, float ppm)
        {
            // Olay Dosyası Paneli (Haritanın tam ortasında beliren diplomatik brifing kartı)
            var dossierRoot = Greybox.Empty("BlindChoice_Dossier", monitorRoot, new Vector3(0f, -0.02f, -0.052f));

            // Kasa (1.36m x 0.72m)
            Greybox.Box("Dossier_Frame", dossierRoot, Vector3.zero, new Vector3(1.36f, 0.72f, 0.02f), new Color(0.05f, 0.07f, 0.10f), unlit: false, collider: false);

            // Canvas (1.32m x 0.68m, 400 PPM -> 528 x 272 piksel)
            var canvasRt = Greybox.WorldCanvas("Dossier_Canvas", dossierRoot, new Vector3(0f, 0f, -0.015f), Vector3.zero, new Vector2(1.32f, 0.68f), ppm);
            var dCanvas = canvasRt.GetComponent<Canvas>();
            if (dCanvas != null)
            {
                dCanvas.overrideSorting = true;
                dCanvas.sortingOrder = 25;
            }

            var mainBox = Greybox.Child(canvasRt, "Main_Panel", new Color(0.06f, 0.08f, 0.12f, 0.98f));
            Greybox.Stretch(mainBox);

            // 1. Başlık ve Temsilci Bilgisi (Üst Panel)
            blindUi.continentTitleText = Greybox.Label(mainBox, "KITA: KUZEY AMERİKA", 12, new Color(0.3f, 0.9f, 1f), TextAnchor.MiddleLeft, 12f, 6f, 260f, 16f, FontStyle.Bold);
            blindUi.representativeText = Greybox.Label(mainBox, "TEMSİLCİ: Vali Jack Harper", 10, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleLeft, 12f, 22f, 260f, 14f);

            blindUi.angerText = Greybox.Label(mainBox, "SİNİR SEVİYESİ: %60", 10, new Color(1f, 0.65f, 0.25f), TextAnchor.MiddleLeft, 280f, 6f, 120f, 14f, FontStyle.Bold);
            blindUi.angerBar = Greybox.Bar(mainBox, 280f, 22f, 95f, 6f, new Color(0.18f, 0.12f, 0.12f), new Color(1f, 0.25f, 0.2f));

            blindUi.timerText = Greybox.Label(mainBox, "40.0s", 12, new Color(1f, 0.35f, 0.35f), TextAnchor.MiddleRight, 380f, 6f, 136f, 16f, FontStyle.Bold);

            // 2. 6 Çevre Sorun Barı (Stats Grid)
            var statsBox = Greybox.Child(mainBox, "Stats_Box", new Color(0.08f, 0.10f, 0.15f, 0.9f));
            Greybox.Place(statsBox, 10f, 38f, 508f, 38f);

            // Sütun 1: Ağaç & Yenilenebilir
            blindUi.treeText = Greybox.Label(statsBox, "Ağaç: %50", 9, new Color(0.4f, 0.9f, 0.4f), TextAnchor.MiddleLeft, 6f, 2f, 150f, 12f);
            blindUi.treeBar = Greybox.Bar(statsBox, 6f, 14f, 150f, 4f, new Color(0.12f, 0.16f, 0.12f), new Color(0.2f, 0.85f, 0.3f));

            blindUi.renewableText = Greybox.Label(statsBox, "Yenilenebilir: %40", 9, new Color(0.3f, 0.85f, 1f), TextAnchor.MiddleLeft, 6f, 19f, 150f, 12f);
            blindUi.renewableBar = Greybox.Bar(statsBox, 6f, 31f, 150f, 4f, new Color(0.1f, 0.14f, 0.18f), new Color(0.2f, 0.8f, 1f));

            // Sütun 2: Fabrika & Gıda
            blindUi.factoryText = Greybox.Label(statsBox, "Fabrika: %85", 9, new Color(1f, 0.65f, 0.3f), TextAnchor.MiddleLeft, 175f, 2f, 150f, 12f);
            blindUi.factoryBar = Greybox.Bar(statsBox, 175f, 14f, 150f, 4f, new Color(0.18f, 0.14f, 0.1f), new Color(1f, 0.55f, 0.2f));

            blindUi.foodText = Greybox.Label(statsBox, "Gıda: %65", 9, new Color(0.9f, 0.85f, 0.4f), TextAnchor.MiddleLeft, 175f, 19f, 150f, 12f);
            blindUi.foodBar = Greybox.Bar(statsBox, 175f, 31f, 150f, 4f, new Color(0.18f, 0.16f, 0.1f), new Color(0.9f, 0.8f, 0.3f));

            // Sütun 3: Kirlilik & Sıfır Atık
            blindUi.pollutionText = Greybox.Label(statsBox, "Kirlilik: %70", 9, new Color(0.95f, 0.4f, 0.4f), TextAnchor.MiddleLeft, 345f, 2f, 150f, 12f);
            blindUi.pollutionBar = Greybox.Bar(statsBox, 345f, 14f, 150f, 4f, new Color(0.2f, 0.12f, 0.12f), new Color(0.95f, 0.3f, 0.3f));

            blindUi.zeroWasteText = Greybox.Label(statsBox, "Sıfır Atık: %25", 9, new Color(0.7f, 0.6f, 0.95f), TextAnchor.MiddleLeft, 345f, 19f, 150f, 12f);
            blindUi.zeroWasteBar = Greybox.Bar(statsBox, 345f, 31f, 150f, 4f, new Color(0.15f, 0.12f, 0.18f), new Color(0.7f, 0.5f, 0.95f));

            // 3. Kriz Başlığı ve Soru Brifingi
            var descBox = Greybox.Child(mainBox, "Desc_Box", new Color(0.07f, 0.09f, 0.13f));
            Greybox.Place(descBox, 10f, 79f, 508f, 44f);

            blindUi.crisisTitleText = Greybox.Label(descBox, "SANAYİ BÖLGESİ BACA FİLTRESİ VE EMİSYON KRİZİ", 11, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleLeft, 8f, 2f, 492f, 15f, FontStyle.Bold);
            blindUi.descriptionText = Greybox.Label(descBox, "Kriz brifingi metni.", 9, new Color(0.88f, 0.90f, 0.92f), TextAnchor.UpperLeft, 8f, 17f, 492f, 25f);

            // 4. 3 Kör Seçenek (KESİN KURAL: ETKİ GÖSTERİMİ YASAK!)
            blindUi.choiceButtons = new PressButton[3];
            blindUi.choiceTexts = new Text[3];
            blindUi.choiceBgImages = new Image[3];

            var btnContainer = Greybox.Empty("Choice_Buttons", dossierRoot, Vector3.zero);

            for (int i = 0; i < 3; i++)
            {
                float panelY = 126f + i * 36f;

                // Tam genişlikte şık diplomatik seçenek kartı (Genişlik: 508px)
                var cPanel = Greybox.Child(mainBox, $"Choice_{i}", new Color(0.08f, 0.12f, 0.18f, 0.95f));
                Greybox.Place(cPanel, 10f, panelY, 508f, 32f);
                blindUi.choiceBgImages[i] = cPanel.GetComponent<Image>();

                // Sol numara hapı
                Color badgeBg = i == 0 ? new Color(0.06f, 0.35f, 0.22f) : (i == 1 ? new Color(0.08f, 0.28f, 0.40f) : new Color(0.35f, 0.25f, 0.12f));
                var badge = Greybox.Child(cPanel, "Badge", badgeBg);
                Greybox.Place(badge, 4f, 4f, 28f, 24f);
                Greybox.Label(badge, $"<b>{i + 1}</b>", 10, Color.white, TextAnchor.MiddleCenter, 0f, 0f, 28f, 24f);

                // YALNIZCA diplomatik karar metni yazılır! (İpucu, sayı, ikon ASLA bulunmaz!)
                blindUi.choiceTexts[i] = Greybox.Label(cPanel, $"Diplomatik karar metni", 8, Color.white, TextAnchor.MiddleLeft, 38f, 2f, 464f, 28f);

                // Tıklanabilir 3D Görünmez Buton (Tüm satırı kapsayan TriggerBox: SIFIR SİYAH KUTU!)
                float cpy = panelY + 16f;
                float cmy = -(cpy - 136f) / ppm;

                var btnGo = Greybox.TriggerBox($"Btn_Choice_{i}", btnContainer, new Vector3(0f, cmy, -0.02f), new Vector3(508f / ppm, 32f / ppm, 0.008f));
                var btn = btnGo.AddComponent<PressButton>();
                btn.Setup(btnGo.transform, new Vector3(0f, 0f, 0.003f), $"MÜDAHALE SEÇ #{i + 1}");
                blindUi.choiceButtons[i] = btn;
            }

            // 5. Kapat Butonu (Tam merkezde şık buton)
            var closePanel = Greybox.Child(mainBox, "Close_Panel_Visual", new Color(0.14f, 0.18f, 0.24f, 0.95f));
            Greybox.Place(closePanel, 154f, 238f, 220f, 26f);
            Greybox.Label(closePanel, "<b>[ ✕ KAPAT / DOKÜMANI ARŞİVLE ]</b>", 9, new Color(0.85f, 0.90f, 0.95f), TextAnchor.MiddleCenter, 0f, 0f, 220f, 26f);

            float clpy = 238f + 13f;
            float clmy = -(clpy - 136f) / ppm;
            var closeGo = Greybox.TriggerBox("Btn_Close", btnContainer, new Vector3(0f, clmy, -0.02f), new Vector3(220f / ppm, 26f / ppm, 0.008f));
            var closeBtn = closeGo.AddComponent<PressButton>();
            closeBtn.Setup(closeGo.transform, new Vector3(0f, 0f, 0.003f), "DOSYAYI KAPAT / HARİTAYA DÖN");
            blindUi.closeButton = closeBtn;

            blindUi.rootObject = dossierRoot.gameObject;
            dossierRoot.gameObject.SetActive(false);
        }

        // ============================================================ E. SADE OLAY KARTI (EVENT DOSSIER)

        void BuildMinimalEventCard(Transform monitorRoot, EventCardUI cardUi, float ppm)
        {
            // Olay Dosyası Paneli (Haritanın tam ortasında beliren sade taktiksel brifing kartı)
            var dossierRoot = Greybox.Empty("EventCard_Dossier", monitorRoot, new Vector3(0f, -0.04f, -0.05f));

            // Kasa
            Greybox.Box("Dossier_Frame", dossierRoot, Vector3.zero, new Vector3(1.22f, 0.58f, 0.02f), new Color(0.07f, 0.09f, 0.12f), unlit: false, collider: false);

            // Canvas (1.20m x 0.56m, 400 PPM -> 480 x 224 piksel)
            var canvasRt = Greybox.WorldCanvas("Dossier_Canvas", dossierRoot, new Vector3(0f, 0f, -0.015f), Vector3.zero, new Vector2(1.20f, 0.56f), ppm);
            var dCanvas = canvasRt.GetComponent<Canvas>();
            if (dCanvas != null)
            {
                dCanvas.overrideSorting = true;
                dCanvas.sortingOrder = 20;
            }

            var mainBox = Greybox.Child(canvasRt, "Main", new Color(0.06f, 0.08f, 0.12f, 0.98f));
            Greybox.Stretch(mainBox);

            // Başlık, Bölge ve Kalan Süre
            cardUi.titleText = Greybox.Label(mainBox, "KRİZ BAŞLIĞI", 15, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleLeft, 12f, 8f, 320f, 20f, FontStyle.Bold);
            cardUi.locationText = Greybox.Label(mainBox, "Bölge: -", 11, new Color(0.4f, 0.9f, 1f), TextAnchor.MiddleLeft, 12f, 26f, 320f, 15f);
            cardUi.timerText = Greybox.Label(mainBox, "40.0s", 13, new Color(1f, 0.35f, 0.35f), TextAnchor.MiddleRight, 330f, 8f, 138f, 20f, FontStyle.Bold);

            // Olay Açıklaması
            var descBox = Greybox.Child(mainBox, "Desc", new Color(0.08f, 0.11f, 0.15f));
            Greybox.Place(descBox, 12f, 44f, 456f, 30f);
            cardUi.descriptionText = Greybox.Label(descBox, "Olay açıklaması.", 10, new Color(0.9f, 0.92f, 0.88f), TextAnchor.MiddleLeft, 8f, 2f, 440f, 26f);

            // 3 Seçenek
            cardUi.choiceButtons = new PressButton[3];
            cardUi.choiceTitles = new Text[3];
            cardUi.choiceStats = new Text[3];
            cardUi.choiceBgImages = new Image[3];

            var btnContainer = Greybox.Empty("Choice_Buttons", dossierRoot, Vector3.zero);

            for (int i = 0; i < 3; i++)
            {
                float panelY = 78f + i * 35f;

                var cPanel = Greybox.Child(mainBox, $"Choice_{i}", new Color(0.10f, 0.14f, 0.19f));
                Greybox.Place(cPanel, 12f, panelY, 456f, 32f);
                cardUi.choiceBgImages[i] = cPanel.GetComponent<Image>();

                cardUi.choiceTitles[i] = Greybox.Label(cPanel, $"[{i + 1}] Seçenek", 11, Color.white, TextAnchor.MiddleLeft, 10f, 2f, 340f, 15f, FontStyle.Bold);
                cardUi.choiceStats[i] = Greybox.Label(cPanel, "Para: 0$ | Halk: 0 | İklim: 0", 10, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleLeft, 10f, 16f, 340f, 13f);

                // Tıklanabilir 3D Buton
                float btnLocalMetersX = 0.46f;
                float btnLocalMetersY = -(panelY + 16f - 112f) / ppm;

                var btnGo = Greybox.Box($"Btn_{i}", btnContainer, new Vector3(btnLocalMetersX, btnLocalMetersY, -0.02f), new Vector3(0.20f, 0.065f, 0.02f), i == 2 ? new Color(0.40f, 0.32f, 0.22f) : new Color(0.16f, 0.60f, 0.30f));
                var btn = btnGo.AddComponent<PressButton>();
                btn.Setup(btnGo.transform, new Vector3(0f, 0f, 0.01f), $"MÜDAHALE SEÇ #{i + 1}");
                cardUi.choiceButtons[i] = btn;

                var bCanvas = Greybox.WorldCanvas($"Btn_Canvas_{i}", btnGo.transform, new Vector3(0f, 0f, -0.012f), Vector3.zero, new Vector2(0.18f, 0.055f), 500f);
                Greybox.Label(bCanvas, $"<b>MÜDAHALE {i + 1}</b>", 12, Color.white, TextAnchor.MiddleCenter, 2f, 2f, 86f, 24f);
            }

            // Kapat Butonu
            var closeGo = Greybox.Box("Btn_Close", btnContainer, new Vector3(0f, -0.23f, -0.02f), new Vector3(0.32f, 0.048f, 0.02f), new Color(0.30f, 0.34f, 0.40f));
            var closeBtn = closeGo.AddComponent<PressButton>();
            closeBtn.Setup(closeGo.transform, new Vector3(0f, 0f, 0.01f), "DOSYAYI KAPAT / HARİTAYA DÖN");
            cardUi.closeButton = closeBtn;

            var closeCanvas = Greybox.WorldCanvas("Close_Canvas", closeGo.transform, new Vector3(0f, 0f, -0.012f), Vector3.zero, new Vector2(0.30f, 0.042f), 500f);
            Greybox.Label(closeCanvas, "<b>[ X ] KAPAT</b>", 12, Color.white, TextAnchor.MiddleCenter, 2f, 2f, 146f, 18f);

            cardUi.cardRoot = dossierRoot.gameObject;
            dossierRoot.gameObject.SetActive(false);
        }

        // ============================================================ 4. NIŞANGAH & OYUN SONU KANVASI

        void BuildCrosshairCanvas(Transform camTransform, HudController hud)
        {
            var go = new GameObject("Player_Screen_Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            // 1. Nişangah Noktası
            var crosshair = Greybox.Child(go.transform, "Crosshair", new Color(1f, 1f, 1f, 0.40f));
            Greybox.PlaceCenter(crosshair, 0f, 6f, 6f);
            hud.crosshairImage = crosshair.GetComponent<Image>();

            // 2. Hover Tooltip
            var tooltipGo = new GameObject("HoverTooltip", typeof(RectTransform));
            tooltipGo.transform.SetParent(go.transform, false);
            var tRt = (RectTransform)tooltipGo.transform;
            Greybox.PlaceCenter(tRt, -40f, 800f, 36f);
            var tText = tooltipGo.AddComponent<Text>();
            tText.font = Greybox.Font;
            tText.fontSize = 18;
            tText.color = Color.yellow;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.supportRichText = true;
            hud.hoverLabelText = tText;

            // 3. Oyun Sonu Ekranı (Game Over / Win Overlay)
            var endRoot = new GameObject("GameOver_Screen", typeof(RectTransform));
            endRoot.transform.SetParent(go.transform, false);
            var endRt = (RectTransform)endRoot.transform;
            Greybox.Stretch(endRt);
            var bgImg = endRoot.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.90f);

            var titleGo = new GameObject("EndTitle", typeof(RectTransform));
            titleGo.transform.SetParent(endRoot.transform, false);
            Greybox.PlaceCenter((RectTransform)titleGo.transform, 130f, 1100f, 110f);
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = Greybox.Font;
            titleTxt.fontSize = 38;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.supportRichText = true;
            hud.endTitleText = titleTxt;

            var reasonGo = new GameObject("EndReason", typeof(RectTransform));
            reasonGo.transform.SetParent(endRoot.transform, false);
            Greybox.PlaceCenter((RectTransform)reasonGo.transform, -30f, 950f, 180f);
            var reasonTxt = reasonGo.AddComponent<Text>();
            reasonTxt.font = Greybox.Font;
            reasonTxt.fontSize = 20;
            reasonTxt.alignment = TextAnchor.MiddleCenter;
            reasonTxt.color = Color.white;
            reasonTxt.supportRichText = true;
            hud.endReasonText = reasonTxt;

            endRoot.SetActive(false);
            hud.endScreenRoot = endRoot;
        }
    }
}
