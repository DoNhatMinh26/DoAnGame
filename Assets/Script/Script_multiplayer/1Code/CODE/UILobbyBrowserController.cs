using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI browser cho danh sách lobby đang mở.
    /// </summary>
    public class UILobbyBrowserController : BasePanelController
    {
        private const string StartedKey = "Started";
        private const string CharacterNameKey = "characterName";
        private const string HostNameKey = "HostName";

        [Header("Browser Buttons")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button backButton;

        [Header("Browser Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;

        [Header("Browser List")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private LobbyBrowserEntryWidget entryPrefab;

        [Header("Navigation")]
        [SerializeField] private UIButtonScreenNavigator backNavigator;
        [SerializeField] private UIButtonScreenNavigator roomNavigator;
        [SerializeField] private UIMultiplayerRoomController roomController;

        [Header("Query Tuning")]
        [SerializeField] private int maxResults = 20;
        [SerializeField] private bool autoRefreshOnShow = true;
        [SerializeField] private bool showFullRooms = true;
        [SerializeField] private bool showStartedRooms = true;
        [SerializeField] private int maxLobbyNameLength = 18;
        [SerializeField] private int maxHostNameLength = 14;

        private readonly List<LobbyBrowserEntryWidget> spawnedEntries = new List<LobbyBrowserEntryWidget>();
        private bool isBusy;
        private AuthManager authManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;

            refreshButton?.onClick.AddListener(() => _ = RefreshLobbyListAsync());
            backButton?.onClick.AddListener(HandleBackClicked);
        }

        protected override void OnShow()
        {
            base.OnShow();
            ResolveRoomController();
            ApplyCompactListLayout();

            if (autoRefreshOnShow)
            {
                _ = RefreshLobbyListAsync();
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            ClearEntries();
        }

        private void OnDestroy()
        {
            refreshButton?.onClick.RemoveAllListeners();
            backButton?.onClick.RemoveAllListeners();
            ClearEntries();
        }

        private void HandleBackClicked()
        {
            if (backNavigator != null)
            {
                backNavigator.NavigateNow();
                return;
            }

            Hide();
        }

        private void ResolveRoomController()
        {
            if (roomController == null)
            {
                roomController = FindObjectOfType<UIMultiplayerRoomController>(true);
            }
        }

        private async Task RefreshLobbyListAsync()
        {
            if (isBusy)
                return;

            isBusy = true;
            SetStatus("Đang tải danh sách phòng...");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_LOBBY_BROWSER", "RefreshLobbyListAsync begin");

            try
            {
                await EnsureMultiplayerReady();

                var response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                {
                    Count = maxResults,
                    Filters = BuildFilters(),
                    Order = BuildOrder()
                });

                RenderLobbyList(response != null ? response.Results : null);
                MultiplayerDetailedLogger.Trace("UI_LOBBY_BROWSER", $"RefreshLobbyListAsync success, results={(response != null && response.Results != null ? response.Results.Count : 0)}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LobbyBrowser] Không tải được danh sách phòng: {ex.Message}");
                SetStatus("Không tải được danh sách phòng.");
                MultiplayerDetailedLogger.TraceException("UI_LOBBY_BROWSER", ex, "RefreshLobbyListAsync failed");
            }
            finally
            {
                isBusy = false;
            }
        }

        private List<QueryFilter> BuildFilters()
        {
            // Tránh lọc quá sớm ở server vì có phòng không có index field S2 sẽ bị ẩn mất.
            // Lọc hiển thị sẽ thực hiện ở client để chắc chắn thấy đủ danh sách.
            return new List<QueryFilter>();
        }

        private List<QueryOrder> BuildOrder()
        {
            return new List<QueryOrder>
            {
                new QueryOrder(true, QueryOrder.FieldOptions.AvailableSlots),
                new QueryOrder(false, QueryOrder.FieldOptions.Created),
                new QueryOrder(false, QueryOrder.FieldOptions.Name)
            };
        }

        private void RenderLobbyList(List<Lobby> lobbies)
        {
            ClearEntries();

            if (titleText != null)
            {
                titleText.SetText("Danh sách phòng đang mở");
            }

            if (lobbies == null || lobbies.Count == 0)
            {
                SetStatus("Không có phòng nào đang mở.");
                return;
            }

            List<Lobby> visibleLobbies = FilterVisibleLobbies(lobbies);
            if (visibleLobbies.Count == 0)
            {
                SetStatus("Không có phòng phù hợp để hiển thị.");
                return;
            }

            SetStatus($"Tìm thấy {visibleLobbies.Count} phòng.");

            for (int i = 0; i < visibleLobbies.Count; i++)
            {
                var lobby = visibleLobbies[i];
                if (lobby == null)
                    continue;

                bool canJoin = CanJoinLobby(lobby);

                var widget = Instantiate(entryPrefab, contentRoot);
                widget.Bind(
                    BuildLobbyName(lobby),
                    BuildHostLabel(lobby),
                    BuildPlayerCount(lobby),
                    BuildLobbyStatus(lobby),
                    canJoin ? (() => _ = JoinLobbyAsync(lobby)) : null);
                spawnedEntries.Add(widget);
            }

            if (contentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
                Canvas.ForceUpdateCanvases();
            }
        }

        private string BuildLobbyName(Lobby lobby)
        {
            if (lobby == null)
                return "Phòng không xác định";

            string value = string.IsNullOrWhiteSpace(lobby.Name) ? "Phòng Math" : lobby.Name;
            return ShortenWithEllipsis(value, maxLobbyNameLength);
        }

        private string BuildHostLabel(Lobby lobby)
        {
            if (lobby == null)
                return "Chủ phòng: Không rõ";

            string hostName = null;
            if (!string.IsNullOrWhiteSpace(lobby.HostId) && lobby.Players != null)
            {
                for (int i = 0; i < lobby.Players.Count; i++)
                {
                    var player = lobby.Players[i];
                    if (player == null || !string.Equals(player.Id, lobby.HostId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    hostName = ResolveLobbyPlayerName(player, i);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(hostName))
            {
                if (lobby.Data != null && lobby.Data.TryGetValue(HostNameKey, out var hostNameData) && hostNameData != null)
                {
                    hostName = hostNameData.Value;
                }
            }

            if (string.IsNullOrWhiteSpace(hostName))
            {
                hostName = "Chủ phòng";
            }

            return $"Chủ phòng: {hostName}";
        }

        private string BuildPlayerCount(Lobby lobby)
        {
            if (lobby == null)
                return "Người chơi: 0/0";

            int currentPlayers = lobby.Players != null ? lobby.Players.Count : 0;
            return $"Người chơi: {currentPlayers}/{lobby.MaxPlayers}";
        }

        private string BuildLobbyStatus(Lobby lobby)
        {
            if (lobby == null || lobby.Data == null)
                return "Đang chờ vào phòng";

            if (lobby.Data.TryGetValue(StartedKey, out var startedData) && startedData != null && startedData.Value == "1")
                return "Đang trong trận";

            if (!HasAvailableSlots(lobby))
                return "Phòng đã đầy";

            return "Đang chờ người chơi";
        }

        private List<Lobby> FilterVisibleLobbies(List<Lobby> lobbies)
        {
            var result = new List<Lobby>();
            if (lobbies == null)
                return result;

            for (int i = 0; i < lobbies.Count; i++)
            {
                var lobby = lobbies[i];
                if (lobby == null)
                    continue;

                bool started = IsLobbyStarted(lobby);
                bool hasSlot = HasAvailableSlots(lobby);

                if (!showStartedRooms && started)
                    continue;

                if (!showFullRooms && !hasSlot)
                    continue;

                result.Add(lobby);
            }

            return result;
        }

        private bool CanJoinLobby(Lobby lobby)
        {
            if (lobby == null)
                return false;

            return !IsLobbyStarted(lobby) && HasAvailableSlots(lobby);
        }

        private bool IsLobbyStarted(Lobby lobby)
        {
            if (lobby == null || lobby.Data == null)
                return false;

            if (!lobby.Data.TryGetValue(StartedKey, out var startedData) || startedData == null)
                return false;

            return string.Equals(startedData.Value, "1", StringComparison.Ordinal);
        }

        private bool HasAvailableSlots(Lobby lobby)
        {
            if (lobby == null)
                return false;

            // AvailableSlots có thể không đồng bộ ở vài thời điểm, fallback bằng Players/MaxPlayers.
            if (lobby.AvailableSlots > 0)
                return true;

            int currentPlayers = lobby.Players != null ? lobby.Players.Count : 0;
            return currentPlayers < lobby.MaxPlayers;
        }

        private string ResolveLobbyPlayerName(Player lobbyPlayer, int index)
        {
            if (lobbyPlayer == null)
                return $"Người chơi {index + 1}";

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

            if (!string.IsNullOrWhiteSpace(lobbyPlayer.Id) && authManager != null && AuthenticationService.Instance != null && string.Equals(lobbyPlayer.Id, AuthenticationService.Instance.PlayerId, StringComparison.OrdinalIgnoreCase))
            {
                if (!displayName.EndsWith("(bạn)", StringComparison.OrdinalIgnoreCase))
                {
                    displayName = $"{displayName} (bạn)";
                }
            }

            return displayName;
        }

        private void ApplyCompactListLayout()
        {
            if (contentRoot == null)
                return;

            var vertical = contentRoot.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                vertical.spacing = 6f;
                vertical.padding.left = 4;
                vertical.padding.right = 4;
                vertical.padding.top = 6;
                vertical.padding.bottom = 6;
                vertical.childControlHeight = true;
                vertical.childControlWidth = true;
                vertical.childForceExpandHeight = false;
                vertical.childForceExpandWidth = false;
            }
        }

        private static string ShortenWithEllipsis(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (maxLength < 4 || value.Length <= maxLength)
                return value;

            return value.Substring(0, maxLength - 1) + "…";
        }

        private static string CompactIdentity(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string clean = value.Trim();
            if (clean.Length <= maxLength)
                return clean;

            bool looksLikeId = true;
            for (int i = 0; i < clean.Length; i++)
            {
                char c = clean[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                {
                    looksLikeId = false;
                    break;
                }
            }

            if (looksLikeId && clean.Length >= 12)
            {
                return clean.Substring(0, 6) + "…" + clean.Substring(clean.Length - 4);
            }

            return ShortenWithEllipsis(clean, maxLength);
        }

        private async Task JoinLobbyAsync(Lobby lobby)
        {
            if (lobby == null || isBusy)
                return;

            isBusy = true;
            SetStatus($"Đang vào phòng '{BuildLobbyName(lobby)}'...");

            try
            {
                ResolveRoomController();
                if (roomController == null)
                {
                    SetStatus("Thiếu UIMultiplayerRoomController trong scene.");
                    return;
                }

                bool joined = await roomController.JoinLobbyFromBrowserAsync(lobby);
                if (joined)
                {
                    SetStatus("Đã vào phòng.");
                    roomController.NotifyEnteredFromBrowser();
                    if (roomNavigator != null)
                    {
                        roomNavigator.NavigateNow();
                    }
                }
                else
                {
                    SetStatus("Không vào được phòng.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LobbyBrowser] Join lỗi: {ex.Message}");
                SetStatus("Không vào được phòng.");
            }
            finally
            {
                isBusy = false;
            }
        }

        private async Task EnsureMultiplayerReady()
        {
            if (RelayManager.Instance == null)
            {
                throw new InvalidOperationException("Thiếu RelayManager trong scene.");
            }

            if (!await RelayManager.Instance.EnsureServicesReady())
            {
                throw new InvalidOperationException("Không kết nối được dịch vụ multiplayer.");
            }

            if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
            {
                throw new InvalidOperationException("Chưa đăng nhập dịch vụ multiplayer.");
            }
        }

        private void ClearEntries()
        {
            for (int i = 0; i < spawnedEntries.Count; i++)
            {
                if (spawnedEntries[i] != null)
                {
                    Destroy(spawnedEntries[i].gameObject);
                }
            }

            spawnedEntries.Clear();
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.SetText(message);
            }

            Debug.Log($"[LobbyBrowser] {message}");
            MultiplayerDetailedLogger.Trace("UI_LOBBY_BROWSER_STATUS", message);
        }
    }
}