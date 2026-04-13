using System;
using UnityEngine;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Quản lý phiên đăng nhập (Session token + expiry)
    /// Chức năng:
    /// - Lưu session token + thời gian hết hạn
    /// - Kiểm tra session còn hạn hay không
    /// - Auto-login nếu session còn hạn
    /// - Logout (xóa session)
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        // PlayerPrefs keys
        // Session config
        private const int SESSION_EXPIRY_HOURS = 24; // 24h expiry

        public event Action OnSessionExpired;

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

        /// <summary>
        /// Lưu session token + expiry
        /// </summary>
        public void SaveSession(string uid, string email = null)
        {
            try
            {
                // Generate simple token (in real app, use proper JWT)
                string token = GenerateSessionToken(uid);

                // Calculate expiry (now + 24h)
                long expiryTicks = DateTime.UtcNow.AddHours(SESSION_EXPIRY_HOURS).Ticks;

                // Save to PlayerPrefs
                PlayerPrefs.SetString(LocalStorageKeyResolver.Key("session_token"), token);
                PlayerPrefs.SetString(LocalStorageKeyResolver.Key("session_expiry"), expiryTicks.ToString());
                
                // Optional: save last email for convenience
                if (!string.IsNullOrEmpty(email))
                {
                    PlayerPrefs.SetString(LocalStorageKeyResolver.Key("last_email"), email);
                }

                PlayerPrefs.Save();

                Debug.Log($"[Session] ✅ Session saved. Expires at: {new DateTime(expiryTicks):yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Session] ❌ Error saving session: {ex.Message}");
            }
        }

        /// <summary>
        /// Kiểm tra session còn hạn hay không
        /// </summary>
        public bool IsSessionValid()
        {
            try
            {
                string token = PlayerPrefs.GetString(LocalStorageKeyResolver.Key("session_token"), null);
                if (string.IsNullOrEmpty(token))
                {
                    token = PlayerPrefs.GetString("session_token", null);
                    if (!string.IsNullOrEmpty(token))
                    {
                        string legacyExpiry = PlayerPrefs.GetString("session_expiry", null);
                        if (!string.IsNullOrEmpty(legacyExpiry))
                        {
                            PlayerPrefs.SetString(LocalStorageKeyResolver.Key("session_token"), token);
                            PlayerPrefs.SetString(LocalStorageKeyResolver.Key("session_expiry"), legacyExpiry);
                            string legacyEmail = PlayerPrefs.GetString("last_email", null);
                            if (!string.IsNullOrEmpty(legacyEmail))
                            {
                                PlayerPrefs.SetString(LocalStorageKeyResolver.Key("last_email"), legacyEmail);
                            }

                            PlayerPrefs.DeleteKey("session_token");
                            PlayerPrefs.DeleteKey("session_expiry");
                            PlayerPrefs.DeleteKey("last_email");
                            PlayerPrefs.Save();
                        }
                    }
                }
                if (string.IsNullOrEmpty(token))
                {
                    Debug.Log("[Session] ℹ️ Không tìm thấy session local");
                    return false;
                }

                string expiryStr = PlayerPrefs.GetString(LocalStorageKeyResolver.Key("session_expiry"), null);
                if (string.IsNullOrEmpty(expiryStr))
                {
                    Debug.Log("[Session] ℹ️ Không tìm thấy thời hạn session local");
                    return false;
                }

                if (!long.TryParse(expiryStr, out long expiryTicks))
                {
                    Debug.LogError("[Session] ❌ Invalid session expiry format");
                    return false;
                }

                DateTime expiryTime = new DateTime(expiryTicks);
                DateTime now = DateTime.UtcNow;

                bool isValid = now < expiryTime;

                if (isValid)
                {
                    Debug.Log($"[Session] ✅ Session local còn hạn. Hết hạn lúc: {expiryTime:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    Debug.Log($"[Session] ℹ️ Session local đã hết hạn. Hết hạn lúc: {expiryTime:yyyy-MM-dd HH:mm:ss}");
                    OnSessionExpired?.Invoke();
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Session] ❌ Error validating session: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy session token
        /// </summary>
        public string GetSessionToken()
        {
            string token = PlayerPrefs.GetString(LocalStorageKeyResolver.Key("session_token"), null);
            if (!string.IsNullOrEmpty(token))
                return token;

            return PlayerPrefs.GetString("session_token", null);
        }

        /// <summary>
        /// Lấy email lần cuối (convenience)
        /// </summary>
        public string GetLastEmail()
        {
            string email = PlayerPrefs.GetString(LocalStorageKeyResolver.Key("last_email"), null);
            if (!string.IsNullOrEmpty(email))
                return email;

            return PlayerPrefs.GetString("last_email", null);
        }

        /// <summary>
        /// Xóa session (logout)
        /// </summary>
        public void ClearSession()
        {
            try
            {
                PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("session_token"));
                PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("session_expiry"));
                PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("last_email"));
                PlayerPrefs.DeleteKey("session_token");
                PlayerPrefs.DeleteKey("session_expiry");
                PlayerPrefs.DeleteKey("last_email");
                PlayerPrefs.Save();

                Debug.Log("[Session] ✅ Đã xóa session local (logout)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Session] ❌ Error clearing session: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate simple session token
        /// Note: In production, use proper JWT with Firebase ID token
        /// </summary>
        private string GenerateSessionToken(string uid)
        {
            // Simple format: uid_timestamp_random
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            int random = UnityEngine.Random.Range(10000, 99999);
            return $"{uid}_{timestamp}_{random}";
        }
    }
}
