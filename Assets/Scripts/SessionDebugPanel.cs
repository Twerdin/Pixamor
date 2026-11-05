using UnityEngine;

namespace Pixamor
{
    // Geçici deneme paneli. Asıl Canvas arayüzü hazır olunca kaldırılabilir.
    [RequireComponent(typeof(GameSession))]
    public class SessionDebugPanel : MonoBehaviour
    {
        private GameSession session;
        private string message = "Sayaç ve kaynak denemesi hazır.";

        private void Awake()
        {
            session = GetComponent<GameSession>();
        }

        private void OnGUI()
        {
            // OnGUI sadece ekranı çizer; oyun zamanı burada ilerletilmez.
            GUILayout.BeginArea(new Rect(20, 20, 370, 620), GUI.skin.box);
            GUILayout.Label("İKLİM OPERATÖRÜ — GEÇİCİ TEST PANELİ");
            GUILayout.Space(8);
            GUILayout.Label($"Kalan süre: {FormatTime(session.RemainingTime)}");
            GUILayout.Label($"Para: {session.Money}");
            GUILayout.Label($"Halk mutluluğu: {session.Happiness} / 100");
            GUILayout.Label($"İklim dengesi: {session.Climate} / 100");
            GUILayout.Label($"Sonraki bütçe: {Mathf.CeilToInt(session.TimeUntilIncome)} sn");
            GUILayout.Label($"Durum: {GetStatus()}");

            if (session.HasHappinessWarning)
                GUILayout.Label("UYARI: Halk protestoları başladı!");
            if (session.HasClimateWarning)
                GUILayout.Label("UYARI: İklim dengesi kritik!");

            GUILayout.Space(12);
            bool running = session.State == SessionState.Running;
            GUI.enabled = running;
            if (GUILayout.Button(session.IsPaused ? "Devam et" : "Duraklat (kart testi)"))
                session.SetPaused(!session.IsPaused);

            GUI.enabled = running && session.CanAfford(25);
            if (GUILayout.Button("Soğutma merkezleri: −25 para, +8 halk, −5 iklim"))
                Apply(25, 8, -5);

            GUI.enabled = running && session.CanAfford(15);
            if (GUILayout.Button("Su ve kısıtlama: −15 para, −3 halk, +5 iklim"))
                Apply(15, -3, 5);

            GUI.enabled = running;
            if (GUILayout.Button("Müdahale etme: ücretsiz, −15 halk, −5 iklim"))
                Apply(0, -15, -5);

            GUILayout.Space(8);
            if (GUILayout.Button("Test: Halkı sıfırla"))
                session.ApplyResourceChanges(-100, 0);
            if (GUILayout.Button("Test: İklimi sıfırla"))
                session.ApplyResourceChanges(0, -100);

            GUI.enabled = true;
            if (GUILayout.Button("Vardiyayı yeniden başlat"))
            {
                session.RestartSession();
                message = "Yeni vardiya başladı.";
            }

            GUILayout.Space(8);
            GUILayout.Label(message);
            if (!running)
                GUILayout.Label(session.EndReason);
            GUILayout.EndArea();
        }

        private void Apply(int cost, int happiness, int climate)
        {
            message = session.TryApplyIntervention(cost, happiness, climate)
                ? "Deneme müdahalesi uygulandı."
                : "Müdahale uygulanamadı.";
        }

        private string GetStatus()
        {
            if (session.State == SessionState.Won) return "KAZANDIN";
            if (session.State == SessionState.Lost) return "KAYBETTİN";
            return session.IsPaused ? "DURAKLATILDI" : "VARDİYA DEVAM EDİYOR";
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
