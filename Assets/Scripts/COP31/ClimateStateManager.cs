using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pixamor.COP31
{
    /// <summary>
    /// Ana oyun yöneticisi (GameManager): makro barlar (İklim, Nüfus, Mental, Para, Küresel Kriz),
    /// zaman çizelgesi döngüsü, kriz üretimi, tercih sonuçları, acil durumlar ve kazan/kaybet.
    /// </summary>
    public class ClimateStateManager : MonoBehaviour
    {
        public static ClimateStateManager Instance { get; private set; }

        [Header("Başlangıç")]
        public float startPopulation = 100f;
        public float startMental = 75f;
        public float startMoney = 1200f;
        public int startYear = 2026;
        public int totalMonths = 60;

        [Header("Eşikler")]
        [Tooltip("Tüm sorun barları bu değerin altına inince İklim %100 olur (KAZANMA).")]
        public float minBarForWin = 12f;
        [Tooltip("Ağırlıklı ortalama sorun bu değerdeyse İklim %0 kabul edilir.")]
        public float collapseBarReference = 60f;
        public float populationCritical = 35f;
        public float emergencyAngerThreshold = 85f;
        public int maxWithdrawals = 3;

        [Header("Aylık Simülasyon")]
        public float monthlyIncome = 60f;
        public float unresolvedCrisisAnger = 7f;
        public float unresolvedCrisisGameOver = 3f;
        public float unresolvedCrisisGrowth = 4f;

        [Header("Tercih 1: Sert Yaptırım")]
        public float hardReduce = 40f;
        public float hardAnger = 22f;
        public float hardMental = 6f;

        [Header("Tercih 2: Finansal Teşvik")]
        public float financialReduce = 35f;
        public float financialBaseCost = 150f;
        public float financialCostPerBar = 3f;
        public float financialAngerRelief = 4f;

        [Header("Tercih 3: Kısmi Taviz")]
        public float partialReduce = 8f;
        public float partialMoney = 120f;
        public float partialAngerRelief = 15f;
        [Range(0f, 1f)] public float partialSideEffectChance = 0.5f;
        public float partialSideEffectAmount = 12f;

        [Header("Bulmaca Sonucu")]
        public float failAnger = 12f;
        public float failGameOver = 7f;
        public float successGameOverRelief = 2f;
        public float relatedReduce = 8f;

        [Header("Çekilme / Acil Durum")]
        public float withdrawGameOver = 25f;
        public float emergencyRelief = 35f;

        static readonly string[] MonthNames =
            { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

        readonly List<CountryNode> countries = new List<CountryNode>();

        public float Climate { get; private set; }
        public float Population { get; private set; }
        public float Mental { get; private set; }
        public float Money { get; private set; }
        public float GameOverBar { get; private set; }
        public int MonthIndex { get; private set; }
        public bool YearMode { get; private set; }
        public int Withdrawals { get; private set; }
        public bool AllProblemsMinimal { get; private set; }
        public GameState State { get; private set; } = GameState.Playing;
        public string EndReason { get; private set; } = "";
        public string LastMessage { get; private set; } = "";
        public float LastMessageTime { get; private set; } = -100f;

        public IReadOnlyList<CountryNode> Countries => countries;
        public int MonthsLeft => Mathf.Max(0, totalMonths - MonthIndex);
        public float TimeProgress01 => totalMonths > 0 ? MonthIndex / (float)totalMonths : 1f;
        public string DateLabel => $"{MonthNames[MonthIndex % 12]} {startYear + MonthIndex / 12}";

        public int ActiveCrisisCount
        {
            get
            {
                int n = 0;
                foreach (var c in countries) if (c != null && c.InCrisis) n++;
                return n;
            }
        }

        void Awake()
        {
            Instance = this;
            Population = startPopulation;
            Mental = startMental;
            Money = startMoney;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterCountries(IEnumerable<CountryNode> nodes)
        {
            countries.Clear();
            countries.AddRange(nodes);
            Recalculate();
        }

        public void Post(string message)
        {
            LastMessage = message;
            LastMessageTime = Time.time;
            Debug.Log($"[COP31] {message}");
        }

        // ============================================================ Zaman Çizelgesi

        public void SetYearMode(bool yearMode)
        {
            YearMode = yearMode;
            Post(yearMode ? "Zaman modu: YIL (kol 12 ay ilerletir, daha çok kriz!)" : "Zaman modu: AY (kol 1 ay ilerletir)");
        }

        public bool CanAdvanceTime(bool postReason)
        {
            string reason = null;
            if (State != GameState.Playing) reason = "Oyun bitti. R ile yeniden başlat.";
            else if (DeskPuzzleController.Instance != null && DeskPuzzleController.Instance.IsMainPuzzleActive)
                reason = "Önce masadaki aktif protokolü tamamla!";

            if (reason == null) return true;
            if (postReason) Post(reason);
            return false;
        }

        /// <summary>Zaman kolu çekildiğinde çağrılır: 1 ay veya 1 yıl simüle eder ve yeni krizleri patlatır.</summary>
        public void AdvanceTime()
        {
            if (!CanAdvanceTime(true)) return;

            int months = YearMode ? 12 : 1;
            for (int i = 0; i < months && State == GameState.Playing; i++)
                SimulateMonth();

            if (State != GameState.Playing) return;

            int crisisCount = YearMode ? Random.Range(3, 6) : (Random.value < 0.7f ? 1 : 2);
            string spawned = SpawnCrises(crisisCount);

            Recalculate();
            CheckEndConditions();
            if (State != GameState.Playing) return;

            Post(string.IsNullOrEmpty(spawned)
                ? $"Zaman ilerledi: {DateLabel}."
                : $"{DateLabel} — ALARM! Yeni kriz: {spawned}");
        }

        void SimulateMonth()
        {
            MonthIndex++;
            Money += monthlyIncome;

            foreach (var c in countries)
            {
                if (c.InCrisis)
                {
                    c.ModifyBar(c.CrisisProblem, unresolvedCrisisGrowth);
                    GameOverBar += unresolvedCrisisGameOver;
                }
                else
                {
                    c.SetAnger(c.Anger - 2f);
                }

                // Yavaş sürünen çevresel bozulma.
                if (Random.value < 0.5f)
                    c.ModifyBar((ProblemType)Random.Range(0, ProblemInfo.Count), Random.Range(1f, 2.5f));
            }

            // Çözülmeyen krizler başkanları kızdırır (çekilme / acil durum tetikleyebilir).
            var snapshot = countries.ToArray();
            foreach (var c in snapshot)
            {
                if (State != GameState.Playing) return;
                if (c.InCrisis) AddAnger(c, unresolvedCrisisAnger);
            }

            float avgFood = AverageBar(ProblemType.Food);
            float avgPollution = (AverageBar(ProblemType.Factory) + AverageBar(ProblemType.Pollution)) * 0.5f;
            float avgAll = AverageAll();

            Population -= Mathf.Max(0f, avgFood - 35f) * 0.12f + Mathf.Max(0f, avgPollution - 45f) * 0.06f;
            if (avgFood < 25f) Population += 0.4f;

            Mental -= Mathf.Max(0f, avgAll - 40f) * 0.08f + ActiveCrisisCount * 0.5f;
            if (avgAll < 35f) Mental += 0.3f;

            Recalculate();
            CheckEndConditions();
        }

        string SpawnCrises(int count)
        {
            var pool = new List<CountryNode>();
            foreach (var c in countries) if (!c.InCrisis) pool.Add(c);

            var sb = new StringBuilder();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                var c = pool[idx];
                pool.RemoveAt(idx);

                var p = PickProblem(c);
                c.ModifyBar(p, Random.Range(10f, 18f));
                c.StartCrisis(p, ProblemInfo.CrisisMessage(p, c.CountryName, c.PresidentName));

                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{c.CountryName} ({ProblemInfo.ShortName(p)})");
            }

            if (sb.Length > 0) Sfx.Play(SfxType.Alarm);
            return sb.ToString();
        }

        /// <summary>Yüksek ve kritik (yıldızlı) sorunların patlama ihtimali daha yüksektir.</summary>
        static ProblemType PickProblem(CountryNode c)
        {
            var weights = new float[ProblemInfo.Count];
            float total = 0f;
            for (int i = 0; i < ProblemInfo.Count; i++)
            {
                var p = (ProblemType)i;
                weights[i] = (c.GetBar(p) + 10f) * (ProblemInfo.IsStarred(p) ? 1.3f : 1f);
                total += weights[i];
            }

            float r = Random.value * total;
            for (int i = 0; i < ProblemInfo.Count; i++)
            {
                r -= weights[i];
                if (r <= 0f) return (ProblemType)i;
            }
            return (ProblemType)(ProblemInfo.Count - 1);
        }

        // ============================================================ Tercihler

        public float FinancialCost(CountryNode c, ProblemType p) =>
            Mathf.Round((financialBaseCost + c.GetBar(p) * financialCostPerBar) / 10f) * 10f;

        /// <summary>Masadaki bulmaca bittiğinde DialogueAndChoiceSystem tarafından çağrılır.</summary>
        public void ApplyChoiceResult(PuzzleRequest req, bool success)
        {
            if (req == null || req.Country == null || State != GameState.Playing) return;
            var c = req.Country;
            var p = req.Problem;

            if (!success)
            {
                GameOverBar += failGameOver;
                c.FlashFail();
                Sfx.Play(SfxType.Fail);
                Post($"PROTOKOL HATASI! {c.CountryName} başkanı öfkeli, kriz sürüyor. (Sinir +{failAnger:0}, Kriz +{failGameOver:0})");
                AddAnger(c, failAnger);
                Recalculate();
                CheckEndConditions();
                return;
            }

            if (req.Choice == ChoiceType.FinancialIncentive)
            {
                float cost = FinancialCost(c, p);
                if (Money < cost)
                {
                    Sfx.Play(SfxType.Fail);
                    Post($"Bütçe yetersiz! Gerekli: {cost:0}$. Teşvik uygulanamadı.");
                    return;
                }
                Money -= cost;
            }

            c.ResolveCrisis();
            GameOverBar = Mathf.Max(0f, GameOverBar - successGameOverRelief);
            Sfx.Play(SfxType.Success);

            switch (req.Choice)
            {
                case ChoiceType.HardSanction:
                    c.ModifyBar(p, -hardReduce);
                    c.ModifyBar(ProblemInfo.Related(p), -relatedReduce);
                    Mental -= hardMental;
                    Post($"{c.CountryName}: Sert yaptırım uygulandı. {ProblemInfo.ShortName(p)} -{hardReduce:0}, Sinir +{hardAnger:0}, Mental -{hardMental:0}");
                    AddAnger(c, hardAnger);
                    break;

                case ChoiceType.FinancialIncentive:
                    c.ModifyBar(p, -financialReduce);
                    c.ModifyBar(ProblemInfo.Related(p), -relatedReduce);
                    AddAnger(c, -financialAngerRelief);
                    Post($"{c.CountryName}: Finansal teşvik aktarıldı. {ProblemInfo.ShortName(p)} -{financialReduce:0}");
                    break;

                default:
                    c.ModifyBar(p, -partialReduce);
                    Money += partialMoney;
                    Mental += 2f;
                    AddAnger(c, -partialAngerRelief);
                    string msg = $"{c.CountryName}: Kısmi taviz verildi. +{partialMoney:0}$, Sinir -{partialAngerRelief:0}";
                    if (Random.value < partialSideEffectChance)
                    {
                        var side = p == ProblemType.Food ? ProblemType.Pollution : ProblemType.Food;
                        c.ModifyBar(side, partialSideEffectAmount);
                        msg += $" — YAN ETKİ: {ProblemInfo.ShortName(side)} +{partialSideEffectAmount:0}";
                    }
                    Post(msg);
                    break;
            }

            Recalculate();
            CheckEndConditions();
        }

        // ============================================================ Sinir / Acil Durum

        /// <summary>Tüm sinir değişimleri buradan geçer: %100'de acil durum veya çekilme tetiklenir.</summary>
        public void AddAnger(CountryNode c, float amount)
        {
            if (c == null || State != GameState.Playing) return;
            c.SetAnger(c.Anger + amount);

            if (amount > 0f && c.Anger >= 100f && !c.EmergencyActive)
            {
                var desk = DeskPuzzleController.Instance;
                if (desk != null && desk.CanStartEmergency)
                {
                    c.SetAnger(99f);
                    TriggerEmergency(c);
                }
                else
                {
                    Withdraw(c);
                }
                return;
            }

            TryTriggerEmergencies();
        }

        void TryTriggerEmergencies()
        {
            var desk = DeskPuzzleController.Instance;
            if (desk == null || State != GameState.Playing || !desk.CanStartEmergency) return;

            foreach (var c in countries)
            {
                if (!c.EmergencyActive && c.Anger >= emergencyAngerThreshold)
                {
                    TriggerEmergency(c);
                    return;
                }
            }
        }

        void TriggerEmergency(CountryNode c)
        {
            var desk = DeskPuzzleController.Instance;
            if (desk == null) return;
            c.EmergencyActive = true;
            if (desk.StartEmergency(c, ok => OnEmergencyResult(c, ok)))
            {
                Post($"ACİL! {c.CountryName} başkanı masayı terk etmek üzere! MODÜL 4 düğmesine bas!");
            }
            else
            {
                c.EmergencyActive = false;
            }
        }

        void OnEmergencyResult(CountryNode c, bool ok)
        {
            c.EmergencyActive = false;
            if (State != GameState.Playing) return;

            if (ok)
            {
                c.SetAnger(c.Anger - emergencyRelief);
                Sfx.Play(SfxType.Success);
                Post($"{c.CountryName} başkanı yatıştırıldı. (Sinir -{emergencyRelief:0})");
            }
            else
            {
                Withdraw(c);
            }

            TryTriggerEmergencies();
        }

        void Withdraw(CountryNode c)
        {
            Withdrawals++;
            GameOverBar += withdrawGameOver;
            c.Withdraw();
            Sfx.Play(SfxType.Fail);
            Post($"{c.CountryName} ANLAŞMADAN ÇEKİLDİ! Küresel Kriz +{withdrawGameOver:0} ({Withdrawals}/{maxWithdrawals})");
            Recalculate();

            if (Withdrawals >= maxWithdrawals)
                EndGame(GameState.Lost, $"{Withdrawals} ülke başkanı sinirden zirveyi terk etti. Anlaşma çöktü!");
            else
                CheckEndConditions();
        }

        // ============================================================ Hesaplama / Bitiş

        float AverageBar(ProblemType p)
        {
            if (countries.Count == 0) return 0f;
            float s = 0f;
            foreach (var c in countries) s += c.GetBar(p);
            return s / countries.Count;
        }

        float AverageAll()
        {
            if (countries.Count == 0) return 0f;
            float s = 0f;
            foreach (var c in countries) s += c.AverageBar;
            return s / countries.Count;
        }

        void Recalculate()
        {
            float sum = 0f, weightSum = 0f;
            bool allMinimal = countries.Count > 0;

            foreach (var c in countries)
            {
                for (int i = 0; i < ProblemInfo.Count; i++)
                {
                    var p = (ProblemType)i;
                    float w = ProblemInfo.Weight(p);
                    float v = c.GetBar(p);
                    sum += v * w;
                    weightSum += w;
                    if (v > minBarForWin) allMinimal = false;
                }
            }

            float avg = weightSum > 0f ? sum / weightSum : 0f;
            float climate = 100f * (1f - (avg - minBarForWin) / Mathf.Max(1f, collapseBarReference - minBarForWin));
            climate = Mathf.Clamp(climate, 0f, 100f);

            Climate = climate;
            AllProblemsMinimal = allMinimal || climate >= 100f;
            Population = Mathf.Clamp(Population, 0f, 100f);
            Mental = Mathf.Clamp(Mental, 0f, 100f);
            Money = Mathf.Max(0f, Money);
            GameOverBar = Mathf.Clamp(GameOverBar, 0f, 100f);
        }

        void CheckEndConditions()
        {
            if (State != GameState.Playing) return;

            if (Climate >= 100f || AllProblemsMinimal)
                EndGame(GameState.Won, "İklim Dengesi %100 seviyesine ulaştı! Küresel iklim krizi tamamen çözüldü, Ütopya kuruldu!");
            else if (GameOverBar >= 100f)
                EndGame(GameState.Lost, "Küresel Kriz Barı doldu! Dünya kaosa sürüklendi.");
            else if (Population <= populationCritical)
                EndGame(GameState.Lost, "İnsan popülasyonu kritik seviyenin altına düştü. Gıda ve kirlilik krizi insanlığı çökertti.");
            else if (Mental <= 0f)
                EndGame(GameState.Lost, "İnsanlığın mental direnci sıfırlandı. Halk ayaklandı ve seni görevden aldı!");
            else if (MonthIndex >= totalMonths)
                EndGame(GameState.Lost, "Zaman çizelgesi doldu ve iklim hedefine ulaşılamadı.");
        }

        void EndGame(GameState result, string reason)
        {
            if (State != GameState.Playing) return;
            State = result;
            EndReason = reason;
            if (result == GameState.Won) Climate = 100f;
            Post(reason);

            if (DeskPuzzleController.Instance != null) DeskPuzzleController.Instance.AbortAll();
            if (DialogueAndChoiceSystem.Instance != null) DialogueAndChoiceSystem.Instance.ShowGrid();
            foreach (var c in countries) c.EmergencyActive = false;

            Sfx.Play(result == GameState.Won ? SfxType.Success : SfxType.Fail);
        }
    }
}
