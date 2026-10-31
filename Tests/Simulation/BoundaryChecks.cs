using System;
using System.Reflection;
using Pixamor;

partial class Check
{
    private static void RunBoundaryChecks()
    {
        RunMenuPauseChecks();
        var session = new GameSession();
        UnityEngine.MonoBehaviour.Session = session;
        session.RestartSession();
        var manager = new CrisisManager();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(CrisisManager).GetMethod("Awake", flags).Invoke(manager, null);
        typeof(CrisisManager).GetMethod("OnEnable", flags).Invoke(manager, null);
        for (int i = 0; i < 30; i++) Tick(session, 1);
        manager.Open(manager.Active[0]);
        Tick(session, 100);
        Assert(session.ElapsedTime == 30 && manager.Active.Count == 2 &&
            manager.SecondsLeft(manager.Active[0]) == 15 && manager.SecondsLeft(manager.Active[1]) == 40,
            "Kart açıkken iki krizin de süresi donar");
        manager.CloseCard();
        while (session.State == SessionState.Running) Tick(session, 1);
        Assert(session.State == SessionState.Lost && session.ElapsedTime == 145 &&
            manager.MissedCount == 5 && manager.HandledCount == 0,
            "Hiçbir karta dokunmadan süre aşımıyla kayıp ve sayılar doğru");

        session.RestartSession();
        // Başka krizlerden zarar vermeden tam 360. saniyedeki zaman aşımını izole et.
        typeof(CrisisManager).GetField("firstCrisisTime", flags).SetValue(manager, 45f);
        var catalog = (CrisisData[])typeof(CrisisManager).GetField("catalog", flags).GetValue(manager);
        foreach (var crisis in catalog)
            crisis.TimeoutResult = new InterventionData("Test", 0, 0, 0, "Test", false);
        catalog[11].TimeoutResult = new InterventionData("Test", 0, 0, -100, "Test", false);
        for (int i = 0; i < 359; i++) Tick(session, 1);
        Assert(session.State == SessionState.Running && manager.MissedCount == 11,
            "Son saniyeden önce vardiya devam ediyor");
        Tick(session, 1);
        Assert(session.State == SessionState.Lost && session.Climate == 0 && manager.MissedCount == 12,
            "Vardiya bitişindeki gerçek kriz süre aşımı kazanmayı geçersiz kılar");
        int money = session.Money;
        Tick(session, 60);
        Assert(session.Money == money && manager.MissedCount == 12,
            "Bitişten sonra bütçe ve kriz cezaları tekrar uygulanmaz");
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
        Assert(session.ElapsedTime==5 && session.IsPaused,"Duraklatma menüsünden dönünce açık kart zamanı dondurmaya devam eder");
        session.SetMenuPaused(true);
        session.SetPaused(false);
        Tick(session,100);
        Assert(session.ElapsedTime==5 && session.IsMenuPaused,"Kartın kapanması menü duraklatmasını kaldıramaz");
        session.SetMenuPaused(false);
        Tick(session,1);
        Assert(session.ElapsedTime==6 && !session.IsPaused,"Her iki duraklatma kapanınca vardiya devam eder");
        session.SetMenuPaused(true);
        session.RestartSession();
        Assert(!session.IsMenuPaused && !session.IsPaused && session.ElapsedTime==0,"Yeni vardiya eski duraklatma durumlarını temizler");
    }
}
