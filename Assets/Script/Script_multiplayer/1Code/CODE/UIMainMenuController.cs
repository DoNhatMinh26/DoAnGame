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

        [Header("Logout Confirm Popup")]
        [SerializeField] private UIConfirmPopupController logoutConfirmPopup;

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
            else
            {
                UpdatePlayerInfo();
            }
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
        }

        private void UpdatePlayerInfo()
        {
            // Kiểm tra nếu đang ở chế độ khách
            if (UIQuickPlayNameController.IsGuestMode())
            {
                string guestName = UIQuickPlayNameController.GetGuestName();
                characterNameText?.SetText($"Khách: {guestName}");
                levelText?.SetText("Lv: 1");
                scoreText?.SetText("Score: 0");
                
                Debug.Log($"[MainMenu] Guest mode: {guestName}");
                return;
            }

            // Người chơi đã đăng nhập
            var data = authManager?.GetCurrentPlayerData();

            string characterName = data?.characterName;
            if (string.IsNullOrWhiteSpace(characterName))
            {
                characterName = authManager?.GetCharacterName();
            }

            if (string.IsNullOrWhiteSpace(characterName) || characterName == "Unknown")
            {
                var firebaseUser = FirebaseManager.Instance?.GetCurrentUser();
                if (firebaseUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(firebaseUser.DisplayName))
                    {
                        characterName = firebaseUser.DisplayName;
                    }
                    else if (!string.IsNullOrWhiteSpace(firebaseUser.Email))
                    {
                        // Fallback: Use email as name
                        characterName = firebaseUser.Email.Split('@')[0]; // Get part before @
                    }
                }
            }
            
            // Last fallback: Try Firebase Auth directly
            if (string.IsNullOrWhiteSpace(characterName) || characterName == "Unknown")
            {
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
                    
                    Debug.Log($"[MainMenu] Using Firebase Auth user: {characterName}");
                }
            }

            if (!string.IsNullOrWhiteSpace(characterName) && characterName != "Unknown")
            {
                characterNameText?.SetText($"Tên Nhân Vật: {characterName}");
                Debug.Log($"[MainMenu] Displaying character name: {characterName}");
            }
            else
            {
                characterNameText?.SetText("Tên Nhân Vật: -----");
                Debug.LogWarning("[MainMenu] Could not load character name!");
            }

            if (data == null)
            {
                levelText?.SetText("Lv: 1");
                scoreText?.SetText("Score: 0");
                return;
            }

            levelText?.SetText($"Lv: {data.level}");
            scoreText?.SetText($"Score: {data.totalScore}");
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
