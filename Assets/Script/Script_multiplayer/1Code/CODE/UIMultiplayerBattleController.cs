using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Multiplayer;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 16: Multiplayer Battle - gán đúng vai trò local player và đối thủ theo Netcode.
    /// Tích hợp với NetworkedMathBattleManager để hiển thị câu hỏi và xử lý đáp án.
    /// </summary>
    public class UIMultiplayerBattleController : BasePanelController
    {
        [Header("Battle Status")]
        [SerializeField] private TMP_Text battleStatusText;

        [Header("Battle System")]
        [SerializeField] private NetworkedMathBattleManager battleManager;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private GameObject answerSlot; // Slot để thả đáp án
        [SerializeField] private MultiplayerDragAndDrop[] answerChoices; // 3-4 đáp án kéo được (MULTIPLAYER)



        [Header("Session End Handling")]
        [SerializeField] private UIMultiplayerRoomController roomController;
        [SerializeField] private bool resetRoomStateOnSessionEnded = true;
        [SerializeField] private UIButtonScreenNavigator onSessionEndedNavigator;
        [SerializeField] private UIButtonScreenNavigator sessionEndedFallbackNavigator;
        [SerializeField] private bool autoResolveSessionEndedNavigator = true;
        [SerializeField] private bool autoNavigateOnSessionEnded = true;
        [SerializeField] private float sessionCheckInterval = 0.5f;
        [SerializeField] private bool enableDebugLogs;

        [Header("Match End Navigation")]
        [SerializeField] private UIButtonScreenNavigator matchEndNavigator; // Navigate to Wins panel

        private bool hadTwoPlayersInSession;
        private bool sessionEndedHandled;
        private float nextSessionCheckAt;

        protected override void OnShow()
        {
            base.OnShow();
            HandlePanelActivated();
            
            // ✅ Bắt đầu countdown "3, 2, 1, Ready, GO!" khi panel hiển thị
            StartCountdown();
        }

        private void OnEnable()
        {
            Debug.Log("[BattleController] OnEnable CALLED");
            HandlePanelActivated();
        }

        private void OnDisable()
        {
            UnregisterNetCallbacks();
            UnsubscribeBattleEvents();
            UnsubscribeNetworkVariables();
        }

        private void Start()
        {
            Debug.Log("[BattleController] Start CALLED");
            
            // Auto-resolve BattleManager nếu chưa gán
            if (battleManager == null)
            {
                battleManager = FindObjectOfType<NetworkedMathBattleManager>();
                if (battleManager != null)
                {
                    Log($"Auto-resolved BattleManager: {battleManager.name}");
                }
                else
                {
                    Debug.LogWarning("[BattleController] BattleManager not found in Start, will retry...");
                }
            }

            // Subscribe vào battle events
            SubscribeBattleEvents();

            // Subscribe vào NetworkVariable changes (delay để đảm bảo NetworkObject đã spawn)
            Invoke(nameof(SubscribeNetworkVariables), 0.5f);

            // Setup drag-drop system
            SetupDragDropSystem();
        }

        private void HandlePanelActivated()
        {
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_BATTLE", "HandlePanelActivated begin");
            hadTwoPlayersInSession = false;
            sessionEndedHandled = false;
            nextSessionCheckAt = 0f;
            
            try
            {
                DisableRaycastOnRuntimePlayerClones();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UIBattle] DisableRaycast error (safe to ignore): {ex.Message}");
            }
            
            if (autoResolveSessionEndedNavigator)
            {
                TryResolveSessionEndedNavigator();
            }
            RegisterNetCallbacks();
            
            // ✅ FIX: Subscribe battle events mỗi khi panel được activate
            // Vì Start() chỉ gọi 1 lần, nhưng OnEnable() gọi mỗi lần panel active
            EnsureBattleManagerAndSubscribe();
            
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_BATTLE", "HandlePanelActivated done");
        }

        private void Update()
        {
            if (!autoNavigateOnSessionEnded || sessionEndedHandled)
                return;

            if (Time.unscaledTime < nextSessionCheckAt)
                return;

            nextSessionCheckAt = Time.unscaledTime + Mathf.Max(0.2f, sessionCheckInterval);
            CheckSessionEndedByConnectivity();
        }

        protected override void OnHide()
        {
            base.OnHide();
            UnregisterNetCallbacks();
        }

        private void OnDestroy()
        {
            UnregisterNetCallbacks();
            UnsubscribeBattleEvents();
        }

        #region BATTLE SYSTEM INTEGRATION

        /// <summary>
        /// Đảm bảo BattleManager được tìm thấy và subscribe events
        /// Gọi mỗi khi panel được activate để đảm bảo events luôn được subscribe
        /// </summary>
        private void EnsureBattleManagerAndSubscribe()
        {
            // Tìm BattleManager nếu chưa có
            if (battleManager == null)
            {
                battleManager = FindObjectOfType<NetworkedMathBattleManager>();
                if (battleManager != null)
                {
                    Debug.Log($"[BattleController] Found BattleManager: {battleManager.name}");
                }
                else
                {
                    Debug.LogWarning("[BattleController] BattleManager not found, will retry in next frame");
                    // Retry trong frame tiếp theo
                    Invoke(nameof(EnsureBattleManagerAndSubscribe), 0.1f);
                    return;
                }
            }

            // Subscribe events
            SubscribeBattleEvents();
        }

        /// <summary>
        /// Subscribe vào events của NetworkedMathBattleManager
        /// </summary>
        private void SubscribeBattleEvents()
        {
            if (battleManager == null)
            {
                Debug.LogWarning("[BattleController] BattleManager is null, cannot subscribe to events");
                return;
            }

            // Unsubscribe trước để tránh duplicate subscription
            battleManager.OnQuestionGenerated -= HandleQuestionGenerated;
            battleManager.OnAnswerResult -= HandleAnswerResult;
            battleManager.OnMatchEnded -= HandleMatchEnded;

            // Subscribe lại
            battleManager.OnQuestionGenerated += HandleQuestionGenerated;
            battleManager.OnAnswerResult += HandleAnswerResult;
            battleManager.OnMatchEnded += HandleMatchEnded;

            Debug.Log("[BattleController] ✅ Subscribed to BattleManager events");
        }

        /// <summary>
        /// Subscribe vào NetworkVariable changes để đồng bộ UI
        /// </summary>
        private void SubscribeNetworkVariables()
        {
            // Retry nếu BattleManager chưa sẵn sàng
            if (battleManager == null)
            {
                battleManager = FindObjectOfType<NetworkedMathBattleManager>();
            }

            if (battleManager == null)
            {
                Debug.LogWarning("[BattleController] BattleManager still null, retrying in 0.5s...");
                Invoke(nameof(SubscribeNetworkVariables), 0.5f);
                return;
            }

            // Kiểm tra xem NetworkObject đã spawn chưa
            var netObj = battleManager.GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                Debug.LogWarning("[BattleController] BattleManager NetworkObject not spawned yet, retrying in 0.5s...");
                Invoke(nameof(SubscribeNetworkVariables), 0.5f);
                return;
            }

            Debug.Log($"[BattleController] 🔗 Subscribing to NetworkVariables (IsSpawned={netObj?.IsSpawned})");

            // Subscribe vào CurrentQuestion changes
            battleManager.CurrentQuestion.OnValueChanged += OnQuestionChanged;
            
            // Subscribe vào Choice changes
            battleManager.Choice1.OnValueChanged += (oldVal, newVal) => OnChoicesChanged();
            battleManager.Choice2.OnValueChanged += (oldVal, newVal) => OnChoicesChanged();
            battleManager.Choice3.OnValueChanged += (oldVal, newVal) => OnChoicesChanged();
            battleManager.Choice4.OnValueChanged += (oldVal, newVal) => OnChoicesChanged();

            // ✅ FIX: Subscribe vào TimeRemaining để update timer UI
            battleManager.TimeRemaining.OnValueChanged += OnTimeRemainingChanged;

            // ✅ FIX: Subscribe vào MatchStarted/MatchEnded
            battleManager.MatchStarted.OnValueChanged += OnMatchStartedChanged;
            battleManager.MatchEnded.OnValueChanged += OnMatchEndedChanged;

            Debug.Log("[BattleController] ✅ Subscribed to NetworkVariable changes");

            // ✅ FIX: FORCE INITIAL SYNC - Đảm bảo Client nhận được data ngay cả khi join sau
            // OnValueChanged chỉ trigger khi VALUE THAY ĐỔI, không trigger cho giá trị ban đầu
            // Nên phải manually sync lần đầu
            Invoke(nameof(ForceInitialSync), 0.2f); // Delay nhỏ để đảm bảo NetworkVariables đã replicate
        }

        /// <summary>
        /// Force sync UI với NetworkVariables lần đầu (cho Client join sau)
        /// </summary>
        private void ForceInitialSync()
        {
            if (battleManager == null)
                return;

            Debug.Log("[BattleController] 🔄 Force initial sync...");

            // Sync question và choices
            string currentQuestion = battleManager.CurrentQuestion.Value.ToString();
            if (!string.IsNullOrEmpty(currentQuestion))
            {
                Debug.Log($"[BattleController] 📝 Initial question found: {currentQuestion}");
                UpdateQuestionUI();
            }
            else
            {
                Debug.Log("[BattleController] ⏳ No question yet, waiting for updates...");
            }

            // Sync match state
            if (battleManager.MatchStarted.Value)
            {
                Debug.Log("[BattleController] ⚔️ Match already started");
                if (battleStatusText != null)
                {
                    battleStatusText.text = "Trận đấu đang diễn ra!";
                }
            }

            if (battleManager.MatchEnded.Value)
            {
                Debug.Log("[BattleController] 🏁 Match already ended");
                if (battleStatusText != null)
                {
                    battleStatusText.text = "Trận đấu đã kết thúc!";
                }
            }
        }

        /// <summary>
        /// Callback khi TimeRemaining thay đổi
        /// </summary>
        private void OnTimeRemainingChanged(float oldValue, float newValue)
        {
            // TODO: Update timer UI
            Log($"⏱️ Time remaining: {newValue:F1}s");
        }

        /// <summary>
        /// Callback khi MatchStarted thay đổi
        /// </summary>
        private void OnMatchStartedChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                Log("⚔️ Match started!");
                if (battleStatusText != null)
                {
                    battleStatusText.text = "Trận đấu bắt đầu!";
                }
            }
        }

        /// <summary>
        /// Callback khi MatchEnded thay đổi
        /// </summary>
        private void OnMatchEndedChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                Log("🏁 Match ended!");
                DragAndDrop.SetGlobalLock(true);
                
                // Hiển thị kết quả
                int winnerId = battleManager.WinnerId.Value;
                var net = NetworkManager.Singleton;
                bool isLocalWinner = net != null && ((net.IsHost && winnerId == 0) || (!net.IsHost && winnerId == 1));

                if (battleStatusText != null)
                {
                    if (isLocalWinner)
                    {
                        battleStatusText.text = "<color=green><size=40>CHIẾN THẮNG!</size></color>";
                    }
                    else
                    {
                        battleStatusText.text = "<color=red><size=40>THUA CUỘC!</size></color>";
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribe khỏi NetworkVariable changes
        /// </summary>
        private void UnsubscribeNetworkVariables()
        {
            if (battleManager == null)
                return;

            battleManager.CurrentQuestion.OnValueChanged -= OnQuestionChanged;
            battleManager.TimeRemaining.OnValueChanged -= OnTimeRemainingChanged;
            battleManager.MatchStarted.OnValueChanged -= OnMatchStartedChanged;
            battleManager.MatchEnded.OnValueChanged -= OnMatchEndedChanged;
            // Note: Choice callbacks are anonymous, cannot unsubscribe individually
        }

        /// <summary>
        /// Callback khi CurrentQuestion thay đổi
        /// </summary>
        private void OnQuestionChanged(FixedString512Bytes oldValue, FixedString512Bytes newValue)
        {
            Log($"📝 Question changed: {newValue}");
            UpdateQuestionUI();
        }

        /// <summary>
        /// Callback khi các choices thay đổi
        /// </summary>
        private void OnChoicesChanged()
        {
            Log("🔢 Choices changed");
            UpdateQuestionUI();
        }

        /// <summary>
        /// Cập nhật UI với question và choices hiện tại
        /// </summary>
        private void UpdateQuestionUI()
        {
            if (battleManager == null) return;

            string question = battleManager.CurrentQuestion.Value.ToString();
            
            // Hiển thị câu hỏi
            if (questionText != null)
            {
                questionText.text = question;
                Log($"✅ Updated question text: {question}");
            }

            // ✅ MỞ KHÓA TRƯỚC KHI RESET (quan trọng!)
            DragAndDrop.SetGlobalLock(false);
            DoAnGame.Multiplayer.MultiplayerDragAndDrop.SetGlobalLock(false);

            // ✅ Reset tất cả đáp án về vị trí gốc (câu hỏi mới)
            if (answerChoices != null && answerChoices.Length >= 4)
            {
                int[] choices = new int[]
                {
                    battleManager.Choice1.Value,
                    battleManager.Choice2.Value,
                    battleManager.Choice3.Value,
                    battleManager.Choice4.Value
                };

                for (int i = 0; i < answerChoices.Length && i < choices.Length; i++)
                {
                    if (answerChoices[i] == null || answerChoices[i].myText == null)
                        continue;

                    answerChoices[i].myText.text = choices[i].ToString();
                    
                    // ✅ Reset về vị trí gốc và clear màu
                    answerChoices[i].ResetForNewQuestion();
                    
                    Log($"✅ Updated choice {i}: {choices[i]}");
                }
            }

            // Update status
            if (battleStatusText != null)
            {
                battleStatusText.text = "Kéo đáp án vào ô!";
            }
        }

        /// <summary>
        /// Unsubscribe khỏi events
        /// </summary>
        private void UnsubscribeBattleEvents()
        {
            if (battleManager == null)
                return;

            battleManager.OnQuestionGenerated -= HandleQuestionGenerated;
            battleManager.OnAnswerResult -= HandleAnswerResult;
            battleManager.OnMatchEnded -= HandleMatchEnded;
        }

        /// <summary>
        /// Setup drag-drop system cho multiplayer
        /// </summary>
        private void SetupDragDropSystem()
        {
            if (answerChoices == null || answerChoices.Length == 0)
            {
                Log("Answer choices not assigned!");
                return;
            }

            if (answerSlot == null)
            {
                Log("Answer slot not assigned!");
                return;
            }

            // Hook vào drag-drop events để detect khi player thả đáp án
            // Sẽ cần modify DragAndDrop.cs để trigger callback khi drop vào slot
            Log($"Setup drag-drop system with {answerChoices.Length} choices");
        }

        /// <summary>
        /// Xử lý khi có câu hỏi mới
        /// </summary>
        private void HandleQuestionGenerated(string question, int[] choices)
        {
            Log($"Question received: {question}");

            // Hiển thị câu hỏi
            if (questionText != null)
            {
                questionText.text = question;
            }

            // ✅ MỞ KHÓA TRƯỚC KHI RESET (quan trọng!)
            DragAndDrop.SetGlobalLock(false);
            DoAnGame.Multiplayer.MultiplayerDragAndDrop.SetGlobalLock(false);

            // ✅ Reset tất cả đáp án về vị trí gốc (câu hỏi mới)
            if (answerChoices != null)
            {
                for (int i = 0; i < answerChoices.Length && i < choices.Length; i++)
                {
                    if (answerChoices[i] == null || answerChoices[i].myText == null)
                        continue;

                    answerChoices[i].myText.text = choices[i].ToString();
                    
                    // ✅ Reset về vị trí gốc và clear màu
                    answerChoices[i].ResetForNewQuestion();
                }
            }

            // Update status
            if (battleStatusText != null)
            {
                battleStatusText.text = "Kéo đáp án vào ô!";
            }

            // ✅ CRITICAL FIX: Start timer cho câu hỏi mới
            // Gọi StartQuestionTimer() để bắt đầu đếm ngược cho câu hỏi này
            StartQuestionTimer();
        }

        /// <summary>
        /// Gọi khi player thả đáp án vào slot (cần hook từ DragAndDrop)
        /// </summary>
        public void OnAnswerDropped(int answer)
        {
            if (battleManager == null)
            {
                Log("BattleManager is null, cannot submit answer");
                return;
            }

            Log($"Player dropped answer: {answer}");

            // Disable dragging
            DragAndDrop.SetGlobalLock(true);

            // Submit đáp án lên server
            battleManager.SubmitAnswerServerRpc(answer);

            // Update status
            if (battleStatusText != null)
            {
                battleStatusText.text = "Đã gửi đáp án...";
            }
        }

        /// <summary>
        /// Xử lý kết quả đáp án
        /// </summary>
        private void HandleAnswerResult(int winnerId, bool correct, long responseTimeMs)
        {
            var net = NetworkManager.Singleton;
            string role = net != null && net.IsHost ? "HOST" : "CLIENT";
            
            Debug.Log($"[BattleController] [{role}] HandleAnswerResult CALLED: Winner={winnerId}, Correct={correct}, Time={responseTimeMs}ms");
            Log($"[{role}] Answer result: Winner={winnerId}, Correct={correct}, Time={responseTimeMs}ms");

            // ✅ KHÓA DRAG-DROP NGAY LẬP TỨC (không cho kéo thả trong thời gian thống kê)
            DragAndDrop.SetGlobalLock(true);
            DoAnGame.Multiplayer.MultiplayerDragAndDrop.SetGlobalLock(true);
            Log($"[{role}] 🔒 Locked drag-drop during answer summary");

            // ✅ Lấy đáp án đúng từ BattleManager
            int correctAnswer = battleManager != null ? battleManager.CorrectAnswer.Value : -1;
            
            if (correctAnswer == -1)
            {
                Debug.LogError($"[BattleController] [{role}] Cannot get correct answer from BattleManager!");
                return;
            }

            Debug.Log($"[BattleController] [{role}] Correct answer: {correctAnswer}");
            Log($"[{role}] Correct answer: {correctAnswer}");

            // ✅ Kiểm tra answerChoices
            if (answerChoices == null)
            {
                Debug.LogError($"[BattleController] [{role}] answerChoices is NULL!");
                return;
            }

            Debug.Log($"[BattleController] [{role}] answerChoices.Length: {answerChoices.Length}");

            // ✅ Hiển thị màu cho TỪNG đáp án dựa trên đúng/sai
            for (int i = 0; i < answerChoices.Length; i++)
            {
                if (answerChoices[i] == null)
                {
                    Debug.LogWarning($"[BattleController] [{role}] answerChoices[{i}] is NULL!");
                    continue;
                }

                if (answerChoices[i].myText == null)
                {
                    Debug.LogWarning($"[BattleController] [{role}] answerChoices[{i}].myText is NULL!");
                    continue;
                }

                // Parse đáp án từ text
                string answerText = answerChoices[i].myText.text;
                Debug.Log($"[BattleController] [{role}] Answer {i} text: '{answerText}'");

                if (int.TryParse(answerText, out int answerValue))
                {
                    // So sánh với đáp án đúng
                    bool isCorrectAnswer = (answerValue == correctAnswer);
                    
                    Debug.Log($"[BattleController] [{role}] Answer {i}: {answerValue} → {(isCorrectAnswer ? "CORRECT" : "WRONG")} → Calling ShowResultForce({isCorrectAnswer})");
                    
                    // ✅ FORCE hiển thị màu cho TẤT CẢ đáp án (kể cả đáp án trong slot)
                    answerChoices[i].ShowResultForce(isCorrectAnswer);
                    
                    Debug.Log($"[BattleController] [{role}] Answer {i}: ShowResultForce DONE");
                }
                else
                {
                    Debug.LogWarning($"[BattleController] [{role}] Cannot parse answer text: '{answerText}'");
                }
            }

            // Hiển thị kết quả text
            bool isLocalWinner = net != null && ((net.IsHost && winnerId == 0) || (!net.IsHost && winnerId == 1));
            
            if (battleStatusText != null)
            {
                if (winnerId == -1)
                {
                    battleStatusText.text = "Cả 2 đều sai!";
                }
                else
                {
                    if (isLocalWinner)
                    {
                        battleStatusText.text = $"<color=green>Bạn trả lời đúng! ({responseTimeMs}ms)</color>";
                    }
                    else
                    {
                        battleStatusText.text = $"<color=red>Đối thủ trả lời đúng nhanh hơn!</color>";
                    }
                }
            }

            Debug.Log($"[BattleController] [{role}] HandleAnswerResult COMPLETED");
        }

        /// <summary>
        /// Xử lý khi trận đấu kết thúc
        /// </summary>
        private void HandleMatchEnded(int winnerId, int winnerHealth)
        {
            Log($"Match ended: Winner={winnerId}, Health={winnerHealth}");

            // Disable dragging
            DragAndDrop.SetGlobalLock(true);

            // Hiển thị kết quả tạm thời
            if (battleStatusText != null)
            {
                var net = NetworkManager.Singleton;
                bool isLocalWinner = net != null && ((net.IsHost && winnerId == 0) || (!net.IsHost && winnerId == 1));

                if (isLocalWinner)
                {
                    battleStatusText.text = "<color=green><size=40>CHIẾN THẮNG!</size></color>";
                }
                else
                {
                    battleStatusText.text = "<color=red><size=40>THUA CUỘC!</size></color>";
                }
            }

            // Navigate to Wins panel sau 2 giây (cho người chơi thấy kết quả)
            Invoke(nameof(NavigateToWinsPanel), 2f);
        }

        /// <summary>
        /// Navigate to Wins panel
        /// </summary>
        private void NavigateToWinsPanel()
        {
            if (matchEndNavigator != null)
            {
                Log("Navigating to Wins panel");
                matchEndNavigator.NavigateNow();
            }
            else
            {
                Debug.LogWarning("[BattleController] matchEndNavigator is not assigned! Cannot navigate to Wins panel.");
            }
        }

        #endregion

        #region COUNTDOWN LOGIC

        /// <summary>
        /// Bắt đầu countdown "3, 2, 1, Ready, GO!" khi vào GameplayPanel
        /// </summary>
        private void StartCountdown()
        {
            Debug.Log("[BattleController] 🎬 Starting countdown...");
            
            // Ẩn tất cả UI trong khi countdown
            HideAllBattleUI();
            
            // Bắt đầu countdown coroutine
            StartCoroutine(CountdownRoutine());
        }

        /// <summary>
        /// Countdown routine: "3, 2, 1, Ready, GO!"
        /// Timing: 0.5s delay → 3 (1s) → 2 (1s) → 1 (1s) → Ready (1s) → GO! (1s)
        /// Tổng: ~5.5 giây
        /// </summary>
        private System.Collections.IEnumerator CountdownRoutine()
        {
            // Delay nhỏ trước khi bắt đầu countdown (cho người chơi chuẩn bị)
            yield return new WaitForSeconds(0.5f);

            // Countdown: 3, 2, 1
            for (int i = 3; i >= 1; i--)
            {
                if (battleStatusText != null)
                {
                    battleStatusText.SetText(i.ToString());
                }
                Debug.Log($"[BattleController] Countdown: {i}");
                yield return new WaitForSeconds(1f); // Hiển thị mỗi số trong 1 giây
            }

            // Ready (hiển thị 1 giây)
            if (battleStatusText != null)
            {
                battleStatusText.SetText("Ready");
            }
            Debug.Log("[BattleController] Countdown: Ready");
            yield return new WaitForSeconds(1f);

            // GO! (hiển thị 1 giây - tăng từ 0.5s để rõ ràng hơn)
            if (battleStatusText != null)
            {
                battleStatusText.SetText("GO!");
            }
            Debug.Log("[BattleController] Countdown: GO!");
            yield return new WaitForSeconds(1f);

            // Hiển thị tất cả UI và bắt đầu timer
            ShowAllBattleUI();
            StartQuestionTimer();
            
            Debug.Log("[BattleController] ✅ Countdown complete, battle started!");
        }

        /// <summary>
        /// Ẩn tất cả UI trong khi countdown (chỉ hiển thị battleStatusText)
        /// </summary>
        private void HideAllBattleUI()
        {
            // Ẩn question text
            if (questionText != null)
            {
                questionText.gameObject.SetActive(false);
            }

            // Ẩn answer slot
            if (answerSlot != null)
            {
                answerSlot.SetActive(false);
            }

            // Ẩn answer choices
            if (answerChoices != null)
            {
                foreach (var choice in answerChoices)
                {
                    if (choice != null)
                    {
                        choice.gameObject.SetActive(false);
                    }
                }
            }

            // Ẩn AnswerSummaryUI (timer, status, answer texts)
            var answerSummaryUI = FindObjectOfType<AnswerSummaryUI>();
            if (answerSummaryUI != null)
            {
                answerSummaryUI.HideAllUI();
            }

            Debug.Log("[BattleController] ✅ Hidden all battle UI for countdown");
        }

        /// <summary>
        /// Hiển thị tất cả UI sau countdown
        /// </summary>
        private void ShowAllBattleUI()
        {
            // Hiển thị question text
            if (questionText != null)
            {
                questionText.gameObject.SetActive(true);
            }

            // Hiển thị answer slot
            if (answerSlot != null)
            {
                answerSlot.SetActive(true);
            }

            // Hiển thị answer choices
            if (answerChoices != null)
            {
                foreach (var choice in answerChoices)
                {
                    if (choice != null)
                    {
                        choice.gameObject.SetActive(true);
                    }
                }
            }

            // Hiển thị AnswerSummaryUI
            var answerSummaryUI = FindObjectOfType<AnswerSummaryUI>();
            if (answerSummaryUI != null)
            {
                answerSummaryUI.ShowAllUI();
            }

            // Reset battle status text về trạng thái mặc định
            if (battleStatusText != null)
            {
                battleStatusText.SetText("Thời gian trả lời câu hỏi");
            }

            Debug.Log("[BattleController] ✅ Shown all battle UI after countdown");
        }

        /// <summary>
        /// Bắt đầu timer cho câu hỏi
        /// - Host: Start cả BattleManager timer (server-side) VÀ AnswerSummaryUI timer (UI)
        /// - Client: Chỉ start AnswerSummaryUI timer (UI)
        /// </summary>
        private void StartQuestionTimer()
        {
            var nm = NetworkManager.Singleton;
            
            // ✅ HOST: Start BattleManager timer (server-side logic)
            if (nm != null && nm.IsServer)
            {
                if (battleManager == null)
                {
                    Debug.LogError("[BattleController] BattleManager is null, cannot start timer!");
                    return;
                }

                // Gọi BattleManager để start timer (server-side)
                battleManager.StartQuestionTimer();
                Debug.Log("[BattleController] ✅ Started BattleManager timer (server-side)");
            }

            // ✅ CẢ HOST VÀ CLIENT: Start AnswerSummaryUI timer (UI countdown)
            var answerSummaryUI = FindObjectOfType<AnswerSummaryUI>();
            if (answerSummaryUI != null)
            {
                answerSummaryUI.StartQuestionTimer();
                Debug.Log("[BattleController] ✅ Started AnswerSummaryUI timer (UI)");
            }
            else
            {
                Debug.LogWarning("[BattleController] ⚠️ AnswerSummaryUI not found!");
            }
        }

        #endregion



        private void RegisterNetCallbacks()
        {
            var net = NetworkManager.Singleton;
            if (net == null)
                return;

            net.OnClientDisconnectCallback -= HandleClientDisconnect;
            net.OnClientDisconnectCallback += HandleClientDisconnect;
            net.OnClientConnectedCallback -= HandleClientConnected;
            net.OnClientConnectedCallback += HandleClientConnected;

            Log("Registered net callbacks");
        }

        private void UnregisterNetCallbacks()
        {
            var net = NetworkManager.Singleton;
            if (net == null)
                return;

            net.OnClientDisconnectCallback -= HandleClientDisconnect;
            net.OnClientConnectedCallback -= HandleClientConnected;
        }

        private void HandleClientConnected(ulong clientId)
        {
            var net = NetworkManager.Singleton;
            if (net == null)
                return;

            int count = net.ConnectedClientsIds.Count;
            if (count >= 2)
            {
                hadTwoPlayersInSession = true;
            }

            Log($"Client connected: {clientId} | count={count}");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_BATTLE", $"HandleClientConnected clientId={clientId}, count={count}");
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            var net = NetworkManager.Singleton;
            int count = net != null ? net.ConnectedClientsIds.Count : 0;

            Log($"Client disconnected: {clientId} | count={count}");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_BATTLE", $"HandleClientDisconnect clientId={clientId}, count={count}");

            if (!autoNavigateOnSessionEnded || sessionEndedHandled)
                return;

            bool isLocalDisconnect = net != null && clientId == net.LocalClientId;
            bool shouldEndSession = hadTwoPlayersInSession && !isLocalDisconnect;

            // Fallback: nếu network đã dừng thì cũng kết thúc session ngay.
            if (!shouldEndSession && (net == null || !net.IsListening))
            {
                shouldEndSession = true;
            }

            if (shouldEndSession)
            {
                NavigateOutAfterSessionEnded();
            }
        }

        private void CheckSessionEndedByConnectivity()
        {
            if (!hadTwoPlayersInSession)
                return;

            var net = NetworkManager.Singleton;
            bool ended = false;

            if (net == null || !net.IsListening)
            {
                ended = true;
            }
            else if (net.ConnectedClientsIds.Count < 2)
            {
                ended = true;
            }

            if (!ended)
                return;

            Log("Connectivity check -> session ended");
            NavigateOutAfterSessionEnded();
        }

        private void NavigateOutAfterSessionEnded()
        {
            if (sessionEndedHandled)
                return;

            sessionEndedHandled = true;
            battleStatusText?.SetText("Đối thủ đã thoát phòng.");

            if (resetRoomStateOnSessionEnded)
            {
                ResolveRoomControllerIfNeeded();
                if (roomController != null)
                {
                    Log("Session ended -> RequestQuitRoom for full reset");
                    roomController.RequestQuitRoom();
                }
                else
                {
                    Log("Session ended -> roomController NULL, skip RequestQuitRoom");
                }
            }

            if (onSessionEndedNavigator != null)
            {
                Log($"Session ended -> navigate using {onSessionEndedNavigator.name}");
                onSessionEndedNavigator.NavigateNow();
            }
            else if (sessionEndedFallbackNavigator != null)
            {
                Log($"Session ended -> navigate using fallback {sessionEndedFallbackNavigator.name}");
                sessionEndedFallbackNavigator.NavigateNow();
            }
            else
            {
                Log("Session ended nhưng chưa gán onSessionEndedNavigator");
            }
        }

        private void TryResolveSessionEndedNavigator()
        {
            if (onSessionEndedNavigator != null || sessionEndedFallbackNavigator != null)
                return;

            // Prefer navigators already configured on the UI16 action hub.
            var hub = FindObjectOfType<UI16ButtonActionHub>(true);
            if (hub != null)
            {
                if (roomController == null && hub.RoomController != null)
                {
                    roomController = hub.RoomController;
                    Log($"Auto-resolved roomController from hub: {roomController.name}");
                }

                if (onSessionEndedNavigator == null && hub.BackToRoomNavigator != null)
                {
                    onSessionEndedNavigator = hub.BackToRoomNavigator;
                    Log($"Auto-resolved onSessionEndedNavigator from hub: {onSessionEndedNavigator.name}");
                }

                if (sessionEndedFallbackNavigator == null && hub.FallbackQuitNavigator != null)
                {
                    sessionEndedFallbackNavigator = hub.FallbackQuitNavigator;
                    Log($"Auto-resolved sessionEndedFallbackNavigator from hub: {sessionEndedFallbackNavigator.name}");
                }

                if (onSessionEndedNavigator != null || sessionEndedFallbackNavigator != null)
                    return;
            }

            var navigators = GetComponentsInChildren<UIButtonScreenNavigator>(true);
            if (navigators == null || navigators.Length == 0)
                return;

            UIButtonScreenNavigator preferred = null;
            UIButtonScreenNavigator first = null;

            for (int i = 0; i < navigators.Length; i++)
            {
                var nav = navigators[i];
                if (nav == null)
                    continue;

                if (first == null)
                    first = nav;

                string n = nav.gameObject.name;
                if (n.IndexOf("Back", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Quit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    preferred = nav;
                    break;
                }
            }

            sessionEndedFallbackNavigator = preferred ?? first;
            if (sessionEndedFallbackNavigator != null)
            {
                Log($"Auto-resolved sessionEndedFallbackNavigator={sessionEndedFallbackNavigator.name}");
            }
        }

        private void ResolveRoomControllerIfNeeded()
        {
            if (roomController != null)
                return;

            var hub = FindObjectOfType<UI16ButtonActionHub>(true);
            if (hub != null && hub.RoomController != null)
            {
                roomController = hub.RoomController;
                Log($"Resolved roomController from hub: {roomController.name}");
                return;
            }

            roomController = FindObjectOfType<UIMultiplayerRoomController>(true);
            if (roomController != null)
            {
                Log($"Resolved roomController by search: {roomController.name}");
            }
        }



        private void DisableRaycastOnRuntimePlayerClones()
        {
            var root = transform.parent;
            if (root == null)
                return;

            int blockedTargetsDisabled = 0;

            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == null || child == transform)
                    continue;

                string n = child.name;
                bool looksLikeRuntimePlayer = n.StartsWith("Player(Clone)") || n.StartsWith("Player (") || n.StartsWith("Player");
                if (!looksLikeRuntimePlayer)
                    continue;

                var graphics = child.GetComponentsInChildren<Graphic>(true);
                for (int g = 0; g < graphics.Length; g++)
                {
                    if (graphics[g] == null || !graphics[g].raycastTarget)
                        continue;
                    
                    // ✅ FIX: BỎ QUA Answer objects (có MultiplayerDragAndDrop component)
                    if (graphics[g].GetComponent<DoAnGame.Multiplayer.MultiplayerDragAndDrop>() != null)
                    {
                        Debug.Log($"[BattleController] Skipping Answer object: {graphics[g].name}");
                        continue;
                    }
                    
                    // ✅ FIX: BỎ QUA Buttons và interactive UI
                    if (graphics[g].GetComponent<UnityEngine.UI.Button>() != null ||
                        graphics[g].GetComponent<UnityEngine.UI.Toggle>() != null ||
                        graphics[g].GetComponent<UnityEngine.UI.InputField>() != null)
                    {
                        continue;
                    }
                    
                    graphics[g].raycastTarget = false;
                    blockedTargetsDisabled++;
                }

                var groups = child.GetComponentsInChildren<CanvasGroup>(true);
                for (int c = 0; c < groups.Length; c++)
                {
                    if (groups[c] != null)
                    {
                        groups[c].blocksRaycasts = false;
                    }
                }
            }

            if (blockedTargetsDisabled > 0)
            {
                Log($"Disabled raycast blockers on runtime players: {blockedTargetsDisabled}");
            }
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[{nameof(UIMultiplayerBattleController)}:{name}] {message}");
        }
    }
}
