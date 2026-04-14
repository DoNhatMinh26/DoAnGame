namespace DoAnGame.UI
{
    /// <summary>
    /// Lưu trạng thái mode/difficulty đang chọn để các UI sau cùng đọc.
    /// </summary>
    public static class GameModeContext
    {
        public static bool IsMultiplayer { get; private set; }
        public static int SelectedLevel { get; private set; } = 1;
        public static string SelectedDifficulty { get; private set; } = "Normal";

        public static void SetMode(bool multiplayer)
        {
            IsMultiplayer = multiplayer;
        }

        public static void SetLevel(int level)
        {
            SelectedLevel = level;
        }

        public static void SetDifficulty(string difficulty)
        {
            SelectedDifficulty = difficulty;
        }
    }
}
