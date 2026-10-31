using TMPro;
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
        [SerializeField] private Vector2 physicalScreenSize = new Vector2(5.5f, 3.575f);
        [SerializeField] private Renderer[] staticScreenPreviews;
        private GameSession session;
        private CrisisManager crises;
        private GameFlowController flow;
        private CommandUI ui;
        private GameObject screenRoot;
        private RectTransform content;
        private CanvasGroup contentFade;
        private GraphicRaycaster raycaster;
        private TMP_Text budget, income, people, climate, clock, clockState, status, news;
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

        public void ConfigureSurface(Vector2 size, Renderer[] previews)
        {
            physicalScreenSize = size;
            staticScreenPreviews = previews;
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
            // Editör önizlemesinin yerini çalışan, tıklanabilir oyun arayüzü alır.
            if (staticScreenPreviews != null)
                foreach (var preview in staticScreenPreviews) if (preview != null) preview.enabled = false;
            EnsureInput();
            flow = GetComponent<GameFlowController>();
            if (flow == null) flow = gameObject.AddComponent<GameFlowController>();
            flow.Initialize(session,crises,player,worldMap);
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
            // Her sahne kendi fiziksel ekran ölçüsünü kullanır; ana odanın varsayılanı korunur.
            root.localScale = new Vector3(physicalScreenSize.x / 1600f, physicalScreenSize.y / 1040f, .001f);
            ui.Panel(root,"Background",0,0,1600,1040,CommandUI.Background);

            // Harita artık bir panelin içinde değil, monitörün tamamının arka planıdır.
            var mapImage = ui.Rect(root,"FullScreenWorldMap",0,0,1600,1040).gameObject.AddComponent<RawImage>();
            mapImage.texture = worldMap;
            mapImage.raycastTarget = false;
            content = ui.Rect(root,"OperationWorkspace",0,0,1600,1040);
            contentFade = content.gameObject.AddComponent<CanvasGroup>();

            ui.Icon(root,"Globe",36,31,46,CommandUI.Teal);
            ui.Text(root,"İKLİM OPERATÖRÜ",100,21,780,64,48,CommandUI.White,"display");
            ui.Caption(root,"KÜRESEL KRİZ KOORDİNASYON MERKEZİ",101,84,850);
            statusLight = ui.Panel(root,"OnlineLight",38,132,10,10,CommandUI.Teal).GetComponent<Image>();
            status = ui.Text(root,"",62,117,1050,46,25,CommandUI.Teal,"strong");
            status.alignment = TextAlignmentOptions.MidlineLeft;
            BuildMetrics(root);
            var pauseButton = ui.Button(root,"PauseShortcut",695,30,500,48,()=>flow?.Pause(),new Color(0,0,0,.15f));
            pauseButton.GetComponent<Outline>().enabled=false;
            var help = ui.Caption(pauseButton.transform,"ESC  DURAKLAT  /  E  AYRIL",0,8,500);
            help.alignment = TextAlignmentOptions.Center;
        }

        private RectTransform Metric(Transform root,string title,string icon,float x,Color color)
        {
            var panel = ui.Panel(root,title,x,858,300,164,new Color(.025f,.06f,.09f,.35f));
            ui.Icon(panel,icon,20,18,30,color);
            ui.Caption(panel,title,62,18,218);
            return panel;
        }

        private void BuildMetrics(Transform root)
        {
            var wallet = Metric(root,"BÜTÇE","Budget",36,CommandUI.Amber);
            budget = ui.Number(wallet,"60",20,48,160,80,74);
            income = ui.Text(wallet,"",20,129,260,30,23,CommandUI.Muted);

            var population = Metric(root,"HALK MUTLULUĞU","People",376,CommandUI.Teal);
            people = ui.Number(population,"70",20,48,155,80,74);
            ui.Text(population,"/ 100",172,87,120,30,25,CommandUI.Muted,"mono");
            peopleBar = ui.Bar(population,20,142,260,4,CommandUI.Teal,true);

            var environment = Metric(root,"İKLİM DENGESİ","Climate",716,CommandUI.Blue);
            climate = ui.Number(environment,"70",20,48,155,80,74);
            ui.Text(environment,"/ 100",172,87,120,30,25,CommandUI.Muted,"mono");
            climateBar = ui.Bar(environment,20,142,260,4,CommandUI.Blue,true);

            var time = ui.Rect(root,"ShiftClock",1240,24,324,152);
            var timeTitle = ui.Caption(time,"VARDİYA SÜRESİ",0,0,324);
            timeTitle.alignment = TextAlignmentOptions.MidlineRight;
            clock = ui.Number(time,"06:00",0,28,324,100,90);
            clock.alignment = TextAlignmentOptions.TopRight;
            clockState = ui.Text(time,"CANLI",0,126,324,26,21,CommandUI.Teal,"strong");
            clockState.alignment = TextAlignmentOptions.MidlineRight;
            clockBar = ui.Bar(time,0,119,324,3,CommandUI.Teal);
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
            raycaster.enabled = player.IsUsingMonitor && !player.UIBlocked && !player.IsChangingView;
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
                crises.OpenCard != null ? "card:"+crises.OpenCard.Id+":"+session.Money+":"+session.Happiness+":"+session.Climate+":"+crises.Active.Count : "operations";
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
                : session.IsPaused ? "VARDİYA DURAKLATILDI"
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
