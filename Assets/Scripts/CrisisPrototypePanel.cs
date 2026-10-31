using UnityEngine;

namespace Pixamor
{
    // Harita ve Canvas tasarımı gelene kadar kriz listesini ve kartı gösterir.
    [RequireComponent(typeof(GameSession), typeof(CrisisManager))]
    public class CrisisPrototypePanel : MonoBehaviour
    {
        private GameSession session;
        private CrisisManager crises;

        private void Awake()
        {
            session = GetComponent<GameSession>();
            crises = GetComponent<CrisisManager>();
        }

        private void OnGUI()
        {
            // Sabit tasarımı Game penceresine sığdır; dar ekranda panel taşmasın.
            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

            DrawResources();
            if (session.State != SessionState.Running) DrawEnd();
            else if (crises.OpenCard != null) DrawCard();
            else DrawCrisisList();

            GUI.enabled = true;
            GUI.matrix = previousMatrix;
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

        private void DrawCrisisList()
        {
            GUILayout.BeginArea(new Rect(20, 20, 715, 675), GUI.skin.box);
            GUILayout.Label("AKTİF KRİZLER — GEÇİCİ HARİTA LİSTESİ");
            GUILayout.Label("Bir krize tıklayarak müdahale kartını aç.");
            GUILayout.Space(12);
            if (crises.Active.Count == 0) GUILayout.Label("Şu anda aktif kriz yok.");
            // Tıklama listeyi değiştirebileceği için çizimden sonra döngüden çıkılır.
            foreach (var crisis in crises.Active)
            {
                string label = $"{crisis.Data.Region} — {crisis.Data.Title}\n" +
                    $"Kalan müdahale süresi: {Mathf.CeilToInt(crises.SecondsLeft(crisis))} sn";
                if (GUILayout.Button(label, GUILayout.Height(65)))
                {
                    crises.Open(crisis);
                    break;
                }
                GUILayout.Space(8);
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
                    break;
                }
            }

            GUILayout.Space(12);
            if (GUILayout.Button("Karar vermeden kapat", GUILayout.Height(40)))
                crises.CloseCard();
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
                session.RestartSession();
            GUILayout.EndArea();
        }

        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();

        private static GUIStyle WrapStyle()
        {
            return new GUIStyle(GUI.skin.label) { wordWrap = true };
        }
    }
}
