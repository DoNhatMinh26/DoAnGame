using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Bổ sung thư viện quản lý Scene
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

namespace DoAnGame.UI
{
    /// <summary>
    /// Script toi gian: gan truc tiep vao Button de chuyen man hinh hoặc chuyển Scene mới.
    /// Rule: an tat ca screen trong root, sau do hien screen dich HOẶC load Scene khác.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonScreenNavigator : MonoBehaviour
    {
        [Serializable]
        private struct PendingSceneUiTargetRequest
        {
            public bool IsValid;
            public string RequestId;
            public string SceneName;
            public string TargetPanelName;
            public bool ScanAllCanvases;
            public bool AllowPartialNameMatch;
            public bool AutoHideSiblingPanels;
            public string TriggerNavigatorName;
        }

        private static bool sceneLoadHookRegistered;
        private static PendingSceneUiTargetRequest pendingSceneUiTargetRequest;

        [Header("Chuyển Scene (Ưu Tiên Cao Nhất)")]
    #if UNITY_EDITOR
        [LocalizedLabel("Kéo thả file Scene vào đây (để nó tự lấy tên)")]
        [SerializeField] private UnityEditor.SceneAsset sceneAsset;
    #endif
        [LocalizedLabel("Tên Scene (Để trống nếu chỉ chuyển UI)")]
        [SerializeField] private string targetSceneName;

        [Header("Panel UI đích (tự động)")]
        [SerializeField, HideInInspector] private string destinationTargetPanelName;

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
            EnsureSceneLoadHookRegistered();

            cachedButton = GetComponent<Button>();
            cachedButton.onClick.AddListener(HandleClick);

            EnsureRootTransformIsUsable();

            if (initializeStartStateOnAwake)
            {
                bool canInitializeFromHere = screensRoot != null && transform.parent == screensRoot;
                if (!canInitializeFromHere)
                {
                    Log("Skip start-state init because navigator is not on a top-level screen object.");
                }
                else if (startScreen == null)
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
            MultiplayerDetailedLogger.TraceUserAction($"UIButtonScreenNavigator:{name}", "ButtonClick", $"targetScene={targetSceneName},targetScreen={(targetScreen != null ? targetScreen.name : "null")}");
            if (!isActiveAndEnabled)
            {
                Log("Click ignored because navigator is disabled.");
                return;
            }

            ExecuteNavigation();
        }

        /// <summary>
        /// Cho phép script khác gọi điều hướng bằng code.
        /// Dùng cho case host bắt đầu trận và muốn cả 2 client chuyển màn đồng bộ.
        /// </summary>
        public void NavigateNow()
        {
            ExecuteNavigation();
        }

        private void ExecuteNavigation()
        {
            Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] Click detected. targetSceneName='{targetSceneName}', targetScreen='{(targetScreen != null ? targetScreen.name : "null")}'");
            MultiplayerDetailedLogger.Trace("UI_NAV", $"ExecuteNavigation name={name}, targetScene={targetSceneName}, targetScreen={(targetScreen != null ? targetScreen.name : "null")}");

            // 1. Kiểm tra nếu có nhập tên Scene -> Ưu tiên chuyển Scene
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                QueuePendingSceneUiTargetRequestIfNeeded();
                
                // ✅ TẠO RUNNER TRƯỚC KHI LOAD SCENE
                CreateDeferredResolverBeforeSceneLoad();
                
                Log($"LoadScene -> {targetSceneName}");
                MultiplayerDetailedLogger.Trace("UI_NAV", $"LoadScene requested: {targetSceneName}");
                SceneManager.LoadScene(targetSceneName);
                return; // Dừng, không tiếp tục xử lý UI nữa
            }

            // 2. Kiểm tra UI destination chỉ khi KHÔNG có targetSceneName
            if (!HasUiDestinationForCurrentScene())
            {
                Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}:{name}] Chưa cấu hình đích UI trong scene hiện tại (targetScreen/rootsToShow). Bỏ qua điều hướng để tránh màn hình trống.");
                return;
            }

            // 3. Xử lý chuyển UI trong cùng scene
            Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] Switching UI to '{(targetScreen != null ? targetScreen.name : "rootsToShow")}'");
            MultiplayerDetailedLogger.Trace("UI_NAV", $"SwitchUI requested: {(targetScreen != null ? targetScreen.name : "rootsToShow")}");
            SwitchTo(targetScreen);
        }

        private void CreateDeferredResolverBeforeSceneLoad()
        {
            if (!pendingSceneUiTargetRequest.IsValid)
            {
                Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}] CreateDeferredResolverBeforeSceneLoad: No pending request!");
                return;
            }

            Debug.Log($"[{nameof(UIButtonScreenNavigator)}] Creating DeferredSceneUiResolveRunner BEFORE scene load: targetScene='{pendingSceneUiTargetRequest.SceneName}', targetPanel='{pendingSceneUiTargetRequest.TargetPanelName}'");

            var runnerGo = new GameObject("[UIButtonScreenNavigator] DeferredSceneUiResolve");
            DontDestroyOnLoad(runnerGo);
            var runner = runnerGo.AddComponent<DeferredSceneUiResolveRunner>();
            runner.InitializeBeforeSceneLoad(pendingSceneUiTargetRequest);

            Debug.Log($"[{nameof(UIButtonScreenNavigator)}] DeferredSceneUiResolveRunner created and marked DontDestroyOnLoad.");
        }

        private void SwitchTo(GameObject screenToShow)
        {
            if (screenToShow == null && !HasRootsToShow())
            {
                Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}:{name}] Không có targetScreen và rootsToShow trống. Không thực hiện ẩn/hiện để tránh blank UI.");
                return;
            }

            EnsureRootTransformIsUsable();

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
                EnsureScreenTransformIsVisible(screenToShow);
                screenToShow.SetActive(true);
                screenToShow.transform.SetAsLastSibling();
                Log($"SwitchTo -> {screenToShow.name}");
                MultiplayerDetailedLogger.Trace("UI_NAV", $"SwitchTo success: {screenToShow.name}");
            }
            else
            {
                Log("SwitchTo -> targetScreen null, dùng rootsToShow");
                MultiplayerDetailedLogger.Trace("UI_NAV", "SwitchTo success via rootsToShow");
            }
        }

        private void EnsureRootTransformIsUsable()
        {
            if (screensRoot == null)
                return;

            var rootRect = screensRoot as RectTransform;
            if (rootRect == null)
                return;

            // Scene merge có thể làm root scale hỏng; ép về (1,1,1) để UI luôn nhìn thấy.
            if (rootRect.localScale != Vector3.one)
            {
                rootRect.localScale = Vector3.one;
                Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}:{name}] Auto-fix screensRoot scale to (1,1,1).");
            }
        }

        private void EnsureScreenTransformIsVisible(GameObject screen)
        {
            if (screen == null)
                return;

            EnsureParentsActive(screen.transform);

            var rect = screen.transform as RectTransform;
            if (rect == null)
                return;

            // Luôn normalize panel đích về full-screen để tránh lệch toạ độ sau merge scene.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;

            Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}:{name}] Auto-normalized panel '{screen.name}' to full-screen.");
        }

        private void EnsureParentsActive(Transform node)
        {
            var current = node;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
        }

        private bool HasUiDestination()
        {
            // ✅ Cho phép navigation nếu có targetSceneName (cho nút Back)
            return !string.IsNullOrEmpty(targetSceneName) || targetScreen != null || HasRootsToShow();
        }

        private bool HasUiDestinationForCurrentScene()
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

        private void QueuePendingSceneUiTargetRequestIfNeeded()
        {
            string panelName = ResolveDestinationTargetPanelName();
            
            // ✅ DEBUG: Log tất cả giá trị
            Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] QueuePendingSceneUiTargetRequestIfNeeded:");
            Debug.Log($"  - destinationTargetPanelName field = '{destinationTargetPanelName}'");
            Debug.Log($"  - targetScreen = {(targetScreen != null ? targetScreen.name : "null")}");
            Debug.Log($"  - Resolved panelName = '{panelName}'");
            
            // ✅ CHỈ log warning nếu THỰC SỰ không có panel name
            // KHÔNG override nếu đã có giá trị
            if (string.IsNullOrWhiteSpace(panelName))
            {
                Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] Back navigation - không có panel đích cụ thể. Sẽ activate Canvas đầu tiên.");
                panelName = ""; // Empty string signals "activate first canvas"
            }
            else
            {
                Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] Scene navigation với panel đích: '{panelName}'");
            }

            pendingSceneUiTargetRequest = new PendingSceneUiTargetRequest
            {
                IsValid = true,
                RequestId = System.Guid.NewGuid().ToString(),
                SceneName = targetSceneName,
                TargetPanelName = panelName,
                ScanAllCanvases = true,
                AllowPartialNameMatch = true, // ✅ Enable partial match để dễ tìm hơn
                AutoHideSiblingPanels = true,
                TriggerNavigatorName = name
            };

            MultiplayerDetailedLogger.Trace(
                "UI_NAV",
                $"Queued scene UI target request: scene={targetSceneName}, panel={panelName}, requestId={pendingSceneUiTargetRequest.RequestId}");
        }

        private string ResolveDestinationTargetPanelName()
        {
            Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] ResolveDestinationTargetPanelName: destinationTargetPanelName='{destinationTargetPanelName}', targetScreen={(targetScreen != null ? targetScreen.name : "null")}");
            
            if (!string.IsNullOrWhiteSpace(destinationTargetPanelName))
            {
                Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] Using destinationTargetPanelName: '{destinationTargetPanelName}'");
                return destinationTargetPanelName.Trim();
            }

            if (targetScreen != null)
            {
                Debug.Log($"[{nameof(UIButtonScreenNavigator)}:{name}] Using targetScreen.name: '{targetScreen.name}'");
                return targetScreen.name;
            }

            Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}:{name}] No destination panel name found!");
            return null;
        }

        private static void EnsureSceneLoadHookRegistered()
        {
            if (sceneLoadHookRegistered)
                return;

            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneLoadHookRegistered = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            pendingSceneUiTargetRequest = new PendingSceneUiTargetRequest
            {
                IsValid = false,
                RequestId = null,
                SceneName = null,
                TargetPanelName = null,
                ScanAllCanvases = false,
                AllowPartialNameMatch = false,
                AutoHideSiblingPanels = false,
                TriggerNavigatorName = null
            };
            sceneLoadHookRegistered = false;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[UIButtonScreenNavigator] HandleSceneLoaded: scene='{scene.name}', mode={mode}, pendingRequest.IsValid={pendingSceneUiTargetRequest.IsValid}, pendingRequest.SceneName='{pendingSceneUiTargetRequest.SceneName}'");
            
            if (!pendingSceneUiTargetRequest.IsValid)
            {
                Debug.Log($"[UIButtonScreenNavigator] HandleSceneLoaded: No pending request, skipping.");
                return;
            }

            if (!string.Equals(scene.name, pendingSceneUiTargetRequest.SceneName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[UIButtonScreenNavigator] HandleSceneLoaded: Scene name mismatch! Loaded='{scene.name}', Expected='{pendingSceneUiTargetRequest.SceneName}'");
                return;
            }

            Debug.Log($"[UIButtonScreenNavigator] HandleSceneLoaded: Creating DeferredSceneUiResolveRunner for scene '{scene.name}'");
            MultiplayerDetailedLogger.Trace("UI_NAV", $"Scene loaded: {scene.name}, processing request ID: {pendingSceneUiTargetRequest.RequestId}");

            var runnerGo = new GameObject("[UIButtonScreenNavigator] DeferredSceneUiResolve");
            DontDestroyOnLoad(runnerGo);
            var runner = runnerGo.AddComponent<DeferredSceneUiResolveRunner>();
            runner.Initialize(scene, pendingSceneUiTargetRequest);

            // Clear the request after processing
            pendingSceneUiTargetRequest = new PendingSceneUiTargetRequest { IsValid = false };
            Debug.Log($"[UIButtonScreenNavigator] HandleSceneLoaded: Runner created, pending request cleared.");
        }

        private static bool TryApplyPendingSceneUiTarget(Scene scene, PendingSceneUiTargetRequest request)
        {
            Transform target = FindPanelTransform(scene, request.TargetPanelName, request.ScanAllCanvases, request.AllowPartialNameMatch);
            if (target == null)
                return false;

            EnsureParentsActiveStatic(target);
            if (request.AutoHideSiblingPanels)
            {
                HideSiblingPanels(target);
            }

            target.gameObject.SetActive(true);

            var rect = target as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
                rect.anchoredPosition = Vector2.zero;
            }

            target.SetAsLastSibling();

            MultiplayerDetailedLogger.Trace(
                "UI_NAV",
                $"Resolved destination panel: scene={scene.name}, panel={target.name}, trigger={request.TriggerNavigatorName}");

            return true;
        }

        private static Transform FindPanelTransform(Scene scene, string panelName, bool scanAllCanvases, bool allowPartialNameMatch)
        {
            if (string.IsNullOrWhiteSpace(panelName))
            {
                Debug.Log($"[UIButtonScreenNavigator] FindPanelTransform: panelName is empty");
                return null;
            }

            Debug.Log($"[UIButtonScreenNavigator] FindPanelTransform: Searching for '{panelName}' in scene '{scene.name}'");

            var roots = scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                Debug.LogWarning($"[UIButtonScreenNavigator] FindPanelTransform: No root objects in scene '{scene.name}'");
                return null;
            }

            if (scanAllCanvases)
            {
                var canvases = new System.Collections.Generic.List<Canvas>();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] == null)
                        continue;

                    roots[i].GetComponentsInChildren(true, canvases);
                }

                Debug.Log($"[UIButtonScreenNavigator] FindPanelTransform: Found {canvases.Count} canvases");

                Transform foundInCanvas = FindByNameInCanvases(canvases, panelName, true);
                if (foundInCanvas != null)
                {
                    Debug.Log($"[UIButtonScreenNavigator] FindPanelTransform: Found exact match '{foundInCanvas.name}' at path '{GetFullPath(foundInCanvas)}'");
                    return foundInCanvas;
                }

                if (allowPartialNameMatch)
                {
                    foundInCanvas = FindByNameInCanvases(canvases, panelName, false);
                    if (foundInCanvas != null)
                    {
                        Debug.Log($"[UIButtonScreenNavigator] FindPanelTransform: Found partial match '{foundInCanvas.name}' at path '{GetFullPath(foundInCanvas)}'");
                        return foundInCanvas;
                    }
                }
            }

            Transform found = FindByNameInSceneRoots(roots, panelName, true);
            if (found != null)
            {
                Debug.Log($"[UIButtonScreenNavigator] FindPanelTransform: Found in scene roots '{found.name}'");
                return found;
            }

            if (!allowPartialNameMatch)
            {
                Debug.LogWarning($"[UIButtonScreenNavigator] FindPanelTransform: Panel '{panelName}' not found (exact match only)");
                return null;
            }

            return FindByNameInSceneRoots(roots, panelName, false);
        }

        private static string GetFullPath(Transform transform)
        {
            if (transform == null) return "";
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static Transform FindByNameInCanvases(System.Collections.Generic.List<Canvas> canvases, string panelName, bool exact)
        {
            if (canvases == null)
                return null;

            for (int i = 0; i < canvases.Count; i++)
            {
                var canvas = canvases[i];
                if (canvas == null)
                    continue;

                Transform match = FindInSubtreeByName(canvas.transform, panelName, exact);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindByNameInSceneRoots(GameObject[] roots, string panelName, bool exact)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                    continue;

                Transform match = FindInSubtreeByName(root.transform, panelName, exact);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static Transform FindInSubtreeByName(Transform root, string panelName, bool exact)
        {
            if (root == null)
                return null;

            var stack = new System.Collections.Generic.Stack<Transform>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null)
                    continue;

                if (IsNameMatch(current.name, panelName, exact))
                    return current;

                for (int i = current.childCount - 1; i >= 0; i--)
                {
                    var child = current.GetChild(i);
                    if (child != null)
                    {
                        stack.Push(child);
                    }
                }
            }

            return null;
        }

        private static bool IsNameMatch(string candidate, string expected, bool exact)
        {
            if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(expected))
                return false;

            if (exact)
            {
                return string.Equals(candidate.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            return candidate.IndexOf(expected.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void EnsureParentsActiveStatic(Transform node)
        {
            var current = node;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }

                current = current.parent;
            }
        }

        private static void HideSiblingPanels(Transform target)
        {
            if (target == null || target.parent == null)
                return;

            var parent = target.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var sibling = parent.GetChild(i);
                if (sibling == null || sibling == target)
                    continue;

                if (sibling.GetComponent<EventSystem>() != null)
                    continue;

                if (sibling.GetComponent<RectTransform>() == null)
                    continue;

                sibling.gameObject.SetActive(false);
            }
        }

        private sealed class DeferredSceneUiResolveRunner : MonoBehaviour
        {
            private const int MAX_ATTEMPTS = 30;
            private const int WAIT_FRAMES = 3;

            private string targetSceneName;
            private PendingSceneUiTargetRequest request;
            private int waitFrames;
            private int attempts;
            private bool sceneLoaded;

            public void InitializeBeforeSceneLoad(PendingSceneUiTargetRequest pendingRequest)
            {
                targetSceneName = pendingRequest.SceneName;
                request = pendingRequest;
                waitFrames = WAIT_FRAMES;
                attempts = 0;
                sceneLoaded = false;
                
                Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner.InitializeBeforeSceneLoad: targetScene='{targetSceneName}', targetPanel='{pendingRequest.TargetPanelName}', requestId={pendingRequest.RequestId}");
                
                // Subscribe to scene loaded event
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (!string.Equals(scene.name, targetSceneName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner.OnSceneLoaded: Scene '{scene.name}' loaded, but waiting for '{targetSceneName}'");
                    return;
                }

                Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner.OnSceneLoaded: Target scene '{scene.name}' loaded!");
                sceneLoaded = true;
                
                // Unsubscribe
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            public void Initialize(Scene scene, PendingSceneUiTargetRequest pendingRequest)
            {
                targetSceneName = scene.name;
                request = pendingRequest;
                waitFrames = WAIT_FRAMES;
                attempts = 0;
                sceneLoaded = true; // Already loaded
                
                Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner.Initialize: scene='{scene.name}', targetPanel='{pendingRequest.TargetPanelName}', requestId={pendingRequest.RequestId}");
            }

            private void OnDestroy()
            {
                // Cleanup
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            private void Update()
            {
                if (!request.IsValid)
                {
                    Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: Request invalid, destroying.");
                    Destroy(gameObject);
                    return;
                }

                if (!sceneLoaded)
                {
                    // Still waiting for scene to load
                    return;
                }

                if (waitFrames > 0)
                {
                    waitFrames--;
                    Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: Waiting... frames left={waitFrames}");
                    return;
                }

                attempts++;
                Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: Attempt {attempts}/{MAX_ATTEMPTS}, targetPanel='{request.TargetPanelName}'");
                
                // Get the target scene
                Scene targetScene = SceneManager.GetSceneByName(targetSceneName);
                if (!targetScene.IsValid())
                {
                    Debug.LogError($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: Target scene '{targetSceneName}' is not valid!");
                    Destroy(gameObject);
                    return;
                }
                
                // ✅ Cho phép Back navigation không cần tìm panel cụ thể
                if (string.IsNullOrEmpty(request.TargetPanelName))
                {
                    Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: Empty panel name, trying to activate first Canvas...");
                    if (TryActivateFirstCanvas(targetScene))
                    {
                        Debug.Log($"[{nameof(UIButtonScreenNavigator)}] Back navigation to scene '{targetScene.name}' completed - activated first Canvas.");
                    }
                    else
                    {
                        Debug.Log($"[{nameof(UIButtonScreenNavigator)}] Back navigation to scene '{targetScene.name}' completed - no Canvas found to activate.");
                    }
                    request = default;
                    Destroy(gameObject);
                    return;
                }
                
                Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: Calling TryApplyPendingSceneUiTarget...");
                if (TryApplyPendingSceneUiTarget(targetScene, request))
                {
                    Debug.Log($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: SUCCESS! Panel found and activated.");
                    request = default;
                    Destroy(gameObject);
                    return;
                }

                Debug.LogWarning($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: Panel '{request.TargetPanelName}' not found yet. Attempt {attempts}/{MAX_ATTEMPTS}");

                if (attempts >= MAX_ATTEMPTS)
                {
                    Debug.LogError($"[UIButtonScreenNavigator] DeferredSceneUiResolveRunner: MAX_ATTEMPTS reached! Trying fallback...");
                    // ✅ Fallback: Activate first Canvas if no specific panel found
                    if (TryActivateFirstCanvas(targetScene))
                    {
                        Debug.LogWarning($"[{nameof(UIButtonScreenNavigator)}] Fallback: Activated first Canvas in scene '{targetScene.name}' instead of panel '{request.TargetPanelName}'");
                    }
                    else
                    {
                        Debug.LogError($"[{nameof(UIButtonScreenNavigator)}] FAILED: Không tìm thấy panel '{request.TargetPanelName}' và không có Canvas nào trong scene '{targetScene.name}'");
                    }
                    
                    request = default;
                    Destroy(gameObject);
                }
            }

            private bool TryActivateFirstCanvas(Scene scene)
            {
                if (!scene.IsValid())
                {
                    Debug.LogError($"[UIButtonScreenNavigator] TryActivateFirstCanvas: Scene is not valid!");
                    return false;
                }

                var roots = scene.GetRootGameObjects();
                Debug.Log($"[UIButtonScreenNavigator] TryActivateFirstCanvas: Found {roots.Length} root objects in scene '{scene.name}'");
                
                foreach (var root in roots)
                {
                    if (root == null) continue;
                    
                    var canvas = root.GetComponentInChildren<Canvas>(true);
                    if (canvas != null)
                    {
                        Debug.Log($"[UIButtonScreenNavigator] TryActivateFirstCanvas: Found Canvas '{canvas.name}' in root '{root.name}'");
                        
                        // Ensure parents are active
                        EnsureParentsActiveStatic(canvas.transform);
                        canvas.gameObject.SetActive(true);
                        
                        // Auto-hide sibling panels if requested
                        if (request.AutoHideSiblingPanels)
                        {
                            HideSiblingPanels(canvas.transform);
                        }
                        
                        return true;
                    }
                }
                
                Debug.LogWarning($"[UIButtonScreenNavigator] TryActivateFirstCanvas: No Canvas found in scene '{scene.name}'");
                return false;
            }
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

#if UNITY_EDITOR
namespace DoAnGame.UI
{
    [UnityEditor.CustomEditor(typeof(UIButtonScreenNavigator))]
    public class UIButtonScreenNavigatorEditor : UnityEditor.Editor
    {
        private UnityEditor.SerializedProperty destinationTargetPanelNameProp;
        private UnityEditor.SerializedProperty targetSceneNameProp;
        private UnityEditor.SerializedProperty sceneAssetProp;
        private readonly System.Collections.Generic.List<UiEntry> cachedEntries = new System.Collections.Generic.List<UiEntry>();
        private bool isCacheInitialized;
        // Đảm bảo khai báo biến scroll
        private UnityEngine.Vector2 panelScrollPos;
        private bool showOnlyTopLevelPanels = true; // ✅ Filter option

        private struct UiEntry
        {
            public string name;
            public string path;
            public string sceneName;
        }

        private void OnEnable()
        {
            destinationTargetPanelNameProp = serializedObject.FindProperty("destinationTargetPanelName");
            targetSceneNameProp = serializedObject.FindProperty("targetSceneName");
            sceneAssetProp = serializedObject.FindProperty("sceneAsset");
            RefreshCache();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Vẽ phần chọn Scene trước
            UnityEditor.EditorGUILayout.PropertyField(sceneAssetProp, new UnityEngine.GUIContent("Scene Đích (Scene Asset)"));
            UnityEditor.EditorGUILayout.PropertyField(targetSceneNameProp, new UnityEngine.GUIContent("Tên Scene (nếu cần)"));

            // Ngay bên dưới là dropdown chọn panel UI đích
            UnityEditor.EditorGUILayout.Space(8f);
            UnityEditor.EditorGUILayout.BeginVertical("box");
            UnityEditor.EditorGUILayout.LabelField("Chọn Panel UI đích từ Scene đã chọn", UnityEditor.EditorStyles.boldLabel);

            // ✅ Thêm button refresh cache và filter toggle
            UnityEditor.EditorGUILayout.BeginHorizontal();
            showOnlyTopLevelPanels = UnityEditor.EditorGUILayout.Toggle("Chỉ Top-Level Panels", showOnlyTopLevelPanels);
            if (UnityEngine.GUILayout.Button("🔄 Refresh", UnityEngine.GUILayout.Width(80)))
            {
                RefreshCache();
            }
            UnityEditor.EditorGUILayout.EndHorizontal();

            // ✅ Thêm thông báo cho Back navigation
            if (!string.IsNullOrEmpty(targetSceneNameProp.stringValue) && 
                string.IsNullOrEmpty(destinationTargetPanelNameProp.stringValue))
            {
                UnityEditor.EditorGUILayout.HelpBox("Back Navigation: Không cần chọn panel cụ thể. Component sẽ tự động activate Canvas đầu tiên trong scene đích.", UnityEditor.MessageType.Info);
            }

            if (!isCacheInitialized)
            {
                RefreshCache();
            }

            var entries = cachedEntries;
            
            // ✅ Apply filter nếu cần
            if (showOnlyTopLevelPanels && entries.Count > 0)
            {
                var filteredEntries = new System.Collections.Generic.List<UiEntry>();
                foreach (var entry in entries)
                {
                    // Chỉ lấy entries có path không chứa nhiều "/" (top-level)
                    int slashCount = 0;
                    foreach (char c in entry.path)
                    {
                        if (c == '/') slashCount++;
                    }
                    // Top-level thường có <= 2 slashes (e.g., "GameUICanvas/MainMenuPanel")
                    if (slashCount <= 2)
                    {
                        filteredEntries.Add(entry);
                    }
                }
                entries = filteredEntries;
            }
            
            if (entries.Count == 0)
            {
                UnityEditor.EditorGUILayout.HelpBox("Không tìm thấy panel nào từ scene đã chọn.", UnityEditor.MessageType.None);
            }
            else
            {
                // ✅ Hiển thị số lượng panels tìm thấy
                UnityEditor.EditorGUILayout.LabelField($"Tìm thấy {entries.Count} panel(s)", UnityEditor.EditorStyles.miniLabel);
                
                var options = BuildOptions(entries);
                int currentIndex = GetCurrentIndex(entries, destinationTargetPanelNameProp != null ? destinationTargetPanelNameProp.stringValue : null);

                panelScrollPos = UnityEditor.EditorGUILayout.BeginScrollView(panelScrollPos, UnityEngine.GUILayout.Height(120));
                UnityEditor.EditorGUI.BeginChangeCheck();
                int selected = UnityEditor.EditorGUILayout.Popup("Panel Đích", currentIndex, options);
                if (UnityEditor.EditorGUI.EndChangeCheck() && selected >= 0 && selected < entries.Count)
                {
                    if (destinationTargetPanelNameProp != null)
                    {
                        destinationTargetPanelNameProp.stringValue = entries[selected].name;
                        Debug.Log($"[UIButtonScreenNavigatorEditor] Chọn panel đích: {entries[selected].name} ({entries[selected].path})");
                    }
                }
                UnityEditor.EditorGUILayout.EndScrollView();

                if (selected >= 0 && selected < entries.Count)
                {
                    UnityEditor.EditorGUILayout.LabelField("Path", entries[selected].path);
                    UnityEditor.EditorGUILayout.LabelField("Scene", entries[selected].sceneName);
                }
            }

            UnityEditor.EditorGUILayout.EndVertical();

            // Vẽ các trường còn lại như mặc định
            UnityEditor.EditorGUILayout.Space(8f);
            DrawPropertiesExcluding(serializedObject, "sceneAsset", "targetSceneName", "destinationTargetPanelName");

            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshCache()
        {
            cachedEntries.Clear();
            CollectUiEntriesFromTargetSceneAsset(cachedEntries);
            isCacheInitialized = true;
        }

        private void CollectUiEntriesFromTargetSceneAsset(System.Collections.Generic.List<UiEntry> result)
        {
            string scenePath = ResolveTargetSceneAssetPath();
            if (string.IsNullOrWhiteSpace(scenePath))
                return;

            var targetScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                CollectSceneUiEntries(targetScene, result);
                return;
            }

            var openedScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                CollectSceneUiEntries(openedScene, result);
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(openedScene, true);
            }
        }

        private string ResolveTargetSceneAssetPath()
        {
            if (sceneAssetProp != null && sceneAssetProp.objectReferenceValue != null)
            {
                string fromAsset = UnityEditor.AssetDatabase.GetAssetPath(sceneAssetProp.objectReferenceValue);
                if (!string.IsNullOrWhiteSpace(fromAsset))
                    return fromAsset;
            }

            string targetSceneName = targetSceneNameProp != null ? targetSceneNameProp.stringValue : null;
            if (string.IsNullOrWhiteSpace(targetSceneName))
                return null;

            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:Scene {targetSceneName}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(fileName, targetSceneName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return null;
        }

        private static void CollectSceneUiEntries(UnityEngine.SceneManagement.Scene scene, System.Collections.Generic.List<UiEntry> result)
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                    continue;

                CollectUiEntriesRecursive(root.transform, scene.name, root.name, result);
            }
        }

        private static void CollectUiEntriesRecursive(UnityEngine.Transform node, string sceneName, string path, System.Collections.Generic.List<UiEntry> result)
        {
            if (node == null)
                return;

            // ✅ Thu thập TẤT CẢ GameObject có RectTransform (UI elements)
            var rectTransform = node.GetComponent<UnityEngine.RectTransform>();
            if (rectTransform != null)
            {
                // Chỉ bỏ qua các UI leaf elements (Button, Text, Image, etc.)
                if (!IsUiLeaf(node))
                {
                    result.Add(new UiEntry
                    {
                        name = node.name,
                        path = path,
                        sceneName = sceneName
                    });
                }
            }

            // Duyệt toàn bộ cây con
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                if (child == null)
                    continue;

                CollectUiEntriesRecursive(child, sceneName, path + "/" + child.name, result);
            }
        }

        private static readonly System.Type[] UiLeafTypes = new System.Type[] {
            typeof(UnityEngine.UI.Button),
            typeof(UnityEngine.UI.Text),
            typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Toggle),
            typeof(UnityEngine.UI.InputField),
            typeof(UnityEngine.UI.Dropdown),
            typeof(UnityEngine.UI.Slider),
            typeof(UnityEngine.UI.Scrollbar),
            typeof(UnityEngine.UI.RawImage),
            // ✅ Không loại bỏ Mask vì nó có thể là panel container
            // typeof(UnityEngine.UI.Mask),
            // ✅ Không loại bỏ Selectable vì Button kế thừa từ nó (đã có Button ở trên)
            // typeof(UnityEngine.UI.Selectable),
            // TextMeshPro
            GetTypeByName("TMPro.TextMeshProUGUI, Unity.TextMeshPro"),
            GetTypeByName("TMPro.TMP_InputField, Unity.TextMeshPro")
        };

        private static System.Type GetTypeByName(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null) return type;
            }
            return null;
        }

        private static bool IsUiLeaf(Transform node)
        {
            if (node == null) return false;
            var go = node.gameObject;
            
            // ✅ Chỉ kiểm tra component trên chính GameObject này, không kiểm tra children
            foreach (var t in UiLeafTypes)
            {
                if (t != null && go.GetComponent(t) != null)
                {
                    // ✅ Nếu có children, vẫn coi là panel container
                    // Chỉ coi là leaf nếu không có children hoặc chỉ có 1 child (thường là Text/Icon)
                    if (node.childCount <= 1)
                        return true;
                }
            }
            return false;
        }

        private static string[] BuildOptions(System.Collections.Generic.List<UiEntry> entries)
        {
            var options = new string[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                options[i] = entries[i].name + "  [" + entries[i].sceneName + "]";
            }

            return options;
        }

        private static int GetCurrentIndex(System.Collections.Generic.List<UiEntry> entries, string currentName)
        {
            if (string.IsNullOrWhiteSpace(currentName))
                return 0;

            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].name, currentName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }
    }
}
#endif
