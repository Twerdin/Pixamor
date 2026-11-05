namespace Pixamor
{
    public enum GameDifficulty { Easy, Hard }
    public static class DifficultyRules
    {
        public static int Budget(GameDifficulty difficulty) => difficulty == GameDifficulty.Easy ? 90 : 60;
        public static float ResponseTime(GameDifficulty difficulty) => difficulty == GameDifficulty.Easy ? 40f : 25f;
        public static string Name(GameDifficulty difficulty) => difficulty == GameDifficulty.Easy ? "KOLAY" : "ZOR";
    }
}
