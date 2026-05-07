using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Singleton quản lý avatar hiện tại của người chơi.
/// - Load tất cả AvatarData từ Resources/Avatars/
/// - Lưu/đọc selection qua PlayerPrefs ("SelectedAvatarID")
/// - Sync lên Firebase Realtime Database khi đã đăng nhập
/// - Hoạt động cho cả guest lẫn logged-in user
/// </summary>
public class AvatarManager : MonoBehaviour
{
    public static AvatarManager Instance { get; private set; }

    private const string PREFS_KEY = "SelectedAvatarID";

    private AvatarData[] allAvatars;
    private AvatarData currentAvatar;

    // Event — các UI subscribe để tự refresh khi avatar thay đổi
    public System.Action<AvatarData> OnAvatarChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAvatars();
    }

    // ─────────────────────────────────────────────────────────────
    // LOAD
    // ─────────────────────────────────────────────────────────────

    private void LoadAvatars()
    {
        allAvatars = Resources.LoadAll<AvatarData>("Avatars");

        if (allAvatars == null || allAvatars.Length == 0)
        {
            Debug.LogWarning("[AvatarManager] ⚠️ Không tìm thấy AvatarData trong Resources/Avatars/");
            return;
        }

        // Sắp xếp theo avatarId để thứ tự nhất quán
        System.Array.Sort(allAvatars, (a, b) => a.avatarId.CompareTo(b.avatarId));

        int savedId = PlayerPrefs.GetInt(PREFS_KEY, 0);
        currentAvatar = GetById(savedId) ?? GetDefault();

        Debug.Log($"[AvatarManager] ✅ Loaded {allAvatars.Length} avatars. Current: {currentAvatar?.avatarName} (id={currentAvatar?.avatarId})");
    }

    // ─────────────────────────────────────────────────────────────
    // GETTERS
    // ─────────────────────────────────────────────────────────────

    public AvatarData[] GetAllAvatars() => allAvatars;

    public AvatarData GetCurrentAvatar() => currentAvatar;

    public int GetCurrentAvatarId() => currentAvatar?.avatarId ?? 0;

    public Sprite GetCurrentThumbnail() => currentAvatar?.thumbnail;

    public Sprite GetCurrentFullAvatar() => currentAvatar?.fullAvatar;

    public RuntimeAnimatorController GetCurrentAnimatorController()
        => currentAvatar?.animatorController;

    public AvatarData GetById(int id)
        => allAvatars?.FirstOrDefault(a => a.avatarId == id);

    private AvatarData GetDefault()
        => allAvatars?.FirstOrDefault(a => a.isDefault) ?? allAvatars?.FirstOrDefault();

    // ─────────────────────────────────────────────────────────────
    // SELECT
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Chọn avatar mới — lưu local ngay lập tức, fire event để UI tự refresh.
    /// Gọi SyncToFirebaseAsync() riêng nếu cần sync lên server.
    /// </summary>
    public void SelectAvatar(int avatarId)
    {
        var avatar = GetById(avatarId);
        if (avatar == null)
        {
            Debug.LogWarning($"[AvatarManager] AvatarId {avatarId} không tồn tại.");
            return;
        }

        currentAvatar = avatar;
        PlayerPrefs.SetInt(PREFS_KEY, avatarId);
        PlayerPrefs.Save();

        OnAvatarChanged?.Invoke(currentAvatar);
        Debug.Log($"[AvatarManager] ✅ Đã chọn: {avatar.avatarName} (id={avatarId})");
    }

    // ─────────────────────────────────────────────────────────────
    // FIREBASE SYNC
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Sync avatarId lên Firestore (users/{uid}/avatarId).
    /// Gọi sau SelectAvatar() khi người dùng đã đăng nhập bằng email.
    /// Không block UI — lỗi chỉ log warning.
    /// </summary>
    public async Task SyncToFirebaseAsync(int avatarId)
    {
        try
        {
            string uid = AuthManager.Instance?.GetCurrentUser()?.UserId;
            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogWarning("[AvatarManager] ⚠️ Không có uid để sync Firebase.");
                return;
            }

            var fs = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
            if (fs == null)
            {
                Debug.LogWarning("[AvatarManager] ⚠️ Firestore chưa sẵn sàng.");
                return;
            }

            var update = new System.Collections.Generic.Dictionary<string, object>
            {
                { "avatarId", avatarId }
            };
            await fs.Collection("users").Document(uid)
                    .SetAsync(update, Firebase.Firestore.SetOptions.MergeAll);

            Debug.Log($"[AvatarManager] ✅ Firestore synced: avatarId={avatarId}");
        }
        catch (System.Exception ex)
        {
            // Không block UI — local đã lưu rồi
            Debug.LogWarning($"[AvatarManager] ⚠️ Firestore sync thất bại (local đã lưu): {ex.Message}");
        }
    }

    /// <summary>
    /// Restore avatarId từ Firebase khi login/auto-login.
    /// Gọi từ CloudSyncService.RestoreProgressFromFirebase() hoặc AuthManager.
    /// </summary>
    public void RestoreFromFirebase(int avatarId)
    {
        var avatar = GetById(avatarId);
        if (avatar == null)
        {
            Debug.LogWarning($"[AvatarManager] ⚠️ RestoreFromFirebase: avatarId={avatarId} không tồn tại.");
            return;
        }

        currentAvatar = avatar;
        PlayerPrefs.SetInt(PREFS_KEY, avatarId);
        PlayerPrefs.Save();

        OnAvatarChanged?.Invoke(currentAvatar);
        Debug.Log($"[AvatarManager] 🔄 Restored từ Firebase: {avatar.avatarName} (id={avatarId})");
    }
}
