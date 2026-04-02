using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 16: Multiplayer Battle - gán đúng vai trò local player và đối thủ theo Netcode.
    /// </summary>
    public class UIMultiplayerBattleController : BasePanelController
    {
        [Header("Role Texts")]
        [SerializeField] private TMP_Text topPlayerText;
        [SerializeField] private TMP_Text bottomPlayerText;
        [SerializeField] private TMP_Text battleStatusText;

        [Header("Optional Texts")]
        [SerializeField] private TMP_Text roomInfoText;

        [Header("Fallback")]
        [SerializeField] private string localPlayerLabel = "Player 1 - người chơi";
        [SerializeField] private string enemyPlayerLabel = "Player 2 - đối thủ";
        [SerializeField] private string aiEnemyLabel = "Máy AI - đối thủ";

        [Header("Session End Handling")]
        [SerializeField] private UIButtonScreenNavigator onSessionEndedNavigator;
        [SerializeField] private bool autoNavigateOnSessionEnded = true;
        [SerializeField] private bool enableDebugLogs;

        private bool hadTwoPlayersInSession;
        private bool sessionEndedHandled;

        protected override void OnShow()
        {
            base.OnShow();
            sessionEndedHandled = false;
            BindRoles();
            RegisterNetCallbacks();
        }

        protected override void OnHide()
        {
            base.OnHide();
            UnregisterNetCallbacks();
        }

        private void OnDestroy()
        {
            UnregisterNetCallbacks();
        }

        private void BindRoles()
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
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            var net = NetworkManager.Singleton;
            int count = net != null ? net.ConnectedClientsIds.Count : 0;

            BindRoles();
            Log($"Client disconnected: {clientId} | count={count}");

            if (!autoNavigateOnSessionEnded || sessionEndedHandled)
                return;

            if (hadTwoPlayersInSession && count < 2)
            {
                sessionEndedHandled = true;
                battleStatusText?.SetText("Đối thủ đã thoát phòng.");

                if (onSessionEndedNavigator != null)
                {
                    onSessionEndedNavigator.NavigateNow();
                }
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

        private void Log(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[{nameof(UIMultiplayerBattleController)}:{name}] {message}");
        }
    }
}
