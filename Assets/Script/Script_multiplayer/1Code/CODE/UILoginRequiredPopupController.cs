using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DoAnGame.UI
{
    /// <summary>
    /// Popup yêu cầu đăng nhập khi người chơi khách cố vào Multiplayer
    /// </summary>
    public class UILoginRequiredPopupController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button cancelButton;

        [Header("Default Text")]
        [SerializeField] private string defaultTitle = "Yêu Cầu Đăng Nhập";
        [SerializeField] private string defaultMessage = "Chế độ Multiplayer yêu cầu tài khoản.\nVui lòng đăng nhập hoặc đăng ký!";

        private System.Action onLoginCallback;
        private System.Action onCancelCallback;

        private void Awake()
        {
            Debug.Log($"[LoginRequiredPopup] Awake() - GameObject: {name}, active: {gameObject.activeSelf}");
            
            // Gán sự kiện cho buttons
            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginButtonClicked);
                Debug.Log("[LoginRequiredPopup] Login button listener added");
            }
            else
            {
                Debug.LogWarning("[LoginRequiredPopup] Login button is NULL!");
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
                Debug.Log("[LoginRequiredPopup] Cancel button listener added");
            }
            else
            {
                Debug.LogWarning("[LoginRequiredPopup] Cancel button is NULL!");
            }

            // Fix RectTransform ngay trong Awake
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
                Debug.Log("[LoginRequiredPopup] RectTransform fixed in Awake");
            }
            
            // KHÔNG gọi SetActive(false) ở đây nữa
            // Để popup inactive trong Inspector thay vì force trong code
        }

        private void OnDestroy()
        {
            loginButton?.onClick.RemoveAllListeners();
            cancelButton?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Hiển thị popup với message tùy chỉnh
        /// </summary>
        public void Show(string title, string message, System.Action onLogin, System.Action onCancel)
        {
            Debug.Log($"[LoginRequiredPopup] Show() called - Before: active={gameObject.activeSelf}, parent={transform.parent?.name}");
            
            // Kiểm tra parent có active không
            Transform current = transform.parent;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    Debug.LogWarning($"[LoginRequiredPopup] Parent '{current.name}' is INACTIVE! Activating...");
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            // Fix RectTransform TRƯỚC KHI SetActive
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
                Debug.Log($"[LoginRequiredPopup] Fixed RectTransform BEFORE SetActive");
            }
            
            // Set text
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            // Lưu callbacks
            onLoginCallback = onLogin;
            onCancelCallback = onCancel;

            // Đưa popup lên trên cùng (để không bị che bởi panels khác)
            transform.SetAsLastSibling();
            Debug.Log($"[LoginRequiredPopup] SetAsLastSibling() - sibling index: {transform.GetSiblingIndex()}");

            // Hiển thị popup
            gameObject.SetActive(true);
            
            Debug.Log($"[LoginRequiredPopup] After SetActive(true): active={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");
            
            // Kiểm tra RectTransform sau khi active
            if (rect != null)
            {
                Debug.Log($"[LoginRequiredPopup] RectTransform: anchorMin={rect.anchorMin}, anchorMax={rect.anchorMax}, sizeDelta={rect.sizeDelta}");
            }

            Debug.Log($"[LoginRequiredPopup] Showing popup: {title}");
        }

        /// <summary>
        /// Hiển thị popup với message mặc định
        /// </summary>
        public void Show(System.Action onLogin, System.Action onCancel)
        {
            Show(defaultTitle, defaultMessage, onLogin, onCancel);
        }

        /// <summary>
        /// Ẩn popup
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            onLoginCallback = null;
            onCancelCallback = null;

            Debug.Log("[LoginRequiredPopup] Popup hidden");
        }

        private void OnLoginButtonClicked()
        {
            Debug.Log("[LoginRequiredPopup] Login button clicked");

            // Gọi callback
            onLoginCallback?.Invoke();

            // Ẩn popup
            Hide();
        }

        private void OnCancelButtonClicked()
        {
            Debug.Log("[LoginRequiredPopup] Cancel button clicked");

            // Gọi callback
            onCancelCallback?.Invoke();

            // Ẩn popup
            Hide();
        }

        /// <summary>
        /// Tìm popup trong scene (singleton pattern)
        /// </summary>
        public static UILoginRequiredPopupController FindInScene()
        {
            return FindObjectOfType<UILoginRequiredPopupController>(true);
        }
    }
}
