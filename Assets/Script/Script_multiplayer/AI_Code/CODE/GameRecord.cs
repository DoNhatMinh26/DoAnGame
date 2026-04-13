namespace DoAnGame.Auth
{
    /// <summary>
    /// Model cho game history record
    /// Lưu trong Firebase: /gameHistory/{uid}/games/{gameId}
    /// </summary>
    [System.Serializable]
    public class GameRecord
    {
        public string gameId;              // unique game identifier
        public string mode;                // "solo" hoặc "multiplayer"
        public string difficulty;          // "easy", "normal", "hard"
        public int score;                  // điểm đạt được
        public int level;                  // màn chơi
        public string opponentName;        // tên đối thủ (chỉ nếu multiplayer)
        public string result;              // "win", "lose", hoặc "draw"
        public long timestamp;             // thời gian trận (Unix timestamp)
        public int correctAnswers;         // số câu đúng
        public int totalQuestions;         // tổng câu hỏi
        public int timeSpentSeconds;       // thời gian chơi (giây)

        public GameRecord() { }

        public GameRecord(
            string gameId,
            string mode,
            string difficulty,
            int score,
            int level,
            string result,
            int correctAnswers,
            int totalQuestions,
            int timeSpentSeconds,
            string opponentName = null)
        {
            this.gameId = gameId;
            this.mode = mode;
            this.difficulty = difficulty;
            this.score = score;
            this.level = level;
            this.opponentName = opponentName ?? "";
            this.result = result;
            this.correctAnswers = correctAnswers;
            this.totalQuestions = totalQuestions;
            this.timeSpentSeconds = timeSpentSeconds;
            this.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
