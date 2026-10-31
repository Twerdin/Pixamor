using UnityEngine;
using UnityEngine.UI;

namespace Pixamor
{
    public partial class MonitorPrototype
    {
        private void BuildOperations()
        {
            var mapPanel = ui.Panel(content,"WorldOperations",0,0,1120,676,CommandUI.Surface,true);
            ui.Caption(mapPanel,"CANLI TAKİP / DÜNYA GENELİ",24,18,750);
            ui.Text(mapPanel,"Operasyon haritası",24,48,800,52,40,CommandUI.White,"strong");
            ui.Badge(mapPanel,"12 KRİZ BÖLGESİ",876,35,220,CommandUI.Teal);

            var mapFrame = ui.Panel(mapPanel,"MapViewport",20,112,1080,478,CommandUI.Background,true);
            mapFrame.gameObject.AddComponent<RectMask2D>();
            var mapImage = ui.Rect(mapFrame,"GeographicMap",0,0,1080,478).gameObject.AddComponent<RawImage>();
            mapImage.texture = worldMap;
            mapImage.raycastTarget = false;
            ui.Text(mapFrame,"80° N",14,8,150,26,18,CommandUI.Muted,"mono");
            ui.Text(mapFrame,"60° S",14,445,150,26,18,CommandUI.Muted,"mono");
            foreach(var crisis in crises.Active) BuildMapMarker(mapFrame,crisis);

            CrisisType[] types = { CrisisType.Flood,CrisisType.Drought,CrisisType.Wildfire,CrisisType.Heatwave };
            float[] legendX = {24,242,495,842};
            for(int i=0;i<types.Length;i++)
            {
                Color color = CommandUI.CrisisColor(types[i]);
                ui.Icon(mapPanel,types[i].ToString(),legendX[i],622,28,color);
                ui.Text(mapPanel,CommandUI.TypeName(types[i]),legendX[i]+40,621,i==2?280:212,34,23,CommandUI.Muted);
            }

            BuildDispatch();
        }

        private void BuildMapMarker(Transform parent,ActiveCrisis crisis)
        {
            Vector2 p = crisis.Data.MapPosition;
            float x=Mathf.Clamp01(p.x)*1080, y=Mathf.Clamp01(p.y)*478;
            Color color=CommandUI.CrisisColor(crisis.Data.Type);
            var halo=ui.Panel(parent,"AlertRadius",x-48,y-48,96,96,new Color(color.r,color.g,color.b,.12f));
            halo.GetComponent<Image>().sprite=ui.Circle;
            halo.GetComponent<Image>().type=Image.Type.Simple;
            var button=ui.Button(parent,"CrisisMarker",x-37,y-37,74,74,()=>crises.Open(crisis),CommandUI.Surface);
            var image=button.GetComponent<Image>();
            image.sprite=ui.Circle;
            image.type=Image.Type.Simple;
            var outline=button.GetComponent<Outline>();
            outline.effectColor=color;
            outline.effectDistance=new Vector2(2,-2);
            ui.Icon(button.transform,crisis.Data.Type.ToString(),23,8,28,color);
            var timer=ui.Text(button.transform,"",0,39,74,28,22,CommandUI.White,"strong");
            timer.alignment=TextAnchor.MiddleCenter;

            float labelX=p.x>.72f ? x-267 : x+47;
            var tag=ui.Panel(parent,"RegionTag",labelX,y-20,220,40,CommandUI.Surface,true);
            var region=ui.Text(tag,crisis.Data.Region,12,5,196,30,22,CommandUI.White,"strong");
            region.resizeTextForBestFit=true;
            region.resizeTextMinSize=19;
            region.resizeTextMaxSize=22;
            refreshers.Add(()=>
            {
                float left=crises.SecondsLeft(crisis);
                Color warning=left<=10f ? CommandUI.Red : color;
                timer.text=Mathf.CeilToInt(left)+" sn";
                timer.color=left<=10f ? CommandUI.Red : CommandUI.White;
                outline.effectColor=warning;
                float pulse=1f+.06f*Mathf.Sin(session.ElapsedTime*(left<=10f?5f:2f));
                halo.localScale=Vector3.one*pulse;
            });
        }

        private void BuildDispatch()
        {
            var dispatch=ui.Panel(content,"Dispatch",1140,0,412,676,CommandUI.Surface,true);
            ui.Caption(dispatch,"MÜDAHALE KUYRUĞU",22,20,320);
            var count=ui.Text(dispatch,crises.Active.Count.ToString("00"),342,16,48,38,30,CommandUI.Amber,"mono");
            count.alignment=TextAnchor.MiddleRight;
            if(crises.Active.Count==0)
            {
                ui.Icon(dispatch,"Globe",158,112,96,CommandUI.Teal);
                var waiting=ui.Text(dispatch,"Tüm bölgeler\nizleniyor.",24,238,364,100,36,CommandUI.White,"display");
                waiting.alignment=TextAnchor.MiddleCenter;
                var hint=ui.Text(dispatch,"Yeni uyarılar bu alana düşer.\nHaritadan veya dosyadan müdahale et.",24,354,364,98,25,CommandUI.Muted);
                hint.alignment=TextAnchor.MiddleCenter;
            }
            else
            {
                int row=0;
                foreach(var crisis in crises.Active)
                {
                    if(row>=2) break;
                    BuildDispatchCard(dispatch,crisis,row);
                    row++;
                }
            }

            var totals=ui.Panel(dispatch,"Totals",20,448,372,68,CommandUI.Background);
            ui.Number(totals,crises.HandledCount.ToString("00"),12,5,64,53,42,CommandUI.Teal);
            ui.Caption(totals,"MÜDAHALE",82,24,120,22);
            ui.Number(totals,crises.MissedCount.ToString("00"),210,5,64,53,42,CommandUI.Amber);
            ui.Text(totals,"KAÇIRILAN",274,24,98,28,20,CommandUI.Muted,"strong");
            var latest=ui.Panel(dispatch,"LatestResult",20,532,372,124,CommandUI.Background);
            ui.Caption(latest,"SON OPERASYON NOTU",14,10,344,25);
            news=ui.Text(latest,crises.LastResult,14,43,344,72,24,CommandUI.White);
            news.resizeTextForBestFit=true;
            news.resizeTextMinSize=20;
            news.resizeTextMaxSize=24;
        }

        private void BuildDispatchCard(Transform parent,ActiveCrisis crisis,int row)
        {
            Color color=CommandUI.CrisisColor(crisis.Data.Type);
            var button=ui.Button(parent,"CrisisFile",20,78+row*178,372,160,()=>crises.Open(crisis));
            ui.Caption(button.transform,"ÖNCELİK 0"+(row+1),16,12,188,30,color);
            var timer=ui.Text(button.transform,"",216,11,140,36,24,color,"mono");
            timer.alignment=TextAnchor.MiddleRight;
            ui.Text(button.transform,crisis.Data.Title,16,48,340,70,29,CommandUI.White,"strong");
            ui.Icon(button.transform,crisis.Data.Type.ToString(),16,120,24,color);
            ui.Text(button.transform,crisis.Data.Region,50,117,286,32,24,CommandUI.Muted);
            var progress=ui.Bar(button.transform,16,153,340,3,color);
            refreshers.Add(()=>
            {
                float left=crises.SecondsLeft(crisis);
                timer.text=Mathf.CeilToInt(left)+" sn ›";
                timer.color=left<=10f ? CommandUI.Red : color;
                CommandUI.SetBar(progress,left/crisis.Data.ResponseDuration,timer.color);
            });
        }
    }
}
