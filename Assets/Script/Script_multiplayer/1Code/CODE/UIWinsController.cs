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
            DisplayMatchResult();
        }

        protected override void OnHide()
        {
            base.OnHide();
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

                // Winner section - Hiển thị tên + "(Bạn)" nếu là local player
                string winnerDisplay = r.WinnerName;
                if (r.WinnerId == r.LocalPlayerId)
                {
                    winnerDisplay += " (Bạn)";
                    GameLogger.Log($"[WinsController] [{role}] Winner is LOCAL player");
                }
                else
                {
                    GameLogger.Log($"[WinsController] [{role}] Winner is OPPONENT");
                }
                GameLogger.Log($"[WinsController] [{role}] Displaying winner section: '{winnerDisplay}'");
                DisplaySection(winContainer, winnerNameText, winnerScoreText, winnerHealthText,
                               winnerDisplay, r.WinnerScore, r.WinnerHealth);

                // Loser section - Hiển thị tên + "(Bạn)" nếu là local player
                string loserDisplay = r.LoserName;
                int    loserId      = (r.WinnerId == 0) ? 1 : 0;
                if (loserId == r.LocalPlayerId)
                {
                    loserDisplay += " (Bạn)";
                    GameLogger.Log($"[WinsController] [{role}] Loser is LOCAL player");
                }
                else
                {
                    GameLogger.Log($"[WinsController] [{role}] Loser is OPPONENT");
                }
                if (r.IsAbandoned && r.AbandonedPlayerId == loserId)
                {
                    loserDisplay += " (Đã Rời Trận)";
                    GameLogger.Log($"[WinsController] [{role}] Loser abandoned the match");
                }
                GameLogger.Log($"[WinsController] [{role}] Displaying loser section: '{loserDisplay}'");
                DisplaySection(lostContainer, loserNameText, loserScoreText, loserHealthText,
                               loserDisplay, r.LoserScore, r.LoserHealth);

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

            LastResult = new MatchResultData
            {
                IsValid           = true,
                WinnerId          = winnerId,
                LocalPlayerId     = localId,
                IsAbandoned       = battleManager.IsAbandoned.Value,
                AbandonedPlayerId = battleManager.AbandonedPlayerId.Value,
                WinnerName        = winner.PlayerName.Value.ToString(),
                WinnerScore       = winner.Score.Value,
                WinnerHealth      = winner.CurrentHealth.Value,
                LoserName         = loser.PlayerName.Value.ToString(),
                LoserScore        = loser.Score.Value,
                LoserHealth       = loser.CurrentHealth.Value,
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

            int winnerAvatarId = 0;
            int loserAvatarId  = 0;

            // Lấy avatarId từ NetworkedPlayerState nếu còn sống
            if (battleManager != null)
            {
                var p1 = battleManager.GetPlayer1State();
                var p2 = battleManager.GetPlayer2State();

                Debug.Log($"[WinsController] [{role}] Player1State: {(p1 != null ? "FOUND" : "NULL")}");
                Debug.Log($"[WinsController] [{role}] Player2State: {(p2 != null ? "FOUND" : "NULL")}");

                var winnerState = (r.WinnerId == 0) ? p1 : p2;
                var loserState  = (r.WinnerId == 0) ? p2 : p1;

                if (winnerState != null)
                {
                    winnerAvatarId = winnerState.AvatarId.Value;
                    Debug.Log($"[WinsController] [{role}] Winner (Player{r.WinnerId + 1}) AvatarId from PlayerState: {winnerAvatarId}");
                }
                else
                {
                    Debug.LogWarning($"[WinsController] [{role}] Winner PlayerState is NULL!");
                }
                
                if (loserState  != null)
                {
                    loserAvatarId  = loserState.AvatarId.Value;
                    Debug.Log($"[WinsController] [{role}] Loser (Player{(r.WinnerId == 0 ? 2 : 1)}) AvatarId from PlayerState: {loserAvatarId}");
                }
                else
                {
                    Debug.LogWarning($"[WinsController] [{role}] Loser PlayerState is NULL!");
                }
            }
            else
            {
                Debug.LogWarning($"[WinsController] [{role}] BattleManager is NULL!");
            }

            // Fallback: local player dùng AvatarManager
            bool isLocalWinner = (r.WinnerId == r.LocalPlayerId);
            int localAvatarId  = AvatarManager.Instance?.GetCurrentAvatarId() ?? 0;
            Debug.Log($"[WinsController] [{role}] IsLocalWinner: {isLocalWinner}, LocalAvatarId from AvatarManager: {localAvatarId}");

            if (isLocalWinner)
            {
                winnerAvatarId = localAvatarId;
                Debug.Log($"[WinsController] [{role}] Using local avatar for winner: {winnerAvatarId}");
            }
            else
            {
                loserAvatarId = localAvatarId;
                Debug.Log($"[WinsController] [{role}] Using local avatar for loser: {loserAvatarId}");
            }

            // ✅ FIX: Apply avatar WITHOUT animation, then trigger Happy/Sad
            // ✅ CRITICAL: Ensure only 1 PSB is visible at a time
            if (winnerCharacter != null)
            {
                Debug.Log($"[WinsController] [{role}] Calling winnerCharacter.SetAvatarWithoutAnimation({winnerAvatarId})...");
                winnerCharacter.SetAvatarWithoutAnimation(winnerAvatarId);
                
                // ✅ CRITICAL: Delay để đảm bảo SetAvatarWithoutAnimation hoàn tất trước khi ShowHappy
                // Tránh race condition giữa SetAvatar và ShowHappy
                Debug.Log($"[WinsController] [{role}] Scheduling winnerCharacter.ShowHappy() after 0.1s delay...");
                StartCoroutine(DelayedShowAnimation(winnerCharacter, true, 0.1f));
            }
            else
            {
                Debug.LogWarning($"[WinsController] [{role}] ⚠️ winnerCharacter is NULL!");
            }

            if (loserCharacter != null)
            {
                Debug.Log($"[WinsController] [{role}] Calling loserCharacter.SetAvatarWithoutAnimation({loserAvatarId})...");
                loserCharacter.SetAvatarWithoutAnimation(loserAvatarId);
                
                // ✅ CRITICAL: Delay để đảm bảo SetAvatarWithoutAnimation hoàn tất trước khi ShowSad
                Debug.Log($"[WinsController] [{role}] Scheduling loserCharacter.ShowSad() after 0.1s delay...");
                StartCoroutine(DelayedShowAnimation(loserCharacter, false, 0.1f));
            }
            else
            {
                Debug.LogWarning($"[WinsController] [{role}] ⚠️ loserCharacter is NULL!");
            }

            Log($"SetWinsPanelAvatars: winner avatarId={winnerAvatarId}, loser avatarId={loserAvatarId}");
            Debug.Log($"[WinsController] [{role}] ===== SetWinsPanelAvatars COMPLETE =====");
        }

        /// <summary>
        /// Delay trước khi gọi ShowHappy/ShowSad để đảm bảo SetAvatarWithoutAnimation hoàn tất.
        /// Tránh race condition giữa SetAvatar và Show animation.
        /// </summary>
        private System.Collections.IEnumerator DelayedShowAnimation(AvatarCharacterDisplay character, bool isHappy, float delay)
        {
            yield return new WaitForSeconds(delay);
            
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
