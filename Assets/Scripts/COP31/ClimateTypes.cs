namespace Pixamor.ClimateOS
{
    /// <summary>Her kıtada izlenen 6 temel ekolojik parametre.</summary>
    public enum ProblemType
    {
        Tree = 0,       // Ağaç / Orman Varlığı
        Factory = 1,    // Sanayi Emisyonu
        Pollution = 2,  // Çevre & Su Kirliliği
        Renewable = 3,  // Yenilenebilir Enerji
        Food = 4,       // Gıda & Su Güvenliği
        ZeroWaste = 5   // Sıfır Atık & Döngüsel Ekonomi
    }

    /// <summary>Müzakere ekranındaki 3 stratejik tercih tipi.</summary>
    public enum ChoiceType
    {
        HardSanction = 0,       // Sert Yaptırım / Sızıntı Tehdidi
        FinancialIncentive = 1, // Finansal / Kripto Lobi Teşviki
        PartialConcession = 2   // Kısmi Taviz / Erteleme
    }

    public enum GameState
    {
        Playing,
        Won,
        Lost
    }
}
