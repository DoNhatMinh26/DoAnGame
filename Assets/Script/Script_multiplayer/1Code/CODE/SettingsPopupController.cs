using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller cho Settings Popup
    /// Hiển thị: Volume slider, Exit button, Close button
    /// Back to Menu button là optional (có thể null)
    /// </summary>
    public class SettingsPopupController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeValueText;
        [SerializeField] private Button exitGameButton;
        [SerializeField] private Button backToMenuButton; // Optional - có thể để null
        [SerializeField] private Button closeButton;

        [Header("Settings")]
        [SerializeField] private string menuSceneName = "GameUIPlay 1";

        private const string VOLUME_KEY = "GameVolume";

        private void Awake()
        {
            Debug.Log($"[SettingsPopup] Awake() - GameObject: {name}");

            // Gán sự kiện cho buttons
            if (exitGameButton != null)
            {
                exitGameButton.onClick.AddListener(OnExitGameClicked);
                Debug.Log("[SettingsPopup] Exit button listener added");
            }
            else
            {
                Debug.LogWarning("[SettingsPopup] Exit button is NULL!");
            }

            // Back to Menu button là optional
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
                Debug.Log("[SettingsPopup] Back to menu button listener added");
            }
            else
            {
                Debug.Log("[SettingsPopup] Back to menu button not assigned (optional)");
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
                Debug.Log("[SettingsPopup] Close button listener added");
            }
            else
            {
                Debug.LogWarning("[SettingsPopup] Close button is NULL!");
            }

            // Gán sự kiện cho volume slider
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                
                // Load volume đã lưu
                float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
                volumeSlider.value = savedVolume;
                UpdateVolumeText(savedVolume);
                
                Debug.Log($"[SettingsPopup] Volume slider initialized: {savedVolume}");
            }

            // Fix RectTransform
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
                Debug.Log("[SettingsPopup] RectTransform fixed");
            }
        }

        private void OnDestroy()
        {
            exitGameButton?.onClick.RemoveAllListeners();
            backToMenuButton?.onClick.RemoveAllListeners();
            closeButton?.onClick.RemoveAllListeners();
            volumeSlider?.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Hiển thị popup
        /// </summary>
        public void Show()
        {
            Debug.Log("[SettingsPopup] Show() called");

            // Đưa popup lên trên cùng
            transform.SetAsLastSibling();

            // Hiển thị popup
            gameObject.SetActive(true);
            
            Debug.Log($"[SettingsPopup] Popup shown - active: {gameObject.activeSelf}");
        }

        /// <summary>
        /// Hiển thị popup (alias cho Show)
        /// </summary>
        public void Open()
        {
            Show();
        }

        /// <summary>
        /// Toggle popup (bật/tắt)
        /// </summary>
        public void Toggle()
        {
            if (gameObject.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Ẩn popup
        /// </summary>
        public void Hide()
        {
            Debug.Log("[SettingsPopup] Hide() called");
            gameObject.SetActive(false);
        }

        private void OnVolumeChanged(float value)
        {
            // Lưu volume
            PlayerPrefs.SetFloat(VOLUME_KEY, value);
            PlayerPrefs.Save();

            // Cập nhật text hiển thị
            UpdateVolumeText(value);

            // TODO: Áp dụng volume vào AudioListener
            // AudioListener.volume = value;

            Debug.Log($"[SettingsPopup] Volume changed: {value:F2} ({Mathf.RoundToInt(value * 100)}%)");
        }

        private void UpdateVolumeText(float value)
        {
            if (volumeValueText != null)
            {
                volumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        private void OnExitGameClicked()
        {
            Debug.Log("[SettingsPopup] Exit game button clicked");

            #if UNITY_EDITOR
                Debug.Log("[SettingsPopup] Stopping play mode (Editor)");
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Debug.Log("[SettingsPopup] Quitting application");
                Application.Quit();
            #endif
        }

        private void OnBackToMenuClicked()
        {
            Debug.Log("[SettingsPopup] Back to menu button clicked");

            // Ẩn popup
            Hide();

            // Load menu scene
            if (!string.IsNullOrEmpty(menuSceneName))
            {
                Debug.Log($"[SettingsPopup] Loading scene: {menuSceneName}");
                UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
            }
            else
            {
                Debug.LogError("[SettingsPopup] Menu scene name is empty!");
            }
        }

        private void OnCloseClicked()
        {
            Debug.Log("[SettingsPopup] Close button clicked");
            Hide();
        }

        /// <summary>
        /// Tìm popup trong scene (singleton pattern)
        /// </summary>
        public static SettingsPopupController FindInScene()
        {
            return FindObjectOfType<SettingsPopupController>(true);
        }
    }
}
