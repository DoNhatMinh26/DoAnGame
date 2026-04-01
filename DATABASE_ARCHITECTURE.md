# 💾 DATABASE ARCHITECTURE: LOCAL + FIREBASE (2-TIER)

**Vấn đề**: Lưu dữ liệu ở đâu? Local? Firebase?  
**Giải pháp**: **Dùng cả 2** - Local cache + Firebase sync

---

## 📊 CẤU TRÚC 2-TIER

```
┌─────────────────────────────────────────────────┐
│              LOCAL DATABASE (SQLite)            │
│  Cache dữ liệu + Cài đặt offline + Lịch sử    │
└─────────────────────────────────────────────────┘
                       ↕ (Sync)
┌─────────────────────────────────────────────────┐
│        FIREBASE REALTIME DATABASE (Cloud)       │
│   Server chính + Cross-device + Backup         │
└─────────────────────────────────────────────────┘
```

---

## 🎯 KHI NÀO DÙNG CÁI NÀO?

| Dữ liệu | Local (SQLite) | Firebase Cloud | Chiến lược |
|---------|---|---|---|
| **Avatar URL** | Cache hình | Store + CDN | Save URL to Cloud, cache ảnh locally |
| **Player Score/XP** | Temp cache | ✅ **Primary** | Sync after each game |
| **Game Settings** | ✅ **Primary** | Optional backup | Save locally, auto-backup |
| **Leaderboard** | ❌ (Quá lớn) | ✅ **Primary** | Fetch from Cloud |
| **Game History** | Last 10 games | ✅ **Primary** | Local: recent, Cloud: all |
| **Offline Mode** | ✅ **Primary** | ❌ (No internet) | Play solo offline, sync when online |
| **User Login** | Session token | ✅ **Primary** | Firebase Auth, cache token locally |

---

## 1️⃣ LOCAL DATABASE STRATEGY (SQLite / PlayerPrefs)

### **Phương án A: PlayerPrefs (Đơn giản)**

```csharp
// Lưu player data locally
public static class LocalDataManager
{
    private const string PLAYER_DATA_KEY = "PlayerData";
    private const string LAST_SYNC_KEY = "LastSync";

    /// Lưu player data vào PlayerPrefs
    public static void SavePlayerDataLocal(PlayerData data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PLAYER_DATA_KEY, json);
        PlayerPrefs.SetString(LAST_SYNC_KEY, System.DateTime.Now.ToString("O"));
        PlayerPrefs.Save();
        
        Debug.Log("[Local] ✅ Lưu player data locally");
    }

    /// Load player data từ PlayerPrefs
    public static PlayerData LoadPlayerDataLocal()
    {
        if (!PlayerPrefs.HasKey(PLAYER_DATA_KEY))
            return null;

        string json = PlayerPrefs.GetString(PLAYER_DATA_KEY);
        try
        {
            return JsonUtility.FromJson<PlayerData>(json);
        }
        catch
        {
            Debug.LogWarning("[Local] ⚠️ Failed to parse player data");
            return null;
        }
    }

    /// Get last sync time
    public static System.DateTime? GetLastSyncTime()
    {
        if (PlayerPrefs.HasKey(LAST_SYNC_KEY))
        {
            if (System.DateTime.TryParseExact(
                PlayerPrefs.GetString(LAST_SYNC_KEY),
                "O",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var result))
            {
                return result;
            }
        }
        return null;
    }

    /// Check nếu data cũ (>1 giờ) - cần sync
    public static bool NeedsSyncFromCloud()
    {
        var lastSync = GetLastSyncTime();
        if (lastSync == null) return true;

        var timeSinceSync = System.DateTime.Now - lastSync.Value;
        return timeSinceSync.TotalHours > 1;
    }
}
```

**Ưu điểm**:
- ✅ Đơn giản, không cần thêm library
- ✅ Chạy nhanh, không delay
- ✅ Có thể chơi offline

**Nhược điểm**:
- ❌ Chỉ cho một app (không cross-device)
- ❌ Dễ bị **xóa nếu gỡ app**

---

### **Phương án B: SQLite Database (Mạnh hơn)**

```csharp
using System.Data;
using Mono.Data.Sqlite;

public class LocalDatabaseManager
{
    private const string DB_PATH = "file::memory:"; // Hoặc file path
    private IDbConnection dbConnection;

    public void Initialize()
    {
        dbConnection = new SqliteConnection(DB_PATH);
        dbConnection.Open();
        CreateTables();
        Debug.Log("[SQLite] ✅ Database initialized");
    }

    private void CreateTables()
    {
        string sql = @"
            CREATE TABLE IF NOT EXISTS PlayerData (
                id INTEGER PRIMARY KEY,
                uid TEXT UNIQUE,
                username TEXT,
                totalScore INTEGER,
                totalXp INTEGER,
                currentLevel INTEGER,
                lastUpdated INTEGER
            );
            
            CREATE TABLE IF NOT EXISTS GameHistory (
                id INTEGER PRIMARY KEY,
                gameId TEXT UNIQUE,
                opponentName TEXT,
                playerScore INTEGER,
                opponentScore INTEGER,
                result TEXT,
                timestamp INTEGER
            );
        ";

        using (IDbCommand cmd = dbConnection.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    /// Lưu player data
    public void SavePlayerData(PlayerData data)
    {
        using (IDbCommand cmd = dbConnection.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR REPLACE INTO PlayerData 
                (uid, username, totalScore, totalXp, currentLevel, lastUpdated) 
                VALUES (@uid, @username, @score, @xp, @level, @time)
            ";

            AddParameter(cmd, "@uid", data.uid);
            AddParameter(cmd, "@username", data.username);
            AddParameter(cmd, "@score", data.totalScore);
            AddParameter(cmd, "@xp", data.totalXp);
            AddParameter(cmd, "@level", data.currentLevel);
            AddParameter(cmd, "@time", System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            cmd.ExecuteNonQuery();
        }

        Debug.Log("[SQLite] ✅ Saved player data");
    }

    public PlayerData LoadPlayerData(string uid)
    {
        using (IDbCommand cmd = dbConnection.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM PlayerData WHERE uid = @uid";
            AddParameter(cmd, "@uid", uid);

            using (IDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new PlayerData
                    {
                        uid = reader["uid"].ToString(),
                        username = reader["username"].ToString(),
                        totalScore = (int)(long)reader["totalScore"],
                        totalXp = (int)(long)reader["totalXp"],
                        currentLevel = (int)(long)reader["currentLevel"]
                    };
                }
            }
        }

        return null;
    }

    private void AddParameter(IDbCommand cmd, string paramName, object value)
    {
        IDbDataParameter param = cmd.CreateParameter();
        param.ParameterName = paramName;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    public void Close()
    {
        dbConnection?.Close();
        Debug.Log("[SQLite] Database closed");
    }
}
```

**Ưu điểm**:
- ✅ Powerful SQL queries
- ✅ Structured dữ liệu
- ✅ Complex relationships

**Nhược điểm**:
- ❌ Cần cài Mono.Data.Sqlite package
- ❌ Chậm hơn PlayerPrefs

---

## 2️⃣ FIREBASE CLOUD STRATEGY

```csharp
public class CloudSyncManager : MonoBehaviour
{
    public static CloudSyncManager Instance { get; private set; }

    private LocalDatabaseManager localDb;
    private FirebaseManager firebaseManager;

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
        localDb = GetComponent<LocalDatabaseManager>();
        firebaseManager = FirebaseManager.Instance;
        
        // Auto-sync khi app start
        await SyncPlayerDataIfNeeded();
    }

    /// <summary>
    /// Smart sync: kiểm tra nó cần sync không trước
    /// </summary>
    public async Task SyncPlayerDataIfNeeded()
    {
        // Load local data
        var localData = LocalDataManager.LoadPlayerDataLocal();
        if (localData == null)
        {
            Debug.Log("[Sync] No local data");
            return;
        }

        // Check last sync time
        var lastSync = LocalDataManager.GetLastSyncTime();
        if (lastSync != null)
        {
            var timeSince = System.DateTime.Now - lastSync.Value;
            if (timeSince.TotalMinutes < 5) // Sync mỗi 5 phút
            {
                Debug.Log("[Sync] ⏭ Too soon, skipping sync");
                return;
            }
        }

        // Download từ Firebase
        var cloudData = await firebaseManager.LoadPlayerDataAsync(localData.uid);
        if (cloudData == null)
        {
            Debug.LogWarning("[Sync] ⚠️ No cloud data");
            return;
        }

        // So sánh: local vs cloud
        if (cloudData.totalScore > localData.totalScore)
        {
            Debug.Log("[Sync] Cloud data is newer, updating local");
            LocalDataManager.SavePlayerDataLocal(cloudData);
        }
        else
        {
            Debug.Log("[Sync] Local data is newer, uploading to cloud");
            await firebaseManager.SavePlayerToDatabase(localData);
        }

        Debug.Log("[Sync] ✅ Sync completed");
    }

    /// <summary>
    /// Save game session: local + cloud
    /// </summary>
    public async Task SaveGameSessionBoth(GameSession session)
    {
        // Save locally first (instant)
        localDb?.SaveGameHistory(session);

        // Save to cloud (async)
        await firebaseManager.SaveGameSessionAsync(session);
        
        // Update last sync time
        LocalDataManager.SavePlayerDataLocal(session);
    }

    /// <summary>
    /// Handle offline mode
    /// </summary>
    public bool IsOnline => Application.internetReachability != NetworkReachability.NotReachable;

    public void PlayOfflineMode()
    {
        // Load local data để chơi offline
        var localData = LocalDataManager.LoadPlayerDataLocal();
        if (localData != null)
        {
            Debug.Log($"[Offline] Playing as {localData.username}");
            // Load game with local data
        }
        else
        {
            Debug.LogWarning("[Offline] No local data available");
        }
    }
}
```

---

## 3️⃣ FLOW THỰC TIỄN

### **Scenario 1: Người chơi Online + Có Internet**

```
1. App launch
   ↓
2. Load local player data (nhanh)
   ↓
3. Check: LastSync > 5 phút?
   - Yes: Fetch từ Firebase
   - No: Skip sync
   ↓
4. Chơi game
   ↓
5. Kết thúc trận
   ↓
6. Save locally (instant)
   ↓
7. Async upload to Firebase
   ↓
8. Update LastSync time
```

### **Scenario 2: Người chơi Offline + Không có Internet**

```
1. App launch (offline)
   ↓
2. Load local player data
   ↓
3. Detect: No internet → PlayOfflineMode()
   ↓
4. Chơi game (solo only)
   ↓
5. Kết thúc trận
   ↓
6. Save locally only
   ↓
7. App queue sync job (background job)
   ↓
8. When internet returns → Auto sync to Firebase
```

### **Scenario 3: Cross-device Login**

```
User: điện thoại B
   ↓
1. Login qua Firebase Auth
   ↓
2. Check Firebase: có account không?
   - Yes: Download player data (score, level, etc)
   - No: Create new account
   ↓
3. Save to local database
   ↓
4. Set LastSync = now
   ↓
5. Người chơi có data từ điện thoại A!
```

---

## 4️⃣ MERGE STRATEGY (AI quan trọng!)

**Vấn đề**: 2 devices cùng sửa dữ liệu → Conflict!

```csharp
/// Merge local vs cloud data
public static PlayerData MergePlayerData(PlayerData local, PlayerData cloud)
{
    // Strategy 1: Always prefer higher score
    if (cloud.totalScore > local.totalScore)
    {
        return cloud;
    }

    // Strategy 2: Most recent wins
    if (cloud.lastUpdated > local.lastUpdated)
    {
        return cloud;
    }

    // Strategy 3: Combine (add XP, keep higher level)
    return new PlayerData
    {
        uid = local.uid,
        username = local.username,
        totalScore = Mathf.Max(local.totalScore, cloud.totalScore),
        totalXp = local.totalXp + cloud.totalXp, // Add both XP
        currentLevel = Mathf.Max(local.currentLevel, cloud.currentLevel),
        gamesPlayed = local.gamesPlayed + cloud.gamesPlayed,
        gamesWon = local.gamesWon + cloud.gamesWon,
        lastUpdated = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };
}
```

---

## 5️⃣ IMPLEMENTATION CHECKLIST

### **Phase 1: Local Database**
- [ ] Implement `LocalDataManager.cs` (PlayerPrefs version)
- [ ] Test: Save/Load player data
- [ ] Test: Cross-session persistence

### **Phase 2: Cloud Sync**
- [ ] Implement `CloudSyncManager.cs`
- [ ] Test: Sync when online
- [ ] Test: Queue sync when offline

### **Phase 3: Merge Logic**
- [ ] Handle conflict resolution
- [ ] Test: 2 devices editing same data
- [ ] Check: Higher score wins

### **Phase 4: Offline Mode**
- [ ] Detect internet connection
- [ ] Test: Play solo offline
- [ ] Test: Auto-sync when back online

### **Phase 5: Game Session Saving**
- [ ] Save to local instantly
- [ ] Upload to cloud async
- [ ] Handle upload failure (retry)

---

## 6️⃣ FILE STRUCTURE

```
Assets/Script/Script_multiplayer/
├── LocalDataManager.cs          (NEW) - PlayerPrefs caching
├── CloudSyncManager.cs          (NEW) - Sync logic
├── FirebaseManager.cs           (UPDATE) - Add sync hooks
├── AuthManager.cs               (UPDATE) - Load local on login
└── AI_Code/CODE/
    └── UIMultiplayerBattleController.cs (UPDATE) - Use CloudSyncManager
```

---

## 7️⃣ TÓMALREADY

✅ **Local Database (PlayerPrefs/SQLite)**:
- Nhanh, offline capable
- Cache dữ liệu hàng ngày
- Cài đặt game (âm thanh, ngôn ngữ)

✅ **Firebase Cloud**:
- Lưu trữ chính, server dữ liệu
- Cross-device sync
- Backup tự động
- Leaderboard

✅ **Sync Strategy**:
- Smart sync (check time)
- Offline queueing
- Conflict resolution
- 5-minute debounce

---

**Kết**: Bạn **sẽ không dùng file UnitOptions.db** đó (nó là Unity Visual Scripting metadata).  
Thay vào đó, dùng **PlayerPrefs (local) + Firebase (cloud)** = perfect! 🚀
