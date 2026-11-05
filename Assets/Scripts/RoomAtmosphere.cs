using UnityEngine;

namespace Pixamor
{
    // Sahneye editör aracıyla yerleştirilmiş ışıkları yönetir; model oluşturmaz.
    public class RoomAtmosphere : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private Renderer[] strips;
        [SerializeField] private Light indicator;
        private MaterialPropertyBlock block;
        private GameFlowController flow;

        public void Configure(GameSession game,Renderer[] renderers,Light light)
        {
            session=game;
            strips=renderers;
            indicator=light;
        }

        private void Awake() { block=new MaterialPropertyBlock(); }

        private void Update()
        {
            if(session==null) return;
            if(flow==null) flow=session.GetComponent<GameFlowController>();
            bool critical=session.HasHappinessWarning || session.HasClimateWarning;
            Color color=critical?CommandUI.Red:CommandUI.Teal;
            float pulse=critical && (flow==null || flow.AnimationsEnabled)
                ? .85f+.15f*Mathf.Sin(Time.unscaledTime*2f):1f;
            block.SetColor("_EmissionColor",color*2.4f*pulse);
            foreach(var strip in strips) if(strip!=null) strip.SetPropertyBlock(block);
            if(indicator!=null)
            {
                indicator.color=color;
                indicator.intensity=3f*pulse;
            }
        }

        private void OnDisable()
        {
            if(strips==null) return;
            foreach(var strip in strips) if(strip!=null) strip.SetPropertyBlock(null);
        }
    }
}
