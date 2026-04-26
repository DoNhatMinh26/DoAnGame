using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DoAnGame.Auth;
using DoAnGame.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller cho button "Xóa dữ liệu" trong Settings
    /// </summary>
    public class UISettingsClearDataController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button clearDataButton;
        [SerializeField] private GameObject confirmPopup;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private TextMeshProUGUI messageText;

        private void Start()
        {
            if (clearDataButton != null)
            {
                clearDataButton.onClick.AddListener(OnClearDataButtonClicked);
            }

            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            }

            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.AddListener(OnConfirmNo);
            }

            // Ẩn popup ban đầu
            if (confirmPopup != null)
            {
                confirmPopup.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            clearDataButton?.onClick.RemoveAllListeners();
            confirmYesButton?.onClick.RemoveAllListeners();
            confirmNoButton?.onClick.RemoveAllListeners();
        }

        private void OnClearDataButtonClicked()
        {
            Debug.Log("[SettingsClearData] Clear data button clicked");

            // Hiển thị popup xác nhận
            if (confirmPopup != null)
            {
                if (messageText != null)
                {
                    messageText.text = "Bạn có chắc muốn xóa toàn bộ dữ liệu?\n\n" +
                                      "Điều này sẽ xóa:\n" +
                                      "• Tất cả tiến trình game\n" +
                                      "• Điểm số\n" +
                                      "• Avatar đã chọn\n" +
                                      "• Tên người chơi\n\n" +
                                      "Hành động này KHÔNG THỂ HOÀN TÁC!";
                }

                confirmPopup.SetActive(true);
            }
            else
            {
                // Không có popup → Xóa trực tiếp (không khuyến khích)
                Debug.LogWarning("[SettingsClearData] No confirm popup! Clearing data directly...");
                ClearAllData();
            }
        }

        private void OnConfirmYes()
        {
            Debug.Log("[SettingsClearData] User confirmed clear data");

            // Xóa tất cả dữ liệu
            ClearAllData();

            // Ẩn popup
            if (confirmPopup != null)
            {
                confirmPopup.SetActive(false);
            }

            // Hiển thị thông báo thành công
            ShowSuccessMessage();
        }

        private void OnConfirmNo()
        {
            Debug.Log("[SettingsClearData] User cancelled clear data");

            // Ẩn popup
            if (confirmPopup != null)
            {
                confirmPopup.SetActive(false);
            }
        }

        private void ClearAllData()
        {
            // Xóa dữ liệu guest
            if (UIQuickPlayNameController.IsGuestMode())
            {
                LocalProgressService.Instance.ClearAllData();
                UIQuickPlayNameController.ClearGuestData();
                Debug.Log("[SettingsClearData] Cleared guest data");
            }
            else
            {
                Debug.LogWarning("[SettingsClearData] User is logged in, cannot clear Firebase data from here");
                // Nếu muốn xóa Firebase data, cần implement riêng
            }

            // Có thể thêm: Xóa PlayerPrefs khác (settings, preferences...)
            // PlayerPrefs.DeleteAll(); // ⚠️ Cẩn thận: Xóa TẤT CẢ PlayerPrefs!
        }

        private void ShowSuccessMessage()
        {
            Debug.Log("[SettingsClearData] Data cleared successfully!");
            
            // TODO: Hiển thị toast/notification
            // Hoặc chuyển về WelcomePanel
        }
    }
}
