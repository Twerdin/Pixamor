using System;
using System.Reflection;
using Pixamor;

partial class Check
{
    private static void RunBoundaryChecks()
    {
        RunMenuPauseChecks();
        CreateSimulation(out var session,out var manager);
        // Bütçe ödemesi yaklaşırken kartı açık tut; diğer afetleri izole et.
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(CrisisManager).GetField("firstCrisisTime",flags).SetValue(manager,50f);
        Tick(session,50);manager.Open(manager.Active[0]);Tick(session,10);
        Assert(session.ElapsedTime==60 && session.Money==90 && manager.OpenCard!=null,"Kart açıkken düzenli bütçe ödemesi sürer");
        session.SetMenuPaused(true);float elapsed=session.ElapsedTime;float left=manager.SecondsLeft(manager.OpenCard);Tick(session,100);
        Assert(session.ElapsedTime==elapsed && manager.SecondsLeft(manager.OpenCard)==left,"Esc menüsünde açık kart ve oyun gerçekten durur");
        session.SetMenuPaused(false);Tick(session,20);
        Assert(manager.OpenCard==null && manager.MissedCount==1,"Menüden dönünce kartın süresi kaldığı yerden ilerler");
        session.RestartSession();
        // Son kriz tam vardiya sonunda biterse kayıp, kazanmayı geçersiz kılar.
        typeof(CrisisManager).GetField("firstCrisisTime",flags).SetValue(manager,30f);
        var catalog=(CrisisData[])typeof(CrisisManager).GetField("catalog",flags).GetValue(manager);
        foreach(var crisis in catalog)crisis.TimeoutResult=new InterventionData("Test",0,0,0,"Test",false);
        catalog[15].TimeoutResult=new InterventionData("Test",0,0,-100,"Test",false);
        for(int i=0;i<359;i++)Tick(session,1);
        Assert(session.State==SessionState.Running && manager.MissedCount==15,"Son saniyeden önce vardiya sürer");
        Tick(session,1);Assert(session.State==SessionState.Lost && session.Climate==0 && manager.MissedCount==16,"Bitişle aynı andaki zaman aşımı kaybettirir");
        int money=session.Money;Tick(session,60);Assert(session.Money==money && manager.MissedCount==16,"Bitiş sonucu ikinci kez uygulanmaz");
    }
    private static void RunMenuPauseChecks()
    {
        var session=new GameSession();
        UnityEngine.MonoBehaviour.Session=session;
        session.RestartSession();
        session.SetMenuPaused(true);
        Tick(session,120);
        Assert(session.ElapsedTime==0 && session.Money==60,"Ana menüde süre ve bütçe ilerlemez");
        Assert(!session.TryApplyIntervention(25,8,-5),"Menü açıkken müdahale uygulanamaz");
        session.ApplyResourceChanges(-100,-100);
        Assert(session.Happiness==70 && session.Climate==70,"Menü açıkken kaynaklar korunur");
        session.SetMenuPaused(false);
        Tick(session,5);
        session.SetPaused(true);
        session.SetMenuPaused(true);
        session.SetMenuPaused(false);
        Tick(session,100);
        Assert(session.ElapsedTime==5 && session.IsPaused,"Menüden dönüş elle duraklatılan saati açmaz");
        session.SetMenuPaused(true);
        session.SetPaused(false);
        Tick(session,100);
        Assert(session.ElapsedTime==5 && session.IsMenuPaused,"Diğer duraklatmanın kapanması menü duraklatmasını kaldıramaz");
        session.SetMenuPaused(false);
        Tick(session,1);
        Assert(session.ElapsedTime==6 && !session.IsPaused,"Her iki duraklatma kapanınca vardiya devam eder");
        session.SetMenuPaused(true);
        session.RestartSession();
        Assert(!session.IsMenuPaused && !session.IsPaused && session.ElapsedTime==0,"Yeni vardiya eski duraklatma durumlarını temizler");
    }
}
