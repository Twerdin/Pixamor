using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pixamor
{
    public enum GameScreen { MainMenu, Playing, Paused, Help, Settings, Results }

    // Menü akışı dünya ekranından ayrıdır. Menü açıkken kart kapanması zamanı başlatamaz.
    [DefaultExecutionOrder(-200)]
    public partial class GameFlowController : MonoBehaviour
    {
        public GameScreen Screen { get; private set; } = GameScreen.MainMenu;
        public bool BlocksControls => Screen != GameScreen.Playing;
        public bool AnimationsEnabled { get; private set; } = true;
        private GameSession session;
        private CrisisManager crises;
        private FirstPersonOperator player;
        private CommandUI ui;
        private GameObject overlay;
        private RectTransform body;
        private TMP_Text subtitle;
        private CanvasGroup fade;
        private RawImage backdrop;
        private GameScreen returnScreen;
        private float oldAudioVolume;
        private bool initialized;
        private GameDifficulty selectedDifficulty;

        public void Initialize(GameSession game,CrisisManager events,FirstPersonOperator controller,Texture2D map)
        {
            if (initialized) return;
            initialized = true;
            session = game;
            crises = events;
            player = controller;
            player.ConfigureFlow(this);
            oldAudioVolume = AudioListener.volume;
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Pixamor.MasterVolume",.75f));
            player.MouseSensitivity = PlayerPrefs.GetFloat("Pixamor.MouseSensitivity",.10f);
            AnimationsEnabled = PlayerPrefs.GetInt("Pixamor.Animations",1) != 0;
            selectedDifficulty = PlayerPrefs.GetInt("Pixamor.Difficulty",0)==1 ? GameDifficulty.Hard : GameDifficulty.Easy;
            GameLanguage.Current = PlayerPrefs.GetInt("Pixamor.Language",0)==1 ? UiLanguage.English : UiLanguage.Turkish;
            ui = new CommandUI();
            BuildOverlay(map);
            session.Ended += OnSessionEnded;
            Show(GameScreen.MainMenu);
        }

        private void BuildOverlay(Texture2D map)
        {
            overlay = new GameObject("GameMenus",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600,1040);
            // Bütün menü 4:3 ve geniş ekranlarda kesilmeden sığar; arka plan ekranı kaplar.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            backdrop = ui.Rect(overlay.transform,"WorldBackdrop",0,0,1600,1040).gameObject.AddComponent<RawImage>();
            Stretch(backdrop.rectTransform);
            backdrop.texture = map;
            backdrop.color = new Color(.65f,.78f,.86f,1);
            backdrop.raycastTarget = false;
            var shade = ui.Panel(overlay.transform,"MenuShade",0,0,1600,1040,new Color(.015f,.03f,.05f,.76f));
            Stretch(shade);
            // Bu yüzey alttaki fiziksel monitöre yanlışlıkla tıklamayı engeller.
            shade.GetComponent<Image>().raycastTarget = true;
            var stage = ui.Rect(overlay.transform,"MenuStage",0,0,1600,1040);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(.5f,.5f);
            stage.anchoredPosition = Vector2.zero;
            ui.Icon(stage,"Globe",70,48,60,CommandUI.Teal);
            ui.Text(stage,"İKLİM OPERATÖRÜ",152,45,1000,64,48,CommandUI.White,"display");
            subtitle = ui.Caption(stage,"",154,112,1200);
            body = ui.Rect(stage,"MenuBody",0,174,1600,790);
            fade = body.gameObject.AddComponent<CanvasGroup>();
            ui.Caption(stage,"KRİZ KOORDİNASYON MERKEZİ / OPERASYON 01",74,990,1452);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            if (!initialized) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Screen == GameScreen.Playing) Pause();
                else if (Screen == GameScreen.Paused) Resume();
                else if (Screen == GameScreen.Settings || Screen == GameScreen.Help) Back();
            }
            if (BlocksControls)
            {
                fade.alpha = AnimationsEnabled ? Mathf.MoveTowards(fade.alpha,1,Time.unscaledDeltaTime*5f) : 1;
                fade.blocksRaycasts = fade.alpha >= .95f;
                if (AnimationsEnabled)
                {
                    float offset = Mathf.Sin(Time.unscaledTime*.045f)*.012f;
                    backdrop.uvRect = new Rect(.02f+offset,.02f, .96f,.96f);
                }
            }
        }

        private void Show(GameScreen screen)
        {
            Screen = screen;
            session.SetMenuPaused(BlocksControls);
            player.SetUIBlocked(BlocksControls);
            overlay.SetActive(BlocksControls);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (!BlocksControls) return;
            foreach (Transform child in body)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            fade.alpha = AnimationsEnabled ? .5f : 1;
            fade.blocksRaycasts = !AnimationsEnabled;
            switch(screen)
            {
                case GameScreen.MainMenu: BuildMainMenu(); break;
                case GameScreen.Paused: BuildPauseMenu(); break;
                case GameScreen.Help: BuildHelpMenu(); break;
                case GameScreen.Settings: BuildSettingsMenu(); break;
                case GameScreen.Results: BuildResultsMenu(); break;
            }
        }

        private void SelectDifficulty(GameDifficulty difficulty)
        {
            if(Screen!=GameScreen.MainMenu)return;
            selectedDifficulty=difficulty;
            PlayerPrefs.SetInt("Pixamor.Difficulty",(int)difficulty);PlayerPrefs.Save();
            Show(GameScreen.MainMenu);
        }
        private void SelectLanguage(UiLanguage language)
        {
            GameLanguage.Current=language;
            PlayerPrefs.SetInt("Pixamor.Language",(int)language);PlayerPrefs.Save();
            Show(GameScreen.MainMenu);
        }
        public void StartShift()
        {
            session.RestartSession(selectedDifficulty);
            Show(GameScreen.Playing);
            player.ResetForShift();
        }

        public void Pause()
        {
            if (Screen == GameScreen.Playing && session.State == SessionState.Running) Show(GameScreen.Paused);
        }

        public void Resume()
        {
            if (Screen == GameScreen.Paused) Show(GameScreen.Playing);
        }

        public void MainMenu()
        {
            crises.CloseCard();
            session.RestartSession();
            Show(GameScreen.MainMenu);
        }

        private void Open(GameScreen screen)
        {
            returnScreen = Screen;
            Show(screen);
        }

        private void Back()
        {
            SaveSettings();
            Show(returnScreen);
        }

        private void OnSessionEnded(SessionState state) { Show(GameScreen.Results); }

        private void OnApplicationFocus(bool focus)
        {
            if (initialized && !focus && Screen == GameScreen.Playing) Pause();
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("Pixamor.MasterVolume",AudioListener.volume);
            PlayerPrefs.SetFloat("Pixamor.MouseSensitivity",player.MouseSensitivity);
            PlayerPrefs.SetInt("Pixamor.Animations",AnimationsEnabled?1:0);
            PlayerPrefs.Save();
        }

        private void QuitGame()
        {
            SaveSettings();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (!initialized) return;
            session.Ended -= OnSessionEnded;
            if (overlay != null) Destroy(overlay);
            ui?.Dispose();
            AudioListener.volume = oldAudioVolume;
        }
    }
}
