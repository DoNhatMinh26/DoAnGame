# Avatar Animation System — Spec & Hướng dẫn Setup

## Tổng quan

Mỗi avatar liên kết với 1 `AnimatorController` riêng. Khi vào multiplayer battle, nhân vật của mỗi người chơi được hiển thị đúng theo avatar đã chọn và chạy animation theo sự kiện game:

| Sự kiện | Animation |
|---|---|
| Đang trong thời gian trả lời câu hỏi | `Idle` |
| Giai đoạn thống kê (summary) — thắng câu | `Happy` |
| Giai đoạn thống kê (summary) — thua câu | `Sad` |
| Wins panel — người thắng | `Happy` |
| Wins panel — người thua | `Sad` |

---

## 1. Cấu trúc AnimatorController

### Mỗi avatar cần 1 AnimatorController với cấu trúc giống nhau

**Parameters (bắt buộc dùng đúng tên):**

| Parameter | Type | Mô tả |
|---|---|---|
| `TriggerIdle` | Trigger | Chuyển về trạng thái đứng yên |
| `TriggerHappy` | Trigger | Chuyển sang animation vui |
| `TriggerSad` | Trigger | Chuyển sang animation buồn |

**States:**

```
[Any State]
    ↓ TriggerIdle
  Idle (default)
    ↓ TriggerHappy
  Happy
    ↓ TriggerSad
  Sad
```

**Transitions:**
- `Any State → Idle` : condition = `TriggerIdle`
- `Any State → Happy` : condition = `TriggerHappy`
- `Any State → Sad` : condition = `TriggerSad`
- Tất cả transitions: `Has Exit Time = false`, `Transition Duration = 0.1`

> **Quan trọng:** Tất cả AnimatorController của các avatar phải dùng **cùng tên parameter** (`TriggerIdle`, `TriggerHappy`, `TriggerSad`) để code dùng chung được.

### Cách tạo AnimatorController trong Unity

1. `Assets/Animation/Character/` → right-click → **Create → Animator Controller**
2. Đặt tên theo avatar, ví dụ: `Avatar_0_MeoTrang.controller`
3. Mở Animator window → thêm 3 parameters: `TriggerIdle`, `TriggerHappy`, `TriggerSad` (đều là Trigger)
4. Tạo 3 states: `Idle` (default), `Happy`, `Sad`
5. Gán animation clip vào mỗi state (idle.anim, happy.anim, sad.anim)
6. Tạo transitions từ `Any State` đến mỗi state với condition tương ứng
7. Lặp lại cho mỗi avatar (mỗi avatar 1 controller riêng)

---

## 2. Gán AnimatorController vào AvatarData

Sau khi tạo xong AnimatorController, mở từng `AvatarData` asset trong `Assets/Resources/Avatars/`:
- Gán `animatorController` = controller tương ứng của avatar đó

---

## 3. Cấu trúc GameObject trong GameplayPanel

Trong scene `Test_FireBase_multi`, `GameplayPanel` cần có 2 GameObject nhân vật:

```
GameplayPanel
├── Player1Character          ← Nhân vật của Player 1 (Host)
│   ├── SpriteRenderer        ← Hiển thị sprite nhân vật
│   └── Animator              ← Chạy animation
├── Player2Character          ← Nhân vật của Player 2 (Client)
│   ├── SpriteRenderer
│   └── Animator
└── ... (các UI khác)
```

Và trong `Wins` panel:
```
Wins
├── Win_image_animation       ← Nhân vật người thắng
│   └── Animator
└── Lost_image_animation      ← Nhân vật người thua
    └── Animator
```

---

## 4. Script: AvatarCharacterDisplay.cs

Tạo file `Assets/Script/Avatar/AvatarCharacterDisplay.cs` — gán lên mỗi GameObject nhân vật:

```csharp
using UnityEngine;

/// <summary>
/// Gán lên Player1Character và Player2Character trong GameplayPanel.
/// Nhận avatarId → load AvatarData → set Animator + Sprite.
/// Expose các method TriggerIdle/Happy/Sad để BattleController gọi.
/// </summary>
public class AvatarCharacterDisplay : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private SpriteRenderer characterSprite; // optional

    private static readonly int HashIdle  = Animator.StringToHash("TriggerIdle");
    private static readonly int HashHappy = Animator.StringToHash("TriggerHappy");
    private static readonly int HashSad   = Animator.StringToHash("TriggerSad");

    /// <summary>
    /// Gọi khi biết avatarId của người chơi — load AvatarData và apply.
    /// </summary>
    public void SetAvatar(int avatarId)
    {
        if (AvatarManager.Instance == null)
        {
            Debug.LogWarning("[AvatarCharacterDisplay] AvatarManager chưa sẵn sàng.");
            return;
        }

        AvatarData data = AvatarManager.Instance.GetById(avatarId);
        if (data == null)
        {
            Debug.LogWarning($"[AvatarCharacterDisplay] Không tìm thấy AvatarData id={avatarId}");
            return;
        }

        // Gán AnimatorController
        if (characterAnimator != null && data.animatorController != null)
        {
            characterAnimator.runtimeAnimatorController = data.animatorController;
            Debug.Log($"[AvatarCharacterDisplay] ✅ Set animator: {data.avatarName}");
        }

        // Gán sprite (nếu dùng SpriteRenderer thay vì Animator sprite)
        if (characterSprite != null && data.fullAvatar != null)
        {
            characterSprite.sprite = data.fullAvatar;
        }

        // Bắt đầu ở trạng thái Idle
        TriggerIdle();
    }

    public void TriggerIdle()
    {
        if (characterAnimator != null)
            characterAnimator.SetTrigger(HashIdle);
    }

    public void TriggerHappy()
    {
        if (characterAnimator != null)
            characterAnimator.SetTrigger(HashHappy);
    }

    public void TriggerSad()
    {
        if (characterAnimator != null)
            characterAnimator.SetTrigger(HashSad);
    }
}
```

---

## 5. Sync AvatarId qua NGO — NetworkedPlayerState

Thêm `NetworkVariable<int> AvatarId` vào `NetworkedPlayerState.cs`:

```csharp
// Thêm vào phần NetworkVariables
public NetworkVariable<int> AvatarId = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);
```

Trong `InitializeBattle()` (nơi client gửi tên), thêm gửi avatarId:

```csharp
// Sau dòng gửi tên
if (IsOwner)
{
    int myAvatarId = AvatarManager.Instance?.GetCurrentAvatarId() ?? 0;
    AvatarId.Value = myAvatarId;
}
```

---

## 6. Tích hợp vào UIMultiplayerBattleController

### 6a. Thêm fields

```csharp
[Header("Avatar Characters")]
[SerializeField] private AvatarCharacterDisplay player1Character; // Player1Character
[SerializeField] private AvatarCharacterDisplay player2Character; // Player2Character
```

### 6b. Khởi tạo avatar khi battle bắt đầu

Trong `HandlePanelActivated()` hoặc sau `InitializeBattle()`, thêm:

```csharp
private void InitAvatarCharacters()
{
    var p1 = battleManager?.GetPlayer1State();
    var p2 = battleManager?.GetPlayer2State();

    if (p1 != null && player1Character != null)
    {
        player1Character.SetAvatar(p1.AvatarId.Value);
        // Subscribe để update nếu AvatarId thay đổi sau khi spawn
        p1.AvatarId.OnValueChanged += (_, newId) => player1Character.SetAvatar(newId);
    }

    if (p2 != null && player2Character != null)
    {
        player2Character.SetAvatar(p2.AvatarId.Value);
        p2.AvatarId.OnValueChanged += (_, newId) => player2Character.SetAvatar(newId);
    }
}
```

### 6c. Trigger animation theo phase

**Trong `HandleAnswerResult()` — giai đoạn thống kê (summary):**

```csharp
// Thêm vào cuối HandleAnswerResult(), sau khi xác định isLocalWinner
var net = NetworkManager.Singleton;
bool isHost = net != null && net.IsHost;

// Player1 = Host, Player2 = Client
bool p1Won = (winnerId == 0);
bool p2Won = (winnerId == 1);
bool bothWrong = (winnerId == -1);

if (player1Character != null)
{
    if (bothWrong) player1Character.TriggerIdle();
    else if (p1Won) player1Character.TriggerHappy();
    else player1Character.TriggerSad();
}

if (player2Character != null)
{
    if (bothWrong) player2Character.TriggerIdle();
    else if (p2Won) player2Character.TriggerHappy();
    else player2Character.TriggerSad();
}
```

**Khi câu hỏi mới xuất hiện — reset về Idle:**

Trong `HandleQuestionGenerated()` hoặc `UpdateQuestionUI()`, thêm:

```csharp
player1Character?.TriggerIdle();
player2Character?.TriggerIdle();
```

---

## 7. Tích hợp vào UIWinsController

### 7a. Thêm fields

```csharp
[Header("Avatar Characters — Wins Panel")]
[SerializeField] private AvatarCharacterDisplay winnerCharacter;  // Win_image_animation
[SerializeField] private AvatarCharacterDisplay loserCharacter;   // Lost_image_animation
```

### 7b. Set avatar và trigger animation trong `DisplayMatchResult()`

Thêm vào cuối `DisplayMatchResult()`, sau khi xác định `isLocalWinner`:

```csharp
// Set avatar và animation cho Wins panel
SetWinsPanelAvatars(r);
```

Thêm method:

```csharp
private void SetWinsPanelAvatars(MatchResultData r)
{
    // Lấy avatarId từ NetworkedPlayerState (nếu còn sống)
    // hoặc từ AvatarManager của local player (fallback)
    int winnerId = r.WinnerId;
    int loserId  = (winnerId == 0) ? 1 : 0;

    int winnerAvatarId = 0;
    int loserAvatarId  = 0;

    var bm = NetworkedMathBattleManager.Instance;
    if (bm != null)
    {
        var p1 = bm.GetPlayer1State();
        var p2 = bm.GetPlayer2State();

        var winnerState = (winnerId == 0) ? p1 : p2;
        var loserState  = (winnerId == 0) ? p2 : p1;

        if (winnerState != null) winnerAvatarId = winnerState.AvatarId.Value;
        if (loserState  != null) loserAvatarId  = loserState.AvatarId.Value;
    }

    // Fallback: local player dùng AvatarManager
    bool isLocalWinner = (r.WinnerId == r.LocalPlayerId);
    int localAvatarId = AvatarManager.Instance?.GetCurrentAvatarId() ?? 0;

    if (isLocalWinner)
        winnerAvatarId = localAvatarId;
    else
        loserAvatarId = localAvatarId;

    // Apply
    if (winnerCharacter != null)
    {
        winnerCharacter.SetAvatar(winnerAvatarId);
        winnerCharacter.TriggerHappy();  // Người thắng → Happy
    }

    if (loserCharacter != null)
    {
        loserCharacter.SetAvatar(loserAvatarId);
        loserCharacter.TriggerSad();     // Người thua → Sad
    }
}
```

---

## 8. Setup trong Unity Inspector

### GameplayPanel

1. Tạo 2 Empty GameObject trong `GameplayPanel`:
   - `Player1Character` — thêm `Animator` + `AvatarCharacterDisplay`
   - `Player2Character` — thêm `Animator` + `AvatarCharacterDisplay`
2. Trong `AvatarCharacterDisplay` Inspector: gán `Character Animator` = Animator trên cùng GameObject
3. Chọn `GameplayPanel` → component `UIMultiplayerBattleController`:
   - Gán `Player1 Character` → `Player1Character`
   - Gán `Player2 Character` → `Player2Character`

### Wins Panel

1. Tìm `Wins/Win_image_animation` → thêm `Animator` + `AvatarCharacterDisplay`
2. Tìm `Wins/Lost_image_animation` → thêm `Animator` + `AvatarCharacterDisplay`
3. Chọn `Wins` → component `UIWinsController`:
   - Gán `Winner Character` → `Win_image_animation`
   - Gán `Loser Character` → `Lost_image_animation`

---

## 9. Flow đồng bộ hoàn chỉnh

```
Người chơi chọn avatar ở Profile
    ↓ AvatarManager.SelectAvatar(id) → lưu PlayerPrefs
    ↓ SyncToFirebaseAsync() → users/{uid}/avatarId

Vào multiplayer battle
    ↓ NetworkedPlayerState spawn
    ↓ IsOwner → AvatarId.Value = AvatarManager.GetCurrentAvatarId()
    ↓ NGO sync AvatarId cho cả Host và Client

GameplayPanel hiển thị
    ↓ InitAvatarCharacters()
    ↓ p1.AvatarId → Player1Character.SetAvatar() → load AnimatorController
    ↓ p2.AvatarId → Player2Character.SetAvatar() → load AnimatorController
    ↓ Cả 2 TriggerIdle()

Câu hỏi mới xuất hiện
    ↓ HandleQuestionGenerated()
    ↓ Player1Character.TriggerIdle()
    ↓ Player2Character.TriggerIdle()

Người chơi trả lời → HandleAnswerResult(winnerId)
    ↓ winnerId == 0 → Player1.TriggerHappy(), Player2.TriggerSad()
    ↓ winnerId == 1 → Player1.TriggerSad(), Player2.TriggerHappy()
    ↓ winnerId == -1 → cả 2 TriggerIdle()

Trận kết thúc → HandleMatchEnded() → NavigateToWinsPanel()
    ↓ UIWinsController.OnShow() → DisplayMatchResult()
    ↓ SetWinsPanelAvatars()
    ↓ winnerCharacter.SetAvatar(winnerAvatarId) → TriggerHappy()
    ↓ loserCharacter.SetAvatar(loserAvatarId)  → TriggerSad()
```

---

## 10. Checklist

- [ ] Mỗi avatar có 1 AnimatorController riêng với đúng 3 parameters: `TriggerIdle`, `TriggerHappy`, `TriggerSad`
- [ ] Mỗi AnimatorController có 3 states: `Idle` (default), `Happy`, `Sad`
- [ ] Transitions từ `Any State` với `Has Exit Time = false`
- [ ] `AvatarData.animatorController` được gán đúng cho từng avatar
- [ ] `NetworkedPlayerState` có `NetworkVariable<int> AvatarId`
- [ ] `AvatarId.Value` được set trong `InitializeBattle()` khi `IsOwner`
- [ ] `Player1Character` và `Player2Character` có `AvatarCharacterDisplay` + `Animator`
- [ ] `UIMultiplayerBattleController` gán đúng 2 character references
- [ ] `Win_image_animation` và `Lost_image_animation` có `AvatarCharacterDisplay` + `Animator`
- [ ] `UIWinsController` gán đúng 2 character references
- [ ] `HandleAnswerResult()` trigger đúng Happy/Sad/Idle theo winnerId
- [ ] `HandleQuestionGenerated()` reset về Idle khi câu hỏi mới
