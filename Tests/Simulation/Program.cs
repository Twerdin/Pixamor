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
 static void RunCrises() {
  var g = new GameSession(); UnityEngine.MonoBehaviour.Session = g; g.RestartSession();
  var m = new CrisisManager(); typeof(CrisisManager).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);
  typeof(CrisisManager).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);
  int spawnNotifications=0; m.CrisisSpawned += _ => spawnNotifications++; var data=CrisisCatalog.Create(); Assert(data.Length==12,"12 authored crises");
  foreach(var d in data) Assert(d.Options.Length==3 && d.Options[2].Cost==0 && d.ResponseDuration==40,"Catalog: three options, free fallback, 40 seconds");
  Tick(g,4); Assert(m.Active.Count==0,"No crisis before 5 seconds"); Tick(g,1); Assert(m.Active.Count==1 && spawnNotifications==1,"First crisis sends one notification");
  var first=m.Active[0]; Assert(m.Open(first) && g.IsPaused,"Opening card pauses session");
  Tick(g,100); Assert(g.ElapsedTime==5 && m.SecondsLeft(first)==40 && g.Money==60 && spawnNotifications==1,"Card freezes crises, shift, budget and new notifications");
  m.CloseCard(); Assert(!g.IsPaused && m.Active.Count==1,"Closing card keeps crisis active");
  Tick(g,25); Assert(m.Active.Count==2 && m.SecondsLeft(first)==15,"Second crisis overlaps at 30 seconds");
  m.Open(first); Assert(m.Choose(0) && g.Money==35 && m.HandledCount==1 && !g.IsPaused,"Decision resolves and resumes");
  Assert(!m.Choose(0) && !m.Open(first) && g.Money==35,"Duplicate decision and resolved card rejected");
  g.RestartSession(); Assert(m.Active.Count==0 && m.HandledCount==0 && m.MissedCount==0,"Restart resets crises and counts");
  Tick(g,45); Assert(m.MissedCount==1 && g.Happiness==55 && g.Climate==65 && m.Active.Count==1,"Timeout applies exactly once at 45 seconds");
  g.RestartSession(); Tick(g,5); g.TryApplyIntervention(60,0,0); m.Open(m.Active[0]);
  Assert(!m.Choose(0) && m.OpenCard!=null && g.Money==0,"Unaffordable option leaves card open");
  Assert(m.Choose(2) && m.MissedCount==1 && g.Happiness==55 && !g.IsPaused,"Free choice at zero money counts as ignored");
  g.RestartSession();
  for(int i=0;i<12;i++) { Tick(g,i==0?5:25); Assert(m.Active.Count==1,"Scheduled crisis "+(i+1)); m.Open(m.Active[0]); Assert(m.Choose(i==1 || i==6 || i==8 ? 0 : 1),"Affordable mixed choice "+(i+1)); }
  Tick(g,80); Assert(g.State==SessionState.Won && m.HandledCount==12 && m.MissedCount==0,"Full shift can be won with balanced decisions");
  g.RestartSession();
  for(int i=0;i<12 && g.State==SessionState.Running;i++) {Tick(g,i==0?5:25); if(m.Active.Count>0) {m.Open(m.Active[0]); m.Choose(2);} }
  Assert(g.State==SessionState.Lost,"Always ignoring crises loses");
  g.RestartSession(); Tick(g,360); Assert(g.State==SessionState.Lost && m.MissedCount>0,"Catch-up processes expirations and loss");
  g.RestartSession(); Tick(g,5); m.Open(m.Active[0]); g.RestartSession(); Assert(!g.IsPaused && m.OpenCard==null && m.Active.Count==0,"Restart during card clears pause and selection");
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



