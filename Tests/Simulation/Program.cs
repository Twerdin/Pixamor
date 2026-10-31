// Gerçek oyun scriptlerini çalıştırır. Yalnızca saf mantık için gerekli Unity API yüzeyi taklit edilir.
// Bu testler Unity Play, görüntü, ses veya fizik testinin yerine geçmez.
using System;
using System.Reflection;
using Pixamor;
namespace UnityEngine {
 public class MonoBehaviour { public static object Session; public T GetComponent<T>() => (T)Session; } public class RequireComponent : Attribute { public RequireComponent(Type t) {} } public struct Vector2 { public float x,y; public Vector2(float a,float b) {x=a;y=b;} }
 public class SerializeField : Attribute {}
 public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
 public class MinAttribute : Attribute { public MinAttribute(float f) {} }
 public class RangeAttribute : Attribute { public RangeAttribute(float a,float b) {} }
 public static class Time { public static float deltaTime; }
 public static class Mathf {
  public static int Max(int a,int b)=>Math.Max(a,b);
  public static float Max(float a,float b)=>Math.Max(a,b);
  public static float Min(float a,float b)=>Math.Min(a,b);
  public static int Clamp(int x,int a,int b)=>Math.Clamp(x,a,b);
 }
}
partial class Check {
 static void Call(GameSession g,string method)=>typeof(GameSession).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,null);
 static void Assert(bool ok,string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS: "+name); }
 static void Tick(GameSession g,float seconds) { UnityEngine.Time.deltaTime=seconds; Call(g,"Update"); Call(g,"LateUpdate"); }
 static void CreateSimulation(out GameSession g,out CrisisManager m) {
  g=new GameSession();UnityEngine.MonoBehaviour.Session=g;g.RestartSession();m=new CrisisManager();
  m.GetType().GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);
  m.GetType().GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);
 }
 static void RunCrises() {
  CreateSimulation(out var g,out var m);
  var data=CrisisCatalog.Create();Assert(data.Length==16,"16 yazılmış afet");
  foreach(var d in data)Assert(d.Options.Length==3 && d.Options[2].Cost==0 && d.ResponseDuration==30,"Üç seçenek, ücretsiz çıkış, 30 saniye");
  Tick(g,4);Assert(m.Active.Count==0,"5. saniyeden önce afet yok");Tick(g,1);
  var first=m.Active[0];Assert(m.Open(first) && !g.IsPaused,"Kart açılması zamanı durdurmaz");
  Tick(g,20);Assert(g.ElapsedTime==25 && m.Active.Count==2 && m.OpenCard==first && m.SecondsLeft(first)==10,"Açık kartta zaman ve ikinci afet ilerler");
  m.CloseCard();m.Open(m.Active[1]);var second=m.OpenCard;
  Tick(g,10);Assert(first.IsResolved && m.MissedCount==1 && m.OpenCard==second && g.Happiness==55,"Diğer afetin süresi dolunca mevcut kart korunur");
  Tick(g,20);Assert(second.IsResolved && m.OpenCard==null && m.MissedCount==2,"Açık kartın süresi dolunca ceza bir kez uygulanır ve kart kapanır");
  Assert(!m.Choose(0) && !m.Open(first),"Süresi dolan krize karar verilemez");
  int missed=m.MissedCount;Tick(g,1);Assert(m.MissedCount==missed,"Aynı kaçırma iki kez sayılmaz");
  g.RestartSession();Tick(g,5);g.TryApplyIntervention(60,0,0);m.Open(m.Active[0]);
  Assert(!m.Choose(0) && m.OpenCard!=null && g.Money==0,"Yetersiz bütçe kartı kapatmaz");
  Assert(m.Choose(2) && m.MissedCount==1,"Ücretsiz seçenek kullanılabilir");
  g.RestartSession();for(int i=0;i<16;i++){Tick(g,i==0?5:20);var crisis=m.Active[0];m.Open(crisis);int cost=crisis.Data.Options[i==1||i==6||i==8?0:1].Cost;int money=g.Money;Assert(m.Choose(i==1||i==6||i==8?0:1) && g.Money==money-cost,"Dengeli karar "+i);Assert(!m.Choose(0) && !m.Open(crisis),"Karar tek kez uygulanır "+i);}
  Tick(g,55);Assert(g.State==SessionState.Won && m.HandledCount==16 && m.MissedCount==0,"16 krizli vardiya kazanılabilir");
  g.RestartSession();Tick(g,360);Assert(g.State==SessionState.Lost && m.MissedCount>0,"Uzun karede süre aşımı ve kayıp işlenir");
  g.RestartSession();Tick(g,5);m.Open(m.Active[0]);g.RestartSession();Assert(m.OpenCard==null && m.Active.Count==0 && !g.IsPaused,"Yeniden başlatma açık krizi temizler");
 }
 static void Main() { RunBalancePolicies(); RunCrises();
  var g=new GameSession(); g.RestartSession();
  Assert(g.Money==60 && g.Happiness==70 && g.Climate==70 && g.RemainingTime==360,"Initial resources");
  g.SetPaused(true); Tick(g,65); Assert(g.RemainingTime==360 && g.Money==60,"Pause freezes time and income");
  Assert(g.TryApplyIntervention(25,8,-5) && g.Money==35 && g.Happiness==78 && g.Climate==65,"Decision works while paused");
  Assert(!g.TryApplyIntervention(40,-5,-5) && g.Money==35 && g.Happiness==78,"Unaffordable decision changes nothing");
  g.SetPaused(false); Tick(g,60); Assert(g.Money==65 && g.RemainingTime==300,"60 seconds pays 30");
  Tick(g,120); Assert(g.Money==125,"Multiple income intervals");
  g.RestartSession(); g.TryApplyIntervention(60,100,100); Assert(g.Money==0 && g.State==SessionState.Running && g.Happiness==100 && g.Climate==100,"Zero money allowed and resources capped");
  Assert(g.TryApplyIntervention(0,0,0),"Free option at zero money");
  g.ApplyResourceChanges(-100,0); Assert(g.State==SessionState.Lost,"Happiness loss");
  Assert(!g.TryApplyIntervention(0,10,10),"Ended session rejects decisions");
  g.RestartSession(); g.ApplyResourceChanges(0,-100); Assert(g.State==SessionState.Lost,"Climate loss");
  g.RestartSession(); Tick(g,500); Assert(g.State==SessionState.Won && g.ElapsedTime==360 && g.Money==240,"Shift victory and no income beyond end");
  g.RestartSession(); UnityEngine.Time.deltaTime=360; Call(g,"Update"); g.ApplyResourceChanges(-100,0); Call(g,"LateUpdate"); Assert(g.State==SessionState.Lost,"Loss overrides victory on final frame");
  g.RestartSession(); g.ApplyResourceChanges(-46,-46); Assert(g.HasHappinessWarning && g.HasClimateWarning,"Warnings below 25");
 }
}
