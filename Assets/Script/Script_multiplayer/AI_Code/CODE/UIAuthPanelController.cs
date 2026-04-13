using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 2: Welcome/Auth Panel (Đăng nhập, đăng ký, chơi nhanh).
    /// </summary>
    public class UIAuthPanelController : BasePanelController
    {
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button quickPlayButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private UIFlowManager flowManager;

        private AuthManager authManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;

            loginButton?.onClick.AddListener(() => flowManager.ShowScreen(UIFlowManager.Screen.Login));
            registerButton?.onClick.AddListener(() => flowManager.ShowScreen(UIFlowManager.Screen.Register));
            quickPlayButton?.onClick.AddListener(() => _ = HandleQuickPlay());
            settingsButton?.onClick.AddListener(() => flowManager.ShowSettings(UIFlowManager.Screen.WelcomeAuth));
        }

        protected override void OnShow()
        {
            base.OnShow();
        }

        private void OnDestroy()
        {
            loginButton?.onClick.RemoveAllListeners();
            registerButton?.onClick.RemoveAllListeners();
            quickPlayButton?.onClick.RemoveAllListeners();
            settingsButton?.onClick.RemoveAllListeners();
        }

        private async Task HandleQuickPlay()
        {
            if (authManager == null)
            {
                SetStatus("AuthManager chưa sẵn sàng", true);
                return;
            }

            SetInteractable(false);
            SetStatus("Đang đăng nhập nhanh...", false);

            bool success = await authManager.QuickPlay();
            if (success)
            {
                SetStatus("Thành công!", false);
                flowManager.ShowScreen(UIFlowManager.Screen.MainMenu);
            }
            else
            {
                SetStatus("Không thể đăng nhập nhanh.", true);
            }

            SetInteractable(true);
        }

        private void SetStatus(string message, bool isError)
        {
            if (statusText == null) return;
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.white;
        }

        private void SetInteractable(bool enabled)
        {
            quickPlayButton.interactable = enabled;
            loginButton.interactable = enabled;
            registerButton.interactable = enabled;
        }
    }
}
