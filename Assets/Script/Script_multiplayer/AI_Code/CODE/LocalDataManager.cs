using UnityEngine;

namespace DoAnGame.Data
{
    /// <summary>
    /// Quản lý lưu trữ local (PlayerPrefs) - cache player data, settings
    /// </summary>
    public static class LocalDataManager
    {
        private const string PLAYER_DATA_KEY = "PlayerData_JSON";
        private const string USER_PROFILE_KEY = "UserProfile_JSON";
        private const string LAST_SYNC_KEY = "LastSyncTime";
        private const string SETTINGS_KEY = "GameSettings_JSON";

        #region Player Data

        /// <summary>
        /// Lưu player data locally (instant)
        /// </summary>
        public static void SavePlayerDataLocal(PlayerData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
                PlayerPrefs.SetString(LAST_SYNC_KEY, System.DateTime.UtcNow.ToString("O"));
                PlayerPrefs.Save();
                
                Debug.Log("[LocalDB] ✅ Saved player data locally");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalDB] ❌ Error saving player data: {ex.Message}");
            }
        }

        /// <summary>
        /// Load player data từ local storage
        /// </summary>
        public static PlayerData LoadPlayerDataLocal()
        {
            try
            {
                if (!PlayerPrefs.HasKey(PLAYER_DATA_KEY))
                {
                    Debug.Log("[LocalDB] ℹ️ No local player data found");
                    return null;
                }

                string json = PlayerPrefs.GetString(PLAYER_DATA_KEY);
                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("[LocalDB] ✅ Loaded player data locally");
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalDB] ❌ Error loading player data: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region User Profile

        /// <summary>
        /// Lưu user profile (avatar URL, tên, tuổi)
        /// </summary>
        public static void SaveUserProfileLocal(UserData userData)
        {
            try
            {
                string json = JsonUtility.ToJson(userData);
                PlayerPrefs.SetString(USER_PROFILE_KEY, json);
                PlayerPrefs.Save();
                
                Debug.Log("[LocalDB] ✅ Saved user profile locally");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalDB] ❌ Error saving user profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Load user profile từ local
        /// </summary>
        public static UserData LoadUserProfileLocal()
        {
            try
            {
                if (!PlayerPrefs.HasKey(USER_PROFILE_KEY))
                {
                    Debug.Log("[LocalDB] ℹ️ No local user profile found");
                    return null;
                }

                string json = PlayerPrefs.GetString(USER_PROFILE_KEY);
                UserData data = JsonUtility.FromJson<UserData>(json);
                Debug.Log("[LocalDB] ✅ Loaded user profile locally");
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalDB] ❌ Error loading user profile: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Settings

        /// <summary>
        /// Lưu game settings (âm thanh, ngôn ngữ, v.v.)
        /// </summary>
        public static void SaveSettingsLocal(GameSettings settings)
        {
            try
            {
                string json = JsonUtility.ToJson(settings);
                PlayerPrefs.SetString(SETTINGS_KEY, json);
                PlayerPrefs.Save();
                
                Debug.Log("[LocalDB] ✅ Saved settings locally");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalDB] ❌ Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Load game settings
        /// </summary>
        public static GameSettings LoadSettingsLocal()
        {
            try
            {
                if (!PlayerPrefs.HasKey(SETTINGS_KEY))
                {
                    Debug.Log("[LocalDB] ℹ️ No local settings found, using defaults");
                    return new GameSettings(); // Return default
                }

                string json = PlayerPrefs.GetString(SETTINGS_KEY);
                GameSettings data = JsonUtility.FromJson<GameSettings>(json);
                Debug.Log("[LocalDB] ✅ Loaded settings locally");
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalDB] ❌ Error loading settings: {ex.Message}");
                return new GameSettings();
            }
        }

        #endregion

        #region Sync Management

        /// <summary>
        /// Get last sync time từ local
        /// </summary>
        public static System.DateTime? GetLastSyncTime()
        {
            try
            {
                if (!PlayerPrefs.HasKey(LAST_SYNC_KEY))
                    return null;

                string timeStr = PlayerPrefs.GetString(LAST_SYNC_KEY);
                if (System.DateTime.TryParseExact(
                    timeStr,
                    "O",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var result))
                {
                    return result;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LocalDB] ⚠️ Error parsing sync time: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Check nếu cần sync từ cloud (dựa vào time)
        /// </summary>
        public static bool NeedsSyncFromCloud(int intervalMinutes = 5)
        {
            var lastSync = GetLastSyncTime();
            if (lastSync == null)
            {
                Debug.Log("[LocalDB] 🔄 No sync history, needs sync");
                return true;
            }

            var timeSince = System.DateTime.UtcNow - lastSync.Value;
            bool needsSync = timeSince.TotalMinutes > intervalMinutes;
            
            if (needsSync)
                Debug.Log($"[LocalDB] 🔄 Last sync was {timeSince.TotalMinutes:F1} min ago, needs refresh");
            else
                Debug.Log($"[LocalDB] ⏭ Last sync was {timeSince.TotalMinutes:F1} min ago, skip");
            
            return needsSync;
        }

        /// <summary>
        /// Update last sync time
        /// </summary>
        public static void UpdateLastSyncTime()
        {
            try
            {
                PlayerPrefs.SetString(LAST_SYNC_KEY, System.DateTime.UtcNow.ToString("O"));
                PlayerPrefs.Save();
                Debug.Log("[LocalDB] ✅ Updated last sync time");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalDB] ❌ Error updating sync time: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clear ALL local data (logout)
        /// </summary>
        public static void ClearAllData()
        {
            try
            {
                PlayerPrefs.DeleteKey(PLAYER_DATA_KEY);
                PlayerPrefs.DeleteKey(USER_PROFILE_KEY);
                PlayerPrefs.DeleteKey(LAST_SYNC_KEY);
                PlayerPrefs.DeleteKey(SETTINGS_KEY);
                PlayerPrefs.Save();
                
                Debug.Log("[LocalDB] ✅ Cleared all local data");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalDB] ❌ Error clearing data: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Game settings structure (lưu locally)
    /// </summary>
    [System.Serializable]
    public class GameSettings
    {
        public float soundVolume = 0.8f;
        public float musicVolume = 0.5f;
        public string language = "vi";
        public string difficulty = "NORMAL";
        public bool notifications = true;
        public bool emailNotifications = false;
    }
}
