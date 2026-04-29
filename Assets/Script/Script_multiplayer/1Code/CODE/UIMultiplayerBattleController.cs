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
        [Header("Role Texts")]
        [SerializeField] private TMP_Text topPlayerText;
        [SerializeField] private TMP_Text bottomPlayerText;
        [SerializeField] private TMP_Text battleStatusText;

        [Header("Optional Texts")]
        [SerializeField] private TMP_Text roomInfoText;

        [Header("Battle System")]
        [SerializeField] private NetworkedMathBattleManager battleManager;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private GameObject answerSlot; // Slot để thả đáp án
        [SerializeField] private MultiplayerDragAndDrop[] answerChoices; // 3-4 đáp án kéo được (MULTIPLAYER)

        [Header("Fallback")]
        [SerializeField] private string localPlayerLabel = "Player 1 - người chơi";
        [SerializeField] private string enemyPlayerLabel = "Player 2 - đối thủ";
        [SerializeField] private string aiEnemyLabel = "Máy AI - đối thủ";

        [Header("Session End Handling")]
        [SerializeField] private UIMultiplayerRoomController roomController;
        [SerializeField] private bool resetRoomStateOnSessionEnded = true;
        [SerializeField] private UIButtonScreenNavigator onSessionEndedNavigator;
        [SerializeField] private UIButtonScreenNavigator sessionEndedFallbackNavigator;
        [SerializeField] private bool autoResolveSessionEndedNavigator = true;
        [SerializeField] private bool autoNavigateOnSessionEnded = true;
        [SerializeField] private float sessionCheckInterval = 0.5f;
        [SerializeField] private bool enableDebugLogs;

        private bool hadTwoPlayersInSession;
        private bool sessionEndedHandled;
        private float nextSessionCheckAt;

        protected override void OnShow()
        {
            base.OnShow();
            HandlePanelActivated();
        }

        private void OnEnable()
        {
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
            BindRoles();
            RegisterNetCallbacks();
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
        /// Subscribe vào events của NetworkedMathBattleManager
        /// </summary>
        private void SubscribeBattleEvents()
        {
            if (battleManager == null)
            {
                Log("BattleManager is null, cannot subscribe to events");
                return;
            }

            battleManager.OnQuestionGenerated -= HandleQuestionGenerated;
            battleManager.OnQuestionGenerated += HandleQuestionGenerated;

            battleManager.OnAnswerResult -= HandleAnswerResult;
            battleManager.OnAnswerResult += HandleAnswerResult;

            battleManager.OnMatchEnded -= HandleMatchEnded;
            battleManager.OnMatchEnded += HandleMatchEnded;

            Log("Subscribed to BattleManager events");
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

            // Hiển thị đáp án
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
                    answerChoices[i].ForceResetPosition();
                    
                    Log($"✅ Updated choice {i}: {choices[i]}");
                }
            }

            // Enable dragging
            DragAndDrop.SetGlobalLock(false);

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

            // Hiển thị đáp án vào các DragAndDrop objects
            if (answerChoices != null)
            {
                for (int i = 0; i < answerChoices.Length && i < choices.Length; i++)
                {
                    if (answerChoices[i] == null || answerChoices[i].myText == null)
                        continue;

                    answerChoices[i].myText.text = choices[i].ToString();
                    
                    // Reset position và enable dragging
                    answerChoices[i].ForceResetPosition();
                }
            }

            // Enable dragging
            DragAndDrop.SetGlobalLock(false);

            // Update status
            if (battleStatusText != null)
            {
                battleStatusText.text = "Kéo đáp án vào ô!";
            }
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
            Log($"Answer result: Winner={winnerId}, Correct={correct}, Time={responseTimeMs}ms");

            // Hiển thị kết quả trên drag-drop objects
            var net = NetworkManager.Singleton;
            bool isLocalWinner = net != null && ((net.IsHost && winnerId == 0) || (!net.IsHost && winnerId == 1));
            
            // Tìm tất cả MultiplayerDragAndDrop
            var dragObjects = FindObjectsOfType<DoAnGame.Multiplayer.MultiplayerDragAndDrop>();
            foreach (var drag in dragObjects)
            {
                if (winnerId == -1)
                {
                    // Cả 2 sai
                    drag.ShowResult(false);
                }
                else if (isLocalWinner)
                {
                    // Local player thắng
                    drag.ShowResult(true);
                }
                else
                {
                    // Đối thủ thắng
                    drag.ShowResult(false);
                }
            }

            // Hiển thị kết quả text
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

            // TODO: Thêm animation, sound effects, particle effects
        }

        /// <summary>
        /// Xử lý khi trận đấu kết thúc
        /// </summary>
        private void HandleMatchEnded(int winnerId, int winnerHealth)
        {
            Log($"Match ended: Winner={winnerId}, Health={winnerHealth}");

            // Disable dragging
            DragAndDrop.SetGlobalLock(true);

            // Hiển thị kết quả
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

            // TODO: Hiển thị màn hình kết quả chi tiết
            // ShowResultPanel(winnerId);
        }

        #endregion

        private void BindRoles()
        {
            try
            {
                var net = NetworkManager.Singleton;
                if (net == null || (!net.IsClient && !net.IsServer))
                {
                    // Trường hợp test UI offline: vẫn hiển thị đúng bố cục player dưới - đối thủ trên.
                    SetBottom(localPlayerLabel);
                    SetTop(aiEnemyLabel);
                    battleStatusText?.SetText("Chế độ test offline");
                    roomInfoText?.SetText("Room: Local Test");
                    return;
                }

                int count = net.ConnectedClientsIds.Count;
                bool hasOpponent = count >= 2;
                if (hasOpponent)
                {
                    hadTwoPlayersInSession = true;
                }

                if (net.IsHost)
                {
                    SetBottom("Player 1 - chủ phòng");
                    SetTop(hasOpponent ? "Player 2 - người chơi" : aiEnemyLabel);
                }
                else
                {
                    SetBottom("Player 2 - bạn");
                    SetTop("Player 1 - chủ phòng");
                }

                battleStatusText?.SetText(hasOpponent ? "Đang đấu 1v1" : "Đang chờ đối thủ...");
                roomInfoText?.SetText($"Connected: {count}/2");
                MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_BATTLE", $"BindRoles success, hasOpponent={hasOpponent}, connected={count}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[UIBattle] BindRoles error: {ex.Message}");
                MultiplayerDetailedLogger.TraceException("UI_BATTLE", ex, "BindRoles failed");
                // Safe fallback display
                SetBottom(localPlayerLabel);
                SetTop("Player 1 - đối thủ");
                battleStatusText?.SetText("Kết nối đang thiết lập...");
            }
        }

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

            BindRoles();
            Log($"Client connected: {clientId} | count={count}");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_BATTLE", $"HandleClientConnected clientId={clientId}, count={count}");
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            var net = NetworkManager.Singleton;
            int count = net != null ? net.ConnectedClientsIds.Count : 0;

            BindRoles();
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

        private void SetTop(string text)
        {
            topPlayerText?.SetText(text);
        }

        private void SetBottom(string text)
        {
            bottomPlayerText?.SetText(text);
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
