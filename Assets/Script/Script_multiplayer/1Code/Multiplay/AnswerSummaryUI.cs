using UnityEngine;
using TMPro;
using System.Collections;
using DoAnGame.Multiplayer;

namespace DoAnGame.UI
{
    /// <summary>
    /// Quản lý UI tổng kết đáp án giữa các câu hỏi
    /// 
    /// LOGIC:
    /// 1. Question Time (10s): 
    ///    - TextTrangThaiDapAn1/2 HIDDEN (không hiển thị)
    ///    - resultText (TrangThaiWin) HIDDEN (không hiển thị)
    /// 2. Summary Time (3s): 
    ///    - TextTrangThaiDapAn1/2 VISIBLE + kết quả + response time (ms)
    ///    - resultText (TrangThaiWin) VISIBLE + "Người chơi X đúng/sai!"
    /// 3. Sau Summary Time:
    ///    - Tất cả text bị ẨN và RESET về trạng thái ban đầu
    /// 
    /// So sánh theo miligiây (ms):
    /// - Cả 2 đúng → So sánh ms (ai nhanh hơn thắng)
    /// - 1 đúng 1 sai → Người đúng thắng
    /// - Cả 2 sai → Cả 2 mất máu
    /// - 1 timeout → Người còn lại thắng
    /// </summary>
    public class AnswerSummaryUI : MonoBehaviour
    {
        [Header("=== ANSWER DISPLAY ===")]
        [SerializeField] private TextMeshProUGUI textTrangThaiDapAn1; // Player 1 answer (HIDDEN during question time)
        [SerializeField] private TextMeshProUGUI textTrangThaiDapAn2; // Player 2 answer (HIDDEN during question time)
        
        [Header("=== STATUS & TIMER (SHARED) ===")]
        [SerializeField] private TextMeshProUGUI timerText; // Dùng chung từ TimerPanel
        [SerializeField] private TextMeshProUGUI trangThaiText; // Text hiển thị trạng thái
        
        [Header("=== RESULT DISPLAY ===")]
        [SerializeField] private TextMeshProUGUI resultText; // Text hiển thị kết quả (optional)
        
        // ✅ Ẩn settings - tự động lấy từ GameRules
        // [Header("=== SETTINGS ===")]
        // Không cần set trong Inspector nữa - tự động lấy từ DefaultGameRules.delayBetweenQuestions
        // Không có min/max hard-code - hoàn toàn linh hoạt theo GameRules

        private NetworkedMathBattleManager battleManager;
        private Coroutine summaryCoroutine;
        private Coroutine questionTimerCoroutine;
        private bool isSummaryActive = false;
        private bool isQuestionActive = false;
        private int initRetryCount = 0;
        private const int MAX_INIT_RETRIES = 10; // Retry tối đa 10 lần (10 giây)
        private bool matchHasEnded = false; // Flag để track match đã kết thúc
        private float actualSummaryDuration; // Thời gian tổng kết thực tế (từ GameRules hoặc default)

        // Enum để quản lý trạng thái
        public enum TimerState
        {
            QuestionTime,      // Thời gian trả lời câu hỏi
            SummaryTime        // Thời gian thống kê đáp án
        }

        private TimerState currentState = TimerState.QuestionTime;

        private void Start()
        {
            battleManager = NetworkedMathBattleManager.Instance;
            
            if (battleManager == null)
            {
                Debug.LogError("[AnswerSummaryUI] BattleManager not found!");
                // Retry sau 1 giây
                Invoke(nameof(InitializeUI), 1f);
                return;
            }

            InitializeUI();
        }

        private void OnEnable()
        {
            // ✅ Reset state mỗi khi GameplayPanel được show lại (lần chơi mới)
            matchHasEnded = false;
            isSummaryActive = false;
            isQuestionActive = false;
            
            // Re-subscribe nếu battleManager đã có
            if (battleManager != null)
            {
                battleManager.OnAnswerResultReceived -= HandleAnswerResult;
                battleManager.OnQuestionGenerated    -= HandleQuestionGenerated;
                battleManager.OnMatchEnded           -= HandleMatchEnded;
                battleManager.OnAnswerResultReceived += HandleAnswerResult;
                battleManager.OnQuestionGenerated    += HandleQuestionGenerated;
                battleManager.OnMatchEnded           += HandleMatchEnded;
            }
        }

        private void OnDisable()
        {
            // ✅ Cancel pending Invoke và stop coroutines khi panel bị ẩn
            CancelInvoke(nameof(InitializeUI));
            if (summaryCoroutine != null)     { StopCoroutine(summaryCoroutine);     summaryCoroutine = null; }
            if (questionTimerCoroutine != null){ StopCoroutine(questionTimerCoroutine); questionTimerCoroutine = null; }
        }

        private void InitializeUI()
        {
            battleManager = NetworkedMathBattleManager.Instance;
            
            if (battleManager == null)
            {
                initRetryCount++;
                if (initRetryCount < MAX_INIT_RETRIES)
                {
                    Debug.LogWarning($"[AnswerSummaryUI] BattleManager not found yet (retry {initRetryCount}/{MAX_INIT_RETRIES}). Retrying in 1s...");
                    Invoke(nameof(InitializeUI), 1f);
                    return;
                }
                else
                {
                    Debug.LogError("[AnswerSummaryUI] BattleManager not found after max retries!");
                    return;
                }
            }

            // Reset retry count
            initRetryCount = 0;

            // ✅ AUTO-LOAD summary duration từ GameRules (không có giới hạn min/max)
            actualSummaryDuration = battleManager.gameRules.delayBetweenQuestions;
            Debug.Log($"[AnswerSummaryUI] Auto-loaded summary duration from GameRules: {actualSummaryDuration}s");

            // Subscribe to events
            battleManager.OnAnswerResultReceived += HandleAnswerResult;
            battleManager.OnQuestionGenerated += HandleQuestionGenerated;
            battleManager.OnMatchEnded += HandleMatchEnded; // ✅ Subscribe to match end event
            
            // Set trạng thái ban đầu
            SetTrangThaiQuestionTime();
            
            // Đảm bảo TextTrangThaiDapAn1/2 và resultText bị ẩn lúc đầu
            HideAnswerTexts();
            
            Debug.Log("[AnswerSummaryUI] ✅ Initialized");
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.OnAnswerResultReceived -= HandleAnswerResult;
                battleManager.OnQuestionGenerated -= HandleQuestionGenerated;
                battleManager.OnMatchEnded -= HandleMatchEnded; // ✅ Unsubscribe
            }
        }

        /// <summary>
        /// Set trạng thái: Thời gian trả lời câu hỏi
        /// </summary>
        private void SetTrangThaiQuestionTime()
        {
            currentState = TimerState.QuestionTime;
            if (trangThaiText != null)
            {
                trangThaiText.text = "Thời gian trả lời câu hỏi";
            }
            Debug.Log("[AnswerSummaryUI] State: Question Time");
        }

        /// <summary>
        /// Set trạng thái: Thời gian thống kê đáp án
        /// </summary>
        private void SetTrangThaiSummaryTime()
        {
            currentState = TimerState.SummaryTime;
            if (trangThaiText != null)
            {
                // ✅ Kiểm tra xem match đã kết thúc chưa
                if (matchHasEnded)
                {
                    trangThaiText.text = "Đã có kết quả tổng kết";
                }
                else
                {
                    trangThaiText.text = "Thời gian thống kê đáp án";
                }
            }
            Debug.Log($"[AnswerSummaryUI] State: Summary Time (matchEnded={matchHasEnded})");
        }

        /// <summary>
        /// Xử lý khi match kết thúc (có người hết máu)
        /// </summary>
        private void HandleMatchEnded(int winnerId, int winnerHealth)
        {
            matchHasEnded = true;
            Debug.Log($"[AnswerSummaryUI] Match ended! Winner={winnerId}, Health={winnerHealth}");
            
            // Cập nhật text ngay lập tức nếu đang trong summary time
            if (currentState == TimerState.SummaryTime && trangThaiText != null)
            {
                trangThaiText.text = "Đã có kết quả tổng kết";
            }
        }

        /// <summary>
        /// Ẩn TextTrangThaiDapAn1/2 và resultText (dùng trong Question Time)
        /// </summary>
        private void HideAnswerTexts()
        {
            if (textTrangThaiDapAn1 != null)
            {
                textTrangThaiDapAn1.gameObject.SetActive(false);
            }
            
            if (textTrangThaiDapAn2 != null)
            {
                textTrangThaiDapAn2.gameObject.SetActive(false);
            }
            
            // ✅ Ẩn resultText (TrangThaiWin) trong Question Time
            if (resultText != null)
            {
                resultText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Hiển thị TextTrangThaiDapAn1/2 và resultText (dùng trong Summary Time)
        /// </summary>
        private void ShowAnswerTexts()
        {
            if (textTrangThaiDapAn1 != null)
            {
                textTrangThaiDapAn1.gameObject.SetActive(true);
            }
            
            if (textTrangThaiDapAn2 != null)
            {
                textTrangThaiDapAn2.gameObject.SetActive(true);
            }
            
            // ✅ Hiển thị resultText (TrangThaiWin) trong Summary Time
            if (resultText != null)
            {
                resultText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Gọi khi có câu hỏi mới
        /// </summary>
        private void HandleQuestionGenerated(string question, int[] choices)
        {
            Debug.Log($"[AnswerSummaryUI] New question generated: {question}");

            // Dừng summary coroutine nếu đang chạy
            if (summaryCoroutine != null)
            {
                StopCoroutine(summaryCoroutine);
                summaryCoroutine = null;
            }

            // Dừng question timer cũ nếu có
            if (questionTimerCoroutine != null)
            {
                StopCoroutine(questionTimerCoroutine);
            }

            // Ẩn đáp án
            HideAnswerTexts();

            // Set trạng thái Question Time
            SetTrangThaiQuestionTime();

            // ❌ XÓA: KHÔNG tự động start timer nữa
            // Timer sẽ được start từ UIMultiplayerBattleController.StartQuestionTimer()
            // questionTimerCoroutine = StartCoroutine(QuestionTimerRoutine());
            
            Debug.Log("[AnswerSummaryUI] Question generated, waiting for timer start signal from BattleController");
        }

        /// <summary>
        /// ✅ MỚI: Start timer từ bên ngoài (gọi từ UIMultiplayerBattleController)
        /// </summary>
        public void StartQuestionTimer()
        {
            if (questionTimerCoroutine != null)
            {
                StopCoroutine(questionTimerCoroutine);
            }
            questionTimerCoroutine = StartCoroutine(QuestionTimerRoutine());
            Debug.Log("[AnswerSummaryUI] ✅ Timer started from external call");
        }

        /// <summary>
        /// Đếm ngược thời gian Question Time (lấy từ GameRules)
        /// </summary>
        private IEnumerator QuestionTimerRoutine()
        {
            isQuestionActive = true;
            
            // ✅ Lấy thời gian từ GameRules thay vì hard-code
            float questionTime = battleManager.gameRules.questionTimeLimit;
            float elapsed = 0f;

            Debug.Log($"[AnswerSummaryUI] Starting question countdown: {questionTime}s");

            while (elapsed < questionTime)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Max(0, questionTime - elapsed);

                if (timerText != null)
                {
                    timerText.text = $"{Mathf.CeilToInt(remaining)}s";
                }

                yield return null;
            }

            // Hết thời gian Question Time
            if (timerText != null)
            {
                timerText.text = "0s";
            }

            isQuestionActive = false;
            Debug.Log("[AnswerSummaryUI] Question Time ended");
        }

        /// <summary>
        /// Gọi khi có kết quả đáp án
        /// </summary>
        private void HandleAnswerResult(int winnerId, bool correct, long player1ResponseTimeMs, long player2ResponseTimeMs, int player1Answer, int player2Answer)
        {
            Debug.Log($"[AnswerSummaryUI] Answer result: Winner={winnerId}, Correct={correct}, P1Time={player1ResponseTimeMs}ms, P2Time={player2ResponseTimeMs}ms, P1Answer={player1Answer}, P2Answer={player2Answer}");

            // Dừng question timer
            if (questionTimerCoroutine != null)
            {
                StopCoroutine(questionTimerCoroutine);
                questionTimerCoroutine = null;
            }

            // Dừng summary coroutine cũ nếu có
            if (summaryCoroutine != null)
            {
                StopCoroutine(summaryCoroutine);
            }

            // Bắt đầu giai đoạn tổng kết
            summaryCoroutine = StartCoroutine(ShowSummaryRoutine(winnerId, correct, player1ResponseTimeMs, player2ResponseTimeMs, player1Answer, player2Answer));
        }

        /// <summary>
        /// Hiển thị tổng kết đáp án
        /// </summary>
        private IEnumerator ShowSummaryRoutine(int winnerId, bool correct, long player1ResponseTimeMs, long player2ResponseTimeMs, int player1Answer, int player2Answer)
        {
            isSummaryActive = true;
            
            // Thay đổi trạng thái sang Summary Time NGAY LẬP TỨC
            SetTrangThaiSummaryTime();
            
            // Hiển thị đáp án người chơi chọn
            ShowAnswerTexts();
            DisplayAnswers(player1Answer, player2Answer, player1ResponseTimeMs, player2ResponseTimeMs);
            
            // Hiển thị kết quả
            DisplayResult(winnerId, correct, player1ResponseTimeMs, player2ResponseTimeMs, player1Answer, player2Answer);
            
            // ✅ Đếm ngược timer từ actualSummaryDuration xuống 0 (auto từ GameRules)
            float duration = actualSummaryDuration;
            float elapsed = 0f;
            
            Debug.Log($"[AnswerSummaryUI] Starting summary countdown: {duration}s");
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Max(0, duration - elapsed);
                
                if (timerText != null)
                {
                    timerText.text = $"{Mathf.CeilToInt(remaining)}s";
                }
                
                yield return null;
            }
            
            // Kết thúc tổng kết
            isSummaryActive = false;
            ClearSummary();
            
            Debug.Log("[AnswerSummaryUI] Summary ended, ready for next question");
        }

        /// <summary>
        /// Hiển thị đáp án người chơi chọn + thời gian trả lời (trong Summary Time)
        /// Format: "Đáp án người chơi 1 chọn là: 5 (3.2450s)"
        /// </summary>
        private void DisplayAnswers(int player1Answer, int player2Answer, long player1ResponseTimeMs, long player2ResponseTimeMs)
        {
            // Convert milliseconds to seconds with 4 decimal places
            float player1ResponseTimeSec = player1ResponseTimeMs / 1000f;
            float player2ResponseTimeSec = player2ResponseTimeMs / 1000f;
            
            if (textTrangThaiDapAn1 != null)
            {
                string p1Text = player1Answer == -1 ? "(Không trả lời)" : player1Answer.ToString();
                string p1TimeText = player1Answer == -1 ? "" : $" ({player1ResponseTimeSec:F4}s)";
                textTrangThaiDapAn1.text = $"Đáp án người chơi 1 chọn là: <color=yellow>{p1Text}</color>{p1TimeText}";
                Debug.Log($"[AnswerSummaryUI] P1 answer: {p1Text}{p1TimeText}");
            }
            
            if (textTrangThaiDapAn2 != null)
            {
                string p2Text = player2Answer == -1 ? "(Không trả lời)" : player2Answer.ToString();
                string p2TimeText = player2Answer == -1 ? "" : $" ({player2ResponseTimeSec:F4}s)";
                textTrangThaiDapAn2.text = $"Đáp án người chơi 2 chọn là: <color=yellow>{p2Text}</color>{p2TimeText}";
                Debug.Log($"[AnswerSummaryUI] P2 answer: {p2Text}{p2TimeText}");
            }
        }

        /// <summary>
        /// Hiển thị kết quả đúng/sai cho cả 2 người chơi
        /// 
        /// Logic so sánh theo miligiây:
        /// - winnerId = 0: Player 1 thắng (đúng hoặc nhanh hơn)
        /// - winnerId = 1: Player 2 thắng (đúng hoặc nhanh hơn)
        /// - winnerId = -1: Cả 2 sai
        /// - winnerId = -2: Hòa (cả 2 đúng cùng thời gian)
        /// </summary>
        private void DisplayResult(int winnerId, bool correct, long player1ResponseTimeMs, long player2ResponseTimeMs, int player1Answer, int player2Answer)
        {
            string resultText = "";
            
            // Kiểm tra xem đáp án có đúng không (dựa vào CorrectAnswer từ BattleManager)
            int correctAnswer = battleManager.CorrectAnswer.Value;
            bool player1Correct = (player1Answer == correctAnswer);
            bool player2Correct = (player2Answer == correctAnswer);
            
            Debug.Log($"[AnswerSummaryUI] Correct answer: {correctAnswer}, P1: {player1Answer} ({player1Correct}), P2: {player2Answer} ({player2Correct})");
            
            if (winnerId == -2)
            {
                // Hòa - cả 2 đúng cùng thời gian
                resultText = "<color=cyan>Hòa! Cả 2 trả lời đúng cùng lúc!</color>";
            }
            else if (winnerId == 0)
            {
                // Player 1 thắng
                if (player1Correct && player2Correct)
                {
                    // Cả 2 đúng, Player 1 nhanh hơn
                    resultText = "<color=green>Cả 2 đều đúng!</color>\n<color=yellow>Người chơi 1 nhanh hơn!</color>";
                }
                else if (player1Correct && !player2Correct)
                {
                    // Player 1 đúng, Player 2 sai
                    resultText = "<color=green>Người chơi 1 đúng!</color>\n<color=red>Người chơi 2 sai!</color>";
                }
                else
                {
                    // Trường hợp khác (không nên xảy ra)
                    resultText = "<color=green>Người chơi 1 thắng!</color>";
                }
            }
            else if (winnerId == 1)
            {
                // Player 2 thắng
                if (player1Correct && player2Correct)
                {
                    // Cả 2 đúng, Player 2 nhanh hơn
                    resultText = "<color=green>Cả 2 đều đúng!</color>\n<color=yellow>Người chơi 2 nhanh hơn!</color>";
                }
                else if (!player1Correct && player2Correct)
                {
                    // Player 1 sai, Player 2 đúng
                    resultText = "<color=red>Người chơi 1 sai!</color>\n<color=green>Người chơi 2 đúng!</color>";
                }
                else
                {
                    // Trường hợp khác (không nên xảy ra)
                    resultText = "<color=green>Người chơi 2 thắng!</color>";
                }
            }
            else if (winnerId == -1)
            {
                // Cả 2 sai
                resultText = "<color=red>Cả 2 đều sai!</color>";
            }
            
            Debug.Log($"[AnswerSummaryUI] Result: {resultText}");
            
            // Hiển thị kết quả nếu có resultText component
            if (this.resultText != null)
            {
                this.resultText.text = resultText;
            }
        }

        /// <summary>
        /// Xóa tổng kết và khôi phục trạng thái
        /// </summary>
        private void ClearSummary()
        {
            // Ẩn đáp án và resultText
            HideAnswerTexts();
            
            // Xóa text đáp án
            if (textTrangThaiDapAn1 != null)
            {
                textTrangThaiDapAn1.text = "";
            }
            
            if (textTrangThaiDapAn2 != null)
            {
                textTrangThaiDapAn2.text = "";
            }
            
            // ✅ Xóa text kết quả (TrangThaiWin)
            if (resultText != null)
            {
                resultText.text = "";
            }
            
            // Khôi phục trạng thái Question Time (sẵn sàng cho câu hỏi tiếp theo)
            SetTrangThaiQuestionTime();
            
            // ✅ Reset timer text về giá trị ban đầu từ GameRules
            if (timerText != null && battleManager != null)
            {
                int initialTime = Mathf.CeilToInt(battleManager.gameRules.questionTimeLimit);
                timerText.text = $"{initialTime}s";
            }
            
            Debug.Log("[AnswerSummaryUI] Summary cleared, all texts hidden");
        }

        /// <summary>
        /// Kiểm tra xem tổng kết có đang chạy không
        /// </summary>
        public bool IsSummaryActive => isSummaryActive;

        /// <summary>
        /// Lấy trạng thái hiện tại
        /// </summary>
        public TimerState GetCurrentState => currentState;

        /// <summary>
        /// Set thời gian tổng kết (admin có thể tuỳ chỉnh) - DEPRECATED
        /// Không còn dùng nữa - thời gian tự động lấy từ GameRules
        /// </summary>
        [System.Obsolete("Use GameRules.delayBetweenQuestions instead")]
        public void SetSummaryDuration(float duration)
        {
            Debug.LogWarning("[AnswerSummaryUI] SetSummaryDuration is deprecated. Use GameRules.delayBetweenQuestions instead.");
        }

        #region COUNTDOWN SUPPORT

        /// <summary>
        /// Ẩn tất cả UI (dùng cho countdown "3, 2, 1, Ready, GO!")
        /// </summary>
        public void HideAllUI()
        {
            // Ẩn timer text
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }

            // Ẩn trạng thái text
            if (trangThaiText != null)
            {
                trangThaiText.gameObject.SetActive(false);
            }

            // Ẩn answer texts
            HideAnswerTexts();

            Debug.Log("[AnswerSummaryUI] ✅ Hidden all UI for countdown");
        }

        /// <summary>
        /// Hiển thị tất cả UI (sau countdown)
        /// </summary>
        public void ShowAllUI()
        {
            // Hiển thị timer text
            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
            }

            // Hiển thị trạng thái text
            if (trangThaiText != null)
            {
                trangThaiText.gameObject.SetActive(true);
            }

            // Answer texts vẫn ẨN (chỉ hiển thị trong Summary Time)
            // resultText vẫn ẨN (chỉ hiển thị trong Summary Time)

            Debug.Log("[AnswerSummaryUI] ✅ Shown all UI after countdown");
        }

        #endregion
    }
}
