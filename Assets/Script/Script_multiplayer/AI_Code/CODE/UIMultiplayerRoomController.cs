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

        [Header("Room Buttons")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button quickJoinButton;
        [SerializeField] private Button joinByCodeButton;
        [SerializeField] private Button startMatchButton;

        [Header("Room Inputs")]
        [SerializeField] private TMP_InputField roomCodeInput;

        [Header("Room Texts")]
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text playerCountText;

        [Header("Callbacks")]
        [SerializeField] private UIButtonScreenNavigator startBattleNavigator;
        [SerializeField] private UnityEvent onBattleStarted;

        private Lobby currentLobby;
        private bool isHost;
        private Coroutine pollingRoutine;
        private Coroutine heartbeatRoutine;
        private bool battleStartNotified;
        private bool initialized;
        private bool isBusy;

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
            SetStatus("Chọn tạo phòng, vào nhanh hoặc nhập mã phòng.");
            SetPlayerCount("Người chơi: 1/2");
            lobbyCodeText?.SetText("Mã phòng: -----");
            startMatchButton?.gameObject.SetActive(false);
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
            if (isBusy)
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

            var lobby = await RefreshLobbySafe();
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
                NotifyBattleStarted();
            }
        }

        private void NotifyBattleStarted()
        {
            if (battleStartNotified)
                return;

            battleStartNotified = true;

            if (startBattleNavigator != null)
            {
                startBattleNavigator.NavigateNow();
            }

            onBattleStarted?.Invoke();
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

        private void SetActionButtonsInteractable(bool interactable)
        {
            if (createRoomButton != null) createRoomButton.interactable = interactable;
            if (quickJoinButton != null) quickJoinButton.interactable = interactable;
            if (joinByCodeButton != null) joinByCodeButton.interactable = interactable;

            if (startMatchButton != null)
            {
                // Start chỉ có ý nghĩa với host, trạng thái đủ người sẽ được cập nhật trong poll/create.
                startMatchButton.interactable = interactable && isHost;
            }
        }
    }
}
