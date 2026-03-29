using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Quản lý luồng Authentication
/// Kết nối Firebase Auth + UI
/// </summary>
public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    private FirebaseManager firebaseManager;
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
            // Tải player data
            var user = firebaseManager.GetCurrentUser();
            currentPlayerData = await firebaseManager.LoadPlayerDataAsync(user.UserId);
        }

        return success;
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    public async Task<bool> Login(string email, string password)
    {
        Debug.Log($"[Auth] 🔑 Đăng nhập: {email}");

        bool success = await firebaseManager.LoginAsync(email, password);
        
        if (success)
        {
            // Tải player data
            var user = firebaseManager.GetCurrentUser();
            currentPlayerData = await firebaseManager.LoadPlayerDataAsync(user.UserId);
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
        }

        return success;
    }

    /// <summary>
    /// Đăng xuất
    /// </summary>
    public void Logout()
    {
        firebaseManager.Logout();
        currentPlayerData = null;
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
