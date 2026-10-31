using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Pixamor
{
    // Yalnızca sunum katmanı. Sayaçlar, para ve kriz kararları kendi yöneticilerinde kalır.
    [RequireComponent(typeof(GameSession), typeof(CrisisManager))]
    public partial class MonitorPrototype : MonoBehaviour
    {
        [SerializeField] private Camera operatorCamera;
        [SerializeField] private Transform screenMount;
        [SerializeField] private FirstPersonOperator player;
        private GameSession session;
        private CrisisManager crises;
        private CommandUI ui;
        private GameObject screenRoot;
        private RectTransform content;
        private CanvasGroup contentFade;
        private GraphicRaycaster raycaster;
        private Text budget, income, people, climate, clock, clockState, status, news;
        private Image peopleBar, climateBar, clockBar;
        private Image statusLight;
        private string currentView;
        private readonly List<System.Action> refreshers = new List<System.Action>();
        private Texture2D worldMap;
        private bool ownsFallbackMap;

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
                Debug.LogError("Monitör bağlantıları eksik. İklim Operatörü > Odayı Kur menüsünü kullan.", this);
                enabled = false;
                return;
            }
            session = GetComponent<GameSession>();
            crises = GetComponent<CrisisManager>();
            if (GetComponent<MonitorFeedback>() == null) gameObject.AddComponent<MonitorFeedback>();
            // Eski test panelleri bu ekranın üstüne çizilmesin.
            var debug = GetComponent<SessionDebugPanel>();
            if (debug != null) debug.enabled = false;
            var oldPanel = GetComponent<CrisisPrototypePanel>();
            if (oldPanel != null) oldPanel.enabled = false;
            ui = new CommandUI();
            worldMap = Resources.Load<Texture2D>("CommandUI/Art/WorldMap");
            if (worldMap == null)
            {
                worldMap = CrisisMapTexture.Create();
                ownsFallbackMap = true;
            }
            BuildScreen();
            EnsureInput();
        }

        private void BuildScreen()
        {
            screenRoot = new GameObject("MonitorScreen", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = screenRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = operatorCamera;
            raycaster = screenRoot.GetComponent<GraphicRaycaster>();
            var root = screenRoot.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(1600,1040);
            root.SetPositionAndRotation(screenMount.position,screenMount.rotation);
            // Fiziksel ekran genişliği 5.5 metre olarak korunur. Yalnızca içerik yenilenir.
            root.localScale = Vector3.one * (5.5f / 1600f);
            ui.Panel(root,"Background",0,0,1600,1040,CommandUI.Background);
            ui.Panel(root,"BrandMark",24,28,60,60,CommandUI.Surface,true);
            ui.Icon(root,"Globe",33,37,42,CommandUI.Teal);
            ui.Text(root,"İKLİM OPERATÖRÜ",104,21,780,60,48,CommandUI.White,"display");
            ui.Caption(root,"KÜRESEL KRİZ KOORDİNASYON MERKEZİ",106,84,820);
            var badge = ui.Panel(root,"SystemStatus",1032,27,544,76,CommandUI.Surface,true);
            statusLight = ui.Panel(badge,"OnlineLight",20,32,10,10,CommandUI.Teal).GetComponent<Image>();
            status = ui.Text(badge,"",48,13,478,54,25,CommandUI.Teal,"strong");
            status.alignment = TextAnchor.MiddleLeft;
            BuildMetrics(root);
            content = ui.Rect(root,"OperationWorkspace",24,292,1552,676);
            contentFade = content.gameObject.AddComponent<CanvasGroup>();
            ui.Caption(root,"OPERATÖR TERMİNALİ   /   01",26,998,700);
            var help = ui.Caption(root,"FARE  SEÇ     /     E  AYRIL     /     ESC  İMLECİ BIRAK",720,998,856);
            help.alignment = TextAnchor.MiddleRight;
        }

        private RectTransform Metric(Transform root,string title,string icon,float x,Color color)
        {
            var panel = ui.Panel(root,title,x,128,376,144,CommandUI.Surface,true);
            ui.Icon(panel,icon,20,18,30,color);
            ui.Caption(panel,title,62,18,294);
            return panel;
        }

        private void BuildMetrics(Transform root)
        {
            var wallet = Metric(root,"MÜDAHALE BÜTÇESİ","Budget",24,CommandUI.Amber);
            budget = ui.Number(wallet,"60",20,48,160,80,74);
            income = ui.Text(wallet,"",174,84,182,30,23,CommandUI.Muted);
            income.alignment = TextAnchor.MiddleRight;

            var population = Metric(root,"HALK MUTLULUĞU","People",416,CommandUI.Teal);
            people = ui.Number(population,"70",20,48,155,80,74);
            ui.Text(population,"/ 100",172,87,120,30,25,CommandUI.Muted,"mono");
            peopleBar = ui.Bar(population,20,126,336,6,CommandUI.Teal,true);

            var environment = Metric(root,"İKLİM DENGESİ","Climate",808,CommandUI.Blue);
            climate = ui.Number(environment,"70",20,48,155,80,74);
            ui.Text(environment,"/ 100",172,87,120,30,25,CommandUI.Muted,"mono");
            climateBar = ui.Bar(environment,20,126,336,6,CommandUI.Blue,true);

            var time = Metric(root,"VARDİYA SÜRESİ","Clock",1200,CommandUI.Teal);
            clock = ui.Number(time,"06:00",20,48,228,80,72);
            clockState = ui.Text(time,"CANLI",240,91,116,25,21,CommandUI.Teal,"strong");
            clockState.alignment = TextAnchor.MiddleRight;
            clockBar = ui.Bar(time,20,126,336,6,CommandUI.Teal);
        }

        private void EnsureInput()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var input = new GameObject("MonitorEventSystem",typeof(EventSystem));
            input.transform.SetParent(screenRoot.transform,false);
            input.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void Update()
        {
            if (screenRoot == null) return;
            raycaster.enabled = player.IsUsingMonitor;
            budget.text = session.Money.ToString();
            income.text = session.State == SessionState.Running
                ? $"+{session.IncomeAmount} / {Mathf.CeilToInt(session.TimeUntilIncome)} sn" : "VARDİYA BİTTİ";
            people.text = session.Happiness.ToString();
            climate.text = session.Climate.ToString();
            people.color = session.HasHappinessWarning ? CommandUI.Red : CommandUI.White;
            climate.color = session.HasClimateWarning ? CommandUI.Red : CommandUI.White;
            CommandUI.SetBar(peopleBar,session.Happiness/100f,session.HasHappinessWarning ? CommandUI.Red : CommandUI.Teal);
            CommandUI.SetBar(climateBar,session.Climate/100f,session.HasClimateWarning ? CommandUI.Red : CommandUI.Blue);
            int seconds = Mathf.CeilToInt(session.RemainingTime);
            clock.text = $"{seconds/60:00}:{seconds%60:00}";
            clockState.text = session.State != SessionState.Running ? "SONLANDI" : session.IsPaused ? "DURDU" : "CANLI";
            clockState.color = session.IsPaused ? CommandUI.Amber : CommandUI.Teal;
            CommandUI.SetBar(clockBar,session.RemainingTime/Mathf.Max(1,session.ElapsedTime+session.RemainingTime),clockState.color);
            RefreshStatus();

            string view = session.State != SessionState.Running ? "end:"+session.State :
                crises.OpenCard != null ? "card:"+crises.OpenCard.Id : "operations";
            if (view == "operations") foreach(var crisis in crises.Active) view += ":"+crisis.Id;
            if (currentView != view)
            {
                currentView = view;
                ClearContent();
                if(session.State != SessionState.Running) BuildReport();
                else if(crises.OpenCard != null) BuildDecision();
                else BuildOperations();
            }
            if (news != null)
                news.text = crises.HandledCount + crises.MissedCount == 0 && crises.Active.Count > 0
                    ? crises.Active[0].Data.Region + " bölgesinden yeni kriz bildirimi alındı. Müdahale bekleniyor."
                    : crises.LastResult;
            foreach(var update in refreshers) update();
            contentFade.alpha = Mathf.MoveTowards(contentFade.alpha,1f,Time.unscaledDeltaTime*6f);
            contentFade.blocksRaycasts = contentFade.alpha >= .95f;
        }

        private void RefreshStatus()
        {
            bool running = session.State == SessionState.Running;
            bool critical = session.HasHappinessWarning || session.HasClimateWarning;
            status.color = critical && running ? CommandUI.Red : session.IsPaused ? CommandUI.Amber : CommandUI.Teal;
            if (session.State == SessionState.Lost) status.color = CommandUI.Red;
            statusLight.color = status.color;
            status.text = !running ? "VARDİYA RAPORU HAZIR" : critical
                ? session.HasHappinessWarning && session.HasClimateWarning ? "KRİTİK / HALK VE İKLİM" :
                    session.HasHappinessWarning ? "KRİTİK / HALK PROTESTOLARI" : "KRİTİK / İKLİM DENGESİ"
                : session.IsPaused ? "KARAR MODU / ZAMAN DURDU"
                : crises.Active.Count > 0 ? $"{crises.Active.Count} AKTİF KRİZ / MÜDAHALE BEKLİYOR" : "CANLI / TÜM BÖLGELER İZLENİYOR";
        }

        private void ClearContent()
        {
            refreshers.Clear();
            news = null;
            foreach(Transform child in content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            contentFade.alpha = .45f;
            contentFade.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            if(screenRoot != null) Destroy(screenRoot);
            if(ownsFallbackMap && worldMap != null) Destroy(worldMap);
            ui?.Dispose();
        }
    }
}
