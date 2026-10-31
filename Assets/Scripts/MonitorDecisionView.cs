using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pixamor
{
    public partial class MonitorPrototype
    {
        private void BuildDecision()
        {
            var crisis=crises.OpenCard;
            Color color=CommandUI.CrisisColor(crisis.Data.Type);
            var panel=ui.Panel(content,"DecisionWorkspace",24,180,1552,676,new Color(.035f,.07f,.105f,.86f));
            ui.Icon(panel,crisis.Data.Type.ToString(),24,22,46,color);
            ui.Caption(panel,"MÜDAHALE DOSYASI / "+crisis.Data.Region,86,25,1080,34,color);
            var countdown=ui.Text(panel,"",1194,20,334,40,28,CommandUI.Amber,"strong");
            countdown.alignment=TextAlignmentOptions.MidlineRight;
            var remaining=ui.Bar(panel,24,191,1504,5,CommandUI.Amber);
            var others=ui.Text(panel,"",24,581,1504,32,24,CommandUI.Amber);
            refreshers.Add(()=>
            {
                float left=crises.SecondsLeft(crisis);
                Color warning=left<=10?CommandUI.Red:CommandUI.Amber;
                countdown.text="KALAN SÜRE / "+Mathf.CeilToInt(left)+" sn";
                countdown.color=warning;
                CommandUI.SetBar(remaining,left/crisis.Data.ResponseDuration,warning);
                ActiveCrisis urgent=null;
                foreach(var other in crises.Active)if(other!=crisis && (urgent==null || other.Deadline<urgent.Deadline))urgent=other;
                others.text=urgent==null?"ZAMAN AKIYOR / Süre dolmadan karar ver.":
                    "DİĞER KRİZ / "+urgent.Data.Region+" / "+Mathf.CeilToInt(crises.SecondsLeft(urgent))+" sn — Haritaya dönerek öncelik seç.";
                others.color=urgent!=null && crises.SecondsLeft(urgent)<=10?CommandUI.Red:CommandUI.Amber;
            });
            ui.Text(panel,crisis.Data.Title,24,77,1504,62,50,CommandUI.White,"display");
            ui.Text(panel,crisis.Data.Description,24,145,1504,46,30,CommandUI.Muted);

            for(int i=0;i<crisis.Data.Options.Length;i++)
                BuildOption(panel,crisis,i,24+i*510);
            var close=ui.Button(panel,"ReturnToMap",24,626,1504,36,crises.CloseCard,new Color(0,0,0,.12f));
            var closeLabel=ui.Text(close.transform,"‹  Haritaya dön. Kararı daha sonra ver.",0,0,1504,36,25,CommandUI.Muted);
            closeLabel.alignment=TextAlignmentOptions.Center;
        }

        private void BuildOption(Transform parent,ActiveCrisis crisis,int index,float x)
        {
            var option=crisis.Data.Options[index];
            bool affordable=session.CanAfford(option.Cost);
            Color accent=!affordable ? CommandUI.Muted : option.IsIntervention ? CommandUI.Teal : CommandUI.Amber;
            var card=ui.Button(parent,"Intervention"+index,x,210,484,360,()=>{if(crises.OpenCard==crisis)crises.Choose(index);},new Color(.06f,.12f,.17f,.68f));
            card.interactable=affordable;
            ui.Panel(card.transform,"Accent",0,18,3,324,accent);
            ui.Caption(card.transform,"SEÇENEK 0"+(index+1),22,18,334,28,accent);
            ui.Text(card.transform,"›",406,12,54,46,34,accent,"strong");
            var title=ui.Text(card.transform,option.Title,22,64,440,76,32,
                affordable ? CommandUI.White : CommandUI.Muted,"strong");
            title.enableAutoSizing=true;
            title.fontSizeMin=28;
            title.fontSizeMax=32;
            ui.Caption(card.transform,"BÜTÇE MALİYETİ",22,148,440,28);
            ui.Number(card.transform,option.Cost==0?"Ücretsiz":"−"+option.Cost,22,178,440,64,
                option.Cost==0?50:60,affordable ? CommandUI.White : CommandUI.Red);

            BuildImpact(card.transform,"People","HALK",option.HappinessChange,session.Happiness,22,244);
            BuildImpact(card.transform,"Climate","İKLİM",option.ClimateChange,session.Climate,248,244);
            var action=ui.Panel(card.transform,"Action",22,314,440,34,
                new Color(accent.r*.15f,accent.g*.15f,accent.b*.15f));
            var text=ui.Text(action,affordable ? option.IsIntervention ? "MÜDAHALEYİ UYGULA  →" : "MÜDAHALE ETME" : "YETERSİZ BÜTÇE / KİLİTLİ",
                6,3,428,28,23,affordable ? accent : CommandUI.Red,"strong");
            text.alignment=TextAlignmentOptions.Center;
        }

        private void BuildImpact(Transform parent,string icon,string title,int delta,int current,float x,float y)
        {
            Color color=delta>=0?CommandUI.Teal:CommandUI.Red;
            ui.Icon(parent,icon,x,y+3,24,color);
            ui.Caption(parent,title,x+34,y,172,25);
            ui.Text(parent,Signed(delta),x,y+29,100,44,36,color,"strong");
            ui.Text(parent,current+" → "+Mathf.Clamp(current+delta,0,100),x+94,y+39,114,28,21,CommandUI.Muted,"mono");
        }

        private void BuildReport()
        {
            bool won=session.State==SessionState.Won;
            Color accent=won?CommandUI.Teal:CommandUI.Red;
            var panel=ui.Panel(content,"ShiftReport",24,180,1552,676,new Color(.035f,.07f,.105f,.88f));
            ui.Caption(panel,"VARDİYA SONU / OPERASYON RAPORU",32,28,1488);
            ui.Text(panel,won?"Görev tamamlandı.":"Kontrol kaybedildi.",32,96,1488,100,76,accent,"display");
            ui.Text(panel,session.EndReason,32,213,1488,70,34,CommandUI.Muted);
            ReportStat(panel,32,"MÜDAHALE EDİLEN",crises.HandledCount.ToString("00"),CommandUI.Teal);
            ReportStat(panel,542,"KAÇIRILAN / MÜDAHALESİZ",crises.MissedCount.ToString("00"),CommandUI.Amber);
            ReportStat(panel,1052,"KALAN BÜTÇE",session.Money.ToString(),CommandUI.White);
            ui.Text(panel,$"Son göstergeler   /   Halk {session.Happiness}/100   /   İklim {session.Climate}/100",
                32,502,1488,44,29,CommandUI.Muted);
            var restart=ui.Button(panel,"RestartShift",32,577,1488,64,()=>
            {
                session.RestartSession();
                currentView=null;
            },new Color(.09f,.28f,.26f));
            var text=ui.Text(restart.transform,"YENİ VARDİYA BAŞLAT  →",0,12,1488,44,32,CommandUI.Teal,"strong");
            text.alignment=TextAlignmentOptions.Center;
        }

        private void ReportStat(Transform parent,float x,string title,string value,Color color)
        {
            var card=ui.Panel(parent,"ReportMetric",x,321,468,150,CommandUI.Background,true);
            ui.Caption(card,title,22,18,424);
            ui.Number(card,value,22,54,424,88,76,color);
        }

        private static string Signed(int value)=>value>0?"+"+value:value.ToString();
    }
}
