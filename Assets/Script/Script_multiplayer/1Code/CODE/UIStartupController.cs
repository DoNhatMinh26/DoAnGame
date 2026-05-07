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

        private async void Start()
        {
            if (!enableAutoSkip)
            {
                Debug.Log("[Startup] Auto-skip disabled, showing WELCOMESCREEN");
                ShowWelcomeScreen();
                return;
            }

            try
            {
                await CheckAndRouteAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Startup] Error in CheckAndRouteAsync: {e.Message}\n{e.StackTrace}");
                // Fallback: Show WELCOMESCREEN
                ShowWelcomeScreen();
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái user và route đến panel phù hợp.
        /// TỐI ƯU: Kiểm tra session local TRƯỚC, chỉ gọi Firebase nếu cần.
        /// Giảm delay startup bằng cách skip Firebase nếu session local còn hạn.
        /// 
        /// FLOW:
        /// 1. Kiểm tra session email (24h auto-login) → MainMenuPanel
        /// 2. Kiểm tra guest session (chơi nhanh) → MainMenuPanel
        /// 3. Gọi Firebase auto-login → MainMenuPanel
        /// 4. Mới user → WELCOMESCREEN
        /// </summary>
        private async System.Threading.Tasks.Task CheckAndRouteAsync()
        {
            // Bước 1: Kiểm tra session email (24h auto-login) — nhanh, local only
            bool hasValidEmailSession = SessionManager.Instance != null && SessionManager.Instance.IsSessionValid();
            
            if (hasValidEmailSession)
            {
                Debug.Log("[Startup] ✅ CASE 1: Valid email session found → Navigating to MainMenuPanel");
                ShowMainMenuPanel();
                
                // Gọi Firebase async ở background để restore grade (không block UI)
                _ = AuthManager.Instance?.CheckAndAutoLogin();
                return;
            }

            // Bước 2: Kiểm tra guest session (chơi nhanh) — nhanh, local only
            bool isGuestMode = UIQuickPlayNameController.IsGuestMode();
            string guestName = UIQuickPlayNameController.GetGuestName();
            int savedGrade = UIQuickPlayNameController.GetSelectedGrade();
            bool hasGuestData = isGuestMode && !string.IsNullOrEmpty(guestName) && guestName != "Guest" && savedGrade > 0;

            Debug.Log($"[Startup] ========== STARTUP ROUTING (OPTIMIZED) ==========");
            Debug.Log($"[Startup] HasValidEmailSession: {hasValidEmailSession}");
            Debug.Log($"[Startup] IsGuestMode: {isGuestMode}");
            Debug.Log($"[Startup] GuestName: '{guestName}'");
            Debug.Log($"[Startup] SavedGrade: {savedGrade}");
            Debug.Log($"[Startup] HasGuestData: {hasGuestData}");
            Debug.Log($"[Startup] ====================================================");

            if (hasGuestData)
            {
                Debug.Log($"[Startup] ✅ CASE 2: Valid guest session found ('{guestName}', Grade {savedGrade}) → Navigating to MainMenuPanel");
                
                // Restore guest state
                UIManager.SelectedGrade = savedGrade;
                
                ShowMainMenuPanel();
                return;
            }

            // Bước 3: Không có session local → Gọi Firebase để kiểm tra auto-login
            Debug.Log("[Startup] ⚠️ CASE 3: No local session → Checking Firebase auto-login...");
            
            bool autoLoginSuccess = false;
            if (AuthManager.Instance != null)
            {
                autoLoginSuccess = await AuthManager.Instance.CheckAndAutoLogin();
                Debug.Log($"[Startup] AuthManager.CheckAndAutoLogin() result: {autoLoginSuccess}");
            }

            if (autoLoginSuccess)
            {
                Debug.Log("[Startup] ✅ CASE 3a: Firebase auto-login success → Navigating to MainMenuPanel");
                ShowMainMenuPanel();
                return;
            }

            // CASE 4: Không có session, không auto-login → WELCOMESCREEN
            Debug.Log("[Startup] ⚠️ CASE 4: New user or no valid session → Showing WELCOMESCREEN");
            ShowWelcomeScreen();
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
