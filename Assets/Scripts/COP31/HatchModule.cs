using UnityEngine;

namespace Pixamor.COP31
{
    /// <summary>
    /// Masadaki gizli kapak: kapak aşağı iner, masanın altına kayar, ardından içerik yukarı yükselir (Lerp).
    /// Kapalıyken içerik deaktiftir (tıklanamaz).
    /// </summary>
    public class HatchModule : MonoBehaviour
    {
        public float duration = 1.1f;

        Transform lid;
        Transform content;
        Vector3 lidClosed, lidDown, lidOpen;
        Vector3 contentClosed, contentOpen;
        float t;
        bool open;
        bool ready;

        public bool IsOpen => open;
        public bool FullyOpen => open && t >= 1f;

        public void Setup(Transform lidPivot, Transform contentRoot, float depth)
        {
            lid = lidPivot;
            content = contentRoot;
            lidClosed = lid.localPosition;
            lidDown = lidClosed + new Vector3(0f, -0.045f, 0f);
            lidOpen = lidDown + new Vector3(0f, 0f, depth + 0.06f);
            contentClosed = content.localPosition;
            contentOpen = new Vector3(contentClosed.x, 0f, contentClosed.z);
            content.gameObject.SetActive(false);
            t = 0f;
            ready = true;
        }

        public void SetOpen(bool value)
        {
            if (!ready) return;
            if (value && !open) Sfx.Play(SfxType.Open, 0.7f);
            open = value;
            if (value) content.gameObject.SetActive(true);
        }

        void Update()
        {
            if (!ready) return;
            t = Mathf.MoveTowards(t, open ? 1f : 0f, Time.deltaTime / duration);

            float a = Smooth(Mathf.Clamp01(t / 0.2f));
            float b = Smooth(Mathf.Clamp01((t - 0.2f) / 0.35f));
            float c = Smooth(Mathf.Clamp01((t - 0.55f) / 0.45f));

            lid.localPosition = lidClosed + (lidDown - lidClosed) * a + (lidOpen - lidDown) * b;
            content.localPosition = Vector3.Lerp(contentClosed, contentOpen, c);

            if (!open && t <= 0f && content.gameObject.activeSelf) content.gameObject.SetActive(false);
        }

        static float Smooth(float x) => x * x * (3f - 2f * x);
    }
}
