using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DoAnGame.Auth;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 3: Login Panel
    /// Cập nhật: Loading + Cross-Device Sync + Validation
    /// </summary>
    public class UILoginPanelController : FlowPanelController
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text messageText;                 // Error/Success message
        [SerializeField] private Button loginButton;
        [SerializeField] private Button forgotPasswordButton;
        [SerializeField] private Button registerRedirectButton;       // Nút "Đăng Ký"
        [SerializeField] private UIFlowManager flowManager;

        [Header("Loading")]
        [SerializeField] private UILoadingIndicator loadingIndicator;

        private AuthManager authManager;
        private UserValidationService validationService;
        private UnityAction loginClickHandler;
        private UnityAction forgotClickHandler;
        private UnityAction registerRedirectClickHandler;
        private bool isSubmitting;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;
            validationService = UserValidationService.Instance;

            var submitNavigator = loginButton != null ? loginButton.GetComponent<UIButtonScreenNavigator>() : null;
            if (submitNavigator != null)
            {
                submitNavigator.enabled = false;
            }

            loginClickHandler = OnLoginClicked;
            forgotClickHandler = HandleForgotPassword;
            registerRedirectClickHandler = HandleRegisterRedirect;

            if (flowManager == null)
            {
                flowManager = GetComponentInParent<UIFlowManager>();
            }

            if (flowManager == null)
            {
                flowManager = FindObjectOfType<UIFlowManager>(true);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindButtonListeners();
        }

        private void OnEnable()
        {
            BindButtonListeners();
            ResetPanelStateForFreshLogin();

            if (authManager == null)
            {
                authManager = AuthManager.Instance;
                if (authManager == null)
                {
                    var authManagerObj = new GameObject("AuthManager");
                    authManager = authManagerObj.AddComponent<AuthManager>();
                }
            }

        }

        protected override void OnShow()
        {
            base.OnShow();
            ResetPanelStateForFreshLogin();
        }

        private void OnDisable()
        {
            UnbindButtonListeners();
        }

        private void ResetPanelStateForFreshLogin()
        {
            isSubmitting = false;
            HideLoading();
            DisableUI(false);
            ClearMessages();

            if (passwordInput != null)
            {
                passwordInput.text = string.Empty;
            }
        }

        private void BindButtonListeners()
        {
            if (loginButton != null && loginClickHandler != null)
            {
                loginButton.onClick.RemoveAllListeners();
                loginButton.onClick.AddListener(loginClickHandler);
            }

            if (forgotPasswordButton != null && forgotClickHandler != null)
            {
                forgotPasswordButton.onClick.RemoveAllListeners();
                forgotPasswordButton.onClick.AddListener(forgotClickHandler);
            }

            if (registerRedirectButton != null && registerRedirectClickHandler != null)
            {
                registerRedirectButton.onClick.RemoveAllListeners();
                registerRedirectButton.onClick.AddListener(registerRedirectClickHandler);
            }
        }

        private void UnbindButtonListeners()
        {
            if (loginButton != null && loginClickHandler != null)
            {
                loginButton.onClick.RemoveListener(loginClickHandler);
            }

            if (forgotPasswordButton != null && forgotClickHandler != null)
            {
                forgotPasswordButton.onClick.RemoveListener(forgotClickHandler);
            }

            if (registerRedirectButton != null && registerRedirectClickHandler != null)
            {
                registerRedirectButton.onClick.RemoveListener(registerRedirectClickHandler);
            }
        }

        private void OnLoginClicked()
        {
            _ = HandleLogin();
        }

        private async Task HandleLogin()
        {
            if (isSubmitting)
            {
                return;
            }

            isSubmitting = true;
            try
            {
                Debug.Log("[UILoginPanel] Login button clicked");
                ClearMessages();

                if (authManager == null)
                {
                    authManager = AuthManager.Instance;
                    if (authManager == null)
                    {
                        var authManagerObj = new GameObject("AuthManager");
                        authManager = authManagerObj.AddComponent<AuthManager>();
                    }
                }

                if (validationService == null)
                {
                    validationService = UserValidationService.Instance;
                }

                string email = emailInput?.text?.Trim();
                string password = passwordInput?.text;

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SetErrorMessage("Phải điền đầy đủ thông tin mới được đăng nhập");
                    return;
                }

                if (authManager == null)
                {
                    SetErrorMessage("Hệ thống đăng nhập chưa sẵn sàng");
                    return;
                }

                // === CLIENT-SIDE VALIDATION ===
                if (validationService != null)
                {
                    var emailResult = validationService.ValidateEmail(email);
                    if (!emailResult.IsValid)
                    {
                        SetErrorMessage(emailResult.Message);
                        return;
                    }

                    if (string.IsNullOrEmpty(password))
                    {
                        SetErrorMessage("Mật khẩu không được để trống");
                        return;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                    {
                        SetErrorMessage("Email không hợp lệ");
                        return;
                    }

                    if (string.IsNullOrEmpty(password))
                    {
                        SetErrorMessage("Mật khẩu không được để trống");
                        return;
                    }
                }

                DisableUI(true);
                ShowLoading("Đang đăng nhập...");

                bool success = await authManager.Login(email, password);
                Debug.Log($"[UILoginPanel] Login result: {success}");

                if (success)
                {
                    SetSuccessMessage("Đăng nhập thành công!");
                    HideLoading();
                    await Task.Delay(1000);

                    if (!TryNavigateToMainMenu())
                    {
                        SetErrorMessage("Không tìm thấy cấu hình điều hướng sau đăng nhập");
                        DisableUI(false);
                    }
                }
                else
                {
                    HideLoading();
                    SetErrorMessage("Đăng nhập thất bại. Kiểm tra lại email/mật khẩu.");
                    DisableUI(false);
                }
            }
            finally
            {
                isSubmitting = false;
            }
        }

        private void HandleForgotPassword()
        {
            SetErrorMessage("Tính năng quên mật khẩu sẽ được cập nhật sau.");
        }

        private void HandleRegisterRedirect()
        {
            Debug.Log("[UILoginPanel] Bấm Đăng Ký");

            if (!UIScreenRouter.TryShow(ref flowManager, UIFlowManager.Screen.Register))
            {
                Debug.LogWarning("[UILoginPanel] Không có UIFlowManager nên không thể mở RegisterPanel.");
                SetErrorMessage("Không tìm thấy UIFlowManager");
                return;
            }
        }

        /// <summary>
        /// Show loading spinner
        /// </summary>
        private void ShowLoading(string message = "Đang xử lý...")
        {
            if (loadingIndicator == null)
            {
                loadingIndicator = UILoadingIndicator.Instance;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.Show(message);
            }
        }

        /// <summary>
        /// Hide loading spinner
        /// </summary>
        private void HideLoading()
        {
            if (loadingIndicator == null)
            {
                loadingIndicator = UILoadingIndicator.Instance;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.Hide();
            }
        }

        /// <summary>
        /// Enable/Disable UI (buttons, fields)
        /// </summary>
        private void DisableUI(bool disabled)
        {
            if (loginButton != null)
                loginButton.interactable = !disabled;

            if (emailInput != null)
                emailInput.interactable = !disabled;
            if (passwordInput != null)
                passwordInput.interactable = !disabled;
            if (forgotPasswordButton != null)
                forgotPasswordButton.interactable = !disabled;
            if (registerRedirectButton != null)
                registerRedirectButton.interactable = !disabled;
        }

        /// <summary>
        /// Clear all messages
        /// </summary>
        private void ClearMessages()
        {
            UIMessagePresenter.Clear(messageText);
        }

        /// <summary>
        /// Show error message (RED)
        /// </summary>
        private void SetErrorMessage(string message)
        {
            UIMessagePresenter.ShowError(messageText, message);
        }

        /// <summary>
        /// Show success message (GREEN)
        /// </summary>
        private void SetSuccessMessage(string message)
        {
            UIMessagePresenter.ShowSuccess(messageText, message);
        }

        private bool TryNavigateToMainMenu()
        {
            bool routed = UIScreenRouter.TryShow(ref flowManager, UIFlowManager.Screen.MainMenu);
            if (routed && IsMainMenuVisible())
            {
                return true;
            }

            return ForceShowMainMenuByName();
        }

        private bool IsMainMenuVisible()
        {
            Transform mainMenu = FindMainMenuTransform();
            return mainMenu != null && mainMenu.gameObject.activeInHierarchy;
        }

        private bool ForceShowMainMenuByName()
        {
            Transform canvas = GameObject.Find("GameUICanvas")?.transform;
            if (canvas == null)
            {
                return false;
            }

            Transform mainMenu = FindMainMenuTransform();
            if (mainMenu == null)
            {
                return false;
            }

            for (int i = 0; i < canvas.childCount; i++)
            {
                var child = canvas.GetChild(i);
                if (child != null)
                {
                    var panel = child.GetComponent<BasePanelController>();
                    if (panel != null)
                    {
                        panel.Hide();
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            var mainMenuPanel = mainMenu.GetComponent<BasePanelController>();
            if (mainMenuPanel != null)
            {
                mainMenuPanel.Show();
            }
            else
            {
                mainMenu.gameObject.SetActive(true);
            }
            mainMenu.SetAsLastSibling();
            return true;
        }

        private Transform FindMainMenuTransform()
        {
            Transform canvas = GameObject.Find("GameUICanvas")?.transform;
            if (canvas == null)
            {
                return null;
            }

            // Ưu tiên đúng tên panel cũ.
            Transform byName = canvas.Find("MainMenuPanel");
            if (byName != null)
            {
                return byName;
            }

            // Fallback theo component để tránh lệch tên object giữa các scene/prefab.
            var mainMenuController = canvas.GetComponentInChildren<UIMainMenuController>(true);
            if (mainMenuController != null)
            {
                return mainMenuController.transform;
            }

            return null;
        }
    }
}
