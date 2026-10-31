using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// Kriz Soru-Cevap Arayüzü ve "Kör Seçenek" (Blind Choice) Paneli:
    /// - Oyuncu yanıp sönen kıtaya tıkladığında ekrana gelir.
    /// - Kıtanın temsilcisini, mevcut Sinir Seviyesini ve 6 Sorun Barını gösterir:
    ///   1. Ağaç / Orman Varlığı
    ///   2. Fabrika & Sanayi Emisyonu
    ///   3. Çevre & Su Kirliliği
    ///   4. Yenilenebilir Enerji Payı
    ///   5. Gıda & Su Güvenliği
    ///   6. Sıfır Atık / Geri Dönüşüm
    /// - Oyuncuya 3 Tercih Butonu sunar.
    /// - KESİN KURAL: Butonlar üzerinde KESİNLİKLE hiçbir sayısal etki gösterilmez (+/- para, sinir, iklim ipucu YASAKTIR).
    ///   Sadece ve sadece kararın diplomatik metni yer alır.
    /// </summary>
    public class BlindChoiceDialogueUI : MonoBehaviour
    {
        public static BlindChoiceDialogueUI Instance { get; private set; }

        [Header("Ana Kök ve Çerçeve")]
        public GameObject rootObject;

        [Header("Kıta ve Temsilci Bilgisi")]
        public Text continentTitleText;
        public Text representativeText;
        public Text angerText;
        public UIBar angerBar;

        [Header("6 Çevre Sorun Barı")]
        public Text treeText;
        public UIBar treeBar;

        public Text factoryText;
        public UIBar factoryBar;

        public Text pollutionText;
        public UIBar pollutionBar;

        public Text renewableText;
        public UIBar renewableBar;

        public Text foodText;
        public UIBar foodBar;

        public Text zeroWasteText;
        public UIBar zeroWasteBar;

        [Header("Kriz Sorusu")]
        public Text crisisTitleText;
        public Text timerText;
        public Text descriptionText;

        [Header("3 Kör Seçenek Butonu (Etki Gösterimi Yasak!)")]
        public PressButton[] choiceButtons = new PressButton[3];
        public Text[] choiceTexts = new Text[3];
        public Image[] choiceBgImages = new Image[3];

        [Header("Kapat Butonu")]
        public PressButton closeButton;

        WorldMapMonitorController.ContinentState currentContinent;

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
                        choiceButtons[i].Pressed += () => OnChoiceSelected(idx);
                }
            }

            if (closeButton != null)
                closeButton.Pressed += Close;

            Close();
        }

        void Update()
        {
            if (currentContinent == null || rootObject == null || !rootObject.activeSelf) return;

            if (currentContinent.inCrisis && timerText != null)
            {
                timerText.text = $"Kalan Müdahale Süresi: <color=#FF4444><b>{Mathf.Max(0f, currentContinent.crisisRemainingTime):0.0}s</b></color>";

                if (currentContinent.crisisRemainingTime <= 0f)
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// Krizdeki kıtanın diplomatik dosyasını açar.
        /// </summary>
        public void OpenForContinent(WorldMapMonitorController.ContinentState cont)
        {
            currentContinent = cont;
            if (cont == null || cont.activeQuestion == null) return;

            if (rootObject != null) rootObject.SetActive(true);

            // 1. Kıta ve Temsilci Bilgisi
            if (continentTitleText != null)
                continentTitleText.text = $"KITA: <color=#00FFFF><b>{cont.name.ToUpper()}</b></color> (İklim: %{cont.health:0})";

            if (representativeText != null)
                representativeText.text = $"TEMSİLCİ: <b>{cont.representativeTitle}</b>";

            // 2. Sinir Seviyesi Barı
            if (angerText != null)
                angerText.text = $"SİNİR SEVİYESİ: %{cont.anger:0}";

            angerBar?.Set(cont.anger / 100f);

            // 3. 6 Çevre Sorun Barı
            UpdateProblemBars(cont);

            // 4. Kriz Başlığı ve Brifing
            var q = cont.activeQuestion;
            if (crisisTitleText != null)
                crisisTitleText.text = $"<color=#FFCC00><b>{q.title.ToUpper()}</b></color>";

            if (descriptionText != null)
                descriptionText.text = q.description;

            // 5. 3 Kör Tercih Butonu (KESİN KURAL: ETKİ GÖSTERİMİ YASAK!)
            for (int i = 0; i < 3; i++)
            {
                if (i >= q.choices.Length) continue;
                var ch = q.choices[i];

                if (choiceTexts != null && i < choiceTexts.Length && choiceTexts[i] != null)
                {
                    // YALNIZCA diplomatik karar metni yazılır! Sayısal etki, ikon, $+/- vb. ASLA yazılmaz!
                    choiceTexts[i].text = $"<b>[{i + 1}]</b> {ch.decisionText}";
                }
            }

            Sfx.Play(SfxType.Click);
        }

        void UpdateProblemBars(WorldMapMonitorController.ContinentState cont)
        {
            if (treeText != null) treeText.text = $"Ağaç: %{cont.tree:0}";
            treeBar?.Set(cont.tree / 100f);

            if (factoryText != null) factoryText.text = $"Fabrika: %{cont.factory:0}";
            factoryBar?.Set(cont.factory / 100f);

            if (pollutionText != null) pollutionText.text = $"Kirlilik: %{cont.pollution:0}";
            pollutionBar?.Set(cont.pollution / 100f);

            if (renewableText != null) renewableText.text = $"Yenilenebilir: %{cont.renewable:0}";
            renewableBar?.Set(cont.renewable / 100f);

            if (foodText != null) foodText.text = $"Gıda: %{cont.food:0}";
            foodBar?.Set(cont.food / 100f);

            if (zeroWasteText != null) zeroWasteText.text = $"Sıfır Atık: %{cont.zeroWaste:0}";
            zeroWasteBar?.Set(cont.zeroWaste / 100f);
        }

        void OnChoiceSelected(int index)
        {
            if (currentContinent == null || currentContinent.activeQuestion == null) return;
            if (index < 0 || index >= currentContinent.activeQuestion.choices.Length) return;

            var chosen = currentContinent.activeQuestion.choices[index];

            // Kararı spawner üzerinden uygula
            ContinentCrisisSpawner.Instance?.ResolveCrisis(currentContinent.type, chosen);

            Close();
        }

        public void Close()
        {
            if (rootObject != null) rootObject.SetActive(false);
            currentContinent = null;
        }
    }
}
