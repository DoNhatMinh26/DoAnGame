using System.Collections;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using DoAnGame.Auth;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Bảo vệ phiên đăng nhập — chỉ cho phép 1 thiết bị đăng nhập cùng lúc.
    ///
    /// Cơ chế:
    ///   1. Khi đăng nhập thành công → ghi sessionId của máy này vào Firestore users/{uid}.activeSessionId
    ///   2. Polling mỗi 30s → đọc lại activeSessionId từ Firestore
    ///   3. Nếu activeSessionId khác với sessionId của máy này → bị kick → tự đăng xuất
    ///
    /// Firestore field: users/{uid}.activeSessionId (string)
    /// Setup: Thêm component này vào AuthServices GameObject.
    /// </summary>
    public class SessionGuardService : MonoBehaviour
    {
        public static SessionGuardService Instance { get; private set; }

        /// <summary>Callback khi bị kick (máy khác đăng nhập cùng tài khoản).</summary>
        public System.Action OnKickedByOtherDevice;

        private const string FIELD_SESSION_ID     = "activeSessionId";
        private const float  POLL_INTERVAL_SECONDS = 30f;

        private FirebaseFirestore firestore;
        private string  currentUid;
        private string  mySessionId;
        private Coroutine pollingRoutine;
        private bool    isGuarding;
        private bool    kickHandled; // guard tránh HandleKicked gọi 2 lần

        // ─────────────────────────────────────────────────────────────
        // UNITY LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            // Không gọi DontDestroyOnLoad — gắn chung AuthServices đã DDOL rồi
        }

        private void Start()
        {
            try
            {
                firestore = FirebaseFirestore.DefaultInstance;
                RuntimeInstanceContext.ConfigureFirestoreSettings(firestore, "SessionGuard");
            }
            catch
            {
                firestore = null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Gọi sau khi đăng nhập thành công.
        /// Ghi sessionId của máy này lên Firestore và bắt đầu polling.
        /// </summary>
        public async Task StartGuarding(string uid)
        {
            if (string.IsNullOrEmpty(uid) || firestore == null) return;

            StopGuarding(); // Dừng polling cũ nếu có

            currentUid  = uid;
            mySessionId = RuntimeInstanceContext.InstanceId; // ID duy nhất của máy này
            kickHandled = false;

            // Ghi sessionId lên Firestore — máy cũ sẽ phát hiện bị kick
            await WriteSessionIdToFirestore(uid, mySessionId);

            // Bắt đầu polling
            isGuarding = true;
            if (isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                pollingRoutine = StartCoroutine(PollSessionRoutine());
            }

            // Safe substring để tránh crash nếu uid/sessionId ngắn hơn dự kiến
            string uidPreview = uid.Length > 8 ? uid[..8] : uid;
            string sidPreview = mySessionId.Length > 12 ? mySessionId[..12] : mySessionId;
            Debug.Log($"[SessionGuard] ✅ Bắt đầu bảo vệ phiên: uid={uidPreview}... sessionId={sidPreview}...");
        }

        /// <summary>
        /// Gọi khi đăng xuất — dừng polling.
        /// </summary>
        public void StopGuarding()
        {
            isGuarding  = false;
            kickHandled = false;

            if (pollingRoutine != null)
            {
                StopCoroutine(pollingRoutine);
                pollingRoutine = null;
            }

            currentUid  = null;
            mySessionId = null;

            Debug.Log("[SessionGuard] ⏹️ Dừng bảo vệ phiên.");
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE — Firestore
        // ─────────────────────────────────────────────────────────────

        private async Task WriteSessionIdToFirestore(string uid, string sessionId)
        {
            try
            {
                var update = new System.Collections.Generic.Dictionary<string, object>
                {
                    { FIELD_SESSION_ID, sessionId }
                };
                await firestore.Collection("users").Document(uid)
                    .SetAsync(update, SetOptions.MergeAll);

                string preview = sessionId.Length > 12 ? sessionId[..12] : sessionId;
                Debug.Log($"[SessionGuard] 📝 Ghi sessionId lên Firestore: {preview}...");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SessionGuard] ⚠️ Không ghi được sessionId: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE — Polling
        // ─────────────────────────────────────────────────────────────

        private IEnumerator PollSessionRoutine()
        {
            while (isGuarding)
            {
                yield return new WaitForSeconds(POLL_INTERVAL_SECONDS);

                if (!isGuarding) yield break;

                _ = CheckSessionAsync();
            }
        }

        private async Task CheckSessionAsync()
        {
            if (!isGuarding || string.IsNullOrEmpty(currentUid) || firestore == null) return;

            try
            {
                var snap = await firestore.Collection("users").Document(currentUid).GetSnapshotAsync();
                if (!snap.Exists) return;

                var d = snap.ToDictionary();
                string firestoreSessionId = d.ContainsKey(FIELD_SESSION_ID)
                    ? d[FIELD_SESSION_ID]?.ToString()
                    : null;

                if (string.IsNullOrEmpty(firestoreSessionId)) return;

                // Nếu sessionId trên Firestore khác với của máy này → bị kick
                if (firestoreSessionId != mySessionId)
                {
                    Debug.LogWarning("[SessionGuard] ⚠️ Phát hiện đăng nhập từ thiết bị khác! Đang đăng xuất...");
                    HandleKicked();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SessionGuard] ⚠️ Lỗi kiểm tra session: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE — Kick handling
        // ─────────────────────────────────────────────────────────────

        private void HandleKicked()
        {
            // Guard tránh gọi 2 lần (async polling có thể overlap)
            if (kickHandled) return;
            kickHandled = true;

            StopGuarding();

            // Invoke callback để UI xử lý nếu cần
            OnKickedByOtherDevice?.Invoke();

            // Đăng xuất
            var authManager = AuthManager.Instance;
            if (authManager != null)
            {
                authManager.Logout();
            }

            // Hiện thông báo toàn cục — hoạt động ở mọi scene/panel
            if (isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                StartCoroutine(ShowKickedNotificationAndReload());
            }
            else
            {
                // Fallback: reload thẳng nếu không thể chạy coroutine
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        private IEnumerator ShowKickedNotificationAndReload()
        {
            // Tạo overlay thông báo đơn giản trên toàn màn hình
            var overlay = CreateKickedOverlay();

            // Đợi 3 giây để user đọc thông báo
            yield return new WaitForSecondsRealtime(3f);

            // Xóa overlay
            if (overlay != null)
                Destroy(overlay);

            // Reload scene về màn đăng nhập
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE — Overlay UI
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Tạo overlay thông báo đơn giản — không phụ thuộc prefab hay panel nào.
        /// Hiển thị trên toàn màn hình với sortingOrder = 9999.
        /// </summary>
        private UnityEngine.GameObject CreateKickedOverlay()
        {
            try
            {
                // Canvas overlay
                var go     = new UnityEngine.GameObject("[KickedOverlay]");
                var canvas = go.AddComponent<UnityEngine.Canvas>();
                canvas.renderMode   = UnityEngine.RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999; // Luôn trên cùng
                go.AddComponent<UnityEngine.UI.CanvasScaler>();
                go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                // Background tối
                var bgGo  = new UnityEngine.GameObject("Background");
                bgGo.transform.SetParent(go.transform, false);
                var bgImg = bgGo.AddComponent<UnityEngine.UI.Image>();
                bgImg.color = new UnityEngine.Color(0f, 0f, 0f, 0.85f);
                var bgRect  = bgGo.GetComponent<UnityEngine.RectTransform>();
                bgRect.anchorMin = UnityEngine.Vector2.zero;
                bgRect.anchorMax = UnityEngine.Vector2.one;
                bgRect.offsetMin = bgRect.offsetMax = UnityEngine.Vector2.zero;

                // Text thông báo
                var textGo = new UnityEngine.GameObject("Message");
                textGo.transform.SetParent(go.transform, false);
                var text   = textGo.AddComponent<TMPro.TextMeshProUGUI>();
                text.text      = "⚠️ Tài khoản của bạn\nvừa được đăng nhập\ntừ thiết bị khác.\n\nĐang đăng xuất...";
                text.fontSize  = 36;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.color     = UnityEngine.Color.white;
                var textRect   = textGo.GetComponent<UnityEngine.RectTransform>();
                textRect.anchorMin = UnityEngine.Vector2.zero;
                textRect.anchorMax = UnityEngine.Vector2.one;
                textRect.offsetMin = textRect.offsetMax = UnityEngine.Vector2.zero;

                return go;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SessionGuard] Không tạo được overlay: {ex.Message}");
                return null;
            }
        }
    }
}
