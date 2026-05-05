using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Reflection; // For IsVisible workaround

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller cho panel Quên Mật Khẩu
    /// Gửi email reset password qua Firebase Auth
    /// </summary>
    public class UIForgotPasswordController : FlowPanelController
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField emailInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button completeButton; // Nút "Hoàn Thành" để quay lại LoginPanel
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text errorText;

        [Header("Anti-Spam")]
        [SerializeField] private float cooldownSeconds = 60f; // 1 phút cooldown
        private static System.Collections.Generic.Dictionary<string, float> emailCooldowns = new System.Collections.Generic.Dictionary<string, float>();

        [SerializeField] private UIFlowManager flowManager;
        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();

            Debug.Log("[ForgotPassword] Awake() called!");
            
            // Force reset IsVisible để Show() có thể hoạt động
            // Workaround cho bug BasePanelController
            if (!gameObject.activeSelf)
            {
                // Panel đang inactive nhưng IsVisible có thể = true
                // Reset về false để Show() có thể bật panel
                var isVisibleField = typeof(BasePanelController).GetProperty("IsVisible");
                if (isVisibleField != null && isVisibleField.CanWrite)
                {
                    isVisibleField.SetValue(this, false);
                    Debug.Log("[ForgotPassword] Reset IsVisible to false");
                }
            }

            // Auto-find components nếu chưa gán
            if (emailInputField == null)
            {
                emailInputField = transform.Find("ContentPanel/EmailInputField")?.GetComponent<TMP_InputField>();
            }

            if (sendButton == null)
            {
                sendButton = transform.Find("ContentPanel/GuiEmailBtn")?.GetComponent<Button>();
            }

            if (completeButton == null)
            {
                completeButton = transform.Find("ContentPanel/HoanThanhBtn (1)")?.GetComponent<Button>();
            }

            if (statusText == null)
            {
                statusText = transform.Find("ContentPanel/StatusText")?.GetComponent<TMP_Text>();
            }

            if (errorText == null)
            {
                errorText = transform.Find("ContentPanel/ErrorText")?.GetComponent<TMP_Text>();
            }

            // Bind buttons
            if (sendButton != null)
            {
                sendButton.onClick.RemoveAllListeners();
                sendButton.onClick.AddListener(OnSendButtonClicked);
            }

            if (completeButton != null)
            {
                completeButton.onClick.RemoveAllListeners();
                completeButton.onClick.AddListener(OnCompleteButtonClicked);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();

            Debug.Log("[ForgotPassword] OnShow() called!");
            Debug.Log($"[ForgotPassword] GameObject.activeSelf = {gameObject.activeSelf}");
            Debug.Log($"[ForgotPassword] GameObject.activeInHierarchy = {gameObject.activeInHierarchy}");
            Debug.Log($"[ForgotPassword] IsVisible = {IsVisible}");

            // Cleanup old cooldowns
            CleanupOldCooldowns();

            // Reset UI
            if (emailInputField != null)
            {
                emailInputField.text = "";
            }

            HideStatus();
            HideError();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            sendButton?.onClick.RemoveAllListeners();
            completeButton?.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Xử lý khi click button "Gửi Email"
        /// </summary>
        private async void OnSendButtonClicked()
        {
            try
            {
                string email = emailInputField != null ? emailInputField.text.Trim() : "";

                Debug.Log($"[ForgotPassword] Send button clicked, email: {email}");

                // Validate email
                if (string.IsNullOrEmpty(email))
                {
                    ShowError("Vui lòng nhập email!");
                    return;
                }

                if (!IsValidEmail(email))
                {
                    ShowError("Email không hợp lệ!");
                    return;
                }

                // ✅ ANTI-SPAM: Kiểm tra cooldown
                if (IsEmailOnCooldown(email))
                {
                    float remainingTime = GetRemainingCooldown(email);
                    ShowError($"❌ Vui lòng đợi {Mathf.Ceil(remainingTime)} giây trước khi gửi lại!");
                    return;
                }

                // Disable button để tránh spam
                if (sendButton != null)
                {
                    sendButton.interactable = false;
                }

                HideError();
                ShowStatus("Đang gửi email...");

                // SIMPLIFIED: Chỉ dùng Firebase Auth, không check database
                // Gửi email reset password
                bool success = await SendPasswordResetEmail(email);

                if (success)
                {
                    // ✅ SET COOLDOWN: Chỉ set khi gửi thành công
                    SetEmailCooldown(email);
                    
                    ShowStatus("✅ Đã gửi email! Vui lòng kiểm tra hộp thư.");
                    Debug.Log($"[ForgotPassword] Password reset email sent to: {email}");

                    // Không tự động quay về - để user click nút "Hoàn Thành"
                }
                else
                {
                    // Error message đã được hiển thị trong SendPasswordResetEmail()
                    // KHÔNG dừng game - chỉ ẩn status và để user nhập lại
                    HideStatus();
                    Debug.LogWarning($"[ForgotPassword] Email send failed for: {email}");
                }
            }
            catch (System.Exception e)
            {
                // CRITICAL: Catch tất cả exceptions để không crash game
                Debug.LogError($"[ForgotPassword] CRITICAL ERROR in OnSendButtonClicked: {e.Message}");
                Debug.LogError($"[ForgotPassword] Stack trace: {e.StackTrace}");
                
                HideStatus();
                ShowError("❌ Có lỗi xảy ra! Vui lòng thử lại.");
            }
            finally
            {
                // ALWAYS enable button lại
                if (sendButton != null)
                {
                    sendButton.interactable = true;
                }
            }
        }

        /// <summary>
        /// Xử lý khi click button "Hoàn Thành" - quay lại LoginPanel
        /// </summary>
        private void OnCompleteButtonClicked()
        {
            try
            {
                Debug.Log("[ForgotPassword] Complete button clicked - returning to LoginPanel");

                // Tìm LoginPanel
                Transform canvas = transform.parent;
                if (canvas == null)
                {
                    Debug.LogError("[ForgotPassword] Cannot find parent canvas!");
                    ShowError("Không thể quay lại màn hình đăng nhập");
                    return;
                }

                Transform loginPanel = canvas.Find("LoginPanel");
                if (loginPanel == null)
                {
                    Debug.LogError("[ForgotPassword] Cannot find LoginPanel!");
                    ShowError("Không tìm thấy màn hình đăng nhập");
                    return;
                }

                // Ẩn ForgotPasswordPanel
                Hide();

                // Hiển thị LoginPanel
                var flowPanel = loginPanel.GetComponent<FlowPanelController>();
                if (flowPanel != null)
                {
                    flowPanel.Show();
                }
                else
                {
                    loginPanel.gameObject.SetActive(true);
                }

                Debug.Log("[ForgotPassword] Successfully navigated back to LoginPanel");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ForgotPassword] Error in OnCompleteButtonClicked: {e.Message}");
                ShowError("❌ Có lỗi xảy ra khi quay lại!");
            }
        }

        /// <summary>
        /// Gửi email reset password qua Firebase Auth
        /// BƯỚC 1: Kiểm tra email trong Firestore collection 'users'
        /// BƯỚC 2: Nếu tồn tại → Gửi email
        /// </summary>
        private async Task<bool> SendPasswordResetEmail(string email)
        {
            try
            {
                // ===== BƯỚC 1: KIỂM TRA EMAIL TRONG FIRESTORE =====
                Debug.Log($"[ForgotPassword] STEP 1: Checking email in Firestore: {email}");
                
                bool emailExistsInDatabase = await CheckEmailInDatabase(email);
                
                if (!emailExistsInDatabase)
                {
                    // Email KHÔNG TỒN TẠI trong Firestore
                    Debug.LogWarning($"[ForgotPassword] Email NOT FOUND in Firestore: {email}");
                    ShowError("❌ Email chưa đăng ký! Vui lòng đăng ký tài khoản trước.");
                    return false;
                }
                
                // ===== BƯỚC 2: EMAIL TỒN TẠI → GỬI EMAIL RESET =====
                Debug.Log($"[ForgotPassword] STEP 2: Email found in Firestore, sending reset email...");
                
                var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
                if (auth == null)
                {
                    Debug.LogError("[ForgotPassword] Firebase Auth is not initialized!");
                    ShowError("❌ Hệ thống chưa sẵn sàng!");
                    return false;
                }

                await auth.SendPasswordResetEmailAsync(email);
                
                Debug.Log($"[ForgotPassword] SUCCESS: Reset email sent to: {email}");
                return true;
            }
            catch (Firebase.FirebaseException firebaseEx)
            {
                Debug.LogError($"[ForgotPassword] Firebase error: {firebaseEx.ErrorCode} - {firebaseEx.Message}");
                ShowError("❌ Lỗi kết nối Firebase! Vui lòng thử lại.");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ForgotPassword] Error: {e.Message}");
                ShowError("❌ Có lỗi xảy ra! Vui lòng thử lại.");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra email có tồn tại trong Firestore collection 'users' không
        /// Query collection 'users' với field 'email'
        /// </summary>
        private async Task<bool> CheckEmailInDatabase(string email)
        {
            try
            {
                Debug.Log($"[ForgotPassword] Querying Firestore for email: {email}");

                // Lấy Firestore instance (KHÔNG PHẢI Realtime Database!)
                var firestore = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                if (firestore == null)
                {
                    Debug.LogError("[ForgotPassword] Firestore not initialized!");
                    throw new System.Exception("Firestore not available");
                }

                // Query users collection với email field
                var query = firestore.Collection("users")
                    .WhereEqualTo("email", email)
                    .Limit(1);
                
                Debug.Log($"[ForgotPassword] Executing Firestore query...");
                
                // Thực hiện query với timeout
                var queryTask = query.GetSnapshotAsync();
                var timeoutTask = Task.Delay(10000); // 10 seconds timeout
                
                var completedTask = await Task.WhenAny(queryTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Debug.LogError("[ForgotPassword] Query TIMEOUT after 10 seconds!");
                    throw new System.TimeoutException("Firestore query timeout");
                }
                
                var snapshot = await queryTask;
                
                // Kiểm tra kết quả
                if (snapshot == null)
                {
                    Debug.Log($"[ForgotPassword] Query returned NULL snapshot");
                    return false;
                }
                
                if (snapshot.Count == 0)
                {
                    Debug.Log($"[ForgotPassword] No documents found with email: {email}");
                    return false;
                }
                
                // TÌM THẤY EMAIL!
                Debug.Log($"[ForgotPassword] ✅ EMAIL FOUND! Document count: {snapshot.Count}");
                
                // Log thông tin để debug
                foreach (var doc in snapshot.Documents)
                {
                    Debug.Log($"[ForgotPassword] Found user ID: {doc.Id}");
                }
                
                return true;
            }
            catch (System.TimeoutException)
            {
                Debug.LogError("[ForgotPassword] Firestore query timeout!");
                ShowError("❌ Kết nối chậm! Vui lòng thử lại.");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ForgotPassword] Error checking Firestore: {e.Message}");
                Debug.LogError($"[ForgotPassword] Stack trace: {e.StackTrace}");
                
                // QUAN TRỌNG: Nếu có lỗi Firestore → Coi như email KHÔNG tồn tại
                // Để tránh gửi email cho email không hợp lệ
                ShowError("❌ Không thể kiểm tra email! Vui lòng thử lại.");
                return false;
            }
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Hiển thị status message (màu xanh)
        /// </summary>
        private void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.gameObject.SetActive(true);
            }

            Debug.Log($"[ForgotPassword] Status: {message}");
        }

        /// <summary>
        /// Ẩn status message
        /// </summary>
        private void HideStatus()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Hiển thị error message (màu đỏ)
        /// </summary>
        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);

                // Tự động ẩn sau 3 giây
                CancelInvoke(nameof(HideError));
                Invoke(nameof(HideError), 3f);
            }

            Debug.LogWarning($"[ForgotPassword] Error: {message}");
        }

        /// <summary>
        /// Ẩn error message
        /// </summary>
        private void HideError()
        {
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Kiểm tra email có đang trong thời gian cooldown không
        /// </summary>
        private bool IsEmailOnCooldown(string email)
        {
            if (!emailCooldowns.ContainsKey(email))
                return false;

            float lastSentTime = emailCooldowns[email];
            float currentTime = Time.time;
            
            return (currentTime - lastSentTime) < cooldownSeconds;
        }

        /// <summary>
        /// Lấy thời gian cooldown còn lại (giây)
        /// </summary>
        private float GetRemainingCooldown(string email)
        {
            if (!emailCooldowns.ContainsKey(email))
                return 0f;

            float lastSentTime = emailCooldowns[email];
            float currentTime = Time.time;
            float elapsed = currentTime - lastSentTime;
            
            return Mathf.Max(0f, cooldownSeconds - elapsed);
        }

        /// <summary>
        /// Set cooldown cho email (sau khi gửi thành công)
        /// </summary>
        private void SetEmailCooldown(string email)
        {
            emailCooldowns[email] = Time.time;
            Debug.Log($"[ForgotPassword] Set cooldown for {email}: {cooldownSeconds}s");
        }

        /// <summary>
        /// Clear old cooldowns (cleanup)
        /// </summary>
        private void CleanupOldCooldowns()
        {
            var keysToRemove = new System.Collections.Generic.List<string>();
            float currentTime = Time.time;

            foreach (var kvp in emailCooldowns)
            {
                if ((currentTime - kvp.Value) > cooldownSeconds * 2) // Double cooldown time
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                emailCooldowns.Remove(key);
            }
        }
    }
}
