using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

/// <summary>
/// Quản lý Firebase: Authentication + Realtime Database (Real-time sync)
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private DatabaseReference dbRef;

    // Events
    public event Action<FirebaseUser> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action<FirebaseUser> OnRegisterSuccess;
    public event Action<string> OnRegisterFailed;
    public event Action<PlayerData> OnPlayerDataLoaded;

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
        try
        {
            await InitializeFirebase();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi khởi tạo: {e.Message}");
        }
    }

    /// <summary>
    /// Khởi tạo Firebase Auth + Realtime Database
    /// </summary>
    private async Task InitializeFirebase()
    {
        Debug.Log("[Firebase] 🔄 Đang khởi tạo...");

        try
        {
            // Init Auth
            auth = FirebaseAuth.DefaultInstance;
            
            // Init Realtime Database
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;

            // Lắng nghe auth state changes
            auth.StateChanged += AuthStateChanged;
            AuthStateChanged(auth, null);

            Debug.Log("[Firebase] ✅ Khởi tạo thành công!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi khởi tạo: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != currentUser)
        {
            bool signedIn = auth.CurrentUser != null && auth.CurrentUser.IsValid();
            
            if (signedIn)
            {
                currentUser = auth.CurrentUser;
                Debug.Log($"[Firebase] ✅ Người dùng đăng nhập: {currentUser.Email}");
            }
            else
            {
                Debug.Log("[Firebase] ℹ️ Chưa đăng nhập");
                currentUser = null;
            }
        }
    }

    /// <summary>
    /// Đăng ký tài khoản + lưu vào Realtime DB
    /// </summary>
    public async Task<bool> RegisterAsync(string email, string password, string username, int age)
    {
        try
        {
            Debug.Log($"[Firebase] 📝 Đăng ký: {email}");

            // Tạo auth user
            var authResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            var newUser = authResult.User;

            // Cập nhật profile
            var userProfile = new UserProfile { DisplayName = username };
            await newUser.UpdateUserProfileAsync(userProfile);

            // Lưu user data vào Realtime DB
            var userData = new UserData
            {
                uid = newUser.UserId,
                email = email,
                username = username,
                age = age,
                createdAt = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            await SaveUserToDatabase(userData);

            // Tạo player data record
            var playerData = new PlayerData
            {
                uid = newUser.UserId,
                username = username,
                totalScore = 0,
                totalXp = 0,
                currentLevel = 1,
                gamesPlayed = 0,
                gamesWon = 0,
                winRate = 0f
            };
            await SavePlayerToDatabase(playerData);

            currentUser = newUser;
            OnRegisterSuccess?.Invoke(newUser);
            Debug.Log("[Firebase] ✅ Đăng ký thành công!");
            return true;
        }
        catch (FirebaseException ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi đăng ký: {ex.Message}");
            OnRegisterFailed?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            Debug.Log($"[Firebase] 🔑 Đăng nhập: {email}");

            var authResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
            currentUser = authResult.User;

            // Lưu UID
            PlayerPrefs.SetString("uid", currentUser.UserId);
            PlayerPrefs.Save();

            OnLoginSuccess?.Invoke(currentUser);
            Debug.Log("[Firebase] ✅ Đăng nhập thành công!");
            return true;
        }
        catch (FirebaseException ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi đăng nhập: {ex.Message}");
            OnLoginFailed?.Invoke(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Đăng nhập ẩn danh (Chơi nhanh)
    /// </summary>
    public async Task<bool> LoginAnonymousAsync()
    {
        try
        {
            Debug.Log("[Firebase] 👤 Đăng nhập ẩn danh");

            var authResult = await auth.SignInAnonymouslyAsync();
            currentUser = authResult.User;

            // Lưu đó là anonymous
            PlayerPrefs.SetString("uid", currentUser.UserId);
            PlayerPrefs.SetInt("isAnonymous", 1);
            PlayerPrefs.Save();

            Debug.Log("[Firebase] ✅ Đăng nhập ẩn danh thành công!");
            return true;
        }
        catch (FirebaseException ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Đăng xuất
    /// </summary>
    public void Logout()
    {
        auth.SignOut();
        currentUser = null;
        PlayerPrefs.DeleteKey("uid");
        PlayerPrefs.Save();
        Debug.Log("[Firebase] 👋 Đã đăng xuất");
    }

    /// <summary>
    /// Lấy user hiện tại
    /// </summary>
    public FirebaseUser GetCurrentUser()
    {
        return currentUser;
    }

    /// <summary>
    /// Lưu user vào Realtime Database
    /// </summary>
    private async Task SaveUserToDatabase(UserData userData)
    {
        try
        {
            string json = JsonUtility.ToJson(userData);
            await dbRef.Child("users").Child(userData.uid).SetRawJsonValueAsync(json);
            Debug.Log("[Firebase] ✅ Lưu user data thành công");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi lưu user: {ex.Message}");
        }
    }

    /// <summary>
    /// Lưu player vào Realtime Database
    /// </summary>
    private async Task SavePlayerToDatabase(PlayerData playerData)
    {
        try
        {
            string json = JsonUtility.ToJson(playerData);
            await dbRef.Child("playerData").Child(playerData.uid).SetRawJsonValueAsync(json);
            Debug.Log("[Firebase] ✅ Lưu player data thành công");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi lưu player data: {ex.Message}");
        }
    }

    /// <summary>
    /// Tải player data từ Realtime Database (một lần)
    /// </summary>
    public async Task<PlayerData> LoadPlayerDataAsync(string uid)
    {
        try
        {
            var snapshot = await dbRef.Child("playerData").Child(uid).GetValueAsync();
            
            if (snapshot.Exists)
            {
                string json = snapshot.GetRawJsonValue();
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                OnPlayerDataLoaded?.Invoke(data);
                Debug.Log("[Firebase] ✅ Tải player data thành công");
                return data;
            }
            else
            {
                Debug.LogWarning("[Firebase] ⚠️ Không tìm thấy player data");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi tải player data: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Cập nhật score/xp (tự động sync với DB)
    /// </summary>
    public async Task UpdatePlayerStatsAsync(string uid, int scoreToAdd, int xpToAdd)
    {
        try
        {
            var player = await LoadPlayerDataAsync(uid);
            if (player == null) return;

            player.totalScore += scoreToAdd;
            player.totalXp += xpToAdd;
            player.gamesPlayed += 1;

            // Tính level (mỗi 100 XP = 1 level)
            player.currentLevel = 1 + (player.totalXp / 100);

            await SavePlayerToDatabase(player);
            Debug.Log($"[Firebase] ✅ Cập nhật stats: +{scoreToAdd} score, +{xpToAdd} xp");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi cập nhật stats: {ex.Message}");
        }
    }

    /// <summary>
    /// Listener real-time cho player data (tự động cập nhật khi DB thay đổi)
    /// </summary>
    public void ListenToPlayerData(string uid, System.Action<PlayerData> onDataChanged)
    {
        try
        {
            dbRef.Child("playerData").Child(uid).ValueChanged += (object sender, ValueChangedEventArgs args) =>
            {
                if (args.DatabaseError != null)
                {
                    Debug.LogError($"[Firebase] ❌ Lỗi listener: {args.DatabaseError.Message}");
                    return;
                }

                if (args.Snapshot.Exists)
                {
                    string json = args.Snapshot.GetRawJsonValue();
                    PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                    onDataChanged?.Invoke(data);
                    Debug.Log("[Firebase] 🔄 Player data cập nhật (real-time)");
                }
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi thiết lập listener: {ex.Message}");
        }
    }
}

/// <summary>
/// Dữ liệu User
/// </summary>
[System.Serializable]
public class UserData
{
    public string uid;
    public string email;
    public string username;
    public int age;
    public string avatar;
    public long createdAt;
    public long lastLogin;
    public bool isActive = true;
}

/// <summary>
/// Dữ liệu Player
/// </summary>
[System.Serializable]
public class PlayerData
{
    public string uid;
    public string username;
    public int totalScore;
    public int totalXp;
    public int currentLevel;
    public int rank;
    public int gamesPlayed;
    public int gamesWon;
    public float winRate;
}

/// <summary>
/// Dữ liệu Game Session (cho multiplayer)
/// </summary>
[System.Serializable]
public class GameSession
{
    public string sessionId;
    public string player1Id;
    public string player2Id;
    public int player1Score;
    public int player2Score;
    public string winner;
    public int level;
    public long startTime;
    public long endTime;
    public string difficulty;
}