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

    [SerializeField] private bool enablePlayerDataSync = true;  // ← Mặc định BẬT để tự động tạo database
    
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
            Debug.LogWarning("[Firebase] Duplicate FirebaseManager detected, disabling duplicate component.");
            enabled = false;
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

                // Tạo 15 gameModeProgress records (3 chế độ x 5 lớp)
                await CreateInitialGameModeProgress(newUser.UserId);
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
    /// Tạo 15 gameModeProgress records ban đầu (3 chế độ x 5 lớp)
    /// </summary>
    private async Task CreateInitialGameModeProgress(string uid)
    {
        try
        {
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng để tạo gameModeProgress.");
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string[] gameModes = { "chonda", "keothada", "phithuyen" };
            int[] grades = { 1, 2, 3, 4, 5 };

            Debug.Log("[Firebase] 🔄 Đang tạo 15 gameModeProgress records...");

            // Tạo 15 records (3 chế độ x 5 lớp)
            foreach (string gameMode in gameModes)
            {
                foreach (int grade in grades)
                {
                    string progressId = $"{uid}_{gameMode}_{grade}";
                    
                    var progressData = new Dictionary<string, object>
                    {
                        { "progressId", progressId },
                        { "uid", uid },
                        { "gameMode", gameMode },
                        { "grade", grade },
                        { "currentLevel", 1 },
                        { "maxLevelUnlocked", 1 },
                        { "totalScore", 0 },
                        { "bestScore", 0 },
                        { "lastPlayed", null }  // NULL = chưa chơi
                    };

                    await firestore.Collection("gameModeProgress").Document(progressId).SetAsync(progressData);
                }
            }

            Debug.Log("[Firebase] ✅ Đã tạo 15 gameModeProgress records thành công!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi tạo gameModeProgress: {ex.Message}");
        }
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
    /// Cập nhật tiến độ sau khi chơi level
    /// </summary>
    public async Task UpdateLevelProgressAsync(string uid, string gameMode, int grade, int levelNumber, int score)
    {
        try
        {
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng để cập nhật level progress.");
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string levelProgressId = $"{uid}_{gameMode}_{grade}_{levelNumber}";

            // 1. Cập nhật/Tạo levelProgress
            var levelProgressRef = firestore.Collection("levelProgress").Document(levelProgressId);
            var levelSnapshot = await levelProgressRef.GetSnapshotAsync();

            if (levelSnapshot.Exists)
            {
                // Level đã chơi rồi → Cập nhật nếu điểm cao hơn
                var data = levelSnapshot.ToDictionary();
                int currentBestScore = GetInt(data, "bestScore", 0);
                int currentAttempts = GetInt(data, "attempts", 0);

                var updateData = new Dictionary<string, object>
                {
                    { "attempts", currentAttempts + 1 }
                };

                if (score > currentBestScore)
                {
                    updateData["bestScore"] = score;
                    Debug.Log($"[Firebase] 🎉 Điểm mới cao hơn! {currentBestScore} → {score}");
                }

                await levelProgressRef.UpdateAsync(updateData);
            }
            else
            {
                // Lần đầu chơi level này → Tạo mới
                var newLevelProgress = new Dictionary<string, object>
                {
                    { "progressId", levelProgressId },
                    { "uid", uid },
                    { "gameMode", gameMode },
                    { "grade", grade },
                    { "levelNumber", levelNumber },
                    { "bestScore", score },
                    { "attempts", 1 }
                };

                await levelProgressRef.SetAsync(newLevelProgress);
                Debug.Log($"[Firebase] ✅ Tạo levelProgress mới: {levelProgressId}");
            }

            // 2. Cập nhật gameModeProgress
            string progressId = $"{uid}_{gameMode}_{grade}";
            var progressRef = firestore.Collection("gameModeProgress").Document(progressId);
            var progressSnapshot = await progressRef.GetSnapshotAsync();

            if (progressSnapshot.Exists)
            {
                var progressData = progressSnapshot.ToDictionary();
                int currentMaxUnlocked = GetInt(progressData, "maxLevelUnlocked", 1);
                int currentTotalScore = GetInt(progressData, "totalScore", 0);
                int currentBestScore = GetInt(progressData, "bestScore", 0);

                var updateProgress = new Dictionary<string, object>
                {
                    { "currentLevel", levelNumber },
                    { "totalScore", currentTotalScore + score },
                    { "lastPlayed", now }
                };

                // Mở khóa level tiếp theo nếu hoàn thành level hiện tại
                if (levelNumber >= currentMaxUnlocked && levelNumber < 100)
                {
                    updateProgress["maxLevelUnlocked"] = levelNumber + 1;
                    Debug.Log($"[Firebase] 🔓 Mở khóa level {levelNumber + 1}");
                }

                // Cập nhật bestScore nếu cao hơn
                if (score > currentBestScore)
                {
                    updateProgress["bestScore"] = score;
                }

                await progressRef.UpdateAsync(updateProgress);
                Debug.Log($"[Firebase] ✅ Cập nhật gameModeProgress: {progressId}");
            }

            // 3. Cập nhật playerData (tổng điểm)
            await UpdatePlayerStatsAsync(uid, score, score / 10); // XP = score / 10

            Debug.Log($"[Firebase] ✅ Hoàn thành cập nhật tiến độ level {levelNumber}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi cập nhật level progress: {ex.Message}");
        }
    }

    /// <summary>
    /// Kiểm tra level có được mở khóa không
    /// </summary>
    public async Task<bool> IsLevelUnlockedAsync(string uid, string gameMode, int grade, int levelNumber)
    {
        try
        {
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng.");
                return false;
            }

            string progressId = $"{uid}_{gameMode}_{grade}";
            var snapshot = await firestore.Collection("gameModeProgress").Document(progressId).GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning($"[Firebase] ⚠️ Không tìm thấy gameModeProgress: {progressId}");
                return false;
            }

            var data = snapshot.ToDictionary();
            int maxLevelUnlocked = GetInt(data, "maxLevelUnlocked", 1);

            bool isUnlocked = levelNumber <= maxLevelUnlocked;
            Debug.Log($"[Firebase] 🔍 Level {levelNumber} unlocked: {isUnlocked} (max: {maxLevelUnlocked})");
            
            return isUnlocked;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi kiểm tra level unlock: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load tiến độ chế độ game (để hiển thị màn hình chọn level)
    /// </summary>
    public async Task<GameModeProgressData> LoadGameModeProgressAsync(string uid, string gameMode, int grade)
    {
        try
        {
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng.");
                return null;
            }

            string progressId = $"{uid}_{gameMode}_{grade}";
            var snapshot = await firestore.Collection("gameModeProgress").Document(progressId).GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning($"[Firebase] ⚠️ Không tìm thấy gameModeProgress: {progressId}");
                return null;
            }

            var data = snapshot.ToDictionary();
            var progressData = new GameModeProgressData
            {
                progressId = progressId,
                uid = uid,
                gameMode = gameMode,
                grade = grade,
                currentLevel = GetInt(data, "currentLevel", 1),
                maxLevelUnlocked = GetInt(data, "maxLevelUnlocked", 1),
                totalScore = GetInt(data, "totalScore", 0),
                bestScore = GetInt(data, "bestScore", 0),
                lastPlayed = GetLong(data, "lastPlayed", 0)
            };

            Debug.Log($"[Firebase] ✅ Load gameModeProgress: {progressId} (maxUnlocked: {progressData.maxLevelUnlocked})");
            return progressData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi load gameModeProgress: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load điểm từng level (để hiển thị trên màn hình chọn level)
    /// </summary>
    public async Task<Dictionary<int, LevelProgressData>> LoadLevelScoresAsync(string uid, string gameMode, int grade)
    {
        try
        {
            if (firestore == null)
            {
                Debug.LogWarning("[Firebase] ⚠️ Firestore chưa sẵn sàng.");
                return new Dictionary<int, LevelProgressData>();
            }

            var query = firestore.Collection("levelProgress")
                .WhereEqualTo("uid", uid)
                .WhereEqualTo("gameMode", gameMode)
                .WhereEqualTo("grade", grade);

            var snapshot = await query.GetSnapshotAsync();
            var levelScores = new Dictionary<int, LevelProgressData>();

            foreach (var doc in snapshot.Documents)
            {
                var data = doc.ToDictionary();
                int levelNumber = GetInt(data, "levelNumber", 0);
                
                var levelData = new LevelProgressData
                {
                    progressId = doc.Id,
                    uid = uid,
                    gameMode = gameMode,
                    grade = grade,
                    levelNumber = levelNumber,
                    bestScore = GetInt(data, "bestScore", 0),
                    attempts = GetInt(data, "attempts", 0)
                };

                levelScores[levelNumber] = levelData;
            }

            Debug.Log($"[Firebase] ✅ Load {levelScores.Count} level scores cho {gameMode} lớp {grade}");
            return levelScores;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Firebase] ❌ Lỗi load level scores: {ex.Message}");
            return new Dictionary<int, LevelProgressData>();
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

/// <summary>
/// Dữ liệu tiến độ chế độ game (gameModeProgress)
/// </summary>
[System.Serializable]
public class GameModeProgressData
{
    public string progressId;
    public string uid;
    public string gameMode;         // chonda, keothada, phithuyen
    public int grade;               // 1-5
    public int currentLevel;        // Level đang chơi
    public int maxLevelUnlocked;    // Level đã mở khóa
    public int totalScore;          // Tổng điểm chế độ này
    public int bestScore;           // Điểm cao nhất 1 level
    public long lastPlayed;         // Timestamp chơi gần nhất (0 = chưa chơi)
}

/// <summary>
/// Dữ liệu điểm từng level (levelProgress)
/// </summary>
[System.Serializable]
public class LevelProgressData
{
    public string progressId;
    public string uid;
    public string gameMode;         // chonda, keothada, phithuyen
    public int grade;               // 1-5
    public int levelNumber;         // 1-100
    public int bestScore;           // Điểm cao nhất
    public int attempts;            // Số lần chơi
}