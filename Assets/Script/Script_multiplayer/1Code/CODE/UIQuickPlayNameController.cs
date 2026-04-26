using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DoAnGame.UI;
using DoAnGame.Auth;

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller cho panel nhập tên chơi nhanh (khách vãng lai)
    /// </summary>
    public class UIQuickPlayNameController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton; // Button "Tiếp tục" (cho returning user)
        [SerializeField] private TextMeshProUGUI statusText; // Text thông báo
        [SerializeField] private TextMeshProUGUI errorText;

        [Header("Navigation")]
        [SerializeField] private string targetPanelName = "MainMenuPanel";

        private const string GUEST_NAME_KEY = "GuestPlayerName";
        private const string IS_GUEST_KEY = "IsGuestMode";
        private const string SELECTED_GRADE_KEY = "SelectedGrade"; // Lưu lớp đã chọn
        private bool isReturningUser = false;

        private void Start()
        {
            // Gán sự kiện cho buttons
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
                continueButton.gameObject.SetActive(false); // Ẩn ban đầu
            }

            // Ẩn texts
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            // Kiểm tra returning user
            CheckReturningUser();
        }

        private void CheckReturningUser()
        {
            if (IsGuestMode())
            {
                string savedName = GetGuestName();
                if (!string.IsNullOrEmpty(savedName) && savedName != "Guest")
                {
                    // Returning user!
                    isReturningUser = true;
                    
                    // Hiển thị welcome back message
                    ShowWelcomeBackMessage(savedName);
                    
                    // Pre-fill tên cũ
                    if (nameInputField != null)
                    {
                        nameInputField.text = savedName;
                    }
                    
                    // Hiển thị button "Tiếp tục"
                    if (continueButton != null)
                    {
                        continueButton.gameObject.SetActive(true);
                    }
                    
                    // Đổi text button "Bắt đầu" thành "Chơi mới"
                    if (startButton != null)
                    {
                        var buttonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null)
                        {
                            buttonText.text = "Chơi Mới";
                        }
                    }
                    
                    Debug.Log($"[QuickPlay] Returning user detected: {savedName}");
                    return;
                }
            }
            
            // New user
            isReturningUser = false;
            
            // Focus vào input field
            if (nameInputField != null)
            {
                nameInputField.Select();
                nameInputField.ActivateInputField();
            }
        }

        private void ShowWelcomeBackMessage(string name)
        {
            if (statusText != null)
            {
                statusText.text = $"<b>Chào mừng trở lại, {name}!</b>\n\n" +
                                 "• Nhấn <color=green><b>Tiếp tục</b></color> để chơi với tên này\n" +
                                 "  (Dữ liệu chơi vẫn được giữ nguyên)\n\n" +
                                 "• Nhấn <color=orange><b>Chơi mới</b></color> và nhập tên khác\n" +
                                 "  để bắt đầu lại từ đầu";
                statusText.gameObject.SetActive(true);
            }
        }

        private void OnContinueButtonClicked()
        {
            // Tiếp tục với tên cũ
            string savedName = GetGuestName();
            
            Debug.Log($"[QuickPlay] User chose to continue with: {savedName}");
            
            // Chuyển sang MainMenuPanel
            NavigateToMainMenu();
        }

        private void OnStartButtonClicked()
        {
            string playerName = nameInputField != null ? nameInputField.text.Trim() : "";

            // Nếu là returning user và nhập tên mới
            if (isReturningUser)
            {
                string oldName = GetGuestName();
                
                // Nếu tên mới khác tên cũ → Xóa dữ liệu cũ
                if (playerName != oldName)
                {
                    Debug.Log($"[QuickPlay] New name detected: '{playerName}' (old: '{oldName}') - Clearing old data");
                    
                    // Xóa dữ liệu chơi cũ
                    LocalProgressService.Instance.ClearAllData();
                    
                    // Hiển thị thông báo
                    if (statusText != null)
                    {
                        statusText.text = "<color=orange><b>Đã xóa dữ liệu cũ!</b></color>\nBắt đầu chơi mới với tên: " + playerName;
                        statusText.gameObject.SetActive(true);
                    }
                }
            }

            // Validate tên
            if (string.IsNullOrEmpty(playerName))
            {
                ShowError("Vui lòng nhập tên của bạn!");
                return;
            }

            if (playerName.Length < 3)
            {
                ShowError("Tên phải có ít nhất 3 ký tự!");
                return;
            }

            if (playerName.Length > 20)
            {
                ShowError("Tên không được quá 20 ký tự!");
                return;
            }

            // Lưu tên người chơi khách
            PlayerPrefs.SetString(GUEST_NAME_KEY, playerName);
            PlayerPrefs.SetInt(IS_GUEST_KEY, 1); // Đánh dấu là chế độ khách
            PlayerPrefs.Save();

            Debug.Log($"[QuickPlay] Saved guest name: {playerName}");

            // Chuyển sang MainMenuPanel
            NavigateToMainMenu();
        }

        private void NavigateToMainMenu()
        {
            Debug.Log($"[QuickPlay] Navigating to {targetPanelName}");
            
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
                Debug.LogError("[QuickPlay] Cannot find GameUICanvas!");
                return;
            }
            
            Debug.Log($"[QuickPlay] Found canvas: {canvas.name}");
            
            // Tìm MainMenuPanel
            Transform mainMenuTransform = FindPanelInCanvas(canvas, targetPanelName);
            if (mainMenuTransform == null)
            {
                Debug.LogError($"[QuickPlay] Không tìm thấy {targetPanelName} trong {canvas.name}!");
                return;
            }
            
            Debug.Log($"[QuickPlay] Found panel: {mainMenuTransform.name}");
            
            // Tắt tất cả siblings (các panels khác)
            HideAllSiblings(canvas);
            
            // Bật MainMenuPanel
            ActivatePanel(mainMenuTransform.gameObject);
            
            Debug.Log($"[QuickPlay] Successfully navigated to {targetPanelName}");
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
            
            Debug.Log($"[QuickPlay] ActivatePanel: {panel.name}, current active: {panel.activeSelf}");
            
            // Bật tất cả parents trước
            Transform current = panel.transform.parent;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    Debug.Log($"[QuickPlay] Activating parent: {current.name}");
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            // Bật panel
            panel.SetActive(true);
            
            Debug.Log($"[QuickPlay] After SetActive(true): {panel.activeSelf}");
            
            // Nếu vẫn không bật được, thử force enable
            if (!panel.activeSelf)
            {
                Debug.LogWarning($"[QuickPlay] Panel still inactive after SetActive(true)! Trying to find blocking component...");
                
                // Kiểm tra các component có thể block
                var components = panel.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null && comp.enabled)
                    {
                        Debug.Log($"[QuickPlay] Found active component: {comp.GetType().Name}");
                    }
                }
            }
            
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
                
                Debug.Log($"[QuickPlay] Normalized RectTransform");
            }
            
            // Đưa lên trên cùng
            panel.transform.SetAsLastSibling();
            
            Debug.Log($"[QuickPlay] SetAsLastSibling done, final active: {panel.activeSelf}");
            
            // Thử gọi Show() nếu có FlowPanelController
            var flowPanel = panel.GetComponent<FlowPanelController>();
            if (flowPanel != null)
            {
                Debug.Log($"[QuickPlay] Found FlowPanelController, calling Show()");
                flowPanel.Show();
            }
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
                
                // Tự động ẩn sau 3 giây
                CancelInvoke(nameof(HideError));
                Invoke(nameof(HideError), 3f);
            }

            Debug.LogWarning($"[QuickPlay] {message}");
        }

        private void HideError()
        {
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đang ở chế độ khách không
        /// </summary>
        public static bool IsGuestMode()
        {
            return PlayerPrefs.GetInt(IS_GUEST_KEY, 0) == 1;
        }

        /// <summary>
        /// Lấy tên người chơi khách
        /// </summary>
        public static string GetGuestName()
        {
            return PlayerPrefs.GetString(GUEST_NAME_KEY, "Guest");
        }

        /// <summary>
        /// Xóa dữ liệu chế độ khách (khi đăng nhập)
        /// </summary>
        public static void ClearGuestData()
        {
            PlayerPrefs.DeleteKey(GUEST_NAME_KEY);
            PlayerPrefs.DeleteKey(IS_GUEST_KEY);
            PlayerPrefs.DeleteKey(SELECTED_GRADE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[QuickPlay] Cleared guest data");
        }

        /// <summary>
        /// Lưu lớp đã chọn
        /// </summary>
        public static void SaveSelectedGrade(int grade)
        {
            PlayerPrefs.SetInt(SELECTED_GRADE_KEY, grade);
            PlayerPrefs.Save();
            Debug.Log($"[QuickPlay] Saved selected grade: {grade}");
        }

        /// <summary>
        /// Lấy lớp đã chọn (0 = chưa chọn)
        /// </summary>
        public static int GetSelectedGrade()
        {
            return PlayerPrefs.GetInt(SELECTED_GRADE_KEY, 0);
        }

        /// <summary>
        /// Kiểm tra xem user đã chọn lớp chưa
        /// </summary>
        public static bool HasSelectedGrade()
        {
            return GetSelectedGrade() > 0;
        }
    }
}
