namespace Pixamor.COP31
{
    /// <summary>Her ülkede yükselen 6 temel çevre sorunu.</summary>
    public enum ProblemType
    {
        Tree = 0,       // Ağaç (Ormansızlaşma)
        Factory = 1,    // Fabrika (Emisyon)
        Pollution = 2,  // Çevre Kirliliği (Toprak/Su)
        Renewable = 3,  // Yenilenebilir Enerji (Kritik)
        Food = 4,       // Gıda Güvenliği (Kritik)
        ZeroWaste = 5   // Sıfır Atık (Kritik)
    }

    /// <summary>Müzakere ekranındaki 3 tercih.</summary>
    public enum ChoiceType
    {
        HardSanction = 0,       // Sert Yaptırım
        FinancialIncentive = 1, // Finansal Teşvik
        PartialConcession = 2   // Kısmi Taviz / Erteleme
    }

    /// <summary>Masadaki fiziksel bulmaca modülleri.</summary>
    public enum PuzzleType
    {
        Switches = 0, // Modül 1
        Numpad = 1,   // Modül 2
        Valve = 2,    // Modül 3
        Emergency = 3 // Modül 4
    }

    public enum NumpadProtocol
    {
        Budget = 0,
        ZeroWaste = 1
    }

    public enum GameState
    {
        Playing,
        Won,
        Lost
    }

    /// <summary>Bir tercihin masada onaylanması için gereken bulmaca isteği.</summary>
    public sealed class PuzzleRequest
    {
        public CountryNode Country;
        public ProblemType Problem;
        public ChoiceType Choice;
        public PuzzleType Puzzle;
    }

    /// <summary>Sorun ve tercihlerle ilgili sabit metin / kurallar.</summary>
    public static class ProblemInfo
    {
        public const int Count = 6;

        static readonly string[] names =
        {
            "Ağaç (Ormansızlaşma)",
            "Fabrika (Emisyon)",
            "Çevre Kirliliği",
            "Yenilenebilir Enerji *",
            "Gıda Güvenliği *",
            "Sıfır Atık *"
        };

        static readonly string[] shortNames = { "AĞAÇ", "FABRİKA", "KİRLİLİK", "ENERJİ*", "GIDA*", "ATIK*" };

        static readonly string[] crisisLines =
        {
            "Ormanlarımız yanıyor, kaçak kesim durdurulamıyor! Ağaçlandırma çöktü.",
            "Fabrika bacaları şehirlerimizi boğuyor, emisyon rekor kırdı!",
            "Nehirlerimiz ve topraklarımız zehirli atıkla kirlendi!",
            "Enerji krizi! Kömür santrallerini yeniden açmak zorundayız.",
            "Kuraklık hasadı yok etti, gıda fiyatları patladı!",
            "Çöp dağları taştı, geri dönüşüm sistemi iflas etti!"
        };

        public static string Name(ProblemType p) => names[(int)p];
        public static string ShortName(ProblemType p) => shortNames[(int)p];

        /// <summary>Yıldızlı / kritik öncelikli sorunlar.</summary>
        public static bool IsStarred(ProblemType p) =>
            p == ProblemType.Renewable || p == ProblemType.Food || p == ProblemType.ZeroWaste;

        public static float Weight(ProblemType p) => IsStarred(p) ? 1.5f : 1f;

        /// <summary>Başarılı çözümde küçük bir azalma alan ilişkili sorun.</summary>
        public static ProblemType Related(ProblemType p)
        {
            switch (p)
            {
                case ProblemType.Tree: return ProblemType.Food;
                case ProblemType.Food: return ProblemType.Tree;
                case ProblemType.Factory: return ProblemType.Renewable;
                case ProblemType.Renewable: return ProblemType.Factory;
                case ProblemType.Pollution: return ProblemType.ZeroWaste;
                default: return ProblemType.Pollution;
            }
        }

        public static string CrisisMessage(ProblemType p, string country, string president) =>
            $"\"{crisisLines[(int)p]}\"\n— {country} Başkanı {president}";

        public static string ChoiceName(ChoiceType c)
        {
            switch (c)
            {
                case ChoiceType.HardSanction: return "Sert Yaptırım";
                case ChoiceType.FinancialIncentive: return "Finansal Teşvik";
                default: return "Kısmi Taviz / Erteleme";
            }
        }

        public static string PuzzleName(PuzzleType t)
        {
            switch (t)
            {
                case PuzzleType.Switches: return "MODÜL 1: Şalter";
                case PuzzleType.Numpad: return "MODÜL 2: Tuş Takımı";
                case PuzzleType.Valve: return "MODÜL 3: Vana";
                default: return "MODÜL 4: Acil";
            }
        }

        public static string ProtocolName(NumpadProtocol p) =>
            p == NumpadProtocol.Budget ? "BÜTÇE TRANSFERİ" : "SIFIR ATIK";
    }
}
