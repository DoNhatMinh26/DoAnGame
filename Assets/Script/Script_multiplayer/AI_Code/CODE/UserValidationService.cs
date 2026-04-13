using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Firebase.Firestore;
using UnityEngine;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Dịch vụ xác thực dữ liệu người dùng (Email, Password, Character Name)
    /// Mục đích: Tập trung logic validation tại 1 chỗ, dễ maintain và test
    /// </summary>
    public class UserValidationService : MonoBehaviour
    {
        public static UserValidationService Instance { get; private set; }

        private FirebaseFirestore firestore;
        private bool hasLoggedPermissionWarning;

        // Validation regex patterns
        private const string EMAIL_PATTERN = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
        private const string VALID_ALPHANUM_PATTERN = @"^[a-zA-Z0-9_]+$";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            try
            {
                firestore = FirebaseFirestore.DefaultInstance;
                RuntimeInstanceContext.ConfigureFirestoreSettings(firestore, "UserValidation");
            }
            catch
            {
                firestore = null;
            }
        }

        /// <summary>
        /// Xác thực email
        /// </summary>
        public ValidationResult ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new ValidationResult(false, "email_empty", "Email không được để trống");
            }

            if (!Regex.IsMatch(email, EMAIL_PATTERN))
            {
                return new ValidationResult(false, "invalid_email_format", "Email không hợp lệ (ví dụ: user@example.com)");
            }

            return new ValidationResult(true, "ok", "Email hợp lệ");
        }

        /// <summary>
        /// Xác thực mật khẩu (phải có: 8+ ký tự, chữ hoa, chữ thường, số)
        /// </summary>
        public ValidationResult ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new ValidationResult(false, "password_empty", "Mật khẩu không được để trống");
            }

            if (password.Length < 8)
            {
                return new ValidationResult(false, "password_too_short", "Mật khẩu phải có ít nhất 8 ký tự");
            }

            bool hasUpperCase = Regex.IsMatch(password, @"[A-Z]");
            bool hasLowerCase = Regex.IsMatch(password, @"[a-z]");
            bool hasNumber = Regex.IsMatch(password, @"[0-9]");

            if (!hasUpperCase || !hasLowerCase || !hasNumber)
            {
                return new ValidationResult(false, "weak_password",
                    "Mật khẩu phải chứa: chữ hoa, chữ thường, và số");
            }

            return new ValidationResult(true, "ok", "Mật khẩu hợp lệ");
        }

        /// <summary>
        /// Xác thực tên nhân vật (3-20 ký tự, chỉ alphanumeric + underscore, check unique trong DB)
        /// </summary>
        public async Task<ValidationResult> ValidateCharacterName(string characterName)
        {
            if (string.IsNullOrWhiteSpace(characterName))
            {
                return new ValidationResult(false, "character_name_empty", "Tên nhân vật không được để trống");
            }

            if (characterName.Length < 3)
            {
                return new ValidationResult(false, "character_name_too_short", "Tên nhân vật phải có ít nhất 3 ký tự");
            }

            if (characterName.Length > 20)
            {
                return new ValidationResult(false, "character_name_too_long", "Tên nhân vật không được vượt quá 20 ký tự");
            }

            if (!Regex.IsMatch(characterName, VALID_ALPHANUM_PATTERN))
            {
                return new ValidationResult(false, "character_name_invalid_chars",
                    "Tên nhân vật chỉ chứa chữ, số và dấu gạch dưới (_)");
            }

            // Check xem tên nhân vật đã tồn tại trong DB chưa?
            bool isUnique = await IsCharacterNameUnique(characterName);
            if (!isUnique)
            {
                return new ValidationResult(false, "character_name_taken", "Tên nhân vật này đã có người dùng");
            }

            return new ValidationResult(true, "ok", "Tên nhân vật hợp lệ");
        }

        /// <summary>
        /// Xác thực tuổi
        /// </summary>
        public ValidationResult ValidateAge(int age)
        {
            if (age < 5 || age > 100)
            {
                return new ValidationResult(false, "age_out_of_range", "Tuổi phải từ 5 đến 100");
            }

            return new ValidationResult(true, "ok", "Tuổi hợp lệ");
        }

        /// <summary>
        /// Xác thực confirm password khớp với password
        /// </summary>
        public ValidationResult ValidatePasswordMatch(string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                return new ValidationResult(false, "password_mismatch", "Xác nhận mật khẩu không khớp");
            }

            return new ValidationResult(true, "ok", "Mật khẩu khớp");
        }

        /// <summary>
        /// Kiểm tra xem tên nhân vật đã tồn tại trong DB chưa
        /// Query Firebase: /users để tìm document có characterName = input
        /// </summary>
        private async Task<bool> IsCharacterNameUnique(string characterName)
        {
            try
            {
                if (firestore != null)
                {
                    Query query = firestore.Collection("users")
                        .WhereEqualTo("characterName", characterName)
                        .Limit(1);

                    QuerySnapshot snapshot = await query.GetSnapshotAsync();
                    return snapshot.Count == 0;
                }

                Debug.LogWarning("[UserValidation] Firestore chưa sẵn sàng, tạm cho qua unique check");
                return true;
            }
            catch (System.Exception ex)
            {
                if (ex.Message != null && ex.Message.Contains("Missing or insufficient permissions"))
                {
                    if (!hasLoggedPermissionWarning)
                    {
                        Debug.LogWarning("[UserValidation] Firestore rules đang chặn query /users khi chưa đăng nhập. Tạm bỏ qua check trùng tên ở client.");
                        hasLoggedPermissionWarning = true;
                    }
                }
                else
                {
                    Debug.LogWarning($"[UserValidation] Unique check skipped: {ex.Message}");
                }
                return true; // Cho phép tạm thời nếu có lỗi
            }
        }
    }

    /// <summary>
    /// Kết quả xác thực
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorCode { get; private set; } // error type (email_empty, weak_password, etc)
        public string Message { get; private set; } // error message for UI

        public ValidationResult(bool isValid, string errorCode, string message)
        {
            IsValid = isValid;
            ErrorCode = errorCode;
            Message = message;
        }
    }
}
