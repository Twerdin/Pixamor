using UnityEngine;

namespace Pixamor
{
    // Harici ses dosyası gerektirmeyen geçici uyarı sesleri.
    [RequireComponent(typeof(GameSession), typeof(CrisisManager), typeof(AudioSource))]
    public class MonitorFeedback : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volume = .2f;
        [SerializeField, Min(1f)] private float alarmInterval = 8f;
        private GameSession session;
        private CrisisManager crises;
        private AudioSource source;
        private AudioClip notification;
        private AudioClip alarm;
        private AudioClip victory;
        private AudioClip defeat;
        private bool wasCritical;
        private float lastAlarmTime;

        private void Awake()
        {
            session = GetComponent<GameSession>();
            crises = GetComponent<CrisisManager>();
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            notification = CreateTone("YeniKriz", 700f, 1000f, .3f);
            alarm = CreateTone("KritikAlarm", 500f, 350f, .5f);
            victory = CreateTone("VardiyaBasarili", 600f, 900f, .6f);
            defeat = CreateTone("VardiyaKayip", 350f, 180f, .7f);
        }

        private void OnEnable()
        {
            crises.CrisisSpawned += OnCrisisSpawned;
            session.Ended += OnEnded;
            session.Restarted += ResetFeedback;
        }

        private void OnDisable()
        {
            crises.CrisisSpawned -= OnCrisisSpawned;
            session.Ended -= OnEnded;
            session.Restarted -= ResetFeedback;
            source.Stop();
        }

        private void Update()
        {
            if (session.State != SessionState.Running || session.IsPaused) return;
            bool critical = session.HasHappinessWarning || session.HasClimateWarning;
            // Gerçek süre değil oyun süresi kullanılır; kart açıkken alarm tekrarlanmaz.
            if (critical && (!wasCritical || session.ElapsedTime - lastAlarmTime >= alarmInterval))
            {
                Play(alarm);
                lastAlarmTime = session.ElapsedTime;
            }
            wasCritical = critical;
        }

        private void OnCrisisSpawned(ActiveCrisis crisis) { Play(notification); }

        private void OnEnded(SessionState state)
        {
            source.Stop();
            Play(state == SessionState.Won ? victory : defeat);
        }

        private void ResetFeedback()
        {
            source.Stop();
            wasCritical = false;
            lastAlarmTime = 0f;
        }

        private void Play(AudioClip clip)
        {
            if (volume > 0f) source.PlayOneShot(clip, volume);
        }

        private static AudioClip CreateTone(string title, float first, float second, float duration)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(sampleRate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float frequency = t < duration / 2f ? first : second;
                // Her iki nota da sessizlikte başlayıp biter; ani tık sesleri oluşmaz.
                float noteTime = t % (duration / 2f);
                float envelope = Mathf.Min(1f, noteTime / .015f,
                    (duration / 2f - noteTime) / .04f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * noteTime) * envelope * .4f;
            }
            var clip = AudioClip.Create(title, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            Destroy(notification);
            Destroy(alarm);
            Destroy(victory);
            Destroy(defeat);
        }
    }
}
