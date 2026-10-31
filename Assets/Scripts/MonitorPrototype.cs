using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Pixamor
{
    // Sahnedeki hazır monitörün ekranında oyun arayüzünü çalıştırır.
    // Oda ve modeller yalnızca editördeki Odayı Kur menüsünden hazırlanır.
    [RequireComponent(typeof(GameSession), typeof(CrisisManager))]
    public class MonitorPrototype : MonoBehaviour
    {
        [SerializeField] private Camera operatorCamera;
        [SerializeField] private Transform screenMount;
        [SerializeField] private FirstPersonOperator player;
        private GameSession session;
        private CrisisManager crises;
        private GameObject prototypeRoot;
        private RectTransform content;
        private Text resources;
        private Font font;
        private Texture2D map;
        private Camera screenCamera;
        private GraphicRaycaster screenRaycaster;
        private string viewKey;
        private readonly List<Action> refreshers = new List<Action>();

        public void Configure(Camera camera, Transform screen, FirstPersonOperator controller)
        {
            operatorCamera = camera;
            screenMount = screen;
            player = controller;
        }

        private void Start()
        {
            if (operatorCamera == null || screenMount == null || player == null)
            {
                Debug.LogError("Monitör bağlantıları eksik. Play modundan çıkıp " +
                    "İklim Operatörü > Odayı Kur menüsünü kullan.", this);
                enabled = false;
                return;
            }
            session = GetComponent<GameSession>();
            crises = GetComponent<CrisisManager>();
            // Eski ekran üstü çizimin fiziksel monitörü örtmesini önler.
            var oldPanel = GetComponent<CrisisPrototypePanel>();
            if (oldPanel != null) oldPanel.enabled = false;
            var debugPanel = GetComponent<SessionDebugPanel>();
            if (debugPanel != null) debugPanel.enabled = false;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            map = CrisisMapTexture.Create();
            prototypeRoot = new GameObject("MonitorPrototype_Runtime");
            screenCamera = operatorCamera;
            BuildScreen();
            EnsureInput();
        }

        private void BuildScreen()
        {
            var canvasObject = new GameObject("MonitorScreen", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(prototypeRoot.transform);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = screenCamera;
            screenRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1100, 720);
            rect.SetPositionAndRotation(screenMount.position, screenMount.rotation);
            rect.localScale = Vector3.one * .005f;

            Panel(rect, "ScreenBackground", 0, 0, 1100, 720, new Color(.035f,.065f,.085f));
            Label(rect, "İKLİM OPERATÖRÜ / KRİZ MASASI", 25, 15, 1050, 40, 26);
            var sidebar = Panel(rect, "Resources", 760, 70, 315, 625, new Color(.07f,.12f,.15f));
            resources = Label(sidebar, "", 15, 15, 285, 590, 20);
            content = Panel(rect, "Content", 25, 70, 710, 625, new Color(.045f,.09f,.12f));
        }

        private void EnsureInput()
        {
            // UI ışınları, kameradan monitördeki World Space Canvas'a gönderilir.
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var input = new GameObject("MonitorEventSystem", typeof(EventSystem));
            input.transform.SetParent(prototypeRoot.transform);
            input.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void Update()
        {
            if (prototypeRoot == null) return;
            // Fare yalnızca monitör kullanımındayken fiziksel ekrana tıklayabilir.
            screenRaycaster.enabled = player.IsUsingMonitor;
            int seconds = Mathf.CeilToInt(session.RemainingTime);
            string warning = (session.HasHappinessWarning ? "\nUYARI: Halk protestoları!" : "") +
                (session.HasClimateWarning ? "\nUYARI: İklim kritik!" : "");
            resources.text = $"VARDİYA  {seconds / 60:00}:{seconds % 60:00}\n\n" +
                $"PARA  {session.Money}\nHALK  {session.Happiness}/100\nİKLİM  {session.Climate}/100\n\n" +
                $"Bütçe: {Mathf.CeilToInt(session.TimeUntilIncome)} sn\n\n" +
                $"Müdahale: {crises.HandledCount}\nKaçırılan: {crises.MissedCount}" + warning +
                (session.IsPaused ? "\n\nKART AÇIK\nZaman duraklatıldı." : "") +
                "\n\nSON HABER\n" + crises.LastResult;

            string key = session.State != SessionState.Running ? "end:" + session.State :
                crises.OpenCard != null ? "card:" + crises.OpenCard.Id : "map";
            if (key == "map")
                foreach (var crisis in crises.Active) key += ":" + crisis.Id;
            if (key != viewKey)
            {
                viewKey = key;
                ClearContent();
                if (session.State != SessionState.Running) BuildEnd();
                else if (crises.OpenCard != null) BuildCard();
                else BuildMap();
            }
            // Sayaç metinlerini güncellerken butonları yeniden üretmeyiz.
            foreach (var refresh in refreshers) refresh();
        }

        private void ClearContent()
        {
            refreshers.Clear();
            foreach (Transform child in content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void BuildMap()
        {
            Label(content, "DÜNYA KRİZ HARİTASI", 15, 10, 680, 35, 24);
            Label(content, "İşarete tıklayarak müdahale kartını aç.", 15, 45, 680, 30, 18);
            var mapRect = Rect(content, "Map", 15, 95, 680, 360);
            var image = mapRect.gameObject.AddComponent<RawImage>();
            image.texture = map;
            image.raycastTarget = false;
            foreach (var crisis in crises.Active)
            {
                var current = crisis;
                Vector2 p = current.Data.MapPosition;
                var button = Button(mapRect, "", Mathf.Clamp01(p.x) * 680 - 29,
                    Mathf.Clamp01(p.y) * 360 - 28, 58, 56, () => crises.Open(current));
                var text = button.GetComponentInChildren<Text>();
                refreshers.Add(() =>
                {
                    float left = crises.SecondsLeft(current);
                    text.text = Code(current.Data.Type) + "\n" + Mathf.CeilToInt(left) + " sn";
                    button.GetComponent<Image>().color = left <= 10f
                        ? new Color(.8f,.15f,.1f) : new Color(.12f,.34f,.45f);
                });
            }
            Label(content, "SEL: Sel   KUR: Kuraklık   YAN: Yangın   SIC: Sıcaklık", 15, 470, 680, 35, 17);
            Label(content, crises.Active.Count == 0 ? "Aktif kriz yok. Uyarılar bekleniyor." :
                $"Aktif kriz: {crises.Active.Count}\nSon 10 saniyede işaret kırmızı olur.", 15, 520, 680, 80, 21);
        }

        private void BuildCard()
        {
            var crisis = crises.OpenCard;
            Label(content, crisis.Data.Title, 15, 10, 680, 40, 26);
            Label(content, crisis.Data.Region + " / " + Mathf.CeilToInt(crises.SecondsLeft(crisis)) +
                " sn — ZAMAN DURDU", 15, 55, 680, 35, 19);
            Label(content, crisis.Data.Description, 15, 100, 680, 75, 21);
            for (int i = 0; i < crisis.Data.Options.Length; i++)
            {
                int index = i; // Her buton kendi seçenek numarasını saklar.
                var option = crisis.Data.Options[i];
                string cost = option.Cost == 0 ? "Ücretsiz" : "Para: −" + option.Cost;
                string text = option.Title + "\n" + cost + " | Halk: " + Signed(option.HappinessChange) +
                    " | İklim: " + Signed(option.ClimateChange);
                bool affordable = session.CanAfford(option.Cost);
                if (!affordable) text += "  [PARA YETERSİZ]";
                var button = Button(content, text, 15, 190 + i * 105, 680, 90, () => crises.Choose(index));
                button.interactable = affordable;
            }
            Button(content, "Karar vermeden kapat", 15, 535, 680, 60, crises.CloseCard);
        }

        private void BuildEnd()
        {
            Label(content, session.State == SessionState.Won ? "VARDİYA TAMAMLANDI" : "VARDİYA KAYBEDİLDİ", 15, 20, 680, 60, 27);
            Label(content, session.EndReason + $"\n\nMüdahale: {crises.HandledCount}\nKaçırılan: {crises.MissedCount}",
                15, 100, 680, 250, 23);
            Button(content, "Yeni vardiya", 15, 400, 680, 70, () =>
            {
                session.RestartSession();
                viewKey = null;
            });
        }

        // Bütün UI konumları ekranın sol üst köşesine göre piksel olarak tanımlanır.
        private RectTransform Rect(Transform parent, string title, float x, float y, float w, float h)
        {
            var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private Text Label(Transform parent, string text, float x, float y, float w, float h, int size)
        {
            var label = Rect(parent, "Text", x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = new Color(.87f,.95f,.94f);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Button Button(Transform parent, string text, float x, float y, float w, float h, Action action)
        {
            var rect = Panel(parent, "Button", x, y, w, h, new Color(.12f,.27f,.34f));
            rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => action());
            var label = Label(rect, text, 6, 4, w - 12, h - 8, 20);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
        private static string Code(CrisisType type)
        {
            switch (type)
            {
                case CrisisType.Flood: return "SEL";
                case CrisisType.Drought: return "KUR";
                case CrisisType.Wildfire: return "YAN";
                default: return "SIC";
            }
        }

        private void OnDestroy()
        {
            if (prototypeRoot != null) Destroy(prototypeRoot);
            if (map != null) Destroy(map);
        }
    }
}
