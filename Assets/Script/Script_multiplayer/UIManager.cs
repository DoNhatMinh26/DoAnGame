using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
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
public class UIManager : NetworkBehaviour
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

    [Header("════ UI 1: WELCOME SCREEN ════")]
    public Button playButton;
    public TMP_Dropdown birthYearDropdown;

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
    public TMP_InputField registerAgeInput;
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

    // Network variable
    private NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    // Managers
    private AuthManager authManager;
    private RelayManager relayManager;

    private bool isBirthYearSelected = false;
    private string selectedBirthYear = string.Empty;

    private void Awake()
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

    async void Start()
    {
        Debug.Log("[UIManager] ✅ Starting...");
        MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_MANAGER", "Start called");

        authManager = AuthManager.Instance;
        relayManager = RelayManager.Instance;

        // Bind buttons
        BindButtons();

        // Initialize birth year choices and Play button state
        InitializeBirthYearDropdown();

        // DISABLED: UIStartupController handles initial panel display
        // ShowUI(0);
        Debug.Log("[UIManager] Initial panel display handled by UIStartupController");

        // Network
        isGameStarted.OnValueChanged += (oldVal, newVal) =>
        {
            MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_MANAGER", $"isGameStarted changed: {oldVal} -> {newVal}");
            if (newVal) SwitchToGameplay();
        };

        await Task.CompletedTask;
    }

    void Update()
    {
        // Cập nhật số người chơi (nếu ở trong Multiplayer Lobby)
        if (lobbyPanel != null && lobbyPanel.activeSelf && NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (statusText != null)
            {
                statusText.text = $"Người chơi: {playerCount}/2";
            }

            if (startButton != null)
            {
                startButton.interactable = (IsServer && playerCount >= 2);
            }
        }
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
        // UI 1 - Welcome Screen
        if (playButton != null)
            playButton.onClick.AddListener(() => ShowUI(1));

        if (birthYearDropdown != null)
            birthYearDropdown.onValueChanged.AddListener(OnBirthYearSelectionChanged);

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

        // Multiplayer Lobby
        if (hostBtn != null)
            hostBtn.onClick.AddListener(OnHostClick);
        if (joinBtn != null)
            joinBtn.onClick.AddListener(OnJoinClick);
        if (startButton != null)
            startButton.onClick.AddListener(OnGameStart);

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

    private void InitializeBirthYearDropdown()
    {
        if (birthYearDropdown == null)
            return;

        birthYearDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>
        {
            "Chọn năm sinh",
            "2019 - Mẫu giáo",
            "2018 - Lớp 1",
            "2017 - Lớp 2",
            "2016 - Lớp 3",
            "2015 - Lớp 4",
            "2014 - Lớp 5"
        };

        birthYearDropdown.AddOptions(options);
        birthYearDropdown.value = 0;
        birthYearDropdown.RefreshShownValue();
        isBirthYearSelected = false;
        selectedBirthYear = string.Empty;
        UpdatePlayButtonState();
    }

    private void OnBirthYearSelectionChanged(int index)
    {
        if (birthYearDropdown == null)
            return;

        isBirthYearSelected = index > 0;

        // --- LOGIC MỚI: Cập nhật SelectedGrade dựa trên Index ---
        // Index 1: Mẫu giáo -> Lớp 1 (Grade 1)
        // Index 2: Lớp 1 -> Lớp 1 (Grade 1)
        // Index 3: Lớp 2 -> Lớp 2 (Grade 2)...
        if (index == 1 || index == 2) SelectedGrade = 1;
        else if (index > 2) SelectedGrade = index - 1;
        else SelectedGrade = 1; // Mặc định là lớp 1 nếu chưa chọn

        selectedBirthYear = isBirthYearSelected ? birthYearDropdown.options[index].text : string.Empty;

        // Lưu grade đã chọn vào PlayerPrefs (cho auto-skip)
        if (isBirthYearSelected)
        {
            DoAnGame.UI.UIQuickPlayNameController.SaveSelectedGrade(SelectedGrade);
        }

        Debug.Log($"[UIManager] Năm sinh: {selectedBirthYear} -> GradeIndex đã lưu: {SelectedGrade}");

        UpdatePlayButtonState();
    }

    private void UpdatePlayButtonState()
    {
        if (playButton == null)
            return;

        if (birthYearDropdown == null)
        {
            playButton.interactable = true;
            return;
        }

        playButton.interactable = isBirthYearSelected;
    }

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
    /// UI 4: Đăng ký
    /// </summary>
    private async void OnRegisterClick()
    {
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;
        string username = registerUsernameInput.text;
        int age = int.TryParse(registerAgeInput.text, out int a) ? a : 0;

        Debug.Log($"[UIManager] 📝 Register: {username}");

        bool success = await authManager.Register(email, password, username, age);

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
    /// Multiplayer: Host
    /// </summary>
    private async void OnHostClick()
    {
        Debug.Log("[UIManager] 🏠 Host...");
        if (relayManager == null) return;

        string code = await relayManager.CreateRelay();
        if (code != null)
        {
            codeDisplayText.text = $"Mã phòng: {code}";
        }
    }

    /// <summary>
    /// Multiplayer: Join
    /// </summary>
    private async void OnJoinClick()
    {
        string code = joinInputField.text;
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
    /// Multiplayer: Bắt đầu game
    /// </summary>
    private void OnGameStart()
    {
        Debug.Log("[UIManager] 🚀 Start game!");
        RequestNetworkGameStart();
    }

    /// <summary>
    /// Yêu cầu bắt đầu game qua network variable.
    /// Dùng cho host sau khi đủ điều kiện bắt đầu phòng.
    /// </summary>
    public bool RequestNetworkGameStart()
    {
        MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_MANAGER", "RequestNetworkGameStart invoked");
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[UIManager] ⚠️ Không có NetworkManager.Singleton để start game.");
            MultiplayerDetailedLogger.TraceWarning("UI_MANAGER", "RequestNetworkGameStart aborted: NetworkManager.Singleton null");
            return false;
        }

        if (!IsServer)
        {
            Debug.LogWarning("[UIManager] ⚠️ Chỉ host/server mới được set start game.");
            MultiplayerDetailedLogger.TraceWarning("UI_MANAGER", "RequestNetworkGameStart aborted: not server");
            return false;
        }

        isGameStarted.Value = true;
        Debug.Log("[UIManager] ✅ Network game start requested");
        MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_MANAGER", "RequestNetworkGameStart success: isGameStarted=true");
        return true;
    }

    /// <summary>
    /// Chuyển sang gameplay
    /// </summary>
    private void SwitchToGameplay()
    {
        SetPanelActiveSafe(lobbyPanel, false, nameof(lobbyPanel));
        SetPanelActiveSafe(gameplayPanel, true, nameof(gameplayPanel));
        Debug.Log("[UIManager] 🎮 Chuyển sang Gameplay!");
        MultiplayerDetailedLogger.TraceNetworkSnapshot("UI_MANAGER", "SwitchToGameplay executed");
    }
    
}