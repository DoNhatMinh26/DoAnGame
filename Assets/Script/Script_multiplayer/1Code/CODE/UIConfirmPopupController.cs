using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DoAnGame.UI
{
    /// <summary>
    /// Popup xác nhận dùng chung — hiển thị tiêu đề, message và 2 nút tuỳ chọn.
    /// Dùng cho: Xác nhận đăng xuất, xác nhận xóa dữ liệu, v.v.
    ///
    /// Setup trong Inspector:
    ///   - titleText      → TMP_Text tiêu đề popup
    ///   - messageText    → TMP_Text nội dung
    ///   - confirmButton  → Button hành động chính (Đăng xuất, Xóa, v.v.)
    ///   - cancelButton   → Button Huỷ
    ///
    /// Cách dùng từ code:
    ///   popup.Show("Đăng xuất?", "Bạn có chắc muốn đăng xuất?",
    ///       confirmLabel: "Đăng xuất",
    ///       onConfirm: () => { /* logic đăng xuất */ },
    ///       onCancel:  () => { /* không làm gì */ });
    /// </summary>
    public class UIConfirmPopupController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI confirmButtonLabel; // Text trên nút confirm
        [SerializeField] private TextMeshProUGUI cancelButtonLabel;  // Text trên nút cancel (optional)

        [Header("Default Text")]
        [SerializeField] private string defaultConfirmLabel = "Xác nhận";
        [SerializeField] private string defaultCancelLabel  = "Huỷ";

        private System.Action onConfirmCallback;
        private System.Action onCancelCallback;

        private void Awake()
        {
            // Auto-find button labels nếu chưa gán
            if (confirmButtonLabel == null && confirmButton != null)
                confirmButtonLabel = confirmButton.GetComponentInChildren<TextMeshProUGUI>();

            if (cancelButtonLabel == null && cancelButton != null)
                cancelButtonLabel = cancelButton.GetComponentInChildren<TextMeshProUGUI>();

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            // Fix RectTransform về full-screen
            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin    = Vector2.zero;
                rect.anchorMax    = Vector2.one;
                rect.offsetMin    = Vector2.zero;
                rect.offsetMax    = Vector2.zero;
                rect.pivot        = new Vector2(0.5f, 0.5f);
                rect.localScale   = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
            }
        }

        private void OnDestroy()
        {
            confirmButton?.onClick.RemoveAllListeners();
            cancelButton?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Hiển thị popup với nội dung và callback tuỳ chỉnh.
        /// </summary>
        /// <param name="title">Tiêu đề popup</param>
        /// <param name="message">Nội dung thông báo</param>
        /// <param name="confirmLabel">Text nút xác nhận (mặc định: "Xác nhận")</param>
        /// <param name="onConfirm">Callback khi nhấn xác nhận</param>
        /// <param name="onCancel">Callback khi nhấn huỷ (có thể null)</param>
        /// <param name="cancelLabel">Text nút huỷ (mặc định: "Huỷ")</param>
        public void Show(
            string title,
            string message,
            string confirmLabel = null,
            System.Action onConfirm = null,
            System.Action onCancel  = null,
            string cancelLabel = null)
        {
            // Set text
            if (titleText != null)
                titleText.text = title;

            if (messageText != null)
                messageText.text = message;

            if (confirmButtonLabel != null)
                confirmButtonLabel.text = confirmLabel ?? defaultConfirmLabel;

            if (cancelButtonLabel != null)
                cancelButtonLabel.text = cancelLabel ?? defaultCancelLabel;

            // Lưu callbacks
            onConfirmCallback = onConfirm;
            onCancelCallback  = onCancel;

            // Đảm bảo parent active
            Transform current = transform.parent;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);
                current = current.parent;
            }

            // Fix RectTransform trước khi hiện
            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin    = Vector2.zero;
                rect.anchorMax    = Vector2.one;
                rect.offsetMin    = Vector2.zero;
                rect.offsetMax    = Vector2.zero;
                rect.pivot        = new Vector2(0.5f, 0.5f);
                rect.localScale   = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
            }

            // Đưa lên trên cùng để không bị che
            transform.SetAsLastSibling();

            gameObject.SetActive(true);
            Debug.Log($"[ConfirmPopup] Showing: '{title}'");
        }

        /// <summary>
        /// Ẩn popup và clear callbacks.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            onConfirmCallback = null;
            onCancelCallback  = null;
            Debug.Log("[ConfirmPopup] Hidden");
        }

        private void OnConfirmClicked()
        {
            Debug.Log("[ConfirmPopup] Confirm clicked");
            var cb = onConfirmCallback;
            Hide(); // ẩn trước để tránh double-click
            cb?.Invoke();
        }

        private void OnCancelClicked()
        {
            Debug.Log("[ConfirmPopup] Cancel clicked");
            var cb = onCancelCallback;
            Hide();
            cb?.Invoke();
        }
    }
}
