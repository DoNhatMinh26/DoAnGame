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
using DoAnGame.Multiplayer;

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
        private const string GradeKey = "Grade";
        private const string Player1ReadyKey = "Player1Ready";
        private const string Player2ReadyKey = "Player2Ready";
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
        [Tooltip("Chỉ hiển thị cho HOST — bắt đầu trận khi client đã sẵn sàng")]
        [SerializeField] private Button startMatchButton;
        [Tooltip("Chỉ hiển thị cho CLIENT — báo sẵn sàng cho host")]
        [SerializeField] private Button readyButton;
        [Tooltip("Nút làm mới thủ công — force refresh lobby ngay lập tức")]
        [SerializeField] private Button refreshButton;

        [Header("Room Inputs")]
        [SerializeField] private TMP_InputField roomCodeInput;

        [Header("Room Texts")]
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text playerCountText;

        [Header("Grade Selection")]
        [SerializeField] private TMP_Dropdown gradeDropdown;

        [Header("Room Roster Texts")]
        [SerializeField] private TMP_Text rosterTitleText;
        [SerializeField] private TMP_Text rosterListText;

        [Header("Callbacks")]
        [SerializeField] private UIFlowManager flowManager;
        [SerializeField] private UIButtonScreenNavigator startBattleNavigator;
        [SerializeField] private UIButtonScreenNavigator quitRoomNavigator;
        [SerializeField] private UIButtonScreenNavigator loadingPanelNavigator; // ← THÊM DÒNG NÀY
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
        
        // ✅ STATIC flag để persist ngay cả khi GameObject inactive
        private static bool s_needsRematchReset; // Flag để báo hiệu cần reset cho rematch
        private float nextLobbyReadAt;
        private Coroutine delayedBattleFallbackRoutine;
        private AuthManager authManager;
        private string roomStatusMessage = DefaultIdleStatus;
        private string authStatusMessage = "Chưa đăng nhập dịch vụ multiplayer";
        private const string StartMatchMessageName = "ui_room_start_match";
        private bool startMatchMessageHandlerRegistered;
        private bool receivedStartSignalFromHost; // Client-only: true khi nhận NGO message từ host

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
            readyButton?.onClick.AddListener(() => _ = HandleReadyButtonClick());
            refreshButton?.onClick.AddListener(() => _ = HandleRefreshButton());
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
            
            // Check nếu cần reset cho rematch
            if (s_needsRematchReset)
            {
                Debug.Log("[UIRoom] 🔄 OnShow detected s_needsRematchReset=true, calling ResetForRematch");
                s_needsRematchReset = false;
                ResetForRematch();
                return; // ResetForRematch đã gọi RefreshAuthState, RefreshRoomRoster, UpdateReadyButtonState
            }
            
            // ✅ FIX 3: Reset flags SAU khi check rematch
            // CHÚ Ý: KHÔNG reset receivedStartSignalFromHost ở đây nếu đang trong lobby session
            // vì host back từ LobbyBrowserPanel không phải là "join mới"
            battleStartNotified = false;
            suppressAutoBattleStart = false;
            isQuitting = false;
            if (currentLobby == null)
            {
                // Chỉ reset khi không có lobby (idle state)
                receivedStartSignalFromHost = false;
            }
            
            // ✅ FIX: Refresh UI với data hiện tại
            RefreshAuthState();
            RefreshRoomRoster();
            EnsureInitialized();
            UpdateReadyButtonState();
            
            // ✅ FIX: Đảm bảo polling chạy khi panel active trở lại
            // (polling có thể bị null nếu chưa được start, hoặc đã bị stop)
            EnsureLobbyRuntimeRoutines();
        }

        /// <summary>
        /// Force refresh lobby khi OnShow() - để host thấy client đã join khi quay lại từ LobbyBrowserPanel
        /// </summary>
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

            // ✅ Subscribe to NetworkManager events để refresh khi client connect
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }

            // ✅ FIX: UIButtonScreenNavigator dùng SetActive trực tiếp, KHÔNG gọi Show()/OnShow()
            // Vì vậy OnEnable() phải làm đầy đủ những gì OnShow() làm:
            // 1. Force refresh lobby để lấy data mới nhất
            // 2. Refresh toàn bộ UI
            // 3. Đảm bảo polling chạy
            if (currentLobby != null)
            {
                Debug.Log("[UIRoom] 🔄 OnEnable: Panel active lại, force refresh lobby...");
                _ = OnEnableRefreshAsync();
            }

            EnsureLobbyRuntimeRoutines();
            EnsureStartMatchMessageHandlerRegistered();
        }

        /// <summary>
        /// Force refresh lobby khi OnEnable() - UIButtonScreenNavigator dùng SetActive trực tiếp
        /// nên OnShow() không được gọi, phải refresh trong OnEnable()
        /// </summary>
        private async Task OnEnableRefreshAsync()
        {
            try
            {
                // Bypass rate limit để lấy data mới nhất ngay lập tức
                nextLobbyReadAt = 0f;

                var refreshed = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                if (refreshed != null)
                {
                    currentLobby = refreshed;
                    int playerCount = refreshed.Players?.Count ?? 0;
                    Debug.Log($"[UIRoom] ✅ OnEnable refresh: {playerCount} players in lobby");

                    RefreshAuthState();
                    RefreshRoomRoster();
                    UpdateReadyButtonState();

                    if (isHost && startMatchButton != null)
                    {
                        UpdateStartMatchButtonState(true);
                    }
                    
                    // ✅ FIX: Aggressive polling kéo dài hơn (10 lần @ 1s) để bắt được ready state
                    // Client có thể mark ready sau khi host quay lại panel
                    if (isHost && playerCount >= 2)
                    {
                        Debug.Log("[UIRoom] 🔄 Starting aggressive polling to catch client ready state...");
                        for (int i = 0; i < 10; i++) // ← Tăng từ 3 lên 10 lần
                        {
                            await Task.Delay(1000); // ← Tăng từ 500ms lên 1000ms
                            var polled = await RefreshLobbySafe();
                            if (polled != null)
                            {
                                currentLobby = polled;
                                RefreshRoomRoster();
                                UpdateReadyButtonState();
                                
                                bool clientReady = IsClientReady(currentLobby);
                                Debug.Log($"[UIRoom] 🔄 Aggressive poll #{i+1}: clientReady={clientReady}");
                                
                                if (clientReady)
                                {
                                    Debug.Log("[UIRoom] ✅ Client ready detected! Stopping aggressive polling.");
                                    break; // Client đã ready, dừng aggressive polling
                                }
                            }
                        }
                    }
                }
            }
            catch (LobbyServiceException ex) when (IsLobbyNotFoundException(ex))
            {
                Debug.LogWarning("[UIRoom] OnEnable refresh: Lobby không còn tồn tại");
                ResetRoomSessionState(DefaultIdleStatus);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] OnEnable refresh failed: {ex.Message}");
                // Vẫn refresh UI với data cũ
                RefreshAuthState();
                RefreshRoomRoster();
            }
        }

        private void OnDisable()
        {
            if (authManager != null)
            {
                authManager.OnCurrentUserChanged -= HandleAuthUserChanged;
            }

            // ✅ Unsubscribe NetworkManager events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        /// <summary>
        /// ✅ Callback khi client connect vào NetworkManager (chỉ host nhận)
        /// </summary>
        private void HandleClientConnected(ulong clientId)
        {
            if (!isHost || currentLobby == null) return;

            Debug.Log($"[UIRoom] 🔔 Client {clientId} connected! Force refreshing lobby...");
            
            // Force refresh lobby để thấy player mới ngay
            _ = ForceRefreshLobby();
        }

        /// <summary>
        /// ✅ Callback khi client disconnect
        /// </summary>
        private void HandleClientDisconnected(ulong clientId)
        {
            if (!isHost || currentLobby == null) return;

            Debug.Log($"[UIRoom] 🔔 Client {clientId} disconnected! Force refreshing lobby...");
            
            // Force refresh lobby để update roster
            _ = ForceRefreshLobby();
        }

        /// <summary>
        /// ✅ Force refresh lobby (bypass rate limit)
        /// </summary>
        private async Task ForceRefreshLobby()
        {
            if (currentLobby == null) return;

            try
            {
                nextLobbyReadAt = 0f; // Bypass rate limit
                await Task.Delay(500); // Đợi server sync
                
                var refreshed = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                if (refreshed != null)
                {
                    currentLobby = refreshed;
                    RefreshRoomRoster();
                    UpdateReadyButtonState();
                    Debug.Log($"[UIRoom] ✅ Force refreshed: {currentLobby.Players?.Count ?? 0} players");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Force refresh failed: {ex.Message}");
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

            // ✅ Chỉ hiển thị roomStatusMessage, không hiển thị authStatusMessage nữa
            statusText.SetText(roomStatusMessage);
        }

        private void RefreshRoomRoster()
        {
            // ✅ KHÔNG gán rosterTitleText - giữ nguyên text từ Inspector
            // ✅ KHÔNG gán playerCountText - giữ nguyên text từ Inspector
            // Chỉ update rosterListText với danh sách người chơi

            if (rosterListText != null)
            {
                rosterListText.SetText(BuildRosterText());
            }
            else if (statusText != null)
            {
                statusText.SetText(BuildRosterText());
            }

            // Always show start button when in a lobby, just update state
            if (currentLobby != null && startMatchButton != null)
            {
                startMatchButton.gameObject.SetActive(true);
                UpdateReadyButtonState();
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
                Debug.LogWarning("[UIRoom] BuildRosterText: currentLobby null or no players");
                return "Chưa có ai trong room.";
            }

            Debug.Log($"[UIRoom] BuildRosterText: {currentLobby.Players.Count} players in lobby");
            var lines = new List<string>(currentLobby.Players.Count);
            for (int i = 0; i < currentLobby.Players.Count; i++)
            {
                var lobbyPlayer = currentLobby.Players[i];
                string displayName = ResolveLobbyPlayerName(lobbyPlayer, i);
                lines.Add($"{i + 1}. {displayName}");
                Debug.Log($"[UIRoom]   Player {i + 1}: {displayName} (ID: {lobbyPlayer?.Id})");
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

        /// <summary>
        /// Lấy grade đã chọn từ Dropdown (1-5)
        /// Mặc định = 1 nếu dropdown null hoặc không chọn
        /// </summary>
        private int GetSelectedGrade()
        {
            if (gradeDropdown == null)
            {
                Debug.LogWarning("[UIRoom] Grade dropdown chưa gán, dùng mặc định Lớp 1");
                return 1;
            }

            // Dropdown value: 0→Lớp 1, 1→Lớp 2, ..., 4→Lớp 5
            int grade = gradeDropdown.value + 1;
            
            // Clamp để đảm bảo trong khoảng 1-5
            grade = Mathf.Clamp(grade, 1, 5);
            
            Debug.Log($"[UIRoom] Grade đã chọn: Lớp {grade}");
            return grade;
        }

        /// <summary>
        /// Đọc grade từ Lobby metadata
        /// Trả về 1 nếu không tìm thấy (mặc định)
        /// </summary>
        private int GetLobbyGrade(Lobby lobby)
        {
            if (lobby == null || lobby.Data == null)
            {
                return 1;  // Mặc định Lớp 1
            }

            if (!lobby.Data.TryGetValue(GradeKey, out var gradeData) || gradeData == null)
            {
                return 1;  // Mặc định Lớp 1
            }

            if (int.TryParse(gradeData.Value, out int grade))
            {
                return Mathf.Clamp(grade, 1, 5);  // Đảm bảo 1-5
            }

            return 1;  // Fallback
        }

        /// <summary>
        /// Lấy grade của lobby hiện tại (public để các controller khác đọc)
        /// </summary>
        public int GetCurrentLobbyGrade()
        {
            return GetLobbyGrade(currentLobby);
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
            
            // ✅ FIX: CHỈ reset flags - KHÔNG dừng polling
            // Polling cần luôn chạy dù panel inactive (host ở LobbyBrowserPanel)
            receivedStartSignalFromHost = false;
            battleStartNotified = false;
            suppressAutoBattleStart = false;
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

            // ✅ FIX 1: Reset ALL state flags
            suppressAutoBattleStart = false;
            isQuitting = false;
            battleStartNotified = false;
            receivedStartSignalFromHost = false;
            
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
                int selectedGrade = GetSelectedGrade();  // ← Lấy grade từ dropdown

                var options = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode, DataObject.IndexOptions.S1) },
                        { StartedKey, new DataObject(DataObject.VisibilityOptions.Public, "0", DataObject.IndexOptions.S2) },
                        { ModeKey, new DataObject(DataObject.VisibilityOptions.Public, "MathDuel") },
                        { HostNameKey, new DataObject(DataObject.VisibilityOptions.Public, GetCurrentPlayerDisplayName()) },
                        { GradeKey, new DataObject(DataObject.VisibilityOptions.Public, selectedGrade.ToString()) }
                    },
                    Player = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
                    {
                        { CharacterNameKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, GetCurrentPlayerDisplayName()) }
                    })
                };

                currentLobby = await LobbyService.Instance.CreateLobbyAsync("Math Room", MaxPlayers, options);
                isHost = true;
                receivedStartSignalFromHost = false; // Reset flag khi tạo phòng mới (host không cần flag này nhưng reset cho nhất quán)
                await SyncLocalPlayerLobbyDataAsync();

                lobbyCodeText?.SetText($"Mã phòng: {currentLobby.LobbyCode}");
                SetStatus($"Đã tạo phòng. Độ khó: Lớp {selectedGrade}. Chờ người chơi thứ 2...");
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

            // ✅ FIX 4: Reset ALL state flags
            suppressAutoBattleStart = false;
            isQuitting = false;
            battleStartNotified = false;
            receivedStartSignalFromHost = false;
            
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

            // ✅ FIX 5: Reset ALL state flags
            suppressAutoBattleStart = false;
            isQuitting = false;
            battleStartNotified = false;
            receivedStartSignalFromHost = false;
            
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
            
            // ✅ FIX: Update dropdown để hiển thị đúng grade của phòng host
            int lobbyGrade = GetLobbyGrade(currentLobby);
            if (gradeDropdown != null)
            {
                gradeDropdown.value = lobbyGrade - 1; // Dropdown: 0→Lớp 1, 1→Lớp 2, ...
                gradeDropdown.interactable = false; // Lock dropdown khi đã join phòng
                Debug.Log($"[UIRoom] ✅ Updated dropdown to Grade {lobbyGrade} (locked)");
            }
            
            int localCount = currentLobby.Players != null ? currentLobby.Players.Count : 0;
            SetStatus($"Đã vào phòng ({localCount}/{MaxPlayers}). Chờ chủ phòng bắt đầu...");
            RefreshRoomRoster();
            await SyncLocalPlayerLobbyDataAsync();

            // ✅ FIX: Bypass rate limit để force refresh ngay sau khi join
            nextLobbyReadAt = 0f; // Reset rate limit
            await Task.Delay(300); // Đợi server propagate changes
            var refreshed = await RefreshLobbySafe();
            if (refreshed != null)
            {
                currentLobby = refreshed;
                RefreshRoomRoster();
                Debug.Log($"[UIRoom] ✅ Force refreshed lobby after join: {currentLobby.Players?.Count ?? 0} players");
            }

            if (startMatchButton != null)
            {
                UpdateReadyButtonState();
            }

            RefreshAuthState();
            EnsureLobbyRuntimeRoutines();
            suppressAutoBattleStart = false;
            receivedStartSignalFromHost = false; // Reset flag khi join phòng mới
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
            if (isBusy || isQuitting) return;
            
            // ✅ FIX 6: Double-check với retry logic
            if (!isHost || currentLobby == null)
            {
                SetStatus("Chỉ chủ phòng mới được bắt đầu.");
                return;
            }
            
            // Đợi NetworkManager sẵn sàng (max 2s)
            int retries = 0;
            while (retries < 4)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    break;
                
                await Task.Delay(500);
                retries++;
            }
            
            bool isActuallyHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            if (!isActuallyHost)
            {
                Debug.LogError($"[UIRoom] NetworkManager not ready after 2s! IsServer={NetworkManager.Singleton?.IsServer}");
                SetStatus("Lỗi kết nối. Thử lại sau.");
                return;
            }

            isBusy = true;
            SetActionButtonsInteractable(false);

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

            // Check if client is ready
            if (!IsClientReady(lobby))
            {
                SetStatus("Chờ người chơi khác sẵn sàng...");
                MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "HandleStartMatch blocked: client not ready");
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
            
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            GameLogger.Log($"[UIRoom] [{role}] HandleQuitRoom START");
            
            if (isBusy)
            {
                GameLogger.Log($"[UIRoom] [{role}] HandleQuitRoom: isBusy=true, returning early");
                return;
            }

            isBusy = true;
            isQuitting = true;
            suppressAutoBattleStart = true;
            SetActionButtonsInteractable(false);
            SetStatus("Đang rời phòng...");
            
            GameLogger.Log($"[UIRoom] [{role}] Set isBusy=true, isQuitting=true, suppressAutoBattleStart=true");

            StopRoutines();
            GameLogger.Log($"[UIRoom] [{role}] Stopped all routines");
            
            // ✅ FIX: Null currentLobby NGAY sau StopRoutines để polling không tiếp tục
            // gọi UpdateReadyButtonState() với data cũ trong khi đang reset
            var lobbyToLeave = currentLobby;
            bool wasHost = isHost;
            currentLobby = null;
            isHost = false;
            GameLogger.Log($"[UIRoom] [{role}] Cleared currentLobby/isHost early to stop stale UI updates");
            
            // FIX 2: Don't delete lobby immediately - mark as abandoned for 30s grace period
            // This allows clients to rejoin if host disconnects momentarily
            if (wasHost && lobbyToLeave != null)
            {
                GameLogger.Log($"[UIRoom] [{role}] Is HOST - marking lobby as abandoned (lobbyId={lobbyToLeave.Id})");
                try
                {
                    // Mark as abandoned instead of deleting
                    await LobbyService.Instance.UpdateLobbyAsync(lobbyToLeave.Id,
                        new UpdateLobbyOptions
                        {
                            IsPrivate = true,  // Lock from new joins
                            Data = new Dictionary<string, DataObject>
                            {
                                { "IsAbandoned", new DataObject(DataObject.VisibilityOptions.Public, "1") },
                                { "AbandonedTime", new DataObject(DataObject.VisibilityOptions.Public, System.DateTime.UtcNow.Ticks.ToString()) }
                            }
                        });
                    Debug.Log($"[UIRoom] Marked lobby {lobbyToLeave.Id} as abandoned (grace period 30s)");
                    GameLogger.Log($"[UIRoom] [{role}] ✅ Lobby marked as abandoned successfully");
                    MultiplayerDetailedLogger.Trace("UI_ROOM", $"HandleQuitRoom host marked lobby abandoned: lobbyId={lobbyToLeave.Id}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UIRoom] Failed to mark abandoned: {ex.Message}, fallback to delete");
                    GameLogger.Log($"[UIRoom] [{role}] ⚠️ Failed to mark abandoned: {ex.Message} - fallback to LeaveLobbySafe");
                    // Fallback: delete lobby directly
                    try { await LobbyService.Instance.DeleteLobbyAsync(lobbyToLeave.Id); } catch { }
                    MultiplayerDetailedLogger.TraceException("UI_ROOM", ex, "HandleQuitRoom mark-abandoned failed, fallback delete");
                }
            }
            else
            {
                // Client: just remove self from lobby
                GameLogger.Log($"[UIRoom] [{role}] Is CLIENT - removing self from lobby");
                if (lobbyToLeave != null && AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
                {
                    try
                    {
                        await LobbyService.Instance.RemovePlayerAsync(lobbyToLeave.Id, AuthenticationService.Instance.PlayerId);
                        GameLogger.Log($"[UIRoom] [{role}] ✅ Removed self from lobby");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UIRoom] RemovePlayer lỗi: {ex.Message}");
                        GameLogger.Log($"[UIRoom] [{role}] ⚠️ RemovePlayer failed: {ex.Message}");
                    }
                }
            }

            // Disconnect Relay
            GameLogger.Log($"[UIRoom] [{role}] Disconnecting Relay...");
            RelayManager.Instance.Disconnect();
            GameLogger.Log($"[UIRoom] [{role}] ✅ Relay disconnected");

            GameLogger.Log($"[UIRoom] [{role}] Calling ResetRoomSessionState...");
            ResetRoomSessionState(DefaultIdleStatus);
            GameLogger.Log($"[UIRoom] [{role}] ✅ ResetRoomSessionState completed");

            onQuitRoom?.Invoke();
            if (quitRoomNavigator != null)
            {
                GameLogger.Log($"[UIRoom] [{role}] Calling quitRoomNavigator.NavigateNow()...");
                quitRoomNavigator.NavigateNow();
                GameLogger.Log($"[UIRoom] [{role}] ✅ Navigation completed");
            }
            else
            {
                GameLogger.Log($"[UIRoom] [{role}] ⚠️ quitRoomNavigator is NULL - cannot navigate");
            }

            isBusy = false;
            isQuitting = false;
            SetActionButtonsInteractable(true);
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", "HandleQuitRoom completed");
            GameLogger.Log($"[UIRoom] [{role}] HandleQuitRoom COMPLETE - isBusy=false, isQuitting=false");
        }

        private void ResetRoomSessionState(string status)
        {
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            GameLogger.Log($"[UIRoom] [{role}] ResetRoomSessionState START");
            
            currentLobby = null;
            isHost = false;
            battleStartNotified = false;
            receivedStartSignalFromHost = false; // Reset flag khi rời phòng
            nextLobbyReadAt = 0f;
            
            GameLogger.Log($"[UIRoom] [{role}] Reset flags: currentLobby=null, isHost=false, battleStartNotified=false, receivedStartSignalFromHost=false");
            
            // ✅ FIX: Unlock dropdown khi rời phòng
            if (gradeDropdown != null)
            {
                gradeDropdown.interactable = true;
                GameLogger.Log($"[UIRoom] [{role}] ✅ Unlocked grade dropdown");
            }
            
            // ✅ FIX: Ẩn tất cả battle panels để LobbyPanel hiển thị đúng
            GameLogger.Log($"[UIRoom] [{role}] Calling HideAllBattlePanels...");
            HideAllBattlePanels();
            
            // ✅ FIX: Reset WinsPanel state để tránh hiển thị data cũ
            GameLogger.Log($"[UIRoom] [{role}] Calling ResetWinsPanelState...");
            ResetWinsPanelState();
            
            // ✅ FIX: Reset BattleManager state để tránh conflict trận mới
            GameLogger.Log($"[UIRoom] [{role}] Calling ResetBattleManagerState...");
            ResetBattleManagerState();
            
            GameLogger.Log($"[UIRoom] [{role}] Calling ApplyIdleVisualState...");
            ApplyIdleVisualState(status);
            roomCodeInput?.SetTextWithoutNotify(string.Empty);
            
            GameLogger.Log($"[UIRoom] [{role}] ResetRoomSessionState COMPLETE");
        }

        /// <summary>
        /// Ẩn tất cả battle panels (GameplayPanel, WinsPanel, LoadingPanel, QuitPopup)
        /// để đảm bảo LobbyPanel hiển thị đúng khi quit room
        /// </summary>
        private void HideAllBattlePanels()
        {
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            
            try
            {
                // Ẩn GameplayPanel
                var gameplayPanel = FindObjectOfType<UIMultiplayerBattleController>(true);
                if (gameplayPanel != null)
                {
                    gameplayPanel.Hide();
                    Debug.Log("[UIRoom] ✅ Hidden GameplayPanel");
                    GameLogger.Log($"[UIRoom] [{role}] ✅ Hidden GameplayPanel");
                }
                else
                {
                    GameLogger.Log($"[UIRoom] [{role}] GameplayPanel not found");
                }

                // Ẩn WinsPanel
                var winsPanel = FindObjectOfType<UIWinsController>(true);
                if (winsPanel != null)
                {
                    winsPanel.Hide();
                    Debug.Log("[UIRoom] ✅ Hidden WinsPanel");
                    GameLogger.Log($"[UIRoom] [{role}] ✅ Hidden WinsPanel");
                }
                else
                {
                    GameLogger.Log($"[UIRoom] [{role}] WinsPanel not found");
                }

                // Ẩn LoadingPanel
                var loadingPanel = FindObjectOfType<UIMultiplayerLoadingController>(true);
                if (loadingPanel != null)
                {
                    loadingPanel.Hide();
                    Debug.Log("[UIRoom] ✅ Hidden LoadingPanel");
                    GameLogger.Log($"[UIRoom] [{role}] ✅ Hidden LoadingPanel");
                }
                else
                {
                    GameLogger.Log($"[UIRoom] [{role}] LoadingPanel not found");
                }

                // Ẩn QuitPopup
                var quitPopup = FindObjectOfType<UIBattleQuitConfirmPopup>(true);
                if (quitPopup != null)
                {
                    quitPopup.Hide();
                    Debug.Log("[UIRoom] ✅ Hidden QuitPopup");
                    GameLogger.Log($"[UIRoom] [{role}] ✅ Hidden QuitPopup");
                }
                else
                {
                    GameLogger.Log($"[UIRoom] [{role}] QuitPopup not found");
                }

                // Hiển thị LobbyPanel
                Show();
                Debug.Log("[UIRoom] ✅ Shown LobbyPanel");
                GameLogger.Log($"[UIRoom] [{role}] ✅ Shown LobbyPanel");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Failed to hide battle panels: {ex.Message}");
                GameLogger.Log($"[UIRoom] [{role}] ❌ ERROR in HideAllBattlePanels: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset WinsPanel state để tránh hiển thị data cũ khi quay lại LobbyPanel
        /// </summary>
        private void ResetWinsPanelState()
        {
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            
            try
            {
                // Reset static cache của WinsPanel
                UIWinsController.LastResult = new UIWinsController.MatchResultData
                {
                    IsValid = false
                };
                Debug.Log("[UIRoom] ✅ Reset WinsPanel state");
                GameLogger.Log($"[UIRoom] [{role}] ✅ Reset WinsPanel.LastResult (IsValid=false)");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Failed to reset WinsPanel state: {ex.Message}");
                GameLogger.Log($"[UIRoom] [{role}] ❌ ERROR in ResetWinsPanelState: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset BattleManager state để tránh conflict khi tạo trận mới
        /// </summary>
        private void ResetBattleManagerState()
        {
            var net = NetworkManager.Singleton;
            string role = (net != null && net.IsServer) ? "HOST" : "CLIENT";
            
            try
            {
                var battleManager = NetworkedMathBattleManager.Instance;
                if (battleManager != null)
                {
                    // Nếu là server, reset NetworkVariables
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    {
                        battleManager.MatchStarted.Value = false;
                        battleManager.MatchEnded.Value = false;
                        battleManager.WinnerId.Value = -1;
                        battleManager.IsAbandoned.Value = false;
                        battleManager.AbandonedPlayerId.Value = -1;
                        battleManager.CurrentQuestion.Value = default;
                        battleManager.CorrectAnswer.Value = 0;
                        battleManager.TimeRemaining.Value = 0f;
                        Debug.Log("[UIRoom] ✅ Reset BattleManager NetworkVariables (Server)");
                        GameLogger.Log($"[UIRoom] [{role}] ✅ Reset BattleManager NetworkVariables (Server)");
                    }
                    else
                    {
                        Debug.Log("[UIRoom] ⚠️ Not server, skipping BattleManager NetworkVariable reset");
                        GameLogger.Log($"[UIRoom] [{role}] ⚠️ Not server, skipping BattleManager NetworkVariable reset");
                    }
                }
                else
                {
                    GameLogger.Log($"[UIRoom] [{role}] BattleManager not found");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Failed to reset BattleManager state: {ex.Message}");
                GameLogger.Log($"[UIRoom] [{role}] ❌ ERROR in ResetBattleManagerState: {ex.Message}");
            }
        }

        private void ApplyIdleVisualState(string status)
        {
            SetStatus(status);
            SetPlayerCount(DefaultIdlePlayerCount);
            lobbyCodeText?.SetText(DefaultIdleCode);

            startMatchButton?.gameObject.SetActive(false);
            readyButton?.gameObject.SetActive(false);
            refreshButton?.gameObject.SetActive(false); // ✅ Ẩn refresh button khi idle
        }

        private IEnumerator PollLobbyRoutine()
        {
            while (true)
            {
                // ✅ FIX: Polling LUÔN CHẠY dù LobbyPanel inactive (host ở LobbyBrowserPanel)
                // Dừng khi: isQuitting, không có lobby, hoặc đã vào battle
                if (currentLobby != null && !isQuitting && !battleStartNotified)
                {
                    _ = PollLobbyOnce();
                }
                
                // ✅ DYNAMIC POLLING:
                // - 0.5s khi chờ player 2 (cần detect join nhanh)
                // - 2s khi đã đủ 2 players (tránh rate limit, chỉ cần detect ready state)
                int playerCount = currentLobby?.Players?.Count ?? 0;
                float interval = (playerCount < MaxPlayers) ? 0.5f : 2f;
                
                // ✅ FIX: Dùng WaitForSecondsRealtime để coroutine chạy ngay cả khi GameObject inactive
                yield return new WaitForSecondsRealtime(interval);
                
                // ✅ Dừng polling khi đã vào battle
                if (battleStartNotified)
                {
                    Debug.Log("[UIRoom] Polling dừng: battleStartNotified=true");
                    break;
                }
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

            // ✅ FIX 7: CHỈ HOST check StartedKey từ poll
            // Client PHẢI đợi NGO message từ host (HandleStartMatchMessageReceived)
            bool isActuallyHost = isHost && 
                                 (NetworkManager.Singleton != null && 
                                  NetworkManager.Singleton.IsServer);
            
            if (isActuallyHost && 
                currentLobby.Data.TryGetValue(StartedKey, out var startedData) && 
                startedData.Value == "1")
            {
                if (suppressAutoBattleStart)
                {
                    MultiplayerDetailedLogger.Trace("UI_ROOM", "Poll detected StartedKey=1 but suppressed");
                    return;
                }

                SetStatus("Trận đấu bắt đầu.");
                MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", $"Poll detected StartedKey=1 in lobbyId={currentLobby.Id}");
                NotifyBattleStarted();
            }

            // Update ready button state based on lobby metadata
            UpdateReadyButtonState();
            
            // ✅ FIX: Adaptive polling - tăng tốc khi có 2 players nhưng chưa ready
            // Giảm interval xuống 1s thay vì 1.5s để bắt ready state nhanh hơn
            // QUAN TRỌNG: Polling này chạy MÃI MÃI cho đến khi client ready hoặc battle start!
            if (isHost && currentLobby.Players != null && currentLobby.Players.Count >= 2)
            {
                bool clientReady = IsClientReady(currentLobby);
                if (!clientReady)
                {
                    // Tăng tốc polling: 1s thay vì 1.5s
                    nextLobbyReadAt = Time.unscaledTime + 1f;
                    // Debug.Log($"[UIRoom] ⚡ Adaptive polling: 2 players but not ready, polling faster (1s)");
                }
                else
                {
                    // Client đã ready, quay lại polling bình thường
                    nextLobbyReadAt = Time.unscaledTime + pollIntervalSeconds;
                }
            }
            else
            {
                // Polling bình thường
                nextLobbyReadAt = Time.unscaledTime + pollIntervalSeconds;
            }
        }

        /// <summary>
        /// Client marks themselves as ready by updating lobby metadata
        /// </summary>
        /// <summary>
        /// Client bấm nút "Sẵn sàng" — chỉ gửi trạng thái, KHÔNG chuyển UI
        /// </summary>
        private async Task HandleReadyButtonClick()
        {
            Debug.Log($"[UIRoom] 🟢 HandleReadyButtonClick START: isHost={isHost}, isBusy={isBusy}");
            if (isBusy || isHost || currentLobby == null)
            {
                Debug.LogWarning($"[UIRoom] HandleReadyButtonClick blocked: isBusy={isBusy}, isHost={isHost}, lobby={(currentLobby != null)}");
                return;
            }

            isBusy = true;
            if (readyButton != null) readyButton.interactable = false;

            try
            {
                Debug.Log("[UIRoom] 🟢 Calling MarkClientReadyAsync...");
                await MarkClientReadyAsync();
                Debug.Log("[UIRoom] 🟢 MarkClientReadyAsync completed. UI should stay on LobbyPanel.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIRoom] HandleReadyButtonClick lỗi: {ex.Message}");
                SetStatus("Không thể gửi trạng thái sẵn sàng. Thử lại.");
                if (readyButton != null) readyButton.interactable = true;
            }
            finally
            {
                isBusy = false;
                Debug.Log($"[UIRoom] 🟢 HandleReadyButtonClick END: isHost={isHost}, receivedStartSignalFromHost={receivedStartSignalFromHost}");
            }
        }

        /// <summary>
        /// Xử lý khi người dùng click nút "Làm mới" — force refresh lobby ngay lập tức
        /// </summary>
        private async Task HandleRefreshButton()
        {
            if (isBusy || currentLobby == null)
            {
                Debug.LogWarning($"[UIRoom] HandleRefreshButton blocked: isBusy={isBusy}, lobby={(currentLobby != null)}");
                return;
            }

            isBusy = true;
            SetActionButtonsInteractable(false);
            SetStatus("Đang làm mới...");
            MultiplayerDetailedLogger.TraceUserAction("UI_ROOM", "HandleRefreshButton", "Manual refresh requested");

            try
            {
                // Bypass rate limit bằng cách reset nextLobbyReadAt
                nextLobbyReadAt = 0f;
                
                // Force refresh lobby
                var refreshed = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                if (refreshed != null)
                {
                    currentLobby = refreshed;
                    
                    // ✅ FIX: Refresh toàn bộ UI state
                    RefreshAuthState();
                    RefreshRoomRoster();
                    UpdateReadyButtonState();
                    
                    int playerCount = refreshed.Players?.Count ?? 0;
                    Debug.Log($"[UIRoom] ✅ Manual refresh successful: {playerCount} players");
                    MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", $"HandleRefreshButton success: players={playerCount}");
                    
                    // Hiển thị status phù hợp
                    if (isHost)
                    {
                        bool clientReady = IsClientReady(currentLobby);
                        if (playerCount < 2)
                        {
                            SetStatus("Chờ người chơi thứ 2 sẵn sàng...");
                        }
                        else if (clientReady)
                        {
                            SetStatus("Người chơi đã sẵn sàng! Có thể bắt đầu.");
                        }
                        else
                        {
                            SetStatus("Chờ người chơi thứ 2 sẵn sàng...");
                        }
                    }
                    else
                    {
                        bool localReady = IsLocalPlayerReady(currentLobby);
                        if (localReady)
                        {
                            SetStatus("Đã sẵn sàng! Chờ chủ phòng bắt đầu...");
                        }
                        else
                        {
                            SetStatus("Nhấn Sẵn sàng để báo hiệu cho chủ phòng.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[UIRoom] Manual refresh returned null");
                    SetStatus("Không thể làm mới. Thử lại.");
                    MultiplayerDetailedLogger.TraceWarning("UI_ROOM", "HandleRefreshButton returned null");
                }
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogError($"[UIRoom] Manual refresh lỗi: {ex.Reason} - {ex.Message}");
                SetStatus("Lỗi làm mới. Thử lại sau.");
                MultiplayerDetailedLogger.TraceException("UI_ROOM", ex, "HandleRefreshButton failed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIRoom] Manual refresh lỗi: {ex.Message}");
                SetStatus("Lỗi làm mới. Thử lại sau.");
                MultiplayerDetailedLogger.TraceException("UI_ROOM", ex, "HandleRefreshButton failed");
            }
            finally
            {
                isBusy = false;
                SetActionButtonsInteractable(true);
            }
        }

        /// <summary>
        /// Async method to mark client as ready - dùng UpdatePlayerAsync vì client không có quyền UpdateLobbyAsync
        /// </summary>
        private async Task MarkClientReadyAsync()
        {
            try
            {
                if (currentLobby == null)
                    return;

                string localPlayerId = AuthenticationService.Instance?.PlayerId;
                if (string.IsNullOrEmpty(localPlayerId))
                {
                    Debug.LogError("[UIRoom] Cannot mark ready: no local player ID");
                    return;
                }

                // Client dùng UpdatePlayerAsync để update Player Data của chính mình
                var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(
                    currentLobby.Id,
                    localPlayerId,
                    new UpdatePlayerOptions
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { Player2ReadyKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "1") }
                        }
                    }
                );

                // Chỉ update currentLobby nếu StartedKey chưa được set
                // Tránh trường hợp lobby snapshot cũ có StartedKey=1 trigger NotifyBattleStarted sớm
                if (updatedLobby != null)
                {
                    bool alreadyStarted = updatedLobby.Data != null &&
                                         updatedLobby.Data.TryGetValue(StartedKey, out var sd) &&
                                         sd.Value == "1";
                    if (!alreadyStarted)
                    {
                        currentLobby = updatedLobby;
                    }
                }

                SetStatus("Đã sẵn sàng! Chờ chủ phòng bắt đầu...");
                UpdateReadyButtonState();
                MultiplayerDetailedLogger.Trace("UI_ROOM", "Client marked ready via UpdatePlayerAsync");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIRoom] Failed to mark ready: {ex.Message}");
                MultiplayerDetailedLogger.TraceException("UI_ROOM", ex, "MarkClientReadyAsync failed");
            }
        }

        /// <summary>
        /// Update ready button text and interactable state based on lobby metadata
        /// </summary>
        /// <summary>
        /// Cập nhật trạng thái 2 button riêng biệt:
        /// - startMatchButton: chỉ hiển thị cho HOST
        /// - readyButton: chỉ hiển thị cho CLIENT
        /// </summary>
        private void UpdateReadyButtonState()
        {
            if (currentLobby == null)
            {
                startMatchButton?.gameObject.SetActive(false);
                readyButton?.gameObject.SetActive(false);
                refreshButton?.gameObject.SetActive(false); // ✅ Ẩn refresh button khi không có lobby
                return;
            }

            // Check nếu đang ở chế độ rematch
            bool isRematchMode = roomStatusMessage == "Bạn có muốn tiếp tục đấu lại?";

            if (isHost)
            {
                // HOST: ẩn readyButton, hiển thị startMatchButton và refreshButton
                readyButton?.gameObject.SetActive(false);
                
                // ✅ FIX: Hiển thị refresh button cho host
                if (refreshButton != null)
                {
                    refreshButton.gameObject.SetActive(true);
                    refreshButton.interactable = !isBusy; // Disable khi đang busy
                }

                if (startMatchButton != null)
                {
                    startMatchButton.gameObject.SetActive(true);
                    bool clientReady = IsClientReady(currentLobby);
                    startMatchButton.interactable = clientReady && IsRelayReadyForMatchHost();
                    // Button luôn hiển thị "Bắt đầu", trạng thái chờ hiển thị ở statusText
                    startMatchButton.GetComponentInChildren<TMP_Text>()?.SetText("Bắt đầu");
                    
                    // Hiển thị trạng thái ở statusText
                    if (isRematchMode)
                    {
                        // Rematch mode: append status to rematch message
                        SetStatus(clientReady
                            ? "Bạn có muốn tiếp tục đấu lại?\nNgười chơi đã sẵn sàng! Có thể bắt đầu."
                            : "Bạn có muốn tiếp tục đấu lại?\nĐợi thành viên sẵn sàng...");
                    }
                    else
                    {
                        // Normal mode
                        SetStatus(clientReady
                            ? "Người chơi đã sẵn sàng! Có thể bắt đầu."
                            : "Chờ người chơi thứ 2 sẵn sàng...");
                    }
                }
            }
            else
            {
                // CLIENT: ẩn startMatchButton và refreshButton, hiển thị readyButton
                startMatchButton?.gameObject.SetActive(false);
                refreshButton?.gameObject.SetActive(false); // ✅ Client không cần refresh button

                if (readyButton != null)
                {
                    readyButton.gameObject.SetActive(true);
                    bool alreadyReady = IsLocalPlayerReady(currentLobby);
                    readyButton.interactable = !alreadyReady;
                    readyButton.GetComponentInChildren<TMP_Text>()?.SetText(
                        alreadyReady ? "Đã sẵn sàng ✓" : "Sẵn sàng");
                    
                    // Hiển thị trạng thái ở statusText
                    if (isRematchMode)
                    {
                        // Rematch mode: append status to rematch message
                        SetStatus(alreadyReady
                            ? "Bạn có muốn tiếp tục đấu lại?\nĐã sẵn sàng! Chờ chủ phòng bắt đầu..."
                            : "Bạn có muốn tiếp tục đấu lại?\nẤn Sẵn sàng để chuẩn bị đấu lại.");
                    }
                    else
                    {
                        // Normal mode
                        SetStatus(alreadyReady
                            ? "Đã sẵn sàng! Chờ chủ phòng bắt đầu..."
                            : "Nhấn Sẵn sàng để báo hiệu cho chủ phòng.");
                    }
                }
            }
        }

        /// <summary>
        /// Check if client (Player 2 / non-host) is ready bằng cách đọc Player Data
        /// </summary>
        private bool IsClientReady(Lobby lobby)
        {
            if (lobby == null || lobby.Players == null)
                return false;

            string hostId = lobby.HostId;

            // Tìm player không phải host (client)
            foreach (var player in lobby.Players)
            {
                if (player == null) continue;
                if (string.Equals(player.Id, hostId, StringComparison.OrdinalIgnoreCase)) continue;

                // Đây là client - check Player Data
                if (player.Data != null &&
                    player.Data.TryGetValue(Player2ReadyKey, out var readyData) &&
                    readyData.Value == "1")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check nếu local player (client) đã mark ready chưa
        /// </summary>
        private bool IsLocalPlayerReady(Lobby lobby)
        {
            if (lobby == null || lobby.Players == null) return false;

            string localId = AuthenticationService.Instance?.PlayerId;
            if (string.IsNullOrEmpty(localId)) return false;

            foreach (var player in lobby.Players)
            {
                if (player == null) continue;
                if (!string.Equals(player.Id, localId, StringComparison.OrdinalIgnoreCase)) continue;

                if (player.Data != null && player.Data.TryGetValue(Player2ReadyKey, out var readyData))
                {
                    bool isReady = readyData != null && readyData.Value == "1";
                    Debug.Log($"[UIRoom] IsLocalPlayerReady: playerId={localId}, hasKey=true, value={readyData?.Value ?? "null"}, isReady={isReady}");
                    return isReady;
                }
                else
                {
                    Debug.Log($"[UIRoom] IsLocalPlayerReady: playerId={localId}, hasKey=false, isReady=false");
                    return false;
                }
            }

            Debug.Log($"[UIRoom] IsLocalPlayerReady: local player not found in lobby");
            return false;
        }

        private void NotifyBattleStarted()
        {
            Debug.LogWarning($"[UIRoom] 🔥 NotifyBattleStarted() CALLED 🔥 isHost={isHost}, receivedStartSignalFromHost={receivedStartSignalFromHost}");
            Debug.LogWarning($"[UIRoom] 🔥 Stack trace: {System.Environment.StackTrace}");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", "NotifyBattleStarted invoked");
            
            if (isQuitting || suppressAutoBattleStart)
            {
                Debug.LogWarning($"[UIRoom] ❌ EARLY RETURN #1: isQuitting={isQuitting}, suppressAutoBattleStart={suppressAutoBattleStart}");
                MultiplayerDetailedLogger.Trace("UI_ROOM", "NotifyBattleStarted ignored due to local quit/back suppression");
                return;
            }

            // CLIENT GUARD: Client KHÔNG ĐƯỢC phép vào battle nếu chưa nhận NGO message từ host
            // Đây là guard cuối cùng - nếu client vẫn vào được, có nghĩa có bug ở đâu đó
            if (!isHost && !receivedStartSignalFromHost)
            {
                Debug.LogError($"[UIRoom] ❌❌❌ CRITICAL: CLIENT TRYING TO ENTER BATTLE WITHOUT HOST PERMISSION! isHost={isHost}, receivedStartSignalFromHost={receivedStartSignalFromHost}");
                Debug.LogError($"[UIRoom] ❌❌❌ Stack trace: {System.Environment.StackTrace}");
                MultiplayerDetailedLogger.Trace("UI_ROOM", "CRITICAL: NotifyBattleStarted blocked - client must receive NGO message from host first");
                return;
            }

            // Nếu đã notify trước đó nhưng UI16 vẫn chưa visible, cho phép retry.
            if (battleStartNotified && IsBattleScreenVisible())
            {
                Debug.LogWarning($"[UIRoom] ❌ EARLY RETURN #3: battleStartNotified={battleStartNotified}, IsBattleScreenVisible={IsBattleScreenVisible()}");
                return;
            }

            // ADDITIONAL GUARD: Nếu đã notify rồi, không gọi lại (tránh multiple calls)
            if (battleStartNotified)
            {
                Debug.LogWarning($"[UIRoom] ❌ EARLY RETURN #4: battleStartNotified={battleStartNotified} - already notified once, prevent re-entry");
                return;
            }

            // Do not hard-block UI transition when relay state is transient.
            // Client can still be routed by lobby StartedKey and scene fallback.
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                Log("NotifyBattleStarted: NetworkManager not listening, continue with UI fallback routing");
            }

            battleStartNotified = true;
            LogState("NotifyBattleStarted: begin");

            // ===== KHỞI TẠO BATTLE MANAGER TRƯỚC KHI CHUYỂN PANEL =====
            Log("[UIRoom] ⚠️ ABOUT TO CALL InitializeMultiplayerBattle()...");
            InitializeMultiplayerBattleImmediate(); // Gọi trực tiếp, không delay
            Log("[UIRoom] ⚠️ AFTER InitializeMultiplayerBattle() call");
            // ==================================================

            // ✅ GIẢI PHÁP MỚI: Dùng BasePanelController.Show()/Hide() trực tiếp
            // Không dùng UIButtonScreenNavigator nữa
            
            // Ẩn LobbyPanel (panel hiện tại)
            Log("[UIRoom] Hiding LobbyPanel...");
            Hide();

            // Tìm và hiển thị LoadingPanel
            var loadingPanel = FindObjectOfType<UIMultiplayerLoadingController>(true); // true = include inactive
            if (loadingPanel != null)
            {
                Log($"[UIRoom] ✅ Found LoadingPanel, showing it...");
                loadingPanel.Show();
            }
            else
            {
                Debug.LogError("[UIRoom] ❌ LoadingPanel not found! Cannot proceed.");
                
                // Fallback: Hiển thị trực tiếp GameplayPanel
                var gameplayPanel = FindObjectOfType<UIMultiplayerBattleController>(true);
                if (gameplayPanel != null)
                {
                    Debug.LogWarning("[UIRoom] Fallback: Showing GameplayPanel directly");
                    gameplayPanel.Show();
                }
                else
                {
                    Debug.LogError("[UIRoom] ❌ GameplayPanel also not found! Critical error.");
                }
            }

            onBattleStarted?.Invoke();
            LogState("NotifyBattleStarted: end");
        }

        /// <summary>
        /// Khởi tạo NetworkedMathBattleManager với grade đã chọn (chỉ Host)
        /// Gọi NGAY LẬP TỨC trước khi GameObject bị inactive
        /// </summary>
        private void InitializeMultiplayerBattleImmediate()
        {
            Debug.LogWarning("[UIRoom] 🔥🔥🔥 InitializeMultiplayerBattleImmediate() ENTRY POINT 🔥🔥🔥");
            Log("[UIRoom] 🔄 InitializeMultiplayerBattleImmediate executing...");
            
            // Kiểm tra NetworkManager
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[UIRoom] ❌ NetworkManager is NULL!");
                return;
            }

            Log($"[UIRoom] NetworkManager state: IsServer={NetworkManager.Singleton.IsServer}, IsListening={NetworkManager.Singleton.IsListening}");

            if (!NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[UIRoom] ⚠️ NetworkManager not listening yet!");
                return;
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning($"[UIRoom] ⚠️ Not server (IsServer={NetworkManager.Singleton.IsServer})");
                return;
            }

            // NetworkManager sẵn sàng - Khởi tạo battle
            Log("[UIRoom] ✅ NetworkManager ready! Initializing battle...");
            
            // Lấy grade đã chọn từ lobby
            int selectedGrade = GetCurrentLobbyGrade();
            Log($"[UIRoom] Selected grade: {selectedGrade}");
            
            // Tìm BattleManager trong scene
            var battleManager = FindObjectOfType<DoAnGame.Multiplayer.NetworkedMathBattleManager>();
            
            if (battleManager != null)
            {
                Log($"[UIRoom] Found BattleManager: {battleManager.gameObject.name}");
                
                // Kiểm tra xem BattleManager có NetworkObject không
                var netObj = battleManager.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    Log($"[UIRoom] ℹ️ BattleManager has NetworkObject (IsSpawned={netObj.IsSpawned})");
                    
                    // BattleManager là scene object, đã tự động spawn khi scene load
                    // KHÔNG CẦN gọi netObj.Spawn() thủ công!
                }
                else
                {
                    Debug.LogWarning("[UIRoom] ⚠️ BattleManager không có NetworkObject component! NetworkVariable sẽ không sync!");
                }
                
                // Khởi tạo battle
                battleManager.InitializeBattle(selectedGrade);
                Log($"[UIRoom] ✅ Initialized battle with Grade {selectedGrade}");
            }
            else
            {
                Debug.LogError("[UIRoom] ❌ NetworkedMathBattleManager not found! Make sure BattleManager exists in scene.");
            }
        }

        /// <summary>
        /// Khởi tạo NetworkedMathBattleManager với grade đã chọn (chỉ Host)
        /// Retry logic để đảm bảo NetworkManager đã sẵn sàng
        /// </summary>
        private void InitializeMultiplayerBattle()
        {
            Debug.LogWarning("[UIRoom] 🔥🔥🔥 InitializeMultiplayerBattle() ENTRY POINT 🔥🔥🔥");
            Log("[UIRoom] 🔄 InitializeMultiplayerBattle called, scheduling delayed init...");
            
            // Kiểm tra xem GameObject có active không
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogError("[UIRoom] ❌ GameObject is INACTIVE! Cannot use Invoke()!");
                // Gọi trực tiếp nếu GameObject inactive
                InitializeMultiplayerBattleDelayed();
                return;
            }
            
            // Delay 1 giây để đảm bảo NetworkManager đã sẵn sàng
            Invoke(nameof(InitializeMultiplayerBattleDelayed), 1f);
        }

        private void InitializeMultiplayerBattleDelayed()
        {
            Debug.LogWarning("[UIRoom] 🔥🔥🔥 InitializeMultiplayerBattleDelayed() ENTRY POINT 🔥🔥🔥");
            Log("[UIRoom] 🔄 InitializeMultiplayerBattleDelayed executing...");
            
            // Kiểm tra NetworkManager
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[UIRoom] ❌ NetworkManager is NULL!");
                return;
            }

            Log($"[UIRoom] NetworkManager state: IsServer={NetworkManager.Singleton.IsServer}, IsListening={NetworkManager.Singleton.IsListening}");

            if (!NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[UIRoom] ⚠️ NetworkManager not listening yet, retrying in 0.5s...");
                Invoke(nameof(InitializeMultiplayerBattleDelayed), 0.5f);
                return;
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning($"[UIRoom] ⚠️ Not server (IsServer={NetworkManager.Singleton.IsServer}), retrying in 0.5s...");
                Invoke(nameof(InitializeMultiplayerBattleDelayed), 0.5f);
                return;
            }

            // NetworkManager sẵn sàng - Khởi tạo battle
            Log("[UIRoom] ✅ NetworkManager ready! Initializing battle...");
            
            // Lấy grade đã chọn từ lobby
            int selectedGrade = GetCurrentLobbyGrade();
            Log($"[UIRoom] Selected grade: {selectedGrade}");
            
            // Tìm BattleManager trong scene
            var battleManager = FindObjectOfType<DoAnGame.Multiplayer.NetworkedMathBattleManager>();
            
            if (battleManager != null)
            {
                Log($"[UIRoom] Found BattleManager: {battleManager.gameObject.name}");
                battleManager.InitializeBattle(selectedGrade);
                Log($"[UIRoom] ✅ Initialized battle with Grade {selectedGrade}");
            }
            else
            {
                Debug.LogError("[UIRoom] ❌ NetworkedMathBattleManager not found! Make sure BattleManager exists in scene.");
            }
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

        public void RequestRefresh()
        {
            _ = HandleRefreshButton();
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

            // Set nextLobbyReadAt trước khi gọi API để tránh concurrent calls
            nextLobbyReadAt = Time.unscaledTime + Mathf.Max(1f, pollIntervalSeconds);

            try
            {
                var result = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                // Reset backoff khi thành công
                nextLobbyReadAt = Time.unscaledTime + pollIntervalSeconds;
                return result;
            }
            catch (LobbyServiceException ex) when (IsRateLimitException(ex))
            {
                // Rate limit: backoff dài hơn
                nextLobbyReadAt = Time.unscaledTime + Mathf.Max(2f, rateLimitBackoffSeconds);
                Debug.LogWarning($"[UIRoom] Refresh lobby bị rate limit, backoff đến t={nextLobbyReadAt:F1}s. {ex.Reason}");
                RefreshRoomRoster();
                return currentLobby;
            }
            catch (LobbyServiceException ex) when (IsLobbyNotFoundException(ex))
            {
                Debug.LogWarning($"[UIRoom] Refresh lobby báo not found: {ex.Reason} - {ex.Message}");
                return null;
            }
            catch (LobbyServiceException ex)
            {
                // Lỗi tạm thời: backoff 2s rồi thử lại
                nextLobbyReadAt = Time.unscaledTime + 2f;
                Debug.LogWarning($"[UIRoom] Refresh lobby lỗi tạm thời, backoff 2s: {ex.Reason} - {ex.Message}");
                return currentLobby;
            }
            catch (Exception ex)
            {
                // Lỗi không xác định: backoff 2s
                nextLobbyReadAt = Time.unscaledTime + 2f;
                Debug.LogWarning($"[UIRoom] Refresh lobby lỗi: {ex.Message}");
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
            EnsureStartMatchMessageHandlerRegistered();

            // ✅ FIX: StartCoroutine cần gameObject active, nhưng một khi đã start,
            // coroutine với WaitForSecondsRealtime sẽ tiếp tục chạy dù panel inactive
            if (pollingRoutine == null && gameObject.activeInHierarchy)
            {
                pollingRoutine = StartCoroutine(PollLobbyRoutine());
            }

            if (isHost && currentLobby != null && heartbeatRoutine == null && gameObject.activeInHierarchy)
            {
                heartbeatRoutine = StartCoroutine(HeartbeatRoutine());
            }
        }

        /// <summary>
        /// Coroutine helper: đợi LobbyPanel active rồi start polling
        /// </summary>
        private static IEnumerator StartPollingWhenReady(UIMultiplayerRoomController controller)
        {
            // Đợi tối đa 10s cho đến khi panel active
            float timeout = 10f;
            float elapsed = 0f;
            while (!controller.gameObject.activeInHierarchy && elapsed < timeout)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                elapsed += 0.1f;
            }

            if (controller != null && controller.gameObject.activeInHierarchy && controller.pollingRoutine == null)
            {
                controller.pollingRoutine = controller.StartCoroutine(controller.PollLobbyRoutine());
                Debug.Log("[UIRoom] ✅ Polling started after panel became active");
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
            Debug.LogWarning($"[UIRoom] 🔴 HandleStartMatchMessageReceived: signal={signal}, sender={senderClientId}, isHost={isHost}");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_ROOM", $"Received start-match signal={signal} from sender={senderClientId}");

            if (signal == 1)
            {
                if (suppressAutoBattleStart)
                {
                    Debug.LogWarning("[UIRoom] 🔴 Ignored: suppressAutoBattleStart=true");
                    MultiplayerDetailedLogger.Trace("UI_ROOM", "Ignored start-match signal because local quit/back suppression is active");
                    return;
                }

                // Set flag để cho phép client vào battle
                receivedStartSignalFromHost = true;
                Debug.LogWarning("[UIRoom] 🔴 TRIGGERING NotifyBattleStarted from NGO message! receivedStartSignalFromHost=true");
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
            
            // ✅ Hiển thị nút khi đủ 2 players (dù client chưa sẵn sàng)
            // Chỉ enable khi client đã click "Sẵn sàng"
            // KHÔNG check IsRelayReadyForMatchHost() ở đây - chỉ check khi thực sự bắt đầu
            bool clientReady = IsClientReady(currentLobby);
            bool visible = isHost && isInLobbySession && hasEnoughPlayers;
            bool relayReady = IsRelayReadyForMatchHost();
            
            startMatchButton.gameObject.SetActive(visible);
            // Xám khi client chưa sẵn sàng, sáng khi client đã sẵn sàng
            startMatchButton.interactable = visible && interactable && clientReady;
            
            Debug.Log($"[UIRoom] UpdateStartMatchButton: players={playerCount}, clientReady={clientReady}, relayReady={relayReady}, visible={visible}, interactable={startMatchButton.interactable}");
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

            // Nút Làm mới: chỉ hiển thị khi đang ở trong room
            if (refreshButton != null)
            {
                refreshButton.gameObject.SetActive(isInLobbySession);
                refreshButton.interactable = interactable && isInLobbySession;
            }

            if (startMatchButton != null)
            {
                UpdateStartMatchButtonState(interactable);
            }

            MultiplayerDetailedLogger.Trace("UI_ROOM", $"SetActionButtonsInteractable: interactable={interactable}, inLobbySession={isInLobbySession}, isHost={isHost}");
        }

        /// <summary>
        /// Set flag để báo hiệu cần reset cho rematch khi LobbyPanel được hiển thị
        /// </summary>
        public void MarkNeedsRematchReset()
        {
            s_needsRematchReset = true;
            Debug.Log("[UIRoom] 🔄 MarkNeedsRematchReset: STATIC flag set to true");
        }

        /// <summary>
        /// Static method để set rematch reset flag từ bên ngoài (e.g., UIWinsController)
        /// </summary>
        public static void SetRematchResetFlag()
        {
            s_needsRematchReset = true;
            Debug.Log("[UIRoom] 🔄 SetRematchResetFlag: STATIC flag set to true");
        }

        /// <summary>
        /// Reset ready state khi quay về từ Wins Panel để chuẩn bị rematch
        /// </summary>
        public void ResetForRematch()
        {
            Debug.Log("[UIRoom] 🔄 ResetForRematch called");

            // Reset flags
            battleStartNotified = false;
            receivedStartSignalFromHost = false;
            suppressAutoBattleStart = false;

            // Reset room status message
            if (currentLobby != null)
            {
                roomStatusMessage = "Bạn có muốn tiếp tục đấu lại?";
                
                // Reset Player2ReadyKey để client phải mark ready lại
                _ = ClearReadyStateAsync();
            }
            else
            {
                roomStatusMessage = DefaultIdleStatus;
            }

            // Refresh UI
            RefreshStatusLabel();
            RefreshRoomRoster();
            UpdateReadyButtonState();
            
            // Restart polling
            StopRoutines();
            pollingRoutine = StartCoroutine(PollLobbyRoutine());

            Debug.Log($"[UIRoom] 🔄 ResetForRematch done: isHost={isHost}, roomStatus={roomStatusMessage}");
        }

        /// <summary>
        /// Clear Player2ReadyKey để reset ready state cho rematch
        /// </summary>
        private async Task ClearReadyStateAsync()
        {
            try
            {
                if (currentLobby == null)
                    return;

                string localPlayerId = AuthenticationService.Instance?.PlayerId;
                if (string.IsNullOrEmpty(localPlayerId))
                {
                    Debug.LogWarning("[UIRoom] Cannot clear ready state: no local player ID");
                    return;
                }

                if (isHost)
                {
                    // Host clear StartedKey trong Lobby Data
                    Debug.Log("[UIRoom] 🔄 Host clearing StartedKey for rematch...");
                    
                    var updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                        currentLobby.Id,
                        new UpdateLobbyOptions
                        {
                            Data = new Dictionary<string, DataObject>
                            {
                                { StartedKey, new DataObject(DataObject.VisibilityOptions.Member, "0") }
                            }
                        }
                    );

                    if (updatedLobby != null)
                    {
                        currentLobby = updatedLobby;
                        Debug.Log($"[UIRoom] ✅ Host cleared StartedKey successfully. StartedKey={updatedLobby.Data?.GetValueOrDefault(StartedKey)?.Value ?? "null"}");
                    }
                }
                else
                {
                    // Client clear Player2ReadyKey trong Player Data
                    Debug.Log($"[UIRoom] 🔄 Client clearing Player2ReadyKey for rematch... localPlayerId={localPlayerId}");

                    var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(
                        currentLobby.Id,
                        localPlayerId,
                        new UpdatePlayerOptions
                        {
                            Data = new Dictionary<string, PlayerDataObject>
                            {
                                { Player2ReadyKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") }
                            }
                        }
                    );

                    if (updatedLobby != null)
                    {
                        currentLobby = updatedLobby;
                        
                        // Debug: check Player2ReadyKey value in updated lobby
                        foreach (var player in updatedLobby.Players)
                        {
                            if (player.Id == localPlayerId && player.Data != null)
                            {
                                var hasKey = player.Data.TryGetValue(Player2ReadyKey, out var readyData);
                                Debug.Log($"[UIRoom] ✅ Client cleared Player2ReadyKey. hasKey={hasKey}, value={readyData?.Value ?? "null"}");
                            }
                        }
                    }
                }

                // Force refresh lobby để đảm bảo có snapshot mới nhất
                await Task.Delay(200); // Đợi server propagate changes
                var refreshedLobby = await RefreshLobbySafe();
                if (refreshedLobby != null)
                {
                    currentLobby = refreshedLobby;
                    Debug.Log("[UIRoom] 🔄 Force refreshed lobby after clearing ready state");
                    
                    // Debug: check Player2ReadyKey value in refreshed lobby
                    if (!isHost)
                    {
                        foreach (var player in refreshedLobby.Players)
                        {
                            if (player.Id == localPlayerId && player.Data != null)
                            {
                                var hasKey = player.Data.TryGetValue(Player2ReadyKey, out var readyData);
                                Debug.Log($"[UIRoom] 🔄 After refresh: hasKey={hasKey}, value={readyData?.Value ?? "null"}");
                            }
                        }
                    }
                }

                // Refresh UI sau khi clear
                RefreshAuthState(); // Gọi RefreshAuthState thay vì chỉ UpdateReadyButtonState
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UIRoom] Failed to clear ready state: {ex.Message}");
            }
        }
    }
}
