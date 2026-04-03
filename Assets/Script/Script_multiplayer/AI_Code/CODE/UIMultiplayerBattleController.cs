using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

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
        }

        private void HandlePanelActivated()
        {
            hadTwoPlayersInSession = false;
            sessionEndedHandled = false;
            nextSessionCheckAt = 0f;
            DisableRaycastOnRuntimePlayerClones();
            if (autoResolveSessionEndedNavigator)
            {
                TryResolveSessionEndedNavigator();
            }
            BindRoles();
            RegisterNetCallbacks();
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
                    if (graphics[g] != null && graphics[g].raycastTarget)
                    {
                        graphics[g].raycastTarget = false;
                        blockedTargetsDisabled++;
                    }
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
