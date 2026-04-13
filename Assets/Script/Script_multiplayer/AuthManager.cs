using System.Threading.Tasks;
using UnityEngine;
using DoAnGame.Auth;

/// <summary>
/// Quản lý luồng Authentication
/// Kết nối: Firebase Auth + UI + Session Management + Player Data
/// Cập nhật: Session 24h + PlayerDataService + Character Name
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseManager firebaseManager;
    private SessionManager sessionManager;
    private PlayerDataService playerDataService;
    private UserValidationService validationService;
    private PlayerData currentPlayerData;
    
    // Events
    public System.Action<PlayerData> OnLoginDataLoaded;  // invoked sau khi load player data
    public System.Action<Firebase.Auth.FirebaseUser> OnCurrentUserChanged;

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

    private void Start()
    {
        if (EnsureServicesReady())
        {
            Debug.Log("[Auth] ✅ AuthManager sẵn sàng");
        }
        else
        {
            Debug.LogError("[Auth] ❌ Thiếu service cốt lõi cho auth");
        }
    }

    private bool EnsureServicesReady()
    {
        firebaseManager = ResolveOrCreateFirebaseManager();
        sessionManager = ResolveOrCreate<SessionManager>("SessionManager");
        playerDataService = ResolveOrCreate<PlayerDataService>("PlayerDataService");
        validationService = ResolveOrCreate<UserValidationService>("UserValidationService");

        return firebaseManager != null;
    }

    private FirebaseManager ResolveOrCreateFirebaseManager()
    {
        if (FirebaseManager.Instance != null)
            return FirebaseManager.Instance;

        FirebaseManager existing = FindObjectOfType<FirebaseManager>(true);
        if (existing != null)
            return existing;

        var go = new GameObject("FirebaseManager");
        return go.AddComponent<FirebaseManager>();
    }

    private T ResolveOrCreate<T>(string gameObjectName) where T : MonoBehaviour
    {
        T singleton = null;

        if (typeof(T) == typeof(SessionManager)) singleton = SessionManager.Instance as T;
        if (typeof(T) == typeof(PlayerDataService)) singleton = PlayerDataService.Instance as T;
        if (typeof(T) == typeof(UserValidationService)) singleton = UserValidationService.Instance as T;

        if (singleton != null)
            return singleton;

        T existing = FindObjectOfType<T>(true);
        if (existing != null)
            return existing;

        var go = new GameObject(gameObjectName);
        return go.AddComponent<T>();
    }

    private void NotifyCurrentUserChanged()
    {
        OnCurrentUserChanged?.Invoke(GetCurrentUser());
    }
    
    /// <summary>
    /// Kiểm tra session còn hạn và auto-login nếu có
    /// Gọi từ UIWelcomeIntroController hoặc scene startup
    /// </summary>
    public async Task<bool> CheckAndAutoLogin()
    {
        Debug.Log("[Auth] 🔍 Kiểm tra session local...");

        if (!EnsureServicesReady())
        {
            Debug.LogWarning("[Auth] ⚠️ Services chưa sẵn sàng cho auto-login");
            return false;
        }

        if (firebaseManager != null && !firebaseManager.IsInitialized)
        {
            Debug.Log("[Auth] ⏳ Chờ Firebase khởi tạo xong trước khi auto-login...");
            if (!await firebaseManager.EnsureInitializedAsync())
            {
                Debug.LogWarning("[Auth] ⚠️ Firebase khởi tạo thất bại khi auto-login");
                return false;
            }
        }

        // Contract cứng: Chỉ cho phép auto-login khi có session local hợp lệ.
        // Nếu Firebase còn giữ trạng thái đăng nhập nhưng session local đã bị xóa/hết hạn,
        // coi như không hợp lệ và ép sign-out để không tự vào lại tài khoản cũ.
        bool hasValidSession = sessionManager != null && sessionManager.IsSessionValid();
        if (!hasValidSession)
        {
            var lingeringUser = firebaseManager != null ? firebaseManager.GetCurrentUser() : null;
            if (lingeringUser != null)
            {
                firebaseManager.Logout();
                Debug.Log("[Auth] ℹ️ Không có session local hợp lệ, đã sign-out user Firebase còn lưu.");
            }

            currentPlayerData = null;
            return false;
        }

        var user = await GetCurrentUserWithRetry();
        if (user == null)
        {
            var cachedData = playerDataService != null ? playerDataService.GetCachedPlayerData() : null;
            if (cachedData != null)
            {
                currentPlayerData = cachedData;
                OnLoginDataLoaded?.Invoke(currentPlayerData);
                Debug.Log($"[Auth] ✅ Auto-load từ session local/cache thành công: {cachedData.characterName}");
                return true;
            }

            Debug.LogWarning("[Auth] ⚠️ Session local còn hạn nhưng chưa có player data cache");
            return false;
        }

        string uid = user.UserId;
        string fallbackName = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName
            : (!string.IsNullOrWhiteSpace(user.Email) ? user.Email : "Player");

        if (playerDataService != null)
        {
            currentPlayerData = await playerDataService.LoadPlayerDataAsync(uid);
        }

        if (currentPlayerData == null)
        {
            currentPlayerData = CreateDefaultPlayerData(uid, fallbackName);
        }

        OnLoginDataLoaded?.Invoke(currentPlayerData);

        if (sessionManager != null && !sessionManager.IsSessionValid())
        {
            sessionManager.SaveSession(uid, user.Email);
        }

        Debug.Log($"[Auth] ✅ Auto-load account thành công: {currentPlayerData.characterName}");
        NotifyCurrentUserChanged();
        return true;
    }

    /// <summary>
    /// Đăng ký tài khoản với tên nhân vật
    /// </summary>
    public async Task<bool> Register(string email, string password, string characterName, int age)
    {
        Debug.Log($"[Auth] 📝 Đăng ký: {characterName}");

        if (!EnsureServicesReady())
        {
            Debug.LogWarning("[Auth] ⚠️ Services chưa sẵn sàng cho đăng ký");
            return false;
        }

        if (firebaseManager != null && !firebaseManager.IsInitialized)
        {
            Debug.Log("[Auth] ⏳ Chờ Firebase khởi tạo xong trước khi đăng ký...");
            if (!await firebaseManager.EnsureInitializedAsync())
            {
                Debug.LogWarning("[Auth] ⚠️ Firebase khởi tạo thất bại");
                return false;
            }
        }

        bool success = await firebaseManager.RegisterAsync(email, password, characterName, age);
        
        if (success)
        {
            var user = await GetCurrentUserWithRetry();
            string uid = user != null ? user.UserId : null;

            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogWarning("[Auth] ⚠️ Register thành công nhưng CurrentUser chưa sẵn sàng. Dùng dữ liệu mặc định tạm thời.");
                currentPlayerData = CreateDefaultPlayerData("unknown", characterName);
                OnLoginDataLoaded?.Invoke(currentPlayerData);
                NotifyCurrentUserChanged();
                return true;
            }

            if (!firebaseManager.IsPlayerDataSyncEnabled())
            {
                currentPlayerData = CreateDefaultPlayerData(uid, characterName);
                OnLoginDataLoaded?.Invoke(currentPlayerData);
                NotifyCurrentUserChanged();
                return true;
            }

            if (playerDataService != null)
            {
                currentPlayerData = await playerDataService.LoadPlayerDataAsync(uid);
                if (currentPlayerData == null)
                {
                    currentPlayerData = CreateDefaultPlayerData(uid, characterName);
                }
                OnLoginDataLoaded?.Invoke(currentPlayerData);
            }

            NotifyCurrentUserChanged();
        }

        return success;
    }

    /// <summary>
    /// Đăng nhập + Session + Load Player Data
    /// </summary>
    public async Task<bool> Login(string email, string password)
    {
        Debug.Log($"[Auth] 🔑 Đăng nhập: {email}");

        if (!EnsureServicesReady())
        {
            Debug.LogWarning("[Auth] ⚠️ Services chưa sẵn sàng cho đăng nhập");
            return false;
        }

        if (firebaseManager != null && !firebaseManager.IsInitialized)
        {
            Debug.Log("[Auth] ⏳ Chờ Firebase khởi tạo xong trước khi đăng nhập...");
            if (!await firebaseManager.EnsureInitializedAsync())
            {
                Debug.LogWarning("[Auth] ⚠️ Firebase khởi tạo thất bại");
                return false;
            }
        }

        bool success = await firebaseManager.LoginAsync(email, password);
        
        if (success)
        {
            var user = await GetCurrentUserWithRetry();
            if (user == null || string.IsNullOrEmpty(user.UserId))
            {
                Debug.LogWarning("[Auth] ⚠️ Login thành công nhưng CurrentUser chưa sẵn sàng.");
                return false;
            }

            if (!firebaseManager.IsPlayerDataSyncEnabled())
            {
                currentPlayerData = CreateDefaultPlayerData(
                    user.UserId,
                    string.IsNullOrWhiteSpace(user.DisplayName) ? "Player" : user.DisplayName);
                OnLoginDataLoaded?.Invoke(currentPlayerData);
                NotifyCurrentUserChanged();

                if (sessionManager != null)
                {
                    sessionManager.SaveSession(user.UserId, email);
                    Debug.Log("[Auth] 💾 Session saved (24h)");
                }

                return true;
            }
            
            // Tải player data từ Firebase (cross-device sync)
            if (playerDataService != null)
            {
                currentPlayerData = await playerDataService.LoadPlayerDataAsync(user.UserId);
                if (currentPlayerData == null)
                {
                    currentPlayerData = CreateDefaultPlayerData(
                        user.UserId,
                        string.IsNullOrWhiteSpace(user.DisplayName) ? "Player" : user.DisplayName);
                }
                OnLoginDataLoaded?.Invoke(currentPlayerData);
            }
            
            // Lưu session(24h expiry)
            if (sessionManager != null)
            {
                sessionManager.SaveSession(user.UserId, email);
                Debug.Log("[Auth] 💾 Session saved (24h)");
            }

            NotifyCurrentUserChanged();
        }

        return success;
    }

    /// <summary>
    /// Chơi nhanh (ẩn danh - không lưu session)
    /// </summary>
    public async Task<bool> QuickPlay()
    {
        Debug.Log("[Auth] 👤 Chơi nhanh");

        if (!EnsureServicesReady())
        {
            Debug.LogWarning("[Auth] ⚠️ Services chưa sẵn sàng cho quick play");
            return false;
        }

        if (firebaseManager != null && !firebaseManager.IsInitialized)
        {
            Debug.Log("[Auth] ⏳ Chờ Firebase khởi tạo xong trước khi quick play...");
            if (!await firebaseManager.EnsureInitializedAsync())
            {
                Debug.LogWarning("[Auth] ⚠️ Firebase khởi tạo thất bại");
                return false;
            }
        }

        bool success = await firebaseManager.LoginAnonymousAsync();
        
        if (success)
        {
            var user = firebaseManager.GetCurrentUser();
            
            // Tạo player data tạm thời (không sync từ DB)
            currentPlayerData = new PlayerData
            {
                uid = user.UserId,
                characterName = "GuestPlayer",
                level = 1,
                totalScore = 0,
                totalXp = 0,
                gamesPlayed = 0,
                gamesWon = 0,
                winRate = 0f
            };
            
            OnLoginDataLoaded?.Invoke(currentPlayerData);
            Debug.Log("[Auth] ✅ Quick play session created (anonymous)");
            NotifyCurrentUserChanged();
        }

        return success;
    }

    /// <summary>
    /// Đăng xuất + Clear Session
    /// </summary>
    public void Logout()
    {
        EnsureServicesReady();

        if (firebaseManager != null)
        {
            firebaseManager.Logout();
        }
        
        // Clear session
        if (sessionManager != null)
        {
            sessionManager.ClearSession();
        }
        
        // Clear cached player data
        if (playerDataService != null)
        {
            playerDataService.ClearLocalCache();
        }

        ClearAllKnownLocalAuthKeys();
        
        currentPlayerData = null;
        Debug.Log("[Auth] 👋 Đã đăng xuất + session cleared");
        NotifyCurrentUserChanged();
    }

    /// <summary>
    /// Lấy user hiện tại
    /// </summary>
    public Firebase.Auth.FirebaseUser GetCurrentUser()
    {
        if (!EnsureServicesReady())
            return null;

        return firebaseManager.GetCurrentUser();
    }

    /// <summary>
    /// Lấy player data (cached hoặc từ service)
    /// </summary>
    public PlayerData GetCurrentPlayerData()
    {
        if (currentPlayerData != null)
            return currentPlayerData;
        
        // Try load từ cache
        if (playerDataService != null)
            return playerDataService.GetCachedPlayerData();
        
        return null;
    }

    /// <summary>
    /// Kiểm tra đã đăng nhập?
    /// </summary>
    public bool IsLoggedIn()
    {
        return GetCurrentUser() != null;
    }

    /// <summary>
    /// Kiểm tra có email không
    /// </summary>
    public bool HasEmail()
    {
        var user = GetCurrentUser();
        return user != null && !string.IsNullOrEmpty(user.Email);
    }
    
    /// <summary>
    /// Lấy character name từ player data
    /// </summary>
    public string GetCharacterName()
    {
        var data = GetCurrentPlayerData();
        return data?.characterName ?? "Unknown";
    }

    private static PlayerData CreateDefaultPlayerData(string uid, string characterName)
    {
        return new PlayerData
        {
            uid = uid,
            characterName = characterName,
            level = 1,
            totalXp = 0,
            totalScore = 0,
            rank = 0,
            gamesPlayed = 0,
            gamesWon = 0,
            winRate = 0f
        };
    }

    private async Task<Firebase.Auth.FirebaseUser> GetCurrentUserWithRetry(int maxAttempts = 5, int delayMs = 120)
    {
        Firebase.Auth.FirebaseUser user = null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            user = firebaseManager != null ? firebaseManager.GetCurrentUser() : null;
            if (user != null && !string.IsNullOrEmpty(user.UserId))
            {
                return user;
            }

            await Task.Delay(delayMs);
        }

        return null;
    }

    private void ClearAllKnownLocalAuthKeys()
    {
        // Cleanup key cũ và key namespaced để đảm bảo logout sạch giữa các phiên bản.
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("uid"));
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("isAnonymous"));
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("session_token"));
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("session_expiry"));
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("last_email"));
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("cached_player_data"));
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("cached_player_data_timestamp"));

        PlayerPrefs.DeleteKey("uid");
        PlayerPrefs.DeleteKey("isAnonymous");
        PlayerPrefs.DeleteKey("session_token");
        PlayerPrefs.DeleteKey("session_expiry");
        PlayerPrefs.DeleteKey("last_email");
        PlayerPrefs.DeleteKey("cached_player_data");
        PlayerPrefs.DeleteKey("cached_player_data_timestamp");
        PlayerPrefs.Save();
    }
}
