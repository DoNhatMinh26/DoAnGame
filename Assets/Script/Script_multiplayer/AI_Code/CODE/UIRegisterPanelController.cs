using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 4: Register Panel
    /// </summary>
    public class UIRegisterPanelController : FlowPanelController
    {
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField confirmPasswordInput;
        [SerializeField] private TMP_Dropdown ageDropdown;
        [SerializeField] private Toggle termsToggle;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text successText;
        [SerializeField] private Button completeButton;
        [SerializeField] private UIFlowManager flowManager;

        private AuthManager authManager;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;
            completeButton?.onClick.AddListener(() => _ = HandleRegister());
        }

        private void OnDestroy()
        {
            base.OnDestroy();
            completeButton?.onClick.RemoveAllListeners();
        }

        private async Task HandleRegister()
        {
            ClearMessages();

            string email = emailInput?.text?.Trim();
            string username = usernameInput?.text?.Trim();
            string password = passwordInput?.text;
            string confirmPassword = confirmPasswordInput?.text;
            int age = ageDropdown != null && ageDropdown.options.Count > 0
                ? int.Parse(ageDropdown.options[ageDropdown.value].text)
                : 18;

            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                SetError("Email không hợp lệ");
                return;
            }

            if (string.IsNullOrEmpty(username) || username.Length < 3)
            {
                SetError("Username phải có ít nhất 3 ký tự");
                return;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                SetError("Mật khẩu phải >= 8 ký tự");
                return;
            }

            if (password != confirmPassword)
            {
                SetError("Xác nhận mật khẩu không khớp");
                return;
            }

            if (termsToggle != null && !termsToggle.isOn)
            {
                SetError("Bạn cần chấp nhận điều khoản");
                return;
            }

            completeButton.interactable = false;

            bool success = await authManager.Register(email, password, username, age);
            if (success)
            {
                SetSuccess("Đăng ký thành công! Vui lòng đăng nhập.");
                flowManager.ShowScreen(UIFlowManager.Screen.Login);
            }
            else
            {
                SetError("Đăng ký thất bại. Thử lại sau.");
            }

            completeButton.interactable = true;
        }

        private void ClearMessages()
        {
            SetError(string.Empty);
            SetSuccess(string.Empty);
        }

        private void SetError(string message)
        {
            if (errorText == null) return;
            errorText.text = message;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void SetSuccess(string message)
        {
            if (successText == null) return;
            successText.text = message;
            successText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}
