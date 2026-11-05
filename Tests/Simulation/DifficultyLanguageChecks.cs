using System;
using System.Linq;
using System.Reflection;
using Pixamor;
partial class Check
{
 static bool HasWinningPlan(CrisisData[] data,int i,int money,int people,int climate)
 {
  if(i==data.Length)return true;
  int previous=i==0?0:(5+(i-1)*20)/60;
  money+=((5+i*20)/60-previous)*30;
  for(int option=0;option<2;option++){
   var response=data[i].Options[option];int p=Math.Min(100,people+response.HappinessChange);int c=Math.Min(100,climate+response.ClimateChange);
   if(money>=response.Cost && p>0 && c>0 && HasWinningPlan(data,i+1,money-response.Cost,p,c))return true;
  }
  return false;
 }
 static void RunDifficultyLanguageChecks()
 {
  CreateSimulation(out var game,out var manager);
  game.RestartSession(GameDifficulty.Easy);Assert(game.Money==90 && game.ResponseDuration==40,"Kolay mod: 90 bütçe, 40 saniye");
  Tick(game,5);var crisis=manager.Active[0];manager.Open(crisis);Tick(game,25);Assert(!crisis.IsResolved && !game.IsPaused,"Kolay modda kart açıkken gerçek zaman ve uzun cevap süresi");
  game.RestartSession(GameDifficulty.Hard);Assert(game.Money==60 && game.ResponseDuration==25 && manager.Active.Count==0,"Zor mod: 60 bütçe, 25 saniye ve temiz başlangıç");
  Tick(game,5);crisis=manager.Active[0];manager.Open(crisis);Tick(game,25);Assert(crisis.IsResolved && manager.MissedCount==1,"Zor modda açık kart 25 saniyede sona erer");
  manager.GetType().GetField("shuffleOnRestart",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,true);
  string previous=null;bool changed=false;
  for(int i=0;i<8;i++){
   game.RestartSession();var catalog=(CrisisData[])manager.GetType().GetField("catalog",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(manager);
   Assert(catalog.Length==16 && catalog.Select(x=>x.Title).Distinct().Count()==16,"Karıştırmada kayıp veya çift afet yok");
   Assert(HasWinningPlan(catalog,0,60,70,70),"Rastgele sırada Zor mod kazanılabilir");
   string order=string.Join("/",catalog.Select(x=>x.Title));if(previous!=null && previous!=order)changed=true;previous=order;
   Tick(game,5);var opening=manager.Active[0].Data.Title;game.RestartSession();Tick(game,5);Assert(manager.Active[0].Data.Title!=opening,"Art arda vardiyalarda ilk afet farklı");
  }
  Assert(changed,"Yeniden başlatmalar farklı afet sıraları üretiyor");
  GameLanguage.Current=UiLanguage.English;
  Assert(GameLanguage.Translate("KOLAY")=="EASY" && GameLanguage.Translate("ZOR")=="HARD","Zorluk seçimi İngilizce");
  foreach(var data in CrisisCatalog.Create()){
   Assert(GameLanguage.Translate(data.Title)!=data.Title && GameLanguage.Translate(data.Description)!=data.Description,"Afet başlığı ve açıklaması çevriliyor");
   foreach(var option in data.Options)Assert(GameLanguage.Translate(option.Title)!=option.Title && GameLanguage.Translate(option.Result)!=option.Result,"Seçenek ve sonuç çevriliyor");
  }
  Assert(GameLanguage.Translate("KALAN SÜRE / 12 sn")=="TIME LEFT / 12 sec","Canlı sayaç çevriliyor");
  GameLanguage.Current=UiLanguage.Turkish;Assert(GameLanguage.Translate("İklim dengesi")=="İklim dengesi","Türkçeye dönüş kaynak metni koruyor");
  foreach(var mode in new[]{GameDifficulty.Easy,GameDifficulty.Hard}){
   CreateSimulation(out game,out manager);game.RestartSession(mode);
   for(int i=0;i<16;i++){Tick(game,i==0?5:20);manager.Open(manager.Active[0]);Assert(manager.Choose(i==1||i==6||i==8?0:1),"Zorlukta dengeli karar: "+mode);}
   Tick(game,55);Assert(game.State==SessionState.Won && manager.HandledCount==16,"Tam vardiya kazanılabilir: "+mode);
  }
 }
}
