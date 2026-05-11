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

        [Header("Avatar Characters")]
        [SerializeField] private AvatarCharacterDisplay player1Character; // Player1Character trong GameplayPanel
        [SerializeField] private AvatarCharacterDisplay player2Character; // Player2Character trong GameplayPanel

        private bool hadTwoPlayersInSession;
        private bool sessionEndedHandled;
        private float nextSessionCheckAt;

        protected override void OnShow()
        {
            base.OnShow();
            
            // ✅ Reset flag để có thể xử lý match end cho trận mới
            hasHandledMatchEnd = false;
            
            // ✅ Force unlock drag-drop khi vào GameplayPanel
            // isLocked là static field — có thể còn true từ lần chơi trước
            // nếu child objects không bị disable/enable (OnEnable không được gọi)
            DoAnGame.Multiplayer.MultiplayerDragAndDrop.SetGlobalLock(false);
            DragAndDrop.SetGlobalLock(false);
            
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

            // ✅ FIX: Không subscribe ở đây nữa - để OnEnable() xử lý
            // Subscribe vào battle events sẽ được gọi trong OnEnable() → EnsureBattleManagerAndSubscribe()
            // SubscribeBattleEvents(); // ← REMOVED to prevent duplicate subscription

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

            // Khởi tạo avatar characters cho cả 2 player
            InitAvatarCharacters();
            
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
            battleManager.OnAnswerResultReceived -= HandleAnswerResultDetailed;
            battleManager.OnMatchEnded -= HandleMatchEnded;

            // Subscribe lại
            battleManager.OnQuestionGenerated += HandleQuestionGenerated;
            battleManager.OnAnswerResultReceived += HandleAnswerResultDetailed; // ✅ Subscribe to detailed event
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
            if (battleManager == null) return;
            battleManager.OnQuestionGenerated -= HandleQuestionGenerated;
            battleManager.OnAnswerResultReceived -= HandleAnswerResultDetailed;
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
        /// Khởi tạo avatar characters cho cả 2 player dựa trên AvatarId từ NetworkedPlayerState.
        /// Gọi trong HandlePanelActivated() — delay nhỏ để đảm bảo player states đã spawn.
        /// </summary>
        private void InitAvatarCharacters()
        {
            if (player1Character == null && player2Character == null) return;

            // Delay để đảm bảo NetworkedPlayerState đã spawn và AvatarId đã sync
            Invoke(nameof(ApplyAvatarCharacters), 0.8f);
        }

        private void ApplyAvatarCharacters()
        {
            if (battleManager == null) return;

            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";

            Debug.Log($"[BattleController] [{role}] ===== ApplyAvatarCharacters START =====");

            var p1 = battleManager.GetPlayer1State();
            var p2 = battleManager.GetPlayer2State();

            Debug.Log($"[BattleController] [{role}] Player1State: {(p1 != null ? "FOUND" : "NULL")}");
            Debug.Log($"[BattleController] [{role}] Player2State: {(p2 != null ? "FOUND" : "NULL")}");
            Debug.Log($"[BattleController] [{role}] player1Character: {(player1Character != null ? player1Character.gameObject.name : "NULL")}");
            Debug.Log($"[BattleController] [{role}] player2Character: {(player2Character != null ? player2Character.gameObject.name : "NULL")}");

            if (p1 != null && player1Character != null)
            {
                int avatarId = p1.AvatarId.Value;
                Debug.Log($"[BattleController] [{role}] Player1 AvatarId={avatarId}, calling SetAvatar()...");
                player1Character.SetAvatar(avatarId);
                
                // Subscribe để update nếu AvatarId sync muộn
                p1.AvatarId.OnValueChanged += (oldId, newId) =>
                {
                    Debug.Log($"[BattleController] [{role}] Player1 AvatarId changed: {oldId} → {newId}");
                    player1Character.SetAvatar(newId);
                };
            }
            else
            {
                if (p1 == null) Debug.LogWarning($"[BattleController] [{role}] ⚠️ Player1State is NULL!");
                if (player1Character == null) Debug.LogWarning($"[BattleController] [{role}] ⚠️ player1Character is NULL!");
            }

            if (p2 != null && player2Character != null)
            {
                int avatarId = p2.AvatarId.Value;
                Debug.Log($"[BattleController] [{role}] Player2 AvatarId={avatarId}, calling SetAvatar()...");
                player2Character.SetAvatar(avatarId);
                
                // Subscribe để update nếu AvatarId sync muộn
                p2.AvatarId.OnValueChanged += (oldId, newId) =>
                {
                    Debug.Log($"[BattleController] [{role}] Player2 AvatarId changed: {oldId} → {newId}");
                    player2Character.SetAvatar(newId);
                };
            }
            else
            {
                if (p2 == null) Debug.LogWarning($"[BattleController] [{role}] ⚠️ Player2State is NULL!");
                if (player2Character == null) Debug.LogWarning($"[BattleController] [{role}] ⚠️ player2Character is NULL!");
            }

            Debug.Log($"[BattleController] [{role}] ===== ApplyAvatarCharacters COMPLETE =====");
        }

        /// <summary>
        /// Xử lý khi có câu hỏi mới
        /// </summary>
        private void HandleQuestionGenerated(string question, int[] choices)
        {
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            
            Debug.Log($"[BattleController] [{role}] ===== HandleQuestionGenerated START =====");
            Debug.Log($"[BattleController] [{role}] Question: '{question}'");
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

            // ✅ AVATAR ANIMATION: Reset về Idle khi câu hỏi mới bắt đầu (Question Time)
            Debug.Log($"[BattleController] [{role}] ===== AVATAR ANIMATION: Question Time → ShowIdle() =====");
            Debug.Log($"[BattleController] [{role}] Calling ShowIdle() for Player1Character...");
            if (player1Character != null)
            {
                player1Character.ShowIdle();
                Debug.Log($"[BattleController] [{role}] ✅ Player1Character.ShowIdle() DONE");
            }
            else
            {
                Debug.LogWarning($"[BattleController] [{role}] ⚠️ player1Character is NULL!");
            }
            
            Debug.Log($"[BattleController] [{role}] Calling ShowIdle() for Player2Character...");
            if (player2Character != null)
            {
                player2Character.ShowIdle();
                Debug.Log($"[BattleController] [{role}] ✅ Player2Character.ShowIdle() DONE");
            }
            else
            {
                Debug.LogWarning($"[BattleController] [{role}] ⚠️ player2Character is NULL!");
            }
            
            Debug.Log($"[BattleController] [{role}] ===== HandleQuestionGenerated COMPLETE =====");
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
        /// Xử lý kết quả đáp án với thông tin đầy đủ (player1Answer, player2Answer)
        /// để phân biệt được "cả 2 đúng" vs "1 đúng 1 sai"
        /// </summary>
        private void HandleAnswerResultDetailed(int winnerId, bool correct, long player1ResponseTimeMs, long player2ResponseTimeMs, int player1Answer, int player2Answer)
        {
            var net = NetworkManager.Singleton;
            string role = net != null && net.IsHost ? "HOST" : "CLIENT";
            
            Debug.Log($"[BattleController] [{role}] ===== HandleAnswerResultDetailed START =====");
            Debug.Log($"[BattleController] [{role}] Winner={winnerId}, Correct={correct}, P1Time={player1ResponseTimeMs}ms, P2Time={player2ResponseTimeMs}ms");
            Debug.Log($"[BattleController] [{role}] P1Answer={player1Answer}, P2Answer={player2Answer}");
            Log($"[{role}] Answer result: Winner={winnerId}, Correct={correct}, P1Time={player1ResponseTimeMs}ms, P2Time={player2ResponseTimeMs}ms");

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

            // ✅ Kiểm tra cả 2 có đúng không
            bool player1Correct = (player1Answer == correctAnswer);
            bool player2Correct = (player2Answer == correctAnswer);
            bool bothCorrect = player1Correct && player2Correct;
            
            Debug.Log($"[BattleController] [{role}] P1Correct={player1Correct}, P2Correct={player2Correct}, BothCorrect={bothCorrect}");

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

            // ✅ Hiển thị kết quả text - phân biệt rõ ràng giữa local player và opponent
            // isLocalWinner: true nếu local player thắng câu này
            bool isLocalWinner = net != null && ((net.IsHost && winnerId == 0) || (!net.IsHost && winnerId == 1));
            
            // Lấy response time của local player để hiển thị
            long localResponseTime = (net != null && net.IsHost) ? player1ResponseTimeMs : player2ResponseTimeMs;
            
            if (battleStatusText != null)
            {
                if (winnerId == -1)
                {
                    // Cả 2 sai
                    battleStatusText.SetText("Cả 2 đều sai!");
                    Debug.Log($"[BattleController] [{role}] battleStatusText: 'Cả 2 đều sai!'");
                }
                else if (winnerId == -2)
                {
                    // Hòa (cả 2 đúng cùng lúc)
                    battleStatusText.SetText("Hòa! Cả 2 đều đúng cùng lúc.");
                    Debug.Log($"[BattleController] [{role}] battleStatusText: 'Hòa! Cả 2 đều đúng cùng lúc.'");
                }
                else
                {
                    // Có người thắng
                    if (isLocalWinner)
                    {
                        // Local player thắng câu này
                        battleStatusText.SetText($"<color=green>Chiến thắng! ({localResponseTime}ms)</color>");
                        Debug.Log($"[BattleController] [{role}] battleStatusText: 'Chiến thắng! ({localResponseTime}ms)' (local player won)");
                    }
                    else
                    {
                        // Opponent thắng câu này
                        battleStatusText.SetText($"<color=red>Thua cuộc! Đối thủ nhanh hơn.</color>");
                        Debug.Log($"[BattleController] [{role}] battleStatusText: 'Thua cuộc! Đối thủ nhanh hơn.' (opponent won)");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[BattleController] [{role}] battleStatusText is NULL!");
            }

            Debug.Log($"[BattleController] [{role}] HandleAnswerResultDetailed text display COMPLETED");

            // ✅ AVATAR ANIMATION: Trigger animation theo kết quả câu trả lời (Summary Time)
            // winnerId: 0 = Player1 thắng, 1 = Player2 thắng, -1 = cả 2 sai, -2 = hòa
            // bothCorrect: true nếu CẢ 2 đều trả lời đúng
            
            Debug.Log($"[BattleController] [{role}] ===== AVATAR ANIMATION: Summary Time START =====");
            Debug.Log($"[BattleController] [{role}] winnerId={winnerId}, bothCorrect={bothCorrect}");
            
            if (winnerId == -1)
            {
                // Cả 2 sai → Sad cho cả 2 (KHÔNG mất máu)
                Debug.Log($"[BattleController] [{role}] Case: Cả 2 sai → ShowSad() for both");
                Debug.Log($"[BattleController] [{role}] Calling Player1Character.ShowSad()...");
                if (player1Character != null)
                {
                    player1Character.ShowSad();
                    Debug.Log($"[BattleController] [{role}] ✅ Player1Character.ShowSad() DONE");
                }
                else
                {
                    Debug.LogWarning($"[BattleController] [{role}] ⚠️ player1Character is NULL!");
                }
                
                Debug.Log($"[BattleController] [{role}] Calling Player2Character.ShowSad()...");
                if (player2Character != null)
                {
                    player2Character.ShowSad();
                    Debug.Log($"[BattleController] [{role}] ✅ Player2Character.ShowSad() DONE");
                }
                else
                {
                    Debug.LogWarning($"[BattleController] [{role}] ⚠️ player2Character is NULL!");
                }
            }
            else if (winnerId == -2)
            {
                // Hòa (cả 2 đúng cùng lúc) → Sad cho cả 2 (KHÔNG mất máu)
                Debug.Log($"[BattleController] [{role}] Case: Hòa (cả 2 đúng cùng lúc) → ShowSad() for both");
                Debug.Log($"[BattleController] [{role}] Calling Player1Character.ShowSad()...");
                if (player1Character != null)
                {
                    player1Character.ShowSad();
                    Debug.Log($"[BattleController] [{role}] ✅ Player1Character.ShowSad() DONE");
                }
                else
                {
                    Debug.LogWarning($"[BattleController] [{role}] ⚠️ player1Character is NULL!");
                }
                
                Debug.Log($"[BattleController] [{role}] Calling Player2Character.ShowSad()...");
                if (player2Character != null)
                {
                    player2Character.ShowSad();
                    Debug.Log($"[BattleController] [{role}] ✅ Player2Character.ShowSad() DONE");
                }
                else
                {
                    Debug.LogWarning($"[BattleController] [{role}] ⚠️ player2Character is NULL!");
                }
            }
            else if (winnerId == 0)
            {
                // Player1 thắng câu (đúng hoặc nhanh hơn)
                // ✅ FIXED LOGIC: Kiểm tra bothCorrect để phân biệt "cả 2 đúng" vs "1 đúng 1 sai"
                
                Debug.Log($"[BattleController] [{role}] Case: Player1 thắng (bothCorrect={bothCorrect})");
                
                if (bothCorrect)
                {
                    // Cả 2 đúng, P1 nhanh hơn → P1 Happy, P2 Sad
                    Debug.Log($"[BattleController] [{role}] Both correct, P1 faster → P1 ShowHappy(), P2 ShowSad()");
                    Debug.Log($"[BattleController] [{role}] Calling Player1Character.ShowHappy()...");
                    if (player1Character != null)
                    {
                        player1Character.ShowHappy();
                        Debug.Log($"[BattleController] [{role}] ✅ Player1Character.ShowHappy() DONE");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] [{role}] ⚠️ player1Character is NULL!");
                    }
                    
                    Debug.Log($"[BattleController] [{role}] Calling Player2Character.ShowSad()...");
                    if (player2Character != null)
                    {
                        player2Character.ShowSad();
                        Debug.Log($"[BattleController] [{role}] ✅ Player2Character.ShowSad() DONE");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] [{role}] ⚠️ player2Character is NULL!");
                    }
                }
                else
                {
                    // 1 đúng 1 sai → P1 Attack THEN Happy, P2 Sad
                    Debug.Log($"[BattleController] [{role}] P1 correct, P2 wrong → P1 ShowAttackThenHappy(), P2 ShowSad()");
                    Debug.Log($"[BattleController] [{role}] Calling Player1Character.ShowAttackThenHappy()...");
                    if (player1Character != null)
                    {
                        player1Character.ShowAttackThenHappy();
                        Debug.Log($"[BattleController] [{role}] ✅ Player1Character.ShowAttackThenHappy() DONE");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] [{role}] ⚠️ player1Character is NULL!");
                    }
                    
                    Debug.Log($"[BattleController] [{role}] Calling Player2Character.ShowSad()...");
                    if (player2Character != null)
                    {
                        player2Character.ShowSad();
                        Debug.Log($"[BattleController] [{role}] ✅ Player2Character.ShowSad() DONE");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] [{role}] ⚠️ player2Character is NULL!");
                    }
                }
            }
            else if (winnerId == 1)
            {
                // Player2 thắng câu (đúng hoặc nhanh hơn)
                // ✅ FIXED LOGIC: Kiểm tra bothCorrect để phân biệt "cả 2 đúng" vs "1 đúng 1 sai"
                
                Debug.Log($"[BattleController] [{role}] Case: Player2 thắng (bothCorrect={bothCorrect})");
                
                if (bothCorrect)
                {
                    // Cả 2 đúng, P2 nhanh hơn → P2 Happy, P1 Sad
                    Debug.Log($"[BattleController] [{role}] Both correct, P2 faster → P2 ShowHappy(), P1 ShowSad()");
                    Debug.Log($"[BattleController] [{role}] Calling Player2Character.ShowHappy()...");
                    if (player2Character != null)
                    {
                        player2Character.ShowHappy();
                        Debug.Log($"[BattleController] [{role}] ✅ Player2Character.ShowHappy() DONE");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] [{role}] ⚠️ player2Character is NULL!");
                    }
                    
                    Debug.Log($"[BattleController] [{role}] Calling Player1Character.ShowSad()...");
                    if (player1Character != null)
                    {
                        player1Character.ShowSad();
                        Debug.Log($"[BattleController] [{role}] ✅ Player1Character.ShowSad() DONE");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] [{role}] ⚠️ player1Character is NULL!");
                    }
                }
                else
                {
                    // 1 đúng 1 sai → P2 Attack THEN Happy, P1 Sad
                    Debug.Log($"[BattleController] [{role}] P2 correct, P1 wrong → P2 ShowAttackThenHappy(), P1 ShowSad()");
                    Debug.Log($"[BattleController] [{role}] Calling Player2Character.ShowAttackThenHappy()...");
                    if (player2Character != null)
                    {
                        player2Character.ShowAttackThenHappy();
                        Debug.Log($"[BattleController] [{role}] ✅ Player2Character.ShowAttackThenHappy() DONE");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] [{role}] ⚠️ player2Character is NULL!");
                    }
                    
                    Debug.Log($"[BattleController] [{role}] Calling Player1Character.ShowSad()...");
                    if (player1Character != null)
                    {
                        player1Character.ShowSad();
                        Debug.Log($"[BattleController] [{role}] ✅ Player1Character.ShowSad() DONE");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] [{role}] ⚠️ player1Character is NULL!");
                    }
                }
            }
            
            Debug.Log($"[BattleController] [{role}] ===== AVATAR ANIMATION: Summary Time COMPLETE =====");
            Debug.Log($"[BattleController] [{role}] ===== HandleAnswerResultDetailed COMPLETE =====");
        }

        /// <summary>
        /// Xử lý khi trận đấu kết thúc (hết máu hoặc đối thủ bỏ cuộc).
        /// Người bỏ cuộc đã tự về LobbyPanel rồi — chỉ người còn lại mới nhận event này.
        /// 
        /// ✅ FIX: Guard để tránh duplicate call (event + ClientRpc)
        /// </summary>
        private bool hasHandledMatchEnd = false;
        
        private void HandleMatchEnded(int winnerId, int winnerHealth)
        {
            // ✅ Guard: Chỉ xử lý 1 lần duy nhất
            if (hasHandledMatchEnd)
            {
                var net = NetworkManager.Singleton;
                string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
                GameLogger.Log($"[BattleController] [{role}] HandleMatchEnded IGNORED - already handled");
                return;
            }
            hasHandledMatchEnd = true;
            
            Log($"Match ended: Winner={winnerId}, Health={winnerHealth}");
            
            var net2 = NetworkManager.Singleton;
            string role2 = (net2 != null && net2.IsServer) ? "HOST" : "CLIENT";
            int localPlayerId = (net2 != null && net2.IsHost) ? 0 : 1;
            bool isAbandoned = battleManager != null && battleManager.IsAbandoned.Value;
            int abandonedPlayerId = battleManager != null ? battleManager.AbandonedPlayerId.Value : -1;
            
            GameLogger.Log($"[BattleController] [{role2}] HandleMatchEnded RECEIVED - winnerId={winnerId}, winnerHealth={winnerHealth}");
            GameLogger.Log($"[BattleController] [{role2}] LocalPlayerId={localPlayerId}, IsAbandoned={isAbandoned}, AbandonedPlayerId={abandonedPlayerId}");

            DragAndDrop.SetGlobalLock(true);

            // Push data vào WinsController cache NGAY BÂY GIỜ, trước khi player states bị destroy
            GameLogger.Log($"[BattleController] [{role2}] Pushing match result to WinsController...");
            PushMatchResultToWinsController(winnerId, localPlayerId);

            // Sync Firebase phía client (uid của client chỉ client biết)
            // ✅ FIX: Chỉ skip sync cho người BỎ CUỘC, người THẮNG vẫn được sync
            bool isLocalPlayerAbandoned = (isAbandoned && abandonedPlayerId == localPlayerId);
            
            if (!isLocalPlayerAbandoned)
            {
                GameLogger.Log($"[BattleController] [{role2}] Syncing own match result to Firebase...");
                SyncOwnMatchResult(winnerId);
            }
            else
            {
                GameLogger.Log($"[BattleController] [{role2}] Local player abandoned - skipping Firebase sync");
            }

            bool isLocalWinner = (winnerId == localPlayerId);
            if (battleStatusText != null)
            {
                battleStatusText.text = isLocalWinner
                    ? "<color=green><size=40>CHIẾN THẮNG!</size></color>"
                    : "<color=red><size=40>THUA CUỘC!</size></color>";
            }
            
            GameLogger.Log($"[BattleController] [{role2}] IsLocalWinner={isLocalWinner}, navigating to WinsPanel in 2s...");
            Invoke(nameof(NavigateToWinsPanel), 2f);
        }

        /// <summary>
        /// Push thông tin trận đấu vào UIWinsController.LastResult NGAY KHI nhận OnMatchEnded,
        /// trước khi NetworkedPlayerState bị destroy do disconnect.
        /// </summary>
        private void PushMatchResultToWinsController(int winnerId, int localPlayerId)
        {
            if (battleManager == null) 
            {
                GameLogger.Log("[BattleController] PushMatchResult: battleManager is NULL - cannot push data");
                return;
            }

            var p1 = battleManager.GetPlayer1State();
            var p2 = battleManager.GetPlayer2State();

            if (p1 == null || p2 == null)
            {
                // ✅ FIX: Player states null khi forfeit — build result từ NetworkVariables trực tiếp
                // (NetworkVariables vẫn còn giá trị dù player state object bị destroy)
                GameLogger.Log($"[BattleController] ⚠️ PushMatchResult: Player states NULL (p1={p1 != null}, p2={p2 != null}) — building from NetworkVariables");
                PushMatchResultFromNetworkVariables(winnerId, localPlayerId);
                return;
            }

            var winner = (winnerId == 0) ? p1 : p2;
            var loser  = (winnerId == 0) ? p2 : p1;

            // ✅ FIX: Đọc HP từ cache (gửi qua ClientRpc) thay vì từ NetworkVariable (có thể chưa sync)
            int winnerHealth = -1;
            int loserHealth  = -1;
            
            var net = NetworkManager.Singleton;
            bool isHost = (net != null && net.IsHost);
            
            if (isHost)
            {
                // Host đọc trực tiếp từ player states (đã sync)
                winnerHealth = winner.CurrentHealth.Value;
                loserHealth  = loser.CurrentHealth.Value;
                GameLogger.Log($"[BattleController] HOST reading HP from player states: Winner={winnerHealth}, Loser={loserHealth}");
            }
            else
            {
                // Client đọc từ cache (gửi qua ClientRpc)
                int p1Health = battleManager.cachedPlayer1Health;
                int p2Health = battleManager.cachedPlayer2Health;
                
                if (p1Health == -1 || p2Health == -1)
                {
                    // Fallback: cache chưa có → đọc từ player states (có thể sai)
                    GameLogger.Log($"[BattleController] ⚠️ CLIENT cache not ready (P1={p1Health}, P2={p2Health}), falling back to player states");
                    winnerHealth = winner.CurrentHealth.Value;
                    loserHealth  = loser.CurrentHealth.Value;
                }
                else
                {
                    // Đọc từ cache
                    winnerHealth = (winnerId == 0) ? p1Health : p2Health;
                    loserHealth  = (winnerId == 0) ? p2Health : p1Health;
                    GameLogger.Log($"[BattleController] CLIENT reading HP from cache: Winner={winnerHealth}, Loser={loserHealth}");
                }
            }

            // ✅ DEBUG: Log loser's health before reading
            GameLogger.Log($"[BattleController] DEBUG: Loser ({loser.PlayerName.Value}) FinalHealth={loserHealth}, CurrentHealth.Value={loser.CurrentHealth.Value}, MaxHealth.Value={loser.MaxHealth.Value}, IsAlive={loser.IsAlive()}");

            UIWinsController.LastResult = new UIWinsController.MatchResultData
            {
                IsValid           = true,
                WinnerId          = winnerId,
                LocalPlayerId     = localPlayerId,
                IsAbandoned       = battleManager.IsAbandoned.Value,
                AbandonedPlayerId = battleManager.AbandonedPlayerId.Value,
                WinnerName        = winner.PlayerName.Value.ToString(),
                WinnerScore       = winner.Score.Value,
                WinnerHealth      = winnerHealth,
                LoserName         = loser.PlayerName.Value.ToString(),
                LoserScore        = loser.Score.Value,
                LoserHealth       = loserHealth,
            };

            Log($"PushMatchResult: winner={UIWinsController.LastResult.WinnerName}, loser={UIWinsController.LastResult.LoserName}");
            GameLogger.Log($"[BattleController] ✅ PushMatchResult SUCCESS:");
            GameLogger.Log($"  - Winner: {UIWinsController.LastResult.WinnerName} (Score:{UIWinsController.LastResult.WinnerScore}, HP:{UIWinsController.LastResult.WinnerHealth})");
            GameLogger.Log($"  - Loser: {UIWinsController.LastResult.LoserName} (Score:{UIWinsController.LastResult.LoserScore}, HP:{UIWinsController.LastResult.LoserHealth})");
            GameLogger.Log($"  - IsAbandoned: {UIWinsController.LastResult.IsAbandoned}, AbandonedPlayerId: {UIWinsController.LastResult.AbandonedPlayerId}");
        }

        /// <summary>
        /// Fallback: build MatchResultData từ NetworkVariables của BattleManager khi player states đã null.
        /// Dùng cho trường hợp forfeit — người quit disconnect trước khi người thắng đọc được player state.
        /// </summary>
        private void PushMatchResultFromNetworkVariables(int winnerId, int localPlayerId)
        {
            if (battleManager == null) return;

            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";

            // Lấy tên từ AuthManager cho local player
            string localName = "Người chơi";
            var authMgr = AuthManager.Instance;
            if (authMgr != null)
            {
                string charName = authMgr.GetCharacterName();
                if (!string.IsNullOrWhiteSpace(charName) && charName != "Unknown")
                    localName = charName;
            }

            // Xác định tên đối thủ — khi forfeit, đối thủ đã disconnect nên dùng placeholder
            string opponentName = "Đối thủ";

            // Xác định winner/loser name dựa trên localPlayerId
            string winnerName = (winnerId == localPlayerId) ? localName : opponentName;
            string loserName  = (winnerId == localPlayerId) ? opponentName : localName;

            // Score: lấy từ NetworkVariable nếu còn, fallback 0
            int winnerScore = 0;
            int loserScore  = 0;
            int winnerHealth = 0;
            int loserHealth  = 0;

            // Thử lấy từ player states một lần nữa (có thể đã được re-found)
            var p1 = battleManager.GetPlayer1State();
            var p2 = battleManager.GetPlayer2State();
            if (p1 != null && p2 != null)
            {
                var winner = (winnerId == 0) ? p1 : p2;
                var loser  = (winnerId == 0) ? p2 : p1;
                winnerScore  = winner.Score.Value;
                loserScore   = loser.Score.Value;
                winnerHealth = winner.CurrentHealth.Value;
                loserHealth  = loser.CurrentHealth.Value;
                winnerName   = winner.PlayerName.Value.ToString();
                loserName    = loser.PlayerName.Value.ToString();
                GameLogger.Log($"[BattleController] [{role}] ✅ Re-found player states on retry");
            }
            else
            {
                GameLogger.Log($"[BattleController] [{role}] ⚠️ Player states still null — using name fallback, score=0");
            }

            UIWinsController.LastResult = new UIWinsController.MatchResultData
            {
                IsValid           = true,
                WinnerId          = winnerId,
                LocalPlayerId     = localPlayerId,
                IsAbandoned       = battleManager.IsAbandoned.Value,
                AbandonedPlayerId = battleManager.AbandonedPlayerId.Value,
                WinnerName        = winnerName,
                WinnerScore       = winnerScore,
                WinnerHealth      = winnerHealth,
                LoserName         = loserName,
                LoserScore        = loserScore,
                LoserHealth       = loserHealth,
            };

            GameLogger.Log($"[BattleController] [{role}] ✅ PushMatchResult (fallback) SUCCESS:");
            GameLogger.Log($"  - Winner: {UIWinsController.LastResult.WinnerName} (Score:{UIWinsController.LastResult.WinnerScore}, HP:{UIWinsController.LastResult.WinnerHealth})");
            GameLogger.Log($"  - Loser: {UIWinsController.LastResult.LoserName} (Score:{UIWinsController.LastResult.LoserScore}, HP:{UIWinsController.LastResult.LoserHealth})");
            GameLogger.Log($"  - IsAbandoned: {UIWinsController.LastResult.IsAbandoned}, AbandonedPlayerId: {UIWinsController.LastResult.AbandonedPlayerId}");
        }

        /// <summary>
        /// Navigate to Wins panel — dùng Show() trực tiếp thay vì navigator.NavigateNow()
        /// vì UIButtonScreenNavigator dùng SetActive() không trigger OnShow() → DisplayMatchResult() không chạy.
        /// </summary>
        private void NavigateToWinsPanel()
        {
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            GameLogger.Log($"[BattleController] [{role}] NavigateToWinsPanel called");

            // ✅ Tìm WinsController và gọi Show() để OnShow() → DisplayMatchResult() được trigger
            var winsController = FindObjectOfType<UIWinsController>(true);
            if (winsController != null)
            {
                GameLogger.Log($"[BattleController] [{role}] Found WinsController, calling Show()...");
                
                // Ẩn GameplayPanel trước
                Hide();
                
                // Hiện WinsPanel qua Show() để OnShow() được gọi
                winsController.Show();
                GameLogger.Log($"[BattleController] [{role}] ✅ WinsController.Show() called");
            }
            else if (matchEndNavigator != null)
            {
                // Fallback: dùng navigator nếu không tìm thấy WinsController
                GameLogger.Log($"[BattleController] [{role}] WinsController not found, fallback to matchEndNavigator");
                matchEndNavigator.NavigateNow();
            }
            else
            {
                Debug.LogWarning("[BattleController] matchEndNavigator is not assigned and WinsController not found!");
                GameLogger.Log($"[BattleController] [{role}] ❌ Cannot navigate to WinsPanel - no navigator or controller found");
            }
        }

        /// <summary>
        /// Cả Host và Client đều tự sync kết quả của mình lên Firebase.
        /// Host đã sync trong NetworkedMathBattleManager.EndMatch(), nhưng gọi lại ở đây
        /// để đảm bảo Client cũng được sync (Host không biết uid của Client).
        /// </summary>
        private void SyncOwnMatchResult(int winnerId)
        {
            var net = NetworkManager.Singleton;
            if (net == null) return;

            // Xác định local player là Player 0 (Host) hay Player 1 (Client)
            bool isHost      = net.IsHost;
            int  localPlayer = isHost ? 0 : 1;
            bool isWin       = (winnerId == localPlayer);

            string role = isHost ? "HOST" : "CLIENT";
            GameLogger.Log($"[BattleController] [{role}] SyncOwnMatchResult START - localPlayer={localPlayer}, winnerId={winnerId}, isWin={isWin}");

            // Lấy điểm của local player từ NetworkedPlayerState
            int score = 0;
            if (battleManager != null)
            {
                var state = isHost ? battleManager.GetPlayer1State() : battleManager.GetPlayer2State();
                if (state != null)
                {
                    score = state.Score.Value;
                    GameLogger.Log($"[BattleController] [{role}] Read score from PlayerState: {score}");
                }
                else
                {
                    GameLogger.Log($"[BattleController] [{role}] ⚠️ PlayerState is NULL!");
                }
            }
            else
            {
                GameLogger.Log($"[BattleController] [{role}] ⚠️ BattleManager is NULL!");
            }

            // Sync lên Firebase qua CloudSyncService
            var cloudSync = DoAnGame.Auth.CloudSyncService.Instance;
            if (cloudSync != null)
            {
                GameLogger.Log($"[BattleController] [{role}] Calling CloudSyncService.OnMultiplayerMatchCompleted(score={score}, isWin={isWin})");
                cloudSync.OnMultiplayerMatchCompleted(score, isWin);
            }
            else
            {
                GameLogger.Log($"[BattleController] [{role}] ⚠️ CloudSyncService is NULL!");
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
