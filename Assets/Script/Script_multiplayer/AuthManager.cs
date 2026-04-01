using System.Threading.Tasks;
using UnityEngine;
using DoAnGame.Data;
using DoAnGame.Multiplayer;

/// <summary>
/// Quản lý luồng Authentication
/// Kết nối Firebase Auth + Local DB + Cloud Sync
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseManager firebaseManager;
    private CloudSyncManager cloudSyncManager;
    private PlayerData currentPlayerData;

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
        firebaseManager = FirebaseManager.Instance;
        cloudSyncManager = CloudSyncManager.Instance;
        
        if (firebaseManager == null)
        {
            Debug.LogError("[Auth] ❌ FirebaseManager không tìm thấy!");
        }
        else
        {
            Debug.Log("[Auth] ✅ AuthManager sẵn sàng");
        }
    }

    /// <summary>
    /// Đăng ký tài khoản
    /// </summary>
    public async Task<bool> Register(string email, string password, string username, int age)
    {
        Debug.Log($"[Auth] 📝 Đăng ký: {username}");

        bool success = await firebaseManager.RegisterAsync(email, password, username, age);
        
        if (success)
        {
            // Tải player data từ Firebase
            var user = firebaseManager.GetCurrentUser();
            currentPlayerData = await firebaseManager.LoadPlayerDataAsync(user.UserId);
            
            // Lưu local
            if (currentPlayerData != null)
            {
                LocalDataManager.SavePlayerDataLocal(currentPlayerData);
            }
        }

        return success;
    }

    /// <summary>
    /// Đăng nhập (load từ Firebase + cache local)
    /// </summary>
    public async Task<bool> Login(string email, string password)
    {
        Debug.Log($"[Auth] 🔑 Đăng nhập: {email}");

        bool success = await firebaseManager.LoginAsync(email, password);
        
        if (success)
        {
            var user = firebaseManager.GetCurrentUser();
            
            // 1. Tải player data từ Firebase
            currentPlayerData = await firebaseManager.LoadPlayerDataAsync(user.UserId);
            if (currentPlayerData != null)
            {
                // 2. Lưu local (instant cache)
                LocalDataManager.SavePlayerDataLocal(currentPlayerData);
                Debug.Log($"[Auth] ✅ Logged in as {currentPlayerData.username}");
            }
            
            // 3. Trigger sync manager (background)
            if (cloudSyncManager != null)
            {
                await cloudSyncManager.SyncPlayerDataIfNeeded();
            }
        }

        return success;
    }

    /// <summary>
    /// Chơi nhanh (ẩn danh)
    /// </summary>
    public async Task<bool> QuickPlay()
    {
        Debug.Log("[Auth] 👤 Chơi nhanh");

        bool success = await firebaseManager.LoginAnonymousAsync();
        
        if (success)
        {
            // Tạo player data tạm thời
            currentPlayerData = new PlayerData
            {
                uid = firebaseManager.GetCurrentUser().UserId,
                username = "GuestPlayer",
                totalScore = 0,
                totalXp = 0,
                currentLevel = 1,
                gamesPlayed = 0,
                gamesWon = 0
            };
            
            // Lưu local
            LocalDataManager.SavePlayerDataLocal(currentPlayerData);
        }

        return success;
    }

    /// <summary>
    /// Đăng xuất (clear local + Firebase)
    /// </summary>
    public void Logout()
    {
        firebaseManager.Logout();
        currentPlayerData = null;
        
        // Clear local data
        LocalDataManager.ClearAllData();
        
        Debug.Log("[Auth] 👋 Đã đăng xuất");
    }

    /// <summary>
    /// Lấy user hiện tại
    /// </summary>
    public Firebase.Auth.FirebaseUser GetCurrentUser()
    {
        return firebaseManager.GetCurrentUser();
    }

    /// <summary>
    /// Lấy player data
    /// </summary>
    public PlayerData GetCurrentPlayerData()
    {
        return currentPlayerData;
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
}
