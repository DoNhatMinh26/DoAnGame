using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using System.Reflection; // Bắt buộc thêm dòng này để dùng BindingFlags
using System.Text;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private bool servicesReady;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Debug.LogWarning("[Relay] Duplicate RelayManager detected, disabling duplicate component.");
            MultiplayerDetailedLogger.TraceWarning("RELAY", "Duplicate RelayManager detected, disabling duplicate component");
            enabled = false;
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        MultiplayerDetailedLogger.Initialize();
        MultiplayerDetailedLogger.TraceNetworkSnapshot("RELAY", "RelayManager Awake");
    }

    async void Start() {
        await EnsureServicesReady();
    }

    public async Task<bool> EnsureServicesReady()
    {
        if (servicesReady)
            return true;

        try {
            MultiplayerDetailedLogger.Trace("RELAY", "EnsureServicesReady begin");
            var initOptions = new InitializationOptions();
            string profile = BuildProfileName();
            initOptions.SetProfile(profile);

            await UnityServices.InitializeAsync(initOptions);
            if (!AuthenticationService.Instance.IsSignedIn) {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[Relay] Đã đăng nhập ẩn danh thành công! Profile={profile}, PlayerId={AuthenticationService.Instance.PlayerId}");
            }
            else
            {
                Debug.Log($"[Relay] Đang dùng phiên đăng nhập sẵn. Profile={profile}, PlayerId={AuthenticationService.Instance.PlayerId}");
            }
            servicesReady = true;
            MultiplayerDetailedLogger.Trace("RELAY", $"EnsureServicesReady done, profile={profile}, playerId={AuthenticationService.Instance.PlayerId}");
            return true;
        } catch (System.Exception e) {
            Debug.LogError("[Relay] Lỗi khởi tạo: " + e.Message);
            MultiplayerDetailedLogger.TraceException("RELAY", e, "EnsureServicesReady failed");
            return false;
        }
    }

    private string BuildProfileName()
    {
        // Profile tách theo project path để main và clone không dùng chung PlayerId.
        // Unity profile name chỉ nên gồm ký tự an toàn.
        string source = Application.dataPath;
        if (string.IsNullOrEmpty(source))
            return "default_profile";

        int hash = source.GetHashCode();
        string raw = $"profile_{Mathf.Abs(hash)}";

        var sb = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                sb.Append(c);
            }
        }

        return sb.Length > 0 ? sb.ToString() : "default_profile";
    }

    public async Task<string> CreateRelay() {
        return await CreateRelay(2);
    }

    public async Task<string> CreateRelay(int maxPlayers) {
        if (!await EnsureServicesReady())
            return null;

        try {
            MultiplayerDetailedLogger.Trace("RELAY", $"CreateRelay begin, maxPlayers={maxPlayers}");
            Debug.Log("[Relay] Đang tạo Allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (!ConfigureRelayTransport(new RelayServerData(allocation, "dtls")))
            {
                return null;
            }

            NetworkManager.Singleton.StartHost();
            Debug.Log("[Relay] Host đã chạy. Mã: " + joinCode);
            MultiplayerDetailedLogger.TraceNetworkSnapshot("RELAY", $"CreateRelay success, joinCode={joinCode}");
            return joinCode;
        } catch (System.Exception e) {
            Debug.LogError("[Relay] Lỗi CreateRelay: " + e.Message);
            MultiplayerDetailedLogger.TraceException("RELAY", e, "CreateRelay failed");
            return null;
        }
    }

    public async Task JoinRelay(string joinCode) {
        await TryJoinRelay(joinCode);
    }

    public async Task<bool> TryJoinRelay(string joinCode)
    {
        if (!await EnsureServicesReady())
            return false;

        try {
            MultiplayerDetailedLogger.Trace("RELAY", $"TryJoinRelay begin, joinCode={joinCode}");
            Debug.Log("[Relay] Đang tham gia phòng...");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            if (!ConfigureRelayTransport(new RelayServerData(joinAllocation, "dtls")))
            {
                return false;
            }

            NetworkManager.Singleton.StartClient();
            Debug.Log("[Relay] Client đang kết nối...");
            MultiplayerDetailedLogger.TraceNetworkSnapshot("RELAY", $"TryJoinRelay success, joinCode={joinCode}");
            return true;
        } catch (System.Exception e) {
            Debug.LogError("[Relay] Lỗi JoinRelay: " + e.Message);
            MultiplayerDetailedLogger.TraceException("RELAY", e, $"TryJoinRelay failed, joinCode={joinCode}");
            return false;
        }
    }

    public void Disconnect()
    {
        MultiplayerDetailedLogger.TraceNetworkSnapshot("RELAY", "Disconnect requested");
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log("[Relay] Đang ngắt kết nối Relay...");
            NetworkManager.Singleton.Shutdown();
            MultiplayerDetailedLogger.TraceNetworkSnapshot("RELAY", "Disconnect completed via NetworkManager.Shutdown");
        }
    }

    private bool ConfigureRelayTransport(RelayServerData relayServerData)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[Relay] Không tìm thấy NetworkManager.Singleton");
            return false;
        }

        var transport = networkManager.NetworkConfig.NetworkTransport;
        MethodInfo method = transport.GetType().GetMethod("SetRelayServerData", new System.Type[] { typeof(RelayServerData) });

        if (method == null)
        {
            Debug.LogError("[Relay] Không tìm thấy hàm SetRelayServerData phù hợp!");
            return false;
        }

        method.Invoke(transport, new object[] { relayServerData });
        Debug.Log("[Relay] Đã cấu hình Transport thành công.");
        return true;
    }
}