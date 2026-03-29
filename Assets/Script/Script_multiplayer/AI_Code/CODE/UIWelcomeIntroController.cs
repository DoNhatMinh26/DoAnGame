using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 1: Welcome Screen (INTRO)
    /// </summary>
    public class UIWelcomeIntroController : FlowPanelController
    {
        [SerializeField] private UIFlowManager flowManager;

        [Header("Bridge sang scene GameUIPlay (Chuyển scene)")]
        [SerializeField] private string gameplaySceneName = "GameUIPlay";
        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

        protected override UIFlowManager FlowManager => flowManager;

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
    }
}
