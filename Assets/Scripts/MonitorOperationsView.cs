using UnityEngine;
using UnityEngine.UI;

namespace Pixamor
{
    public partial class MonitorPrototype
    {
        private void BuildOperations()
        {
            // İşaretler doğrudan tam ekran haritanın koordinatlarına bağlanır.
            foreach(var crisis in crises.Active) BuildMapMarker(content,crisis);
            BuildDispatch();
            var latest=ui.Panel(content,"LatestResult",1090,858,474,164,new Color(.025f,.06f,.09f,.35f));
            ui.Caption(latest,"SON OPERASYON NOTU",18,16,438);
            news=ui.Text(latest,crises.LastResult,18,57,438,92,26,CommandUI.White);
            news.resizeTextForBestFit=true;
            news.resizeTextMinSize=22;
            news.resizeTextMaxSize=26;
        }

        private void BuildMapMarker(Transform parent,ActiveCrisis crisis)
        {
            Vector2 p=crisis.Data.MapPosition;
            float x=Mathf.Clamp01(p.x)*1600, y=Mathf.Clamp01(p.y)*1040;
            Color color=CommandUI.CrisisColor(crisis.Data.Type);
            var halo=ui.Panel(parent,"AlertRadius",x-48,y-48,96,96,new Color(color.r,color.g,color.b,.12f));
            halo.GetComponent<Image>().sprite=ui.Circle;
            halo.GetComponent<Image>().type=Image.Type.Simple;
            var button=ui.Button(parent,"CrisisMarker",x-37,y-37,74,74,()=>crises.Open(crisis),new Color(.04f,.08f,.11f,.72f));
            var image=button.GetComponent<Image>();
            image.sprite=ui.Circle;
            image.type=Image.Type.Simple;
            var outline=button.GetComponent<Outline>();
            outline.effectColor=color;
            outline.effectDistance=new Vector2(2,-2);
            ui.Icon(button.transform,crisis.Data.Type.ToString(),23,8,28,color);
            var timer=ui.Text(button.transform,"",0,39,74,28,22,CommandUI.White,"strong");
            timer.alignment=TextAnchor.MiddleCenter;
            float labelX=p.x>.72f?x-287:x+47;
            var tag=ui.Panel(parent,"RegionTag",labelX,y-22,240,44,new Color(.025f,.06f,.09f,.48f));
            var region=ui.Text(tag,crisis.Data.Region,10,5,220,34,24,CommandUI.White,"strong");
            region.resizeTextForBestFit=true;
            region.resizeTextMinSize=21;
            region.resizeTextMaxSize=24;
            refreshers.Add(()=>
            {
                float left=crises.SecondsLeft(crisis);
                Color warning=left<=10f?CommandUI.Red:color;
                timer.text=Mathf.CeilToInt(left)+" sn";
                timer.color=left<=10f?CommandUI.Red:CommandUI.White;
                outline.effectColor=warning;
                halo.localScale=Vector3.one*(flow != null && !flow.AnimationsEnabled ? 1f :
                    1f+.06f*Mathf.Sin(session.ElapsedTime*(left<=10f?5f:2f)));
            });
        }

        private void BuildDispatch()
        {
            var dispatch=ui.Rect(content,"FloatingDispatch",1180,184,384,268);
            ui.Caption(dispatch,"AKTİF KRİZLER",10,0,280);
            var count=ui.Text(dispatch,crises.Active.Count.ToString("00"),302,-2,72,36,28,CommandUI.Amber,"mono");
            count.alignment=TextAnchor.MiddleRight;
            if(crises.Active.Count==0)
                ui.Text(dispatch,"Bölgeler izleniyor.",10,46,364,46,28,CommandUI.Muted);
            int row=0;
            foreach(var crisis in crises.Active)
            {
                if(row>=2) break;
                BuildDispatchCard(dispatch,crisis,row++);
            }
        }

        private void BuildDispatchCard(Transform parent,ActiveCrisis crisis,int row)
        {
            Color color=CommandUI.CrisisColor(crisis.Data.Type);
            var button=ui.Button(parent,"CrisisFile",0,42+row*110,384,104,()=>crises.Open(crisis),
                new Color(.025f,.06f,.09f,.32f));
            // Kuyrukta da opak çerçeve yok; harita arkadan okunmaya devam eder.
            button.GetComponent<Outline>().enabled=false;
            ui.Text(button.transform,crisis.Data.Region,12,8,238,30,22,color,"strong");
            var timer=ui.Text(button.transform,"",252,7,120,30,22,color,"mono");
            timer.alignment=TextAnchor.MiddleRight;
            var title=ui.Text(button.transform,crisis.Data.Title,12,40,360,49,28,CommandUI.White,"strong");
            title.resizeTextForBestFit=true;
            title.resizeTextMinSize=24;
            title.resizeTextMaxSize=28;
            var progress=ui.Bar(button.transform,12,98,360,2,color);
            refreshers.Add(()=>
            {
                float left=crises.SecondsLeft(crisis);
                timer.text=Mathf.CeilToInt(left)+" sn ›";
                timer.color=left<=10f?CommandUI.Red:color;
                CommandUI.SetBar(progress,left/crisis.Data.ResponseDuration,timer.color);
            });
        }
    }
}
