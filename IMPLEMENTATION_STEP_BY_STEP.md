# 🚀 IMPLEMENTATION GUIDE: FIREBASE PLAYER DATA SYNC

**Mục tiêu**: Thêm Avatar, HP, Score tracking vào UI 16 + Lưu toàn bộ dữ liệu trên Firebase

---

## PHASE 1: CẬP NHẬT FIREBASE MANAGER (Updated Methods)

### File: `Assets/Script/Script_multiplayer/FirebaseManager.cs`

**Thêm 5 methods mới:**

```csharp
/// <summary>
/// Lưu user profile (bao gồm avatar URL)
/// </summary>
public async Task SaveUserProfileAsync(string uid, UserData userData)
{
    try
    {
        string json = JsonUtility.ToJson(userData);
        await dbRef.Child("users").Child(uid).SetRawJsonValueAsync(json);
        Debug.Log("[Firebase] ✅ Lưu user profile (avatar) thành công");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi lưu user profile: {ex.Message}");
    }
}

/// <summary>
/// Tải user profile từ DB
/// </summary>
public async Task<UserData> LoadUserProfileAsync(string uid)
{
    try
    {
        var snapshot = await dbRef.Child("users").Child(uid).GetValueAsync();
        if (snapshot.Exists)
        {
            string json = snapshot.GetRawJsonValue();
            UserData data = JsonUtility.FromJson<UserData>(json);
            Debug.Log("[Firebase] ✅ Tải user profile thành công");
            return data;
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi tải user profile: {ex.Message}");
    }
    return null;
}

/// <summary>
/// Cập nhật avatar URL
/// </summary>
public async Task UpdateAvatarUrlAsync(string uid, string avatarUrl)
{
    try
    {
        await dbRef.Child("users").Child(uid).Child("avatarUrl").SetValueAsync(avatarUrl);
        Debug.Log("[Firebase] ✅ Cập nhật avatar URL thành công");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi cập nhật avatar: {ex.Message}");
    }
}

/// <summary>
/// Lưu game session (trận đấu)
/// </summary>
public async Task SaveGameSessionAsync(GameSession session)
{
    try
    {
        string json = JsonUtility.ToJson(session);
        
        // Lưu cho player 1
        await dbRef.Child("gameHistory")
            .Child(session.player1Id)
            .Child(session.sessionId)
            .SetRawJsonValueAsync(json);
        
        // Lưu cho player 2
        await dbRef.Child("gameHistory")
            .Child(session.player2Id)
            .Child(session.sessionId)
            .SetRawJsonValueAsync(json);
        
        Debug.Log("[Firebase] ✅ Lưu game session thành công");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi lưu game session: {ex.Message}");
    }
}

/// <summary>
/// Cập nhật HP và Score trong real-time
/// </summary>
public async Task UpdatePlayerMultiplierStatsAsync(string uid, int hpChange, int scoreChange)
{
    try
    {
        var player = await LoadPlayerDataAsync(uid);
        if (player == null) return;

        player.totalScore += scoreChange;
        // HP được quản lý riêng trong Netcode, chỉ cập nhật score DB
        
        await SavePlayerToDatabase(player);
        Debug.Log($"[Firebase] ✅ Cập nhật multiplayer stats: +{scoreChange} score");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi cập nhật stats: {ex.Message}");
    }
}
```

---

## PHASE 2: CẬP NHẬT AUTH MANAGER

### File: `Assets/Script/Script_multiplayer/AuthManager.cs`

**Thêm event + Constructor cập nhật dữ liệu:**

```csharp
public event Action<UserData> OnUserProfileLoaded;
public event Action<GameSession> OnGameSessionEnded;

/// <summary>
/// Đăng nhập - load toàn bộ profile
/// </summary>
public async Task<bool> Login(string email, string password)
{
    Debug.Log($"[Auth] 🔑 Đăng nhập: {email}");

    bool success = await firebaseManager.LoginAsync(email, password);
    
    if (success)
    {
        var user = firebaseManager.GetCurrentUser();
        
        // Tải USER PROFILE (bao gồm avatar, tên, tuổi)
        var userData = await firebaseManager.LoadUserProfileAsync(user.UserId);
        
        // Tải PLAYER DATA (score, level, rank)
        var playerData = await firebaseManager.LoadPlayerDataAsync(user.UserId);
        
        // Cache vào memory
        currentPlayerData = playerData;
        
        // Trigger event
        OnUserProfileLoaded?.Invoke(userData);
        OnPlayerDataLoaded?.Invoke(playerData);
        
        Debug.Log($"[Auth] ✅ Load profile: {userData?.username}, Level: {playerData?.currentLevel}");
    }

    return success;
}

/// <summary>
/// Khi trận đấu kết thúc, lưu vào Firebase
/// </summary>
public async Task SaveGameResultAsync(GameSession session)
{
    Debug.Log($"[Auth] 💾 Lưu trận đấu: {session.sessionId}");
    
    await firebaseManager.SaveGameSessionAsync(session);
    
    // Cập nhật stats người chơi
    if (session.winner == firebaseManager.GetCurrentUser().UserId)
    {
        currentPlayerData.gamesWon++;
        await firebaseManager.UpdatePlayerStatsAsync(
            firebaseManager.GetCurrentUser().UserId,
            (int)session.player1Score,  // Score
            50  // XP cho chiến thắng
        );
    }
    else
    {
        await firebaseManager.UpdatePlayerStatsAsync(
            firebaseManager.GetCurrentUser().UserId,
            0,  // Không có score
            10  // XP cho tham gia
        );
    }
    
    OnGameSessionEnded?.Invoke(session);
}
```

---

## PHASE 3: CẬP NHẬT UI 16 - AVATAR LOADER

### File: `Assets/Script/Script_multiplayer/AI_Code/CODE/AvatarLoader.cs` (NEW FILE)

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Async load avatar from URL hoặc placeholder
    /// </summary>
    public class AvatarLoader : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private Sprite placeholderAvatar;

        /// <summary>
        /// Load avatar từ URL (async)
        /// </summary>
        public void LoadAvatarAsync(string avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl))
            {
                avatarImage.sprite = placeholderAvatar;
                return;
            }

            StartCoroutine(LoadAvatarCoroutine(avatarUrl));
        }

        private IEnumerator LoadAvatarCoroutine(string url)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                var downloadHandler = new UnityEngine.Networking.DownloadHandlerTexture();
                request.downloadHandler = downloadHandler;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = downloadHandler.texture;
                    avatarImage.sprite = Sprite.Create(texture, 
                        new Rect(0, 0, texture.width, texture.height), 
                        new Vector2(0.5f, 0.5f));
                    
                    Debug.Log("[Avatar] ✅ Load avatar from URL thành công");
                }
                else
                {
                    Debug.LogWarning($"[Avatar] ⚠️ Lỗi load avatar: {request.error}");
                    avatarImage.sprite = placeholderAvatar;
                }
            }
        }
    }
}
```

---

## PHASE 4: CẬP NHẬT UI 16 - MULTIPLAYER BATTLE CONTROLLER

### File: `Assets/Script/Script_multiplayer/AI_Code/CODE/UIMultiplayerBattleController.cs` (EXTENDED)

**Thêm Avatar + HP/Score fields + load data:**

```csharp
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    public class UIMultiplayerBattleController : BasePanelController
    {
        [Header("Role Texts")]
        [SerializeField] private TMP_Text topPlayerText;
        [SerializeField] private TMP_Text bottomPlayerText;
        [SerializeField] private TMP_Text battleStatusText;

        [Header("Opponent Display (TOP)")]
        [SerializeField] private Image topPlayerAvatarImage;
        [SerializeField] private TMP_Text topPlayerNameText;
        [SerializeField] private Image topPlayerHpBar;
        [SerializeField] private TMP_Text topPlayerHpText;
        [SerializeField] private TMP_Text topPlayerScoreText;
        [SerializeField] private AvatarLoader topAvatarLoader;

        [Header("Question & Answers")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TMP_Text[] answerTexts = new TMP_Text[4];

        [Header("Local Player Display (BOTTOM)")]
        [SerializeField] private Image bottomPlayerAvatarImage;
        [SerializeField] private TMP_Text bottomPlayerNameText;
        [SerializeField] private Image bottomPlayerHpBar;
        [SerializeField] private TMP_Text bottomPlayerHpText;
        [SerializeField] private TMP_Text bottomPlayerScoreText;
        [SerializeField] private AvatarLoader bottomAvatarLoader;

        [Header("Fallback")]
        [SerializeField] private Sprite defaultAvatarSprite;
        [SerializeField] private string localPlayerLabel = "Player 1 - bạn";
        [SerializeField] private string enemyPlayerLabel = "Player 2 - đối thủ";
        [SerializeField] private string aiEnemyLabel = "Máy AI - đối thủ";

        // Runtime data
        private UserData localUserData;
        private UserData opponentUserData;
        private PlayerData localPlayerData;
        private PlayerData opponentPlayerData;
        private int localHp = 100;
        private int opponentHp = 100;

        protected override void OnShow()
        {
            base.OnShow();
            BindRolesAndLoadData();
        }

        private async void BindRolesAndLoadData()
        {
            var net = NetworkManager.Singleton;
            if (net == null || (!net.IsClient && !net.IsServer))
            {
                // Test offline mode
                SetOfflineUI();
                return;
            }

            // Load local player data
            var auth = AuthManager.Instance;
            if (auth != null)
            {
                var currentUser = FirebaseManager.Instance.GetCurrentUser();
                localUserData = await FirebaseManager.Instance.LoadUserProfileAsync(currentUser.UserId);
                localPlayerData = await FirebaseManager.Instance.LoadPlayerDataAsync(currentUser.UserId);
                
                // Display local player (bottom)
                DisplayLocalPlayer();
            }

            // Setup opponent
            int count = net.ConnectedClientsIds.Count;
            bool hasOpponent = count >= 2;

            if (net.IsHost)
            {
                SetBottom("Player 1 - chủ phòng");
            }
            else
            {
                SetBottom("Player 2 - bạn");
            }

            SetTop(hasOpponent ? "Player 2 - đối thủ" : aiEnemyLabel);
            battleStatusText?.SetText(hasOpponent ? "Đang đấu 1v1" : "Đang chờ đối thủ...");
        }

        private void DisplayLocalPlayer()
        {
            // Bottom player (local)
            bottomPlayerNameText?.SetText(localUserData?.username ?? "Player");
            bottomPlayerScoreText?.SetText($"Score: {localPlayerData?.totalScore ?? 0}");
            bottomPlayerHpText?.SetText($"{localHp}/100");
            
            // Load avatar
            if (!string.IsNullOrEmpty(localUserData?.avatarUrl))
            {
                bottomAvatarLoader?.LoadAvatarAsync(localUserData.avatarUrl);
            }
            else
            {
                bottomPlayerAvatarImage.sprite = defaultAvatarSprite;
            }

            // Update HP bar
            UpdateHpBar(bottomPlayerHpBar, localHp);
        }

        private void SetOfflineUI()
        {
            SetBottom(localPlayerLabel);
            SetTop(aiEnemyLabel);
            battleStatusText?.SetText("Chế độ test offline");
            
            // Display placeholder
            bottomPlayerNameText?.SetText("Player");
            bottomPlayerScoreText?.SetText("Score: 0");
            topPlayerNameText?.SetText("AI");
            topPlayerScoreText?.SetText("Score: 0");
            
            bottomPlayerAvatarImage.sprite = defaultAvatarSprite;
            topPlayerAvatarImage.sprite = defaultAvatarSprite;
        }

        /// <summary>
        /// Update HP bar khi người chơi bị tổn thương hoặc hồi phục
        /// </summary>
        public void UpdateLocalPlayerHp(int hpLoss)
        {
            localHp = Mathf.Max(0, localHp - hpLoss);
            bottomPlayerHpText?.SetText($"{localHp}/100");
            UpdateHpBar(bottomPlayerHpBar, localHp);
            
            if (localHp <= 0)
            {
                Debug.Log("[UI16] ❌ Local player defeated!");
                // Trigger game over
            }
        }

        /// <summary>
        /// Update score khi trả lời đúng
        /// </summary>
        public void UpdateLocalPlayerScore(int scoreAdd)
        {
            if (localPlayerData != null)
            {
                localPlayerData.totalScore += scoreAdd;
                bottomPlayerScoreText?.SetText($"Score: {localPlayerData.totalScore}");
            }
        }

        /// <summary>
        /// Cập nhật HP bar (Slider visual)
        /// </summary>
        private void UpdateHpBar(Image hpBar, int currentHp)
        {
            if (hpBar != null)
            {
                hpBar.fillAmount = currentHp / 100f;
                
                // Color change: green → yellow → red
                if (currentHp > 50)
                    hpBar.color = Color.green;
                else if (currentHp > 20)
                    hpBar.color = Color.yellow;
                else
                    hpBar.color = Color.red;
            }
        }

        private void SetTop(string text)
        {
            topPlayerText?.SetText(text);
        }

        private void SetBottom(string text)
        {
            bottomPlayerText?.SetText(text);
        }
    }
}
```

---

## PHASE 5: NETWORK SYNC - HP & SCORE TRACKING

### File: `Assets/Script/Script_multiplayer/AI_Code/CODE/MultiplayerGameManager.cs` (NEW FILE)

```csharp
using Unity.Netcode;
using UnityEngine;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Quản lý game state + HP/Score sync trong multiplayer
    /// </summary>
    public class MultiplayerGameManager : NetworkBehaviour
    {
        public static MultiplayerGameManager Instance { get; private set; }

        [SerializeField] private UIMultiplayerBattleController battleUI;

        // Network variables để sync HP/Score
        private NetworkVariable<int> localPlayerHp = new NetworkVariable<int>(100);
        private NetworkVariable<int> opponentPlayerHp = new NetworkVariable<int>(100);
        private NetworkVariable<int> localPlayerScore = new NetworkVariable<int>(0);
        private NetworkVariable<int> opponentPlayerScore = new NetworkVariable<int>(0);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Player trả lời sai - mất HP
        /// </summary>
        public void OnAnswerWrong()
        {
            // Local: mất 10 HP
            int newHp = Mathf.Max(0, localPlayerHp.Value - 10);
            UpdateLocalHpServerRpc(newHp);
        }

        /// <summary>
        /// Player trả lời đúng - được điểm
        /// </summary>
        public void OnAnswerCorrect(int scoreReward)
        {
            // Local: +50 điểm
            UpdateLocalScoreServerRpc(scoreReward);
            
            // Optional: +5 HP
            int newHp = Mathf.Min(100, localPlayerHp.Value + 5);
            UpdateLocalHpServerRpc(newHp);
        }

        [ServerRpc(RequireOwnershipCheck = false)]
        private void UpdateLocalHpServerRpc(int newHp)
        {
            if (IsHost)
                localPlayerHp.Value = newHp;
            else
                opponentPlayerHp.Value = newHp;
        }

        [ServerRpc(RequireOwnershipCheck = false)]
        private void UpdateLocalScoreServerRpc(int scoreToAdd)
        {
            if (IsHost)
                localPlayerScore.Value += scoreToAdd;
            else
                opponentPlayerScore.Value += scoreToAdd;
        }

        public override void OnNetworkSpawn()
        {
            // Subscribe to network variable changes
            localPlayerHp.OnValueChanged += (oldVal, newVal) =>
            {
                battleUI?.UpdateLocalPlayerHp(oldVal - newVal);
            };

            localPlayerScore.OnValueChanged += (oldVal, newVal) =>
            {
                battleUI?.UpdateLocalPlayerScore(newVal - oldVal);
            };
        }
    }
}
```

---

## PHASE 6: KẾT THÚC TRẬN - LƯU FIREBASE

### File: `Assets/Script/Script_multiplayer/AI_Code/CODE/GameResultManager.cs` (NEW FILE)

```csharp
using UnityEngine;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Xử lý kết thúc trận + lưu vào Firebase
    /// </summary>
    public class GameResultManager : MonoBehaviour
    {
        public static GameResultManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Trận đấu kết thúc - lưu vào Firebase
        /// </summary>
        public async void OnGameEnd(int player1Score, int player2Score, string winnerId)
        {
            var firebaseManager = FirebaseManager.Instance;
            var currentUser = firebaseManager.GetCurrentUser();

            var gameSession = new GameSession
            {
                sessionId = System.Guid.NewGuid().ToString(),
                player1Id = "player1_uid",  // TODO: Get from Netcode
                player1Name = "Player 1",
                player2Id = "player2_uid",  // TODO: Get from Netcode
                player2Name = "Player 2",
                player1Score = player1Score,
                player2Score = player2Score,
                player1Hp = 0,  // TODO: Get from Netcode
                player2Hp = 0,
                winner = winnerId,
                questionsCount = 10,  // TODO: Count từ trận
                startTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                endTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                difficulty = "NORMAL"
            };

            // Lưu vào Firebase
            await firebaseManager.SaveGameSessionAsync(gameSession);
            
            // Update player stats
            var authManager = AuthManager.Instance;
            if (authManager != null)
            {
                await authManager.SaveGameResultAsync(gameSession);
            }

            Debug.Log("[GameResult] ✅ Trận đấu đã lưu vào Firebase");
        }
    }
}
```

---

## PHASE 7: TEST CHECKLIST

- [x] Cập nhật FirebaseManager methods
- [x] Cập nhật AuthManager để load profile
- [x] Tạo AvatarLoader.cs
- [x] Cập nhật UIMultiplayerBattleController với Avatar/HP/Score
- [x] Tạo MultiplayerGameManager để network sync HP/Score
- [x] Tạo GameResultManager để lưu kết quả
- [ ] Assign tất cả serialized fields trong UI 16 Inspector
- [ ] Test: Đăng ký → Xem Firebase Console rằng UserData có avatar
- [ ] Test: Login → Verify avatar load từ URL
- [ ] Test: Chơi game → HP/Score update real-time
- [ ] Test: Kết thúc trận → Xem Firebase có game session mới
- [ ] Test: Logout rồi login → Verify data vẫn có (Score, Level, v.v.)
- [ ] Test: Login máy khác → Data vẫn xuất hiện

---

## 🎯 DỰ ÁN BẠN ĐANG CÓ:

```
✅ FirebaseManager.cs - Setup rồi
✅ AuthManager.cs - Setup rồi  
✅ UIMultiplayerRoomController.cs - Setup rồi
✅ UIMultiplayerBattleController.cs - Basic role binding (cần extended)

❌ AvatarLoader.cs - CẦN TẠO
❌ MultiplayerGameManager.cs - CẦN TẠO
❌ GameResultManager.cs - CẦN TẠO
❌ Serialized fields trong UI 16 - CẦN ASSIGN
```

---

## 💡 NEXT STEPS:

1. **Copy code từ các PHASE vào respective files**
2. **Create missing files** (AvatarLoader, MultiplayerGameManager, GameResultManager)
3. **Assign serialized fields** trong UI 16 script từ Inspector
4. **Test trên ParrelSync** (2 instances)
5. **Monitor Firebase Console** để xem data được lưu

Good luck! 🚀
