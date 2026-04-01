using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Quan ly popup Settings dang cua so de len man hinh hien tai.
    /// Khong doi screen goc. Dong popup bang nut X (hoac backdrop/Escape tuy chon).
    /// </summary>
    public class UISettingsPopupController : MonoBehaviour
    {
        [Header("Khôi phục màn hình")]
        [LocalizedLabel("Root chứa các screen (tùy chọn)")]
        [SerializeField] private Transform screensRoot;

        [LocalizedLabel("Tự khôi phục màn trước khi đóng")]
        [SerializeField] private bool restorePreviousScreenOnClose = true;

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

                UnityAction action = openButtonsUseToggle ? Toggle : Open;
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
            CachePreviousActiveScreen();
            SetPopupVisible(true);
        }

        public void Close()
        {
            SetPopupVisible(false);

            if (!restorePreviousScreenOnClose)
                return;

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
                Debug.LogError($"[{nameof(UISettingsPopupController)}] popupRoot chưa được gán trên {name}");
                return;
            }

            popupRoot.SetActive(isVisible);
        }

        private void CachePreviousActiveScreen()
        {
            if (screensRoot == null)
            {
                previousActiveScreen = null;
                return;
            }

            previousActiveScreen = null;
            for (int i = 0; i < screensRoot.childCount; i++)
            {
                var child = screensRoot.GetChild(i)?.gameObject;
                if (child == null || child == popupRoot)
                    continue;

                if (child.activeSelf)
                {
                    previousActiveScreen = child;
                    break;
                }
            }
        }

        private void RestorePreviousScreenIfNeeded()
        {
            if (screensRoot == null || previousActiveScreen == null)
                return;

            bool hasAnyScreenActive = false;

            for (int i = 0; i < screensRoot.childCount; i++)
            {
                var child = screensRoot.GetChild(i)?.gameObject;
                if (child == null || child == popupRoot)
                    continue;

                if (child.activeSelf)
                {
                    hasAnyScreenActive = true;
                    break;
                }
            }

            if (!hasAnyScreenActive)
            {
                previousActiveScreen.SetActive(true);
            }
        }
    }
}
