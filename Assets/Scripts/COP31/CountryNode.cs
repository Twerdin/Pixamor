using UnityEngine;
using UnityEngine.UI;

namespace Pixamor.COP31
{
    /// <summary>
    /// Dev ekrandaki ülke kutucuğu: 6 çevre sorun barı, başkan sinir seviyesi,
    /// kırmızı yanıp sönen alarm durumu ve tıklama (müzakere açma) etkileşimi.
    /// </summary>
    public class CountryNode : Interactable
    {
        public string CountryName { get; private set; }
        public string PresidentName { get; private set; }
        public float Anger { get; private set; }
        public bool InCrisis { get; private set; }
        public ProblemType CrisisProblem { get; private set; }
        public string CrisisMessage { get; private set; } = "";
        public bool EmergencyActive { get; set; }
        public int WithdrawCount { get; private set; }

        readonly float[] bars = new float[ProblemInfo.Count];

        Material tileMaterial;
        Text nameText;
        Text infoText;
        float flashTimer;
        Color flashColor;
        float labelTimer;

        static readonly Color CrisisDark = new Color(0.35f, 0.02f, 0.02f);
        static readonly Color CrisisBright = new Color(1f, 0.1f, 0.08f);
        static readonly Color Healthy = new Color(0.05f, 0.42f, 0.28f);
        static readonly Color Unhealthy = new Color(0.32f, 0.26f, 0.08f);
        static readonly Color SuccessGreen = new Color(0.1f, 1f, 0.35f);
        static readonly Color FailOrange = new Color(1f, 0.55f, 0f);
        static readonly Color WithdrawPurple = new Color(0.35f, 0.05f, 0.45f);

        public void Init(string country, string president, float anger, float[] startBars,
            Renderer tile, Text nameLabel, Text infoLabel)
        {
            CountryName = country;
            PresidentName = president;
            Anger = Mathf.Clamp(anger, 0f, 100f);
            for (int i = 0; i < ProblemInfo.Count; i++)
                bars[i] = Mathf.Clamp(startBars != null && i < startBars.Length ? startBars[i] : 30f, 0f, 100f);
            tileMaterial = tile != null ? tile.sharedMaterial : null;
            nameText = nameLabel;
            infoText = infoLabel;
            RefreshLabel();
        }

        // ------------------------------------------------------------ Data

        public float GetBar(ProblemType p) => bars[(int)p];

        public void ModifyBar(ProblemType p, float delta) =>
            bars[(int)p] = Mathf.Clamp(bars[(int)p] + delta, 0f, 100f);

        public void SetAnger(float value) => Anger = Mathf.Clamp(value, 0f, 100f);

        public float AverageBar
        {
            get
            {
                float s = 0f;
                for (int i = 0; i < bars.Length; i++) s += bars[i];
                return s / bars.Length;
            }
        }

        public ProblemType WorstProblem
        {
            get
            {
                int worst = 0;
                for (int i = 1; i < bars.Length; i++)
                    if (bars[i] > bars[worst]) worst = i;
                return (ProblemType)worst;
            }
        }

        public void StartCrisis(ProblemType p, string message)
        {
            InCrisis = true;
            CrisisProblem = p;
            CrisisMessage = message;
            labelTimer = 0f;
        }

        public void ResolveCrisis()
        {
            InCrisis = false;
            CrisisMessage = "";
            Flash(SuccessGreen, 1.5f);
        }

        public void FlashFail() => Flash(FailOrange, 0.8f);

        /// <summary>Başkan anlaşmadan çekildi: kriz silinir, sorunlar artar, sinir sıfırlanmaz ama düşer.</summary>
        public void Withdraw()
        {
            WithdrawCount++;
            InCrisis = false;
            CrisisMessage = "";
            Anger = 50f;
            for (int i = 0; i < bars.Length; i++) bars[i] = Mathf.Clamp(bars[i] + 8f, 0f, 100f);
            Flash(WithdrawPurple, 2.5f);
        }

        void Flash(Color c, float seconds)
        {
            flashColor = c;
            flashTimer = seconds;
            labelTimer = 0f;
        }

        // ------------------------------------------------------------ Interaction

        public override string HoverLabel =>
            InCrisis ? $"{CountryName} — KRİZ! (Tıkla: Müzakere)" : $"{CountryName} — Detayları gör";

        public override void OnPressStart()
        {
            if (!isInteractable) return;
            var dialogue = DialogueAndChoiceSystem.Instance;
            if (dialogue != null) dialogue.OpenCountry(this);
        }

        // ------------------------------------------------------------ Visuals

        void Update()
        {
            Color c;
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                c = flashColor;
            }
            else if (InCrisis)
            {
                float k = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                c = Color.Lerp(CrisisDark, CrisisBright, k);
            }
            else
            {
                float health = Mathf.InverseLerp(60f, 12f, AverageBar);
                c = Color.Lerp(Unhealthy, Healthy, health);
            }
            Greybox.SetColor(tileMaterial, c);

            labelTimer -= Time.deltaTime;
            if (labelTimer <= 0f)
            {
                labelTimer = 0.2f;
                RefreshLabel();
            }
        }

        void RefreshLabel()
        {
            if (nameText != null)
                nameText.text = EmergencyActive ? $"{CountryName}  <color=#FF4040>!!! ACİL</color>" : CountryName;

            if (infoText == null) return;
            var worst = WorstProblem;
            string status = InCrisis
                ? $"<color=#FFD24D>KRİZ: {ProblemInfo.Name(CrisisProblem)}</color>"
                : "Durum: Stabil";
            infoText.text =
                $"Başkan: {PresidentName}\n" +
                $"Sinir: %{Anger:0}   |   En kötü: {ProblemInfo.ShortName(worst)} %{GetBar(worst):0}\n" +
                status;
        }
    }
}
