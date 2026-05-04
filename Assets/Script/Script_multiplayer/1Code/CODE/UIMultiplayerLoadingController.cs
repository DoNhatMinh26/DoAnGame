using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using DoAnGame.Multiplayer;

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller cho Multiplayer Loading Panel
    /// Hiển thị progress bar và status khi đang load trận đấu
    /// 
    /// MODES:
    /// - Battle: Load trận đấu (check network, player states) → Navigate to GameplayPanel
    /// - Simple: Hiển thị loading đơn giản với custom message → Không navigate
    /// </summary>
    public class UIMultiplayerLoadingController : BasePanelController
    {
        [Header("UI References")]
        [SerializeField] private Image progressBarFill;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text statusText;

        [Header("Settings")]
        [SerializeField] private float minLoadingTime = 2f; // Tối thiểu 2s

        // ❌ XÓA: Không cần gameplayNavigator nữa
        // [Header("Navigation")]
        // [SerializeField] private UIButtonScreenNavigator gameplayNavigator;

        private float currentProgress = 0f;
        private float targetProgress = 0f;
        private bool isLoading = false;

        // ✅ THÊM: Mode và custom message
        public enum LoadingMode
        {
            Battle,  // Load trận đấu (check network, player states)
            Simple   // Hiển thị loading đơn giản với custom message
        }

        private LoadingMode currentMode = LoadingMode.Battle;
        private string customMessage = "";
        private float simpleDuration = 1f;

        /// <summary>
        /// Show LoadingPanel với mode Battle (default)
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();
            
            // ✅ Chỉ start Battle loading nếu mode là Battle
            // Simple mode sẽ tự start từ ShowSimpleLoading()
            if (currentMode == LoadingMode.Battle)
            {
                ShowBattleLoading();
            }
        }

        /// <summary>
        /// Show LoadingPanel với mode Simple và custom message
        /// </summary>
        public void ShowSimpleLoading(string message, float duration = 1f)
        {
            customMessage = message;
            simpleDuration = duration;
            currentMode = LoadingMode.Simple;
            Show();
            StartSimpleLoading();
        }

        /// <summary>
        /// Show LoadingPanel với mode Battle (load trận đấu)
        /// </summary>
        public void ShowBattleLoading()
        {
            currentMode = LoadingMode.Battle;
            StartLoading();
        }

        protected override void OnHide()
        {
            base.OnHide();
            StopLoading();
        }

        private void StartLoading()
        {
            if (isLoading) return;

            isLoading = true;
            currentProgress = 0f;
            targetProgress = 0f;
            UpdateProgressUI(0f, "Đang kết nối...");
            StartCoroutine(LoadingRoutine());
        }

        private void StopLoading()
        {
            isLoading = false;
            StopAllCoroutines();
        }

        /// <summary>
        /// Simple loading routine - chỉ hiển thị progress bar với custom message
        /// </summary>
        private void StartSimpleLoading()
        {
            if (isLoading) return;

            isLoading = true;
            currentProgress = 0f;
            targetProgress = 1f;
            UpdateProgressUI(0f, customMessage);
            StartCoroutine(SimpleLoadingRoutine());
        }

        /// <summary>
        /// Simple loading coroutine - không check network/player states
        /// </summary>
        private IEnumerator SimpleLoadingRoutine()
        {
            Debug.Log($"[LoadingPanel] 🔄 SimpleLoadingRoutine START: {customMessage}");

            // Animate progress từ 0% → 100%
            float elapsed = 0f;
            while (elapsed < simpleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / simpleDuration);
                UpdateProgressUI(progress, customMessage);
                yield return null;
            }

            // Hoàn tất
            UpdateProgressUI(1f, customMessage);
            Debug.Log("[LoadingPanel] ✅ Simple loading complete!");

            // ✅ KHÔNG ẨN panel ở đây - để caller tự ẩn khi cần
            // Điều này tránh khoảng đen khi chuyển panel
        }

        private IEnumerator LoadingRoutine()
        {
            float startTime = Time.time;
            Debug.Log("[LoadingPanel] 🔄 LoadingRoutine START");

            // Phase 1: Kết nối (0-30%)
            SetTargetProgress(0.3f, "Đang kết nối...");
            Debug.Log("[LoadingPanel] Phase 1: Kết nối...");
            yield return new WaitForSeconds(0.5f);

            // Phase 2: Đồng bộ dữ liệu (30-70%)
            SetTargetProgress(0.7f, "Đang đồng bộ dữ liệu...");
            Debug.Log("[LoadingPanel] Phase 2: Đồng bộ dữ liệu...");
            Debug.Log($"[LoadingPanel] IsNetworkReady: {IsNetworkReady()}");
            yield return new WaitUntil(() => IsNetworkReady());
            Debug.Log("[LoadingPanel] ✅ Network ready!");

            // Phase 3: Chuẩn bị trận đấu (70-100%)
            SetTargetProgress(1f, "Đang chuẩn bị trận đấu...");
            Debug.Log("[LoadingPanel] Phase 3: Chuẩn bị trận đấu...");
            Debug.Log($"[LoadingPanel] ArePlayerStatesReady: {ArePlayerStatesReady()}");
            yield return new WaitUntil(() => ArePlayerStatesReady());
            Debug.Log("[LoadingPanel] ✅ Player states ready!");

            // Đảm bảo loading tối thiểu minLoadingTime
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < minLoadingTime)
            {
                float waitTime = minLoadingTime - elapsedTime;
                Debug.Log($"[LoadingPanel] Waiting {waitTime:F2}s to reach minLoadingTime...");
                yield return new WaitForSeconds(waitTime);
            }

            // Hoàn tất
            UpdateProgressUI(1f, "Hoàn tất!");
            Debug.Log("[LoadingPanel] ✅ Loading complete!");
            yield return new WaitForSeconds(0.3f);

            // Navigate to GameplayPanel
            NavigateToGameplay();
        }

        private void SetTargetProgress(float target, string status)
        {
            targetProgress = Mathf.Clamp01(target);
            if (statusText != null)
            {
                statusText.SetText(status);
            }
        }

        private void Update()
        {
            if (!isLoading) return;

            // Smooth progress animation
            if (currentProgress < targetProgress)
            {
                currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 0.8f);
                UpdateProgressUI(currentProgress, null);
            }
        }

        private void UpdateProgressUI(float progress, string status)
        {
            progress = Mathf.Clamp01(progress);

            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = progress;
            }

            if (progressText != null)
            {
                progressText.SetText($"{Mathf.RoundToInt(progress * 100)}%");
            }

            if (status != null && statusText != null)
            {
                statusText.SetText(status);
            }
        }

        private bool IsNetworkReady()
        {
            var nm = NetworkManager.Singleton;
            bool isReady = nm != null && nm.IsListening && (nm.IsServer || nm.IsClient);
            
            if (!isReady)
            {
                Debug.Log($"[LoadingPanel] Network NOT ready: nm={nm != null}, IsListening={nm?.IsListening}, IsServer={nm?.IsServer}, IsClient={nm?.IsClient}");
            }
            
            return isReady;
        }

        private bool ArePlayerStatesReady()
        {
            var battleManager = NetworkedMathBattleManager.Instance;
            if (battleManager == null)
            {
                Debug.Log("[LoadingPanel] BattleManager is NULL");
                return false;
            }

            var player1 = battleManager.GetPlayer1State();
            var player2 = battleManager.GetPlayer2State();

            bool isReady = player1 != null && player2 != null && 
                           player1.IsSpawned && player2.IsSpawned;

            if (!isReady)
            {
                Debug.Log($"[LoadingPanel] Player states NOT ready: P1={player1 != null}, P2={player2 != null}, P1Spawned={player1?.IsSpawned}, P2Spawned={player2?.IsSpawned}");
            }

            return isReady;
        }

        private void NavigateToGameplay()
        {
            Debug.Log("[LoadingPanel] ✅ Loading complete, navigating to GameplayPanel");

            // Ẩn LoadingPanel
            Hide();

            // Tìm và hiển thị GameplayPanel
            var gameplayPanel = FindObjectOfType<UIMultiplayerBattleController>(true); // true = include inactive
            if (gameplayPanel != null)
            {
                Debug.Log("[LoadingPanel] ✅ Found GameplayPanel, showing it...");
                gameplayPanel.Show();
            }
            else
            {
                Debug.LogError("[LoadingPanel] ❌ GameplayPanel not found! Cannot proceed.");
            }
        }
    }
}
