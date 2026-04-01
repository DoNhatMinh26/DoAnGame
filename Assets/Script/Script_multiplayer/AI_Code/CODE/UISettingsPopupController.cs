using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Quan ly popup Settings dang cua so de len man hinh hien tai.
    /// Khong doi screen goc. Dong popup bang nut X (hoac backdrop/Escape tuy chon).
    /// Luu reference den screen parent truoc khi Settings mo.
    /// </summary>
    public class UISettingsPopupController : MonoBehaviour
    {
        [Header("Khôi phục màn hình")]
        [LocalizedLabel("Parent canvas chứa tất cả screens")]
        [SerializeField] private Transform screensRoot;

        [LocalizedLabel("Tự khôi phục màn trước khi đóng")]
        [SerializeField] private bool restorePreviousScreenOnClose = true;
        
        [LocalizedLabel("Debug mode - show restore messages")]
        [SerializeField] private bool debugMode = true;

        [Header("Nút mở Settings")]
        [LocalizedLabel("Danh sách nút mở Settings")]
        [SerializeField] private Button[] openButtons;

        [LocalizedLabel("Nút mở dùng chế độ bật/tắt")]
        [SerializeField] private bool openButtonsUseToggle;

        [Header("Popup Settings")]
        [LocalizedLabel("Root popup (GameObject)")]
        [SerializeField] private GameObject popupRoot;

        [LocalizedLabel("Nút đóng (X)")]
        [SerializeField] private Button closeButton;

        [LocalizedLabel("Nút nền mờ (tùy chọn)")]
        [SerializeField] private Button backdropButton;

        [LocalizedLabel("Ẩn popup khi bắt đầu")]
        [SerializeField] private bool hideOnStart = true;

        [LocalizedLabel("Đóng bằng phím Escape")]
        [SerializeField] private bool closeOnEscape = true;

        private readonly List<(Button button, UnityAction action)> openButtonHandlers = new List<(Button, UnityAction)>();
        private GameObject previousActiveScreen;
        
        // Global cache for last non-settings screen.
        private static GameObject lastActiveScreen;

        public static void SetLastActiveScreen(GameObject screen)
        {
            if (screen == null)
                return;

            // Never cache Settings popup as previous screen.
            if (screen.name.Contains("Setting"))
                return;

            lastActiveScreen = screen;
        }

        /// <summary>
        /// Capture source screen from the clicked button hierarchy.
        /// Works even if that screen is already deactivated in the same frame.
        /// </summary>
        public void CapturePreviousScreenFrom(Transform source)
        {
            if (source == null || screensRoot == null)
                return;

            Transform cursor = source;
            while (cursor != null && cursor != screensRoot)
            {
                if (cursor.parent == screensRoot)
                {
                    var candidate = cursor.gameObject;
                    if (candidate != popupRoot && !candidate.name.Contains("Setting"))
                    {
                        previousActiveScreen = candidate;
                        lastActiveScreen = candidate;

                        if (debugMode)
                            Debug.Log($"[Settings] Captured source screen: {candidate.name}");
                    }
                    return;
                }

                cursor = cursor.parent;
            }
        }

        private void Awake()
        {
            RegisterOpenButtons();

            closeButton?.onClick.AddListener(Close);
            backdropButton?.onClick.AddListener(Close);
        }

        private void Start()
        {
            // Luôn đảm bảo popup bị ẩn khi game bắt đầu chạy
            if (hideOnStart)
            {
                SetPopupVisible(false);
            }
        }

        private void OnDestroy()
        {
            UnregisterOpenButtons();
            closeButton?.onClick.RemoveListener(Close);
            backdropButton?.onClick.RemoveListener(Close);
        }

        private void RegisterOpenButtons()
        {
            if (openButtons == null || openButtons.Length == 0)
                return;

            foreach (var openButton in openButtons)
            {
                if (openButton == null)
                    continue;

                UnityAction action = () =>
                {
                    CapturePreviousScreenFrom(openButton.transform);

                    if (openButtonsUseToggle)
                    {
                        Toggle();
                    }
                    else
                    {
                        Open();
                    }
                };

                openButton.onClick.AddListener(action);
                openButtonHandlers.Add((openButton, action));
            }
        }

        private void UnregisterOpenButtons()
        {
            if (openButtonHandlers.Count == 0)
                return;

            foreach (var handler in openButtonHandlers)
            {
                handler.button?.onClick.RemoveListener(handler.action);
            }

            openButtonHandlers.Clear();
        }

        private void Update()
        {
            if (!closeOnEscape)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen())
            {
                Close();
            }
        }

        public void Open()
        {
            if (debugMode)
                Debug.Log("[Settings] 🔓 Opening Settings popup...");
            
            // ⚠️ CRITICAL: Cache BEFORE popup shows (before any screen gets disabled)
            CachePreviousActiveScreen();
            
            SetPopupVisible(true);
        }

        public void Close()
        {
            if (debugMode)
                Debug.Log("[Settings] 🔒 Closing Settings popup...");
            
            // Hide popup FIRST
            SetPopupVisible(false);

            if (!restorePreviousScreenOnClose)
            {
                if (debugMode)
                    Debug.Log("[Settings] Restore disabled");
                return;
            }

            // Then restore screen after a tiny delay to let UI update
            StartCoroutine(RestoreScreenDelayed());
        }

        private System.Collections.IEnumerator RestoreScreenDelayed()
        {
            yield return new WaitForEndOfFrame(); // Wait 1 frame
            RestorePreviousScreenIfNeeded();
        }

        public void Toggle()
        {
            SetPopupVisible(!IsOpen());
        }

        public bool IsOpen()
        {
            return popupRoot != null && popupRoot.activeSelf;
        }

        private void SetPopupVisible(bool isVisible)
        {
            if (popupRoot == null)
            {
                Debug.LogError($"[{nameof(UISettingsPopupController)}] ❌ popupRoot chưa được gán trên {name}");
                return;
            }

            popupRoot.SetActive(isVisible);
            if (debugMode)
                Debug.Log($"[Settings] {(isVisible ? "✅ Popup shown" : "⬜ Popup hidden")}");
        }

        private void CachePreviousActiveScreen()
        {
            // Prefer explicit capture from clicked button (if already set).
            if (previousActiveScreen != null && previousActiveScreen != popupRoot && !previousActiveScreen.name.Contains("Setting"))
            {
                if (debugMode)
                    Debug.Log($"[Settings] Using explicitly captured screen: {previousActiveScreen.name}");
                return;
            }

            // Second: global cache from navigator/open-button.
            if (lastActiveScreen != null)
            {
                if (!lastActiveScreen.name.Contains("Setting"))
                {
                    previousActiveScreen = lastActiveScreen;
                    if (debugMode)
                        Debug.Log($"[Settings] Using global cached screen: {lastActiveScreen.name}");
                    return;
                }
                else
                {
                    if (debugMode)
                        Debug.Log("[Settings] Global cache points to Settings, ignored");
                }
            }

            // Fallback: scan screensRoot for active screen
            if (screensRoot == null)
            {
                Debug.LogWarning("[Settings] screensRoot is not assigned");
                previousActiveScreen = null;
                return;
            }

            previousActiveScreen = null;
            for (int i = 0; i < screensRoot.childCount; i++)
            {
                var child = screensRoot.GetChild(i)?.gameObject;
                if (child == null || child == popupRoot || !child.activeSelf)
                    continue;

                if (!child.name.Contains("Setting"))
                {
                    previousActiveScreen = child;
                    lastActiveScreen = child;
                    if (debugMode)
                        Debug.Log($"[Settings] Scanned active screen: {child.name}");
                    return;
                }
            }

            if (debugMode)
                Debug.LogWarning("[Settings] No active screen found");
        }

        private void RestorePreviousScreenIfNeeded()
        {
            if (previousActiveScreen == null)
            {
                Debug.LogWarning("[Settings] previousActiveScreen is null. Cannot restore");
                return;
            }

            // Reject invalid target.
            if (previousActiveScreen == popupRoot || previousActiveScreen.name.Contains("Setting"))
            {
                Debug.LogWarning("[Settings] previousActiveScreen is invalid for restore");
                return;
            }

            // Enable the screen
            if (!previousActiveScreen.activeSelf)
            {
                previousActiveScreen.SetActive(true);
                if (debugMode)
                    Debug.Log($"[Settings] Restored screen: {previousActiveScreen.name}");
            }
            else
            {
                if (debugMode)
                    Debug.Log($"[Settings] Screen already active: {previousActiveScreen.name}");
            }
        }
    }
}
