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

                // Title
                if (trangThaiText != null)
                {
                    trangThaiText.text  = isLocalWinner ? "CHIẾN THẮNG!" : "THUA CUỘC!";
                    trangThaiText.color = isLocalWinner ? Color.green : Color.red;
                    GameLogger.Log($"[WinsController] [{role}] ✅ Set title: {trangThaiText.text}");
                }
                else
                {
                    GameLogger.Log($"[WinsController] [{role}] ⚠️ trangThaiText is NULL");
                }

                // Winner section
                string winnerDisplay = r.WinnerName;
                if (r.WinnerId == r.LocalPlayerId) winnerDisplay += " (Bạn)";
                GameLogger.Log($"[WinsController] [{role}] Displaying winner section: {winnerDisplay}");
                DisplaySection(winContainer, winnerNameText, winnerScoreText, winnerHealthText,
                               winnerDisplay, r.WinnerScore, r.WinnerHealth);

                // Loser section
                string loserDisplay = r.LoserName;
                int    loserId      = (r.WinnerId == 0) ? 1 : 0;
                if (loserId == r.LocalPlayerId) loserDisplay += " (Bạn)";
                if (r.IsAbandoned && r.AbandonedPlayerId == loserId) loserDisplay += " (Đã Rời Trận)";
                GameLogger.Log($"[WinsController] [{role}] Displaying loser section: {loserDisplay}");
                DisplaySection(lostContainer, loserNameText, loserScoreText, loserHealthText,
                               loserDisplay, r.LoserScore, r.LoserHealth);

                GameLogger.Log($"[WinsController] [{role}] ✅ DisplayMatchResult COMPLETE");
                Log("Match result displayed successfully");
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
                nameText.text = name;
                GameLogger.Log($"[WinsController] [{role}] ✅ Set name: {name}");
            }
            else
            {
                GameLogger.Log($"[WinsController] [{role}] ⚠️ nameText is NULL");
            }
            
            if (scoreText != null)
            {
                scoreText.text = $"Điểm Số: {score}";
                GameLogger.Log($"[WinsController] [{role}] ✅ Set score: {score}");
            }
            else
            {
                GameLogger.Log($"[WinsController] [{role}] ⚠️ scoreText is NULL");
            }
            
            if (healthText != null)
            {
                healthText.text = $"Số Máu Còn Lại: {health}";
                GameLogger.Log($"[WinsController] [{role}] ✅ Set health: {health}");
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
