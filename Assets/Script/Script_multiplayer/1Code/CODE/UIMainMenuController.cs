using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Auth;
using DoAnGame.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 5: Main Menu Hub
    /// </summary>
    public class UIMainMenuController : FlowPanelController
    {
        [Header("Buttons (Nút)")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private GameObject welcomeScreenRoot;

        [Header("Texts (Thông tin người chơi)")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text gradeText;  // ✅ Thêm Grade

        [Header("Logout Confirm Popup")]
        [SerializeField] private UIConfirmPopupController logoutConfirmPopup;

        [Header("Avatar")]
        [SerializeField] private Image avatarImage;  // MainMenuPanel/Avatar

        [SerializeField] private UIFlowManager flowManager;

        private AuthManager authManager;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;

            if (characterNameText == null)
            {
                Transform nameNode = transform.Find("TenNhanVat");
                if (nameNode != null)
                {
                    characterNameText = nameNode.GetComponent<TMP_Text>();
                }
            }

            if (logoutButton == null)
            {
                Transform logoutNode = transform.Find("DangXuat");
                if (logoutNode == null)
                {
                    logoutNode = transform.Find("DangXuatBtn");
                }

                if (logoutNode != null)
                {
                    logoutButton = logoutNode.GetComponent<Button>();
                }
            }

            if (welcomeScreenRoot == null)
            {
                Transform canvas = transform.parent;
                if (canvas != null)
                {
                    var welcomeNode = canvas.Find("WELCOMESCREEN");
                    if (welcomeNode != null)
                    {
                        welcomeScreenRoot = welcomeNode.gameObject;
                    }
                }
            }

            // Auto-find Avatar image nếu chưa gán trong Inspector
            if (avatarImage == null)
            {
                Transform avatarNode = transform.Find("Avatar");
                if (avatarNode != null)
                    avatarImage = avatarNode.GetComponent<Image>();
            }

            // Subscribe AvatarManager event để tự refresh khi avatar thay đổi
            if (AvatarManager.Instance != null)
                AvatarManager.Instance.OnAvatarChanged += OnAvatarChanged;

            if (logoutButton != null)
            {
                var logoutNavigator = logoutButton.GetComponent<UIButtonScreenNavigator>();
                if (logoutNavigator != null)
                {
                    logoutNavigator.enabled = false;
                }

                logoutButton.onClick.RemoveAllListeners();
                logoutButton.onClick.AddListener(HandleLogoutButtonClicked);
            }

            // Auto-find popup nếu chưa gán trong Inspector
            if (logoutConfirmPopup == null)
            {
                logoutConfirmPopup = GetComponentInChildren<UIConfirmPopupController>(true);
                if (logoutConfirmPopup == null)
                {
                    // Tìm rộng hơn trong canvas
                    logoutConfirmPopup = FindObjectOfType<UIConfirmPopupController>(true);
                }
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (authManager == null)
            {
                authManager = AuthManager.Instance;
            }
            
            // Force load player data if logged in
            if (!UIQuickPlayNameController.IsGuestMode())
            {
                Debug.Log("[MainMenu] Logged-in user detected, loading player data...");
                LoadPlayerDataAsync();
            }
            
            // ✅ Luôn gọi UpdatePlayerInfo() để hiển thị dữ liệu mới nhất
            // (Cho cả guest lẫn logged-in user)
            UpdatePlayerInfo();
        }

        protected override void OnHide()
        {
            base.OnHide();
        }
        
        private async void LoadPlayerDataAsync()
        {
            try
            {
                if (authManager != null)
                {
                    // Wait for AuthManager to load data
                    await System.Threading.Tasks.Task.Delay(100); // Small delay to let Firebase sync
                    
                    // Try to get Firebase user
                    var firebaseUser = Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser;
                    if (firebaseUser != null)
                    {
                        Debug.Log($"[MainMenu] Firebase user: {firebaseUser.Email}, DisplayName: {firebaseUser.DisplayName}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainMenu] Error loading player data: {e.Message}");
            }
            finally
            {
                // Update UI regardless of success/failure
                UpdatePlayerInfo();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            logoutButton?.onClick.RemoveAllListeners();
            if (AvatarManager.Instance != null)
                AvatarManager.Instance.OnAvatarChanged -= OnAvatarChanged;
        }

        /// <summary>
        /// Callback từ AvatarManager khi avatar thay đổi — tự refresh image.
        /// </summary>
        private void OnAvatarChanged(AvatarData newAvatar)
        {
            RefreshAvatarImage();
        }

        /// <summary>
        /// Cập nhật avatar image từ AvatarManager.
        /// </summary>
        private void RefreshAvatarImage()
        {
            if (avatarImage == null || AvatarManager.Instance == null) return;
            Sprite sprite = AvatarManager.Instance.GetCurrentFullAvatar();
            if (sprite != null)
                avatarImage.sprite = sprite;
        }

        public void UpdatePlayerInfo()
        {
            // ✅ DEBUG: Log trạng thái
            Debug.Log($"[MainMenu] ========== UPDATE PLAYER INFO ==========");
            Debug.Log($"[MainMenu] IsGuestMode: {UIQuickPlayNameController.IsGuestMode()}");
            Debug.Log($"[MainMenu] GuestName: '{UIQuickPlayNameController.GetGuestName()}'");
            Debug.Log($"[MainMenu] SelectedGrade: {UIManager.SelectedGrade}");
            Debug.Log($"[MainMenu] ==========================================");
            
            // Kiểm tra nếu đang ở chế độ khách
            if (UIQuickPlayNameController.IsGuestMode())
            {
                string guestName = UIQuickPlayNameController.GetGuestName();
                characterNameText?.SetText($"Khách: {guestName}");
                
                // ✅ Đọc từ đúng key cho guest mode
                int guestScore = PlayerPrefs.GetInt("LocalGuestScore", 0);
                int guestLevel = PlayerPrefs.GetInt("LocalGuestLevel", 1);
                int guestGrade = UIManager.SelectedGrade;
                
                levelText?.SetText($"Lv: {guestLevel}");
                scoreText?.SetText($"Score: {guestScore}");
                gradeText?.SetText($"Lớp: {guestGrade}");
                
                Debug.Log($"[MainMenu] Guest mode: {guestName}, Level: {guestLevel}, Score: {guestScore}, Grade: {guestGrade}");

                // Cập nhật avatar
                RefreshAvatarImage();
                return;
            }

            // Người chơi đã đăng nhập
            var data = authManager?.GetCurrentPlayerData();

            // ✅ Nếu không có player data, hiển thị từ PlayerPrefs và return
            if (data == null)
            {
                Debug.LogWarning("[MainMenu] No player data available, showing from PlayerPrefs");
                
                // Hiển thị tên từ Firebase Auth
                string characterName = "Unknown";
                var currentUser = Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser;
                if (currentUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(currentUser.DisplayName))
                    {
                        characterName = currentUser.DisplayName;
                    }
                    else if (!string.IsNullOrWhiteSpace(currentUser.Email))
                    {
                        characterName = currentUser.Email.Split('@')[0];
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(characterName) && characterName != "Unknown")
                {
                    characterNameText?.SetText($"Tên Nhân Vật: {characterName}");
                }
                else
                {
                    characterNameText?.SetText("Tên Nhân Vật: -----");
                }
                
                // Hiển thị score, level, grade từ PlayerPrefs
                int score = PlayerPrefs.GetInt("UserScore", 0);
                int level = PlayerPrefs.GetInt("UserLevel", 1);
                int grade = UIManager.SelectedGrade;
                levelText?.SetText($"Lv: {level}");
                scoreText?.SetText($"Score: {score}");
                gradeText?.SetText($"Lớp: {grade}");
                
                Debug.Log($"[MainMenu] Logged-in (no data): {characterName}, Level: {level}, Score: {score}, Grade: {grade}");

                // Cập nhật avatar
                RefreshAvatarImage();
                return;  // ✅ QUAN TRỌNG: Return để không chạy code phía dưới
            }

            // ✅ Có player data - hiển thị từ Firebase
            string finalCharacterName = data.characterName;
            if (string.IsNullOrWhiteSpace(finalCharacterName))
            {
                finalCharacterName = authManager?.GetCharacterName();
            }

            if (string.IsNullOrWhiteSpace(finalCharacterName) || finalCharacterName == "Unknown")
            {
                var firebaseUser = FirebaseManager.Instance?.GetCurrentUser();
                if (firebaseUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(firebaseUser.DisplayName))
                    {
                        finalCharacterName = firebaseUser.DisplayName;
                    }
                    else if (!string.IsNullOrWhiteSpace(firebaseUser.Email))
                    {
                        finalCharacterName = firebaseUser.Email.Split('@')[0];
                    }
                }
            }
            
            // Last fallback: Try Firebase Auth directly
            if (string.IsNullOrWhiteSpace(finalCharacterName) || finalCharacterName == "Unknown")
            {
                var currentUser = Firebase.Auth.FirebaseAuth.DefaultInstance?.CurrentUser;
                if (currentUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(currentUser.DisplayName))
                    {
                        finalCharacterName = currentUser.DisplayName;
                    }
                    else if (!string.IsNullOrWhiteSpace(currentUser.Email))
                    {
                        finalCharacterName = currentUser.Email.Split('@')[0];
                    }
                    
                    Debug.Log($"[MainMenu] Using Firebase Auth user: {finalCharacterName}");
                }
            }

            if (!string.IsNullOrWhiteSpace(finalCharacterName) && finalCharacterName != "Unknown")
            {
                characterNameText?.SetText($"Tên Nhân Vật: {finalCharacterName}");
                Debug.Log($"[MainMenu] Displaying character name: {finalCharacterName}");
            }
            else
            {
                characterNameText?.SetText("Tên Nhân Vật: -----");
                Debug.LogWarning("[MainMenu] Could not load character name!");
            }

            // Hiển thị level, score và grade từ Firebase
            levelText?.SetText($"Lv: {data.level}");
            scoreText?.SetText($"Score: {data.totalScore}");
            gradeText?.SetText($"Lớp: {UIManager.SelectedGrade}");
            
            Debug.Log($"[MainMenu] Logged-in user: {finalCharacterName}, Level: {data.level}, Score: {data.totalScore}, Grade: {UIManager.SelectedGrade}");

            // Cập nhật avatar
            RefreshAvatarImage();
        }

        /// <summary>
        /// Bước 1: Nhấn nút Đăng Xuất → hiện popup xác nhận
        /// </summary>
        private void HandleLogoutButtonClicked()
        {
            Debug.Log("[MainMenu] Logout button clicked — showing confirm popup");

            if (logoutConfirmPopup == null)
            {
                // Không có popup → logout thẳng (fallback an toàn)
                Debug.LogWarning("[MainMenu] logoutConfirmPopup chưa gán, logout thẳng.");
                _ = ExecuteLogout();
                return;
            }

            // Xác định tên người dùng để hiển thị trong popup
            string userName = UIQuickPlayNameController.IsGuestMode()
                ? UIQuickPlayNameController.GetGuestName()
                : (authManager?.GetCharacterName() ?? "bạn");

            logoutConfirmPopup.Show(
                title:        "Xác nhận đăng xuất",
                message:      $"Bạn có chắc muốn đăng xuất khỏi tài khoản <b>{userName}</b>?",
                confirmLabel: "Đăng xuất",
                onConfirm:    () => _ = ExecuteLogout(),
                onCancel:     null,   // Huỷ → chỉ đóng popup, không làm gì
                cancelLabel:  "Huỷ"
            );
        }

        /// <summary>
        /// Bước 2: Người dùng xác nhận → thực hiện logout
        /// </summary>
        private async System.Threading.Tasks.Task ExecuteLogout()
        {
            Debug.Log("[MainMenu] ExecuteLogout — clearing session...");

            // Clear session tokens
            PlayerPrefs.DeleteKey("SessionToken");
            PlayerPrefs.DeleteKey("SessionExpiry");
            PlayerPrefs.Save();

            // Logout Firebase
            authManager?.Logout();

            // Xóa dữ liệu khách nếu có
            UIQuickPlayNameController.ClearGuestData();

            Debug.Log("[MainMenu] Session cleared, reloading scene...");

            // Đợi Firebase sign-out hoàn tất
            await System.Threading.Tasks.Task.Delay(100);

            // Reload scene để reset toàn bộ trạng thái
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
}
