using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using DoAnGame.Auth;

/// <summary>
/// Quản lý Firebase: Authentication + Firestore
/// Cập nhật: Thêm support cho character name + detailed error codes
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [SerializeField] private bool enablePlayerDataSync = false;
    
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private FirebaseFirestore firestore;
    private UserValidationService validationService;
    private bool isInitialized;
    private TaskCompletionSource<bool> initializationTcs;
    private string lastRegisterErrorDetail;
    private string lastRegisterErrorMessage;
    private bool authOperationInProgress;

    // Events
    public event Action<FirebaseUser> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action<string> OnLoginFailedDetail; // Error code + message
    public event Action<FirebaseUser> OnRegisterSuccess;
    public event Action<string> OnRegisterFailed;
    public event Action<string> OnRegisterFailedDetail; // Error code + message
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
    /// Khởi tạo Firebase Auth + Firestore + Validation Service
    /// </summary>
    private async Task InitializeFirebase()
    {
        Debug.Log("[Firebase] 🔄 Đang khởi tạo...");
        initializationTcs ??= new TaskCompletionSource<bool>();

        try
        {
            // Step 0: Check Firebase dependencies
            Debug.Log("[Firebase] 0️⃣ Kiểm tra Firebase dependencies...");
            var dependencyStatus = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus != Firebase.DependencyStatus.Available)
            {
                Debug.LogError($"[Firebase] ❌ Firebase dependencies không available: {dependencyStatus}");
                return;
            }
            Debug.Log("[Firebase] ✅ Firebase dependencies sẵn sàng");

            // Init Auth
            Debug.Log("[Firebase] 1️⃣ Khởi tạo Auth...");
            auth = FirebaseAuth.DefaultInstance;
            Debug.Log("[Firebase] ✅ Auth khởi tạo xong");

            // Init Firestore cho auth profile/users
            try
            {
                firestore = FirebaseFirestore.DefaultInstance;
                ConfigureFirestoreForCurrentProcess();
                Debug.Log("[Firebase] ✅ Firestore khởi tạo xong");
            }
            catch (Exception fsEx)
            {
                Debug.LogWarning($"[Firebase] ⚠️ Firestore chưa sẵn sàng: {fsEx.Message}");
                firestore = null;
            }
            
            // Get Validation Service
            Debug.Log("[Firebase] 2️⃣ Lấy UserValidationService...");
            validationService = UserValidationService.Instance;
            if (validationService == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ UserValidationService not found, creating new instance");
            }
            else
            {
                Debug.Log("[Firebase] ✅ UserValidationService lấy được");
            }

            // Lắng nghe auth state changes
            Debug.Log("[Firebase] 3️⃣ Setup auth state listener...");
            auth.StateChanged += AuthStateChanged;
            AuthStateChanged(auth, null);

            isInitialized = true;
            initializationTcs.TrySetResult(true);

            Debug.Log("[Firebase] ✅ Khởi tạo thành công!");
        }
        catch (Exception ex)
        {
            isInitialized = false;
            initializationTcs.TrySetResult(false);
            Debug.LogError($"[Firebase] ❌ Lỗi khởi tạo: {ex.Message}");
            Debug.LogError($"[Firebase] ❌ Stack trace: {ex.StackTrace}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tránh đụng LOCK file Firestore LevelDB khi chạy nhiều tiến trình Unity trong Editor (main + clone).
    /// </summary>
    private void ConfigureFirestoreForCurrentProcess()
    {
        if (firestore == null)
        {
            return;
        }

        RuntimeInstanceContext.ConfigureFirestoreSettings(firestore, "Firebase");
    }

    public bool IsInitialized => isInitialized;

    public async Task<bool> EnsureInitializedAsync()
    {
        if (isInitialized)
            return true;

        initializationTcs ??= new TaskCompletionSource<bool>();
        return await initializationTcs.Task;
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != currentUser)
        {
            bool signedIn = auth.CurrentUser != null && auth.CurrentUser.IsValid();
            
            if (signedIn)
            {
                currentUser = auth.CurrentUser;
                Debug.Log($"[Firebase] ✅ Khôi phục phiên Firebase: {currentUser.Email}");
            }
            else
            {
                Debug.Log("[Firebase] ℹ️ Không có phiên Firebase đang hoạt động");
                currentUser = null;
            }
        }
    }

    /// <summary>
    /// Đăng ký tài khoản + lưu vào Firestore
    /// Tham số: email, password, characterName (tên nhân vật), age
    /// </summary>
    public async Task<bool> RegisterAsync(string email, string password, string characterName, int age)
    {
        try
        {
            lastRegisterErrorDetail = null;
            lastRegisterErrorMessage = null;
            Debug.Log($"[Firebase] 📝 Đăng ký: {email} | Character: {characterName}");

            // Validation (using UserValidationService)
            if (validationService != null)
            {
                // Validate email
                var emailResult = validationService.ValidateEmail(email);
                if (!emailResult.IsValid)
                {
                    lastRegisterErrorDetail = $"email_invalid:{emailResult.Message}";
                    lastRegisterErrorMessage = emailResult.Message;
                    Debug.LogWarning($"[Firebase] ⚠️ Email validation failed: {emailResult.Message}");
                    OnRegisterFailedDetail?.Invoke(lastRegisterErrorDetail);
                    return false;
                }

                // Validate password
                var passwordResult = validationService.ValidatePassword(password);
                if (!passwordResult.IsValid)
                {
                    lastRegisterErrorDetail = $"weak_password:{passwordResult.Message}";
                    lastRegisterErrorMessage = passwordResult.Message;
                    Debug.LogWarning($"[Firebase] ⚠️ Password validation failed: {passwordResult.Message}");
                    OnRegisterFailedDetail?.Invoke(lastRegisterErrorDetail);
                    return false;
                }

                // Validate character name (includes unique check)
                var charNameResult = await validationService.ValidateCharacterName(characterName);
                if (!charNameResult.IsValid)
                {
                    lastRegisterErrorDetail = $"{charNameResult.ErrorCode}:{charNameResult.Message}";
                    lastRegisterErrorMessage = charNameResult.Message;
                    Debug.LogWarning($"[Firebase] ⚠️ Character name validation failed: {charNameResult.Message}");
                    OnRegisterFailedDetail?.Invoke(lastRegisterErrorDetail);
                    return false;
                }

                // Validate age
                var ageResult = validationService.ValidateAge(age);
                if (!ageResult.IsValid)
                {
                    lastRegisterErrorDetail = $"age_out_of_range:{ageResult.Message}";
                    lastRegisterErrorMessage = ageResult.Message;
                    Debug.LogWarning($"[Firebase] ⚠️ Age validation failed: {ageResult.Message}");
                    OnRegisterFailedDetail?.Invoke(lastRegisterErrorDetail);
                    return false;
                }
            }

            // Tạo auth user
            var authResult = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            var newUser = authResult.User;

            // Cập nhật profile (Firebase DisplayName = characterName)
            var userProfile = new UserProfile { DisplayName = characterName };
            await newUser.UpdateUserProfileAsync(userProfile);

            // Lưu user data vào Firestore
            var userData = new UserData
            {
                uid = newUser.UserId,
                email = email,
                characterName = characterName,  // ← Tên nhân vật
                age = age,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                lastLogin = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            await SaveUserToDatabase(userData);

            if (enablePlayerDataSync)
            {
                // Tạo player data record (initial)
                var playerData = new PlayerData
                {
                    uid = newUser.UserId,
                    characterName = characterName,  // ← Sync characterName
                    level = 1,
                    totalXp = 0,
                    totalScore = 0,
                    rank = 0,
                    gamesPlayed = 0,
                    gamesWon = 0,
                    winRate = 0f,
                    lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                await SavePlayerToDatabase(playerData);
            }

            currentUser = newUser;
            OnRegisterSuccess?.Invoke(newUser);
            Debug.Log($"[Firebase] ✅ Đăng ký thành công! Character: {characterName}");
            return true;
        }
        catch (FirebaseException ex)
        {
            string errorCode = "register_failed";
            
            // Parse Firebase error codes
            if (ex.Message.Contains("EMAIL_EXISTS") || ex.Message.Contains("email"))
                errorCode = "email_already_exists";
            else if (ex.Message.Contains("WEAK_PASSWORD"))
                errorCode = "weak_password";

            lastRegisterErrorDetail = $"{errorCode}:{ex.Message}";
            lastRegisterErrorMessage = ex.Message;
            Debug.LogWarning($"[Firebase] ⚠️ Đăng ký thất bại ({errorCode}): {ex.Message}");
            OnRegisterFailed?.Invoke(ex.Message);
            OnRegisterFailedDetail?.Invoke(lastRegisterErrorDetail);
            return false;
        }
    }

    public string GetLastRegisterErrorDetail()
    {
        return lastRegisterErrorDetail;
    }

    public string GetLastRegisterErrorMessage()
    {
        return lastRegisterErrorMessage;
    }

    /// <summary>
    /// Đăng nhập + update lastLogin
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        if (authOperationInProgress)
        {
            Debug.LogWarning("[Firebase] ⚠️ Đang có tác vụ auth khác, bỏ qua login trùng.");
            return false;
        }

        authOperationInProgress = true;
        try
        {
            Debug.Log($"[Firebase] 🔑 Đăng nhập: {email}");

            if (!isInitialized)
            {
                bool initOk = await EnsureInitializedAsync();
                if (!initOk)
                {
                    Debug.LogWarning("[Firebase] ⚠️ Firebase chưa sẵn sàng cho login.");
                    return false;
                }
            }

            if (auth == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Auth instance null khi login.");
                return false;
            }

            var signInTask = auth.SignInWithEmailAndPasswordAsync(email, password);
            var timeoutTask = Task.Delay(15000);
            var finished = await Task.WhenAny(signInTask, timeoutTask);
            if (finished == timeoutTask)
            {
                Debug.LogWarning("[Firebase] ⚠️ Login timeout (15s).");
                OnLoginFailed?.Invoke("timeout");
                OnLoginFailedDetail?.Invoke("network_timeout:Login timeout (15s)");
                return false;
            }

            var authResult = await signInTask;
            currentUser = authResult.User;

            // Update lastLogin timestamp
            await UpdateLastLogin(currentUser.UserId);

            // Lưu UID vào local
            PlayerPrefs.SetString(LocalStorageKeyResolver.Key("uid"), currentUser.UserId);
            PlayerPrefs.Save();

            OnLoginSuccess?.Invoke(currentUser);
            Debug.Log("[Firebase] ✅ Đăng nhập thành công!");
            return true;
        }
        catch (FirebaseException ex)
        {
            string errorCode = "login_failed";
            string message = ex.Message ?? string.Empty;
            
            // Parse Firebase error codes
            if (message.Contains("EMAIL_NOT_FOUND") || message.Contains("USER_DISABLED") || message.Contains("USER_NOT_FOUND"))
                errorCode = "user_not_found";
            else if (message.Contains("INVALID_PASSWORD") || message.Contains("WRONG_PASSWORD") || message.Contains("INVALID_LOGIN_CREDENTIALS"))
                errorCode = "invalid_password";
            else if (message.Contains("NETWORK") || message.Contains("network"))
                errorCode = "network_error";
            else if (message.Contains("An internal error has occurred") || message.Contains("INTERNAL_ERROR"))
                errorCode = "invalid_credentials";
            
            // Expected login failures should not be logged as hard errors to avoid pausing play mode.
            Debug.LogWarning($"[Firebase] ⚠️ Đăng nhập thất bại ({errorCode}): {message}");
            OnLoginFailed?.Invoke(message);
            OnLoginFailedDetail?.Invoke($"{errorCode}:{message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Firebase] ⚠️ Đăng nhập thất bại: {ex.Message}");
            OnLoginFailed?.Invoke(ex.Message);
            OnLoginFailedDetail?.Invoke($"login_failed:{ex.Message}");
            return false;
        }
        finally
        {
            authOperationInProgress = false;
        }
    }

    /// <summary>
    /// Update lastLogin timestamp cho user
    /// </summary>
    private async Task UpdateLastLogin(string uid)
    {
        try
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng để update lastLogin.");
                return;
            }

            var payload = new Dictionary<string, object>
            {
                { "lastLogin", now }
            };
            await firestore.Collection("users").Document(uid).SetAsync(payload, SetOptions.MergeAll);

            Debug.Log("[Firebase] ✅ Updated lastLogin");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Firebase] ⚠️ Error updating lastLogin: {ex.Message}");
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
            PlayerPrefs.SetString(LocalStorageKeyResolver.Key("uid"), currentUser.UserId);
            PlayerPrefs.SetInt(LocalStorageKeyResolver.Key("isAnonymous"), 1);
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
        if (auth != null)
        {
            auth.SignOut();
        }

        currentUser = null;
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("uid"));
        PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("isAnonymous"));
        PlayerPrefs.DeleteKey("uid");
        PlayerPrefs.DeleteKey("isAnonymous");
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

    public bool IsPlayerDataSyncEnabled()
    {
        return enablePlayerDataSync;
    }

    /// <summary>
    /// Lưu user vào Firestore
    /// </summary>
    private async Task SaveUserToDatabase(UserData userData)
    {
        try
        {
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng để lưu user.");
                return;
            }

            var payload = new Dictionary<string, object>
            {
                { "uid", userData.uid },
                { "email", userData.email },
                { "characterName", userData.characterName },
                { "age", userData.age },
                { "avatar", userData.avatar ?? string.Empty },
                { "createdAt", userData.createdAt },
                { "lastLogin", userData.lastLogin },
                { "isActive", userData.isActive },
                { "emailVerified", userData.emailVerified }
            };

            await firestore.Collection("users").Document(userData.uid).SetAsync(payload);

            Debug.Log("[Firebase] ✅ Lưu user data thành công");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi lưu user: {ex.Message}");
        }
    }

    /// <summary>
    /// Lưu player vào Firestore
    /// </summary>
    private async Task SavePlayerToDatabase(PlayerData playerData)
    {
        try
        {
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng để lưu player data.");
                return;
            }

            await firestore.Collection("playerData").Document(playerData.uid).SetAsync(PlayerDataToMap(playerData));
            Debug.Log("[Firebase] ✅ Lưu player data thành công");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi lưu player data: {ex.Message}");
        }
    }

    /// <summary>
    /// Tải player data từ Firestore (một lần)
    /// </summary>
    public async Task<PlayerData> LoadPlayerDataAsync(string uid)
    {
        try
        {
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng để tải player data.");
                return null;
            }

            var snapshot = await firestore.Collection("playerData").Document(uid).GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning("[Firebase] ⚠️ Không tìm thấy player data");
                return null;
            }

            PlayerData data = MapToPlayerData(uid, snapshot.ToDictionary());
            OnPlayerDataLoaded?.Invoke(data);
            Debug.Log("[Firebase] ✅ Tải player data thành công");
            return data;
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
            player.level = 1 + (player.totalXp / 100);
            player.lastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng để listen player data.");
                return;
            }

            firestore.Collection("playerData").Document(uid).Listen(snapshot =>
            {
                if (!snapshot.Exists)
                {
                    return;
                }

                PlayerData data = MapToPlayerData(uid, snapshot.ToDictionary());
                onDataChanged?.Invoke(data);
                Debug.Log("[Firebase] 🔄 Player data cập nhật (real-time)");
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi thiết lập listener: {ex.Message}");
        }
    }

    private static Dictionary<string, object> PlayerDataToMap(PlayerData playerData)
    {
        return new Dictionary<string, object>
        {
            { "uid", playerData.uid },
            { "characterName", playerData.characterName ?? string.Empty },
            { "level", playerData.level },
            { "totalXp", playerData.totalXp },
            { "totalScore", playerData.totalScore },
            { "rank", playerData.rank },
            { "gamesPlayed", playerData.gamesPlayed },
            { "gamesWon", playerData.gamesWon },
            { "winRate", playerData.winRate },
            { "lastUpdated", playerData.lastUpdated }
        };
    }

    private static PlayerData MapToPlayerData(string uid, Dictionary<string, object> map)
    {
        return new PlayerData
        {
            uid = GetString(map, "uid", uid),
            characterName = GetString(map, "characterName", "Player"),
            level = GetInt(map, "level", 1),
            totalXp = GetInt(map, "totalXp", 0),
            totalScore = GetInt(map, "totalScore", 0),
            rank = GetInt(map, "rank", 0),
            gamesPlayed = GetInt(map, "gamesPlayed", 0),
            gamesWon = GetInt(map, "gamesWon", 0),
            winRate = GetFloat(map, "winRate", 0f),
            lastUpdated = GetLong(map, "lastUpdated", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        };
    }

    private static string GetString(Dictionary<string, object> map, string key, string fallback)
    {
        if (map != null && map.TryGetValue(key, out object value) && value != null)
        {
            return value.ToString();
        }
        return fallback;
    }

    private static int GetInt(Dictionary<string, object> map, string key, int fallback)
    {
        if (map != null && map.TryGetValue(key, out object value) && value != null)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch { }
        }
        return fallback;
    }

    private static long GetLong(Dictionary<string, object> map, string key, long fallback)
    {
        if (map != null && map.TryGetValue(key, out object value) && value != null)
        {
            try
            {
                return Convert.ToInt64(value);
            }
            catch { }
        }
        return fallback;
    }

    private static float GetFloat(Dictionary<string, object> map, string key, float fallback)
    {
        if (map != null && map.TryGetValue(key, out object value) && value != null)
        {
            try
            {
                return Convert.ToSingle(value);
            }
            catch { }
        }
        return fallback;
    }
}

/// <summary>
/// Dữ liệu User (lưu trong /users/{uid})
/// </summary>
[System.Serializable]
public class UserData
{
    public string uid;
    public string email;
    public string characterName;        // ← Tên nhân vật (dùng trong game + multiplayer)
    public int age;
    public string avatar;
    public long createdAt;
    public long lastLogin;
    public bool isActive = true;
    public bool emailVerified = false;
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