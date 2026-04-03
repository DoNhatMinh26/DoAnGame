using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Bổ sung thư viện quản lý Scene
using UnityEngine.EventSystems;

namespace DoAnGame.UI
{
    /// <summary>
    /// Script toi gian: gan truc tiep vao Button de chuyen man hinh hoặc chuyển Scene mới.
    /// Rule: an tat ca screen trong root, sau do hien screen dich HOẶC load Scene khác.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonScreenNavigator : MonoBehaviour
    {
        [Header("Chuyển Scene (Ưu Tiên Cao Nhất)")]
#if UNITY_EDITOR
        [LocalizedLabel("Kéo thả file Scene vào đây (để nó tự lấy tên)")]
        [SerializeField] private UnityEditor.SceneAsset sceneAsset;
#endif
        [LocalizedLabel("Tên Scene (Để trống nếu chỉ chuyển UI)")]
        [SerializeField] private string targetSceneName;

        [Header("Gốc Màn Hình (Chung Canvas)")]
        [LocalizedLabel("Danh sách màn hình (Screens Root)")]
        [SerializeField] private Transform screensRoot;

        [Header("Điều Hướng Chính")]
        [LocalizedLabel("Màn hình đích (Target Screen)")]
        [SerializeField] private GameObject targetScreen;

        [Header("Chuyển Canvas/Root Khác (Tùy Chọn)")]
        [LocalizedLabel("Các Root/Canvas cần Ẩn đi")]
        [SerializeField] private GameObject[] rootsToHide;
        
        [LocalizedLabel("Các Root/Canvas cần Hiện lên")]
        [SerializeField] private GameObject[] rootsToShow;

        [Header("Thiết Lập Khởi Đầu (Tùy Chọn)")]
        [LocalizedLabel("Khởi tạo trạng thái lúc Awake")]
        [SerializeField] private bool initializeStartStateOnAwake;
        [LocalizedLabel("Màn hình bắt đầu")]
        [SerializeField] private GameObject startScreen;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs;

        private Button cachedButton;

        private void Awake()
        {
            cachedButton = GetComponent<Button>();
            cachedButton.onClick.AddListener(HandleClick);

            if (initializeStartStateOnAwake)
            {
                if (startScreen == null)
                {
                    Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}] startScreen chua duoc gan tren {name}.");
                }
                else
                {
                    SwitchTo(startScreen);
                }
            }
        }

        private void OnDestroy()
        {
            cachedButton?.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            // 1. Kiểm tra nếu có nhập tên Scene -> Ưu tiên chuyển Scene
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Log($"LoadScene -> {targetSceneName}");
                SceneManager.LoadScene(targetSceneName);
                return; // Dừng, không tiếp tục xử lý UI nữa
            }

            if (!HasUiDestination())
            {
                Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}:{name}] Chưa cấu hình đích UI (targetScreen/rootsToShow). Bỏ qua điều hướng để tránh màn hình trống.");
                return;
            }

            // 2. Xử lý chuyển UI nếu không chuyển Scene
            SwitchTo(targetScreen);
        }

        /// <summary>
        /// Cho phép script khác gọi điều hướng bằng code.
        /// Dùng cho case host bắt đầu trận và muốn cả 2 client chuyển màn đồng bộ.
        /// </summary>
        public void NavigateNow()
        {
            HandleClick();
        }

        private void SwitchTo(GameObject screenToShow)
        {
            if (screenToShow == null && !HasRootsToShow())
            {
                Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}:{name}] Không có targetScreen và rootsToShow trống. Không thực hiện ẩn/hiện để tránh blank UI.");
                return;
            }

            // A. Tắt toàn bộ anh em trong root (nếu dùng chung 1 Menu/Canvas)
            if (screensRoot != null)
            {
                for (int i = 0; i < screensRoot.childCount; i++)
                {
                    var child = screensRoot.GetChild(i);
                    if (child != null)
                    {
                        if (ShouldSkipAutoHide(child.gameObject))
                        {
                            Log($"Skip auto-hide: {child.gameObject.name}");
                            continue;
                        }

                        child.gameObject.SetActive(false);
                    }
                }
            }

            // B. Ẩn hẳn các Canvas/Root cũ theo chỉ định ở mảng rootsToHide
            if (rootsToHide != null && rootsToHide.Length > 0)
            {
                foreach (var oldRoot in rootsToHide)
                {
                    if (oldRoot != null) oldRoot.SetActive(false);
                }
            }

            // C. Hiện toàn bộ Canvas/Root mới theo chỉ định ở mảng rootsToShow
            if (rootsToShow != null && rootsToShow.Length > 0)
            {
                foreach (var newRoot in rootsToShow)
                {
                    if (newRoot != null) newRoot.SetActive(true);
                }
            }

            // D. Cuối cùng, bật màn hình đích (Target Screen)
            if (screenToShow != null)
            {
                screenToShow.SetActive(true);
                Log($"SwitchTo -> {screenToShow.name}");
            }
            else
            {
                Log("SwitchTo -> targetScreen null, dùng rootsToShow");
            }
        }

        private bool HasUiDestination()
        {
            return targetScreen != null || HasRootsToShow();
        }

        private bool ShouldSkipAutoHide(GameObject go)
        {
            if (go == null)
                return true;

            // EventSystem phải luôn bật để UI nhận click.
            return go.GetComponent<EventSystem>() != null;
        }

        private bool HasRootsToShow()
        {
            if (rootsToShow == null || rootsToShow.Length == 0)
                return false;

            for (int i = 0; i < rootsToShow.Length; i++)
            {
                if (rootsToShow[i] != null)
                    return true;
            }

            return false;
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] {message}");
        }

        [ContextMenu("Apply Start State")]
        private void ApplyStartStateInEditor()
        {
            if (startScreen == null)
            {
                Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}] startScreen chua duoc gan tren {name}.");
                return;
            }

            SwitchTo(startScreen);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Tự động gán tên Scene nếu bạn kéo thả cái file Scene (hình khối vuông) vào đây
            if (sceneAsset != null && sceneAsset.name != targetSceneName)
            {
                targetSceneName = sceneAsset.name;
            }
        }
#endif
    }
}
