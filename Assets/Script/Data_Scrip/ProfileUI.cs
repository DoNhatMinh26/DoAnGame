using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Auth;
using DoAnGame.UI;

/// <summary>
/// Profile UI — hiển thị thống kê người chơi + 2 tính năng mới:
///   1. Xoá tài khoản (chỉ khi đã đăng nhập bằng email)
///   2. Đổi độ khó (lớp 1–5) — reset tiến độ 3 chế độ về màn 1, giữ level/điểm/tiền
/// </summary>
public class ProfileUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // FIELDS — UI Thông tin người chơi (hiện có)
    // ─────────────────────────────────────────────────────────────

    [Header("UI Thông tin người chơi")]
    public TextMeshProUGUI levelUserTxt;
    public TextMeshProUGUI diemUserTxt;
    public TextMeshProUGUI totalCoinTxt;
    public TextMeshProUGUI currentGradeTxt;

    [Header("UI Tiến độ các chế độ")]
    public TextMeshProUGUI lopHocLevelTxt;
    public TextMeshProUGUI phongThuLevelTxt;
    public TextMeshProUGUI phiThuyenLevelTxt;

    [Header("Status Text (thông báo kết quả)")]
    public TextMeshProUGUI statusText;

    // ─────────────────────────────────────────────────────────────
    // FIELDS — Chọn Avatar / Nhân Vật
    // ─────────────────────────────────────────────────────────────

    [Header("Chọn Avatar / Nhân Vật")]
    public Image avatarDisplayImage;            // AvatarSection/AvatarImage — ảnh lớn đang chọn
    public Button chonNhanVatBtn;               // AvatarSection/ChonNhanVatBtn
    public GameObject avatarSelectionPopup;     // AvatarSelectionPopup (Canvas ScreenSpaceOverlay)
    public Transform avatarItemContainer;       // Content bên trong ScrollView của popup
    public GameObject avatarItemPrefab;         // Prefab AvatarItem
    public Button closeAvatarPopupBtn;          // Nút Đóng trong popup

    // ─────────────────────────────────────────────────────────────
    // FIELDS — Xoá Tài Khoản
    // ─────────────────────────────────────────────────────────────

    [Header("Xoá Tài Khoản")]
    public Button deleteAccountBtn;
    public GameObject deleteAccountPopup;
    public Button confirmDeleteBtn;
    public Button cancelDeleteAccountBtn;

    // ─────────────────────────────────────────────────────────────
    // FIELDS — Đổi Độ Khó
    // ─────────────────────────────────────────────────────────────

    [Header("Đổi Độ Khó (Lớp học)")]
    public TMP_Dropdown gradeDropdown;
    public Button applyDifficultyBtn;
    public GameObject changeDifficultyPopup;
    public TextMeshProUGUI changeDifficultyMessageText;
    public Button confirmChangeBtn;
    public Button cancelChangeDifficultyBtn;

    // ─────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    private bool isBusy = false;
    private int pendingGrade = -1;

    // ─────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private bool isStarted = false;

    private void Start()
    {
        // Gán sự kiện — Chọn avatar
        if (chonNhanVatBtn != null)
            chonNhanVatBtn.onClick.AddListener(HandleOpenAvatarSelection);
        if (closeAvatarPopupBtn != null)
            closeAvatarPopupBtn.onClick.AddListener(HideAvatarSelectionPopup);
        avatarSelectionPopup?.SetActive(false);

        // Subscribe AvatarManager event để tự refresh khi avatar thay đổi từ nơi khác
        if (AvatarManager.Instance != null)
            AvatarManager.Instance.OnAvatarChanged += OnAvatarChangedExternal;

        // Gán sự kiện — Xoá tài khoản
        if (deleteAccountBtn != null)
            deleteAccountBtn.onClick.AddListener(HandleDeleteAccountClick);
        if (confirmDeleteBtn != null)
            confirmDeleteBtn.onClick.AddListener(() => _ = HandleConfirmDeleteAsync());
        if (cancelDeleteAccountBtn != null)
            cancelDeleteAccountBtn.onClick.AddListener(HideDeletePopup);

        // Gán sự kiện — Đổi độ khó
        if (applyDifficultyBtn != null)
            applyDifficultyBtn.onClick.AddListener(HandleApplyDifficulty);
        if (confirmChangeBtn != null)
            confirmChangeBtn.onClick.AddListener(() => _ = HandleConfirmChangeDifficultyAsync());
        if (cancelChangeDifficultyBtn != null)
            cancelChangeDifficultyBtn.onClick.AddListener(HideChangeDifficultyPopupAndReset);

        // Ẩn popup — đảm bảo trạng thái sạch ngay từ đầu
        deleteAccountPopup?.SetActive(false);
        changeDifficultyPopup?.SetActive(false);

        // Reset state
        isBusy = false;
        pendingGrade = -1;
        isStarted = true;

        // Xoá status text
        SetStatusText(string.Empty);

        // Subscribe AuthManager event — tự refresh khi login/restore hoàn tất
        if (AuthManager.Instance != null)
            AuthManager.Instance.OnLoginDataLoaded += OnLoginDataRestored;

        // Refresh lần đầu (OnEnable có thể đã chạy trước Start)
        UpdateProfileDisplay();
        RefreshDeleteButtonVisibility();
        SyncDropdownToCurrentGrade();
    }

    private void OnDestroy()
    {
        // Unsubscribe để tránh memory leak
        if (AuthManager.Instance != null)
            AuthManager.Instance.OnLoginDataLoaded -= OnLoginDataRestored;
        if (AvatarManager.Instance != null)
            AvatarManager.Instance.OnAvatarChanged -= OnAvatarChangedExternal;
    }

    /// <summary>
    /// Callback khi AuthManager hoàn tất login + restore từ Firebase.
    /// Đảm bảo Profile luôn hiển thị grade đúng dù có race condition.
    /// </summary>
    private void OnLoginDataRestored(DoAnGame.Auth.PlayerData playerData)
    {
        // Chạy trên main thread — safe để update UI
        UpdateProfileDisplay();
        RefreshDeleteButtonVisibility();
        SyncDropdownToCurrentGrade();
        Debug.Log($"[ProfileUI] 🔄 Refreshed sau login restore. Grade: {UIManager.SelectedGrade}");
    }

    private void OnEnable()
    {
        // Nếu Start chưa chạy (lần đầu tiên), bỏ qua — Start sẽ xử lý
        if (!isStarted) return;

        // Đảm bảo popup luôn ẩn khi mở lại Profile
        deleteAccountPopup?.SetActive(false);
        changeDifficultyPopup?.SetActive(false);
        avatarSelectionPopup?.SetActive(false);

        // Reset busy state phòng trường hợp bị kẹt
        isBusy = false;
        pendingGrade = -1;

        UpdateProfileDisplay();
        RefreshDeleteButtonVisibility();
        SyncDropdownToCurrentGrade();
        SetStatusText(string.Empty);
    }

    // ─────────────────────────────────────────────────────────────
    // DISPLAY
    // ─────────────────────────────────────────────────────────────

    public void UpdateProfileDisplay()
    {
        // ✅ Dùng LocalStorageKeyResolver để đọc đúng key (có prefix)
        bool isGuest = DoAnGame.UI.UIQuickPlayNameController.IsGuestMode();
        string scoreKey = isGuest
            ? DoAnGame.Auth.LocalStorageKeyResolver.LocalGuestScore
            : DoAnGame.Auth.LocalStorageKeyResolver.UserScore;
        string levelKey = isGuest
            ? DoAnGame.Auth.LocalStorageKeyResolver.LocalGuestLevel
            : DoAnGame.Auth.LocalStorageKeyResolver.UserLevel;
        
        // ✅ DEBUG: Log exact keys being read
        Debug.Log($"[ProfileUI] ========== UPDATE PROFILE DISPLAY ==========");
        Debug.Log($"[ProfileUI] IsGuestMode: {isGuest}");
        Debug.Log($"[ProfileUI] ScoreKey: '{scoreKey}'");
        Debug.Log($"[ProfileUI] LevelKey: '{levelKey}'");
        Debug.Log($"[ProfileUI] Reading score from PlayerPrefs...");
        
        int score = PlayerPrefs.GetInt(scoreKey, 0);
        int level = PlayerPrefs.GetInt(levelKey, 1);
        int coins = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.TotalCoins, 0);
        
        Debug.Log($"[ProfileUI] Score: {score}, Level: {level}, Coins: {coins}");
        Debug.Log($"[ProfileUI] ================================================");
        
        if (levelUserTxt != null)
            levelUserTxt.text = level.ToString();
        if (diemUserTxt != null)
            diemUserTxt.text = score.ToString();
        if (totalCoinTxt != null)
            totalCoinTxt.text = coins.ToString();
        if (currentGradeTxt != null)
            currentGradeTxt.text = UIManager.SelectedGrade.ToString();
        if (lopHocLevelTxt != null)
            lopHocLevelTxt.text = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.ClassHighest, 1).ToString();
        if (phongThuLevelTxt != null)
            phongThuLevelTxt.text = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.KeoThaHighest, 1).ToString();
        if (phiThuyenLevelTxt != null)
            phiThuyenLevelTxt.text = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SpaceHighest, 1).ToString();

        // Cập nhật ảnh avatar hiện tại
        RefreshAvatarDisplay();
    }

    private void SetStatusText(string message)
    {
        if (statusText != null)
            statusText.SetText(message);
    }

    // ─────────────────────────────────────────────────────────────
    // CHỌN AVATAR — Logic
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Cập nhật ảnh avatar hiển thị theo avatar đang chọn trong AvatarManager.
    /// </summary>
    private void RefreshAvatarDisplay()
    {
        if (avatarDisplayImage == null || AvatarManager.Instance == null) return;

        Sprite sprite = AvatarManager.Instance.GetCurrentFullAvatar();
        if (sprite != null)
            avatarDisplayImage.sprite = sprite;
    }

    /// <summary>
    /// Callback từ AvatarManager.OnAvatarChanged — tự refresh khi avatar thay đổi từ nơi khác.
    /// </summary>
    private void OnAvatarChangedExternal(AvatarData newAvatar)
    {
        RefreshAvatarDisplay();
    }

    /// <summary>
    /// Nhấn "Chọn Nhân Vật" → sinh danh sách avatar và mở popup.
    /// </summary>
    private void HandleOpenAvatarSelection()
    {
        if (isBusy || AvatarManager.Instance == null) return;

        BuildAvatarList();

        avatarSelectionPopup?.SetActive(true);
        avatarSelectionPopup?.transform.SetAsLastSibling();
        EnsureGraphicRaycaster(avatarSelectionPopup);
    }

    private void HideAvatarSelectionPopup()
    {
        avatarSelectionPopup?.SetActive(false);
    }

    /// <summary>
    /// Sinh lại toàn bộ danh sách AvatarItem trong popup.
    /// Xoá items cũ trước để tránh duplicate.
    /// </summary>
    private void BuildAvatarList()
    {
        if (avatarItemContainer == null || avatarItemPrefab == null)
        {
            Debug.LogWarning("[ProfileUI] avatarItemContainer hoặc avatarItemPrefab chưa được gán!");
            return;
        }

        // Xoá items cũ
        foreach (Transform child in avatarItemContainer)
            Destroy(child.gameObject);

        int currentId = AvatarManager.Instance.GetCurrentAvatarId();
        AvatarData[] allAvatars = AvatarManager.Instance.GetAllAvatars();

        if (allAvatars == null || allAvatars.Length == 0)
        {
            Debug.LogWarning("[ProfileUI] Không có AvatarData nào trong Resources/Avatars/");
            return;
        }

        foreach (AvatarData avatarData in allAvatars)
        {
            GameObject item = Instantiate(avatarItemPrefab, avatarItemContainer);
            AvatarItemUI itemUI = item.GetComponent<AvatarItemUI>();
            if (itemUI != null)
                itemUI.Setup(avatarData, avatarData.avatarId == currentId, OnAvatarItemSelected);
        }
    }

    /// <summary>
    /// Callback khi người dùng chọn 1 avatar trong danh sách.
    /// </summary>
    private void OnAvatarItemSelected(int avatarId)
    {
        if (AvatarManager.Instance == null) return;

        // Lưu local ngay lập tức (AvatarManager cũng fire OnAvatarChanged → RefreshAvatarDisplay)
        AvatarManager.Instance.SelectAvatar(avatarId);

        // Rebuild danh sách để cập nhật selectedIndicator
        BuildAvatarList();

        // Sync Firebase background (chỉ khi đã đăng nhập bằng email)
        bool isEmailUser = AuthManager.Instance != null && AuthManager.Instance.HasEmail();
        if (isEmailUser)
            _ = AvatarManager.Instance.SyncToFirebaseAsync(avatarId);

        // Cập nhật MainMenuPanel
        var mainMenu = FindObjectOfType<UIMainMenuController>(true);
        mainMenu?.UpdatePlayerInfo();

        HideAvatarSelectionPopup();

        string name = AvatarManager.Instance.GetCurrentAvatar()?.avatarName ?? "Nhân vật";
        SetStatusText($"Đã chọn: {name}");
    }

    // ─────────────────────────────────────────────────────────────
    // XOÁ TÀI KHOẢN — Logic
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Ẩn/hiện nút Xoá Tài Khoản tuỳ theo trạng thái đăng nhập.
    /// Chỉ hiện khi người chơi đăng nhập bằng email (không phải guest).
    /// </summary>
    private void RefreshDeleteButtonVisibility()
    {
        if (deleteAccountBtn == null) return;

        bool isEmailUser = AuthManager.Instance != null && AuthManager.Instance.HasEmail();
        deleteAccountBtn.gameObject.SetActive(isEmailUser);
    }

    /// <summary>
    /// Nhấn nút "Xoá Tài Khoản" → hiện popup xác nhận.
    /// </summary>
    private void HandleDeleteAccountClick()
    {
        if (isBusy) return;
        SetStatusText(string.Empty);
        deleteAccountPopup?.SetActive(true);
        deleteAccountPopup?.transform.SetAsLastSibling(); // Đưa lên trên cùng
        
        // ✅ FIX: Đảm bảo popup có GraphicRaycaster để click được button
        EnsureGraphicRaycaster(deleteAccountPopup);
    }

    private void HideDeletePopup()
    {
        deleteAccountPopup?.SetActive(false);
    }

    /// <summary>
    /// Nhấn "Xác nhận Xoá" trong popup → thực hiện xoá tài khoản.
    /// Flow: Xoá Firestore → Xoá Firebase Auth → Xoá local → Logout → WELCOMESCREEN
    /// </summary>
    private async Task HandleConfirmDeleteAsync()
    {
        if (isBusy) return;
        isBusy = true;
        HideDeletePopup();

        UILoadingIndicator.Instance?.Show("Đang xoá tài khoản...");

        try
        {
            // Lấy uid và firebaseUser TRƯỚC khi Logout (sau Logout sẽ null)
            string uid = AuthManager.Instance?.GetCurrentUser()?.UserId;
            var firebaseUser = AuthManager.Instance?.GetCurrentUser();

            // Bước 1: Xoá Firestore documents (không block nếu lỗi)
            if (!string.IsNullOrEmpty(uid))
            {
                await DeleteFirestoreDataAsync(uid);
            }

            // Bước 2: Xoá Firebase Auth user
            if (firebaseUser != null)
            {
                await firebaseUser.DeleteAsync();
            }

            // Bước 3: Xoá toàn bộ local data
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // Bước 4: Reset grade về mặc định
            UIManager.SelectedGrade = 1;

            // Bước 5: Logout + clear session/cache
            AuthManager.Instance?.Logout();

            UILoadingIndicator.Instance?.Hide();

            // Bước 6: Điều hướng về WELCOMESCREEN
            // Gọi sau cùng — Profile panel có thể bị ẩn bởi router
            NavigateToWelcomeScreen();
        }
        catch (Firebase.FirebaseException ex)
        {
            UILoadingIndicator.Instance?.Hide();
            isBusy = false;
            string msg = ex.Message ?? string.Empty;

            if (msg.Contains("requires-recent-login") || msg.Contains("CREDENTIAL_TOO_OLD_LOGIN_AGAIN"))
            {
                SetStatusText("Phiên đăng nhập đã hết hạn. Vui lòng đăng xuất và đăng nhập lại để xoá tài khoản.");
            }
            else
            {
                SetStatusText("Có lỗi xảy ra. Vui lòng thử lại.");
            }

            Debug.LogError($"[ProfileUI] Xoá tài khoản thất bại (Firebase): {ex.Message}");
        }
        catch (Exception ex)
        {
            UILoadingIndicator.Instance?.Hide();
            isBusy = false;
            SetStatusText("Mất kết nối. Kiểm tra mạng và thử lại.");
            Debug.LogError($"[ProfileUI] Xoá tài khoản thất bại: {ex.Message}");
        }
        // Không có finally reset isBusy ở đây vì nếu thành công
        // NavigateToWelcomeScreen sẽ ẩn panel này — isBusy không còn quan trọng
    }

    /// <summary>
    /// Xoá toàn bộ Firestore documents của user.
    /// Không throw — lỗi chỉ log warning để không block bước xoá Auth.
    /// </summary>
    private async Task DeleteFirestoreDataAsync(string uid)
    {
        Firebase.Firestore.FirebaseFirestore fs;
        try
        {
            fs = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        }
        catch
        {
            Debug.LogWarning("[ProfileUI] Firestore không khả dụng, bỏ qua xoá Firestore.");
            return;
        }

        if (fs == null) return;

        // playerData/{uid}
        await TryDeleteDocAsync(fs, "playerData", uid);

        // users/{uid}
        await TryDeleteDocAsync(fs, "users", uid);

        // gameModeProgress — 15 records (3 chế độ × 5 lớp)
        string[] modes  = { "chonda", "keothada", "phithuyen" };
        int[]    grades = { 1, 2, 3, 4, 5 };
        foreach (string mode in modes)
            foreach (int grade in grades)
                await TryDeleteDocAsync(fs, "gameModeProgress", $"{uid}_{mode}_{grade}");

        // playerShop — 4 records
        string[] shopTypes = { "chonda_skin", "keothada_skin", "keothada_phao", "phithuyen_ship" };
        foreach (string shopType in shopTypes)
            await TryDeleteDocAsync(fs, "playerShop", $"{uid}_{shopType}");

        Debug.Log("[ProfileUI] ✅ Đã xoá toàn bộ Firestore data");
    }

    private async Task TryDeleteDocAsync(Firebase.Firestore.FirebaseFirestore fs, string collection, string docId)
    {
        try
        {
            await fs.Collection(collection).Document(docId).DeleteAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ProfileUI] ⚠️ Xoá {collection}/{docId} thất bại (bỏ qua): {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ĐỔI ĐỘ KHÓ — Logic
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Đồng bộ Dropdown về lớp hiện tại của người chơi.
    /// Gọi mỗi khi Profile được bật.
    /// </summary>
    private void SyncDropdownToCurrentGrade()
    {
        if (gradeDropdown == null) return;

        int currentGrade = UIManager.SelectedGrade;
        if (currentGrade < 1 || currentGrade > 5) currentGrade = 1;

        // Dropdown index 0 = Lớp 1, index 4 = Lớp 5
        gradeDropdown.value = currentGrade - 1;
    }

    /// <summary>
    /// Nhấn nút "Áp Dụng" → kiểm tra grade có thay đổi không → hiện popup xác nhận.
    /// </summary>
    private void HandleApplyDifficulty()
    {
        if (isBusy || gradeDropdown == null) return;

        int selectedGrade = gradeDropdown.value + 1; // index 0 → Lớp 1
        int currentGrade  = UIManager.SelectedGrade;

        if (selectedGrade == currentGrade)
        {
            SetStatusText("Đây là lớp hiện tại của bạn.");
            return;
        }

        pendingGrade = selectedGrade;
        SetStatusText(string.Empty);

        // Cập nhật nội dung popup
        if (changeDifficultyMessageText != null)
        {
            changeDifficultyMessageText.SetText(
                $"Chuyển sang Lớp {selectedGrade}?\n\n" +
                $"Tiến độ các chế độ ChonDA, KeoThaDA, PhiThuyen sẽ được làm mới về Màn 1.\n\n" +
                $"Level, điểm và tiền của bạn vẫn được giữ nguyên."
            );
        }

        changeDifficultyPopup?.SetActive(true);
        changeDifficultyPopup?.transform.SetAsLastSibling(); // Đưa lên trên cùng
        
        // ✅ FIX: Đảm bảo popup có GraphicRaycaster để click được button
        EnsureGraphicRaycaster(changeDifficultyPopup);
    }

    private void HideChangeDifficultyPopup()
    {
        changeDifficultyPopup?.SetActive(false);
        // KHÔNG reset pendingGrade ở đây — HandleConfirmChangeDifficultyAsync cần nó
        // KHÔNG sync dropdown ở đây — sẽ sync sau khi grade thực sự được cập nhật
    }

    private void HideChangeDifficultyPopupAndReset()
    {
        changeDifficultyPopup?.SetActive(false);
        pendingGrade = -1;
        // Reset dropdown về grade hiện tại (người dùng nhấn Huỷ)
        SyncDropdownToCurrentGrade();
    }

    /// <summary>
    /// Nhấn "Xác nhận" trong popup đổi độ khó → thực hiện đổi lớp.
    /// - Cập nhật UIManager.SelectedGrade
    /// - Reset tiến độ 3 chế độ về màn 1 (local)
    /// - Sync Firebase nếu đã đăng nhập bằng email
    /// - Giữ nguyên level, điểm, tiền
    /// </summary>
    private async Task HandleConfirmChangeDifficultyAsync()
    {
        if (isBusy || pendingGrade < 1 || pendingGrade > 5) return;
        isBusy = true;

        int newGrade = pendingGrade;

        // Ẩn popup TRƯỚC khi cập nhật grade
        // (HideChangeDifficultyPopup không reset pendingGrade hay sync dropdown)
        HideChangeDifficultyPopup();

        // Bây giờ mới reset pendingGrade
        pendingGrade = -1;

        try
        {
            // Bước 1: Cập nhật grade
            UIManager.SelectedGrade = newGrade;
            DoAnGame.UI.UIQuickPlayNameController.SaveSelectedGrade(newGrade);

            // Bước 2: Reset tiến độ local (chỉ 3 chế độ, giữ score/level/coins)
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.ClassHighest, 1);
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.KeoThaHighest, 1);
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SpaceHighest, 1);
            PlayerPrefs.Save();

            // Bước 3: Sync Firebase (chỉ khi đã đăng nhập bằng email)
            bool isEmailUser = AuthManager.Instance != null && AuthManager.Instance.HasEmail();
            if (isEmailUser)
            {
                await SyncGradeChangeToFirebaseAsync(newGrade);
            }

            // Bước 4: Refresh toàn bộ UI Profile (grade mới đã được set ở Bước 1)
            UpdateProfileDisplay();
            SyncDropdownToCurrentGrade(); // Sync dropdown về grade mới
            
            // ✅ Bước 5: Refresh MainMenuPanel nếu nó đang hiển thị
            var mainMenuController = FindObjectOfType<UIMainMenuController>(true);
            if (mainMenuController != null)
            {
                mainMenuController.UpdatePlayerInfo();
                Debug.Log($"[ProfileUI] ✅ Refreshed MainMenuPanel after grade change");
            }

            SetStatusText($"Đã chuyển sang Lớp {newGrade}. Chúc bạn chơi vui!");
            Debug.Log($"[ProfileUI] ✅ Đổi độ khó thành công: Lớp {newGrade}");
        }
        catch (Exception ex)
        {
            SetStatusText("Có lỗi xảy ra khi đổi độ khó. Vui lòng thử lại.");
            Debug.LogError($"[ProfileUI] Đổi độ khó thất bại: {ex.Message}");
        }
        finally
        {
            isBusy = false;
        }
    }

    /// <summary>
    /// Sync grade mới lên Firebase:
    ///   - users/{uid}.grade = newGrade
    ///   - gameModeProgress/{uid}_{mode}_{newGrade} → reset về màn 1 (3 chế độ)
    /// Chỉ reset gameModeProgress của grade mới, các lớp khác giữ nguyên.
    /// </summary>
    private async Task SyncGradeChangeToFirebaseAsync(int newGrade)
    {
        Firebase.Firestore.FirebaseFirestore fs;
        try
        {
            fs = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        }
        catch
        {
            Debug.LogWarning("[ProfileUI] Firestore không khả dụng, bỏ qua sync grade.");
            return;
        }

        string uid = AuthManager.Instance?.GetCurrentUser()?.UserId;
        if (fs == null || string.IsNullOrEmpty(uid)) return;

        try
        {
            // Cập nhật grade trong users/{uid}
            var gradeUpdate = new Dictionary<string, object> { { "grade", newGrade } };
            await fs.Collection("users").Document(uid)
                    .SetAsync(gradeUpdate, Firebase.Firestore.SetOptions.MergeAll);

            // Reset gameModeProgress cho grade mới (3 chế độ)
            string[] modes = { "chonda", "keothada", "phithuyen" };
            foreach (string mode in modes)
            {
                string progressId = $"{uid}_{mode}_{newGrade}";
                var resetData = new Dictionary<string, object>
                {
                    { "currentLevel",     1 },
                    { "maxLevelUnlocked", 1 },
                    { "totalScore",       0 },
                    { "bestScore",        0 },
                    { "lastPlayed",       null }
                };
                await fs.Collection("gameModeProgress").Document(progressId)
                        .SetAsync(resetData, Firebase.Firestore.SetOptions.MergeAll);
            }

            Debug.Log($"[ProfileUI] ✅ Firebase synced: grade={newGrade}, gameModeProgress reset cho 3 chế độ");
        }
        catch (Exception ex)
        {
            // Không block UI — local đã cập nhật rồi, chỉ log warning
            Debug.LogWarning($"[ProfileUI] ⚠️ Firebase sync thất bại (local đã cập nhật): {ex.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // NAVIGATION
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Điều hướng về WELCOMESCREEN sau khi xoá tài khoản.
    /// Dùng UIScreenRouter (static) — không cần flowManager reference.
    /// </summary>
    private void NavigateToWelcomeScreen()
    {
        // Tìm WELCOMESCREEN trong GameUICanvas và hiển thị
        UIFlowManager flowManager = null;
        bool routed = UIScreenRouter.TryShow(ref flowManager, UIFlowManager.Screen.WelcomeAuth, false);

        if (!routed)
        {
            // Fallback: tìm trực tiếp theo tên
            GameObject welcomeScreen = GameObject.Find("WELCOMESCREEN");
            if (welcomeScreen != null)
            {
                UIScreenRouter.TryShowRoot(welcomeScreen);
            }
            else
            {
                Debug.LogWarning("[ProfileUI] Không tìm thấy WELCOMESCREEN để điều hướng.");
            }
        }
    }

    /// <summary>
    /// ✅ FIX: Đảm bảo popup có Canvas + GraphicRaycaster để click được button.
    /// Nếu popup là GameObject riêng (không phải child của main Canvas),
    /// cần có Canvas + GraphicRaycaster để EventSystem nhận click.
    /// QUAN TRỌNG: Canvas phải ở ScreenSpaceOverlay để hiển thị đúng trên màn hình.
    /// </summary>
    private void EnsureGraphicRaycaster(GameObject popup)
    {
        if (popup == null) return;

        // Kiểm tra xem popup có Canvas không
        Canvas popupCanvas = popup.GetComponent<Canvas>();
        if (popupCanvas == null)
        {
            Debug.LogWarning($"[ProfileUI] {popup.name} không có Canvas component! Thêm Canvas...");
            popupCanvas = popup.AddComponent<Canvas>();
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else
        {
            // ✅ FIX: Nếu Canvas ở WorldSpace, chuyển sang ScreenSpaceOverlay
            if (popupCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"[ProfileUI] {popup.name} Canvas ở {popupCanvas.renderMode}! Chuyển sang ScreenSpaceOverlay...");
                popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        // Kiểm tra xem popup có GraphicRaycaster không
        GraphicRaycaster raycaster = popup.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogWarning($"[ProfileUI] {popup.name} không có GraphicRaycaster! Thêm GraphicRaycaster...");
            popup.AddComponent<GraphicRaycaster>();
        }

        Debug.Log($"[ProfileUI] ✅ {popup.name} đã có Canvas (ScreenSpaceOverlay) + GraphicRaycaster");
    }
}
