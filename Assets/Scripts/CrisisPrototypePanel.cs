using UnityEngine;

namespace Pixamor
{
    // Geçici monitör arayüzü: harita, kaynaklar ve müdahale kartları.
    [RequireComponent(typeof(GameSession), typeof(CrisisManager))]
    public class CrisisPrototypePanel : MonoBehaviour
    {
        private GameSession session;
        private CrisisManager crises;
        private Texture2D mapTexture;

        private void Awake()
        {
            session = GetComponent<GameSession>();
            crises = GetComponent<CrisisManager>();
            // Haritayı her karede değil, yalnızca bir kez üretiriz.
            mapTexture = CrisisMapTexture.Create();
        }

        private void OnDestroy()
        {
            // Çalışma anında üretilen görselin belleğini serbest bırakırız.
            if (mapTexture != null) Destroy(mapTexture);
        }

        private void OnGUI()
        {
            // Sabit tasarımı Game penceresine sığdır; dar ekranda panel taşmasın.
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

            try
            {
                DrawResources();
                if (session.State != SessionState.Running) DrawEnd();
                else if (crises.OpenCard != null) DrawCard();
                else DrawMap();
            }
            finally
            {
                GUI.enabled = true;
                GUI.matrix = previousMatrix;
            }
        }

        private void DrawResources()
        {
            GUILayout.BeginArea(new Rect(755, 20, 325, 675), GUI.skin.box);
            GUILayout.Label("İKLİM OPERATÖRÜ");
            int seconds = Mathf.CeilToInt(session.RemainingTime);
            GUILayout.Label($"Vardiya: {seconds / 60:00}:{seconds % 60:00}");
            GUILayout.Label($"Para: {session.Money}");
            GUILayout.Label($"Halk: {session.Happiness} / 100");
            GUILayout.Label($"İklim: {session.Climate} / 100");
            GUILayout.Label($"Sonraki bütçe: {Mathf.CeilToInt(session.TimeUntilIncome)} sn");
            GUILayout.Space(12);
            GUILayout.Label($"Müdahale edilen: {crises.HandledCount}");
            GUILayout.Label($"Kaçırılan / müdahale edilmeyen: {crises.MissedCount}");
            if (session.HasHappinessWarning) GUILayout.Label("UYARI: Halk protestoları!");
            if (session.HasClimateWarning) GUILayout.Label("UYARI: İklim kritik!");
            if (session.IsPaused) GUILayout.Label("KART AÇIK — OYUN ZAMANI DURDU");
            GUILayout.Space(20);
            GUILayout.Label("SONUÇ HABERİ");
            GUILayout.Label(crises.LastResult, WrapStyle());
            GUILayout.EndArea();
        }

        private void DrawMap()
        {
            GUI.Box(new Rect(20, 20, 715, 675), GUIContent.none);
            GUI.Label(new Rect(35, 30, 680, 25), "DÜNYA KRİZ MONİTÖRÜ — ŞEMATİK HARİTA");
            GUI.Label(new Rect(35, 55, 680, 25), "İşarete tıkla ve müdahale et. Son 10 saniye: kırmızı uyarı.");

            Rect mapRect = new Rect(35, 90, 680, 360);
            GUI.DrawTexture(mapRect, mapTexture);
            foreach (var crisis in crises.Active)
            {
                float left = crises.SecondsLeft(crisis);
                Vector2 position = crisis.Data.MapPosition;
                // Aynı koordinatlar farklı pencere boyutlarında da haritaya bağlı kalır.
                float x = mapRect.x + Mathf.Clamp01(position.x) * mapRect.width;
                float y = mapRect.y + Mathf.Clamp01(position.y) * mapRect.height;
                Rect marker = new Rect(x - 26, y - 22, 52, 44);
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = left <= 10f ? new Color(1f, .25f, .2f) : TypeColor(crisis.Data.Type);
                string label = $"{TypeCode(crisis.Data.Type)}\n{Mathf.CeilToInt(left)} sn";
                bool clicked = GUI.Button(marker, new GUIContent(label,
                    crisis.Data.Region + " — " + crisis.Data.Title));
                GUI.backgroundColor = previousColor;
                if (clicked)
                {
                    crises.Open(crisis);
                    GUIUtility.ExitGUI();
                }
            }

            GUI.Label(new Rect(35, 458, 680, 25), "SEL: Sel    KUR: Kuraklık    YAN: Orman yangını    SIC: Aşırı sıcaklık");
            DrawCrisisList();
        }

        private void DrawCrisisList()
        {
            // Haritadaki küçük işaretlere alternatif, daha büyük tıklama alanları.
            GUILayout.BeginArea(new Rect(35, 495, 680, 185));
            GUILayout.Label($"AKTİF KRİZLER: {crises.Active.Count}");
            if (crises.Active.Count == 0) GUILayout.Label("Şu anda aktif kriz yok.");
            // Tıklama listeyi değiştirebileceği için çizimden sonra döngüden çıkılır.
            foreach (var crisis in crises.Active)
            {
                string label = $"{crisis.Data.Region} — {crisis.Data.Title}\n" +
                    $"Kalan müdahale süresi: {Mathf.CeilToInt(crises.SecondsLeft(crisis))} sn";
                if (GUILayout.Button(label, GUILayout.Height(55)))
                {
                    crises.Open(crisis);
                    GUIUtility.ExitGUI();
                }
                GUILayout.Space(4);
            }
            GUILayout.EndArea();
        }

        private void DrawCard()
        {
            var crisis = crises.OpenCard;
            GUILayout.BeginArea(new Rect(20, 20, 715, 675), GUI.skin.box);
            GUILayout.Label(crisis.Data.Title.ToUpperInvariant());
            GUILayout.Label("Bölge: " + crisis.Data.Region);
            GUILayout.Label($"Kalan süre: {Mathf.CeilToInt(crises.SecondsLeft(crisis))} sn (duraklatıldı)");
            GUILayout.Space(12);
            GUILayout.Label(crisis.Data.Description, WrapStyle());
            GUILayout.Space(20);

            for (int i = 0; i < crisis.Data.Options.Length; i++)
            {
                var option = crisis.Data.Options[i];
                bool affordable = session.CanAfford(option.Cost);
                GUI.enabled = affordable;
                string price = option.Cost == 0 ? "Ücretsiz" : $"Para: −{option.Cost}";
                string label = option.Title + "\n" + price +
                    $"   |   Halk: {Signed(option.HappinessChange)}   |   İklim: {Signed(option.ClimateChange)}" +
                    (affordable ? "" : "   [PARA YETERSİZ]");
                bool clicked = GUILayout.Button(label, GUILayout.Height(75));
                GUI.enabled = true;
                GUILayout.Space(10);
                if (clicked)
                {
                    crises.Choose(i);
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.Space(12);
            if (GUILayout.Button("Karar vermeden kapat", GUILayout.Height(40)))
            {
                crises.CloseCard();
                // Kart/listenin kontrol sayısı değişince eski çizim turunu sonlandır.
                GUIUtility.ExitGUI();
            }
            GUILayout.EndArea();
        }

        private void DrawEnd()
        {
            GUILayout.BeginArea(new Rect(20, 20, 715, 675), GUI.skin.box);
            GUILayout.Label(session.State == SessionState.Won ? "VARDİYA TAMAMLANDI!" : "VARDİYA KAYBEDİLDİ");
            GUILayout.Label(session.EndReason, WrapStyle());
            GUILayout.Label($"Müdahale edilen kriz: {crises.HandledCount}");
            GUILayout.Label($"Kaçırılan / müdahale edilmeyen kriz: {crises.MissedCount}");
            GUILayout.Label($"Bitişte açık kalan kriz: {crises.Active.Count}");
            GUILayout.Space(20);
            if (GUILayout.Button("Yeni vardiya başlat", GUILayout.Height(45)))
            {
                session.RestartSession();
                GUIUtility.ExitGUI();
            }
            GUILayout.EndArea();
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();

        private static string TypeCode(CrisisType type)
        {
            switch (type)
            {
                case CrisisType.Flood: return "SEL";
                case CrisisType.Drought: return "KUR";
                case CrisisType.Wildfire: return "YAN";
                default: return "SIC";
            }
        }

        private static Color TypeColor(CrisisType type)
        {
            switch (type)
            {
                case CrisisType.Flood: return new Color(.35f, .7f, 1f);
                case CrisisType.Drought: return new Color(1f, .85f, .35f);
                case CrisisType.Wildfire: return new Color(1f, .5f, .2f);
                default: return new Color(1f, .55f, .65f);
            }
        }

        private static GUIStyle WrapStyle()
        {
            return new GUIStyle(GUI.skin.label) { wordWrap = true };
        }
    }
}
