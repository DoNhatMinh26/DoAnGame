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
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 15: Multiplayer Room - tạo phòng, quick join, nhập code join và start khi đủ 2 người.
    /// </summary>
    public class UIMultiplayerRoomController : MonoBehaviour
    {
        private const string JoinCodeKey = "JoinCode";
        private const string StartedKey = "Started";
        private const string ModeKey = "Mode";
        private const int MaxPlayers = 2;

        [Header("Room Buttons")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button quickJoinButton;
        [SerializeField] private Button joinByCodeButton;
        [SerializeField] private Button startMatchButton;
        [SerializeField] private Button quitRoomButton;

        [Header("Room Inputs")]
        [SerializeField] private TMP_InputField roomCodeInput;

        [Header("Room Texts")]
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text playerCountText;

        [Header("Chuyển Cảnh Khi Game Bắt Đầu")]
        [LocalizedLabel("Tên Scene Battle (Nếu khác Scene)")]
        [SerializeField] private string battleSceneName;
        [LocalizedLabel("Màn hình Battle (Màn hình đích)")]
        [SerializeField] private GameObject battleScreenObject;
        [LocalizedLabel("Root chứa UI phòng hiện tại (Để tắt đi)")]
        [SerializeField] private GameObject currentRoomScreen;

        private Lobby currentLobby;
        private bool isHost;
        private Coroutine pollingRoutine;
        private Coroutine heartbeatRoutine;

        private void Awake()
        {
            createRoomButton?.onClick.AddListener(() => _ = HandleCreateRoom());
            quickJoinButton?.onClick.AddListener(() => _ = HandleQuickJoin());
            joinByCodeButton?.onClick.AddListener(() => _ = HandleJoinByCode());
            startMatchButton?.onClick.AddListener(() => _ = HandleStartMatch());
            quitRoomButton?.onClick.AddListener(() => _ = HandleQuitRoom());
        }

        private void OnEnable()
        {
            SetStatus("Chọn tạo phòng, vào nhanh hoặc nhập mã phòng.");
            SetPlayerCount("Người chơi: 1/2");
            lobbyCodeText?.SetText("Mã phòng: -----");
            startMatchButton?.gameObject.SetActive(false);

            StopRoutines();
            pollingRoutine = StartCoroutine(PollLobbyRoutine());
        }

        private void OnDisable()
        {
            StopRoutines();
        }

        private void OnDestroy()
        {
            StopRoutines();
            createRoomButton?.onClick.RemoveAllListeners();
            quickJoinButton?.onClick.RemoveAllListeners();
            joinByCodeButton?.onClick.RemoveAllListeners();
            startMatchButton?.onClick.RemoveAllListeners();
            quitRoomButton?.onClick.RemoveAllListeners();
        }

        private async Task HandleCreateRoom()
        {
            if (!await EnsureRelayManager()) return;

            SetStatus("Đang tạo phòng...");
            string relayJoinCode = await RelayManager.Instance.CreateRelay(MaxPlayers);
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                SetStatus("Tạo phòng thất bại.");
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
        }

        private async Task HandleQuickJoin()
        {
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
        }

        private async Task HandleJoinByCode()
        {
            if (!await EnsureRelayManager()) return;

            string lobbyCode = roomCodeInput?.text?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(lobbyCode))
            {
                SetStatus("Vui lòng nhập mã phòng.");
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
                SetStatus("Không vào được phòng. Kiểm tra lại mã.");
                Debug.LogWarning($"[UIRoom] JoinByCode lỗi: {ex.Message}");
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
            if (!isHost || currentLobby == null)
            {
                SetStatus("Chỉ chủ phòng mới được bắt đầu.");
                return;
            }

            var lobby = await RefreshLobbySafe();
            if (lobby == null)
            {
                SetStatus("Không đọc được lobby để bắt đầu.");
                return;
            }

            if (lobby.Players.Count < MaxPlayers)
            {
                SetStatus("Chưa đủ 2 người chơi.");
                startMatchButton.interactable = false;
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
                GoToBattleScreen();
            }
            catch (Exception ex)
            {
                SetStatus("Không thể bắt đầu trận.");
                Debug.LogError($"[UIRoom] StartMatch lỗi: {ex.Message}");
            }
        }

        private IEnumerator PollLobbyRoutine()
        {
            while (true)
            {
                _ = PollLobbyOnce();
                yield return new WaitForSeconds(2f);
            }
        }

        private async Task PollLobbyOnce()
        {
            if (currentLobby == null) return;

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
                GoToBattleScreen();
            }
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
            try
            {
                return await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UIRoom] Refresh lobby lỗi: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> EnsureRelayManager()
        {
            if (RelayManager.Instance == null)
            {
                SetStatus("Thiếu RelayManager trong scene.");
                Debug.LogError("[UIRoom] RelayManager.Instance == null");
                return false;
            }

            if (!await RelayManager.Instance.EnsureServicesReady())
            {
                SetStatus("Không kết nối được dịch vụ multiplayer.");
                return false;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                SetStatus("Chưa đăng nhập dịch vụ multiplayer.");
                return false;
            }

            return true;
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
        }

        private void SetStatus(string text)
        {
            statusText?.SetText(text);
            Debug.Log($"[UIRoom] {text}");
        }

        private void SetPlayerCount(string text)
        {
            playerCountText?.SetText(text);
        }

        private void GoToBattleScreen()
        {
            if (!string.IsNullOrEmpty(battleSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
                return;
            }

            if (battleScreenObject != null)
            {
                // Ẩn phòng chờ, hiện phòng đánh
                if (currentRoomScreen != null) currentRoomScreen.SetActive(false);
                battleScreenObject.SetActive(true);
            }
        }

        private async Task HandleQuitRoom()
        {
            if (currentLobby == null)
            {
                SetStatus("Chưa vào phòng nào.");
                return;
            }

            try
            {
                SetStatus("Đang thoát phòng...");

                // Thoát lobby
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                Debug.Log("[UIRoom] Đã thoát lobby thành công.");

                // Shutdown network
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                    Debug.Log("[UIRoom] Đã shutdown network.");
                }

                // Reset trạng thái
                currentLobby = null;
                isHost = false;
                StopRoutines();

                // Reset UI
                lobbyCodeText?.SetText("Mã phòng: -----");
                SetPlayerCount("Người chơi: 0/2");
                SetStatus("Đã thoát phòng. Chọn một tùy chọn khác.");
                startMatchButton?.gameObject.SetActive(false);
                if (roomCodeInput != null) roomCodeInput.text = "";
            }
            catch (Exception ex)
            {
                SetStatus($"Lỗi thoát phòng: {ex.Message}");
                Debug.LogError($"[UIRoom] HandleQuitRoom lỗi: {ex.Message}");
            }
        }
    }
}
