using System.Collections;
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

    // ── Session Guard ──────────────────────────────────────────────
    private const string FIELD_SESSION_ID      = "activeSessionId";
    private const float  SESSION_POLL_INTERVAL = 5f; // Kiểm tra mỗi 5 giây
    private string  mySessionId;
    private string  guardedUid;
    private bool    isGuarding;
    private bool    kickHandled;
    private Coroutine sessionPollRoutine;
    // ──────────────────────────────────────────────────────────────
    
    // Events
    public System.Action<PlayerData> OnLoginDataLoaded;
    public System.Action<Firebase.Auth.FirebaseUser> OnCurrentUserChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Auth] Duplicate AuthManager detected, disabling duplicate component.");
            enabled = false;
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

        // Tự động tạo FirebaseManager nếu chưa có
        Debug.Log("[Auth] 🔧 Tự động tạo FirebaseManager...");
        
        // Tìm AuthServices GameObject để gắn vào (nếu có)
        GameObject authServicesObj = GameObject.Find("AuthServices");
        if (authServicesObj != null)
        {
            var fm = authServicesObj.AddComponent<FirebaseManager>();
            Debug.Log("[Auth] ✅ Đã thêm FirebaseManager vào AuthServices");
            return fm;
        }
        
        // Nếu không có AuthServices, tạo GameObject mới
        var go = new GameObject("FirebaseManager");
        var firebaseManager = go.AddComponent<FirebaseManager>();
        Debug.Log("[Auth] ✅ Đã tạo FirebaseManager GameObject mới");
        return firebaseManager;
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
                CacheCurrentPlayerDataLocal();
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

        CacheCurrentPlayerDataLocal();
        OnLoginDataLoaded?.Invoke(currentPlayerData);

        if (sessionManager != null && !sessionManager.IsSessionValid())
        {
            sessionManager.SaveSession(uid, user.Email);
        }

        // Restore tiến độ từ Firebase (kể cả auto-login)
        var cloudSyncAuto = DoAnGame.Auth.CloudSyncService.Instance;
        if (cloudSyncAuto != null)
        {
            _ = cloudSyncAuto.RestoreProgressFromFirebase();
            Debug.Log("[Auth] 🔄 Auto-login: đang restore tiến độ từ Firebase...");
        }

        Debug.Log($"[Auth] ✅ Auto-load account thành công: {currentPlayerData.characterName}");
        NotifyCurrentUserChanged();
        return true;
    }

    /// <summary>
    /// Đăng ký tài khoản với tên nhân vật và lớp học
    /// </summary>
    public async Task<bool> Register(string email, string password, string characterName, int grade)
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

        bool success = await firebaseManager.RegisterAsync(email, password, characterName, grade);
        
        if (success)
        {
            var user = await GetCurrentUserWithRetry();
            string uid = user != null ? user.UserId : null;

            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogWarning("[Auth] ⚠️ Register thành công nhưng CurrentUser chưa sẵn sàng. Dùng dữ liệu mặc định tạm thời.");
                currentPlayerData = CreateDefaultPlayerData("unknown", characterName);
                CacheCurrentPlayerDataLocal();
                OnLoginDataLoaded?.Invoke(currentPlayerData);
                NotifyCurrentUserChanged();
                return true;
            }

            if (!firebaseManager.IsPlayerDataSyncEnabled())
            {
                currentPlayerData = CreateDefaultPlayerData(uid, characterName);
                CacheCurrentPlayerDataLocal();
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

                CacheCurrentPlayerDataLocal();
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
                CacheCurrentPlayerDataLocal();
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

                CacheCurrentPlayerDataLocal();
                OnLoginDataLoaded?.Invoke(currentPlayerData);
            }
            
            // Lưu session(24h expiry)
            if (sessionManager != null)
            {
                sessionManager.SaveSession(user.UserId, email);
                Debug.Log("[Auth] 💾 Session saved (24h)");
            }

            // Restore tiến độ single-player và coins từ Firebase về máy này
            var cloudSync = DoAnGame.Auth.CloudSyncService.Instance;
            if (cloudSync != null)
            {
                _ = cloudSync.RestoreProgressFromFirebase();
                Debug.Log("[Auth] 🔄 Đang restore tiến độ từ Firebase...");
            }

            // Bắt đầu bảo vệ phiên (kick nếu máy khác đăng nhập cùng tài khoản)
            _ = StartSessionGuard(user.UserId);

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

            CacheCurrentPlayerDataLocal();
            
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

        // Dừng bảo vệ phiên
        StopSessionGuard();

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

    private void CacheCurrentPlayerDataLocal()
    {
        if (currentPlayerData == null)
            return;

        if (playerDataService == null)
            return;

        playerDataService.SavePlayerDataLocal(currentPlayerData);
    }

    private void ClearAllKnownLocalAuthKeys()    {
        // ── Auth keys ──────────────────────────────────────────────
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

        // ── Game progress keys — phải xóa khi đổi tài khoản ───────
        // Nếu không xóa, acc mới sẽ thấy dữ liệu của acc cũ
        PlayerPrefs.DeleteKey("UserScore");
        PlayerPrefs.DeleteKey("UserLevel");
        PlayerPrefs.DeleteKey("TotalCoins");
        PlayerPrefs.DeleteKey("Class_HighestLevel");
        PlayerPrefs.DeleteKey("HighestLevelReached");
        PlayerPrefs.DeleteKey("Space_HighestLevel");

        // ── Guest data ─────────────────────────────────────────────
        PlayerPrefs.DeleteKey("GuestPlayerName");
        PlayerPrefs.DeleteKey("IsGuestMode");
        PlayerPrefs.DeleteKey("SelectedGrade");

        // ── Shop / Skin keys ───────────────────────────────────────
        PlayerPrefs.DeleteKey("SelectedClassSkinID");
        PlayerPrefs.DeleteKey("SelectedSkinID");
        PlayerPrefs.DeleteKey("SelectedPhaoID");
        PlayerPrefs.DeleteKey("SelectedShipID");

        // Xóa unlock flags (tối đa 10 skin mỗi loại là đủ)
        for (int i = 1; i <= 10; i++)
        {
            PlayerPrefs.DeleteKey("ClassSkinUnlocked" + i);
            PlayerPrefs.DeleteKey("SkinUnlocked_" + i);
            PlayerPrefs.DeleteKey("PhaoUnlocked_" + i);
            PlayerPrefs.DeleteKey("ShipUnlocked_" + i);
        }

        PlayerPrefs.Save();
        Debug.Log("[Auth] 🗑️ Đã xóa toàn bộ local data của tài khoản cũ.");
    }

    // ─────────────────────────────────────────────────────────────
    // SESSION GUARD — chỉ cho phép 1 thiết bị đăng nhập cùng lúc
    // ─────────────────────────────────────────────────────────────

    private async Task StartSessionGuard(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return;

        StopSessionGuard();

        guardedUid  = uid;
        mySessionId = DoAnGame.Auth.RuntimeInstanceContext.InstanceId;
        kickHandled = false;

        // Ghi sessionId lên Firestore — máy cũ sẽ phát hiện bị kick
        await WriteSessionIdAsync(uid, mySessionId);

        isGuarding = true;
        if (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            sessionPollRoutine = StartCoroutine(SessionPollRoutine());
        }

        string uidP = uid.Length > 8 ? uid[..8] : uid;
        string sidP = mySessionId.Length > 12 ? mySessionId[..12] : mySessionId;
        Debug.Log($"[Auth] 🔒 Session guard started: uid={uidP}... sid={sidP}...");
    }

    private void StopSessionGuard()
    {
        isGuarding  = false;
        kickHandled = false;

        if (sessionPollRoutine != null)
        {
            StopCoroutine(sessionPollRoutine);
            sessionPollRoutine = null;
        }

        guardedUid  = null;
        mySessionId = null;
    }

    private async Task WriteSessionIdAsync(string uid, string sessionId)
    {
        try
        {
            var fs = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
            if (fs == null) return;

            var update = new System.Collections.Generic.Dictionary<string, object>
            {
                { FIELD_SESSION_ID, sessionId }
            };
            await fs.Collection("users").Document(uid).SetAsync(update, Firebase.Firestore.SetOptions.MergeAll);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Auth] ⚠️ Không ghi được sessionId: {ex.Message}");
        }
    }

    private IEnumerator SessionPollRoutine()
    {
        while (isGuarding)
        {
            yield return new WaitForSeconds(SESSION_POLL_INTERVAL);
            if (!isGuarding) yield break;
            _ = CheckSessionAsync();
        }
    }

    private async Task CheckSessionAsync()
    {
        if (!isGuarding || string.IsNullOrEmpty(guardedUid)) return;

        try
        {
            var fs = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
            if (fs == null) return;

            var snap = await fs.Collection("users").Document(guardedUid).GetSnapshotAsync();
            if (!snap.Exists) return;

            var d = snap.ToDictionary();
            string firestoreSid = d.ContainsKey(FIELD_SESSION_ID) ? d[FIELD_SESSION_ID]?.ToString() : null;

            if (string.IsNullOrEmpty(firestoreSid)) return;

            if (firestoreSid != mySessionId)
            {
                Debug.LogWarning("[Auth] ⚠️ Phát hiện đăng nhập từ thiết bị khác!");
                HandleSessionKicked();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Auth] ⚠️ Lỗi kiểm tra session: {ex.Message}");
        }
    }

    private void HandleSessionKicked()
    {
        if (kickHandled) return;
        kickHandled = true;

        StopSessionGuard();

        // Kiểm tra xem có đang trong trận multiplayer không
        // Nếu có → đợi trận kết thúc rồi mới kick (tránh crash NGO)
        bool inMultiplayerBattle = IsInMultiplayerBattle();

        if (inMultiplayerBattle)
        {
            Debug.LogWarning("[Auth] ⚠️ Bị kick nhưng đang trong trận multiplayer — sẽ đăng xuất sau khi trận kết thúc.");
            StartCoroutine(WaitForBattleEndThenKick());
            return;
        }

        // Không trong trận → kick ngay
        ExecuteKick();
    }

    private bool IsInMultiplayerBattle()
    {
        // Kiểm tra NetworkManager đang chạy (đang trong multiplayer session)
        var nm = Unity.Netcode.NetworkManager.Singleton;
        return nm != null && nm.IsListening;
    }

    private IEnumerator WaitForBattleEndThenKick()
    {
        // Hiện thông báo nhỏ không chặn gameplay
        var warningOverlay = CreateKickedWarningOverlay();

        // Đợi cho đến khi trận kết thúc (NetworkManager ngừng lắng nghe)
        while (IsInMultiplayerBattle())
        {
            yield return new UnityEngine.WaitForSeconds(5f);
        }

        Debug.Log("[Auth] ✅ Trận kết thúc, tiến hành đăng xuất.");

        if (warningOverlay != null) Destroy(warningOverlay);
        ExecuteKick();
    }

    private void ExecuteKick()
    {
        // Shutdown NGO trước để tránh crash khi reload scene
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            Debug.Log("[Auth] 🔌 Shutdown NetworkManager trước khi kick...");
            nm.Shutdown();
        }

        Logout();

        if (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            StartCoroutine(ShowKickedOverlayAndReload());
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// Overlay cảnh báo nhỏ ở góc màn hình — không chặn gameplay.
    /// Hiển thị khi đang trong trận và bị kick.
    /// </summary>
    private UnityEngine.GameObject CreateKickedWarningOverlay()
    {
        try
        {
            var go     = new UnityEngine.GameObject("[KickedWarning]");
            var canvas = go.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode   = UnityEngine.RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9998;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();

            var textGo = new UnityEngine.GameObject("Msg");
            textGo.transform.SetParent(go.transform, false);
            var text   = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            text.text      = "⚠️ Tài khoản đăng nhập từ thiết bị khác.\nSẽ đăng xuất sau khi trận kết thúc.";
            text.fontSize  = 20;
            text.alignment = TMPro.TextAlignmentOptions.BottomRight;
            text.color     = new UnityEngine.Color(1f, 0.8f, 0f, 1f); // Vàng

            var textRect = textGo.GetComponent<UnityEngine.RectTransform>();
            textRect.anchorMin = UnityEngine.Vector2.zero;
            textRect.anchorMax = UnityEngine.Vector2.one;
            textRect.offsetMin = new UnityEngine.Vector2(10f, 10f);
            textRect.offsetMax = new UnityEngine.Vector2(-10f, -10f);

            return go;
        }
        catch
        {
            return null;
        }
    }

    private IEnumerator ShowKickedOverlayAndReload()
    {
        var overlay = CreateKickedOverlay();
        yield return new WaitForSecondsRealtime(3f);
        if (overlay != null) Destroy(overlay);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private UnityEngine.GameObject CreateKickedOverlay()
    {
        try
        {
            var go     = new UnityEngine.GameObject("[KickedOverlay]");
            var canvas = go.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode   = UnityEngine.RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var bgGo  = new UnityEngine.GameObject("BG");
            bgGo.transform.SetParent(go.transform, false);
            var bgImg = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new UnityEngine.Color(0f, 0f, 0f, 0.85f);
            var bgRect  = bgGo.GetComponent<UnityEngine.RectTransform>();
            bgRect.anchorMin = bgRect.offsetMin = UnityEngine.Vector2.zero;
            bgRect.anchorMax = UnityEngine.Vector2.one;
            bgRect.offsetMax = UnityEngine.Vector2.zero;

            var textGo = new UnityEngine.GameObject("Msg");
            textGo.transform.SetParent(go.transform, false);
            var text   = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            text.text      = "⚠️ Tài khoản của bạn\nvừa được đăng nhập\ntừ thiết bị khác.\n\nĐang đăng xuất...";
            text.fontSize  = 36;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color     = UnityEngine.Color.white;
            var textRect   = textGo.GetComponent<UnityEngine.RectTransform>();
            textRect.anchorMin = textRect.offsetMin = UnityEngine.Vector2.zero;
            textRect.anchorMax = UnityEngine.Vector2.one;
            textRect.offsetMax = UnityEngine.Vector2.zero;

            return go;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Auth] Không tạo được overlay: {ex.Message}");
            return null;
        }
    }
}
