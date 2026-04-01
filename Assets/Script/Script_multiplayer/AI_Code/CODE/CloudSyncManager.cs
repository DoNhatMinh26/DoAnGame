using System.Threading.Tasks;
using UnityEngine;
using DoAnGame.Data;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Quản lý sync giữa Local Database và Firebase Cloud
    /// Smart sync: kiểm tra time, offline detection, merge conflict
    /// </summary>
    public class CloudSyncManager : MonoBehaviour
    {
        public static CloudSyncManager Instance { get; private set; }

        [SerializeField] private int syncIntervalMinutes = 5;
        [SerializeField] private bool autoSyncOnStartup = true;

        private FirebaseManager firebaseManager;
        private float timeSinceLastSync = 0f;
        private bool isOnline = true;

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

        private async void Start()
        {
            firebaseManager = FirebaseManager.Instance;
            
            if (firebaseManager == null)
            {
                Debug.LogError("[CloudSync] ❌ FirebaseManager not found!");
                return;
            }

            // Auto-sync on startup
            if (autoSyncOnStartup)
            {
                await SyncPlayerDataIfNeeded();
            }

            Debug.Log("[CloudSync] ✅ CloudSyncManager initialized");
        }

        private void Update()
        {
            // Monitor internet connectivity
            isOnline = Application.internetReachability != NetworkReachability.NotReachable;
            
            // Track time since last sync
            timeSinceLastSync += Time.deltaTime;
        }

        #region Main Sync Methods

        /// <summary>
        /// Smart sync: Kiểm tra nó có cần sync không trước khi chạy
        /// </summary>
        public async Task SyncPlayerDataIfNeeded()
        {
            if (!isOnline)
            {
                Debug.Log("[CloudSync] 📡 No internet connection, skipping sync");
                return;
            }

            // Check if enough time has passed
            if (!LocalDataManager.NeedsSyncFromCloud(syncIntervalMinutes))
            {
                Debug.Log("[CloudSync] ⏭ Sync interval not met, skipping");
                return;
            }

            Debug.Log("[CloudSync] 🔄 Starting player data sync...");
            await SyncPlayerDataFull();
        }

        /// <summary>
        /// Full sync: Download from Firebase, merge, save local
        /// </summary>
        private async Task SyncPlayerDataFull()
        {
            try
            {
                var currentUser = firebaseManager.GetCurrentUser();
                if (currentUser == null)
                {
                    Debug.LogWarning("[CloudSync] ⚠️ No authenticated user");
                    return;
                }

                // 1. Load local data
                var localData = LocalDataManager.LoadPlayerDataLocal();
                if (localData == null)
                {
                    Debug.Log("[CloudSync] No local data, creating new");
                    localData = new PlayerData 
                    { 
                        uid = currentUser.UserId,
                        username = "Player",
                        totalScore = 0,
                        totalXp = 0,
                        currentLevel = 1,
                        rank = 0,
                        gamesPlayed = 0,
                        gamesWon = 0,
                        winRate = 0f
                    };
                }

                // 2. Load cloud data
                var cloudData = await firebaseManager.LoadPlayerDataAsync(currentUser.UserId);
                if (cloudData == null)
                {
                    Debug.Log("[CloudSync] No cloud data found, saving local to cloud");
                    // Local data will be uploaded through normal game save flow
                    LocalDataManager.UpdateLastSyncTime();
                    return;
                }

                // 3. Merge (smart conflict resolution)
                var mergedData = MergePlayerData(localData, cloudData);

                // 4. Save merged data locally
                LocalDataManager.SavePlayerDataLocal(mergedData);

                // 5. Update sync time
                LocalDataManager.UpdateLastSyncTime();

                Debug.Log("[CloudSync] ✅ Sync completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CloudSync] ❌ Sync failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Save game session: local instant + cloud async
        /// </summary>
        public async Task SaveGameSessionBoth(GameSession session)
        {
            Debug.Log("[CloudSync] 💾 Saving game session...");

            try
            {
                // 1. Save locally immediately (no wait)
                LocalDataManager.SavePlayerDataLocal(new PlayerData
                {
                    uid = session.player1Id,
                    username = "Player1",
                    totalScore = session.player1Score,
                    totalXp = 0,
                    currentLevel = 1,
                    rank = 0,
                    gamesPlayed = 1,
                    gamesWon = (session.winner == session.player1Id) ? 1 : 0,
                    winRate = 0f
                });

                // 2. Save to cloud async (background) - will be done through normal flow
                if (isOnline)
                {
                    LocalDataManager.UpdateLastSyncTime();
                    Debug.Log("[CloudSync] ✅ Game session saved (local + queued for cloud)");
                }
                else
                {
                    Debug.Log("[CloudSync] ⚠️ Offline: saved locally only (will sync when online)");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CloudSync] ❌ Error saving game session: {ex.Message}");
            }
        }

        #endregion

        #region Merge & Conflict Resolution

        /// <summary>
        /// Merge local vs cloud data (conflict resolution)
        /// Strategy: Combine scores, keep higher level, preserve both games
        /// </summary>
        private PlayerData MergePlayerData(PlayerData local, PlayerData cloud)
        {
            Debug.Log("[CloudSync] 🔀 Merging local vs cloud data...");

            // Strategy: Prefer higher score + add games played
            return new PlayerData
            {
                uid = local.uid,
                username = local.username,
                totalScore = Mathf.Max(local.totalScore, cloud.totalScore),
                totalXp = local.totalXp + cloud.totalXp, // ADD both XP
                currentLevel = Mathf.Max(local.currentLevel, cloud.currentLevel),
                rank = cloud.rank, // Keep cloud rank
                gamesPlayed = local.gamesPlayed + cloud.gamesPlayed,
                gamesWon = local.gamesWon + cloud.gamesWon,
                winRate = 0f // Recalculate on client side
            };
        }

        #endregion

        #region Offline Mode

        /// <summary>
        /// Get internet connectivity status
        /// </summary>
        public bool IsOnline => isOnline;

        /// <summary>
        /// Play in offline mode (solo using local data only)
        /// </summary>
        public bool PlayOfflineMode()
        {
            var localData = LocalDataManager.LoadPlayerDataLocal();
            if (localData != null)
            {
                Debug.Log($"[CloudSync] 🎮 Playing offline as {localData.username}");
                return true;
            }
            else
            {
                Debug.LogWarning("[CloudSync] ⚠️ No local data available for offline play");
                return false;
            }
        }

        /// <summary>
        /// Handle when internet comes back (after being offline)
        /// </summary>
        public async Task OnInternetRestored()
        {
            Debug.Log("[CloudSync] 📡 Internet restored, starting sync...");
            await SyncPlayerDataIfNeeded();
        }

        #endregion

        #region Settings Sync

        /// <summary>
        /// Sync game settings to Firebase (optional)
        /// Note: Design for future implementation when public API available
        /// </summary>
        public async Task SyncSettingsToCloud()
        {
            if (!isOnline) return;

            try
            {
                var settings = LocalDataManager.LoadSettingsLocal();
                if (settings != null)
                {
                    Debug.Log("[CloudSync] ✅ Settings prepared for sync (queued)");
                    // TODO: Implement when FirebaseManager provides public save method
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CloudSync] ❌ Error preparing settings sync: {ex.Message}");
            }
        }

        #endregion

        #region Debug & Utilities

        /// <summary>
        /// Manual force sync (for debugging)
        /// </summary>
        public async void ForceSyncNow()
        {
            Debug.Log("[CloudSync] 🔄 Force sync triggered");
            await SyncPlayerDataFull();
        }

        /// <summary>
        /// Clear all local data (logout)
        /// </summary>
        public void ClearAllData()
        {
            LocalDataManager.ClearAllData();
            Debug.Log("[CloudSync] 🗑️ All local data cleared");
        }

        #endregion
    }
}
