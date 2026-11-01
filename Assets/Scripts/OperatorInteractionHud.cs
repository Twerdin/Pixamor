using UnityEngine;
using UnityEngine.UI;
namespace Pixamor
{
    // Serbest dolaşımda yalnızca nişangâh ve bağlama uygun küçük etkileşim ipucu gösterir.
    [RequireComponent(typeof(FirstPersonOperator))]
    public sealed class OperatorInteractionHud : MonoBehaviour
    {
        private FirstPersonOperator player;
        private CommandUI ui;
        private GameObject screen;
        private CanvasGroup visibility, prompt;
        private Image dot;
        private readonly Image[] strokes=new Image[4];
        private float focus;
        public bool IsTargetHighlighted => focus>.5f;
        private void Start()
        {
            player=GetComponent<FirstPersonOperator>();ui=new CommandUI();
            screen=new GameObject("OperatorInteractionHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));
            var canvas=screen.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=screen.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            visibility=screen.GetComponent<CanvasGroup>();visibility.alpha=0;visibility.blocksRaycasts=false;visibility.interactable=false;
            var cross=ui.Rect(screen.transform,"AdaptiveCrosshair",0,0,64,64);cross.anchorMin=cross.anchorMax=cross.pivot=new Vector2(.5f,.5f);cross.anchoredPosition=Vector2.zero;
            dot=ui.Panel(cross,"CenterDot",0,0,3,3,CommandUI.White).GetComponent<Image>();Center(dot.rectTransform);
            dot.sprite=ui.Circle;dot.type=Image.Type.Simple;
            for(int i=0;i<4;i++){strokes[i]=ui.Panel(cross,"Stroke"+i,0,0,5,2,CommandUI.White).GetComponent<Image>();Center(strokes[i].rectTransform);}
            var hint=ui.Rect(screen.transform,"MonitorFocusPrompt",0,0,420,90);hint.anchorMin=hint.anchorMax=hint.pivot=Vector2.zero;hint.anchoredPosition=new Vector2(46,38);
            prompt=hint.gameObject.AddComponent<CanvasGroup>();prompt.alpha=0;prompt.blocksRaycasts=false;
            ui.Panel(hint,"Accent",0,18,3,54,CommandUI.Teal);
            var key=ui.Panel(hint,"InteractionKey",18,18,54,54,new Color(.025f,.055f,.08f,.9f),true);
            var keyText=ui.Text(key,"E",0,0,54,54,32,CommandUI.White,"strong");keyText.alignment=TMPro.TextAlignmentOptions.Center;
            var label=ui.Text(hint,"PANEL ERİŞİMİ",90,15,310,29,20,CommandUI.Teal,"strong");
            var action=ui.Text(hint,"Yakınlaşmak için",90,40,310,42,30,CommandUI.White,"strong");
            // Açık renkli duvarlarda da okunabilsin; bütün alt ekranı kaplayan kutu kullanılmaz.
            label.outlineWidth=.15f;label.outlineColor=new Color32(0,0,0,210);
            action.outlineWidth=.15f;action.outlineColor=new Color32(0,0,0,210);
        }
        private static void Center(RectTransform rect){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;}
        private void LateUpdate()
        {
            if(screen==null || player==null)return;
            bool visible=!player.UIBlocked && !player.IsUsingMonitor && !player.IsChangingView;
            bool target=visible && player.CanInteractWithMonitor;
            float delta=Time.unscaledDeltaTime;
            visibility.alpha=Mathf.MoveTowards(visibility.alpha,visible?1:0,delta*9);
            focus=Mathf.MoveTowards(focus,target?1:0,delta*7);
            prompt.alpha=Mathf.MoveTowards(prompt.alpha,target?1:0,delta*7);
            Color color=Color.Lerp(new Color(.9f,.95f,1,.75f),CommandUI.Teal,focus);
            float gap=Mathf.Lerp(6,12,focus), length=Mathf.Lerp(4,7,focus);
            for(int i=0;i<4;i++){
                var r=strokes[i].rectTransform;r.sizeDelta=i<2?new Vector2(length,2):new Vector2(2,length);
                r.anchoredPosition=i==0?new Vector2(-gap,0):i==1?new Vector2(gap,0):i==2?new Vector2(0,-gap):new Vector2(0,gap);
                strokes[i].color=color;
            }
            dot.color=color;dot.rectTransform.sizeDelta=Vector2.one*Mathf.Lerp(2.5f,4,focus);
        }
        private void OnEnable(){if(screen!=null)screen.SetActive(true);}
        private void OnDisable(){if(screen!=null)screen.SetActive(false);}
        private void OnDestroy(){if(screen!=null)Destroy(screen);ui?.Dispose();}
    }
}

