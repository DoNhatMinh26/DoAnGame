using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UIManager: Quản lý tất cả UI từ Welcome → GamePlay
/// UI 1: Welcome Screen (Intro)
/// UI 2: Welcome/Auth (Chọn: Đăng ký/Đăng nhập/Chơi nhanh)
/// UI 3: Login Panel
/// UI 4: Register Panel
/// UI 5: Main Menu (HUB)
/// </summary>
public class UIManager : MonoBehaviour
{
    public static int SelectedGrade = 1;

    public static UIManager Instance { get; private set; }

    [Header("════ UI PANELS ════")]
    public GameObject welcomeScreenPanel;    // UI 1
    public GameObject authChoicePanel;       // UI 2
    public GameObject loginPanel;            // UI 3
    public GameObject registerPanel;         // UI 4
    public GameObject mainMenuPanel;         // UI 5
    public GameObject lobbyPanel;            // Multiplayer (Room)
    public GameObject gameplayPanel;         // Chơi game

    [Header("════ UI 2: AUTH CHOICE ════")]
    public Button registrationBtn;
    public Button loginBtn;
    public Button quickPlayBtn;

    [Header("════ UI 3: LOGIN ════")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginSubmitBtn;
    public Button loginBackBtn;
    public TMP_Text loginErrorText;

    [Header("════ UI 4: REGISTER ════")]
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerUsernameInput;
    // registerAgeInput đã xóa — thay bằng gradeDropdown trong UIRegisterPanelController
    public Button registerSubmitBtn;
    public Button registerBackBtn;
    public TMP_Text registerErrorText;

    [Header("════ UI 5: MAIN MENU ════")]
    public Button multiplayerBtn;
    public Button singlePlayerBtn;
    public Button leaderboardBtn;
    public Button profileBtn;
    public Button settingsBtn;
    public Button logoutBtn;
    public TMP_Text welcomeText;

    [Header("════ MULTIPLAYER LOBBY ════")]
    public TMP_Text statusText;
    public TMP_Text codeDisplayText;
    public Button startButton;
    public TMP_InputField joinInputField;
    public Button hostBtn;
    public Button joinBtn;

    // Managers
    private AuthManager authManager;
    private RelayManager relayManager;

    private bool isBirthYearSelected = false;
    private string selectedBirthYear = string.Empty;    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[UIManager] Duplicate UIManager detected, disabling duplicate component.");
            enabled = false;
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Debug.Log("[UIManager] ✅ Starting...");

        authManager = AuthManager.Instance;
        relayManager = RelayManager.Instance;

        // Bind buttons
        BindButtons();

        // Nút Play trên WELCOMESCREEN luôn enabled (grade chọn ở NhapTen_choiNhanh)
        UpdatePlayButtonState();

        // DISABLED: UIStartupController handles initial panel display
        Debug.Log("[UIManager] Initial panel display handled by UIStartupController");
    }

    void Update()
    {
        // Update() giữ lại để tránh lỗi nếu có code khác gọi
    }

    private void SetPanelActiveSafe(GameObject panel, bool active, string panelName)
    {
        if (panel == null)
        {
            Debug.LogWarning($"[UIManager] ⚠️ Panel '{panelName}' chưa được gán hoặc đã bị destroy.");
            return;
        }

        panel.SetActive(active);
    }

    /// <summary>
    /// Liên kết tất cả nút bấm
    /// </summary>
    private void BindButtons()
    {
        // UI 2 - Auth Choice
        if (registrationBtn != null)
            registrationBtn.onClick.AddListener(() => ShowUI(3));
        if (loginBtn != null)
            loginBtn.onClick.AddListener(() => ShowUI(2));
        if (quickPlayBtn != null)
            quickPlayBtn.onClick.AddListener(OnQuickPlayClick);

        // UI 3 - Login
        if (loginBackBtn != null)
            loginBackBtn.onClick.AddListener(() => ShowUI(1));
        if (loginSubmitBtn != null)
            loginSubmitBtn.onClick.AddListener(OnLoginClick);

        // UI 4 - Register
        if (registerBackBtn != null)
            registerBackBtn.onClick.AddListener(() => ShowUI(1));
        if (registerSubmitBtn != null)
            registerSubmitBtn.onClick.AddListener(OnRegisterClick);

        // UI 5 - Main Menu
        if (multiplayerBtn != null)
            multiplayerBtn.onClick.AddListener(OnMultiplayerClick);
        if (singlePlayerBtn != null)
            singlePlayerBtn.onClick.AddListener(OnSinglePlayerClick);
        if (logoutBtn != null)
            logoutBtn.onClick.AddListener(OnLogoutClick);

        // Multiplayer Lobby (legacy)
        if (hostBtn != null)
            hostBtn.onClick.AddListener(OnHostClick);
        if (joinBtn != null)
            joinBtn.onClick.AddListener(OnJoinClick);

        Debug.Log("[UIManager] ✅ Tất cả nút đã được liên kết");
    }

    /// <summary>
    /// Hiển thị Panel
    /// </summary>
    private void ShowUI(int uiIndex)
    {
        // Ẩn tất cả
        SetPanelActiveSafe(welcomeScreenPanel, false, nameof(welcomeScreenPanel));
        SetPanelActiveSafe(authChoicePanel, false, nameof(authChoicePanel));
        SetPanelActiveSafe(loginPanel, false, nameof(loginPanel));
        SetPanelActiveSafe(registerPanel, false, nameof(registerPanel));
        SetPanelActiveSafe(mainMenuPanel, false, nameof(mainMenuPanel));
        SetPanelActiveSafe(lobbyPanel, false, nameof(lobbyPanel));
        SetPanelActiveSafe(gameplayPanel, false, nameof(gameplayPanel));

        // Hiện UI được chọn
        switch (uiIndex)
        {
            case 0:
                SetPanelActiveSafe(welcomeScreenPanel, true, nameof(welcomeScreenPanel));
                UpdatePlayButtonState();
                Debug.Log("[UIManager] 📱 UI 0: Welcome Screen");
                break;
            case 1:
                SetPanelActiveSafe(authChoicePanel, true, nameof(authChoicePanel));
                Debug.Log("[UIManager] 📱 UI 1: Auth Choice");
                break;
            case 2:
                SetPanelActiveSafe(loginPanel, true, nameof(loginPanel));
                Debug.Log("[UIManager] 📱 UI 2: Login");
                break;
            case 3:
                SetPanelActiveSafe(registerPanel, true, nameof(registerPanel));
                Debug.Log("[UIManager] 📱 UI 3: Register");
                break;
            case 4:
                SetPanelActiveSafe(mainMenuPanel, true, nameof(mainMenuPanel));
                if (welcomeText != null)
                    welcomeText.text = $"Chào {authManager.GetCurrentPlayerData()?.characterName ?? "Khách"}! 👋";
                Debug.Log("[UIManager] 📱 UI 4: Main Menu");
                break;
            case 5:
                SetPanelActiveSafe(lobbyPanel, true, nameof(lobbyPanel));
                Debug.Log("[UIManager] 📱 UI 5: Multiplayer Lobby");
                break;
        }
    }

    private void UpdatePlayButtonState()
    {
        // Nút Play luôn enabled — grade được chọn ở NhapTen_choiNhanh
        // (playButton đã bị xóa khỏi WELCOMESCREEN, không cần làm gì)
    }

    // InitializeBirthYearDropdown và OnBirthYearSelectionChanged đã được xóa.
    // Grade selection chuyển sang UIQuickPlayNameController (NhapTen_choiNhanh panel).

    /// <summary>
    /// UI 2: Chơi nhanh (ẩn danh)
    /// </summary>
    private async void OnQuickPlayClick()
    {
        Debug.Log("[UIManager] 👤 Chơi nhanh...");
        bool success = await authManager.QuickPlay();
        if (success)
        {
            ShowUI(4); // Main Menu
        }
        else
        {
            Debug.LogError("[UIManager] ❌ Chơi nhanh thất bại");
        }
    }

    /// <summary>
    /// UI 3: Đăng nhập
    /// </summary>
    private async void OnLoginClick()
    {
        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;

        Debug.Log($"[UIManager] 🔑 Login: {email}");

        bool success = await authManager.Login(email, password);

        if (success)
        {
            loginErrorText.text = "✅ Đăng nhập thành công!";
            await Task.Delay(1000);
            ShowUI(4); // Main Menu
        }
        else
        {
            loginErrorText.text = "❌ Email hoặc mật khẩu sai!";
        }
    }

    /// <summary>
    /// UI 4: Đăng ký — legacy, không còn dùng (UIRegisterPanelController xử lý thay)
    /// </summary>
    private async void OnRegisterClick()
    {
        string email = registerEmailInput != null ? registerEmailInput.text : "";
        string password = registerPasswordInput != null ? registerPasswordInput.text : "";
        string username = registerUsernameInput != null ? registerUsernameInput.text : "";
        int grade = 1; // default — UIRegisterPanelController tự lấy từ gradeDropdown

        Debug.Log($"[UIManager] 📝 Register (legacy): {username}");

        bool success = await authManager.Register(email, password, username, grade);

        if (success)
        {
            registerErrorText.text = "✅ Đăng ký thành công!";
            await Task.Delay(1000);
            ShowUI(2); // Login
        }
        else
        {
            registerErrorText.text = "❌ Lỗi đăng ký! Email có thể đã tồn tại.";
        }
    }

    /// <summary>
    /// UI 5: Multiplayer
    /// </summary>
    private void OnMultiplayerClick()
    {
        Debug.Log("[UIManager] 🎮 Chế độ Multiplayer");
        ShowUI(5); // Lobby
    }

    /// <summary>
    /// UI 5: Single Player
    /// </summary>
    private void OnSinglePlayerClick()
    {
        Debug.Log("[UIManager] 🎮 Chế độ Single Player");
        // TODO: Load game scene
    }

    /// <summary>
    /// UI 5: Đăng xuất
    /// </summary>
    private void OnLogoutClick()
    {
        authManager.Logout();
        ShowUI(0); // Welcome Screen
    }

    /// <summary>
    /// Multiplayer: Host — legacy, dùng RelayManager trực tiếp thay thế
    /// </summary>
    private async void OnHostClick()
    {
        Debug.Log("[UIManager] 🏠 Host...");
        if (relayManager == null) return;

        string code = await relayManager.CreateRelay();
        if (code != null && codeDisplayText != null)
        {
            codeDisplayText.text = $"Mã phòng: {code}";
        }
    }

    /// <summary>
    /// Multiplayer: Join — legacy
    /// </summary>
    private async void OnJoinClick()
    {
        string code = joinInputField != null ? joinInputField.text : "";
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[UIManager] ⚠️ Nhập mã phòng!");
            return;
        }

        Debug.Log($"[UIManager] 📡 Join: {code}");
        if (relayManager != null)
        {
            await relayManager.JoinRelay(code);
        }
    }

    /// <summary>
    /// RequestNetworkGameStart — không còn dùng NetworkVariable, giữ lại để tương thích
    /// </summary>
    public bool RequestNetworkGameStart()
    {
        Debug.LogWarning("[UIManager] RequestNetworkGameStart: UIManager không còn là NetworkBehaviour. Dùng UIMultiplayerRoomController để start game.");
        return false;
    }
    
}