using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Pixamor.ClimateOS
{
    /// <summary>
    /// V3: Aqua-Modern (60-30-10 Su Yeşili / Seafoam) İklim Bakanlığı OS Sahne ve Arayüz Mimarı (v9.0):
    /// - Sahnedeki hiçbir UI elemanı sprite = null değildir; Assets/UI/EcoModern_Sprites altındaki 25+ RGBA PNG Sprite kullanılır.
    /// - Harita üzerinde world_map_aqua_grid.png (Holografik Katman) ve map_pin_crisis.png (Modern Kriz İşaretçileri) yer alır.
    /// - EnsureUIBindings() ile sıfır NullReferenceException garantisi sağlar.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [ExecuteAlways]
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class SceneAutoBuilder : MonoBehaviour
    {
        public const int TargetSceneVersion = 11;

        [Header("Kalıcı Sahne Sürümü")]
        public int builtSceneVersion = 0;
        public bool forceRebuildOnAwake = false;

#if UNITY_EDITOR
        static SceneAutoBuilder()
        {
            EditorApplication.delayCall += AutoBuildInEditModeIfNeeded;
        }

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += AutoBuildInEditModeIfNeeded;
            }
        }

        static void AutoBuildInEditModeIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded) return;

            var builder = FindAnyObjectByType<SceneAutoBuilder>();
            var workstation = GameObject.Find("Computer_Workstation");
            var player = GameObject.Find("FirstPersonPlayer");
            var gameMgr = GameObject.Find("GameManager");
            var uiMgr = GameObject.Find("OS_UIManager");

            bool needsBuild = builder == null
                              || builder.builtSceneVersion < TargetSceneVersion
                              || workstation == null
                              || player == null
                              || gameMgr == null
                              || uiMgr == null;

            if (!needsBuild) return;

            if (builder == null)
            {
                var go = new GameObject("SceneAutoBuilder");
                builder = go.AddComponent<SceneAutoBuilder>();
            }

            builder.BuildScene();
            builder.builtSceneVersion = TargetSceneVersion;

            EditorUtility.SetDirty(builder);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=#2DD4BF>[AQUA-MODERN OS V3 v9.0] 60-30-10 Su Yeşili 25+ PNG Sprite mimarisi kuruldu ve sahne kaydedildi!</color>");
        }

        [MenuItem("ShadowOS/Sahneyi Yeniden Kur ve Kaydet (Build Permanent Scene)", false, 10)]
        public static void BuildSceneMenuItem()
        {
            var builder = FindAnyObjectByType<SceneAutoBuilder>();
            if (builder == null)
            {
                var go = new GameObject("SceneAutoBuilder");
                builder = go.AddComponent<SceneAutoBuilder>();
            }

            builder.BuildScene();
            builder.builtSceneVersion = TargetSceneVersion;

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
            Debug.Log("<color=#2DD4BF>[AQUA-MODERN OS V3 v9.0] Kalıcı Hierarchy nesneleri oluşturuldu ve sahne kaydedildi!</color>");
        }
#endif

        void Awake()
        {
            if (!forceRebuildOnAwake &&
                builtSceneVersion >= TargetSceneVersion &&
                GameObject.Find("Computer_Workstation") != null &&
                GameObject.Find("FirstPersonPlayer") != null &&
                GameObject.Find("GameManager") != null &&
                GameObject.Find("OS_UIManager") != null)
            {
                return;
            }

            BuildScene();
            builtSceneVersion = TargetSceneVersion;
        }

        [ContextMenu("Sahneyi Sıfırdan İnşa Et (Kalıcı Hierarchy)")]
        public void BuildScene()
        {
#if UNITY_EDITOR
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
#endif
            CleanupLegacyObjects();

            // 1. KALICI GAME MANAGER KÖKÜ
            var gameMgrGo = new GameObject("GameManager");
            var mgr = gameMgrGo.AddComponent<ClimateOperatorManager>();
            var crisisSpawner = gameMgrGo.AddComponent<ContinentCrisisSpawner>();
            gameMgrGo.AddComponent<Sfx>();

            // 2. KALICI OS_UIMANAGER KÖKÜ VE EVENTSYSTEM
            var uiMgrGo = new GameObject("OS_UIManager");
            var mapController = uiMgrGo.AddComponent<WorldMapMonitorController>();
            var hud = uiMgrGo.AddComponent<HudController>();
            var blindUi = uiMgrGo.AddComponent<BlindChoiceDialogueUI>();
            var miniGameUi = uiMgrGo.AddComponent<SanctionMiniGameUI>();

            EnsureEventSystem(uiMgrGo.transform);

            // 3. 3D ODA MODELİ (Iklim_Yonetim_Odasi) & MESH COLLIDER'LAR
            Transform roomTransform = SetupRoomAndColliders();

            // 4. ODA IŞIKLANDIRMASI (Room_Lighting - Doğal Nötr Gün Işığı, Renkli 3D Işık Yok)
            BuildRoomLighting(roomTransform);

            // 5. ODA GEOMETRİSİNDEN MASA VE OYUNCU KONUMLARINI HESAPLA
            ComputeRoomPlacement(roomTransform,
                out Vector3 workstationPos,
                out Quaternion workstationRot,
                out Vector3 playerStartPos,
                out Quaternion playerStartRot);

            // 6. TAŞINABİLİR BİLGİSAYAR İSTASYONU (Computer_Workstation)
            MovableComputerStation workstation = BuildMovableComputerStation(
                workstationPos,
                workstationRot,
                mgr,
                hud,
                mapController,
                crisisSpawner,
                blindUi,
                miniGameUi);

            // 7. SERBEST DOLAŞIM BİRİNCİ ŞAHIS KARAKTERİ (FirstPersonPlayer)
            PlayerInteractor interactor = BuildFirstPersonPlayer(playerStartPos, playerStartRot, workstation);
            hud.interactor = interactor;

            // 8. 'E' TUŞU ETKİLEŞİM İKONU, SİYAH EKRAN ("GÜN 2") GEÇİŞİ VE DÖNEM SONU RAPORU
            BuildCrosshairAndEndReportCanvas(uiMgrGo.transform, hud);

            builtSceneVersion = TargetSceneVersion;
        }

        void EnsureEventSystem(Transform parent)
        {
            var existingEs = FindAnyObjectByType<EventSystem>();
            if (existingEs != null) return;

            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(parent, false);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        void CleanupLegacyObjects()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()) return;

            var existingCam = Camera.main;
            if (existingCam != null)
            {
                existingCam.transform.SetParent(null, true);
                var oldInteractors = existingCam.GetComponents<PlayerInteractor>();
                for (int i = 0; i < oldInteractors.Length; i++)
                {
                    if (oldInteractors[i] != null) Greybox.Kill(oldInteractors[i]);
                }
            }

            var roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var ro = roots[i];
                if (ro == null || ro == gameObject) continue;

                if (ro.name == "Cube" || ro.name == "Sphere" || ro.name == "Capsule")
                {
                    Greybox.Kill(ro);
                }
                else if (ro.name.StartsWith("===") ||
                         ro.name == "_OLD_ENV_ROOT_" ||
                         ro.name == "Player_Screen_Canvas" ||
                         ro.name == "Player_HUD_Canvas" ||
                         ro.name == "GameManager" ||
                         ro.name == "OS_UIManager" ||
                         ro.name == "Computer_Workstation" ||
                         ro.name == "FirstPersonPlayer" ||
                         ro.name == "Room_Lighting" ||
                         ro.name == "Directional Light" ||
                         ro.name == "SceneAutoBuilder_Runtime")
                {
                    Greybox.Kill(ro);
                }
            }

            // Sahnedeki tüm eski renkli/başıboş 3D Light bileşenlerini temizle
            var allLights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            for (int i = 0; i < allLights.Length; i++)
            {
                if (allLights[i] != null) Greybox.Kill(allLights[i].gameObject);
            }
        }

        // ============================================================ 1. 3D ODA VE MESH COLLIDER KURULUMU

        Transform SetupRoomAndColliders()
        {
            GameObject roomInstance = FindExisting3DRoomInScene();

            if (roomInstance == null)
            {
                GameObject roomPrefab = null;
#if UNITY_EDITOR
                roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assetler/iklim_yönetim_odası (3).fbx");
#endif
                if (roomPrefab == null)
                {
                    roomPrefab = Resources.Load<GameObject>("iklim_yonetim_odasi");
                }

                if (roomPrefab != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        roomInstance = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
                    else
                        roomInstance = Instantiate(roomPrefab);
#else
                    roomInstance = Instantiate(roomPrefab);
#endif
                }
            }

            if (roomInstance != null)
            {
                roomInstance.name = "Iklim_Yonetim_Odasi";
                roomInstance.transform.SetParent(null, true);
                roomInstance.transform.position = Vector3.zero;
                roomInstance.transform.rotation = Quaternion.identity;
                roomInstance.transform.localScale = Vector3.one;
                roomInstance.SetActive(true);

                var oldLight = roomInstance.transform.Find("Room_Lighting");
                if (oldLight != null) Greybox.Kill(oldLight.gameObject);

                // Oda içindeki olası tüm eski/renkli 3D Light bileşenlerini temizle
                var embeddedLights = roomInstance.GetComponentsInChildren<Light>(true);
                for (int i = 0; i < embeddedLights.Length; i++)
                {
                    if (embeddedLights[i] != null) Greybox.Kill(embeddedLights[i]);
                }

                var filters = roomInstance.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < filters.Length; i++)
                {
                    var mf = filters[i];
                    if (mf == null || mf.sharedMesh == null) continue;

                    var oldBox = mf.gameObject.GetComponent<BoxCollider>();
                    if (oldBox != null) Greybox.Kill(oldBox);

                    var meshCol = mf.gameObject.GetComponent<MeshCollider>();
                    if (meshCol == null)
                    {
                        meshCol = mf.gameObject.AddComponent<MeshCollider>();
                    }
                    meshCol.sharedMesh = mf.sharedMesh;
                    meshCol.convex = false;
                }

                return roomInstance.transform;
            }

            var fallback = new GameObject("Iklim_Yonetim_Odasi").transform;
            Greybox.Box("Plane", fallback, new Vector3(0f, -0.05f, 0f), new Vector3(5f, 0.1f, 4f), new Color(0.12f, 0.16f, 0.23f));
            return fallback;
        }

        static GameObject FindExisting3DRoomInScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return null;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var r = roots[i];
                if (r == null) continue;
                if (r.name.StartsWith("===") || r.name == "_OLD_ENV_ROOT_") continue;

                if (r.transform.Find("Plane.015") != null) return r;
                string lower = r.name.ToLowerInvariant();
                if (lower.Contains("iklim_y") || lower.Contains("iklim_yonetim"))
                    return r;
            }
            return null;
        }

        void BuildRoomLighting(Transform roomTransform)
        {
            // KURAL 2: Odayı su yeşiline veya pembeye boyayan tüm renkli 3D ışıklar kaldırıldı.
            // Doğal, nötr ve temiz bir iç mekan ışığı (#FFFFFF / sıcak gün ışığı #FFF8E7) ayarlandı; beyaz kitaplık gerçekten beyaz görünür.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.95f, 0.94f, 0.92f); // Doğal temiz iç mekan ortam ışığı

            var lightsRoot = new GameObject("Room_Lighting").transform;
            lightsRoot.position = Vector3.zero;
            lightsRoot.rotation = Quaternion.identity;

            var dirGo = new GameObject("Directional_Daylight");
            dirGo.transform.SetParent(lightsRoot, false);
            dirGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var dirLight = dirGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.98f, 0.94f); // #FFF8E7 Doğal Sıcak Gün Işığı
            dirLight.intensity = 1.0f;
            dirLight.shadows = LightShadows.None;

            Vector3 roomCenter = new Vector3(-0.5f, 2.20f, 0.4f);
            if (roomTransform != null)
            {
                var floorR = roomTransform.Find("Plane")?.GetComponent<Renderer>();
                if (floorR != null)
                {
                    roomCenter = new Vector3(floorR.bounds.center.x, floorR.bounds.max.y + 2.25f, floorR.bounds.center.z);
                }
            }

            Greybox.PointLight("Ceiling_Neutral_White", lightsRoot, roomCenter, Color.white, 0.95f, 11f);
        }

        // ============================================================ 2. MESH BOUNDS İLE MASA VE OYUNCU KONUMLANDIRMA

        void ComputeRoomPlacement(
            Transform roomTransform,
            out Vector3 workstationPos,
            out Quaternion workstationRot,
            out Vector3 playerStartPos,
            out Quaternion playerStartRot)
        {
            workstationPos = new Vector3(-0.15f, 0.80f, -0.58f);
            workstationRot = Quaternion.LookRotation(Vector3.back, Vector3.up);
            playerStartPos = new Vector3(-0.15f, 0.02f, 0.85f);
            playerStartRot = Quaternion.LookRotation(Vector3.back, Vector3.up);

            if (roomTransform == null) return;

            var floorR = roomTransform.Find("Plane")?.GetComponent<Renderer>();
            var deskR = roomTransform.Find("Plane.015")?.GetComponent<Renderer>();
            var shelfR = roomTransform.Find("Cube.003")?.GetComponent<Renderer>();
            var bedR = roomTransform.Find("Plane.013")?.GetComponent<Renderer>();

            if (deskR == null) return;

            Bounds deskB = deskR.bounds;
            float floorY = floorR != null ? floorR.bounds.max.y : 0f;
            float deskTopY = deskB.max.y;

            float intoRoomSignZ = 1f;
            if (shelfR != null && bedR != null)
            {
                intoRoomSignZ = (bedR.bounds.center.z >= shelfR.bounds.center.z) ? 1f : -1f;
            }

            Vector3 towardDeskWallDir = new Vector3(0f, 0f, -intoRoomSignZ);
            float deskBackWallZ = (intoRoomSignZ > 0f) ? deskB.min.z : deskB.max.z;

            float stationX = -0.15f;
            float stationZ = deskBackWallZ + intoRoomSignZ * 0.42f;
            workstationPos = new Vector3(stationX, deskTopY, stationZ);
            workstationRot = Quaternion.LookRotation(towardDeskWallDir, Vector3.up);

            float playerZ = deskBackWallZ + intoRoomSignZ * 1.82f;
            playerStartPos = new Vector3(stationX, floorY + 0.02f, playerZ);
            playerStartRot = Quaternion.LookRotation(towardDeskWallDir, Vector3.up);
        }

        // ============================================================ 3. TAŞINABİLİR BİLGİSAYAR İSTASYONU (Computer_Workstation)

        MovableComputerStation BuildMovableComputerStation(
            Vector3 worldPos,
            Quaternion worldRot,
            ClimateOperatorManager mgr,
            HudController hud,
            WorldMapMonitorController mapController,
            ContinentCrisisSpawner crisisSpawner,
            BlindChoiceDialogueUI blindUi,
            SanctionMiniGameUI miniGameUi)
        {
            var stationGo = new GameObject("Computer_Workstation");
            stationGo.transform.position = worldPos;
            stationGo.transform.rotation = worldRot;

            var stationTrigger = stationGo.AddComponent<BoxCollider>();
            stationTrigger.isTrigger = true;
            stationTrigger.center = new Vector3(0f, 0.30f, -0.04f);
            stationTrigger.size = new Vector3(1.02f, 0.64f, 0.36f);

            var station = stationGo.AddComponent<MovableComputerStation>();
            station.interactionTrigger = stationTrigger;

            // 1. 3D Monitör Modeli (Koyu Nötr Antrasit Kasa #0F172A & #1E293B)
            var modelRoot = Greybox.Empty("Monitor_3D_Model", stationGo.transform, Vector3.zero);
            station.monitor3DModel = modelRoot;

            Greybox.Box("Monitor_Stand_Base", modelRoot, new Vector3(0f, 0.008f, 0.02f), new Vector3(0.28f, 0.014f, 0.18f), new Color(0.06f, 0.09f, 0.16f), unlit: false, collider: false);
            Greybox.Box("Monitor_Stand_Neck", modelRoot, new Vector3(0f, 0.15f, 0.05f), new Vector3(0.045f, 0.28f, 0.03f), new Color(0.12f, 0.16f, 0.23f), unlit: false, collider: false);

            var monitorHead = Greybox.Empty("Monitor_Head", modelRoot, new Vector3(0f, 0.30f, 0f), new Vector3(4f, 0f, 0f));
            station.monitorHead = monitorHead;

            Greybox.Box("Monitor_Bezel_Outer", monitorHead, new Vector3(0f, 0f, 0.004f), new Vector3(0.96f, 0.54f, 0.022f), new Color(0.12f, 0.16f, 0.23f), unlit: false, collider: false);
            Greybox.Box("Monitor_Frame", monitorHead, Vector3.zero, new Vector3(0.945f, 0.525f, 0.024f), new Color(0.06f, 0.09f, 0.16f), unlit: false, collider: true);

            // 2. CameraFocusPoint
            var focusPoint = Greybox.Empty("CameraFocusPoint", monitorHead, new Vector3(0f, 0f, -0.62f), Vector3.zero);
            station.cameraFocusPoint = focusPoint;

            // 3. World-Space Monitor_OS_Canvas (624x344 px, Scale: 0.0015 -> 0.936m x 0.516m)
            float ppm = 666.6667f;
            Vector2 screenSizeMeters = new Vector2(624f / ppm, 344f / ppm);

            var canvasRt = Greybox.WorldCanvas("Monitor_OS_Canvas", monitorHead, new Vector3(0f, 0f, -0.015f), Vector3.zero, screenSizeMeters, ppm);
            var mCanvas = canvasRt.GetComponent<Canvas>();
            if (mCanvas != null)
            {
                mCanvas.overrideSorting = true;
                mCanvas.sortingOrder = 1;
            }
            station.monitorOSCanvas = mCanvas;

            // Koyu Nötr Antrasit Zemin (#0F172A + 1px #10B981 kenarlık): panel_main_aqua.png
            var screenBg = Greybox.SpriteBox(canvasRt, "OS_Desktop_BG", "panel_main_aqua", 0f, 0f, 624f, 344f, raycast: false);
            Greybox.Stretch(screenBg);

            // A. ÜST DURUM ÇUBUĞU
            BuildMinisterTopBar(screenBg, monitorHead, hud, ppm);

            // B. DÜNYA HARİTASI (Renk Değerleriyle Kriz Gösterimi, 3D Işık Yok)
            BuildWorldMapWorkspace(screenBg, monitorHead, mapController, crisisSpawner, blindUi, ppm, screenSizeMeters);

            // C. KRİZ & TELEMETRİ DOSYASI + YAPTIRIM MİNİ-OYUN MODALI
            BuildMinisterDossier(screenBg, monitorHead, blindUi, miniGameUi, ppm);

            // D. MONİTÖR ÜZERİ SİYAH EKRAN ("GÜN 2") GEÇİŞ KATMANI (#000000)
            var monBlackGo = new GameObject("Monitor_DayTransition_BlackScreen", typeof(RectTransform));
            monBlackGo.transform.SetParent(canvasRt, false);
            var monBlackRt = (RectTransform)monBlackGo.transform;
            Greybox.Stretch(monBlackRt);
            var monBlackImg = monBlackGo.AddComponent<Image>();
            monBlackImg.color = Color.black; // #000000 Saf Siyah
            monBlackImg.raycastTarget = true;
            var monBlackCg = monBlackGo.AddComponent<CanvasGroup>();
            monBlackCg.alpha = 0f;
            monBlackCg.blocksRaycasts = false;
            monBlackCg.interactable = false;

            var monDayTitle = Greybox.Label(monBlackRt, "GÜN 2", 34, new Color(0.98f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 0f, 0f, 624f, 344f, FontStyle.Bold);
            monDayTitle.gameObject.name = "Monitor_DayTransition_Title";
            Greybox.Stretch((RectTransform)monDayTitle.transform);

            hud.monitorDayTransitionGroup = monBlackCg;
            hud.monitorDayTransitionTitleText = monDayTitle;
            monBlackGo.SetActive(false);

            return station;
        }

        PressButton CreateCanvasTrigger(string name, Transform parent, float canvasX, float canvasY, float canvasW, float canvasH, float ppm, string hoverText = "", float zOffset = -0.019f)
        {
            float centerX = canvasX + canvasW * 0.5f;
            float centerY = canvasY + canvasH * 0.5f;
            float localMetersX = (centerX - 312f) / ppm;
            float localMetersY = -(centerY - 172f) / ppm;

            var go = Greybox.TriggerBox(name, parent, new Vector3(localMetersX, localMetersY, zOffset), new Vector3(canvasW / ppm, canvasH / ppm, 0.006f));
            var btn = go.AddComponent<PressButton>();
            btn.Setup(go.transform, new Vector3(0f, 0f, 0.002f), hoverText);
            return btn;
        }

        // ============================================================ A. ÜST DURUM ÇUBUĞU

        void BuildMinisterTopBar(RectTransform screenBg, Transform monitorHead, HudController hud, float ppm)
        {
            var headerRt = Greybox.SpriteBox(screenBg, "TopBar_Panel", "panel_main_aqua", 8f, 6f, 608f, 38f);

            // 1. Gösterge: Küresel İklim (card_stat_climate.png + icon_climate_aqua.png + bar_bg_pill.png + bar_fill_climate.png)
            var cap1 = Greybox.SpriteBox(headerRt, "Stat_GlobalClimate", "card_stat_climate", 5f, 4f, 144f, 30f);
            Greybox.Icon(cap1, "icon_climate_aqua", 6f, 5f, 20f, 20f, Color.white);
            hud.climateText = Greybox.Label(cap1, "Küresel İklim  <color=#10B981>%68</color>", 8, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleLeft, 30f, 3f, 108f, 13f, FontStyle.Bold);
            hud.climateBar = Greybox.Bar(cap1, 30f, 19f, 106f, 6f, Color.white, Color.white, false, "bar_bg_pill", "bar_fill_climate");
            hud.climateBar.Set(0.68f, Color.white);

            // 2. Gösterge: Kamuoyu Desteği (card_stat_public.png + icon_public_aqua.png + bar_bg_pill.png + bar_fill_public.png)
            var cap2 = Greybox.SpriteBox(headerRt, "Stat_PublicSupport", "card_stat_public", 153f, 4f, 150f, 30f);
            Greybox.Icon(cap2, "icon_public_aqua", 6f, 5f, 20f, 20f, Color.white);
            hud.mentalText = Greybox.Label(cap2, "Kamuoyu Desteği  <color=#38BDF8>%70</color>", 8, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleLeft, 30f, 3f, 114f, 13f, FontStyle.Bold);
            hud.mentalBar = Greybox.Bar(cap2, 30f, 19f, 112f, 6f, Color.white, Color.white, false, "bar_bg_pill", "bar_fill_public");
            hud.mentalBar.Set(0.70f, Color.white);

            // 3. Gösterge: Bakanlık Bütçesi + 15 Saniyelik Sayaç (card_stat_budget.png + icon_budget_aqua.png + ring_budget_15s.png)
            var cap3 = Greybox.SpriteBox(headerRt, "Stat_MinistryBudget", "card_stat_budget", 307f, 4f, 156f, 30f);
            Greybox.Icon(cap3, "icon_budget_aqua", 6f, 5f, 20f, 20f, Color.white);
            hud.moneyText = Greybox.Label(cap3, "Bütçe <color=#F59E0B>$85</color> <size=6><color=#94A3B8>(+$40/15sn)</color></size>", 7, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleLeft, 28f, 2f, 124f, 11f, FontStyle.Bold);
            hud.authorityText = Greybox.Label(cap3, "Otorite <color=#A855F7><b>5</b></color>   İmza <color=#E2E8F0><b>3/3</b></color>", 7, new Color(0.89f, 0.91f, 0.94f), TextAnchor.MiddleLeft, 28f, 12f, 124f, 10f);
            hud.fundingTimerBar = Greybox.Bar(cap3, 28f, 23f, 120f, 4f, Color.white, Color.white, false, "bar_bg_pill", "ring_budget_15s");
            hud.fundingTimerBar.fill.parent.name = "BudgetTimerRing";
            hud.fundingTimerBar.Set(0.1f, Color.white);

            // Floating "+$40 Kamuoyu Fonu" Animasyon Metni (Üst barın altına yerleştirildi, metinlerle çakışmaz)
            var floatTxt = Greybox.Label(headerRt, "<b>+$40 Kamuoyu Fonu</b>", 8, new Color(0.06f, 0.73f, 0.51f, 1f), TextAnchor.MiddleCenter, 305f, 42f, 160f, 14f, FontStyle.Bold);
            floatTxt.gameObject.name = "Floating_Budget_Popup";
            floatTxt.gameObject.SetActive(false);
            hud.floatingBudgetText = floatTxt;

            // KURAL 5: Sağ Köşe 1: GÜN 1 Göstergesi (Sadece bilgi amaçlı gösterge; "Günü Bitir" yazısı ve buton özelliği YOK)
            var dayBox = Greybox.SpriteBox(headerRt, "Day_Badge", "card_stat_top_aqua", 467f, 4f, 64f, 30f);
            hud.dayText = Greybox.Label(dayBox, "GÜN 1", 10, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 0f, 0f, 64f, 30f, FontStyle.Bold);
            hud.nextDayButton = null;

            var topBarTriggers = Greybox.Empty("TopBar_3D_Triggers", monitorHead, Vector3.zero);

            // Sağ Köşe 2: Masadan Kalk (btn_header_utility.png)
            var exitBox = Greybox.SpriteBox(headerRt, "Btn_ExitDesk_Visual", "btn_header_utility", 535f, 4f, 68f, 30f);
            Greybox.Label(exitBox, "<b>Masadan Kalk</b>\n<size=6><color=#94A3B8>Odaya Dön</color></size>", 7, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 0f, 1f, 68f, 28f);

            hud.fullscreenButton = CreateCanvasTrigger("Btn_ExitDesk_Trigger", topBarTriggers, 8f + 535f, 6f + 4f, 68f, 30f, ppm, "");
        }

        // ============================================================ B. HOLOGRAFİK SU YEŞİLİ DÜNYA HARİTASI

        void BuildWorldMapWorkspace(
            RectTransform screenBg,
            Transform monitorHead,
            WorldMapMonitorController controller,
            ContinentCrisisSpawner spawner,
            BlindChoiceDialogueUI blindUi,
            float ppm,
            Vector2 screenSizeMeters)
        {
            var worldMap2D = Greybox.SpriteBox(screenBg, "World_Map_2D_Group", "panel_main_aqua", 8f, 48f, 608f, 288f);
            blindUi.backgroundMap2D = worldMap2D.gameObject;

            var mapGo = new GameObject("World_Map_Image", typeof(RectTransform));
            mapGo.transform.SetParent(worldMap2D, false);
            var mapRt = (RectTransform)mapGo.transform;
            Greybox.Place(mapRt, 3f, 3f, 602f, 282f);

            var rawImg = mapGo.AddComponent<RawImage>();
            rawImg.raycastTarget = false;
            controller.BindRawImage(rawImg);

            // KURAL 2.1: Holografik Harita Katmanı (world_map_aqua_grid.png)
            var gridOverlay = Greybox.Icon(worldMap2D, "world_map_aqua_grid", 3f, 3f, 602f, 282f, Color.white);
            gridOverlay.gameObject.name = "World_Map_Aqua_Grid_Overlay";
            gridOverlay.preserveAspect = false;

            var worldMap3D = Greybox.Empty("World_Map_3D_Group", monitorHead, Vector3.zero);
            blindUi.backgroundMap3D = worldMap3D.gameObject;

            var screenHitGo = Greybox.Empty("Screen_Raycast_Surface", worldMap3D, new Vector3(0f, 0f, -0.017f));
            var boxCol = screenHitGo.gameObject.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(screenSizeMeters.x, screenSizeMeters.y, 0.006f);
            var screenInt = screenHitGo.gameObject.AddComponent<WorldMapScreenInteractable>();
            screenInt.SetHoverLabel("");
            screenInt.screenPhysicalSize = screenSizeMeters;
            screenInt.mapPixelOffset = new Vector2(11f, 51f);
            screenInt.mapPixelSize = new Vector2(602f, 282f);
            screenInt.canvasTotalSize = new Vector2(624f, 344f);

            var pinContainer = Greybox.Empty("Continent_Pin_Container", worldMap3D, Vector3.zero);

            for (int i = 0; i < 7; i++)
            {
                var cont = controller.continents[i];
                Vector2 uv = cont.primaryLandUV;

                float pixelX = 11f + uv.x * 602f;
                float pixelY = 51f + (1f - uv.y) * 282f;

                float localMetersX = (pixelX - 312f) / ppm;
                float localMetersY = -(pixelY - 172f) / ppm;

                var pin = CreateMinimalContinentPin(pinContainer, i, cont.name, localMetersX, localMetersY);
                spawner.RegisterPin(cont.type, pin);
            }
        }

        WorldMapPin CreateMinimalContinentPin(Transform parent, int contIndex, string contName, float localX, float localY)
        {
            var pinRoot = Greybox.Empty($"Pin_{contIndex}_{contName}", parent, new Vector3(localX, localY, -0.022f));

            // KURAL 2: Kriz pinlerinde 3D PointLight KESİNLİKLE YOK; yalnızca materyal ve UI renk değerleri kullanılır.
            Greybox.Prim(PrimitiveType.Cylinder, "Ring", pinRoot, Vector3.zero, new Vector3(0.018f, 0.002f, 0.018f), new Color(0.06f, 0.73f, 0.51f), unlit: true, collider: false);

            var sphereGo = Greybox.Prim(PrimitiveType.Sphere, "Beacon", pinRoot, new Vector3(0f, 0.008f, 0f), new Vector3(0.020f, 0.020f, 0.020f), new Color(0.98f, 0.44f, 0.52f), unlit: true, collider: true);

            var pinCanvas = Greybox.WorldCanvas("Canvas", pinRoot, new Vector3(0f, 0.026f, 0f), Vector3.zero, new Vector2(0.14f, 0.028f), 750f);
            var pCanvas = pinCanvas.GetComponent<Canvas>();
            if (pCanvas != null)
            {
                pCanvas.overrideSorting = true;
                pCanvas.sortingOrder = 10;
            }
            var pinBadge = Greybox.SpriteBox(pinCanvas, "Badge", "card_stat_top_aqua", 0f, 0f, 105f, 21f);
            Greybox.Stretch(pinBadge);

            var pinIcon = Greybox.Icon(pinBadge, "map_pin_crisis", 3f, 2f, 16f, 16f, Color.white);
            var titleTxt = Greybox.Label(pinBadge, contName, 8, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleLeft, 22f, 2f, 80f, 17f, FontStyle.Bold);

            var pin = sphereGo.AddComponent<WorldMapPin>();
            pin.pinRenderer = sphereGo.GetComponent<Renderer>();
            pin.pinIconImage = pinIcon;
            pin.titleText = titleTxt;
            pin.uiRoot = pinCanvas.gameObject;

            pin.SetupContinent(contIndex, contName);
            return pin;
        }

        // ============================================================ C. V3 KRİZ & TELEMETRİ DOSYASI + YAPTIRIM MİNİ-OYUN MODALI

        void BuildMinisterDossier(
            RectTransform screenBg,
            Transform monitorHead,
            BlindChoiceDialogueUI blindUi,
            SanctionMiniGameUI miniGameUi,
            float ppm)
        {
            var dossierGroup = Greybox.Empty("Ministry_Dossier_Root", monitorHead, Vector3.zero);
            blindUi.rootObject = dossierGroup.gameObject;

            float dX = 8f;
            float dY = 48f;
            float dW = 608f;
            float dH = 288f;

            Vector2 fullCanvasMeters = new Vector2(624f / ppm, 344f / ppm);
            var dossierCanvasRt = Greybox.WorldCanvas("Ministry_Dossier_Canvas", dossierGroup, new Vector3(0f, 0f, -0.017f), Vector3.zero, fullCanvasMeters, ppm);
            var dCanvas = dossierCanvasRt.GetComponent<Canvas>();
            if (dCanvas != null)
            {
                dCanvas.overrideSorting = true;
                dCanvas.sortingOrder = 20;
            }

            var mainBox = Greybox.SpriteBox(dossierCanvasRt, "Dossier_Main", "panel_main_aqua", dX, dY, dW, dH);

            var btnRoot3D = Greybox.Empty("Dossier_3D_Buttons", dossierGroup, Vector3.zero);

            // ------------------------------------------------------------
            // 1. SOL/ORTA KISIM: MUHATAP KARTI, UNVAN ETİKETİ, BEYAN & UYDU TELEMETRİSİ
            // ------------------------------------------------------------
            var leftCard = Greybox.SpriteBox(mainBox, "Left_Continent_Card", "card_stat_top_aqua", 8f, 8f, 204f, 236f);

            blindUi.continentTitleText = Greybox.Label(leftCard, "KUZEY AMERİKA", 11, new Color(0.22f, 0.74f, 0.97f), TextAnchor.MiddleLeft, 10f, 6f, 184f, 15f, FontStyle.Bold);
            blindUi.representativeText = Greybox.Label(leftCard, "Başkan Jack Harper", 9, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleLeft, 10f, 22f, 184f, 14f, FontStyle.Bold);

            // Muhatap Unvan Etiketi: badge_speaker_tag.png
            var badgeRt = Greybox.SpriteBox(leftCard, "Status_Badge", "badge_speaker_tag", 10f, 39f, 184f, 20f);
            blindUi.statusBadgeBg = badgeRt.GetComponent<Image>();
            blindUi.statusBadgeText = Greybox.Label(badgeRt, "Kıta Başkanı  |  Uyum %25", 7, new Color(0.89f, 0.91f, 0.94f), TextAnchor.MiddleCenter, 4f, 1f, 176f, 18f, FontStyle.Bold);

            // Kutu 1: Muhatap Beyanı (card_dialogue_president.png / card_dialogue_citizen.png)
            var stmtCard = Greybox.SpriteBox(leftCard, "Official_Statement_Box", "card_dialogue_president", 8f, 63f, 188f, 64f);
            blindUi.statementCardBg = stmtCard.GetComponent<Image>();
            Greybox.Label(stmtCard, "<b>MUHATAP BEYANI</b>", 7, new Color(0.58f, 0.64f, 0.72f), TextAnchor.MiddleLeft, 6f, 3f, 176f, 11f);
            blindUi.officialStatementText = Greybox.Label(stmtCard, "Muhatap beyanı metni.", 7, new Color(0.94f, 0.96f, 0.98f), TextAnchor.UpperLeft, 6f, 15f, 176f, 46f);

            // Kutu 2: Uydu & Sensör Telemetrisi (TelemetryPanel -> panel_telemetry_glass.png + icon_satellite_sensor.png)
            var sensorCard = Greybox.SpriteBox(leftCard, "TelemetryPanel", "panel_telemetry_glass", 8f, 131f, 188f, 66f);
            blindUi.telemetryCardBg = sensorCard.GetComponent<Image>();
            Greybox.Icon(sensorCard, "icon_satellite_sensor", 5f, 2f, 12f, 12f, Color.white);
            Greybox.Label(sensorCard, "<b>UYDU & SENSÖR TELEMETRİSİ</b>", 7, new Color(0.66f, 0.33f, 0.97f), TextAnchor.MiddleLeft, 20f, 3f, 162f, 11f);
            blindUi.satelliteSensorText = Greybox.Label(sensorCard, "Karbon Yoğunluğu: %78 (Kritik) | Filtre Aktifliği: %12", 7, new Color(0.89f, 0.91f, 0.94f), TextAnchor.UpperLeft, 6f, 15f, 176f, 48f);

            // Standart "Denetim & Yaptırım Başlat" Butonu (btn_sanction_strike.png + icon_sanction_gavel.png)
            var verifyRt = Greybox.SpriteBox(leftCard, "Btn_Verify_Visual", "btn_sanction_strike", 8f, 201f, 188f, 28f, raycast: true);
            blindUi.verifyReportButtonBg = verifyRt.GetComponent<Image>();
            var verifyUiBtn = verifyRt.gameObject.AddComponent<Button>();
            verifyUiBtn.targetGraphic = blindUi.verifyReportButtonBg;
            blindUi.verifyReportUIButton = verifyUiBtn;
            Greybox.Icon(verifyRt, "icon_sanction_gavel", 8f, 5f, 18f, 18f, Color.white);
            blindUi.verifyReportButtonText = Greybox.Label(verifyRt, "<color=#FEF2F2><b>Denetim & Yaptırım Başlat</b></color>", 8, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 24f, 2f, 158f, 24f, FontStyle.Bold);

            blindUi.verifyReportButton = CreateCanvasTrigger(
                "Btn_Verify_Report",
                btnRoot3D,
                dX + 8f + 8f,
                dY + 8f + 201f,
                188f,
                28f,
                ppm,
                "");

            // ------------------------------------------------------------
            // 2. SAĞ ÜST KÖŞE: KAPATMA İKONU '✕' (btn_header_utility.png)
            // ------------------------------------------------------------
            var closeRt = Greybox.SpriteBox(mainBox, "Btn_Close_X", "btn_header_utility", 572f, 8f, 28f, 24f);
            Greybox.Label(closeRt, "<b>✕</b>", 10, new Color(0.94f, 0.99f, 0.98f), TextAnchor.MiddleCenter, 0f, 0f, 28f, 24f, FontStyle.Bold);

            blindUi.closeButton = CreateCanvasTrigger(
                "Btn_Close_Dossier",
                btnRoot3D,
                dX + 572f,
                dY + 8f,
                28f,
                24f,
                ppm,
                "");

            // ------------------------------------------------------------
            // 3. ORTA / SAĞ KISIM: ERİYEN ZAMAN ÇUBUĞU (bar_timer_urgent.png) & 3 SEÇİM KARTI (btn_choice_aqua.png)
            // ------------------------------------------------------------
            var rightArea = Greybox.SpriteBox(mainBox, "Right_Crisis_Area", "card_stat_top_aqua", 220f, 8f, 346f, 56f);

            blindUi.decisionTimerBar = Greybox.Bar(rightArea, 10f, 4f, 326f, 6f, Color.white, Color.white, false, "bar_bg_pill", "bar_timer_urgent");
            blindUi.decisionTimerBar.Set(1f, Color.white);

            blindUi.crisisTitleText = Greybox.Label(rightArea, "SANAYİ EMİSYONU VE FİLTRE KRİZİ", 9, new Color(0.94f, 0.27f, 0.27f), TextAnchor.MiddleLeft, 10f, 12f, 326f, 14f, FontStyle.Bold);
            blindUi.descriptionText = Greybox.Label(rightArea, "Kriz açıklaması.", 7, new Color(0.94f, 0.99f, 0.98f), TextAnchor.UpperLeft, 10f, 27f, 326f, 26f);

            blindUi.choiceButtons = new PressButton[3];
            blindUi.choiceTexts = new Text[3];
            blindUi.choiceBgImages = new Image[3];

            for (int i = 0; i < 3; i++)
            {
                float cardX = 220f;
                float cardY = 68f + i * 58f;
                float cardW = 380f;
                float cardH = 52f;

                var cPanel = Greybox.SpriteBox(mainBox, $"Choice_Card_{i + 1}", "btn_choice_aqua", cardX, cardY, cardW, cardH);
                blindUi.choiceBgImages[i] = cPanel.GetComponent<Image>();

                var numBadge = Greybox.SpriteBox(cPanel, "Number_Badge", "btn_choice_hover", 8f, 12f, 28f, 28f);
                Greybox.Label(numBadge, $"<b>{i + 1}</b>", 10, new Color(0.94f, 0.99f, 0.98f), TextAnchor.MiddleCenter, 0f, 0f, 28f, 28f, FontStyle.Bold);

                blindUi.choiceTexts[i] = Greybox.Label(cPanel, "Bakanlık karar metni", 8, new Color(0.94f, 0.99f, 0.98f), TextAnchor.MiddleLeft, 44f, 6f, 326f, 40f);

                blindUi.choiceButtons[i] = CreateCanvasTrigger(
                    $"Btn_Choice_{i + 1}",
                    btnRoot3D,
                    dX + cardX,
                    dY + cardY,
                    cardW,
                    cardH,
                    ppm,
                    "");
            }

            // ------------------------------------------------------------
            // 4. ALT KISIM: BAKANLIK MÜDAHALESİ (btn_bribe_lobby.png & btn_header_utility.png)
            // ------------------------------------------------------------
            var supRt = Greybox.SpriteBox(mainBox, "Btn_BudgetSupport_Visual", "btn_bribe_lobby", 8f, 250f, 292f, 30f);
            Greybox.Icon(supRt, "icon_budget_aqua", 12f, 5f, 20f, 20f, Color.white);
            blindUi.budgetSupportText = Greybox.Label(supRt, "<b>Bütçe Desteği Sağla</b>", 9, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 28f, 3f, 254f, 24f, FontStyle.Bold);

            blindUi.budgetSupportButton = CreateCanvasTrigger(
                "Btn_BudgetSupport_Trigger",
                btnRoot3D,
                dX + 8f,
                dY + 250f,
                292f,
                30f,
                ppm,
                "");

            var audRt = Greybox.SpriteBox(mainBox, "Btn_AuditPressure_Visual", "btn_header_utility", 308f, 250f, 292f, 30f);
            Greybox.Icon(audRt, "icon_sanction_gavel", 12f, 5f, 20f, 20f, Color.white);
            blindUi.auditPressureText = Greybox.Label(audRt, "<b>Denetim Raporuyla Baskı Kur</b>", 9, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 28f, 3f, 254f, 24f, FontStyle.Bold);

            blindUi.auditPressureButton = CreateCanvasTrigger(
                "Btn_AuditPressure_Trigger",
                btnRoot3D,
                dX + 308f,
                dY + 250f,
                292f,
                30f,
                ppm,
                "");

            // ------------------------------------------------------------
            // 5. KURAL 5: IN-OS YAPTIRIM MİNİ-OYUN MODALI (MiniGameModal -> panel_minigame_frame.png)
            // ------------------------------------------------------------
            BuildSanctionMiniGameModal(dossierGroup, dossierCanvasRt, miniGameUi, ppm);

            dossierGroup.gameObject.SetActive(false);
        }

        void BuildSanctionMiniGameModal(
            Transform dossierGroup,
            RectTransform dossierCanvasRt,
            SanctionMiniGameUI miniGameUi,
            float ppm)
        {
            if (miniGameUi == null) return;

            var modalGroup = Greybox.Empty("Sanction_MiniGame_Modal", dossierGroup, Vector3.zero);
            miniGameUi.modalRoot = modalGroup.gameObject;

            Vector2 fullCanvasMeters = new Vector2(624f / ppm, 344f / ppm);
            var mgCanvasRt = Greybox.WorldCanvas("MiniGame_Overlay_Canvas", modalGroup, new Vector3(0f, 0f, -0.023f), Vector3.zero, fullCanvasMeters, ppm);
            var mgCanvas = mgCanvasRt.GetComponent<Canvas>();
            if (mgCanvas != null)
            {
                mgCanvas.overrideSorting = true;
                mgCanvas.sortingOrder = 40;
            }

            var dimBg = Greybox.SpriteBox(mgCanvasRt, "MiniGame_Dim_BG", "panel_main_aqua", 8f, 48f, 608f, 288f, raycast: true);
            dimBg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);

            // Orta Mini-Oyun Penceresi (MiniGameModal -> panel_minigame_frame.png)
            float mX = 92f;
            float mY = 64f;
            float mW = 440f;
            float mH = 256f;
            var modalWin = Greybox.SpriteBox(mgCanvasRt, "MiniGame_Window", "panel_minigame_frame", mX, mY, mW, mH, raycast: true);

            miniGameUi.timerBar = Greybox.Bar(modalWin, 16f, 10f, 408f, 8f, Color.white, Color.white, false, "bar_bg_pill", "bar_timer_urgent");
            miniGameUi.timerBar.Set(1f, Color.white);

            miniGameUi.titleText = Greybox.Label(modalWin, "<color=#10B981>KANIT FREKANSI KİLİTLEME PROTOKOLÜ</color>", 11, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 16f, 22f, 408f, 18f, FontStyle.Bold);
            miniGameUi.subtitleText = Greybox.Label(modalWin, "İmleç yeşil hedef bölgeye girdiğinde SPACE veya Sol Tık ile 3 kez kilitleyin!", 8, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 16f, 42f, 408f, 16f);
            miniGameUi.statusProgressText = Greybox.Label(modalWin, "Kilitlenen Kanıt: 0 / 3", 9, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 16f, 60f, 408f, 16f, FontStyle.Bold);

            // ============================================================
            // MINI-OYUN 1: KANIT FREKANSI KİLİTLEME (minigame_target_zone.png & minigame_cursor_needle.png)
            // ============================================================
            var timingPanel = Greybox.SpriteBox(modalWin, "Timing_Container", "card_stat_top_aqua", 24f, 84f, 392f, 152f);
            miniGameUi.timingModeRoot = timingPanel.gameObject;

            var trackRt = Greybox.SpriteBox(timingPanel, "Timing_Track", "bar_bg_pill", 16f, 28f, 360f, 28f);
            miniGameUi.timingTrackRt = trackRt;

            var targetZoneRt = Greybox.SpriteBox(trackRt, "Green_Target_Zone", "minigame_target_zone", 130f, 2f, 96f, 24f);
            miniGameUi.timingTargetZoneRt = targetZoneRt;

            var cursorRt = Greybox.SpriteBox(trackRt, "Sliding_Cursor", "minigame_cursor_needle", 20f, -3f, 14f, 34f);
            miniGameUi.timingCursorRt = cursorRt;

            var lockBtnRt = Greybox.SpriteBox(timingPanel, "Btn_Lock_Evidence", "btn_choice_hover", 56f, 84f, 280f, 46f, raycast: true);
            var lockUiBtn = lockBtnRt.gameObject.AddComponent<Button>();
            lockUiBtn.targetGraphic = lockBtnRt.GetComponent<Image>();
            miniGameUi.timingLockUIButton = lockUiBtn;

            Greybox.Label(lockBtnRt, "<b>KANITI KİLİTLE (SPACE / SOL TIK)</b>", 10, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 0f, 0f, 280f, 46f, FontStyle.Bold);

            var timingTriggersRoot = Greybox.Empty("Timing_3D_Triggers", modalGroup, Vector3.zero);
            miniGameUi.timingTriggersRoot = timingTriggersRoot.gameObject;
            miniGameUi.timingLock3DButton = CreateCanvasTrigger(
                "Btn_Lock_Evidence_3D",
                timingTriggersRoot,
                mX + 24f + 56f,
                mY + 84f + 84f,
                280f,
                46f,
                ppm,
                "",
                zOffset: -0.026f);

            // ============================================================
            // MINI-OYUN 2: VERİ KARARTMA DÜĞÜMLERİNİ TEMİZLEME (icon_node_corrupt.png & icon_node_verified.png)
            // ============================================================
            var gridPanel = Greybox.SpriteBox(modalWin, "Grid_Container", "card_stat_top_aqua", 24f, 84f, 392f, 152f);
            miniGameUi.gridModeRoot = gridPanel.gameObject;

            var gridTriggersRoot = Greybox.Empty("Grid_3D_Triggers", modalGroup, Vector3.zero);
            miniGameUi.gridTriggersRoot = gridTriggersRoot.gameObject;

            miniGameUi.gridNodeImages = new Image[9];
            miniGameUi.gridNodeIcons = new Image[9];
            miniGameUi.gridNodeTexts = new Text[9];
            miniGameUi.gridNode3DButtons = new PressButton[9];
            miniGameUi.gridNodeUIButtons = new Button[9];

            for (int i = 0; i < 9; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float gx = 16f + col * 122f;
                float gy = 10f + row * 46f;
                float gw = 114f;
                float gh = 40f;

                var nodeRt = Greybox.SpriteBox(gridPanel, $"Data_Node_{i + 1}", "btn_choice_aqua", gx, gy, gw, gh, raycast: true);
                var nodeImg = nodeRt.GetComponent<Image>();
                var nodeUiBtn = nodeRt.gameObject.AddComponent<Button>();
                nodeUiBtn.targetGraphic = nodeImg;

                miniGameUi.gridNodeImages[i] = nodeImg;
                miniGameUi.gridNodeUIButtons[i] = nodeUiBtn;
                miniGameUi.gridNodeIcons[i] = Greybox.Icon(nodeRt, "icon_node_verified", 6f, 6f, 28f, 28f, Color.white);
                miniGameUi.gridNodeTexts[i] = Greybox.Label(nodeRt, "<b>ONAYLI\nKANIT</b>", 7, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleLeft, 38f, 2f, gw - 42f, gh - 4f, FontStyle.Bold);

                miniGameUi.gridNode3DButtons[i] = CreateCanvasTrigger(
                    $"Btn_Grid_Node_{i + 1}",
                    gridTriggersRoot,
                    mX + 24f + gx,
                    mY + 84f + gy,
                    gw,
                    gh,
                    ppm,
                    "",
                    zOffset: -0.026f);
            }

            modalGroup.gameObject.SetActive(false);
        }

        // ============================================================ 4. SERBEST DOLAŞIM KARAKTERİ (FirstPersonPlayer)

        PlayerInteractor BuildFirstPersonPlayer(Vector3 startPos, Quaternion startRot, MovableComputerStation workstation)
        {
            var playerGo = new GameObject("FirstPersonPlayer");
            playerGo.transform.position = startPos;
            playerGo.transform.rotation = startRot;

            var cc = playerGo.AddComponent<CharacterController>();
            cc.radius = 0.24f;
            cc.height = 1.62f;
            cc.center = new Vector3(0f, 0.81f, 0f);
            cc.stepOffset = 0.25f;
            cc.skinWidth = 0.03f;

            var cameraMount = Greybox.Empty("PlayerCameraMount", playerGo.transform, new Vector3(0f, 1.42f, 0f), new Vector3(6f, 0f, 0f));

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }

            cam.transform.SetParent(cameraMount, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 40f;
            cam.backgroundColor = new Color(0.06f, 0.09f, 0.16f);

            var interactor = playerGo.AddComponent<PlayerInteractor>();
            interactor.characterController = cc;
            interactor.cameraMount = cameraMount;
            interactor.playerCamera = cam;
            interactor.workstation = workstation;
            interactor.monitorTarget = workstation != null ? workstation.cameraFocusPoint : null;
            interactor.Setup(cam, 6f);

            return interactor;
        }

        // ============================================================ 5. 'E' TUŞU ETKİLEŞİM İKONU, SİYAH EKRAN ("GÜN 2") GEÇİŞİ & DÖNEM SONU RAPORU

        void BuildCrosshairAndEndReportCanvas(Transform parent, HudController hud)
        {
            var go = new GameObject("Player_HUD_Canvas");
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();

            var dot = Greybox.SpriteBox(go.transform, "Crosshair", "btn_choice_hover", 0f, 0f, 6f, 6f);
            Greybox.PlaceCenter(dot, 0f, 6f, 6f);
            hud.crosshairImage = dot.GetComponent<Image>();

            // KURAL 1: Uzun etkileşim yazısı kaldırıldı; sadece bilgisayara yaklaşıp monitöre bakınca
            // ekranın ortasında minimalist 'E' Tuşu İkonu (KeyPrompt_E_Badge -> icon_key_e.png) belirir.
            var keyPromptImg = Greybox.Icon(go.transform, "icon_key_e", 0f, 0f, 64f, 64f, Color.white);
            keyPromptImg.gameObject.name = "KeyPrompt_E_Badge";
            Greybox.PlaceCenter(keyPromptImg.rectTransform, 0f, 64f, 64f);
            keyPromptImg.gameObject.SetActive(false);
            hud.keyPromptBadgeRoot = keyPromptImg.gameObject;
            hud.hoverLabelText = null;

            // KURAL 5: SİYAH EKRAN ("GÜN 2") GEÇİŞİ (DayTransition_BlackScreen - #000000 CanvasGroup Fade-In / 2.5sn / Fade-Out)
            var dayTransGo = new GameObject("DayTransition_BlackScreen", typeof(RectTransform));
            dayTransGo.transform.SetParent(go.transform, false);
            var dayTransRt = (RectTransform)dayTransGo.transform;
            Greybox.Stretch(dayTransRt);
            var dayTransImg = dayTransGo.AddComponent<Image>();
            dayTransImg.color = Color.black; // #000000 Saf Siyah Ekran
            dayTransImg.raycastTarget = true;

            var dayTransCg = dayTransGo.AddComponent<CanvasGroup>();
            dayTransCg.alpha = 0f;
            dayTransCg.blocksRaycasts = false;
            dayTransCg.interactable = false;

            var dayTransTitle = Greybox.Label(dayTransRt, "GÜN 2", 72, new Color(0.98f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 0f, 0f, 1920f, 1080f, FontStyle.Bold);
            dayTransTitle.gameObject.name = "DayTransition_Title";
            Greybox.Stretch((RectTransform)dayTransTitle.transform);

            hud.dayTransitionCanvasGroup = dayTransCg;
            hud.dayTransitionTitleText = dayTransTitle;
            dayTransGo.SetActive(false);

            // MODERN "BAKANLIK DÖNEM RAPORU" PENCERESİ (EndScreen / EndScreenGrid)
            var endRoot = Greybox.SpriteBox(go.transform, "EndScreen", "panel_main_aqua", 0f, 0f, 1920f, 1080f, raycast: true);
            Greybox.Stretch(endRoot);
            endRoot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);

            var modalCard = Greybox.SpriteBox(endRoot, "EndScreenGrid", "panel_main_aqua", 0f, 0f, 980f, 620f, raycast: true);
            Greybox.PlaceCenter(modalCard, 0f, 980f, 620f);

            var title = Greybox.Label(modalCard, "DÖNEM BAŞARIYLA TAMAMLANDI", 30, new Color(0.06f, 0.73f, 0.51f), TextAnchor.MiddleCenter, 40f, 28f, 900f, 44f, FontStyle.Bold);
            var subtitle = Greybox.Label(modalCard, "Görev dönemi özeti.", 16, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 60f, 76f, 860f, 54f);

            string[] widgetTitles = new string[6]
            {
                "Görev Günü",
                "İmzalanan Kararlar",
                "Küresel İklim",
                "Kamuoyu Desteği",
                "Bakanlık Bütçesi",
                "Siyasi Otorite"
            };
            string[] widgetIcons = new string[6]
            {
                "icon_climate_aqua",
                "icon_sanction_gavel",
                "icon_climate_aqua",
                "icon_public_aqua",
                "icon_budget_aqua",
                "icon_satellite_sensor"
            };
            string[] defaultValues = new string[6]
            {
                "Gün 12 / 12",
                "15 Karar",
                "%82",
                "%78",
                "$125",
                "7 Puan"
            };

            hud.endStatValueTexts = new Text[6];
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float wx = 46f + col * 304f;
                float wy = 148f + row * 150f;
                float ww = 280f;
                float wh = 128f;

                var wCard = Greybox.SpriteBox(modalCard, $"Stat_Widget_{i}", "card_stat_top_aqua", wx, wy, ww, wh);
                Greybox.Icon(wCard, widgetIcons[i], 20f, 28f, 64f, 64f, Color.white);
                Greybox.Label(wCard, widgetTitles[i], 15, new Color(0.06f, 0.73f, 0.51f), TextAnchor.MiddleLeft, 98f, 28f, 168f, 26f, FontStyle.Bold);
                hud.endStatValueTexts[i] = Greybox.Label(wCard, defaultValues[i], 24, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleLeft, 98f, 60f, 168f, 40f, FontStyle.Bold);
            }

            var restartBtnRt = Greybox.SpriteBox(modalCard, "Btn_Restart_Period", "btn_header_utility", 310f, 492f, 360f, 68f, raycast: true);
            var btnImg = restartBtnRt.GetComponent<Image>();
            var uiBtn = restartBtnRt.gameObject.AddComponent<Button>();
            uiBtn.targetGraphic = btnImg;

            var colors = uiBtn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.80f, 0.88f, 0.95f, 1f);
            uiBtn.colors = colors;

            Greybox.Label(restartBtnRt, "<b>Yeni Dönemi Başlat</b>", 22, new Color(0.97f, 0.98f, 0.99f), TextAnchor.MiddleCenter, 0f, 0f, 360f, 68f, FontStyle.Bold);

            hud.endScreenRoot = endRoot.gameObject;
            hud.endTitleText = title;
            hud.endSubtitleText = subtitle;
            hud.restartButton = uiBtn;

            endRoot.gameObject.SetActive(false);
        }
    }
}
