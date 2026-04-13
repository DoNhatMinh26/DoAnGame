using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Dịch vụ quản lý dữ liệu người chơi (load từ Firebase, cache locally)
    /// Mục đích: Đồng bộ dữ liệu qua device (cross-device sync)
    /// - Load player data từ Firebase sau khi login
    /// - Cache locally để dùng offline
    /// - Tự động sync khi app resumed
    /// </summary>
    public class PlayerDataService : MonoBehaviour
    {
        public static PlayerDataService Instance { get; private set; }

        private FirebaseFirestore firestore;
        private PlayerData cachedPlayerData;

        // Events
        public delegate void OnPlayerDataLoaded(PlayerData playerData);
        public event OnPlayerDataLoaded PlayerDataLoadedEvent;

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
                RuntimeInstanceContext.ConfigureFirestoreSettings(firestore, "PlayerData");
            }
            catch
            {
                firestore = null;
            }
        }

        /// <summary>
        /// Tải player data từ Firebase
        /// </summary>
        public async Task<PlayerData> LoadPlayerDataAsync(string uid)
        {
            try
            {
                if (firestore == null)
                {
                    Debug.LogWarning("[PlayerData] Firestore chưa sẵn sàng");
                    return null;
                }

                Debug.Log($"[PlayerData] 📥 Loading player data for UID: {uid}");

                // Query Firestore: playerData/{uid}
                var snapshot = await firestore.Collection("playerData").Document(uid).GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogWarning($"[PlayerData] ⚠️ Player data not found for {uid}");
                    return null;
                }

                PlayerData data = MapToPlayerData(uid, snapshot.ToDictionary());

                // Cache locally
                cachedPlayerData = data;
                CachePlayerDataLocal(data);

                Debug.Log($"[PlayerData] ✅ Player data loaded: {data.characterName} Lv{data.level}");
                
                // Invoke event
                PlayerDataLoadedEvent?.Invoke(data);

                return data;
            }
            catch (System.Exception ex)
            {
                string message = ex.Message ?? string.Empty;
                if (message.Contains("Missing or insufficient permissions"))
                {
                    Debug.LogWarning("[PlayerData] ⚠️ Không có quyền đọc playerData theo Firestore rules hiện tại.");
                }
                else
                {
                    Debug.LogWarning($"[PlayerData] ⚠️ Error loading player data: {message}");
                }
                return null;
            }
        }

        private static PlayerData MapToPlayerData(string uid, Dictionary<string, object> map)
        {
            return new PlayerData
            {
                uid = GetString(map, "uid", uid),
                characterName = GetString(map, "characterName", "Player"),
                level = GetInt(map, "level", 1),
                totalXp = GetInt(map, "totalXp", 0),
                totalScore = GetInt(map, "totalScore", 0),
                rank = GetInt(map, "rank", 0),
                gamesPlayed = GetInt(map, "gamesPlayed", 0),
                gamesWon = GetInt(map, "gamesWon", 0),
                winRate = GetFloat(map, "winRate", 0f),
                lastUpdated = GetLong(map, "lastUpdated", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            };
        }

        private static string GetString(Dictionary<string, object> map, string key, string fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
            {
                return value.ToString();
            }
            return fallback;
        }

        private static int GetInt(Dictionary<string, object> map, string key, int fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
            {
                try
                {
                    return System.Convert.ToInt32(value);
                }
                catch { }
            }
            return fallback;
        }

        private static long GetLong(Dictionary<string, object> map, string key, long fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
            {
                try
                {
                    return System.Convert.ToInt64(value);
                }
                catch { }
            }
            return fallback;
        }

        private static float GetFloat(Dictionary<string, object> map, string key, float fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
            {
                try
                {
                    return System.Convert.ToSingle(value);
                }
                catch { }
            }
            return fallback;
        }

        /// <summary>
        /// Lấy cached player data (không gọi API)
        /// </summary>
        public PlayerData GetCachedPlayerData()
        {
            if (cachedPlayerData != null)
            {
                return cachedPlayerData;
            }

            // Try load from local cache
            return LoadPlayerDataLocal();
        }

        /// <summary>
        /// Lưu player data vào local (PlayerPrefs)
        /// </summary>
        private void CachePlayerDataLocal(PlayerData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(LocalStorageKeyResolver.Key("cached_player_data"), json);
                PlayerPrefs.SetString(LocalStorageKeyResolver.Key("cached_player_data_timestamp"), System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                PlayerPrefs.Save();

                Debug.Log("[PlayerData] 💾 Player data cached locally");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerData] ❌ Error caching player data: {ex.Message}");
            }
        }

        /// <summary>
        /// Tải player data từ local cache
        /// </summary>
        private PlayerData LoadPlayerDataLocal()
        {
            try
            {
                string json = PlayerPrefs.GetString(LocalStorageKeyResolver.Key("cached_player_data"), null);
                if (string.IsNullOrEmpty(json))
                {
                    json = PlayerPrefs.GetString("cached_player_data", null);
                    if (!string.IsNullOrEmpty(json))
                    {
                        PlayerPrefs.SetString(LocalStorageKeyResolver.Key("cached_player_data"), json);

                        string legacyTimestamp = PlayerPrefs.GetString("cached_player_data_timestamp", null);
                        if (!string.IsNullOrEmpty(legacyTimestamp))
                        {
                            PlayerPrefs.SetString(LocalStorageKeyResolver.Key("cached_player_data_timestamp"), legacyTimestamp);
                        }

                        PlayerPrefs.DeleteKey("cached_player_data");
                        PlayerPrefs.DeleteKey("cached_player_data_timestamp");
                        PlayerPrefs.Save();
                    }
                }
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                cachedPlayerData = data;

                Debug.Log($"[PlayerData] 📂 Loaded from local cache: {data.characterName}");
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerData] ❌ Error loading local cache: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Xóa local cache (khi logout)
        /// </summary>
        public void ClearLocalCache()
        {
            try
            {
                PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("cached_player_data"));
                PlayerPrefs.DeleteKey(LocalStorageKeyResolver.Key("cached_player_data_timestamp"));
                PlayerPrefs.DeleteKey("cached_player_data");
                PlayerPrefs.DeleteKey("cached_player_data_timestamp");
                PlayerPrefs.Save();

                cachedPlayerData = null;

                Debug.Log("[PlayerData] 🗑️ Local cache cleared");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerData] ❌ Error clearing cache: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Player data model (match Firebase schema)
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public string uid;
        public string characterName;
        public int level = 1;
        public int totalXp = 0;
        public int totalScore = 0;
        public int rank = 0;
        public int gamesPlayed = 0;
        public int gamesWon = 0;
        public float winRate = 0f;
        public long lastUpdated;
    }
}
