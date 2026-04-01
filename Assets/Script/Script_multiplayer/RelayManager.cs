using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using System.Reflection; // Bắt buộc thêm dòng này để dùng BindingFlags

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private bool servicesReady;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start() {
        await EnsureServicesReady();
    }

    public async Task<bool> EnsureServicesReady()
    {
        if (servicesReady)
            return true;

        try {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[Relay] Đã đăng nhập ẩn danh thành công!");
            }
            servicesReady = true;
            return true;
        } catch (System.Exception e) {
            Debug.LogError("[Relay] Lỗi khởi tạo: " + e.Message);
            return false;
        }
    }

    public async Task<string> CreateRelay() {
        return await CreateRelay(2);
    }

    public async Task<string> CreateRelay(int maxPlayers) {
        if (!await EnsureServicesReady())
            return null;

        try {
            Debug.Log("[Relay] Đang tạo Allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (!ConfigureRelayTransport(new RelayServerData(allocation, "dtls")))
            {
                return null;
            }

            NetworkManager.Singleton.StartHost();
            Debug.Log("[Relay] Host đã chạy. Mã: " + joinCode);
            return joinCode;
        } catch (System.Exception e) {
            Debug.LogError("[Relay] Lỗi CreateRelay: " + e.Message);
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
            Debug.Log("[Relay] Đang tham gia phòng...");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            if (!ConfigureRelayTransport(new RelayServerData(joinAllocation, "dtls")))
            {
                return false;
            }

            NetworkManager.Singleton.StartClient();
            Debug.Log("[Relay] Client đang kết nối...");
            return true;
        } catch (System.Exception e) {
            Debug.LogError("[Relay] Lỗi JoinRelay: " + e.Message);
            return false;
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