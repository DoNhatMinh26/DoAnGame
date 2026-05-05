using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using DoAnGame.Auth;

namespace DoAnGame.UI
{
    /// <summary>
    /// Controller cho BangXepHang Panel
    /// Hiển thị top players từ Firebase Firestore
    /// </summary>
    public class UILeaderboardPanelController : BasePanelController
    {
        [Header("UI References")]
        [SerializeField] private Button loadButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Transform contentContainer; // Content của ScrollView
        [SerializeField] private LeaderboardEntryWidget entryPrefab; // Template entry

        [Header("Settings")]
        [SerializeField] private int maxEntries = 50; // Số lượng top players hiển thị
        [SerializeField] private bool autoLoadOnShow = true; // Tự động load khi mở panel
        [SerializeField] private bool highlightCurrentPlayer = true; // Highlight player hiện tại

        private FirebaseFirestore firestore;
        private List<LeaderboardEntryWidget> activeEntries = new List<LeaderboardEntryWidget>();
        private bool isLoading = false;
        private string currentPlayerUid; // UID của player hiện tại

        protected override void Awake()
        {
            base.Awake();

            // Setup buttons
            if (loadButton != null)
                loadButton.onClick.AddListener(() => _ = LoadLeaderboardAsync());

            // Nút Back sẽ dùng UIButtonScreenNavigator, không cần code ở đây
            // if (backButton != null)
            //     backButton.onClick.AddListener(OnBackButtonClicked);

            // Initialize Firestore
            try
            {
                firestore = FirebaseFirestore.DefaultInstance;
                RuntimeInstanceContext.ConfigureFirestoreSettings(firestore, "Leaderboard");
                Debug.Log("[Leaderboard] ✅ Firestore initialized");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Leaderboard] ❌ Firestore init failed: {ex.Message}");
                firestore = null;
            }
        }

        protected override void OnShow()
        {
            base.OnShow();

            // Set title
            if (titleText != null)
                titleText.text = "BẢNG XẾP HẠNG";

            // Lấy UID của player hiện tại
            GetCurrentPlayerUid();

            // Auto load nếu được bật
            if (autoLoadOnShow)
            {
                _ = LoadLeaderboardAsync();
            }
            else
            {
                UpdateStatusText("Nhấn 'Load' để tải bảng xếp hạng");
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            ClearEntries();
        }

        /// <summary>
        /// Lấy UID của player hiện tại
        /// </summary>
        private void GetCurrentPlayerUid()
        {
            // Thử lấy từ FirebaseManager
            var firebaseManager = FirebaseManager.Instance;
            if (firebaseManager != null)
            {
                var currentUser = firebaseManager.GetCurrentUser();
                if (currentUser != null)
                {
                    currentPlayerUid = currentUser.UserId;
                    Debug.Log($"[Leaderboard] Current player UID: {currentPlayerUid}");
                    return;
                }
            }

            // Fallback: Lấy từ PlayerPrefs
            currentPlayerUid = PlayerPrefs.GetString(LocalStorageKeyResolver.Key("uid"), null);
            if (string.IsNullOrEmpty(currentPlayerUid))
            {
                currentPlayerUid = PlayerPrefs.GetString("uid", null);
            }

            if (!string.IsNullOrEmpty(currentPlayerUid))
            {
                Debug.Log($"[Leaderboard] Current player UID (from cache): {currentPlayerUid}");
            }
            else
            {
                Debug.LogWarning("[Leaderboard] ⚠️ Không tìm thấy UID của player hiện tại");
            }
        }

        /// <summary>
        /// Load top players từ Firebase
        /// </summary>
        public async Task LoadLeaderboardAsync()
        {
            if (isLoading)
            {
                Debug.LogWarning("[Leaderboard] ⚠️ Đang load, bỏ qua request trùng");
                return;
            }

            if (firestore == null)
            {
                UpdateStatusText("❌ Lỗi: Firestore chưa sẵn sàng");
                Debug.LogError("[Leaderboard] ❌ Firestore is null");
                return;
            }

            isLoading = true;
            UpdateStatusText("⏳ Đang tải dữ liệu...");
            ClearEntries();

            try
            {
                Debug.Log("[Leaderboard] 📥 Loading top players...");

                // Query Firestore: playerData collection
                // Sắp xếp theo totalScore giảm dần, lấy top maxEntries
                var query = firestore.Collection("playerData")
                    .OrderByDescending("totalScore")
                    .Limit(maxEntries);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Count == 0)
                {
                    UpdateStatusText("📭 Chưa có dữ liệu xếp hạng");
                    Debug.Log("[Leaderboard] ℹ️ No data found");
                    return;
                }

                Debug.Log($"[Leaderboard] ✅ Loaded {snapshot.Count} players");

                // Parse data
                List<PlayerData> players = new List<PlayerData>();
                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var data = doc.ToDictionary();
                        PlayerData player = MapToPlayerData(doc.Id, data);
                        players.Add(player);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[Leaderboard] ⚠️ Error parsing player {doc.Id}: {ex.Message}");
                    }
                }

                // Sắp xếp lại theo totalScore (đảm bảo đúng thứ tự)
                players = players.OrderByDescending(p => p.totalScore).ToList();

                // Tạo UI entries
                for (int i = 0; i < players.Count; i++)
                {
                    CreateEntry(i + 1, players[i]);
                }

                UpdateStatusText($"Danh Sách Top {maxEntries}:");
                Debug.Log($"[Leaderboard] ✅ Displayed {players.Count} entries");
            }
            catch (System.Exception ex)
            {
                UpdateStatusText($"❌ Lỗi: {ex.Message}");
                Debug.LogError($"[Leaderboard] ❌ Error loading leaderboard: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// Tạo một entry trong bảng xếp hạng
        /// </summary>
        private void CreateEntry(int rank, PlayerData player)
        {
            if (entryPrefab == null || contentContainer == null)
            {
                Debug.LogError("[Leaderboard] ❌ EntryPrefab hoặc ContentContainer chưa được gán!");
                return;
            }

            // Instantiate entry từ prefab
            LeaderboardEntryWidget entry = Instantiate(entryPrefab, contentContainer);
            entry.gameObject.SetActive(true);

            // Set data
            entry.SetData(rank, player.characterName, player.totalScore, player.level);

            // Kiểm tra xem có phải player hiện tại không
            bool isCurrentPlayer = highlightCurrentPlayer && 
                                   !string.IsNullOrEmpty(currentPlayerUid) && 
                                   player.uid == currentPlayerUid;

            if (isCurrentPlayer)
            {
                // Đánh dấu là player hiện tại (màu xanh dương)
                entry.SetCurrentPlayer(true);
                Debug.Log($"[Leaderboard] 👤 Found current player at rank {rank}");
            }
            else if (rank <= 3)
            {
                // Top 3 nhưng không phải player hiện tại (màu vàng)
                entry.SetHighlight(true);
            }

            // Add to list
            activeEntries.Add(entry);
        }

        /// <summary>
        /// Xóa tất cả entries hiện tại
        /// </summary>
        private void ClearEntries()
        {
            foreach (var entry in activeEntries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            activeEntries.Clear();
        }

        /// <summary>
        /// Cập nhật status text
        /// </summary>
        private void UpdateStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        /// <summary>
        /// Map Firestore data to PlayerData
        /// </summary>
        private static PlayerData MapToPlayerData(string uid, Dictionary<string, object> map)
        {
            return new PlayerData
            {
                uid           = GetString(map, "uid", uid),
                characterName = GetString(map, "characterName", "Player"),
                level         = GetInt(map, "level", 1),
                totalXp       = GetInt(map, "totalXp", 0),
                totalScore    = GetInt(map, "totalScore", 0),
                rank          = GetInt(map, "rank", 0),
                coins         = GetInt(map, "coins", 0),
                gamesPlayed   = GetInt(map, "gamesPlayed", 0),
                gamesWon      = GetInt(map, "gamesWon", 0),
                winRate       = GetFloat(map, "winRate", 0f),
                lastUpdated   = GetLong(map, "lastUpdated", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            };
        }

        private static string GetString(Dictionary<string, object> map, string key, string fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
                return value.ToString();
            return fallback;
        }

        private static int GetInt(Dictionary<string, object> map, string key, int fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
            {
                try { return System.Convert.ToInt32(value); }
                catch { }
            }
            return fallback;
        }

        private static long GetLong(Dictionary<string, object> map, string key, long fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
            {
                try { return System.Convert.ToInt64(value); }
                catch { }
            }
            return fallback;
        }

        private static float GetFloat(Dictionary<string, object> map, string key, float fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
            {
                try { return System.Convert.ToSingle(value); }
                catch { }
            }
            return fallback;
        }
    }
}
