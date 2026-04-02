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

        [Header("Debug")]
        [LocalizedLabel("Bật log debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private readonly List<(Button button, UnityAction action)> openButtonHandlers = new List<(Button, UnityAction)>();
        private readonly List<GameObject> previouslyActiveObjects = new List<GameObject>();
        private GameObject lastObservedActiveScreen;

        private void Awake()
        {
            LogDebug($"Awake | screensRoot={(screensRoot != null ? screensRoot.name : "null")} | popupRoot={(popupRoot != null ? popupRoot.name : "null")}");
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
                LogDebug("Start | hideOnStart=true => popup hidden");
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
            {
                LogDebug("RegisterOpenButtons | no buttons configured");
                return;
            }

            foreach (var openButton in openButtons)
            {
                if (openButton == null)
                    continue;

                UnityAction action = openButtonsUseToggle ? Toggle : Open;
                openButton.onClick.AddListener(action);
                openButtonHandlers.Add((openButton, action));
                LogDebug($"RegisterOpenButtons | bound button={openButton.name} mode={(openButtonsUseToggle ? "Toggle" : "Open")}");
            }

            LogDebug($"RegisterOpenButtons | total bound={openButtonHandlers.Count}");
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
            if (!IsOpen())
            {
                TrackLastActiveScreen();
            }

            if (!closeOnEscape)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) && IsOpen())
            {
                Close();
            }
        }

        public void Open()
        {
            LogDebug("Open | begin");
            CachePreviouslyActiveObjects();
            SetPopupVisible(true);
            LogDebug($"Open | popup now open={IsOpen()} | cachedCount={previouslyActiveObjects.Count}");
        }

        public void Close()
        {
            LogDebug("Close | begin");
            SetPopupVisible(false);
            LogDebug($"Close | popup now open={IsOpen()}");

            if (!restorePreviousScreenOnClose)
            {
                LogDebug("Close | restorePreviousScreenOnClose=false, skip restore");
                return;
            }

            RestorePreviousScreenIfNeeded();
            LogDebug("Close | restore finished");
        }

        public void Toggle()
        {
            LogDebug($"Toggle | current open={IsOpen()}");
            if (IsOpen())
            {
                Close();
            }
            else
            {
                Open();
            }
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
            LogDebug($"SetPopupVisible | popup={popupRoot.name} active={isVisible}");
        }

        private void CachePreviousActiveScreen()
        {
            var effectiveRoot = GetEffectiveScreensRoot();
            if (effectiveRoot == null)
            {
                previouslyActiveObjects.Clear();
                LogDebug("Cache | effectiveRoot=null, nothing cached");
                return;
            }

            previouslyActiveObjects.Clear();
            CollectActiveObjects(effectiveRoot);
            LogDebug($"Cache | effectiveRoot={effectiveRoot.name} | cachedCount={previouslyActiveObjects.Count}");
        }

        private void RestorePreviousScreenIfNeeded()
        {
            if (previouslyActiveObjects.Count == 0)
            {
                LogDebug("Restore | cachedCount=0, nothing to restore");
                RestoreFallbackScreenIfNeeded();
                return;
            }

            int restoredCount = 0;

            for (int i = 0; i < previouslyActiveObjects.Count; i++)
            {
                var go = previouslyActiveObjects[i];
                if (go == null)
                    continue;

                if (!go.activeSelf)
                {
                    go.SetActive(true);
                    restoredCount++;
                    LogDebug($"Restore | reactivated={go.name}");
                }
            }

            LogDebug($"Restore | done restoredCount={restoredCount}");

            if (restoredCount == 0)
            {
                RestoreFallbackScreenIfNeeded();
            }
        }

        private void CachePreviouslyActiveObjects()
        {
            CachePreviousActiveScreen();
        }

        private void CollectActiveObjects(Transform node)
        {
            if (node == null)
                return;

            if (popupRoot != null && (node == popupRoot.transform || node.IsChildOf(popupRoot.transform)))
                return;

            var go = node.gameObject;
            if (go.activeSelf)
            {
                previouslyActiveObjects.Add(go);
            }

            for (int i = 0; i < node.childCount; i++)
            {
                CollectActiveObjects(node.GetChild(i));
            }
        }

        [ContextMenu("Debug/Simulate Open Close")]
        private void SimulateOpenClose()
        {
            Open();
            Close();
            Debug.Log($"[{nameof(UISettingsPopupController)}] Simulate Open/Close xong. Da luu {previouslyActiveObjects.Count} doi tuong active de khoi phuc.");
        }

        private Transform GetEffectiveScreensRoot()
        {
            if (popupRoot != null && popupRoot.transform.parent != null)
            {
                if (screensRoot == null)
                {
                    LogDebug($"GetEffectiveScreensRoot | screensRoot null => use popup parent {popupRoot.transform.parent.name}");
                    return popupRoot.transform.parent;
                }

                bool popupIsDirectChild = popupRoot.transform.parent == screensRoot;
                if (!popupIsDirectChild)
                {
                    LogDebug($"GetEffectiveScreensRoot | screensRoot mismatch => use popup parent {popupRoot.transform.parent.name}");
                    return popupRoot.transform.parent;
                }
            }

            LogDebug($"GetEffectiveScreensRoot | use configured root {(screensRoot != null ? screensRoot.name : "null")}");

            return screensRoot;
        }

        private void TrackLastActiveScreen()
        {
            var effectiveRoot = GetEffectiveScreensRoot();
            if (effectiveRoot == null)
                return;

            for (int i = 0; i < effectiveRoot.childCount; i++)
            {
                var child = effectiveRoot.GetChild(i)?.gameObject;
                if (child == null || child == popupRoot)
                    continue;

                if (child.activeSelf)
                {
                    lastObservedActiveScreen = child;
                    return;
                }
            }
        }

        private void RestoreFallbackScreenIfNeeded()
        {
            if (lastObservedActiveScreen == null)
            {
                LogDebug("RestoreFallback | no lastObservedActiveScreen");
                return;
            }

            if (!lastObservedActiveScreen.activeSelf)
            {
                lastObservedActiveScreen.SetActive(true);
                LogDebug($"RestoreFallback | reactivated lastObserved={lastObservedActiveScreen.name}");
            }
            else
            {
                LogDebug($"RestoreFallback | lastObserved already active={lastObservedActiveScreen.name}");
            }
        }

        private void OnValidate()
        {
            if (screensRoot != null && popupRoot != null && popupRoot.transform.parent != screensRoot)
            {
                Debug.LogWarning($"[{nameof(UISettingsPopupController)}] screensRoot đang không cùng cấp trực tiếp với popupRoot trên {name}. Script sẽ tự dùng parent của popupRoot để khôi phục màn trước.");
            }
        }

        private void LogDebug(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[{nameof(UISettingsPopupController)}:{name}] {message}");
        }
    }
}
