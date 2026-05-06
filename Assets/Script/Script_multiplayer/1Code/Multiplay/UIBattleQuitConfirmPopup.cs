using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Multiplayer;

namespace DoAnGame.UI
{
    /// <summary>
    /// Popup xác nhận Quit trong khi đang chiến đấu.
    ///
    /// FLOW:
    /// 1. Người chơi bấm nút Quit trong GameplayPanel → popup hiện lên
    ///    (thời gian trận vẫn chạy bình thường)
    /// 2. Bấm "Huỷ" → đóng popup, tiếp tục chơi
    /// 3. Bấm "Quit" (xác nhận):
    ///    a. Gửi ServerRpc để server kết thúc trận, tính đối thủ thắng
    ///    b. Người này → RequestQuitRoom() → về LobbyPanel, reset sạch
    ///    c. Người còn lại → nhận OnMatchEnded bình thường → WinsPanel → Tiếp tục → LobbyPanel
    /// </summary>
    public class UIBattleQuitConfirmPopup : BasePanelController
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmQuitButton;
        [SerializeField] private Button cancelButton;

        [Header("References")]
        [SerializeField] private UIMultiplayerRoomController roomController;
        [SerializeField] private bool enableDebugLogs = true;

        protected override void Awake()
        {
            base.Awake();
            confirmQuitButton?.onClick.AddListener(HandleConfirmQuit);
            cancelButton?.onClick.AddListener(HandleCancel);
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (titleText != null)
                titleText.SetText("Rời Trận?");
            if (messageText != null)
                messageText.SetText("Bạn có chắc muốn rời trận không?\nĐối thủ sẽ được tính là chiến thắng.\nThời gian trận vẫn tiếp tục chạy.");
            Log("Popup shown");
        }

        protected override void OnHide()
        {
            base.OnHide();
            // ✅ Cancel QuitAfterDelay coroutine nếu đang chạy
            StopAllCoroutines();
            Log("Popup hidden");
        }

        private void OnDestroy()
        {
            confirmQuitButton?.onClick.RemoveAllListeners();
            cancelButton?.onClick.RemoveAllListeners();
        }

        // ─── Nút "Huỷ" ───────────────────────────────────────────────────────────
        private void HandleCancel()
        {
            Log("Cancelled — continuing battle");
            
            // ✅ LOG: Track cancel action
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            GameLogger.Log($"[QuitPopup] [{role}] User CANCELLED quit - continuing battle");
            
            Hide();
        }

        // ─── Nút "Quit" (xác nhận) ───────────────────────────────────────────────
        private void HandleConfirmQuit()
        {
            Log("Confirmed quit");
            
            // ✅ LOG: Track quit confirmation
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            ulong clientId = net != null ? net.LocalClientId : 999;
            GameLogger.Log($"[QuitPopup] [{role}] [ClientID:{clientId}] User CONFIRMED quit - starting forfeit flow");
            
            // ❌ KHÔNG Hide() ở đây - sẽ làm GameObject inactive và coroutine không chạy được
            // Hide() sẽ được gọi trong ExecuteQuitToLobby()

            // Bước 1: Báo server kết thúc trận, tính đối thủ thắng
            // Server sẽ gửi OnMatchEnded cho người còn lại → họ vào WinsPanel bình thường
            var battleManager = NetworkedMathBattleManager.Instance;
            if (battleManager != null)
            {
                GameLogger.Log($"[QuitPopup] [{role}] Sending RequestForfeitServerRpc to server...");
                battleManager.RequestForfeitServerRpc();
                Log("Sent RequestForfeitServerRpc");
                GameLogger.Log($"[QuitPopup] [{role}] ✅ RequestForfeitServerRpc sent successfully");
            }
            else
            {
                Debug.LogWarning("[QuitPopup] BattleManager not found, skipping forfeit signal.");
                GameLogger.Log($"[QuitPopup] [{role}] ⚠️ BattleManager NOT FOUND - cannot send forfeit signal!");
            }

            // Bước 2: Delay 1 giây để server xử lý và gửi thông báo cho người còn lại
            // Sau đó người này về LobbyPanel
            GameLogger.Log($"[QuitPopup] [{role}] Starting 1s delay before ExecuteQuitToLobby...");
            StartCoroutine(QuitAfterDelay(1f));
        }

        /// <summary>
        /// Delay trước khi quit để server có thời gian gửi thông báo cho người còn lại
        /// </summary>
        private IEnumerator QuitAfterDelay(float delay)
        {
            Log($"Waiting {delay}s before quitting...");
            
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            GameLogger.Log($"[QuitPopup] [{role}] Waiting {delay}s for server to process forfeit...");
            
            yield return new WaitForSeconds(delay);
            
            GameLogger.Log($"[QuitPopup] [{role}] Delay complete - calling ExecuteQuitToLobby");
            ExecuteQuitToLobby();
        }

        private void ExecuteQuitToLobby()
        {
            Log("ExecuteQuitToLobby");
            
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            GameLogger.Log($"[QuitPopup] [{role}] ExecuteQuitToLobby START");

            // ✅ FIX: Cancel pending NavigateToWinsPanel invoke để tránh client bị navigate lại
            var gameplayPanel = FindObjectOfType<UIMultiplayerBattleController>(true);
            if (gameplayPanel != null)
            {
                GameLogger.Log($"[QuitPopup] [{role}] Cancelling pending NavigateToWinsPanel invoke...");
                gameplayPanel.CancelInvoke("NavigateToWinsPanel");
                GameLogger.Log($"[QuitPopup] [{role}] ✅ Cancelled NavigateToWinsPanel");
                
                gameplayPanel.Hide();
                Log("Hidden GameplayPanel");
                GameLogger.Log($"[QuitPopup] [{role}] ✅ Hidden GameplayPanel");
            }
            else
            {
                GameLogger.Log($"[QuitPopup] [{role}] ⚠️ GameplayPanel NOT FOUND");
            }

            // Ẩn popup
            Hide();
            GameLogger.Log($"[QuitPopup] [{role}] ✅ Hidden QuitPopup");

            // Resolve roomController nếu chưa gán
            if (roomController == null)
            {
                GameLogger.Log($"[QuitPopup] [{role}] roomController is null, searching...");
                roomController = FindObjectOfType<UIMultiplayerRoomController>(true);
                Log($"Resolved roomController: {roomController != null}");
                GameLogger.Log($"[QuitPopup] [{role}] roomController found: {roomController != null}");
            }

            if (roomController != null)
            {
                // ✅ FIX: KHÔNG gọi Show() trước RequestQuitRoom()
                // RequestQuitRoom() → ResetRoomSessionState() → HideAllBattlePanels() → Show()
                // Gọi Show() trước sẽ khiến OnShow() → UpdateReadyButtonState() đọc currentLobby cũ
                // → hiển thị button "Sẵn sàng" trong khi đang reset (race condition với async)
                GameLogger.Log($"[QuitPopup] [{role}] Calling RequestQuitRoom (will Show LobbyPanel internally)...");
                roomController.RequestQuitRoom();
                Log("Called RequestQuitRoom");
                GameLogger.Log($"[QuitPopup] [{role}] ✅ RequestQuitRoom called - ExecuteQuitToLobby COMPLETE");
            }
            else
            {
                Debug.LogError("[QuitPopup] roomController not found! Cannot navigate back to lobby.");
                GameLogger.Log($"[QuitPopup] [{role}] ❌ ERROR: roomController NOT FOUND - cannot navigate to lobby!");
            }
        }

        private void Log(string msg)
        {
            if (enableDebugLogs)
                Debug.Log($"[QuitPopup] {msg}");
        }
    }
}
