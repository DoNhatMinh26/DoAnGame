using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private const string IsAbandonedKey = "IsAbandoned";
        private const string CharacterNameKey = "characterName";
        private const string HostNameKey = "HostName";
        private const int MaxPlayers = 2;
        private const int QuickJoinQueryMaxResults = 20;
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

        [Header("Room Roster Texts")]
        [SerializeField] private TMP_Text rosterTitleText;
        [SerializeField] private TMP_Text rosterListText;

        [Header("Callbacks")]
        [SerializeField] private UIFlowManager flowManager;
        [SerializeField] private UIButtonScreenNavigator startBattleNavigator;
        [SerializeField] private UIButtonScreenNavigator quitRoomNavigator;
        [SerializeField] private UnityEvent onBattleStarted;
        [SerializeField] private UnityEvent onQuitRoom;

        [Header("Battle Fallback UI")]
        [SerializeField] private Transform screensRootForBattleFallback;
        [SerializeField] private GameObject battleScreenFallback;
        [SerializeField] private string battleSceneName = "GameUIPlay";
        [SerializeField] private LoadSceneMode battleSceneLoadMode = LoadSceneMode.Single;
        [SerializeField] private bool enableDetailedLogs = true;

        [Header("Lobby Read Tuning")]
        [SerializeField] private float pollIntervalSeconds = 1.5f;  // Faster polling (was 3.5s) to detect StartedKey quicker on client
        [SerializeField] private float rateLimitBackoffSeconds = DefaultRateLimitBackoffSeconds;

        private Lobby currentLobby;
        private bool isHost;
        private Coroutine pollingRoutine;
        private Coroutine heartbeatRoutine;
        private bool battleStartNotified;
        private bool initialized;
        private bool isBusy;
        private bool isQuitting;
        private bool suppressAutoBattleStart;
        private float nextLobbyReadAt;
        private Coroutine delayedBattleFallbackRoutine;
        private AuthManager authManager;
        private string roomStatusMessage = DefaultIdleStatus;
        private string authStatusMessage = "Chưa đăng nhập dịch vụ multiplayer";
        private const string StartMatchMessageName = "ui_room_start_match";
        private bool startMatchMessageHandlerRegistered;

        protected override void Awake()
        {
            base.Awake();
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", "Awake");

            authManager = AuthManager.Instance;
            if (flowManager == null)
            {
                flowManager = GetComponentInParent<UIFlowManager>();
            }
            if (flowManager == null)
            {
                flowManager = FindObjectOfType<UIFlowManager>(true);
            }
            ResolveTextReferences();

            if (rosterTitleText != null && rosterListText != null && rosterTitleText == rosterListText)
            {
                Debug.LogWarning("[UIRoom] Roster title và roster list đang trỏ cùng 1 TMP_Text. Nên tách ra để tránh ghi đè nội dung.");
            }

            createRoomButton?.onClick.AddListener(() => _ = HandleCreateRoom());
            quickJoinButton?.onClick.AddListener(() => _ = HandleQuickJoin());
            joinByCodeButton?.onClick.AddListener(() => _ = HandleJoinByCode());
            quitRoomButton?.onClick.AddListener(() => _ = HandleQuitRoom());
            startMatchButton?.onClick.AddListener(() => _ = HandleStartMatch());
            EnsureStartMatchMessageHandlerRegistered();
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
            ResolveTextReferences();
            RefreshAuthState();
            RefreshRoomRoster();
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

        private void OnEnable()
        {
            if (authManager == null)
            {
                authManager = AuthManager.Instance;
            }

            if (authManager != null)
            {
                authManager.OnCurrentUserChanged += HandleAuthUserChanged;
            }

            // Trong một số flow dùng UIButtonScreenNavigator, OnShow có thể không được gọi.
            // Chủ động đảm bảo polling/heartbeat khi panel được kích hoạt lại.
            EnsureLobbyRuntimeRoutines();
            EnsureStartMatchMessageHandlerRegistered();
        }

        private void OnDisable()
        {
            if (authManager != null)
            {
                authManager.OnCurrentUserChanged -= HandleAuthUserChanged;
            }
        }

        private void HandleAuthUserChanged(Firebase.Auth.FirebaseUser user)
        {
            RefreshAuthState();
            if (currentLobby != null)
            {
                _ = SyncLocalPlayerLobbyDataAsync();
            }
        }

        private void RefreshAuthState()
        {
            if (authManager == null)
            {
                authManager = AuthManager.Instance;
            }

            var user = authManager != null ? authManager.GetCurrentUser() : null;
            if (user == null)
            {
                if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
                {
                    string lobbyName = TryResolveCurrentLobbyLocalPlayerName();
                    if (!string.IsNullOrWhiteSpace(lobbyName))
                    {
                        authStatusMessage = $"Đã kết nối multiplayer: {lobbyName}";
                    }
                    else
                    {
                        authStatusMessage = "Đã kết nối dịch vụ multiplayer";
                    }
                }
                else
                {
                    authStatusMessage = "Chưa đăng nhập dịch vụ multiplayer";
                }
            }
            else
            {
                string displayName = authManager != null ? authManager.GetCharacterName() : null;
                if (string.IsNullOrWhiteSpace(displayName) || displayName == "Unknown")
                {
                    displayName = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.Email;
                }

                authStatusMessage = $"Đã đăng nhập: {displayName}";
            }

            RefreshStatusLabel();
            RefreshRoomRoster();
        }

        private string TryResolveCurrentLobbyLocalPlayerName()
        {
            if (currentLobby == null || currentLobby.Players == null || currentLobby.Players.Count == 0)
                return null;

            if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
                return null;

            string localPlayerId = AuthenticationService.Instance.PlayerId;
            if (string.IsNullOrWhiteSpace(localPlayerId))
                return null;

            for (int i = 0; i < currentLobby.Players.Count; i++)
            {
                var player = currentLobby.Players[i];
                if (player == null || string.IsNullOrWhiteSpace(player.Id))
                    continue;

                if (!string.Equals(player.Id, localPlayerId, StringComparison.OrdinalIgnoreCase))
                    continue;

                return ResolveLobbyPlayerName(player, i);
            }

            return null;
        }

        private void RefreshStatusLabel()
        {
            if (statusText == null)
                return;

            if (string.IsNullOrWhiteSpace(roomStatusMessage))
            {
                roomStatusMessage = DefaultIdleStatus;
            }

            statusText.SetText($"{authStatusMessage}\n{roomStatusMessage}");
        }

        private void RefreshRoomRoster()
        {
            string titleText = currentLobby == null
                ? "Người chơi trong room"
                : $"Người chơi trong room ({GetCurrentLobbyPlayerCount()}/{MaxPlayers})";

            string playerCountDisplayText = currentLobby == null
                ? "Người chơi: 0/2"
                : $"Người chơi: {GetCurrentLobbyPlayerCount()}/{MaxPlayers}";

            if (rosterTitleText != null)
            {
                rosterTitleText.SetText(titleText);
            }
            else if (playerCountText != null)
            {
                // Fallback: nếu không có rosterTitleText, dùng playerCountText
                playerCountText.SetText(titleText);
            }

            // Nếu có playerCountText riêng biệt, update nó với format "Người chơi: X/2"
            if (playerCountText != null && rosterTitleText != null)
            {
                playerCountText.SetText(playerCountDisplayText);
            }

            if (rosterListText != null)
            {
                rosterListText.SetText(BuildRosterText());
            }
            else if (statusText != null)
            {
                statusText.SetText(BuildRosterText());
            }
        }

        private int GetCurrentLobbyPlayerCount()
        {
            return currentLobby?.Players != null ? currentLobby.Players.Count : 0;
        }

        private string BuildRosterText()
        {
            if (currentLobby == null || currentLobby.Players == null || currentLobby.Players.Count == 0)
            {
                return "Chưa có ai trong room.";
            }

            var lines = new List<string>(currentLobby.Players.Count);
            for (int i = 0; i < currentLobby.Players.Count; i++)
            {
                var lobbyPlayer = currentLobby.Players[i];
                string displayName = ResolveLobbyPlayerName(lobbyPlayer, i);
                lines.Add($"{i + 1}. {displayName}");
            }

            return string.Join("\n", lines);
        }

        private string ResolveLobbyPlayerName(Player lobbyPlayer, int index)
        {
            if (lobbyPlayer == null)
            {
                return $"Người chơi {index + 1}";
            }

            string displayName = null;

            if (lobbyPlayer.Profile != null)
            {
                displayName = lobbyPlayer.Profile.Name;
            }

            if (string.IsNullOrWhiteSpace(displayName) && lobbyPlayer.Data != null && lobbyPlayer.Data.TryGetValue(CharacterNameKey, out var nameData))
            {
                displayName = nameData != null ? nameData.Value : null;
            }

            if (string.IsNullOrWhiteSpace(displayName) && authManager != null)
            {
                var currentUser = authManager.GetCurrentUser();
                string localPlayerId = AuthenticationService.Instance != null ? AuthenticationService.Instance.PlayerId : null;
                if (currentUser != null && !string.IsNullOrWhiteSpace(localPlayerId) && string.Equals(lobbyPlayer.Id, localPlayerId, StringComparison.OrdinalIgnoreCase))
                {
                    displayName = authManager.GetCharacterName();
                    if (string.IsNullOrWhiteSpace(displayName) || displayName == "Unknown")
                    {
                        displayName = !string.IsNullOrWhiteSpace(currentUser.DisplayName) ? currentUser.DisplayName : currentUser.Email;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = $"Người chơi {index + 1}";
            }

            if (currentLobby != null && !string.IsNullOrWhiteSpace(currentLobby.HostId) && !string.IsNullOrWhiteSpace(lobbyPlayer.Id) && string.Equals(lobbyPlayer.Id, currentLobby.HostId, StringComparison.OrdinalIgnoreCase))
            {
                if (!displayName.EndsWith("(chủ phòng)", StringComparison.OrdinalIgnoreCase))
                {
                    displayName = $"{displayName} (chủ phòng)";
                }
            }

            return displayName;
        }

        private string GetCurrentPlayerDisplayName()
        {
            if (authManager != null)
            {
                string characterName = authManager.GetCharacterName();
                if (!string.IsNullOrWhiteSpace(characterName) && characterName != "Unknown")
                {
                    return characterName;
                }

                var currentUser = authManager.GetCurrentUser();
                if (currentUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(currentUser.DisplayName))
                    {
                        return currentUser.DisplayName;
                    }

                    if (!string.IsNullOrWhiteSpace(currentUser.Email))
                    {
                        return currentUser.Email;
                    }
                }
            }

            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            {
                return AuthenticationService.Instance.PlayerId;
            }

            return "Player";
        }

        private async Task SyncLocalPlayerLobbyDataAsync()
        {
            if (currentLobby == null)
                return;

            if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
                return;

            string playerName = GetCurrentPlayerDisplayName();
            if (string.IsNullOrWhiteSpace(playerName))
                return;

            try
            {
                var playerData = new Dictionary<string, PlayerDataObject>
                {
                    { CharacterNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
                };

                currentLobby = await LobbyService.Instance.UpdatePlayerAsync(
                    currentLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    new UpdatePlayerOptions
                    {
                        Data = playerData
                    });

                if (isHost)
                {
                    currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
                    {
                        Data = new Dictionary<string, DataObject>
                        {
                            { HostNameKey, new DataObject(DataObject.VisibilityOptions.Public, playerName) }
                        }
                    });
                }

                RefreshRoomRoster();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Không đồng bộ được tên người chơi vào lobby: {ex.Message}");
            }
        }

        private void ResolveTextReferences()
        {
            if (statusText != null && playerCountText != null && rosterTitleText != null && rosterListText != null && statusText != playerCountText && rosterTitleText != rosterListText)
                return;

            var texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts == null || texts.Length == 0)
                return;

            TMP_Text rosterTitleCandidate = null;
            TMP_Text rosterListCandidate = null;
            TMP_Text statusCandidate = null;
            TMP_Text countCandidate = null;

            for (int i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                    continue;

                if (rosterTitleCandidate == null && text.gameObject.name == "DanhSachplayer")
                {
                    rosterTitleCandidate = text;
                    continue;
                }

                if (rosterListCandidate == null && text.gameObject.name == "DanhSachTenHienThi")
                {
                    rosterListCandidate = text;
                    continue;
                }

                if (statusCandidate == null && text.gameObject.name == "StatusText")
                {
                    statusCandidate = text;
                    continue;
                }

                if (countCandidate == null && text.gameObject.name == "StatusText (1)")
                {
                    countCandidate = text;
                }
            }

            if (rosterTitleText == null)
            {
                rosterTitleText = rosterTitleCandidate != null ? rosterTitleCandidate : statusCandidate;
            }

            if (rosterListText == null)
            {
                rosterListText = rosterListCandidate != null ? rosterListCandidate : countCandidate;
            }

            if (statusText == null)
            {
                statusText = statusCandidate != null ? statusCandidate : (rosterTitleText != null ? rosterTitleText : texts[0]);
            }

            if (playerCountText == null || playerCountText == statusText)
            {
                playerCountText = countCandidate != null && countCandidate != statusText
                    ? countCandidate
                    : FindFirstDifferentText(texts, statusText, rosterTitleText);
            }
        }

        private TMP_Text FindFirstDifferentText(TMP_Text[] texts, TMP_Text excluded, TMP_Text secondaryExcluded = null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i] != excluded && texts[i] != secondaryExcluded)
                    return texts[i];
            }

            return excluded;
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
            UnregisterStartMatchMessageHandler();
            _ = LeaveLobbySafe();
        }

        private async Task HandleCreateRoom()
        {
            MultiplayerDetailedLogger.TraceUserAction("UIRoom", "HandleCreateRoom", "createRoomButton");
            if (isBusy)
                return;

            suppressAutoBattleStart = false;
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
                        { ModeKey, new DataObject(DataObject.VisibilityOptions.Public, "MathDuel") },
                        { HostNameKey, new DataObject(DataObject.VisibilityOptions.Public, GetCurrentPlayerDisplayName()) }
                    },
                    Player = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
                    {
                        { CharacterNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, GetCurrentPlayerDisplayName()) }
                    })
                };

                currentLobby = await LobbyService.Instance.CreateLobbyAsync("Math Room", MaxPlayers, options);
                isHost = true;
                await SyncLocalPlayerLobbyDataAsync();

                lobbyCodeText?.SetText($"Mã phòng: {currentLobby.LobbyCode}");
                SetStatus("Đã tạo phòng. Chờ người chơi thứ 2...");
                RefreshRoomRoster();
                UpdateStartMatchButtonState(true);

                // Start polling/heartbeat only when this panel is active to avoid StartCoroutine on inactive object.
                EnsureLobbyRuntimeRoutines();
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
            MultiplayerDetailedLogger.TraceUserAction("UIRoom", "HandleQuickJoin", "quickJoinButton");
            if (isBusy)
                return;

            suppressAutoBattleStart = false;
            isBusy = true;
            SetActionButtonsInteractable(false);

            if (!await EnsureRelayManager()) return;

            SetStatus("Đang tìm phòng nhanh...");

            try
            {
                // Prefer newest, healthy lobbies and skip stale/abandoned entries.
                var response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                {
                    Count = Mathf.Clamp(QuickJoinQueryMaxResults, 1, 25),
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                        new QueryFilter(QueryFilter.FieldOptions.S2, "0", QueryFilter.OpOptions.EQ)
                    },
                    Order = new List<QueryOrder>
                    {
                        new QueryOrder(false, QueryOrder.FieldOptions.Created)
                    }
                });

                Lobby joinedLobby = null;
                var candidates = response != null ? response.Results : null;
                if (candidates != null)
                {
                    for (int i = 0; i < candidates.Count; i++)
                    {
                        var candidate = candidates[i];
                        if (!IsQuickJoinCandidateUsable(candidate))
                            continue;

                        if (await JoinLobbyAndRelayAsync(candidate))
                        {
                            joinedLobby = candidate;
                            break;
                        }
                    }
                }

                if (joinedLobby == null)
                {
                    SetStatus("Không có phòng phù hợp để vào nhanh.");
                    MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "QuickJoin failed: no usable lobby candidate");
                }
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
            MultiplayerDetailedLogger.TraceUserAction("UIRoom", "HandleJoinByCode", "joinByCodeButton");
            if (isBusy)
                return;

            suppressAutoBattleStart = false;
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
                await JoinLobbyAndRelayAsync(lobby);
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

        public async Task<bool> JoinLobbyFromBrowserAsync(Lobby lobby)
        {
            return await JoinLobbyAndRelayAsync(lobby);
        }

        public async Task<bool> JoinLobbyAndRelayAsync(Lobby lobby)
        {
            MultiplayerDetailedLogger.Trace("UI_ROOM", $"JoinLobbyAndRelayAsync begin: lobbyId={(lobby != null ? lobby.Id : "null")}");
            if (lobby == null)
            {
                SetStatus("Lobby không hợp lệ.");
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "JoinLobbyAndRelayAsync aborted: lobby null");
                return false;
            }

            if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
            {
                SetStatus("Chưa đăng nhập dịch vụ multiplayer.");
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "JoinLobbyAndRelayAsync aborted: multiplayer not signed in");
                return false;
            }

            string localPlayerId = AuthenticationService.Instance.PlayerId;
            bool joinedLobbyFromApi = false;
            Lobby joinedLobby = lobby;

            bool isAlreadyMember = false;
            if (!string.IsNullOrWhiteSpace(localPlayerId) && joinedLobby.Players != null)
            {
                for (int i = 0; i < joinedLobby.Players.Count; i++)
                {
                    var p = joinedLobby.Players[i];
                    if (p != null && string.Equals(p.Id, localPlayerId, StringComparison.OrdinalIgnoreCase))
                    {
                        isAlreadyMember = true;
                        break;
                    }
                }
            }

            if (!isAlreadyMember)
            {
                try
                {
                    joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
                    joinedLobbyFromApi = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UIRoom] JoinLobbyById lỗi: {ex.Message}");
                    SetStatus("Không vào được phòng lobby.");
                    MultiplayerDetailedLogger.TraceException("UI_ROOM", ex, "JoinLobbyByIdAsync failed");
                    return false;
                }
            }

            if (joinedLobby == null || joinedLobby.Data == null || !joinedLobby.Data.TryGetValue(JoinCodeKey, out var joinCodeData) || string.IsNullOrEmpty(joinCodeData.Value))
            {
                SetStatus("Phòng không có relay join code.");
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "JoinLobbyAndRelayAsync aborted: missing relay join code");
                return false;
            }

            if (currentLobby != null && !string.Equals(currentLobby.Id, joinedLobby.Id, StringComparison.OrdinalIgnoreCase))
            {
                await LeaveLobbySafe();
                // Disconnect Relay cũ trước join Relay mới để tránh race condition
                RelayManager.Instance.Disconnect();
                await Task.Delay(500); // Đợi Relay cũ disconnect hoàn toàn
            }

            bool joined = await RelayManager.Instance.TryJoinRelay(joinCodeData.Value);
            if (!joined)
            {
                if (joinedLobbyFromApi)
                {
                    try
                    {
                        await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, localPlayerId);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UIRoom] Rollback RemovePlayer lỗi sau khi join relay thất bại: {ex.Message}");
                    }
                }

                SetStatus("Join relay thất bại.");
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", $"JoinLobbyAndRelayAsync aborted: TryJoinRelay failed, joinCode={joinCodeData.Value}");
                return false;
            }

            currentLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            isHost = !string.IsNullOrWhiteSpace(currentLobby.HostId) && string.Equals(currentLobby.HostId, localPlayerId, StringComparison.OrdinalIgnoreCase);
            lobbyCodeText?.SetText($"Mã phòng: {currentLobby.LobbyCode}");
            int localCount = currentLobby.Players != null ? currentLobby.Players.Count : 0;
            SetStatus($"Đã vào phòng ({localCount}/{MaxPlayers}). Chờ chủ phòng bắt đầu...");
            RefreshRoomRoster();
            await SyncLocalPlayerLobbyDataAsync();

            // Đọc lại lobby ngay sau khi join để host/client đều thấy roster mới nhất.
            var refreshed = await RefreshLobbySafe();
            if (refreshed != null)
            {
                currentLobby = refreshed;
                RefreshRoomRoster();
            }

            if (startMatchButton != null)
            {
                UpdateStartMatchButtonState(true);
            }

            RefreshAuthState();
            EnsureLobbyRuntimeRoutines();
            suppressAutoBattleStart = false;
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", $"JoinLobbyAndRelayAsync success: lobbyId={currentLobby.Id}, isHost={isHost}, players={(currentLobby.Players != null ? currentLobby.Players.Count : 0)}");

            return true;
        }

        public void NotifyEnteredFromBrowser()
        {
            ResolveTextReferences();
            RefreshAuthState();
            RefreshRoomRoster();
            EnsureLobbyRuntimeRoutines();
        }

        private async Task HandleStartMatch()
        {
            MultiplayerDetailedLogger.TraceUserAction("UIRoom", "HandleStartMatch", "startMatchButton");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", "HandleStartMatch invoked");
            if (isBusy || isQuitting)
                return;

            isBusy = true;
            SetActionButtonsInteractable(false);

            if (!isHost || currentLobby == null)
            {
                SetStatus("Chỉ chủ phòng mới được bắt đầu.");
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "HandleStartMatch denied: local user is not host or lobby null");
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
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", $"HandleStartMatch blocked: players={(lobby != null && lobby.Players != null ? lobby.Players.Count : 0)}/{MaxPlayers}");
                startMatchButton.interactable = false;
                isBusy = false;
                SetActionButtonsInteractable(true);
                return;
            }

            if (!IsRelayReadyForMatchHost())
            {
                SetStatus("Lobby đã đủ 2/2 nhưng Relay chưa kết nối đủ. Đợi vài giây rồi thử lại.");
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "HandleStartMatch blocked: relay not ready (connected clients < 2)");
                if (startMatchButton != null)
                {
                    startMatchButton.interactable = false;
                }
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
                var uiManager = UIManager.Instance;
                if (uiManager != null && uiManager.RequestNetworkGameStart())
                {
                    Log("StartMatch -> Network game start requested");
                    MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", $"HandleStartMatch set StartedKey=1, lobbyId={currentLobby.Id}");
                }
                else
                {
                    Log("StartMatch -> UIManager NULL hoặc không set được network game start");
                    MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "HandleStartMatch: UIManager null or RequestNetworkGameStart failed");
                }

                // Reliable sync path: push start signal directly to connected clients.
                SendStartMatchSignalToClients();
                NotifyBattleStarted();
            }
            catch (Exception ex)
            {
                SetStatus("Không thể bắt đầu trận.");
                Debug.LogError($"[UIRoom] StartMatch lỗi: {ex.Message}");
                MultiplayerDetailedLogger.TraceException("UI_ROOM", ex, "HandleStartMatch failed");
            }
            finally
            {
                isBusy = false;
                SetActionButtonsInteractable(true);
            }
        }

        private async Task HandleQuitRoom()
        {
            MultiplayerDetailedLogger.TraceUserAction("UIRoom", "HandleQuitRoom", "quitRoomButton");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", "HandleQuitRoom invoked");
            if (isBusy)
                return;

            isBusy = true;
            isQuitting = true;
            suppressAutoBattleStart = true;
            SetActionButtonsInteractable(false);
            SetStatus("Đang rời phòng...");

            StopRoutines();
            
            // FIX 2: Don't delete lobby immediately - mark as abandoned for 30s grace period
            // This allows clients to rejoin if host disconnects momentarily
            if (isHost && currentLobby != null)
            {
                try
                {
                    // Mark as abandoned instead of deleting
                    await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id,
                        new UpdateLobbyOptions
                        {
                            IsPrivate = true,  // Lock from new joins
                            Data = new Dictionary<string, DataObject>
                            {
                                { "IsAbandoned", new DataObject(DataObject.VisibilityOptions.Public, "1") },
                                { "AbandonedTime", new DataObject(DataObject.VisibilityOptions.Public, System.DateTime.UtcNow.Ticks.ToString()) }
                            }
                        });
                    Debug.Log($"[UIRoom] Marked lobby {currentLobby.Id} as abandoned (grace period 30s)");
                    MultiplayerDetailedLogger.Trace("UI_ROOM", $"HandleQuitRoom host marked lobby abandoned: lobbyId={currentLobby.Id}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UIRoom] Failed to mark abandoned: {ex.Message}, fallback to delete");
                    // Fallback to delete if mark fails
                    _ = LeaveLobbySafe();
                    MultiplayerDetailedLogger.TraceException("UI_ROOM", ex, "HandleQuitRoom mark-abandoned failed, fallback LeaveLobbySafe");
                }
            }
            else
            {
                // Client: just remove self from lobby
                await LeaveLobbySafe();
            }

            // Disconnect Relay
            RelayManager.Instance.Disconnect();

            ResetRoomSessionState(DefaultIdleStatus);

            onQuitRoom?.Invoke();
            if (quitRoomNavigator != null)
            {
                quitRoomNavigator.NavigateNow();
            }

            isBusy = false;
            isQuitting = false;
            SetActionButtonsInteractable(true);
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", "HandleQuitRoom completed");
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
            
            // Only treat null as fatal when lobby is confirmed deleted/not found.
            if (refreshed == null) 
            {
                Debug.LogWarning("[UIRoom] Lobby bị xóa (chủ phòng đã rời hoặc hết hạn)");
                SetStatus("Phòng bị chủ phòng đóng lại. Quay lại menu...");
                await Task.Delay(1500);
                
                // Force quit back to menu
                if (!isBusy && !isQuitting)
                {
                    isBusy = true;
                    isQuitting = true;
                    SetActionButtonsInteractable(false);
                    
                    StopRoutines();
                    RelayManager.Instance.Disconnect();
                    ResetRoomSessionState(DefaultIdleStatus);
                    
                    onQuitRoom?.Invoke();
                    if (quitRoomNavigator != null)
                    {
                        quitRoomNavigator.NavigateNow();
                    }

                    // Always unlock local UI state after forced quit path.
                    // If navigator keeps this panel visible, buttons must not stay stuck disabled.
                    isBusy = false;
                    isQuitting = false;
                    SetActionButtonsInteractable(true);
                }
                return;
            }

            currentLobby = refreshed;
            
            // Refresh toàn bộ UI: player count, roster, auth status (RefreshAuthState gọi RefreshRoomRoster() cuối)
            RefreshAuthState();

            if (isHost && startMatchButton != null)
            {
                UpdateStartMatchButtonState(true);
            }

            if (currentLobby.Data.TryGetValue(StartedKey, out var startedData) && startedData.Value == "1")
            {
                if (suppressAutoBattleStart)
                {
                    MultiplayerDetailedLogger.Trace("UI_ROOM", "Poll detected StartedKey=1 but auto-start is suppressed by local quit/back state");
                    return;
                }

                SetStatus("Trận đấu bắt đầu.");
                MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", $"Poll detected StartedKey=1 in lobbyId={currentLobby.Id}");
                NotifyBattleStarted();
            }
        }

        private void NotifyBattleStarted()
        {
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", "NotifyBattleStarted invoked");
            if (isQuitting || suppressAutoBattleStart)
            {
                MultiplayerDetailedLogger.Trace("UI_ROOM", "NotifyBattleStarted ignored due to local quit/back suppression");
                return;
            }

            // Nếu đã notify trước đó nhưng UI16 vẫn chưa visible, cho phép retry.
            if (battleStartNotified && IsBattleScreenVisible())
                return;

            // Do not hard-block UI transition when relay state is transient.
            // Client can still be routed by lobby StartedKey and scene fallback.
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                Log("NotifyBattleStarted: NetworkManager not listening, continue with UI fallback routing");
            }

            battleStartNotified = true;
            LogState("NotifyBattleStarted: begin");

            bool routedToBattle = TryShowBattlePanel();
            Log($"NotifyBattleStarted: direct battle route => {routedToBattle}");

            // Scene fallback chỉ chạy khi route UI thất bại hoàn toàn.
            if (!routedToBattle && !IsBattleScreenVisible())
            {
                LoadBattleSceneFallback();
            }

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

        private bool TryShowBattlePanel()
        {
            if (UIScreenRouter.TryShow(ref flowManager, UIFlowManager.Screen.MultiplayerBattle))
            {
                return true;
            }

            if (battleScreenFallback != null)
            {
                battleScreenFallback.SetActive(true);
                return true;
            }

            return false;
        }

        private void LoadBattleSceneFallback()
        {
            if (string.IsNullOrWhiteSpace(battleSceneName))
            {
                Log("LoadBattleSceneFallback: battleSceneName empty, skip scene load");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(battleSceneName))
            {
                Log($"LoadBattleSceneFallback: scene '{battleSceneName}' is not in Build Settings, skip load");
                return;
            }

            // Request multiplayer battle screen for next scene, then load the battle scene.
            SceneFlowBridge.RequestScreen(UIFlowManager.Screen.MultiplayerBattle);
            Log($"LoadBattleSceneFallback: loading scene '{battleSceneName}' mode={battleSceneLoadMode}");
            SceneManager.LoadScene(battleSceneName, battleSceneLoadMode);
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

            RefreshRoomRoster();
            try
            {
                return await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            }
            catch (LobbyServiceException ex) when (IsRateLimitException(ex))
            {
                nextLobbyReadAt = Time.unscaledTime + Mathf.Max(1f, rateLimitBackoffSeconds);
            RefreshRoomRoster();
                Debug.LogWarning($"[UIRoom] Refresh lobby bị rate limit, tạm dừng đọc đến t={nextLobbyReadAt:F1}. {ex.Reason} - {ex.Message}");
                return currentLobby;
            }
            catch (LobbyServiceException ex) when (IsLobbyNotFoundException(ex))
            {
                Debug.LogWarning($"[UIRoom] Refresh lobby báo not found: {ex.Reason} - {ex.Message}");
                return null;
            }
            catch (LobbyServiceException ex) when (!IsLobbyNotFoundException(ex))
            {
                Debug.LogWarning($"[UIRoom] Refresh lobby lỗi tạm thời, giữ state hiện tại: {ex.Reason} - {ex.Message}");
                return currentLobby;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Refresh lobby lỗi tạm thời: {ex.Message}");
                return currentLobby;
            }
        }

        private bool IsLobbyNotFoundException(LobbyServiceException ex)
        {
            if (ex == null)
                return false;

            string msg = ex.Message ?? string.Empty;
            string reason = ex.Reason.ToString();
            return msg.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                   || reason.IndexOf("NotFound", StringComparison.OrdinalIgnoreCase) >= 0
                   || reason.IndexOf("LobbyNotFound", StringComparison.OrdinalIgnoreCase) >= 0;
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
                RefreshAuthState();
                isBusy = false;
                SetActionButtonsInteractable(true);
                return false;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                SetStatus("Chưa đăng nhập dịch vụ multiplayer.");
                RefreshAuthState();
                isBusy = false;
                SetActionButtonsInteractable(true);
                return false;
            }

            // Services đã sẵn sàng và UGS đã signed-in, cập nhật auth status ngay.
            RefreshAuthState();

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

        private void EnsureLobbyRuntimeRoutines()
        {
            if (!gameObject.activeInHierarchy)
                return;

            EnsureStartMatchMessageHandlerRegistered();

            if (pollingRoutine == null)
            {
                pollingRoutine = StartCoroutine(PollLobbyRoutine());
            }

            if (isHost && currentLobby != null && heartbeatRoutine == null)
            {
                heartbeatRoutine = StartCoroutine(HeartbeatRoutine());
            }
        }

        private void EnsureStartMatchMessageHandlerRegistered()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || nm.CustomMessagingManager == null || !nm.IsListening)
                return;

            // Sau khi Shutdown -> StartClient/StartHost, named handlers có thể mất.
            // Luôn rebind khi network đang listening để tránh trạng thái stale.
            if (startMatchMessageHandlerRegistered)
            {
                nm.CustomMessagingManager.UnregisterNamedMessageHandler(StartMatchMessageName);
                startMatchMessageHandlerRegistered = false;
            }

            nm.CustomMessagingManager.RegisterNamedMessageHandler(StartMatchMessageName, HandleStartMatchMessageReceived);
            startMatchMessageHandlerRegistered = true;
            MultiplayerDetailedLogger.Trace("UI_ROOM", "Registered start-match named message handler");
        }

        private void UnregisterStartMatchMessageHandler()
        {
            if (!startMatchMessageHandlerRegistered)
                return;

            var nm = NetworkManager.Singleton;
            if (nm != null && nm.CustomMessagingManager != null)
            {
                nm.CustomMessagingManager.UnregisterNamedMessageHandler(StartMatchMessageName);
            }

            startMatchMessageHandlerRegistered = false;
            MultiplayerDetailedLogger.Trace("UI_ROOM", "Unregistered start-match named message handler");
        }

        private void SendStartMatchSignalToClients()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || nm.CustomMessagingManager == null)
            {
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "SendStartMatchSignalToClients skipped: network/server not ready");
                return;
            }

            using (var writer = new FastBufferWriter(sizeof(int), Allocator.Temp))
            {
                writer.WriteValueSafe(1);
                foreach (var clientId in nm.ConnectedClientsIds)
                {
                    if (clientId == nm.LocalClientId)
                        continue;

                    nm.CustomMessagingManager.SendNamedMessage(StartMatchMessageName, clientId, writer);
                    MultiplayerDetailedLogger.Trace("UI_ROOM", $"Sent start-match signal to clientId={clientId}");
                }
            }
        }

        private void HandleStartMatchMessageReceived(ulong senderClientId, FastBufferReader reader)
        {
            int signal = 0;
            reader.ReadValueSafe(out signal);
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", $"Received start-match signal={signal} from sender={senderClientId}");

            if (signal == 1)
            {
                if (suppressAutoBattleStart)
                {
                    MultiplayerDetailedLogger.Trace("UI_ROOM", "Ignored start-match signal because local quit/back suppression is active");
                    return;
                }

                SetStatus("Nhận tín hiệu bắt đầu từ host...");
                NotifyBattleStarted();
            }
        }

        private bool IsRelayReadyForMatchHost()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || !nm.IsListening)
                return false;

            return nm.ConnectedClientsIds != null && nm.ConnectedClientsIds.Count >= MaxPlayers;
        }

        private bool IsQuickJoinCandidateUsable(Lobby lobby)
        {
            if (lobby == null)
                return false;

            if (lobby.Data == null || !lobby.Data.TryGetValue(JoinCodeKey, out var joinCodeData) || string.IsNullOrWhiteSpace(joinCodeData.Value))
                return false;

            if (lobby.Data.TryGetValue(StartedKey, out var startedData) && startedData != null && startedData.Value == "1")
                return false;

            int currentPlayers = lobby.Players != null ? lobby.Players.Count : 0;
            if (currentPlayers >= lobby.MaxPlayers)
                return false;

            if (lobby.Data.TryGetValue(IsAbandonedKey, out var abandonedData) && abandonedData != null && abandonedData.Value == "1")
                return false;

            return true;
        }

        private void UpdateStartMatchButtonState(bool interactable)
        {
            if (startMatchButton == null)
                return;

            bool isInLobbySession = currentLobby != null;
            int playerCount = currentLobby != null && currentLobby.Players != null ? currentLobby.Players.Count : 0;
            bool hasEnoughPlayers = playerCount >= MaxPlayers;

            // Only show start when lobby is full to avoid misleading host UI in 1/2 state.
            bool visible = isHost && isInLobbySession && hasEnoughPlayers;
            startMatchButton.gameObject.SetActive(visible);
            startMatchButton.interactable = visible && interactable && IsRelayReadyForMatchHost();
        }

        private void SetStatus(string text)
        {
            roomStatusMessage = text;
            RefreshStatusLabel();
            Debug.Log($"[UIRoom] {text}");
            MultiplayerDetailedLogger.Trace("UI_ROOM_STATUS", text);
        }

        private void Log(string message)
        {
            MultiplayerDetailedLogger.Trace("UI_ROOM_LOG", message);
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
            RefreshRoomRoster();
        }

        private void SetActionButtonsInteractable(bool interactable)
        {
            bool isInLobbySession = currentLobby != null;

            // Khi đã ở trong room thì khóa các hành động Create/Join để tránh lệch trạng thái giữa 2 client.
            if (createRoomButton != null) createRoomButton.interactable = interactable && !isInLobbySession;
            if (quickJoinButton != null) quickJoinButton.interactable = interactable && !isInLobbySession;
            if (joinByCodeButton != null) joinByCodeButton.interactable = interactable && !isInLobbySession;

            // Chỉ cho quit khi đang ở trong room.
            if (quitRoomButton != null) quitRoomButton.interactable = interactable && isInLobbySession;

            if (startMatchButton != null)
            {
                UpdateStartMatchButtonState(interactable);
            }

            MultiplayerDetailedLogger.Trace("UI_ROOM", $"SetActionButtonsInteractable: interactable={interactable}, inLobbySession={isInLobbySession}, isHost={isHost}");
        }
    }
}
