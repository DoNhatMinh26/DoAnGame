using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Multiplayer;
using Unity.Netcode;
using System.Linq;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI Controller cho Wins Panel - Hiển thị kết quả trận đấu
    /// 
    /// LOGIC:
    /// - UIMultiplayerBattleController push MatchResultData trước khi navigate
    /// - WinsController đọc từ cache (không phụ thuộc NetworkedPlayerState còn sống không)
    /// </summary>
    public class UIWinsController : BasePanelController
    {
        // ─── Cache data (push từ UIMultiplayerBattleController trước khi navigate) ───
        public struct MatchResultData
        {
            public bool IsValid;
            public int  WinnerId;       // 0 hoặc 1
            public int  LocalPlayerId;  // 0 = Host, 1 = Client
            public bool IsAbandoned;
            public int  AbandonedPlayerId;

            public string WinnerName;
            public int    WinnerScore;
            public int    WinnerHealth;

            public string LoserName;
            public int    LoserScore;
            public int    LoserHealth;
        }

        // Static để tồn tại qua panel hide/show
        public static MatchResultData LastResult;

        [Header("=== TITLE ===")]
        [SerializeField] private TextMeshProUGUI trangThaiText;
        
        [Header("=== WINNER SECTION ===")]
        [SerializeField] private GameObject winContainer;
        [SerializeField] private TextMeshProUGUI winnerNameText;
        [SerializeField] private Image winnerImage;
        [SerializeField] private TextMeshProUGUI winnerScoreText;
        [SerializeField] private TextMeshProUGUI winnerHealthText;
        
        [Header("=== LOSER SECTION ===")]
        [SerializeField] private GameObject lostContainer;
        [SerializeField] private TextMeshProUGUI loserNameText;
        [SerializeField] private Image loserImage;
        [SerializeField] private TextMeshProUGUI loserScoreText;
        [SerializeField] private TextMeshProUGUI loserHealthText;
        
        [Header("=== BATTLE MANAGER ===")]
        [SerializeField] private NetworkedMathBattleManager battleManager;
        
        [Header("=== NAVIGATION ===")]
        [SerializeField] private UIButtonScreenNavigator backToLobbyNavigator;
        
        [Header("=== SETTINGS ===")]
        [SerializeField] private bool enableDebugLogs = true;

        [Header("=== AVATAR CHARACTERS ===")]
        [SerializeField] private AvatarCharacterDisplay winnerCharacter;  // Win_image_animation
        [SerializeField] private AvatarCharacterDisplay loserCharacter;   // Lost_image_animation

        private Coroutine winnerAnimationRoutine;
        private Coroutine loserAnimationRoutine;

        private void Start()
        {
            // Auto-resolve BattleManager nếu chưa gán (dùng cho fallback)
            if (battleManager == null)
                battleManager = NetworkedMathBattleManager.Instance;
        }

        protected override void OnShow()
        {
            base.OnShow();
            
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            GameLogger.Log($"[WinsController] [{role}] OnShow called");
            
            Log("OnShow called");
            StopPendingAnimationRoutines();
            DisplayMatchResult();
        }

        protected override void OnHide()
        {
            base.OnHide();
            StopPendingAnimationRoutines();
            Log("OnHide called");
        }

        /// <summary>
        /// Hiển thị kết quả từ LastResult cache (push bởi UIMultiplayerBattleController).
        /// Không phụ thuộc NetworkedPlayerState còn sống hay không.
        /// </summary>
        private void DisplayMatchResult()
        {
            try
            {
                var net = NetworkManager.Singleton;
                string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
                
                GameLogger.Log($"[WinsController] [{role}] DisplayMatchResult START");
                Log("DisplayMatchResult START");

                if (!LastResult.IsValid)
                {
                    // Fallback: thử đọc từ BattleManager nếu cache chưa có
                    GameLogger.Log($"[WinsController] [{role}] LastResult not valid, trying BattleManager fallback...");
                    Log("LastResult not valid, trying BattleManager fallback...");
                    if (!TryBuildResultFromBattleManager())
                    {
                        GameLogger.Log($"[WinsController] [{role}] ❌ Fallback failed - no data available");
                        SetErrorState("Không tìm thấy thông tin người chơi.");
                        return;
                    }
                    GameLogger.Log($"[WinsController] [{role}] ✅ Fallback success - data loaded from BattleManager");
                }

                var r = LastResult;
                GameLogger.Log($"[WinsController] [{role}] Data: WinnerId={r.WinnerId}, LocalPlayerId={r.LocalPlayerId}, IsAbandoned={r.IsAbandoned}");
                GameLogger.Log($"[WinsController] [{role}] Winner: {r.WinnerName} (Score:{r.WinnerScore}, HP:{r.WinnerHealth})");
                GameLogger.Log($"[WinsController] [{role}] Loser: {r.LoserName} (Score:{r.LoserScore}, HP:{r.LoserHealth})");
                Log($"WinnerId={r.WinnerId}, LocalPlayerId={r.LocalPlayerId}, IsAbandoned={r.IsAbandoned}");

                bool isLocalWinner = (r.WinnerId == r.LocalPlayerId);
                int localPlayerId = r.LocalPlayerId;
                int opponentPlayerId = (localPlayerId == 0) ? 1 : 0;

                string localDisplay = isLocalWinner ? r.WinnerName : r.LoserName;
                int localScore = isLocalWinner ? r.WinnerScore : r.LoserScore;
                int localHealth = isLocalWinner ? r.WinnerHealth : r.LoserHealth;

                string opponentDisplay = isLocalWinner ? r.LoserName : r.WinnerName;
                int opponentScore = isLocalWinner ? r.LoserScore : r.WinnerScore;
                int opponentHealth = isLocalWinner ? r.LoserHealth : r.WinnerHealth;

                if (!string.IsNullOrEmpty(localDisplay))
                {
                    localDisplay += " (Bạn)";
                }

                if (r.IsAbandoned && r.AbandonedPlayerId == localPlayerId)
                {
                    localDisplay += " (Đã Rời Trận)";
                }

                if (r.IsAbandoned && r.AbandonedPlayerId == opponentPlayerId)
                {
                    opponentDisplay += " (Đã Rời Trận)";
                }
                
                GameLogger.Log($"[WinsController] [{role}] ===== TEXT DISPLAY LOGIC =====");
                GameLogger.Log($"[WinsController] [{role}] WinnerId: {r.WinnerId}");
                GameLogger.Log($"[WinsController] [{role}] LocalPlayerId: {r.LocalPlayerId}");
                GameLogger.Log($"[WinsController] [{role}] IsLocalWinner: {isLocalWinner}");

                // Title - Hiển thị rõ ràng cho local player
                if (trangThaiText != null)
                {
                    string titleText = isLocalWinner ? "CHIẾN THẮNG!" : "THUA CUỘC!";
                    Color titleColor = isLocalWinner ? Color.green : Color.red;
                    
                    trangThaiText.SetText(titleText);  // ✅ Dùng SetText() thay vì .text
                    trangThaiText.color = titleColor;
                    
                    GameLogger.Log($"[WinsController] [{role}] ✅ Set title text: '{titleText}' (color: {(isLocalWinner ? "GREEN" : "RED")})");
                    Debug.Log($"[WinsController] [{role}] ✅ trangThaiText.text = '{trangThaiText.text}' (expected: '{titleText}')");
                }
                else
                {
                    GameLogger.Log($"[WinsController] [{role}] ⚠️ trangThaiText is NULL!");
                    Debug.LogError($"[WinsController] [{role}] ⚠️ trangThaiText is NULL - cannot display title!");
                }
                
                GameLogger.Log($"[WinsController] [{role}] ===== TEXT DISPLAY COMPLETE =====");

                // Left side = local player, right side = opponent
                GameLogger.Log($"[WinsController] [{role}] Displaying local section (left): '{localDisplay}'");
                DisplaySection(winContainer, winnerNameText, winnerScoreText, winnerHealthText,
                               localDisplay, localScore, localHealth);

                // Ensure any duplicate or decorative stat labels under the left/local container
                // also receive the updated values (robustness against scene variants).
                UpdateContainerStats(winContainer, localScore, localHealth);

                GameLogger.Log($"[WinsController] [{role}] Displaying opponent section (right): '{opponentDisplay}'");
                DisplaySection(lostContainer, loserNameText, loserScoreText, loserHealthText,
                               opponentDisplay, opponentScore, opponentHealth);

                // Ensure any duplicate or decorative stat labels under the right/opponent container
                // also receive the updated values.
                UpdateContainerStats(lostContainer, opponentScore, opponentHealth);

                GameLogger.Log($"[WinsController] [{role}] ✅ DisplayMatchResult COMPLETE");
                Log("Match result displayed successfully");

                // Set avatar và trigger animation Happy/Sad
                SetWinsPanelAvatars(r);
            }
            catch (System.Exception ex)
            {
                var net = NetworkManager.Singleton;
                string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
                Debug.LogError($"[WinsController] Error: {ex.Message}\n{ex.StackTrace}");
                GameLogger.Log($"[WinsController] [{role}] ❌ ERROR: {ex.Message}");
                SetErrorState("Có lỗi xảy ra khi hiển thị kết quả.");
            }
        }

        private void DisplaySection(GameObject container, TextMeshProUGUI nameText,
                                    TextMeshProUGUI scoreText, TextMeshProUGUI healthText,
                                    string name, int score, int health)
        {
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            
            if (container != null)
            {
                container.SetActive(true);
                GameLogger.Log($"[WinsController] [{role}] ✅ Activated container: {container.name}");
            }
            else
            {
                GameLogger.Log($"[WinsController] [{role}] ⚠️ Container is NULL");
            }
            
            if (nameText != null)
            {
                nameText.SetText(name);  // ✅ Dùng SetText() thay vì .text
                GameLogger.Log($"[WinsController] [{role}] ✅ Set name: '{name}' (actual: '{nameText.text}')");
            }
            else
            {
                GameLogger.Log($"[WinsController] [{role}] ⚠️ nameText is NULL");
            }
            
            if (scoreText != null)
            {
                string scoreDisplay = $"Điểm Số: {score}";
                scoreText.SetText(scoreDisplay);  // ✅ Dùng SetText() thay vì .text
                GameLogger.Log($"[WinsController] [{role}] ✅ Set score: '{scoreDisplay}'");
            }
            else
            {
                GameLogger.Log($"[WinsController] [{role}] ⚠️ scoreText is NULL");
            }
            
            if (healthText != null)
            {
                string healthDisplay = $"Số Máu Còn Lại: {health}";
                healthText.SetText(healthDisplay);  // ✅ Dùng SetText() thay vì .text
                GameLogger.Log($"[WinsController] [{role}] ✅ Set health: '{healthDisplay}'");
            }
            else
            {
                GameLogger.Log($"[WinsController] [{role}] ⚠️ healthText is NULL");
            }
        }

        /// <summary>
        /// Robustly update common stat text children under a container (handles duplicate
        /// or otherwise-named TMP text objects in the Win/Lost panels). This helps
        /// avoid cases where another TMP object (e.g. decorative/stat panel) still shows
        /// a stale value even though the main serialized field was updated.
        /// </summary>
        private void UpdateContainerStats(GameObject container, int score, int health)
        {
            if (container == null)
                return;

            var texts = container.GetComponentsInChildren<TextMeshProUGUI>(true);
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";

            GameLogger.Log($"[WinsController] [{role}] Debug: found {texts.Length} TMP children under '{container.name}'");

            foreach (var t in texts)
            {
                if (t == null || string.IsNullOrEmpty(t.gameObject.name))
                    continue;

                var n = t.gameObject.name.ToLowerInvariant();

                // Log existing value before change
                GameLogger.Log($"[WinsController] [{role}] Debug BEFORE: '{t.gameObject.name}' = '{t.text}'");

                // Common Vietnamese name patterns used in the scene exports
                if (n.Contains("diem") || n.Contains("diem_so") || n.Contains("diem_so_nhan"))
                {
                    t.SetText($"Điểm Số: {score}");
                    GameLogger.Log($"[WinsController] [{role}] Debug SET: '{t.gameObject.name}' -> 'Điểm Số: {score}'");
                    continue;
                }

                if (n.Contains("so_mau") || n.Contains("so_mau_con") || n.Contains("so_mau_con_lai"))
                {
                    t.SetText($"Số Máu Còn Lại: {health}");
                    GameLogger.Log($"[WinsController] [{role}] Debug SET: '{t.gameObject.name}' -> 'Số Máu Còn Lại: {health}'");
                    continue;
                }

                // Optionally log if we didn't match any pattern
                GameLogger.Log($"[WinsController] [{role}] Debug SKIP: '{t.gameObject.name}' (no pattern matched)");
            }
        }

        /// <summary>
        /// Fallback: đọc từ BattleManager nếu cache chưa được push
        /// (trường hợp trận kết thúc bình thường, không phải forfeit)
        /// </summary>
        private bool TryBuildResultFromBattleManager()
        {
            if (battleManager == null)
                battleManager = NetworkedMathBattleManager.Instance;

            if (battleManager == null || !battleManager.MatchEnded.Value)
                return false;

            var p1 = battleManager.GetPlayer1State();
            var p2 = battleManager.GetPlayer2State();
            if (p1 == null || p2 == null) return false;

            var net = NetworkManager.Singleton;
            int localId = (net != null && net.IsHost) ? 0 : 1;
            int winnerId = battleManager.WinnerId.Value;

            NetworkedPlayerState winner = (winnerId == 0) ? p1 : p2;
            NetworkedPlayerState loser  = (winnerId == 0) ? p2 : p1;

            int cachedP1Health = battleManager.cachedPlayer1Health;
            int cachedP2Health = battleManager.cachedPlayer2Health;
            int cachedP1Score = battleManager.cachedPlayer1Score;
            int cachedP2Score = battleManager.cachedPlayer2Score;

            int winnerHealth = (cachedP1Health >= 0 && cachedP2Health >= 0)
                ? ((winnerId == 0) ? cachedP1Health : cachedP2Health)
                : winner.CurrentHealth.Value;
            int loserHealth = (cachedP1Health >= 0 && cachedP2Health >= 0)
                ? ((winnerId == 0) ? cachedP2Health : cachedP1Health)
                : loser.CurrentHealth.Value;
            int winnerScore = (cachedP1Score >= 0 && cachedP2Score >= 0)
                ? ((winnerId == 0) ? cachedP1Score : cachedP2Score)
                : winner.Score.Value;
            int loserScore = (cachedP1Score >= 0 && cachedP2Score >= 0)
                ? ((winnerId == 0) ? cachedP2Score : cachedP1Score)
                : loser.Score.Value;

            LastResult = new MatchResultData
            {
                IsValid           = true,
                WinnerId          = winnerId,
                LocalPlayerId     = localId,
                IsAbandoned       = battleManager.IsAbandoned.Value,
                AbandonedPlayerId = battleManager.AbandonedPlayerId.Value,
                WinnerName        = winner.PlayerName.Value.ToString(),
                WinnerScore       = winnerScore,
                WinnerHealth      = winnerHealth,
                LoserName         = loser.PlayerName.Value.ToString(),
                LoserScore        = loserScore,
                LoserHealth       = loserHealth,
            };
            return true;
        }

        /// <summary>
        /// Set avatar đúng nhân vật và trigger Happy/Sad cho Wins panel.
        /// Winner → Happy, Loser → Sad.
        /// ✅ FIX: Dùng SetAvatarWithoutAnimation() để tránh double-trigger (Idle → Happy/Sad)
        /// ✅ FIX: Cleanup PSB visibility trước khi set avatar để tránh overlapping
        /// </summary>
        private void SetWinsPanelAvatars(MatchResultData r)
        {
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            
            Debug.Log($"[WinsController] [{role}] ===== SetWinsPanelAvatars START =====");
            Debug.Log($"[WinsController] [{role}] winnerCharacter: {(winnerCharacter != null ? winnerCharacter.gameObject.name : "NULL")}");
            Debug.Log($"[WinsController] [{role}] loserCharacter: {(loserCharacter != null ? loserCharacter.gameObject.name : "NULL")}");
            
            if (winnerCharacter == null && loserCharacter == null)
            {
                Debug.LogWarning($"[WinsController] [{role}] Both winnerCharacter and loserCharacter are NULL!");
                return;
            }

            int leftAvatarId = 0;
            int rightAvatarId  = 0;
            bool isLocalWinner = (r.WinnerId == r.LocalPlayerId);
            int localPlayerId = r.LocalPlayerId;
            int opponentPlayerId = (localPlayerId == 0) ? 1 : 0;

            // Lấy avatarId từ NetworkedPlayerState nếu còn sống
            if (battleManager != null)
            {
                var p1 = battleManager.GetPlayer1State();
                var p2 = battleManager.GetPlayer2State();

                Debug.Log($"[WinsController] [{role}] Player1State: {(p1 != null ? "FOUND" : "NULL")}");
                Debug.Log($"[WinsController] [{role}] Player2State: {(p2 != null ? "FOUND" : "NULL")}");

                var localState = (localPlayerId == 0) ? p1 : p2;
                var opponentState = (localPlayerId == 0) ? p2 : p1;

                if (localState != null)
                {
                    rightAvatarId = localState.AvatarId.Value;
                    Debug.Log($"[WinsController] [{role}] Local (Player{localPlayerId + 1}) AvatarId from PlayerState: {rightAvatarId}");
                }
                else
                {
                    Debug.LogWarning($"[WinsController] [{role}] Local PlayerState is NULL!");
                }
                
                if (opponentState != null)
                {
                    leftAvatarId = opponentState.AvatarId.Value;
                    Debug.Log($"[WinsController] [{role}] Opponent (Player{opponentPlayerId + 1}) AvatarId from PlayerState: {leftAvatarId}");
                }
                else
                {
                    Debug.LogWarning($"[WinsController] [{role}] Opponent PlayerState is NULL!");
                }
            }
            else
            {
                Debug.LogWarning($"[WinsController] [{role}] BattleManager is NULL!");
            }

            // Fallback: local player dùng AvatarManager
            int localAvatarId  = AvatarManager.Instance?.GetCurrentAvatarId() ?? 0;
            Debug.Log($"[WinsController] [{role}] IsLocalWinner: {isLocalWinner}, LocalAvatarId from AvatarManager: {localAvatarId}");

            if (rightAvatarId == 0)
            {
                rightAvatarId = localAvatarId;
                Debug.Log($"[WinsController] [{role}] Using local avatar for right/local side: {rightAvatarId}");
            }

            // ✅ FIX: Apply avatar WITHOUT animation, then trigger Happy/Sad
            // ✅ CRITICAL: Ensure only 1 PSB is visible at a time
            if (winnerCharacter != null)
            {
                Debug.Log($"[WinsController] [{role}] Calling winnerCharacter.SetAvatarWithoutAnimation({leftAvatarId})...");
                winnerCharacter.SetAvatarWithoutAnimation(leftAvatarId);
                
                // Left side shows local outcome
                bool leftIsHappy = isLocalWinner;
                Debug.Log($"[WinsController] [{role}] Scheduling winnerCharacter.{(leftIsHappy ? "ShowHappy" : "ShowSad")}() after 0.1s delay...");
                winnerAnimationRoutine = StartCoroutine(DelayedShowAnimation(winnerCharacter, leftIsHappy, 0.1f));
            }
            else
            {
                Debug.LogWarning($"[WinsController] [{role}] ⚠️ winnerCharacter is NULL!");
            }

            if (loserCharacter != null)
            {
                Debug.Log($"[WinsController] [{role}] Calling loserCharacter.SetAvatarWithoutAnimation({rightAvatarId})...");
                loserCharacter.SetAvatarWithoutAnimation(rightAvatarId);
                
                // Right side is always opponent player
                Debug.Log($"[WinsController] [{role}] Scheduling loserCharacter.{(!isLocalWinner ? "ShowHappy" : "ShowSad")}() after 0.1s delay...");
                loserAnimationRoutine = StartCoroutine(DelayedShowAnimation(loserCharacter, !isLocalWinner, 0.1f));
            }
            else
            {
                Debug.LogWarning($"[WinsController] [{role}] ⚠️ loserCharacter is NULL!");
            }

            Log($"SetWinsPanelAvatars: left avatarId={leftAvatarId}, right avatarId={rightAvatarId}");
            Debug.Log($"[WinsController] [{role}] ===== SetWinsPanelAvatars COMPLETE =====");
        }

        /// <summary>
        /// Delay trước khi gọi ShowHappy/ShowSad để đảm bảo SetAvatarWithoutAnimation hoàn tất.
        /// Tránh race condition giữa SetAvatar và Show animation.
        /// </summary>
        private System.Collections.IEnumerator DelayedShowAnimation(AvatarCharacterDisplay character, bool isHappy, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (character == null || !isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                yield break;
            }
            
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            
            if (isHappy)
            {
                Debug.Log($"[WinsController] [{role}] Calling {character.gameObject.name}.ShowHappy()...");
                character.ShowHappy();
                Debug.Log($"[WinsController] [{role}] ✅ {character.gameObject.name}.ShowHappy() DONE");
            }
            else
            {
                Debug.Log($"[WinsController] [{role}] Calling {character.gameObject.name}.ShowSad()...");
                character.ShowSad();
                Debug.Log($"[WinsController] [{role}] ✅ {character.gameObject.name}.ShowSad() DONE");
            }
        }

        private void StopPendingAnimationRoutines()
        {
            if (winnerAnimationRoutine != null)
            {
                StopCoroutine(winnerAnimationRoutine);
                winnerAnimationRoutine = null;
            }

            if (loserAnimationRoutine != null)
            {
                StopCoroutine(loserAnimationRoutine);
                loserAnimationRoutine = null;
            }
        }

        private void SetErrorState(string errorMessage)
        {
            if (trangThaiText != null)
            {
                trangThaiText.text  = errorMessage;
                trangThaiText.color = Color.red;
            }
            if (winContainer  != null) winContainer.SetActive(false);
            if (lostContainer != null) lostContainer.SetActive(false);
        }

        /// <summary>
        /// Public method để navigate về Lobby (có thể gọi từ button hoặc code)
        /// ✅ Dùng LoadingPanel Simple mode để tạo trải nghiệm mượt mà
        /// </summary>
        public void NavigateBackToLobby()
        {
            Log("NavigateBackToLobby called - Showing LoadingPanel then quitting room");

            // ✅ Ẩn WinsPanel
            Hide();

            // ✅ Hiển thị LoadingPanel với Simple mode
            var loadingPanel = FindObjectOfType<UIMultiplayerLoadingController>(true);
            if (loadingPanel != null)
            {
                Log("Found LoadingPanel, showing it with Simple mode...");
                loadingPanel.ShowSimpleLoading("Đã hoàn thành trận đấu...\nĐang rời phòng", 1.5f);
                
                // ✅ Delay 1.5 giây để người chơi thấy loading, sau đó quit room
                Invoke(nameof(QuitRoomAfterLoading), 1.5f);
            }
            else
            {
                Debug.LogWarning("[WinsController] LoadingPanel not found! Quitting room immediately.");
                QuitRoomAfterLoading();
            }
        }

        /// <summary>
        /// Quit room sau khi hiển thị LoadingPanel
        /// </summary>
        private void QuitRoomAfterLoading()
        {
            Log("QuitRoomAfterLoading called");

            // ✅ Tìm UIMultiplayerRoomController (có thể inactive nên dùng includeInactive)
            var roomController = FindObjectsOfType<UIMultiplayerRoomController>(true).FirstOrDefault();
            
            if (roomController != null)
            {
                Log("Found UIMultiplayerRoomController, calling RequestQuitRoom");
                roomController.RequestQuitRoom();
                // RequestQuitRoom sẽ tự động navigate về LobbyPanel qua quitRoomNavigator
            }
            else
            {
                Debug.LogWarning("[WinsController] UIMultiplayerRoomController not found! Fallback to direct navigation.");
                
                // Fallback: navigate trực tiếp nếu không tìm thấy controller
                if (backToLobbyNavigator != null)
                {
                    backToLobbyNavigator.NavigateNow();
                }
                else
                {
                    Debug.LogError("[WinsController] backToLobbyNavigator is not assigned!");
                }
            }
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[{nameof(UIWinsController)}] {message}");
        }
    }
}
