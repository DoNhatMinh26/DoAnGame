using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Auth;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 4: Register Panel
    /// Cập nhật: Character Name + Loading + Validation + Cross-Device Sync
    /// </summary>
    public class UIRegisterPanelController : FlowPanelController
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField characterNameInput;    // ← Character Name (tên nhân vật)
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField confirmPasswordInput;
        [SerializeField] private TMP_Dropdown ageDropdown;
        [SerializeField] private Toggle termsToggle;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text messageText;                 // ← Merge error + success
        [SerializeField] private Button completeButton;
        [SerializeField] private UIFlowManager flowManager;
        [SerializeField] private UIButtonScreenNavigator registerSuccessNavigator;

        [Header("Loading")]
        [SerializeField] private UILoadingIndicator loadingIndicator;

        private AuthManager authManager;
        private UserValidationService validationService;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;
            validationService = UserValidationService.Instance;

            var submitNavigator = completeButton != null ? completeButton.GetComponent<UIButtonScreenNavigator>() : null;
            if (submitNavigator != null)
            {
                submitNavigator.enabled = false;
            }

            completeButton?.onClick.AddListener(() => _ = HandleRegister());

            if (termsToggle != null)
            {
                // Start unchecked to avoid accidental "click once = uncheck" confusion.
                termsToggle.SetIsOnWithoutNotify(false);
                termsToggle.onValueChanged.AddListener(HandleTermsChanged);
            }

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
            if (termsToggle != null)
            {
                termsToggle.onValueChanged.RemoveListener(HandleTermsChanged);
            }
            completeButton?.onClick.RemoveAllListeners();
        }

        private void HandleTermsChanged(bool isOn)
        {
            Debug.Log($"[UIRegisterPanel] Terms changed => isOn:{isOn}");
            if (isOn)
            {
                ClearMessages();
            }
        }

        private void OnEnable()
        {
            if (termsToggle != null)
            {
                // Every time panel is opened, require explicit acceptance again.
                termsToggle.SetIsOnWithoutNotify(false);
            }

            ClearMessages();
        }

        private async Task HandleRegister()
        {
            ClearMessages();

            try
            {
                // Resolve services again at submit time in case startup order made Awake too early.
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

                if (termsToggle == null)
                {
                    var toggles = GetComponentsInChildren<Toggle>(true);
                    foreach (var t in toggles)
                    {
                        if (t != null && t.gameObject.activeInHierarchy)
                        {
                            termsToggle = t;
                            break;
                        }
                    }
                }

                bool acceptedTerms = IsTermsAccepted();
                Debug.Log($"[UIRegisterPanel] Terms status => assigned:{(termsToggle != null)}, isOn:{acceptedTerms}, interactable:{(termsToggle != null && termsToggle.interactable)}, active:{(termsToggle != null && termsToggle.gameObject.activeInHierarchy)}");

                // Get input values
                string email = emailInput?.text?.Trim();
                string characterName = characterNameInput?.text?.Trim();
                string password = passwordInput?.text;
                string confirmPassword = confirmPasswordInput?.text;
                int age = 18;

                if (ageDropdown != null && ageDropdown.options.Count > 0)
                {
                    string selectedAge = ageDropdown.options[ageDropdown.value].text?.Trim();
                    if (!int.TryParse(selectedAge, out age))
                    {
                        SetErrorMessage("Tuổi không hợp lệ. Vui lòng chọn số tuổi.");
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(email)
                    || string.IsNullOrWhiteSpace(characterName)
                    || string.IsNullOrWhiteSpace(password)
                    || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    SetErrorMessage("Phải điền đầy đủ thông tin mới được đăng ký");
                    return;
                }

                if (!acceptedTerms)
                {
                    SetErrorMessage("Bạn cần chấp nhận điều khoản");
                    return;
                }

                if (authManager == null)
                {
                    SetErrorMessage("Hệ thống đăng ký chưa sẵn sàng");
                    return;
                }

                // === CLIENT-SIDE VALIDATION ===
                if (validationService != null)
                {
                    // Validate email
                    var emailResult = validationService.ValidateEmail(email);
                    if (!emailResult.IsValid)
                    {
                        SetErrorMessage(emailResult.Message);
                        return;
                    }

                    // Validate character name (already includes unique check)
                    var charNameResult = await validationService.ValidateCharacterName(characterName);
                    if (!charNameResult.IsValid)
                    {
                        SetErrorMessage(charNameResult.Message);
                        return;
                    }

                    // Validate password
                    var passwordResult = validationService.ValidatePassword(password);
                    if (!passwordResult.IsValid)
                    {
                        SetErrorMessage(passwordResult.Message);
                        return;
                    }

                    // Validate password match
                    var matchResult = validationService.ValidatePasswordMatch(password, confirmPassword);
                    if (!matchResult.IsValid)
                    {
                        SetErrorMessage(matchResult.Message);
                        return;
                    }

                    // Validate age
                    var ageResult = validationService.ValidateAge(age);
                    if (!ageResult.IsValid)
                    {
                        SetErrorMessage(ageResult.Message);
                        return;
                    }
                }

                // === REGISTER (with loading) ===
                DisableUI(true);
                ShowLoading("Đang tạo tài khoản...");

                bool success = await authManager.Register(email, password, characterName, age);

                if (success)
                {
                    SetSuccessMessage("Đăng ký thành công! Vui lòng đăng nhập.");
                    HideLoading();
                    
                    // Auto-navigate to Login sau 1 giây
                    await Task.Delay(1000);

                    bool routed = false;
                    if (registerSuccessNavigator != null)
                    {
                        registerSuccessNavigator.NavigateNow();
                        routed = true;
                    }

                    if (!routed)
                    {
                        routed = UIScreenRouter.TryShow(ref flowManager, UIFlowManager.Screen.Login);
                    }

                    if (!routed)
                    {
                        SetErrorMessage("Không tìm thấy cấu hình điều hướng sau đăng ký");
                        DisableUI(false);
                    }
                }
                else
                {
                    HideLoading();
                    string detail = FirebaseManager.Instance != null ? FirebaseManager.Instance.GetLastRegisterErrorDetail() : null;
                    SetErrorMessage(MapRegisterFailureMessage(detail));
                    DisableUI(false);
                }
            }
            catch (System.Exception ex)
            {
                HideLoading();
                DisableUI(false);
                SetErrorMessage("Có lỗi khi đăng ký. Vui lòng thử lại.");
                Debug.LogWarning($"[UIRegisterPanel] HandleRegister exception: {ex.Message}");
            }
        }

        private string MapRegisterFailureMessage(string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
                return "Đăng ký thất bại. Thử lại sau.";

            string code = detail;
            int idx = detail.IndexOf(':');
            if (idx > 0)
            {
                code = detail.Substring(0, idx);
            }

            switch (code)
            {
                case "email_already_exists":
                    return "Email đã được đăng ký. Hãy dùng email khác hoặc đăng nhập.";
                case "weak_password":
                    return "Mật khẩu yếu. Cần ít nhất 8 ký tự gồm chữ hoa, chữ thường và số.";
                case "character_name_taken":
                    return "Tên nhân vật đã tồn tại. Vui lòng chọn tên khác.";
                case "invalid_email_format":
                case "email_invalid":
                    return "Email không hợp lệ.";
                case "age_out_of_range":
                    return "Tuổi không hợp lệ (5-100).";
                default:
                    return "Đăng ký thất bại. Kiểm tra lại thông tin và thử lại.";
            }
        }

        /// <summary>
        /// Show loading spinner
        /// </summary>
        private void ShowLoading(string message = "Đang xử lý...")
        {
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
            if (completeButton != null)
                completeButton.interactable = !disabled;

            if (emailInput != null)
                emailInput.interactable = !disabled;
            if (characterNameInput != null)
                characterNameInput.interactable = !disabled;
            if (passwordInput != null)
                passwordInput.interactable = !disabled;
            if (confirmPasswordInput != null)
                confirmPasswordInput.interactable = !disabled;
            if (ageDropdown != null)
                ageDropdown.interactable = !disabled;
            if (termsToggle != null)
                termsToggle.interactable = !disabled;
        }

        /// <summary>
        /// Clear all messages
        /// </summary>
        private void ClearMessages()
        {
            UIMessagePresenter.Clear(messageText, normalizeRect: true);
        }

        /// <summary>
        /// Show error message (RED)
        /// </summary>
        private void SetErrorMessage(string message)
        {
            UIMessagePresenter.ShowError(messageText, message, normalizeRect: true);
        }

        /// <summary>
        /// Show success message (GREEN)
        /// </summary>
        private void SetSuccessMessage(string message)
        {
            UIMessagePresenter.ShowSuccess(messageText, message, normalizeRect: true);
        }

        private bool IsTermsAccepted()
        {
            return termsToggle != null && termsToggle.isOn;
        }
    }
}
