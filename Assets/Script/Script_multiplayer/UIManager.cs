using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;
using System;
using System.Collections;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        Debug.Log("[UIManager] ✅ Starting...");

        authManager = AuthManager.Instance;
        relayManager = RelayManager.Instance;

        // Thiết lập Panel ban đầu
        ShowUI(0); // Hiện Welcome Screen

        // Bind buttons
        BindButtons();

        // Network
        isGameStarted.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal) SwitchToGameplay();
        };

        await Task.CompletedTask;
    }

    void Update()
    {
        // Cập nhật số người chơi (nếu ở trong Multiplayer Lobby)
        if (lobbyPanel.activeSelf && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            statusText.text = $"Người chơi: {playerCount}/2";
            startButton.interactable = (IsServer && playerCount >= 2);
        }
    }

    /// <summary>
    /// Liên kết tất cả nút bấm
    /// </summary>
    private void BindButtons()
    {
        // UI 1 - Welcome Screen
        if (playButton != null)
            playButton.onClick.AddListener(() => ShowUI(1));

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
        welcomeScreenPanel.SetActive(false);
        authChoicePanel.SetActive(false);
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        gameplayPanel.SetActive(false);

        // Hiện UI được chọn
        switch (uiIndex)
        {
            case 0:
                welcomeScreenPanel.SetActive(true);
                Debug.Log("[UIManager] 📱 UI 0: Welcome Screen");
                break;
            case 1:
                authChoicePanel.SetActive(true);
                Debug.Log("[UIManager] 📱 UI 1: Auth Choice");
                break;
            case 2:
                loginPanel.SetActive(true);
                Debug.Log("[UIManager] 📱 UI 2: Login");
                break;
            case 3:
                registerPanel.SetActive(true);
                Debug.Log("[UIManager] 📱 UI 3: Register");
                break;
            case 4:
                mainMenuPanel.SetActive(true);
                if (welcomeText != null)
                    welcomeText.text = $"Chào {authManager.GetCurrentPlayerData()?.username ?? "Khách"}! 👋";
                Debug.Log("[UIManager] 📱 UI 4: Main Menu");
                break;
            case 5:
                lobbyPanel.SetActive(true);
                Debug.Log("[UIManager] 📱 UI 5: Multiplayer Lobby");
                break;
        }
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
        if (IsServer)
        {
            isGameStarted.Value = true;
        }
    }

    /// <summary>
    /// Chuyển sang gameplay
    /// </summary>
    private void SwitchToGameplay()
    {
        lobbyPanel.SetActive(false);
        gameplayPanel.SetActive(true);
        Debug.Log("[UIManager] 🎮 Chuyển sang Gameplay!");
    }
}