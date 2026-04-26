using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Auth;

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller cho ModSelectionPanel
    /// Kiểm tra đăng nhập trước khi cho phép vào Multiplayer
    /// </summary>
    public class UIModSelectionPanelController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button multiplayerButton;
        [SerializeField] private Button singlePlayerButton;

        [Header("Multiplayer Scene")]
        [SerializeField] private string multiplayerSceneName = "Test_FireBase_multi";
        
        [Header("Login Required Popup")]
        [SerializeField] private UILoginRequiredPopupController loginRequiredPopup;

        private AuthManager authManager;

        private void Awake()
        {
            // Ensure AuthManager exists
            if (AuthManager.Instance == null)
            {
                Debug.LogWarning("[ModSelection] AuthManager.Instance is null in Awake, trying to find in scene...");
                authManager = FindObjectOfType<AuthManager>();
                
                if (authManager == null)
                {
                    Debug.LogWarning("[ModSelection] AuthManager not found in scene! Creating AuthServices...");
                    
                    // Try to find AuthServices GameObject
                    GameObject authServices = GameObject.Find("AuthServices");
                    if (authServices == null)
                    {
                        Debug.LogError("[ModSelection] AuthServices GameObject not found! Multiplayer will not work properly.");
                        Debug.LogError("[ModSelection] Please ensure AuthServices exists in the scene or is DontDestroyOnLoad.");
                    }
                    else
                    {
                        authManager = authServices.GetComponent<AuthManager>();
                        if (authManager == null)
                        {
                            Debug.LogError("[ModSelection] AuthManager component not found on AuthServices!");
                        }
                    }
                }
            }
            else
            {
                authManager = AuthManager.Instance;
                Debug.Log("[ModSelection] AuthManager found via Instance");
            }

            // Tìm popup nếu chưa gán
            if (loginRequiredPopup == null)
            {
                Debug.Log("[ModSelection] Popup not assigned in Inspector, searching in scene...");
                loginRequiredPopup = UILoginRequiredPopupController.FindInScene();
                if (loginRequiredPopup == null)
                {
                    Debug.LogWarning("[ModSelection] UILoginRequiredPopupController not found in scene!");
                }
                else
                {
                    Debug.Log($"[ModSelection] Found popup: {loginRequiredPopup.name}");
                }
            }
            else
            {
                Debug.Log($"[ModSelection] Popup already assigned: {loginRequiredPopup.name}");
            }

            // Tìm multiplayer button
            if (multiplayerButton == null)
            {
                Transform multiplayerNode = transform.Find("multiplayerBtn");
                if (multiplayerNode != null)
                {
                    multiplayerButton = multiplayerNode.GetComponent<Button>();
                }
            }

            // Setup multiplayer button
            if (multiplayerButton != null)
            {
                // Disable UIButtonScreenNavigator để tự xử lý
                var navigator = multiplayerButton.GetComponent<UIButtonScreenNavigator>();
                if (navigator != null)
                {
                    navigator.enabled = false;
                }

                // Gán sự kiện kiểm tra đăng nhập
                multiplayerButton.onClick.RemoveAllListeners();
                multiplayerButton.onClick.AddListener(OnMultiplayerButtonClicked);
            }
        }

        private void OnDestroy()
        {
            multiplayerButton?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Xử lý khi click button Multiplayer
        /// </summary>
        private void OnMultiplayerButtonClicked()
        {
            Debug.Log("[ModSelection] Multiplayer button clicked");
            
            // Re-check AuthManager in case it was destroyed
            if (authManager == null)
            {
                authManager = AuthManager.Instance;
                if (authManager == null)
                {
                    Debug.LogError("[ModSelection] AuthManager still null! Cannot check login status.");
                    ShowLoginRequiredPopup();
                    return;
                }
            }
            
            // Kiểm tra nếu đang ở chế độ khách
            bool isGuest = UIQuickPlayNameController.IsGuestMode();
            Debug.Log($"[ModSelection] IsGuestMode: {isGuest}");
            
            if (isGuest)
            {
                ShowGuestLoginRequiredPopup();
                return;
            }

            // Kiểm tra nếu chưa đăng nhập Firebase
            bool isLoggedIn = authManager.IsLoggedIn();
            Debug.Log($"[ModSelection] AuthManager is null: {authManager == null}");
            Debug.Log($"[ModSelection] IsLoggedIn: {isLoggedIn}");
            
            if (!isLoggedIn)
            {
                ShowLoginRequiredPopup();
                return;
            }

            // Đã đăng nhập → Cho phép vào multiplayer
            Debug.Log("[ModSelection] User is logged in, navigating to multiplayer");
            NavigateToMultiplayer();
        }

        /// <summary>
        /// Hiển thị popup yêu cầu đăng nhập cho khách
        /// </summary>
        private void ShowGuestLoginRequiredPopup()
        {
            Debug.LogWarning("[ModSelection] Guest mode - Login required for multiplayer");
            Debug.Log($"[ModSelection] loginRequiredPopup is null? {loginRequiredPopup == null}");
            
            if (loginRequiredPopup != null)
            {
                Debug.Log($"[ModSelection] Calling popup.Show() on '{loginRequiredPopup.name}'");
                
                // Hiển thị popup với 2 button
                loginRequiredPopup.Show(
                    "Yêu Cầu Đăng Nhập",
                    "Chế độ Multiplayer yêu cầu tài khoản.\nVui lòng đăng nhập hoặc đăng ký!",
                    onLogin: () => {
                        Debug.Log("[ModSelection] User chose to login");
                        NavigateToLogin();
                    },
                    onCancel: () => {
                        Debug.Log("[ModSelection] User cancelled login");
                        // Không làm gì, chỉ đóng popup
                    }
                );
            }
            else
            {
                // Fallback: Chuyển thẳng sang LoginPanel
                Debug.LogWarning("[ModSelection] Popup not found, navigating directly to LoginPanel");
                NavigateToLogin();
            }
        }

        /// <summary>
        /// Hiển thị popup yêu cầu đăng nhập (session hết hạn)
        /// </summary>
        private void ShowLoginRequiredPopup()
        {
            Debug.LogWarning("[ModSelection] Not logged in - Login required for multiplayer");
            
            if (loginRequiredPopup != null)
            {
                // Hiển thị popup với 2 button
                loginRequiredPopup.Show(
                    "Yêu Cầu Đăng Nhập",
                    "Bạn chưa đăng nhập.\nVui lòng đăng nhập để chơi Multiplayer!",
                    onLogin: () => {
                        Debug.Log("[ModSelection] User chose to login");
                        NavigateToLogin();
                    },
                    onCancel: () => {
                        Debug.Log("[ModSelection] User cancelled login");
                        // Không làm gì, chỉ đóng popup
                    }
                );
            }
            else
            {
                // Fallback: Chuyển thẳng sang LoginPanel
                Debug.LogWarning("[ModSelection] Popup not found, navigating directly to LoginPanel");
                NavigateToLogin();
            }
        }

        /// <summary>
        /// Chuyển sang LoginPanel
        /// </summary>
        private void NavigateToLogin()
        {
            // Xóa dữ liệu khách
            UIQuickPlayNameController.ClearGuestData();

            Debug.Log("[ModSelection] Navigating to LoginPanel");
            
            // Tìm GameUICanvas
            Transform canvas = null;
            
            // Thử tìm từ parent của panel này
            Transform current = transform.parent;
            while (current != null)
            {
                if (current.name == "GameUICanvas" || current.GetComponent<Canvas>() != null)
                {
                    canvas = current;
                    break;
                }
                current = current.parent;
            }
            
            // Nếu không tìm thấy, tìm trong scene
            if (canvas == null)
            {
                Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
                foreach (var c in allCanvases)
                {
                    if (c.name == "GameUICanvas")
                    {
                        canvas = c.transform;
                        break;
                    }
                }
            }
            
            if (canvas == null)
            {
                Debug.LogError("[ModSelection] Cannot find GameUICanvas!");
                return;
            }
            
            Debug.Log($"[ModSelection] Found canvas: {canvas.name}");
            
            // Tìm LoginPanel
            Transform loginPanelTransform = FindPanelInCanvas(canvas, "LoginPanel");
            if (loginPanelTransform == null)
            {
                Debug.LogError("[ModSelection] Không tìm thấy LoginPanel!");
                return;
            }
            
            Debug.Log($"[ModSelection] Found panel: {loginPanelTransform.name}");
            
            // Tắt tất cả siblings (các panels khác)
            HideAllSiblings(canvas);
            
            // Bật LoginPanel
            ActivatePanel(loginPanelTransform.gameObject);
            
            Debug.Log("[ModSelection] Successfully navigated to LoginPanel");
        }
        
        private Transform FindPanelInCanvas(Transform canvas, string panelName)
        {
            // Tìm trong children của canvas
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (child.name == panelName)
                {
                    return child;
                }
            }
            return null;
        }
        
        private void HideAllSiblings(Transform canvas)
        {
            // Tắt tất cả panels trong canvas (trừ EventSystem)
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (child == null) continue;
                
                // Không tắt EventSystem
                if (child.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
                    continue;
                
                child.gameObject.SetActive(false);
            }
        }
        
        private void ActivatePanel(GameObject panel)
        {
            if (panel == null) return;
            
            Debug.Log($"[ModSelection] ActivatePanel: {panel.name}, current active: {panel.activeSelf}");
            
            // Bật tất cả parents trước
            Transform current = panel.transform.parent;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    Debug.Log($"[ModSelection] Activating parent: {current.name}");
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            // Bật panel
            panel.SetActive(true);
            
            Debug.Log($"[ModSelection] After SetActive(true): {panel.activeSelf}");
            
            // Normalize RectTransform về full-screen
            RectTransform rect = panel.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
                
                Debug.Log($"[ModSelection] Normalized RectTransform");
            }
            
            // Đưa lên trên cùng
            panel.transform.SetAsLastSibling();
            
            Debug.Log($"[ModSelection] SetAsLastSibling done, final active: {panel.activeSelf}");
            
            // Thử gọi Show() nếu có FlowPanelController
            var flowPanel = panel.GetComponent<FlowPanelController>();
            if (flowPanel != null)
            {
                Debug.Log($"[ModSelection] Found FlowPanelController, calling Show()");
                flowPanel.Show();
            }
        }

        /// <summary>
        /// Chuyển sang multiplayer scene
        /// </summary>
        private void NavigateToMultiplayer()
        {
            Debug.Log($"[ModSelection] Navigating to multiplayer scene: {multiplayerSceneName}");

            // Kiểm tra scene name có hợp lệ không
            if (string.IsNullOrEmpty(multiplayerSceneName))
            {
                Debug.LogError("[ModSelection] Multiplayer scene name is empty! Please set it in Inspector.");
                return;
            }

            // Load multiplayer scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(multiplayerSceneName);
        }
    }
}
