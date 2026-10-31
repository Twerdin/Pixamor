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
        private Text alertBanner;
        private Text clockText, moneyText, incomeText, happinessText, climateText;
        private Text handledText, missedText, newsText, clockState;
        private Image happinessFill, climateFill, shiftFill;
        private Sprite roundedSprite, circleSprite;
        private Texture2D shapeTexture, circleTexture;
        private Font font;
        private Texture2D map;
        private Camera screenCamera;
        private GraphicRaycaster screenRaycaster;
        private string viewKey;
        private readonly List<Action> refreshers = new List<Action>();
        // Ortak renkler: bütün ekran aynı görsel dili kullanır.
        private static readonly Color Background = new Color32(9, 17, 27, 255);
        private static readonly Color Surface = new Color32(16, 29, 43, 255);
        private static readonly Color Raised = new Color32(25, 43, 59, 255);
        private static readonly Color Teal = new Color32(69, 224, 193, 255);
        private static readonly Color Muted = new Color32(142, 166, 186, 255);
        private static readonly Color Amber = new Color32(255, 194, 97, 255);
        private static readonly Color Danger = new Color32(255, 113, 112, 255);

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
            if (GetComponent<MonitorFeedback>() == null)
                gameObject.AddComponent<MonitorFeedback>();
            // Eski ekran üstü çizimin fiziksel monitörü örtmesini önler.
            var oldPanel = GetComponent<CrisisPrototypePanel>();
            if (oldPanel != null) oldPanel.enabled = false;
            var debugPanel = GetComponent<SessionDebugPanel>();
            if (debugPanel != null) debugPanel.enabled = false;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roundedSprite = CreateShape(false, out shapeTexture);
            circleSprite = CreateShape(true, out circleTexture);
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

            Panel(rect, "ScreenBackground", 0, 0, 1100, 720, Background);
            var logo = Panel(rect, "Logo", 24, 22, 46, 46, Teal);
            var initials = Label(logo, "İO", 0, 0, 46, 46, 22);
            initials.color = Background;
            initials.alignment = TextAnchor.MiddleCenter;
            initials.fontStyle = FontStyle.Bold;
            Heading(rect, "İKLİM OPERATÖRÜ", 84, 20, 370, 32, 25);
            Caption(rect, "KÜRESEL KRİZ KOORDİNASYON MERKEZİ", 85, 54, 420, 20);
            alertBanner = Label(rect, "SİSTEM ÇEVRİMİÇİ", 480, 25, 596, 36, 16);
            alertBanner.alignment = TextAnchor.MiddleRight;
            Panel(rect, "HeaderLine", 24, 83, 1052, 2, Raised);

            content = Panel(rect, "Content", 24, 100, 724, 568, Surface);
            var sidebar = Panel(rect, "Resources", 768, 100, 308, 568, Surface);
            Caption(sidebar, "VARDİYA SÜRESİ", 20, 16, 170, 20);
            clockText = Heading(sidebar, "06:00", 20, 37, 160, 46, 38);
            clockState = Label(sidebar, "CANLI", 178, 48, 110, 25, 12);
            clockState.alignment = TextAnchor.MiddleRight;
            shiftFill = Bar(sidebar, 20, 90, 268, 5, Teal);

            var wallet = Panel(sidebar, "Budget", 14, 112, 280, 87, Raised);
            Caption(wallet, "KULLANILABİLİR BÜTÇE", 12, 9, 256, 18);
            moneyText = Heading(wallet, "60", 12, 28, 100, 44, 34);
            incomeText = Label(wallet, "", 110, 35, 158, 34, 14);
            incomeText.alignment = TextAnchor.MiddleRight;

            Caption(sidebar, "HALK MUTLULUĞU", 20, 218, 200, 20);
            happinessText = Heading(sidebar, "70", 210, 212, 78, 31, 25);
            happinessText.alignment = TextAnchor.MiddleRight;
            happinessFill = Bar(sidebar, 20, 251, 268, 10, Teal);
            Caption(sidebar, "Güvenlik ve yönetime güven", 20, 269, 268, 20);
            Caption(sidebar, "İKLİM DENGESİ", 20, 307, 200, 20);
            climateText = Heading(sidebar, "70", 210, 301, 78, 31, 25);
            climateText.alignment = TextAnchor.MiddleRight;
            climateFill = Bar(sidebar, 20, 340, 268, 10, new Color32(112, 181, 255, 255));
            Caption(sidebar, "25'in altı: kritik seviye", 20, 358, 268, 20);
            Panel(sidebar, "Divider", 20, 391, 268, 1, Raised);
            handledText = Heading(sidebar, "0", 20, 401, 115, 32, 25);
            missedText = Heading(sidebar, "0", 164, 401, 124, 32, 25);
            Caption(sidebar, "MÜDAHALE", 20, 435, 120, 18);
            Caption(sidebar, "KAÇIRILAN", 164, 435, 124, 18);
            var news = Panel(sidebar, "News", 14, 469, 280, 85, Raised);
            Caption(news, "SON DURUM", 12, 7, 256, 18);
            newsText = Label(news, "", 12, 29, 256, 50, 14);
            Caption(rect, "OPERATÖR TERMİNALİ / 01", 25, 686, 390, 20);
            var footer = Caption(rect, "FARE  SEÇ     /     E  AYRIL     /     ESC  İMLECİ BIRAK", 440, 686, 636, 20);
            footer.alignment = TextAnchor.MiddleRight;
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
            UpdateAlertBanner();
            int seconds = Mathf.CeilToInt(session.RemainingTime);
            clockText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            moneyText.text = session.Money.ToString();
            incomeText.text = $"+{session.IncomeAmount} bütçe\n{Mathf.CeilToInt(session.TimeUntilIncome)} sn sonra";
            if (session.State != SessionState.Running) incomeText.text = "VARDİYA\nKAPANDI";
            happinessText.text = session.Happiness.ToString();
            climateText.text = session.Climate.ToString();
            SetBar(happinessFill, session.Happiness / 100f, session.HasHappinessWarning ? Danger : Teal);
            SetBar(climateFill, session.Climate / 100f, session.HasClimateWarning ? Danger : new Color32(112,181,255,255));
            SetBar(shiftFill, session.RemainingTime / Mathf.Max(1f, session.RemainingTime + session.ElapsedTime), Teal);
            handledText.text = crises.HandledCount.ToString();
            missedText.text = crises.MissedCount.ToString();
            newsText.text = crises.LastResult;
            clockState.text = session.State != SessionState.Running ? "SONLANDI" : session.IsPaused ? "DURAKLATILDI" : "CANLI";
            clockState.color = session.State == SessionState.Lost ? Danger : session.IsPaused ? Amber : Teal;

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

        private void UpdateAlertBanner()
        {
            bool critical = session.HasHappinessWarning || session.HasClimateWarning;
            bool running = session.State == SessionState.Running;
            if (critical && running)
            {
                alertBanner.text = session.HasHappinessWarning && session.HasClimateWarning
                    ? "KRİTİK: HALK VE İKLİM TEHLİKEDE"
                    : session.HasHappinessWarning ? "PROTESTOLAR: HALKIN GÜVENİ KRİTİK"
                    : "KRİTİK: İKLİM DENGESİ BOZULUYOR";
                // Kart açıkken ışık sabitlenir; alarm metni görünür kalır.
                float pulse = session.IsPaused ? 1f : .7f + .3f * Mathf.Sin(session.ElapsedTime * 4f);
                alertBanner.color = new Color(1f, .25f + .3f * pulse, .15f);
            }
            else
            {
                alertBanner.text = running && crises.Active.Count > 0
                    ? $"●  {crises.Active.Count} AKTİF KRİZ / MÜDAHALE BEKLENİYOR"
                    : running ? "●  SİSTEM ÇEVRİMİÇİ / İZLEME SÜRÜYOR" : "VARDİYA RAPORU HAZIR";
                alertBanner.color = running && crises.Active.Count > 0 ? Amber : Teal;
            }
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
            Caption(content, "CANLI TAKİP", 20, 14, 300, 18);
            Heading(content, "Dünya kriz haritası", 20, 34, 500, 32, 25);
            Chip(content, crises.Active.Count + " AKTİF", 580, 24, 124, Amber);
            var mapRect = Rect(content, "Map", 22, 76, 680, 350);
            var image = mapRect.gameObject.AddComponent<RawImage>();
            image.texture = map;
            image.raycastTarget = false;
            foreach (var crisis in crises.Active)
            {
                var current = crisis;
                Vector2 p = current.Data.MapPosition;
                var color = TypeColor(current.Data.Type);
                var button = Button(mapRect, "", Mathf.Clamp01(p.x) * 680 - 25,
                    Mathf.Clamp01(p.y) * 350 - 25, 50, 50, () => crises.Open(current));
                button.GetComponent<Image>().sprite = circleSprite;
                button.GetComponent<Image>().type = Image.Type.Simple;
                var outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = color;
                outline.effectDistance = new Vector2(2, -2);
                var text = button.GetComponentInChildren<Text>();
                text.fontSize = 14;
                refreshers.Add(() =>
                {
                    float left = crises.SecondsLeft(current);
                    text.text = Code(current.Data.Type) + "\n" + Mathf.CeilToInt(left) + " sn";
                    button.GetComponent<Image>().color = left <= 10f ? new Color(.5f,.12f,.13f) : Raised;
                    outline.effectColor = left <= 10f ? Danger : color;
                });
            }
            Caption(content, "SEL  Sel     KUR  Kuraklık     YAN  Yangın     SIC  Sıcaklık", 22, 434, 680, 20);
            if (crises.Active.Count == 0)
            {
                var empty = Panel(content, "NoCrises", 20, 470, 684, 74, Raised);
                Label(empty, "Bölgeler izleniyor. Yeni uyarılar haritada görünecek.", 15, 22, 654, 30, 18);
            }
            int row = 0;
            foreach (var crisis in crises.Active)
            {
                if (row >= 2) break;
                var current = crisis;
                var button = Button(content, "", 20, 467 + row * 46, 684, 40, () => crises.Open(current));
                Panel(button.transform, "TypeAccent", 0, 6, 3, 28, TypeColor(current.Data.Type));
                Label(button.transform, current.Data.Region + " / " + current.Data.Title, 14, 9, 554, 24, 17);
                var remaining = Label(button.transform, "", 578, 9, 90, 24, 17);
                remaining.alignment = TextAnchor.MiddleRight;
                refreshers.Add(() =>
                {
                    float left = crises.SecondsLeft(current);
                    remaining.text = Mathf.CeilToInt(left) + " sn  ›";
                    remaining.color = left <= 10 ? Danger : Amber;
                });
                row++;
            }
        }

        private void BuildCard()
        {
            var crisis = crises.OpenCard;
            Caption(content, "MÜDAHALE DOSYASI / " + crisis.Data.Region, 20, 14, 520, 20);
            Chip(content, "ZAMAN DURDU", 550, 14, 154, Amber);
            Heading(content, crisis.Data.Title, 20, 48, 684, 42, 29);
            Label(content, crisis.Data.Description, 20, 101, 684, 58, 20);
            Caption(content, "BİR MÜDAHALE SEÇ / ETKİLER ANINDA UYGULANIR", 20, 163, 684, 22);
            for (int i = 0; i < crisis.Data.Options.Length; i++)
            {
                int index = i; // Her buton kendi seçenek numarasını saklar.
                var option = crisis.Data.Options[i];
                bool affordable = session.CanAfford(option.Cost);
                var button = Button(content, "", 20, 196 + i * 102, 684, 90, () => crises.Choose(index));
                button.interactable = affordable;
                Color accent = !affordable ? Muted : option.IsIntervention ? Teal : Amber;
                Panel(button.transform, "Accent", 0, 12, 3, 66, accent);
                var number = Heading(button.transform, "0" + (i + 1), 14, 14, 40, 30, 22);
                number.color = accent;
                var title = Heading(button.transform, option.Title, 64, 12, 490, 28, 19);
                if (!affordable) title.color = Muted;
                var action = Caption(button.transform, affordable ? "SEÇ  ›" : "KİLİTLİ", 568, 17, 100, 20);
                action.alignment = TextAnchor.MiddleRight;
                action.color = accent;
                Chip(button.transform, option.Cost == 0 ? "ÜCRETSİZ" : "BÜTÇE  −" + option.Cost,
                    64, 49, 158, affordable ? Amber : Danger);
                Chip(button.transform, "HALK  " + Signed(option.HappinessChange), 234, 49, 158,
                    option.HappinessChange >= 0 ? Teal : Danger);
                Chip(button.transform, "İKLİM  " + Signed(option.ClimateChange), 404, 49, 158,
                    option.ClimateChange >= 0 ? Teal : Danger);
                if (!affordable)
                    Caption(button.transform, "BÜTÇE\nYETERSİZ", 574, 46, 98, 35).color = Danger;
            }
            var back = Button(content, "‹  Haritaya dön / sonra karar ver", 20, 514, 684, 36, crises.CloseCard);
            back.GetComponent<Image>().color = Surface;
            back.GetComponentInChildren<Text>().fontSize = 16;
        }

        private void BuildEnd()
        {
            bool won = session.State == SessionState.Won;
            Caption(content, "VARDİYA SONU RAPORU", 24, 24, 676, 22);
            var title = Heading(content, won ? "Görev tamamlandı." : "Kontrol kaybedildi.", 24, 72, 676, 64, 36);
            title.color = won ? Teal : Danger;
            Label(content, session.EndReason, 24, 149, 676, 75, 22);
            var first = Panel(content, "Handled", 24, 256, 326, 115, Raised);
            var second = Panel(content, "Missed", 374, 256, 326, 115, Raised);
            Heading(first, crises.HandledCount.ToString(), 20, 12, 286, 54, 42).color = Teal;
            Caption(first, "MÜDAHALE EDİLEN KRİZ", 20, 78, 286, 20);
            Heading(second, crises.MissedCount.ToString(), 20, 12, 286, 54, 42).color = Amber;
            Caption(second, "KAÇIRILAN / MÜDAHALESİZ", 20, 78, 286, 20);
            Label(content, $"Son durum: Halk {session.Happiness}/100   /   İklim {session.Climate}/100", 24, 402, 676, 34, 21);
            var restart = Button(content, "YENİ VARDİYA BAŞLAT  ›", 24, 478, 676, 62, () =>
            {
                session.RestartSession();
                viewKey = null;
            });
            restart.GetComponent<Image>().color = new Color(.08f,.35f,.31f);
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
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
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
            label.supportRichText = false;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Button Button(Transform parent, string text, float x, float y, float w, float h, Action action)
        {
            var rect = Panel(parent, "Button", x, y, w, h, Raised);
            rect.GetComponent<Image>().raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(.75f, 1f, .95f);
            colors.pressedColor = new Color(.5f, .8f, .75f);
            colors.disabledColor = new Color(.5f, .56f, .62f, .7f);
            colors.fadeDuration = .12f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => action());
            var label = Label(rect, text, 6, 4, w - 12, h - 8, 20);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private Text Heading(Transform parent, string text, float x, float y, float w, float h, int size)
        {
            var label = Label(parent, text, x, y, w, h, size);
            label.fontStyle = FontStyle.Bold;
            return label;
        }

        private Text Caption(Transform parent, string text, float x, float y, float w, float h)
        {
            var label = Label(parent, text, x, y, w, h, 13);
            label.color = Muted;
            return label;
        }

        private void Chip(Transform parent, string text, float x, float y, float width, Color color)
        {
            var chip = Panel(parent, "Badge", x, y, width, 28,
                new Color(color.r * .16f, color.g * .16f, color.b * .16f, 1f));
            var label = Label(chip, text, 5, 4, width - 10, 20, 13);
            label.color = color;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
        }

        private Image Bar(Transform parent, float x, float y, float width, float height, Color color)
        {
            var track = Panel(parent, "GaugeTrack", x, y, width, height, Raised);
            var fill = Panel(track, "GaugeFill", 0, 0, width, height, color);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.sizeDelta = Vector2.zero;
            fill.anchoredPosition = Vector2.zero;
            // Kritik eşik, renk dışında küçük bir işaretle de belirtilir.
            if (height >= 10) Panel(track, "CriticalThreshold", width * .25f, -3, 1, height + 6, Muted);
            return fill.GetComponent<Image>();
        }

        private static void SetBar(Image image, float value, Color color)
        {
            image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1);
            image.color = color;
        }

        private static Color TypeColor(CrisisType type)
        {
            switch (type)
            {
                case CrisisType.Flood: return new Color32(112, 181, 255, 255);
                case CrisisType.Drought: return Amber;
                case CrisisType.Wildfire: return new Color32(255, 145, 92, 255);
                default: return new Color32(237, 141, 175, 255);
            }
        }

        private static Sprite CreateShape(bool circle, out Texture2D texture)
        {
            // Küçük UI şekilleri bir kez üretilip bütün kartlarda paylaşılır.
            const int size = 64;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - 31.5f);
                float dy = Mathf.Abs(y - 31.5f);
                float distance = circle ? Mathf.Sqrt(dx * dx + dy * dy) - 31f :
                    Mathf.Sqrt(Mathf.Pow(Mathf.Max(dx - 18f, 0f), 2f) +
                    Mathf.Pow(Mathf.Max(dy - 18f, 0f), 2f)) - 13f;
                pixels[y * size + x] = new Color32(255, 255, 255,
                    (byte)(Mathf.Clamp01(.5f - distance) * 255));
            }
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = circle ? "MarkerShape" : "PanelShape";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new UnityEngine.Rect(0, 0, size, size),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect,
                circle ? Vector4.zero : new Vector4(14, 14, 14, 14));
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
            if (roundedSprite != null) Destroy(roundedSprite);
            if (circleSprite != null) Destroy(circleSprite);
            if (shapeTexture != null) Destroy(shapeTexture);
            if (circleTexture != null) Destroy(circleTexture);
        }
    }
}
