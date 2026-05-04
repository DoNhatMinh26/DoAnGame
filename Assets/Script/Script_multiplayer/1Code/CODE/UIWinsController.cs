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
    /// - Subscribe vào NetworkedMathBattleManager.OnMatchEnded
    /// - Lấy thông tin từ NetworkedPlayerState (tên, điểm, máu)
    /// - Xác định winner/loser từ WinnerId.Value
    /// - Hiển thị thông tin đúng cho Host và Client
    /// - Có nút "Tiếp tục" để quay về LobbyPanel
    /// </summary>
    public class UIWinsController : BasePanelController
    {
        [Header("=== TITLE ===")]
        [SerializeField] private TextMeshProUGUI trangThaiText; // "CHIẾN THẮNG!" / "THUA CUỘC!"
        
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
            // Auto-resolve BattleManager nếu chưa gán
            if (battleManager == null)
            {
                battleManager = NetworkedMathBattleManager.Instance;
                if (battleManager != null)
                {
                    Log($"Auto-resolved BattleManager: {battleManager.name}");
                }
                else
                {
                    Debug.LogWarning("[WinsController] BattleManager not found in Start");
                }
            }
            
            // ✅ FIX: Delay để đảm bảo NetworkVariables đã sync
            Invoke(nameof(DisplayMatchResult), 0.5f);
        }

        protected override void OnShow()
        {
            base.OnShow();
            Log("OnShow called");
            
            // Hiển thị kết quả trận đấu
            DisplayMatchResult();
        }

        protected override void OnHide()
        {
            base.OnHide();
            Log("OnHide called");
        }

        /// <summary>
        /// Hiển thị kết quả trận đấu
        /// </summary>
        private void DisplayMatchResult()
        {
            try
            {
                Log("DisplayMatchResult START");

                // Tìm BattleManager nếu chưa có
                if (battleManager == null)
                {
                    battleManager = NetworkedMathBattleManager.Instance;
                }

                if (battleManager == null)
                {
                    Debug.LogError("[WinsController] BattleManager is null! Cannot display match result.");
                    SetErrorState("Không thể tải kết quả trận đấu.");
                    return;
                }

                // ✅ Kiểm tra match đã kết thúc chưa
                if (!battleManager.MatchEnded.Value)
                {
                    Debug.LogWarning("[WinsController] Match has not ended yet! Retrying in 0.5s...");
                    Invoke(nameof(DisplayMatchResult), 0.5f);
                    return;
                }

                // Lấy winnerId
                int winnerId = battleManager.WinnerId.Value;
                Log($"WinnerId: {winnerId}");

                // ✅ Kiểm tra winnerId hợp lệ
                if (winnerId < 0 || winnerId > 1)
                {
                    Debug.LogError($"[WinsController] Invalid WinnerId: {winnerId}");
                    SetErrorState("Kết quả trận đấu không hợp lệ.");
                    return;
                }

                // Tìm player states
                var player1State = battleManager.GetPlayer1State();
                var player2State = battleManager.GetPlayer2State();

                if (player1State == null || player2State == null)
                {
                    Debug.LogError("[WinsController] Cannot find player states!");
                    SetErrorState("Không tìm thấy thông tin người chơi.");
                    return;
                }

                Log($"Player1: {player1State.PlayerName.Value}, Score={player1State.Score.Value}, HP={player1State.CurrentHealth.Value}");
                Log($"Player2: {player2State.PlayerName.Value}, Score={player2State.Score.Value}, HP={player2State.CurrentHealth.Value}");

                // Xác định winner và loser
                NetworkedPlayerState winner = (winnerId == 0) ? player1State : player2State;
                NetworkedPlayerState loser = (winnerId == 0) ? player2State : player1State;

                Log($"Winner: {winner.PlayerName.Value} (Player {winnerId})");
                Log($"Loser: {loser.PlayerName.Value} (Player {(winnerId == 0 ? 1 : 0)})");

                // Kiểm tra xem local player có phải winner không
                bool isLocalWinner = IsLocalPlayer(winner);
                Log($"Is local player winner: {isLocalWinner}");

                // Hiển thị title
                if (trangThaiText != null)
                {
                    trangThaiText.text = isLocalWinner ? "CHIẾN THẮNG!" : "THUA CUỘC!";
                    trangThaiText.color = isLocalWinner ? Color.green : Color.red;
                    Log($"Title set: {trangThaiText.text}");
                }
                else
                {
                    Debug.LogWarning("[WinsController] trangThaiText is NULL!");
                }

                // Hiển thị thông tin winner
                DisplayWinnerInfo(winner);

                // Hiển thị thông tin loser
                DisplayLoserInfo(loser);

                Log("Match result displayed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WinsController] Error displaying match result: {ex.Message}\n{ex.StackTrace}");
                SetErrorState("Có lỗi xảy ra khi hiển thị kết quả.");
            }
        }

        /// <summary>
        /// Hiển thị thông tin người thắng
        /// </summary>
        private void DisplayWinnerInfo(NetworkedPlayerState winner)
        {
            if (winner == null)
            {
                Debug.LogWarning("[WinsController] Winner state is null");
                return;
            }

            Log("DisplayWinnerInfo START");

            // Kiểm tra xem winner có phải local player không
            bool isLocalPlayer = IsLocalPlayer(winner);

            // Hiển thị tên
            if (winnerNameText != null)
            {
                string winnerName = winner.PlayerName.Value.ToString();
                if (string.IsNullOrEmpty(winnerName))
                {
                    winnerName = "Player";
                }

                // Thêm "(Bạn)" nếu là local player
                if (isLocalPlayer)
                {
                    winnerName += " (Bạn)";
                }

                winnerNameText.text = winnerName;
                Log($"Winner name: {winnerNameText.text}");
            }
            else
            {
                Debug.LogWarning("[WinsController] winnerNameText is NULL!");
            }

            // Hiển thị điểm số
            if (winnerScoreText != null)
            {
                winnerScoreText.text = $"Điểm Số: {winner.Score.Value}";
                Log($"Winner score: {winner.Score.Value}");
            }
            else
            {
                Debug.LogWarning("[WinsController] winnerScoreText is NULL!");
            }

            // Hiển thị máu còn lại
            if (winnerHealthText != null)
            {
                winnerHealthText.text = $"Số Máu Còn Lại: {winner.CurrentHealth.Value}";
                Log($"Winner health: {winner.CurrentHealth.Value}");
            }
            else
            {
                Debug.LogWarning("[WinsController] winnerHealthText is NULL!");
            }

            // Hiển thị container
            if (winContainer != null)
            {
                winContainer.SetActive(true);
                Log("Winner container activated");
            }
            else
            {
                Debug.LogWarning("[WinsController] winContainer is NULL!");
            }
        }

        /// <summary>
        /// Hiển thị thông tin người thua
        /// </summary>
        private void DisplayLoserInfo(NetworkedPlayerState loser)
        {
            if (loser == null)
            {
                Debug.LogWarning("[WinsController] Loser state is null");
                return;
            }

            Log("DisplayLoserInfo START");

            // Kiểm tra xem loser có phải local player không
            bool isLocalPlayer = IsLocalPlayer(loser);

            // Hiển thị tên
            if (loserNameText != null)
            {
                string loserName = loser.PlayerName.Value.ToString();
                if (string.IsNullOrEmpty(loserName))
                {
                    loserName = "Player";
                }

                // Thêm "(Bạn)" nếu là local player
                if (isLocalPlayer)
                {
                    loserName += " (Bạn)";
                }

                loserNameText.text = loserName;
                Log($"Loser name: {loserNameText.text}");
            }
            else
            {
                Debug.LogWarning("[WinsController] loserNameText is NULL!");
            }

            // Hiển thị điểm số
            if (loserScoreText != null)
            {
                loserScoreText.text = $"Điểm Số: {loser.Score.Value}";
                Log($"Loser score: {loser.Score.Value}");
            }
            else
            {
                Debug.LogWarning("[WinsController] loserScoreText is NULL!");
            }

            // Hiển thị máu còn lại
            if (loserHealthText != null)
            {
                loserHealthText.text = $"Số Máu Còn Lại: {loser.CurrentHealth.Value}";
                Log($"Loser health: {loser.CurrentHealth.Value}");
            }
            else
            {
                Debug.LogWarning("[WinsController] loserHealthText is NULL!");
            }

            // Hiển thị container
            if (lostContainer != null)
            {
                lostContainer.SetActive(true);
                Log("Loser container activated");
            }
            else
            {
                Debug.LogWarning("[WinsController] lostContainer is NULL!");
            }
        }

        /// <summary>
        /// Kiểm tra xem player có phải local player không
        /// </summary>
        private bool IsLocalPlayer(NetworkedPlayerState state)
        {
            if (state == null)
                return false;

            var nm = NetworkManager.Singleton;
            if (nm == null)
                return false;

            // Host = Player 0, Client = Player 1
            bool isHost = nm.IsHost;
            int playerId = state.PlayerId.Value;

            bool result = (isHost && playerId == 0) || (!isHost && playerId == 1);
            Log($"IsLocalPlayer check: isHost={isHost}, playerId={playerId}, result={result}");
            
            return result;
        }

        /// <summary>
        /// Hiển thị trạng thái lỗi
        /// </summary>
        private void SetErrorState(string errorMessage)
        {
            if (trangThaiText != null)
            {
                trangThaiText.text = errorMessage;
                trangThaiText.color = Color.red;
            }

            // Ẩn các container
            if (winContainer != null)
                winContainer.SetActive(false);
            
            if (lostContainer != null)
                lostContainer.SetActive(false);
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
