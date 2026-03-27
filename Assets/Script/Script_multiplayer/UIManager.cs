using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro; // Sử dụng TextMeshPro

public class UIManager : NetworkBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Các Bảng Giao Diện (Panels)")]
    public GameObject lobbyPanel;      
    public GameObject gameplayPanel;   

    [Header("Thành phần UI ở Sảnh (Lobby)")]
    public TMP_Text statusText;            
    public TMP_Text codeDisplayText;       
    public Button startButton;             
    public TMP_InputField joinInputField;  

    // Biến mạng đồng bộ trạng thái bắt đầu game
    private NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake() => Instance = this;

    void Start() {
        Debug.Log("[UI] Script UIManager đã khởi động.");
        
        // Cấu hình ban đầu cho Panel
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);

        // Lắng nghe sự kiện chuyển màn hình khi biến isGameStarted thay đổi
        isGameStarted.OnValueChanged += (oldVal, newVal) => {
            if (newVal == true) SwitchToGameplay();
        };
    }

    void Update() {
        // Cập nhật số người chơi hiện có trong phòng
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer) {
            int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            statusText.text = "Người chơi trong phòng: " + playerCount + "/2";

            if (IsServer) {
                // Chỉ Host mới bật được nút Start khi đủ từ 2 người
                startButton.interactable = (playerCount >= 2);
            } else {
                startButton.interactable = false;
            }
        }
    }

    // Gán vào nút Tạo Phòng (Host)
    public async void OnClickHost() {
        Debug.Log("[UI] Đang nhấn nút HOST...");
        if (RelayManager.Instance == null) return;

        string code = await RelayManager.Instance.CreateRelay();
        if (code != null) {
            codeDisplayText.text = "Mã phòng: " + code;
        }
    }

    // Gán vào nút Vào Phòng (Join)
    public async void OnClickJoin() {
        Debug.Log("[UI] Đang nhấn nút JOIN...");
        if (string.IsNullOrEmpty(joinInputField.text)) {
            Debug.LogWarning("[UI] Hãy nhập mã phòng!");
            return;
        }
        
        if (RelayManager.Instance != null) {
            await RelayManager.Instance.JoinRelay(joinInputField.text);
        }
    }

    // Gán vào nút Bắt Đầu (Start)
    public void OnClickStart() {
        Debug.Log("[UI] Host nhấn BẮT ĐẦU GAME.");
        if (IsServer) {
            isGameStarted.Value = true; // Kích hoạt biến mạng để tất cả máy cùng chuyển Panel
        }
    }

    private void SwitchToGameplay() {
        lobbyPanel.SetActive(false);
        gameplayPanel.SetActive(true);
        Debug.Log("[UI] Đã chuyển sang màn hình chơi game!");
    }
}