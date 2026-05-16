using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller cho Settings Popup
    /// Hiển thị: Music Volume slider, SFX Volume slider, Exit button, Close button
    /// Back to Menu button là optional (có thể null)
    /// </summary>
    public class SettingsPopupController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Header("Music Volume Controls")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TextMeshProUGUI musicValueText;
        
        [Header("SFX Volume Controls")]
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI sfxValueText;
        
        [Header("Buttons")]
        [SerializeField] private Button exitGameButton;
        [SerializeField] private Button backToMenuButton; // Optional - có thể để null
        [SerializeField] private Button closeButton;

        [Header("Settings")]
        [SerializeField] private string menuSceneName = "GameUIPlay 1";

        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";

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

            // Gán sự kiện cho music volume slider
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                
                // Load music volume đã lưu
                float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
                musicSlider.value = savedMusicVolume;
                UpdateMusicVolumeText(savedMusicVolume);
                
                // Áp dụng volume vào AudioManager
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMusicVolume(savedMusicVolume);
                }
                
                Debug.Log($"[SettingsPopup] Music slider initialized: {savedMusicVolume}");
            }
            else
            {
                Debug.LogWarning("[SettingsPopup] Music slider is NULL!");
            }

            // Gán sự kiện cho SFX volume slider
            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
                
                // Load SFX volume đã lưu
                float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
                sfxSlider.value = savedSFXVolume;
                UpdateSFXVolumeText(savedSFXVolume);
                
                // Áp dụng volume vào AudioManager
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetSFXVolume(savedSFXVolume);
                }
                
                Debug.Log($"[SettingsPopup] SFX slider initialized: {savedSFXVolume}");
            }
            else
            {
                Debug.LogWarning("[SettingsPopup] SFX slider is NULL!");
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
            musicSlider?.onValueChanged.RemoveAllListeners();
            sfxSlider?.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Hiển thị popup
        /// </summary>
        public void Show()
        {
            Debug.Log("[SettingsPopup] Show() called");

            // Load và áp dụng music volume từ PlayerPrefs
            float savedMusicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
            if (musicSlider != null)
            {
                musicSlider.value = savedMusicVolume;
            }

            // Load và áp dụng SFX volume từ PlayerPrefs
            float savedSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            if (sfxSlider != null)
            {
                sfxSlider.value = savedSFXVolume;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(savedMusicVolume);
                AudioManager.Instance.SetSFXVolume(savedSFXVolume);
            }

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

        /// <summary>
        /// Xử lý khi music volume thay đổi
        /// </summary>
        private void OnMusicVolumeChanged(float value)
        {
            // Lưu music volume
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
            PlayerPrefs.Save();

            // Cập nhật text hiển thị
            UpdateMusicVolumeText(value);

            // Áp dụng volume vào AudioManager
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(value);
                Debug.Log($"[SettingsPopup] Music volume updated: {value:F2} ({Mathf.RoundToInt(value * 100)}%)");
            }
            else
            {
                Debug.LogWarning("[SettingsPopup] AudioManager.Instance is NULL!");
            }
        }

        /// <summary>
        /// Xử lý khi SFX volume thay đổi
        /// </summary>
        private void OnSFXVolumeChanged(float value)
        {
            // Lưu SFX volume
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
            PlayerPrefs.Save();

            // Cập nhật text hiển thị
            UpdateSFXVolumeText(value);

            // Áp dụng volume vào AudioManager
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
                Debug.Log($"[SettingsPopup] SFX volume updated: {value:F2} ({Mathf.RoundToInt(value * 100)}%)");
            }
            else
            {
                Debug.LogWarning("[SettingsPopup] AudioManager.Instance is NULL!");
            }
        }

        /// <summary>
        /// Cập nhật text hiển thị music volume
        /// </summary>
        private void UpdateMusicVolumeText(float value)
        {
            if (musicValueText != null)
            {
                musicValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        /// <summary>
        /// Cập nhật text hiển thị SFX volume
        /// </summary>
        private void UpdateSFXVolumeText(float value)
        {
            if (sfxValueText != null)
            {
                sfxValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
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
