using System;
using UnityEngine;
using UnityEngine.UI;

namespace Pixamor
{
    public partial class GameFlowController
    {
        private Button MenuButton(Transform parent,string label,float x,float y,float w,Action action,bool primary=false)
        {
            var button=ui.Button(parent,label,x,y,w,78,action,
                primary?new Color(.07f,.29f,.26f,.95f):new Color(.035f,.07f,.11f,.68f));
            ui.Text(button.transform,label,24,18,w-48,46,32,primary?CommandUI.Teal:CommandUI.White,"strong");
            return button;
        }

        private void BuildMainMenu()
        {
            subtitle.text="OPERASYON TERMİNALİ / VARDİYA BAŞLANGICI";
            ui.Text(body,"BİR DÜNYA.\nON İKİ KRİZ.\nTEK OPERATÖR.",80,34,920,350,88,CommandUI.White,"display");
            ui.Text(body,"Halkı koru. Kaynaklarını yönet.\nİklim dengesini ayakta tut.",80,418,860,100,34,CommandUI.Muted);
            MenuButton(body,"VARDİYAYI BAŞLAT  →",80,574,860,StartShift,true);
            ui.Text(body,$"{FormatTime(session.RemainingTime)} VARDİYA   /   12 KRİZ   /   TEK ODA",80,682,890,38,25,CommandUI.Muted,"mono");
            var right=ui.Panel(body,"MenuActions",1040,68,480,588,new Color(.025f,.06f,.09f,.44f),true);
            ui.Icon(right,"Globe",28,28,70,CommandUI.Teal);
            ui.Caption(right,"DÜNYA SENİ BEKLİYOR",28,124,424,34,CommandUI.Teal);
            ui.Text(right,"Her müdahalenin bir bedeli var.\nKarar senin.",28,181,424,110,33,CommandUI.White,"display");
            MenuButton(right,"Nasıl oynanır?",24,306,432,()=>Open(GameScreen.Help));
            MenuButton(right,"Ayarlar",24,398,432,()=>Open(GameScreen.Settings));
            MenuButton(right,"Oyundan çık",24,490,432,QuitGame);
        }

        private void BuildPauseMenu()
        {
            subtitle.text="OPERASYON TERMİNALİ / DURAKLATILDI";
            ui.Text(body,"Vardiya duraklatıldı.",80,40,1440,120,82,CommandUI.White,"display");
            ui.Text(body,"Sayaçlar, kriz süreleri ve bütçe ödemeleri bekliyor.",80,183,1440,65,32,CommandUI.Muted);
            MenuButton(body,"DEVAM ET  →",80,292,860,Resume,true);
            MenuButton(body,"Nasıl oynanır?",80,388,860,()=>Open(GameScreen.Help));
            MenuButton(body,"Ayarlar",80,484,860,()=>Open(GameScreen.Settings));
            MenuButton(body,"Ana menüye dön",80,580,860,MainMenu);
            ui.Icon(body,"Clock",1160,318,136,CommandUI.Amber);
            ui.Number(body,FormatTime(session.RemainingTime),1030,482,400,120,94,CommandUI.Amber);
            ui.Caption(body,crises.OpenCard!=null?"KARAR DOSYASI KORUNDU":"KALAN VARDİYA SÜRESİ",1030,621,470,42);
        }

        private void BuildHelpMenu()
        {
            subtitle.text="OPERATÖR KILAVUZU / KONTROLLER";
            ui.Text(body,"Dünyayı masandan koru.",80,10,1440,90,66,CommandUI.White,"display");
            HelpCard(80,135,"01 / MONİTÖRE YAKLAŞ","Globe","WASD ile yürü, fareyle bak. Monitöre yaklaş ve E ile ekranı kullan.");
            HelpCard(830,135,"02 / MÜDAHALE SEÇ","Budget","Kriz işaretine tıkla. Maliyet ve etkileri karşılaştır. Kart açıkken bütün oyun sayaçları durur.");
            HelpCard(80,376,"03 / DENGEYİ KORU","People","Halk veya iklim sıfıra ulaşırsa kaybedersin. Para sıfırken ücretsiz seçenekleri kullanabilirsin.");
            HelpCard(830,376,"04 / VARDİYAYI TAMAMLA","Clock","Her 60 saniyede bütçe gelir. Krizlere 40 saniyede cevap ver. Esc menüyü açar, E monitörden ayırır.");
            MenuButton(body,"‹  Geri",80,652,1440,Back);
        }

        private void HelpCard(float x,float y,string title,string icon,string copy)
        {
            var card=ui.Panel(body,title,x,y,690,215,new Color(.025f,.06f,.09f,.7f),true);
            ui.Icon(card,icon,24,24,38,CommandUI.Teal);
            ui.Text(card,title,82,22,584,48,30,CommandUI.White,"strong");
            ui.Text(card,copy,24,89,642,112,28,CommandUI.Muted);
        }

        private void BuildSettingsMenu()
        {
            subtitle.text="OPERATÖR TERCİHLERİ / AYARLAR";
            ui.Text(body,"Kontrolü kendine göre ayarla.",80,12,1440,100,66,CommandUI.White,"display");
            SettingsSlider("ANA SES",80,174,0,1,AudioListener.volume,
                value=>AudioListener.volume=value,value=>Mathf.RoundToInt(value*100)+" %");
            SettingsSlider("FARE HASSASİYETİ",80,344,.03f,.3f,player.MouseSensitivity,
                value=>player.MouseSensitivity=value,value=>(value/.1f).ToString("0.0")+" x");
            Text animationLabel=null;
            var toggle=ui.Button(body,"Animations",80,530,1440,78,()=>
            {
                AnimationsEnabled=!AnimationsEnabled;
                animationLabel.text="EKRAN HAREKETLERİ / "+(AnimationsEnabled?"AÇIK":"KAPALI");
                if(!AnimationsEnabled) backdrop.uvRect=new Rect(0,0,1,1);
            },new Color(.035f,.07f,.11f,.68f));
            animationLabel=ui.Text(toggle.transform,"EKRAN HAREKETLERİ / "+(AnimationsEnabled?"AÇIK":"KAPALI"),24,18,1392,44,30,CommandUI.White,"strong");
            MenuButton(body,"‹  Kaydet ve geri dön",80,652,1440,Back,true);
        }

        private void SettingsSlider(string label,float x,float y,float min,float max,float value,
            Action<float> changed,Func<float,string> format)
        {
            ui.Text(body,label,x,y,1000,42,30,CommandUI.White,"strong");
            var number=ui.Text(body,format(value),1220,y,300,42,29,CommandUI.Teal,"mono");
            number.alignment=TextAnchor.MiddleRight;
            var rect=ui.Rect(body,"Slider",x,y+54,1440,66);
            var hitArea=rect.gameObject.AddComponent<Image>();
            hitArea.color=new Color(0,0,0,.001f);
            var track=ui.Panel(rect,"Track",0,28,1440,8,CommandUI.Border);
            var fill=ui.Panel(track,"Fill",0,0,1440,8,CommandUI.Teal);
            fill.anchorMin=Vector2.zero;
            fill.anchorMax=Vector2.one;
            fill.sizeDelta=fill.anchoredPosition=Vector2.zero;
            var handleArea=ui.Rect(rect,"HandleArea",18,15,1404,36);
            var handle=ui.Panel(handleArea,"Handle",0,0,36,0,CommandUI.White);
            handle.pivot=new Vector2(.5f,.5f);
            handle.anchoredPosition=Vector2.zero;
            handle.GetComponent<Image>().sprite=ui.Circle;
            handle.GetComponent<Image>().type=Image.Type.Simple;
            handle.GetComponent<Image>().raycastTarget=true;
            var slider=rect.gameObject.AddComponent<Slider>();
            slider.fillRect=fill;
            slider.handleRect=handle;
            slider.targetGraphic=handle.GetComponent<Image>();
            slider.minValue=min;
            slider.maxValue=max;
            slider.value=value;
            slider.navigation=new Navigation{mode=Navigation.Mode.None};
            slider.onValueChanged.AddListener(next=>{changed(next);number.text=format(next);});
        }

        private void BuildResultsMenu()
        {
            bool won=session.State==SessionState.Won;
            bool climateLoss=session.Climate<=0;
            Color color=won?CommandUI.Teal:CommandUI.Red;
            subtitle.text=won?"VARDİYA SONU / BAŞARILI OPERASYON":"VARDİYA SONU / OPERASYON BAŞARISIZ";
            string title=won?"Dengeyi korudun.":climateLoss?"İklim kontrolden çıktı.":"Halkın güveni tükendi.";
            ui.Text(body,title,80,38,1440,132,88,color,"display");
            ui.Text(body,session.EndReason,80,204,1440,76,32,CommandUI.Muted);
            ResultMetric(80,"MÜDAHALE",crises.HandledCount.ToString("00"),CommandUI.Teal);
            ResultMetric(446,"KAÇIRILAN",crises.MissedCount.ToString("00"),CommandUI.Amber);
            ResultMetric(812,"KALAN BÜTÇE",session.Money.ToString(),CommandUI.White);
            ResultMetric(1178,"GÖREV SÜRESİ",FormatTime(session.ElapsedTime),CommandUI.White);
            ui.Text(body,$"HALK  {session.Happiness}/100     /     İKLİM  {session.Climate}/100",80,518,1440,46,30,CommandUI.Muted,"mono");
            MenuButton(body,won?"YENİ VARDİYA  →":"YENİDEN DENE  →",80,626,680,StartShift,true);
            MenuButton(body,"Ana menü",790,626,470,MainMenu);
            MenuButton(body,"Çıkış",1290,626,230,QuitGame);
        }

        private void ResultMetric(float x,string title,string value,Color color)
        {
            var card=ui.Panel(body,title,x,338,342,150,new Color(.025f,.06f,.09f,.7f),true);
            ui.Caption(card,title,22,18,298);
            ui.Number(card,value,22,57,298,83,68,color);
        }

        private static string FormatTime(float seconds)
        {
            int total=Mathf.CeilToInt(seconds);
            return $"{total/60:00}:{total%60:00}";
        }
    }
}
