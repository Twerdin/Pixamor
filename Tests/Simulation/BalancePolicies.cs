using System;
using System.Reflection;
using Pixamor;

partial class Check
{
    private sealed class PolicyResult
    {
        public SessionState State;
        public int Money, Happiness, Climate, Handled, Missed, Locked, MinimumMoney, PeakActive;
        public float EndTime;
        public override string ToString() =>
            $"{State}: {EndTime} sn, para={Money}, halk={Happiness}, iklim={Climate}, " +
            $"müdahale={Handled}, kaçırılan={Missed}, bütçe engeli={Locked}";
    }

    // Gerçek GameSession ve CrisisManager ile 1 saniyelik karelerden bir vardiya oynatılır.
    private static PolicyResult SimulatePolicy(Func<int, CrisisData, int> choose, int delay = 0)
    {
        var session = new GameSession();
        UnityEngine.MonoBehaviour.Session = session;
        session.RestartSession();
        var manager = new CrisisManager();
        manager.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
        manager.GetType().GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
        var result = new PolicyResult { MinimumMoney = session.Money };

        for (int i = 0; i < 12 && session.State == SessionState.Running; i++)
        {
            while (session.ElapsedTime < 5 + 25 * i + delay && session.State == SessionState.Running)
            {
                Tick(session, 1);
                result.PeakActive = Math.Max(result.PeakActive, manager.Active.Count);
            }
            if (session.State != SessionState.Running) break;

            var crisis = manager.Active[0];
            if (crisis.Id != i) throw new Exception("Yanlış krize karar verildi.");
            int index = choose(i, crisis.Data);
            manager.Open(crisis);
            if (!session.CanAfford(crisis.Data.Options[index].Cost))
            {
                // Sabit pahalı politika, parası bitince ücretsiz seçeneğe düşer.
                result.Locked++;
                index = 2;
            }
            if (!manager.Choose(index)) throw new Exception("Geçerli karar uygulanamadı.");
            result.MinimumMoney = Math.Min(result.MinimumMoney, session.Money);
        }
        while (session.State == SessionState.Running) Tick(session, 1);
        result.State = session.State;
        result.EndTime = session.ElapsedTime;
        result.Money = session.Money;
        result.Happiness = session.Happiness;
        result.Climate = session.Climate;
        result.Handled = manager.HandledCount;
        result.Missed = manager.MissedCount;
        return result;
    }

    private static void RunBalancePolicies()
    {
        var first = SimulatePolicy((i, data) => 0);
        var second = SimulatePolicy((i, data) => 1);
        var free = SimulatePolicy((i, data) => 2);
        var mixed = SimulatePolicy((i, data) => i == 1 || i == 6 || i == 8 ? 0 : 1);
        var delayed = SimulatePolicy((i, data) => i == 1 || i == 6 || i == 8 ? 0 : 1, 30);
        Console.WriteLine("Pahalı tercih + bütçe yoksa ücretsiz: " + first);
        Console.WriteLine("Daima ikinci seçenek: " + second);
        Console.WriteLine("Daima ücretsiz: " + free);
        Console.WriteLine("Karma kararlar: " + mixed);
        Assert(first.State == SessionState.Lost && first.Locked > 0, "Pahalı politika bütçeyi tüketir ve kaybeder");
        Assert(second.State == SessionState.Lost, "Daima ikinci seçeneğe basmak kaybettirir");
        Assert(free.State == SessionState.Lost, "Daima ücretsiz seçenek kaybettirir");
        Assert(mixed.State == SessionState.Won && mixed.Handled == 12 && mixed.Missed == 0 && mixed.Locked == 0,
            "Karma rota bütün krizlere müdahale ederek kazanabilir");
        Assert(mixed.Happiness >= 25 && mixed.Climate >= 25 && mixed.MinimumMoney <= 10,
            "Karma rota kaynakları korurken bütçeyi kullanmayı gerektirir");
        Assert(delayed.State == SessionState.Won && delayed.Handled == 12 && delayed.PeakActive == 2,
            "İki kriz arasında öncelik seçilen gecikmeli rotayla da kazanılabilir");
        RunBoundaryChecks();

        // 2^12 ücretli rota: kazanmak tek bir ezberlenmiş diziye bağlı olmasın.
        int winningRoutes = 0;
        for (int mask = 0; mask < (1 << 12); mask++)
        {
            int route = mask;
            var run = SimulatePolicy((i, data) => (route >> i) & 1);
            if (run.State == SessionState.Won && run.Locked == 0 && run.Handled == 12) winningRoutes++;
        }
        Console.WriteLine("Tüm krizlere müdahale eden kazanılabilir ücretli rota: " + winningRoutes + " / 4096");
        Assert(winningRoutes > 1 && winningRoutes < 4096, "Birden fazla kazanılabilir rota var; her rota kazandırmıyor");

        foreach (var crisis in CrisisCatalog.Create())
        {
            var a = crisis.Options[0];
            var b = crisis.Options[1];
            bool aDominates = a.Cost <= b.Cost && a.HappinessChange >= b.HappinessChange && a.ClimateChange >= b.ClimateChange;
            bool bDominates = b.Cost <= a.Cost && b.HappinessChange >= a.HappinessChange && b.ClimateChange >= a.ClimateChange;
            Assert(!aDominates && !bDominates, crisis.Title + ": ücretli seçenekler farklı bedeller taşıyor");
        }
    }
}
