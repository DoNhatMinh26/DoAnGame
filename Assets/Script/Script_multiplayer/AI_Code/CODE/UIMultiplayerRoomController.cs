using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 15: Multiplayer Room - tạo phòng, quick join, nhập code join và start khi đủ 2 người.
    /// </summary>
    public class UIMultiplayerRoomController : BasePanelController
    {
        private const string JoinCodeKey = "JoinCode";
        private const string StartedKey = "Started";
        private const string ModeKey = "Mode";
        private const int MaxPlayers = 2;
        private const string DefaultIdleStatus = "Mời tạo phòng để chơi.";
        private const string DefaultIdleCode = "Mã phòng: -----";
        private const string DefaultIdlePlayerCount = "Người chơi: 1/2";
        private const float DefaultPollIntervalSeconds = 3.5f;
        private const float DefaultRateLimitBackoffSeconds = 2.5f;

        [Header("Room Buttons")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button quickJoinButton;
        [SerializeField] private Button joinByCodeButton;
        [SerializeField] private Button quitRoomButton;
        [SerializeField] private Button startMatchButton;

        [Header("Room Inputs")]
        [SerializeField] private TMP_InputField roomCodeInput;

        [Header("Room Texts")]
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text playerCountText;

        [Header("Callbacks")]
        [SerializeField] private UIButtonScreenNavigator startBattleNavigator;
        [SerializeField] private UIButtonScreenNavigator quitRoomNavigator;
        [SerializeField] private UnityEvent onBattleStarted;
        [SerializeField] private UnityEvent onQuitRoom;

        [Header("Battle Fallback UI")]
        [SerializeField] private Transform screensRootForBattleFallback;
        [SerializeField] private GameObject battleScreenFallback;
        [SerializeField] private bool enableDetailedLogs = true;

        [Header("Lobby Read Tuning")]
        [SerializeField] private float pollIntervalSeconds = DefaultPollIntervalSeconds;
        [SerializeField] private float rateLimitBackoffSeconds = DefaultRateLimitBackoffSeconds;

        private Lobby currentLobby;
        private bool isHost;
        private Coroutine pollingRoutine;
        private Coroutine heartbeatRoutine;
        private bool battleStartNotified;
        private bool initialized;
        private bool isBusy;
        private bool isQuitting;
        private float nextLobbyReadAt;
        private Coroutine delayedBattleFallbackRoutine;

        protected override void Awake()
        {
            base.Awake();

            if (statusText != null && playerCountText != null && statusText == playerCountText)
            {
                Debug.LogWarning("[UIRoom] Status Text và Player Count Text đang trỏ cùng 1 TMP_Text. Nên tách ra để tránh ghi đè nội dung.");
            }

            createRoomButton?.onClick.AddListener(() => _ = HandleCreateRoom());
            quickJoinButton?.onClick.AddListener(() => _ = HandleQuickJoin());
            joinByCodeButton?.onClick.AddListener(() => _ = HandleJoinByCode());
            quitRoomButton?.onClick.AddListener(() => _ = HandleQuitRoom());
            startMatchButton?.onClick.AddListener(() => _ = HandleStartMatch());
        }

        private void Start()
        {
            // Khi không dùng Flow.Show(), vẫn cần init state + polling nếu panel đang active.
            EnsureInitialized();
            if (gameObject.activeInHierarchy && pollingRoutine == null)
            {
                pollingRoutine = StartCoroutine(PollLobbyRoutine());
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            EnsureInitialized();
            battleStartNotified = false;

            StopRoutines();
            pollingRoutine = StartCoroutine(PollLobbyRoutine());
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            battleStartNotified = false;
            ApplyIdleVisualState(DefaultIdleStatus);
            TryAutoResolveBattleFallback();
        }

        protected override void OnHide()
        {
            base.OnHide();
            StopRoutines();
        }

        private void OnDestroy()
        {
            StopRoutines();
            createRoomButton?.onClick.RemoveAllListeners();
            quickJoinButton?.onClick.RemoveAllListeners();
            joinByCodeButton?.onClick.RemoveAllListeners();
            quitRoomButton?.onClick.RemoveAllListeners();
            startMatchButton?.onClick.RemoveAllListeners();
            _ = LeaveLobbySafe();
        }

        private async Task HandleCreateRoom()
        {
            if (isBusy)
                return;

            isBusy = true;
            SetActionButtonsInteractable(false);

            if (!await EnsureRelayManager()) return;

            SetStatus("Đang tạo phòng...");
            string relayJoinCode = await RelayManager.Instance.CreateRelay(MaxPlayers);
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                SetStatus("Tạo phòng thất bại.");
                isBusy = false;
                SetActionButtonsInteractable(true);
                return;
            }

            try
            {
                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode, DataObject.IndexOptions.S1) },
                        { StartedKey, new DataObject(DataObject.VisibilityOptions.Public, "0", DataObject.IndexOptions.S2) },
                        { ModeKey, new DataObject(DataObject.VisibilityOptions.Public, "MathDuel") }
                    }
                };

                currentLobby = await LobbyService.Instance.CreateLobbyAsync("Math Room", MaxPlayers, options);
                isHost = true;

                lobbyCodeText?.SetText($"Mã phòng: {currentLobby.LobbyCode}");
                SetStatus("Đã tạo phòng. Chờ người chơi thứ 2...");
                SetPlayerCount($"Người chơi: {currentLobby.Players.Count}/{MaxPlayers}");
                startMatchButton?.gameObject.SetActive(true);
                startMatchButton.interactable = currentLobby.Players.Count >= MaxPlayers;

                heartbeatRoutine = StartCoroutine(HeartbeatRoutine());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIRoom] CreateLobby lỗi: {ex.Message}");
                SetStatus("Không thể tạo lobby cloud.");
            }
            finally
            {
                isBusy = false;
                SetActionButtonsInteractable(true);
            }
        }

        private async Task HandleQuickJoin()
        {
            if (isBusy)
                return;

            isBusy = true;
            SetActionButtonsInteractable(false);

            if (!await EnsureRelayManager()) return;

            SetStatus("Đang tìm phòng nhanh...");

            try
            {
                var lobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions
                {
                    Filter = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.S2, "0", QueryFilter.OpOptions.EQ)
                    }
                });

                await JoinLobbyAndRelay(lobby);
            }
            catch (LobbyServiceException ex)
            {
                SetStatus("Không có phòng phù hợp để vào nhanh.");
                Debug.LogWarning($"[UIRoom] QuickJoin lỗi: {ex.Reason} - {ex.Message}");
            }
            finally
            {
                isBusy = false;
                SetActionButtonsInteractable(true);
            }
        }

        private async Task HandleJoinByCode()
        {
            if (isBusy)
                return;

            isBusy = true;
            SetActionButtonsInteractable(false);

            if (!await EnsureRelayManager()) return;

            string lobbyCode = roomCodeInput?.text?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(lobbyCode))
            {
                SetStatus("Vui lòng nhập mã phòng.");
                isBusy = false;
                SetActionButtonsInteractable(true);
                return;
            }

            SetStatus("Đang tham gia phòng...");

            try
            {
                var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
                await JoinLobbyAndRelay(lobby);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] JoinByCode lobby code lỗi: {ex.Message}");
                // Giữ logic ổn định cho 2 người: yêu cầu join bằng Lobby Code để sync trạng thái Started.
                SetStatus("Không vào được phòng. Hãy nhập đúng Lobby Code.");
            }
            finally
            {
                isBusy = false;
                SetActionButtonsInteractable(true);
            }
        }

        private async Task JoinLobbyAndRelay(Lobby lobby)
        {
            if (lobby == null)
            {
                SetStatus("Lobby không hợp lệ.");
                return;
            }

            if (!lobby.Data.TryGetValue(JoinCodeKey, out var joinCodeData) || string.IsNullOrEmpty(joinCodeData.Value))
            {
                SetStatus("Phòng không có relay join code.");
                return;
            }

            bool joined = await RelayManager.Instance.TryJoinRelay(joinCodeData.Value);
            if (!joined)
            {
                SetStatus("Join relay thất bại.");
                return;
            }

            currentLobby = lobby;
            isHost = false;
            lobbyCodeText?.SetText($"Mã phòng: {currentLobby.LobbyCode}");
            SetStatus("Đã vào phòng. Chờ chủ phòng bắt đầu...");
            SetPlayerCount($"Người chơi: {currentLobby.Players.Count}/{MaxPlayers}");
            startMatchButton?.gameObject.SetActive(false);
        }

        private async Task HandleStartMatch()
        {
            if (isBusy || isQuitting)
                return;

            isBusy = true;
            SetActionButtonsInteractable(false);

            if (!isHost || currentLobby == null)
            {
                SetStatus("Chỉ chủ phòng mới được bắt đầu.");
                isBusy = false;
                SetActionButtonsInteractable(true);
                return;
            }

            var lobby = currentLobby;
            if (lobby != null && lobby.Players.Count < MaxPlayers)
            {
                // Chỉ refresh khi local snapshot chưa đủ người để giảm spam API.
                lobby = await RefreshLobbySafe();
            }

            if (lobby == null)
            {
                SetStatus("Không đọc được lobby để bắt đầu.");
                isBusy = false;
                SetActionButtonsInteractable(true);
                return;
            }

            if (lobby.Players.Count < MaxPlayers)
            {
                SetStatus("Chưa đủ 2 người chơi.");
                startMatchButton.interactable = false;
                isBusy = false;
                SetActionButtonsInteractable(true);
                return;
            }

            try
            {
                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { StartedKey, new DataObject(DataObject.VisibilityOptions.Public, "1", DataObject.IndexOptions.S2) }
                    }
                });

                SetStatus("Bắt đầu trận đấu...");
                NotifyBattleStarted();
            }
            catch (Exception ex)
            {
                SetStatus("Không thể bắt đầu trận.");
                Debug.LogError($"[UIRoom] StartMatch lỗi: {ex.Message}");
            }
            finally
            {
                isBusy = false;
                SetActionButtonsInteractable(true);
            }
        }

        private async Task HandleQuitRoom()
        {
            if (isBusy)
                return;

            isBusy = true;
            isQuitting = true;
            SetActionButtonsInteractable(false);
            SetStatus("Đang rời phòng...");

            StopRoutines();
            await LeaveLobbySafe();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            ResetRoomSessionState(DefaultIdleStatus);

            onQuitRoom?.Invoke();
            if (quitRoomNavigator != null)
            {
                quitRoomNavigator.NavigateNow();
            }

            isBusy = false;
            isQuitting = false;
            SetActionButtonsInteractable(true);
        }

        private void ResetRoomSessionState(string status)
        {
            currentLobby = null;
            isHost = false;
            battleStartNotified = false;
            nextLobbyReadAt = 0f;
            ApplyIdleVisualState(status);
            roomCodeInput?.SetTextWithoutNotify(string.Empty);
        }

        private void ApplyIdleVisualState(string status)
        {
            SetStatus(status);
            SetPlayerCount(DefaultIdlePlayerCount);
            lobbyCodeText?.SetText(DefaultIdleCode);

            if (startMatchButton != null)
            {
                startMatchButton.gameObject.SetActive(false);
                startMatchButton.interactable = false;
            }
        }

        private IEnumerator PollLobbyRoutine()
        {
            while (true)
            {
                _ = PollLobbyOnce();
                yield return new WaitForSeconds(Mathf.Max(1.5f, pollIntervalSeconds));
            }
        }

        private async Task PollLobbyOnce()
        {
            if (currentLobby == null || isQuitting) return;

            if (Time.unscaledTime < nextLobbyReadAt)
                return;

            var refreshed = await RefreshLobbySafe();
            if (refreshed == null) return;

            currentLobby = refreshed;
            SetPlayerCount($"Người chơi: {currentLobby.Players.Count}/{MaxPlayers}");

            if (isHost && startMatchButton != null)
            {
                startMatchButton.interactable = currentLobby.Players.Count >= MaxPlayers;
            }

            if (currentLobby.Data.TryGetValue(StartedKey, out var startedData) && startedData.Value == "1")
            {
                SetStatus("Trận đấu bắt đầu.");
                NotifyBattleStarted();
            }
        }

        private void NotifyBattleStarted()
        {
            if (isQuitting)
                return;

            // Nếu đã notify trước đó nhưng UI16 vẫn chưa visible, cho phép retry.
            if (battleStartNotified && IsBattleScreenVisible())
                return;

            battleStartNotified = true;
            LogState("NotifyBattleStarted: begin");

            if (startBattleNavigator != null)
            {
                Log($"NotifyBattleStarted: navigator={startBattleNavigator.name}");
                startBattleNavigator.NavigateNow();
            }
            else
            {
                Log("NotifyBattleStarted: startBattleNavigator is NULL");
            }

            // Fallback chống case client bị blank (đã ẩn lobby nhưng chưa bật được panel battle).
            // Nếu object đã bị inactive bởi navigator thì không thể StartCoroutine trên object này.
            if (isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                if (delayedBattleFallbackRoutine != null)
                {
                    StopCoroutine(delayedBattleFallbackRoutine);
                }
                delayedBattleFallbackRoutine = StartCoroutine(EnsureBattleScreenVisibleNextFrame());
            }
            else
            {
                Log("NotifyBattleStarted: room panel inactive sau navigate, áp fallback ngay lập tức");
                EnsureBattleScreenVisibleImmediate();
            }

            onBattleStarted?.Invoke();
            LogState("NotifyBattleStarted: end");
        }

        private bool IsBattleScreenVisible()
        {
            return battleScreenFallback != null && battleScreenFallback.activeInHierarchy;
        }

        private IEnumerator EnsureBattleScreenVisibleNextFrame()
        {
            yield return null;

            EnsureBattleScreenVisibleImmediate();
            delayedBattleFallbackRoutine = null;
        }

        private void EnsureBattleScreenVisibleImmediate()
        {
            TryAutoResolveBattleFallback();
            LogState("EnsureBattleScreenVisibleImmediate: check");

            if (battleScreenFallback == null)
            {
                Log("EnsureBattleScreenVisibleImmediate: battleScreenFallback is NULL");
                return;
            }

            if (battleScreenFallback.activeInHierarchy)
            {
                Log("EnsureBattleScreenVisibleImmediate: battle screen already active");
                return;
            }

            if (screensRootForBattleFallback != null)
            {
                for (int i = 0; i < screensRootForBattleFallback.childCount; i++)
                {
                    var child = screensRootForBattleFallback.GetChild(i);
                    if (child != null)
                    {
                        if (child.GetComponent<EventSystem>() != null)
                        {
                            Log("Fallback skip auto-hide EventSystem");
                            continue;
                        }

                        child.gameObject.SetActive(false);
                    }
                }
            }

            battleScreenFallback.SetActive(true);
            Log("Applied battle fallback UI activation.");
            LogState("EnsureBattleScreenVisibleImmediate: done");
        }

        private void TryAutoResolveBattleFallback()
        {
            if (battleScreenFallback != null && screensRootForBattleFallback != null)
                return;

            var allBattleControllers = Resources.FindObjectsOfTypeAll<UIMultiplayerBattleController>();
            if (allBattleControllers != null && allBattleControllers.Length > 0)
            {
                for (int i = 0; i < allBattleControllers.Length; i++)
                {
                    var ctrl = allBattleControllers[i];
                    if (ctrl == null)
                        continue;

                    var go = ctrl.gameObject;
                    var scene = go.scene;
                    if (!scene.IsValid() || !scene.isLoaded)
                        continue;

                    if (battleScreenFallback == null)
                    {
                        battleScreenFallback = go;
                        Log($"Auto-resolved battleScreenFallback={go.name}");
                    }

                    if (screensRootForBattleFallback == null)
                    {
                        screensRootForBattleFallback = go.transform.parent;
                        Log($"Auto-resolved screensRootForBattleFallback={(screensRootForBattleFallback != null ? screensRootForBattleFallback.name : "null")}");
                    }

                    if (battleScreenFallback != null && screensRootForBattleFallback != null)
                        return;
                }
            }
        }

        // Public wrappers to allow other UI layers (e.g. UI16 action hub)
        // to invoke the same validated flow as internal button listeners.
        public void RequestCreateRoom()
        {
            _ = HandleCreateRoom();
        }

        public void RequestQuickJoin()
        {
            _ = HandleQuickJoin();
        }

        public void RequestJoinByCode()
        {
            _ = HandleJoinByCode();
        }

        public void RequestStartMatch()
        {
            _ = HandleStartMatch();
        }

        public void RequestQuitRoom()
        {
            _ = HandleQuitRoom();
        }

        private IEnumerator HeartbeatRoutine()
        {
            while (true)
            {
                if (isHost && currentLobby != null)
                {
                    _ = SendHeartbeatSafe(currentLobby.Id);
                }

                yield return new WaitForSeconds(15f);
            }
        }

        private async Task SendHeartbeatSafe(string lobbyId)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Heartbeat lỗi: {ex.Message}");
            }
        }

        private async Task<Lobby> RefreshLobbySafe()
        {
            if (currentLobby == null)
                return null;

            if (Time.unscaledTime < nextLobbyReadAt)
                return currentLobby;

            try
            {
                return await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            }
            catch (LobbyServiceException ex) when (IsRateLimitException(ex))
            {
                nextLobbyReadAt = Time.unscaledTime + Mathf.Max(1f, rateLimitBackoffSeconds);
                Debug.LogWarning($"[UIRoom] Refresh lobby bị rate limit, tạm dừng đọc đến t={nextLobbyReadAt:F1}. {ex.Reason} - {ex.Message}");
                return currentLobby;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Refresh lobby lỗi: {ex.Message}");
                return null;
            }
        }

        private bool IsRateLimitException(LobbyServiceException ex)
        {
            if (ex == null)
                return false;

            string msg = ex.Message ?? string.Empty;
            return msg.IndexOf("Too Many Requests", StringComparison.OrdinalIgnoreCase) >= 0
                   || ex.Reason.ToString().IndexOf("Rate", StringComparison.OrdinalIgnoreCase) >= 0
                   || ex.Reason.ToString().IndexOf("TooMany", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task<bool> EnsureRelayManager()
        {
            if (RelayManager.Instance == null)
            {
                SetStatus("Thiếu RelayManager trong scene.");
                Debug.LogError("[UIRoom] RelayManager.Instance == null");
                isBusy = false;
                SetActionButtonsInteractable(true);
                return false;
            }

            if (!await RelayManager.Instance.EnsureServicesReady())
            {
                SetStatus("Không kết nối được dịch vụ multiplayer.");
                isBusy = false;
                SetActionButtonsInteractable(true);
                return false;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                SetStatus("Chưa đăng nhập dịch vụ multiplayer.");
                isBusy = false;
                SetActionButtonsInteractable(true);
                return false;
            }

            return true;
        }

        private async Task LeaveLobbySafe()
        {
            if (currentLobby == null)
                return;

            try
            {
                if (isHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else if (AuthenticationService.Instance.IsSignedIn)
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] LeaveLobby lỗi: {ex.Message}");
            }
            finally
            {
                currentLobby = null;
                isHost = false;
            }
        }

        private void StopRoutines()
        {
            if (pollingRoutine != null)
            {
                StopCoroutine(pollingRoutine);
                pollingRoutine = null;
            }

            if (heartbeatRoutine != null)
            {
                StopCoroutine(heartbeatRoutine);
                heartbeatRoutine = null;
            }

            if (delayedBattleFallbackRoutine != null)
            {
                StopCoroutine(delayedBattleFallbackRoutine);
                delayedBattleFallbackRoutine = null;
            }
        }

        private void SetStatus(string text)
        {
            statusText?.SetText(text);
            Debug.Log($"[UIRoom] {text}");
        }

        private void Log(string message)
        {
            if (!enableDetailedLogs)
                return;

            Debug.Log($"[UIRoom:{name}] {message}");
        }

        private void LogState(string tag)
        {
            if (!enableDetailedLogs)
                return;

            string navigatorName = startBattleNavigator != null ? startBattleNavigator.name : "null";
            string fallbackName = battleScreenFallback != null ? battleScreenFallback.name : "null";
            bool fallbackActive = battleScreenFallback != null && battleScreenFallback.activeInHierarchy;
            string rootName = screensRootForBattleFallback != null ? screensRootForBattleFallback.name : "null";
            Debug.Log($"[UIRoom:{name}] {tag} | roomActive={gameObject.activeInHierarchy} | enabled={isActiveAndEnabled} | navigator={navigatorName} | fallback={fallbackName} active={fallbackActive} | root={rootName}");
        }

        private void SetPlayerCount(string text)
        {
            playerCountText?.SetText(text);
        }

        private void SetActionButtonsInteractable(bool interactable)
        {
            if (createRoomButton != null) createRoomButton.interactable = interactable;
            if (quickJoinButton != null) quickJoinButton.interactable = interactable;
            if (joinByCodeButton != null) joinByCodeButton.interactable = interactable;
            if (quitRoomButton != null) quitRoomButton.interactable = interactable;

            if (startMatchButton != null)
            {
                // Start chỉ có ý nghĩa với host, trạng thái đủ người sẽ được cập nhật trong poll/create.
                startMatchButton.interactable = interactable && isHost;
            }
        }
    }
}
