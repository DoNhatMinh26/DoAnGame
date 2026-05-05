using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using DoAnGame.Auth;

namespace DoAnGame.Auth
{
    /// <summary>
    /// Service đồng bộ tiến độ single-player và coins lên Firebase.
    /// - Nếu user đã đăng nhập → sync lên Firestore ngay lập tức
    /// - Nếu guest → chỉ lưu PlayerPrefs local (LocalProgressService)
    ///
    /// Cách dùng từ mini-game:
    ///   CloudSyncService.Instance.OnLevelCompleted("chonda", grade, levelNumber, score, coinsEarned);
    ///   CloudSyncService.Instance.OnCoinsChanged(newTotal);
    ///
    /// Firestore collections:
    ///   playerData/{uid}                          → coins, totalScore, level, xp
    ///   gameModeProgress/{uid}_{mode}_{grade}     → maxLevelUnlocked, totalScore
    ///   levelProgress/{uid}_{mode}_{grade}_{lvl}  → bestScore, attempts
    /// </summary>
    public class CloudSyncService : MonoBehaviour
    {
        public static CloudSyncService Instance { get; private set; }

        private FirebaseFirestore firestore;

        // Firestore collection names
        private const string COL_PLAYER_DATA      = "playerData";
        private const string COL_GAME_PROGRESS    = "gameModeProgress";
        private const string COL_LEVEL_PROGRESS   = "levelProgress";

        // PlayerPrefs keys (dùng chung với mini-game managers)
        private const string KEY_CHONDA    = "Class_HighestLevel";
        private const string KEY_KEOTHADA  = "HighestLevelReached";
        private const string KEY_PHITHUYEN = "Space_HighestLevel";
        private const string KEY_COINS     = "TotalCoins";
        private const string KEY_SCORE     = "UserScore";
        private const string KEY_LEVEL     = "UserLevel";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Chỉ destroy component, không destroy gameObject (vì gán chung AuthServices)
                Destroy(this);
                return;
            }
            Instance = this;
            // Không gọi DontDestroyOnLoad ở đây vì AuthServices đã DontDestroyOnLoad rồi
        }

        private void Start()
        {
            try
            {
                firestore = FirebaseFirestore.DefaultInstance;
                RuntimeInstanceContext.ConfigureFirestoreSettings(firestore, "CloudSync");
            }
            catch
            {
                firestore = null;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PUBLIC API — gọi từ mini-game managers
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Gọi khi người chơi thắng 1 màn trong single-player.
        /// Tự động lưu PlayerPrefs và sync Firebase nếu đã đăng nhập.
        /// </summary>
        /// <param name="gameMode">"chonda" | "keothada" | "phithuyen"</param>
        /// <param name="grade">Lớp 1–5 (UIManager.SelectedGrade)</param>
        /// <param name="levelNumber">Màn vừa thắng (LevelManager.CurrentLevel)</param>
        /// <param name="score">Điểm đạt được trong màn này</param>
        /// <param name="coinsEarned">Tiền kiếm được trong màn này</param>
        public void OnLevelCompleted(string gameMode, int grade, int levelNumber, int score, int coinsEarned = 0)
        {
            // 1. Cập nhật PlayerPrefs local (luôn làm, kể cả guest)
            UpdateLocalProgress(gameMode, levelNumber);

            // 2. Sync Firebase nếu đã đăng nhập
            string uid = GetCurrentUid();
            if (!string.IsNullOrEmpty(uid))
            {
                _ = SyncLevelCompletedAsync(uid, gameMode, grade, levelNumber, score, coinsEarned);
            }
        }

        /// <summary>
        /// Gọi khi tổng tiền thay đổi (mua skin, nhặt coin...).
        /// Sync coins lên Firebase nếu đã đăng nhập.
        /// </summary>
        public void OnCoinsChanged(int newTotal)
        {
            string uid = GetCurrentUid();
            if (!string.IsNullOrEmpty(uid))
            {
                _ = SyncCoinsAsync(uid, newTotal);
            }
        }

        /// <summary>
        /// Gọi khi người dùng nhấn Reset ở Profile.
        /// Reset toàn bộ playerData, gameModeProgress, playerShop về 0 trên Firebase.
        /// Giữ nguyên: grade (lớp học), characterName, email, uid.
        /// </summary>
        public async Task ResetPlayerDataOnFirebase()
        {
            string uid = GetCurrentUid();
            if (string.IsNullOrEmpty(uid) || firestore == null)
            {
                Debug.Log("[CloudSync] Không có uid hoặc Firestore, bỏ qua reset Firebase.");
                return;
            }

            Debug.Log("[CloudSync] 🔄 Resetting player data on Firebase...");

            try
            {
                long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // ── 1. Reset playerData ──────────────────────────────
                var playerReset = new Dictionary<string, object>
                {
                    { "totalScore",  0 },
                    { "totalXp",     0 },
                    { "level",       1 },
                    { "coins",       0 },
                    { "gamesPlayed", 0 },
                    { "gamesWon",    0 },
                    { "winRate",     0f },
                    { "lastUpdated", now }
                };
                await firestore.Collection(COL_PLAYER_DATA).Document(uid)
                    .SetAsync(playerReset, SetOptions.MergeAll);

                // ── 2. Reset gameModeProgress (15 records) ───────────
                string[] modes  = { "chonda", "keothada", "phithuyen" };
                int[]    grades = { 1, 2, 3, 4, 5 };

                foreach (var mode in modes)
                {
                    foreach (var grade in grades)
                    {
                        string progressId = $"{uid}_{mode}_{grade}";
                        var progressReset = new Dictionary<string, object>
                        {
                            { "currentLevel",     1 },
                            { "maxLevelUnlocked", 1 },
                            { "totalScore",       0 },
                            { "bestScore",        0 },
                            { "lastPlayed",       null }
                        };
                        await firestore.Collection(COL_GAME_PROGRESS).Document(progressId)
                            .SetAsync(progressReset, SetOptions.MergeAll);
                    }
                }

                // ── 3. Reset playerShop (về skin mặc định) ───────────
                string[] shopTypes = { "chonda_skin", "keothada_skin", "keothada_phao", "phithuyen_ship" };
                foreach (var shopType in shopTypes)
                {
                    var shopReset = new Dictionary<string, object>
                    {
                        { "selectedId",  0 },
                        { "unlockedIds", "0" }
                    };
                    await firestore.Collection("playerShop").Document($"{uid}_{shopType}")
                        .SetAsync(shopReset, SetOptions.MergeAll);
                }

                // ── 4. Xóa levelProgress (điểm từng màn) ─────────────
                // Không thể xóa collection trong Firestore client SDK,
                // nên reset về 0 bằng cách ghi đè các document đã biết.
                // Thực tế levelProgress chỉ tạo khi chơi, nên sau reset
                // gameModeProgress.maxLevelUnlocked = 1 sẽ khóa lại các màn.
                // levelProgress cũ sẽ bị ghi đè khi chơi lại.
                Debug.Log("[CloudSync] ℹ️ levelProgress giữ nguyên (sẽ bị ghi đè khi chơi lại).");

                Debug.Log("[CloudSync] ✅ Firebase reset hoàn tất (grade giữ nguyên).");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Firebase reset thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// Gọi khi kết thúc trận multiplayer (cả Host và Client đều gọi).
        /// Sync score, gamesPlayed, gamesWon, winRate lên Firebase.
        /// </summary>
        public void OnMultiplayerMatchCompleted(int scoreEarned, bool isWin)
        {
            string uid = GetCurrentUid();
            if (!string.IsNullOrEmpty(uid))
            {
                _ = SyncMultiplayerResultAsync(uid, scoreEarned, isWin);
            }
        }
        public void OnScoreChanged(int newScore, int newLevel)
        {
            string uid = GetCurrentUid();
            if (!string.IsNullOrEmpty(uid))
            {
                _ = SyncScoreAsync(uid, newScore, newLevel);
            }
        }

        /// <summary>
        /// Gọi khi điểm số / level thay đổi (DataManager.AddScore).
        /// Sync score + level lên Firebase nếu đã đăng nhập.
        /// </summary>
        /// </summary>
        /// <param name="shopType">"chonda_skin" | "keothada_skin" | "keothada_phao" | "phithuyen_ship"</param>
        /// <param name="selectedId">ID skin đang trang bị</param>
        /// <param name="unlockedIds">Danh sách ID đã mua (bao gồm cả ID 0)</param>
        public void OnShopPurchased(string shopType, int selectedId, int[] unlockedIds)
        {
            string uid = GetCurrentUid();
            if (!string.IsNullOrEmpty(uid))
            {
                _ = SyncShopAsync(uid, shopType, selectedId, unlockedIds);
            }
        }

        /// <summary>
        /// Gọi khi mua skin thành công — sync trạng thái shop lên Firebase.
        /// Đảm bảo màn chơi đã mở trên Firebase cũng được mở trên máy này.
        /// </summary>
        public async Task RestoreProgressFromFirebase()
        {
            string uid = GetCurrentUid();
            if (string.IsNullOrEmpty(uid) || firestore == null)
            {
                Debug.Log("[CloudSync] Không có uid hoặc Firestore, bỏ qua restore.");
                return;
            }

            Debug.Log("[CloudSync] 🔄 Restoring progress from Firebase...");

            // ── Restore grade từ users/{uid} TRƯỚC TIÊN ──────────────
            // Phải restore grade trước để RestoreModeProgress dùng đúng grade
            await RestoreGrade(uid);

            int grade = UIManager.SelectedGrade;
            if (grade < 1 || grade > 5) grade = 1;

            // Restore tiến độ 3 chế độ cho grade hiện tại
            await RestoreModeProgress(uid, "chonda",   grade, KEY_CHONDA);
            await RestoreModeProgress(uid, "keothada", grade, KEY_KEOTHADA);
            await RestoreModeProgress(uid, "phithuyen",grade, KEY_PHITHUYEN);

            // Restore coins
            await RestoreCoins(uid);

            // Restore score + level
            await RestoreScoreAndLevel(uid);

            // Restore shop (skin đã mua)
            await RestoreShop(uid, "chonda_skin",    "ClassSkinUnlocked", "SelectedClassSkinID");
            await RestoreShop(uid, "keothada_skin",  "SkinUnlocked_",     "SelectedSkinID");
            await RestoreShop(uid, "keothada_phao",  "PhaoUnlocked_",     "SelectedPhaoID");
            await RestoreShop(uid, "phithuyen_ship", "ShipUnlocked_",     "SelectedShipID");

            Debug.Log("[CloudSync] ✅ Restore hoàn tất.");
        }

        /// <summary>
        /// Gọi sau khi đăng nhập thành công để restore tiến độ từ Firebase về máy.

        private async Task SyncMultiplayerResultAsync(string uid, int scoreEarned, bool isWin)
        {
            if (firestore == null) return;
            try
            {
                var docRef = firestore.Collection(COL_PLAYER_DATA).Document(uid);
                var snap   = await docRef.GetSnapshotAsync();

                int prevScore  = 0;
                int prevXp     = 0;
                int prevPlayed = 0;
                int prevWon    = 0;

                if (snap.Exists)
                {
                    var d = snap.ToDictionary();
                    prevScore  = GetInt(d, "totalScore", 0);
                    prevXp     = GetInt(d, "totalXp", 0);
                    prevPlayed = GetInt(d, "gamesPlayed", 0);
                    prevWon    = GetInt(d, "gamesWon", 0);
                }

                int newScore   = prevScore + scoreEarned;
                int newXp      = prevXp + (scoreEarned / 10);
                int newLevel   = 1 + (newXp / 100);
                int newPlayed  = prevPlayed + 1;
                int newWon     = prevWon + (isWin ? 1 : 0);
                float winRate  = newPlayed > 0 ? (float)newWon / newPlayed : 0f;

                var update = new Dictionary<string, object>
                {
                    { "totalScore",  newScore },
                    { "totalXp",     newXp },
                    { "level",       newLevel },
                    { "gamesPlayed", newPlayed },
                    { "gamesWon",    newWon },
                    { "winRate",     winRate },
                    { "lastUpdated", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };

                await docRef.SetAsync(update, SetOptions.MergeAll);

                // Cập nhật PlayerPrefs local để Profile hiển thị đúng ngay
                PlayerPrefs.SetInt(KEY_SCORE, newScore);
                PlayerPrefs.SetInt(KEY_LEVEL, newLevel);
                PlayerPrefs.Save();

                Debug.Log($"[CloudSync] ✅ Multiplayer result synced: +{scoreEarned}đ, win={isWin}, winRate={winRate:P0}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Multiplayer sync thất bại: {ex.Message}");
            }
        }

        private async Task SyncLevelCompletedAsync(
            string uid, string gameMode, int grade,
            int levelNumber, int score, int coinsEarned)
        {
            if (firestore == null) return;

            try
            {
                long now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // ── 1. gameModeProgress ──────────────────────────────
                string progressId = $"{uid}_{gameMode}_{grade}";
                var progressRef   = firestore.Collection(COL_GAME_PROGRESS).Document(progressId);
                var progressSnap  = await progressRef.GetSnapshotAsync();

                int currentMax   = 1;
                int currentTotal = 0;
                int currentBest  = 0;

                if (progressSnap.Exists)
                {
                    var d = progressSnap.ToDictionary();
                    currentMax   = GetInt(d, "maxLevelUnlocked", 1);
                    currentTotal = GetInt(d, "totalScore", 0);
                    currentBest  = GetInt(d, "bestScore", 0);
                }

                int newMax = Mathf.Max(currentMax, levelNumber + 1);
                newMax = Mathf.Min(newMax, 100);

                var progressUpdate = new Dictionary<string, object>
                {
                    { "progressId",       progressId },
                    { "uid",              uid },
                    { "gameMode",         gameMode },
                    { "grade",            grade },
                    { "currentLevel",     levelNumber },
                    { "maxLevelUnlocked", newMax },
                    { "totalScore",       currentTotal + score },
                    { "bestScore",        Mathf.Max(currentBest, score) },
                    { "lastPlayed",       now }
                };
                await progressRef.SetAsync(progressUpdate, SetOptions.MergeAll);

                // ── 2. levelProgress ─────────────────────────────────
                string levelId  = $"{uid}_{gameMode}_{grade}_{levelNumber}";
                var levelRef    = firestore.Collection(COL_LEVEL_PROGRESS).Document(levelId);
                var levelSnap   = await levelRef.GetSnapshotAsync();

                int prevBest     = 0;
                int prevAttempts = 0;
                if (levelSnap.Exists)
                {
                    var d = levelSnap.ToDictionary();
                    prevBest     = GetInt(d, "bestScore", 0);
                    prevAttempts = GetInt(d, "attempts", 0);
                }

                var levelUpdate = new Dictionary<string, object>
                {
                    { "progressId",  levelId },
                    { "uid",         uid },
                    { "gameMode",    gameMode },
                    { "grade",       grade },
                    { "levelNumber", levelNumber },
                    { "bestScore",   Mathf.Max(prevBest, score) },
                    { "attempts",    prevAttempts + 1 }
                };
                await levelRef.SetAsync(levelUpdate, SetOptions.MergeAll);

                // ── 3. playerData (totalScore, xp, coins) ────────────
                var playerRef  = firestore.Collection(COL_PLAYER_DATA).Document(uid);
                var playerSnap = await playerRef.GetSnapshotAsync();

                int prevScore = 0;
                int prevXp    = 0;
                int prevCoins = 0;
                if (playerSnap.Exists)
                {
                    var d = playerSnap.ToDictionary();
                    prevScore = GetInt(d, "totalScore", 0);
                    prevXp    = GetInt(d, "totalXp", 0);
                    prevCoins = GetInt(d, "coins", 0);
                }

                int newScore = prevScore + score;
                int newXp    = prevXp + (score / 10);
                int newLevel = 1 + (newXp / 100);
                int newCoins = prevCoins + coinsEarned;

                var playerUpdate = new Dictionary<string, object>
                {
                    { "totalScore",  newScore },
                    { "totalXp",     newXp },
                    { "level",       newLevel },
                    { "coins",       newCoins },
                    { "lastUpdated", now }
                };
                await playerRef.SetAsync(playerUpdate, SetOptions.MergeAll);

                Debug.Log($"[CloudSync] ✅ Synced: {gameMode} Lớp{grade} Màn{levelNumber} +{score}đ +{coinsEarned}xu");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Sync thất bại (sẽ thử lại lần sau): {ex.Message}");
            }
        }

        private async Task SyncCoinsAsync(string uid, int newTotal)
        {
            if (firestore == null) return;
            try
            {
                var update = new Dictionary<string, object> { { "coins", newTotal } };
                await firestore.Collection(COL_PLAYER_DATA).Document(uid)
                    .SetAsync(update, SetOptions.MergeAll);
                Debug.Log($"[CloudSync] ✅ Coins synced: {newTotal}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Coins sync thất bại: {ex.Message}");
            }
        }

        private async Task SyncScoreAsync(string uid, int newScore, int newLevel)
        {
            if (firestore == null) return;
            try
            {
                // Tính XP từ score (1 điểm = 0.1 XP, làm tròn)
                int newXp = newScore / 10;

                var update = new Dictionary<string, object>
                {
                    { "totalScore",  newScore },
                    { "totalXp",     newXp },
                    { "level",       newLevel },
                    { "lastUpdated", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };
                await firestore.Collection(COL_PLAYER_DATA).Document(uid)
                    .SetAsync(update, SetOptions.MergeAll);
                Debug.Log($"[CloudSync] ✅ Score synced: score={newScore}, level={newLevel}, xp={newXp}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Score sync thất bại: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE — restore từ Firebase về máy
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Restore grade từ Firestore users/{uid} về UIManager.SelectedGrade và PlayerPrefs.
        /// </summary>
        private async Task RestoreGrade(string uid)
        {
            if (firestore == null) return;
            try
            {
                var snap = await firestore.Collection("users").Document(uid).GetSnapshotAsync();
                if (!snap.Exists) return;

                int cloudGrade = GetInt(snap.ToDictionary(), "grade", 0);
                if (cloudGrade < 1 || cloudGrade > 5) return;

                // Áp dụng vào UIManager (dùng khắp game)
                UIManager.SelectedGrade = cloudGrade;

                // Lưu vào PlayerPrefs để UIStartupController restore khi mở lại app
                DoAnGame.UI.UIQuickPlayNameController.SaveSelectedGrade(cloudGrade);

                Debug.Log($"[CloudSync] ✅ Restored grade: Lớp {cloudGrade}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Restore grade thất bại: {ex.Message}");
            }
        }

        private async Task RestoreModeProgress(string uid, string gameMode, int grade, string localKey)
        {
            if (firestore == null) return;
            try
            {
                string progressId = $"{uid}_{gameMode}_{grade}";
                var snap = await firestore.Collection(COL_GAME_PROGRESS).Document(progressId).GetSnapshotAsync();
                if (!snap.Exists) return;

                int cloudMax = GetInt(snap.ToDictionary(), "maxLevelUnlocked", 1);
                int localMax = PlayerPrefs.GetInt(localKey, 1);

                // Cloud là nguồn chính. Chỉ giữ local nếu cloud = 1 (chưa sync)
                int finalMax = (cloudMax > 1) ? cloudMax : Mathf.Max(cloudMax, localMax);
                PlayerPrefs.SetInt(localKey, finalMax);
                PlayerPrefs.Save();

                Debug.Log($"[CloudSync] Restored {gameMode} Lớp{grade}: maxLevel={finalMax} (cloud={cloudMax}, local={localMax})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Restore {gameMode} thất bại: {ex.Message}");
            }
        }

        private async Task RestoreCoins(string uid)
        {
            if (firestore == null) return;
            try
            {
                var snap = await firestore.Collection(COL_PLAYER_DATA).Document(uid).GetSnapshotAsync();
                if (!snap.Exists) return;

                int cloudCoins = GetInt(snap.ToDictionary(), "coins", 0);

                // Cloud là nguồn chính xác nhất (đã sync từ tất cả device)
                // Chỉ dùng local nếu cloud = 0 và local > 0 (lần đầu đăng nhập chưa sync)
                int localCoins = PlayerPrefs.GetInt(KEY_COINS, 0);
                int finalCoins = (cloudCoins > 0) ? cloudCoins : localCoins;

                PlayerPrefs.SetInt(KEY_COINS, finalCoins);
                PlayerPrefs.Save();

                Debug.Log($"[CloudSync] Restored coins: {finalCoins} (cloud={cloudCoins}, local={localCoins})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Restore coins thất bại: {ex.Message}");
            }
        }

        private async Task RestoreScoreAndLevel(string uid)
        {
            if (firestore == null) return;
            try
            {
                var snap = await firestore.Collection(COL_PLAYER_DATA).Document(uid).GetSnapshotAsync();
                if (!snap.Exists) return;

                var d = snap.ToDictionary();
                int cloudScore = GetInt(d, "totalScore", 0);
                int cloudXp    = GetInt(d, "totalXp", 0);
                int cloudLevel = GetInt(d, "level", 1);

                // Nếu XP = 0 nhưng score > 0 (sửa thủ công hoặc dữ liệu cũ)
                // → tính lại XP và level từ score
                if (cloudXp == 0 && cloudScore > 0)
                {
                    cloudXp    = cloudScore / 10;
                    cloudLevel = 1 + (cloudXp / 100);
                    Debug.Log($"[CloudSync] Tính lại XP/Level từ score: score={cloudScore} → xp={cloudXp}, level={cloudLevel}");

                    // Cập nhật lại Firebase cho đúng
                    var fix = new Dictionary<string, object>
                    {
                        { "totalXp", cloudXp },
                        { "level",   cloudLevel }
                    };
                    await firestore.Collection(COL_PLAYER_DATA).Document(uid)
                        .SetAsync(fix, SetOptions.MergeAll);
                }

                int localScore = PlayerPrefs.GetInt(KEY_SCORE, 0);
                int localLevel = PlayerPrefs.GetInt(KEY_LEVEL, 1);

                // Cloud là nguồn chính xác nhất.
                // Chỉ dùng local nếu cloud = 0 (lần đầu đăng nhập chưa sync)
                int finalScore = (cloudScore > 0) ? cloudScore : localScore;
                int finalLevel = (cloudLevel > 1) ? cloudLevel : Mathf.Max(cloudLevel, localLevel);

                PlayerPrefs.SetInt(KEY_SCORE, finalScore);
                PlayerPrefs.SetInt(KEY_LEVEL, finalLevel);
                PlayerPrefs.Save();

                Debug.Log($"[CloudSync] Restored score={finalScore} level={finalLevel} (cloud: {cloudScore}/{cloudLevel}, local: {localScore}/{localLevel})");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Restore score/level thất bại: {ex.Message}");
            }
        }

        private async Task SyncShopAsync(string uid, string shopType, int selectedId, int[] unlockedIds)
        {
            if (firestore == null) return;
            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "selectedId",  selectedId },
                    { "unlockedIds", string.Join(",", unlockedIds) } // lưu dạng "0,1,2"
                };

                await firestore.Collection("playerShop")
                    .Document($"{uid}_{shopType}")
                    .SetAsync(payload, SetOptions.MergeAll);

                Debug.Log($"[CloudSync] ✅ Shop synced: {shopType} selected={selectedId} unlocked=[{string.Join(",", unlockedIds)}]");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Shop sync thất bại ({shopType}): {ex.Message}");
            }
        }

        /// <summary>
        /// Restore trạng thái shop từ Firebase về PlayerPrefs.
        /// </summary>
        /// <param name="unlockedKeyPrefix">Prefix của key unlock, ví dụ "ClassSkinUnlocked" hoặc "SkinUnlocked_"</param>
        /// <param name="selectedKey">Key lưu ID đang trang bị, ví dụ "SelectedClassSkinID"</param>
        private async Task RestoreShop(string uid, string shopType, string unlockedKeyPrefix, string selectedKey)
        {
            if (firestore == null) return;
            try
            {
                var snap = await firestore.Collection("playerShop")
                    .Document($"{uid}_{shopType}")
                    .GetSnapshotAsync();

                if (!snap.Exists) return;

                var d = snap.ToDictionary();

                // Restore selectedId — lấy giá trị cloud (cloud là nguồn chính xác nhất)
                int cloudSelected = GetInt(d, "selectedId", 0);
                PlayerPrefs.SetInt(selectedKey, cloudSelected);

                // Restore unlockedIds — merge cloud + local (union: đã mua ở đâu thì giữ)
                string cloudUnlockedStr = d.ContainsKey("unlockedIds") ? d["unlockedIds"]?.ToString() ?? "" : "";
                var cloudUnlocked = ParseIntArray(cloudUnlockedStr);

                foreach (int id in cloudUnlocked)
                {
                    if (id == 0) continue; // ID 0 luôn mở, không cần lưu
                    string key = unlockedKeyPrefix + id;
                    // Chỉ set nếu chưa có (không ghi đè local đã mua)
                    if (PlayerPrefs.GetInt(key, 0) == 0)
                    {
                        PlayerPrefs.SetInt(key, 1);
                    }
                }

                PlayerPrefs.Save();
                Debug.Log($"[CloudSync] Restored shop {shopType}: selected={cloudSelected}, unlocked=[{cloudUnlockedStr}]");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CloudSync] ⚠️ Restore shop {shopType} thất bại: {ex.Message}");
            }
        }

        private static int[] ParseIntArray(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new int[0];
            var parts = csv.Split(',');
            var result = new System.Collections.Generic.List<int>();
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int v))
                    result.Add(v);
            }
            return result.ToArray();
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE — helpers
        // ─────────────────────────────────────────────────────────────

        private void UpdateLocalProgress(string gameMode, int levelNumber)
        {
            string key = gameMode switch
            {
                "chonda"    => KEY_CHONDA,
                "keothada"  => KEY_KEOTHADA,
                "phithuyen" => KEY_PHITHUYEN,
                _           => null
            };

            if (key == null) return;

            int current = PlayerPrefs.GetInt(key, 1);
            if (levelNumber >= current)
            {
                PlayerPrefs.SetInt(key, levelNumber + 1);
                PlayerPrefs.Save();
            }
        }

        private string GetCurrentUid()
        {
            // Ưu tiên AuthManager
            var authManager = AuthManager.Instance;
            if (authManager != null)
            {
                var user = authManager.GetCurrentUser();
                if (user != null && !string.IsNullOrEmpty(user.UserId))
                    return user.UserId;
            }

            // Fallback: PlayerPrefs
            string uid = PlayerPrefs.GetString(LocalStorageKeyResolver.Key("uid"), null);
            if (!string.IsNullOrEmpty(uid)) return uid;

            return PlayerPrefs.GetString("uid", null);
        }

        private static int GetInt(Dictionary<string, object> map, string key, int fallback)
        {
            if (map != null && map.TryGetValue(key, out object value) && value != null)
            {
                try { return System.Convert.ToInt32(value); } catch { }
            }
            return fallback;
        }
    }
}
