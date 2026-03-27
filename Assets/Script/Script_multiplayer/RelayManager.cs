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

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start() {
        try {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[Relay] Đã đăng nhập ẩn danh thành công!");
            }
        } catch (System.Exception e) {
            Debug.LogError("[Relay] Lỗi khởi tạo: " + e.Message);
        }
    }

    public async Task<string> CreateRelay() {
        try {
            Debug.Log("[Relay] Đang tạo Allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            var relayServerData = new RelayServerData(allocation, "dtls");

            // SỬA LỖI AMBIGUOUS MATCH TẠI ĐÂY:
            // Chúng ta tìm chính xác hàm nhận vào 1 tham số duy nhất là RelayServerData
            MethodInfo method = transport.GetType().GetMethod("SetRelayServerData", new System.Type[] { typeof(RelayServerData) });
            
            if (method != null) {
                method.Invoke(transport, new object[] { relayServerData });
                Debug.Log("[Relay] Đã cấu hình Transport thành công.");
            } else {
                Debug.LogError("[Relay] Không tìm thấy hàm SetRelayServerData phù hợp!");
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
        try {
            Debug.Log("[Relay] Đang tham gia phòng...");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            var relayServerData = new RelayServerData(joinAllocation, "dtls");

            // SỬA LỖI AMBIGUOUS MATCH TẠI ĐÂY:
            MethodInfo method = transport.GetType().GetMethod("SetRelayServerData", new System.Type[] { typeof(RelayServerData) });
            
            if (method != null) {
                method.Invoke(transport, new object[] { relayServerData });
            }

            NetworkManager.Singleton.StartClient();
            Debug.Log("[Relay] Client đang kết nối...");
        } catch (System.Exception e) {
            Debug.LogError("[Relay] Lỗi JoinRelay: " + e.Message);
        }
    }
}