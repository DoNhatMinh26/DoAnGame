using UnityEngine;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// ScriptableObject chứa cấu hình game rules cho multiplayer battle.
    /// Tạo asset: Right-click → Create → Math Game → Multiplayer Game Rules
    /// </summary>
    [CreateAssetMenu(fileName = "MultiplayerGameRules", menuName = "Math Game/Multiplayer Game Rules")]
    public class GameRulesConfig : ScriptableObject
    {
        [Header("=== HỆ THỐNG MÁU ===")]
        [Tooltip("Số máu ban đầu của mỗi người chơi (mặc định: 3)")]
        [Range(1, 10)]
        public int startingHealth = 3; // Starting Health

        [Tooltip("Số máu mất khi trả lời sai (mặc định: 1)")]
        [Range(1, 5)]
        public int damageOnWrongAnswer = 1; // Damage On Wrong Answer

        [Tooltip("Số máu mất khi hết thời gian không trả lời (mặc định: 1)")]
        [Range(1, 5)]
        public int damageOnTimeout = 1; // Damage On Timeout

        [Header("=== HỆ THỐNG THỜI GIAN ===")]
        [Tooltip("Thời gian cho mỗi câu hỏi (giây) - Mặc định: 10 giây")]
        [Range(5f, 30f)]
        public float questionTimeLimit = 10f; // Question Time Limit

        [Tooltip("Thời gian chờ giữa các câu hỏi (giây) - Mặc định: 2 giây")]
        [Range(0.5f, 5f)]
        public float delayBetweenQuestions = 2f; // Delay Between Questions

        [Header("=== TĂNG ĐỘ KHÓ ===")]
        [Tooltip("Số câu trả lời đúng liên tiếp để tăng độ khó (mặc định: 2 câu)")]
        [Range(1, 10)]
        public int correctAnswersToIncreaseDifficulty = 2; // Correct Answers To Increase Difficulty

        [Tooltip("Mức tăng độ khó mỗi lần (level) - Mặc định: tăng 10 level")]
        [Range(1, 20)]
        public int difficultyIncrement = 10; // Difficulty Increment

        [Tooltip("Độ khó tối đa (level 1-100) - Mặc định: 100")]
        [Range(1, 100)]
        public int maxDifficulty = 100; // Max Difficulty

        [Header("=== HỆ THỐNG ĐIỂM SỐ (Dự phòng cho tương lai) ===")]
        [Tooltip("Điểm cộng khi trả lời đúng (mặc định: 10 điểm)")]
        public int pointsPerCorrectAnswer = 10; // Points Per Correct Answer

        [Tooltip("Điểm thưởng khi trả lời nhanh < 3 giây (mặc định: 5 điểm)")]
        public int speedBonusPoints = 5; // Speed Bonus Points

        [Tooltip("Điểm thưởng khi thắng trận (mặc định: 100 điểm)")]
        public int winBonusPoints = 100; // Win Bonus Points

        [Header("=== SINH ĐÁP ÁN ===")]
        [Tooltip("Số đáp án sai sinh ra cho mỗi câu hỏi (mặc định: 3 đáp án sai)")]
        [Range(2, 5)]
        public int numberOfWrongAnswers = 3; // Number Of Wrong Answers

        [Tooltip("Hệ số sai lệch đáp án sai (nhân với lớp) - Mặc định: 5")]
        [Range(1f, 10f)]
        public float wrongAnswerOffsetMultiplier = 5f; // Wrong Answer Offset Multiplier

        [Header("=== CHẾ ĐỘ CHƠI ===")]
        [Tooltip("Chế độ mất máu:\n• Người thắng gây sát thương: Ai đúng nhanh → đối thủ mất máu\n• Người thua tự mất máu: Ai sai → mình mất máu\n• Cả 2 sai đều mất máu: Cả 2 sai → cả 2 mất máu")]
        public DamageMode damageMode = DamageMode.WinnerDamagesLoser; // Damage Mode

        public enum DamageMode
        {
            [Tooltip("Người thắng gây sát thương cho người thua")]
            WinnerDamagesLoser, // Người thắng gây sát thương
            
            [Tooltip("Người thua tự mất máu")]
            LoserTakesDamage, // Người thua mất máu
            
            [Tooltip("Cả 2 người sai đều mất máu")]
            BothWrongBothDamaged // Cả 2 sai đều mất máu
        }

        /// <summary>
        /// Validate giá trị khi thay đổi trong Inspector
        /// </summary>
        private void OnValidate()
        {
            startingHealth = Mathf.Max(1, startingHealth);
            questionTimeLimit = Mathf.Max(1f, questionTimeLimit);
            maxDifficulty = Mathf.Clamp(maxDifficulty, 1, 100);
        }

        /// <summary>
        /// Tính thời gian timeout dưới dạng milliseconds (cho độ chính xác cao)
        /// </summary>
        public long GetTimeoutMilliseconds()
        {
            return (long)(questionTimeLimit * 1000);
        }

        /// <summary>
        /// Kiểm tra xem có nên tăng độ khó không
        /// </summary>
        public bool ShouldIncreaseDifficulty(int correctAnswersInRow)
        {
            return correctAnswersInRow >= correctAnswersToIncreaseDifficulty;
        }

        /// <summary>
        /// Tính độ khó mới sau khi tăng
        /// </summary>
        public int CalculateNewDifficulty(int currentDifficulty)
        {
            return Mathf.Min(maxDifficulty, currentDifficulty + difficultyIncrement);
        }
    }
}
