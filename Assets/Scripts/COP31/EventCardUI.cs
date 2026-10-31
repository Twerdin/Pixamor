using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// Haritada bir kriz pinine tıklandığında açılan Olay Kartı (Event Dossier).
    /// message.txt belgesindeki başlık, durum mesajı ve 3 tercihin net etkilerini gösterir.
    /// </summary>
    public class EventCardUI : MonoBehaviour
    {
        public static EventCardUI Instance { get; private set; }

        [Header("UI Elemanları")]
        public GameObject cardRoot;
        public Text titleText;
        public Text locationText;
        public Text timerText;
        public Text descriptionText;

        [Header("3 Seçenek Butonları")]
        public PressButton[] choiceButtons = new PressButton[3];
        public Text[] choiceTitles = new Text[3];
        public Text[] choiceStats = new Text[3];
        public Image[] choiceBgImages = new Image[3];

        [Header("Kapat Butonu")]
        public PressButton closeButton;

        CrisisEvent currentEvent;

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
            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    int idx = i;
                    if (choiceButtons[i] != null)
                        choiceButtons[i].Pressed += () => OnChoiceClicked(idx);
                }
            }

            if (closeButton != null)
                closeButton.Pressed += Close;

            Close();
        }

        public void Open(CrisisEvent data, float remainingTime)
        {
            currentEvent = data;
            if (cardRoot != null) cardRoot.SetActive(true);

            if (titleText != null) titleText.text = $"<b>{data.title.ToUpper()}</b>";
            if (locationText != null) locationText.text = $"Bölge: <color=#00FFFF>{data.locationName}</color>";
            if (descriptionText != null) descriptionText.text = data.description;

            UpdateTimer(remainingTime);
            RefreshChoices();
        }

        public void UpdateTimer(float remainingTime)
        {
            if (timerText != null && currentEvent != null)
            {
                timerText.text = $"Müdahale Süresi: <color=#FF4444><b>{remainingTime:0.0}s</b></color>";
            }
        }

        public void RefreshChoices()
        {
            if (currentEvent == null) return;
            var mgr = ClimateOperatorManager.Instance;
            int currentMoney = mgr != null ? mgr.Money : 0;

            for (int i = 0; i < 3; i++)
            {
                if (i >= currentEvent.choices.Length) continue;
                var ch = currentEvent.choices[i];

                if (choiceTitles != null && i < choiceTitles.Length && choiceTitles[i] != null)
                    choiceTitles[i].text = $"<b>[{i + 1}] {ch.title}</b>";

                if (choiceStats != null && i < choiceStats.Length && choiceStats[i] != null)
                {
                    string costStr = ch.moneyDelta != 0 ? $"{ch.moneyDelta}$" : "0$";
                    string halkStr = (ch.halkDelta >= 0 ? "+" : "") + ch.halkDelta;
                    string iklimStr = (ch.iklimDelta >= 0 ? "+" : "") + ch.iklimDelta;

                    choiceStats[i].text = $"Para: <b>{costStr}</b>  |  Halk: <b>{halkStr}</b>  |  İklim: <b>{iklimStr}</b>";
                }

                // Bütçe yetersizse pahalı seçenek kilitlenir
                bool canAfford = (currentMoney + ch.moneyDelta >= 0) || (ch.moneyDelta >= 0);
                if (choiceButtons != null && i < choiceButtons.Length && choiceButtons[i] != null)
                {
                    choiceButtons[i].isInteractable = canAfford;
                }

                if (choiceBgImages != null && i < choiceBgImages.Length && choiceBgImages[i] != null)
                {
                    choiceBgImages[i].color = canAfford
                        ? new Color(0.12f, 0.16f, 0.22f, 0.95f)
                        : new Color(0.15f, 0.08f, 0.08f, 0.65f);
                }
            }
        }

        void OnChoiceClicked(int index)
        {
            if (currentEvent == null) return;
            var mgr = ClimateOperatorManager.Instance;
            if (mgr != null)
            {
                mgr.ApplyChoice(currentEvent, index);
            }
            Close();
        }

        public void Close()
        {
            currentEvent = null;
            if (cardRoot != null) cardRoot.SetActive(false);
        }

        public bool IsOpen => currentEvent != null;
        public int CurrentEventId => currentEvent != null ? currentEvent.id : -1;
    }
}
