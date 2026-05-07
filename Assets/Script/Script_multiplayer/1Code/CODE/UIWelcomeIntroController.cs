using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 1: Welcome Screen (INTRO)
    /// ✅ UPDATED: Xóa nút "ChoiTiep" (không còn dùng)
    /// Session checking đã được chuyển sang UIStartupController
    /// </summary>
    public class UIWelcomeIntroController : FlowPanelController
    {
        [SerializeField] private UIFlowManager flowManager;

        [Header("Bridge sang scene GameUIPlay (Chuyển scene)")]
        [SerializeField] private string gameplaySceneName = "GameUIPlay";
        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

        private AuthManager authManager;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = EnsureAuthManagerReady();
        }

        private void OnEnable()
        {
            authManager = EnsureAuthManagerReady();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override bool TryHandleNavigationOverride(FlowButtonConfig config)
        {
            if (config.Action == FlowButtonAction.ShowScreen)
            {
                if (flowManager != null)
                    return false;

                LoadGameScene(config.TargetScreen);
                return true;
            }

            if (config.Action == FlowButtonAction.ShowSettings)
            {
                if (flowManager != null)
                    return false;

                LoadGameScene(UIFlowManager.Screen.Settings);
                return true;
            }

            return false;
        }

        private void LoadGameScene(UIFlowManager.Screen targetScreen)
        {
            if (string.IsNullOrEmpty(gameplaySceneName))
            {
                Debug.LogError("[UIWelcomeIntro] gameplaySceneName chưa được cấu hình");
                return;
            }

            SceneFlowBridge.RequestScreen(targetScreen);
            SceneManager.LoadScene(gameplaySceneName, loadMode);
        }

        private Transform FindMainMenuTransform()
        {
            Transform canvas = GameObject.Find("GameUICanvas")?.transform;
            if (canvas == null)
                return null;

            Transform byName = canvas.Find("MainMenuPanel");
            if (byName != null)
                return byName;

            var byController = canvas.GetComponentInChildren<UIMainMenuController>(true);
            return byController != null ? byController.transform : null;
        }

        private AuthManager EnsureAuthManagerReady()
        {
            if (authManager != null)
                return authManager;

            authManager = AuthManager.Instance;
            if (authManager != null)
                return authManager;

            authManager = FindObjectOfType<AuthManager>(true);
            if (authManager != null)
                return authManager;

            var authManagerObj = new GameObject("AuthManager");
            authManager = authManagerObj.AddComponent<AuthManager>();
            return authManager;
        }
    }
}
