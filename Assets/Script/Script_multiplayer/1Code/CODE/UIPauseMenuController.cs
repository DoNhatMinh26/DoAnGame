using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 10: Pause Menu
    /// </summary>
    public class UIPauseMenuController : BasePanelController
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private UIFlowManager flowManager;

        public UnityEvent OnResumeRequested = new UnityEvent();
        public UnityEvent OnQuitRequested = new UnityEvent();

        protected override void Awake()
        {
            base.Awake();
            resumeButton?.onClick.AddListener(HandleResume);
            settingsButton?.onClick.AddListener(() => flowManager.ShowSettings(UIFlowManager.Screen.PauseMenu));
            quitButton?.onClick.AddListener(HandleQuit);
        }

        private void OnDestroy()
        {
            resumeButton?.onClick.RemoveAllListeners();
            settingsButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.RemoveAllListeners();
        }

        private void HandleResume()
        {
            OnResumeRequested?.Invoke();
            flowManager.ShowScreen(UIFlowManager.Screen.InGame);
        }

        private void HandleQuit()
        {
            OnQuitRequested?.Invoke();
            flowManager.ShowScreen(UIFlowManager.Screen.MainMenu);
        }
    }
}
