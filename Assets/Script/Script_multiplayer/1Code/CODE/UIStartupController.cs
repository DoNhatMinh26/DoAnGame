using UnityEngine;
using UnityEngine.SceneManagement;
using DoAnGame.UI;
using DoAnGame.Auth;

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller khởi động app - tự động skip WELCOMESCREEN cho returning users
    /// Gắn vào GameObject trong scene đầu tiên
    /// </summary>
    public class UIStartupController : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject welcomeScreenPanel; // WELCOMESCREEN (chọn lớp)
        [SerializeField] private GameObject welcomePanel;       // WellcomePanel (menu chính)
        [SerializeField] private GameObject mainMenuPanel;      // MainMenuPanel (nếu đã đăng nhập)

        [Header("Settings")]
        [SerializeField] private bool enableAutoSkip = true;    // Bật/tắt auto-skip

        private void Start()
        {
            if (!enableAutoSkip)
            {
                Debug.Log("[Startup] Auto-skip disabled, showing WELCOMESCREEN");
                ShowWelcomeScreen();
                return;
            }

            try
            {
                CheckAndRoute();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Startup] Error in CheckAndRoute: {e.Message}\n{e.StackTrace}");
                // Fallback: Show WELCOMESCREEN
                ShowWelcomeScreen();
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái user và route đến panel phù hợp
        /// </summary>
        private void CheckAndRoute()
        {
            // Kiểm tra đã đăng nhập chưa
            bool isLoggedIn = CheckIfLoggedIn();
            
            // Kiểm tra là guest và đã chọn lớp chưa
            bool isGuest = UIQuickPlayNameController.IsGuestMode();
            bool hasSelectedGrade = UIQuickPlayNameController.HasSelectedGrade();
            string guestName = UIQuickPlayNameController.GetGuestName();

            Debug.Log($"[Startup] Status - LoggedIn: {isLoggedIn}, Guest: {isGuest}, HasGrade: {hasSelectedGrade}, Name: {guestName}");

            // CASE 1: Đã đăng nhập → Skip thẳng đến MainMenuPanel
            if (isLoggedIn)
            {
                Debug.Log("[Startup] User logged in → Navigating to MainMenuPanel");
                ShowMainMenuPanel();
                return;
            }

            // CASE 2: Guest mode + đã có tên + đã chọn lớp → Skip to WellcomePanel
            if (isGuest && !string.IsNullOrEmpty(guestName) && guestName != "Guest" && hasSelectedGrade)
            {
                Debug.Log($"[Startup] Returning guest '{guestName}' with grade {UIQuickPlayNameController.GetSelectedGrade()} → Navigating to WellcomePanel");
                
                // Restore SelectedGrade vào UIManager
                int savedGrade = UIQuickPlayNameController.GetSelectedGrade();
                UIManager.SelectedGrade = savedGrade;
                
                ShowWellcomePanel();
                return;
            }

            // CASE 3: New user hoặc chưa chọn lớp → Show WELCOMESCREEN
            Debug.Log("[Startup] New user or no grade selected → Showing WELCOMESCREEN");
            ShowWelcomeScreen();
        }

        /// <summary>
        /// Kiểm tra xem user đã đăng nhập chưa
        /// </summary>
        private bool CheckIfLoggedIn()
        {
            try
            {
                // Kiểm tra Firebase Auth
                if (Firebase.Auth.FirebaseAuth.DefaultInstance != null)
                {
                    var currentUser = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
                    if (currentUser != null)
                    {
                        Debug.Log($"[Startup] Firebase user found: {currentUser.Email}");
                        return true;
                    }
                    else
                    {
                        Debug.Log("[Startup] Firebase Auth: No current user");
                    }
                }
                else
                {
                    Debug.Log("[Startup] Firebase Auth: DefaultInstance is null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Startup] Error checking Firebase auth: {e.Message}");
            }

            // Kiểm tra session token (24h auto-login)
            string sessionToken = PlayerPrefs.GetString("SessionToken", "");
            if (!string.IsNullOrEmpty(sessionToken))
            {
                Debug.Log($"[Startup] Found session token: {sessionToken.Substring(0, System.Math.Min(10, sessionToken.Length))}...");
                
                // Kiểm tra token còn hạn không
                string expiryStr = PlayerPrefs.GetString("SessionExpiry", "0");
                if (long.TryParse(expiryStr, out long sessionExpiry))
                {
                    long currentTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    
                    if (currentTime < sessionExpiry)
                    {
                        Debug.Log("[Startup] Valid session token found");
                        return true;
                    }
                    else
                    {
                        Debug.Log("[Startup] Session token expired");
                        // Clear expired token
                        PlayerPrefs.DeleteKey("SessionToken");
                        PlayerPrefs.DeleteKey("SessionExpiry");
                        PlayerPrefs.Save();
                    }
                }
            }
            else
            {
                Debug.Log("[Startup] No session token found");
            }

            Debug.Log("[Startup] User is NOT logged in");
            return false;
        }

        /// <summary>
        /// Hiển thị WELCOMESCREEN (chọn lớp)
        /// </summary>
        private void ShowWelcomeScreen()
        {
            try
            {
                if (welcomeScreenPanel != null)
                {
                    welcomeScreenPanel.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("[Startup] welcomeScreenPanel is NULL!");
                }

                if (welcomePanel != null)
                {
                    welcomePanel.SetActive(false);
                }

                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(false);
                }

                Debug.Log("[Startup] Showing WELCOMESCREEN");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Startup] Error in ShowWelcomeScreen: {e.Message}");
            }
        }

        /// <summary>
        /// Hiển thị WellcomePanel (menu chính)
        /// </summary>
        private void ShowWellcomePanel()
        {
            try
            {
                Debug.Log($"[Startup] ShowWellcomePanel called - welcomePanel is null: {welcomePanel == null}");
                
                if (welcomeScreenPanel != null)
                {
                    welcomeScreenPanel.SetActive(false);
                    Debug.Log("[Startup] Disabled WELCOMESCREEN");
                }

                if (welcomePanel != null)
                {
                    welcomePanel.SetActive(true);
                    Debug.Log($"[Startup] Enabled WellcomePanel - Active: {welcomePanel.activeSelf}");
                }
                else
                {
                    Debug.LogError("[Startup] WellcomePanel reference is NULL!");
                }

                if (mainMenuPanel != null)
                {
                    mainMenuPanel.SetActive(false);
                }

                Debug.Log("[Startup] Showing WellcomePanel");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Startup] Error in ShowWellcomePanel: {e.Message}");
            }
        }

        /// <summary>
        /// Hiển thị MainMenuPanel (cho logged-in users)
        /// </summary>
        private void ShowMainMenuPanel()
        {
            try
            {
                Debug.Log($"[Startup] ShowMainMenuPanel called - mainMenuPanel is null: {mainMenuPanel == null}");
                
                if (welcomeScreenPanel != null)
                {
                    welcomeScreenPanel.SetActive(false);
                }

                if (welcomePanel != null)
                {
                    welcomePanel.SetActive(false);
                }

                if (mainMenuPanel != null)
                {
                    // Force enable all parents
                    Transform current = mainMenuPanel.transform.parent;
                    while (current != null)
                    {
                        if (!current.gameObject.activeSelf)
                        {
                            Debug.Log($"[Startup] Enabling parent: {current.name}");
                            current.gameObject.SetActive(true);
                        }
                        current = current.parent;
                    }
                    
                    mainMenuPanel.SetActive(true);
                    Debug.Log($"[Startup] Enabled MainMenuPanel - Active: {mainMenuPanel.activeSelf}");
                    
                    // Try to call Show() if it has FlowPanelController
                    var flowPanel = mainMenuPanel.GetComponent<DoAnGame.UI.FlowPanelController>();
                    if (flowPanel != null)
                    {
                        Debug.Log("[Startup] Calling FlowPanelController.Show()");
                        flowPanel.Show();
                    }
                    
                    // Double check after 1 frame
                    StartCoroutine(VerifyPanelActive());
                }
                else
                {
                    Debug.LogError("[Startup] MainMenuPanel reference is NULL!");
                }

                Debug.Log("[Startup] Showing MainMenuPanel");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Startup] Error in ShowMainMenuPanel: {e.Message}");
            }
        }
        
        private System.Collections.IEnumerator VerifyPanelActive()
        {
            yield return null; // Wait 1 frame
            
            if (mainMenuPanel != null)
            {
                Debug.Log($"[Startup] Verify after 1 frame - MainMenuPanel Active: {mainMenuPanel.activeSelf}");
                
                if (!mainMenuPanel.activeSelf)
                {
                    Debug.LogError("[Startup] MainMenuPanel is STILL inactive after SetActive(true)! Something is overriding it.");
                    
                    // List all components on MainMenuPanel
                    var components = mainMenuPanel.GetComponents<MonoBehaviour>();
                    Debug.Log($"[Startup] MainMenuPanel has {components.Length} MonoBehaviour components:");
                    foreach (var comp in components)
                    {
                        if (comp != null)
                        {
                            Debug.Log($"[Startup]   - {comp.GetType().Name} (enabled: {comp.enabled})");
                        }
                    }
                }
            }
        }
    }
}
