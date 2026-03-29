using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 3: Login Panel
    /// </summary>
    public class UILoginPanelController : FlowPanelController
    {
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private UIFlowManager flowManager;

        private AuthManager authManager;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;

            loginButton?.onClick.AddListener(() => _ = HandleLogin());
            forgotPasswordButton?.onClick.AddListener(HandleForgotPassword);
        }

        private void OnDestroy()
        {
            base.OnDestroy();
            loginButton?.onClick.RemoveAllListeners();
            forgotPasswordButton?.onClick.RemoveAllListeners();
        }

        private async Task HandleLogin()
        {
            SetError(string.Empty);

            string email = emailInput?.text?.Trim();
            string password = passwordInput?.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SetError("Vui lòng nhập email và mật khẩu");
                return;
            }

            loginButton.interactable = false;

            bool success = await authManager.Login(email, password);
            if (success)
            {
                flowManager.ShowScreen(UIFlowManager.Screen.MainMenu);
            }
            else
            {
                SetError("Đăng nhập thất bại. Kiểm tra lại thông tin.");
            }

            loginButton.interactable = true;
        }

        private void HandleForgotPassword()
        {
            SetError("Tính năng quên mật khẩu sẽ được cập nhật sau.");
        }

        private void SetError(string message)
        {
            if (errorText == null) return;
            errorText.text = message;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}
