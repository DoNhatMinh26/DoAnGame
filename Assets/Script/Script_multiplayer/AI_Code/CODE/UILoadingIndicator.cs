using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI Loading indicator: Spinner + Loading text
    /// Dùng chung cho tất cả panels (Login, Register) >> tiết kiệm memory
    /// </summary>
    public class UILoadingIndicator : MonoBehaviour
    {
        public static UILoadingIndicator Instance { get; private set; }

        [SerializeField] private GameObject loadingRoot;          // Root object chứa spinner + text
        [SerializeField] private Image spinnerImage;              // Hình spinner (quay tròn)
        [SerializeField] private TMP_Text loadingText;           // Text "Đang xử lý..."
        [SerializeField] private float spinSpeedDegrees = 360f;   // Tốc độ quay
        [SerializeField] private float autoHideDelay = 0f;        // Nếu > 0, auto-hide sau X giây

        private Coroutine spinningCoroutine;
        private float hideCountdown;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Tìm các components
            if (loadingRoot == null)
                loadingRoot = FindInChildren("LoadingRoot");
            if (spinnerImage == null)
                spinnerImage = FindInChildren<Image>("Spinner");
            if (loadingText == null)
                loadingText = FindInChildren<TMP_Text>("LoadingText");
        }

        private void Start()
        {
            // Ẩn loading indicator lúc startup
            Hide();
        }

        private void Update()
        {
            // Auto-hide sau delay (nếu cấu hình)
            if (autoHideDelay > 0 && loadingRoot.activeSelf)
            {
                hideCountdown -= Time.deltaTime;
                if (hideCountdown <= 0)
                {
                    Hide();
                }
            }
        }

        /// <summary>
        /// Hiển thị loading indicator + bắt đầu spin
        /// </summary>
        public void Show(string message = "Đang xử lý...")
        {
            if (loadingRoot != null)
                loadingRoot.SetActive(true);

            if (loadingText != null)
                loadingText.text = message;

            // Start spinning
            if (spinningCoroutine != null)
                StopCoroutine(spinningCoroutine);
            spinningCoroutine = StartCoroutine(SpinSpinner());

            // Reset auto-hide countdown
            if (autoHideDelay > 0)
                hideCountdown = autoHideDelay;

            Debug.Log($"[Loading] 🔄 {message}");
        }

        /// <summary>
        /// Ẩn loading indicator + dừng spin
        /// </summary>
        public void Hide()
        {
            if (loadingRoot != null)
                loadingRoot.SetActive(false);

            if (spinningCoroutine != null)
            {
                StopCoroutine(spinningCoroutine);
                spinningCoroutine = null;
            }

            Debug.Log("[Loading] ✅ Hidden");
        }

        /// <summary>
        /// Coroutine quay spinner
        /// </summary>
        private IEnumerator SpinSpinner()
        {
            while (true)
            {
                if (spinnerImage != null)
                {
                    spinnerImage.transform.Rotate(0, 0, -spinSpeedDegrees * Time.deltaTime);
                }
                yield return null;
            }
        }

        /// <summary>
        /// Helper: Tìm GameObject con theo tên
        /// </summary>
        private GameObject FindInChildren(string name)
        {
            Transform found = transform.Find(name);
            return found != null ? found.gameObject : null;
        }

        /// <summary>
        /// Helper: Tìm component con theo type
        /// </summary>
        private T FindInChildren<T>(string name) where T : Component
        {
            Transform found = transform.Find(name);
            if (found != null)
                return found.GetComponent<T>();
            return null;
        }

        /// <summary>
        /// Helper: Tìm component con (không cần tên)
        /// </summary>
        private T FindInChildren<T>() where T : Component
        {
            return GetComponentInChildren<T>();
        }
    }
}
