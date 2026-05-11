# Avatar Animation System — Spec & Hướng dẫn Setup

## Tổng quan

Nhân vật mèo gồm **3 PSB riêng biệt**. Mỗi PSB có **bộ xương riêng** và **AnimatorController riêng** — không thể dùng chung controller vì xương khác nhau hoàn toàn.

Bên trong mỗi PSB có **4 skin** dùng chung xương của PSB đó.

**Chọn avatar = bật đúng skin theo `avatarId` trên cả 3 PSB, tắt 3 skin còn lại.**

---

## 1. Cấu trúc PSB

### 3 PSB — bộ xương và controller riêng biệt

| PSB | Controller (đã có sẵn) | Animation | Dùng khi |
|---|---|---|---|
| `Character Meo.psb` | 1 controller (Idle + Happy) | Idle, Happy | Đang chờ, trả lời đúng |
| `Character Meo_Sad.psb` | 1 controller (Sad) | Sad | Trả lời sai |
| `MeoGoc34 Fix.psb` | 1 controller (Attack) | Attack (góc 3/4) | *(tính sau)* |

> Controller đã gán sẵn trong PSB khi import — **không cần swap từ code**.

### Skin bên trong mỗi PSB (dùng chung xương của PSB đó)

```
Character Meo          Character Meo_Sad      MeoGoc34 Fix
├── mascost1 (id=0)    ├── mascost1 (id=0)    ├── Meo1 (id=0)
├── mascost2 (id=1)    ├── mascost2 (id=1)    ├── Meo2 (id=1)
├── mascost3 (id=2)    ├── mascost3 (id=2)    ├── Meo3 (id=2)
├── mascost4 (id=3)    ├── mascost4 (id=3)    ├── Meo4 (id=3)
└── root (xương)       └── root (xương)       └── Root (xương)
```

---

## 2. AvatarData ScriptableObject

Mỗi avatar có 1 asset trong `Assets/Resources/Avatars/`. Chỉ lưu thông tin UI:

| Field | Mô tả |
|---|---|
| `avatarId` | 0, 1, 2, 3 |
| `avatarName` | Tên hiển thị |
| `thumbnail` | Ảnh nhỏ dùng trong danh sách chọn |
| `fullAvatar` | Ảnh lớn dùng ở Profile & MainMenu |

> **Không có `animatorController`** — 3 PSB có 3 bộ xương khác nhau, controller đã gán sẵn trong từng PSB, không cần lưu hay swap từ AvatarData.

---

## 3. Script: AvatarCharacterDisplay.cs

Gán lên 1 container GameObject chứa cả 3 PSB con.

**Inspector fields:**
- `Character Meo` → PSB GameObject (Idle/Happy)
- `Character Meo Sad` → PSB GameObject (Sad)
- `Meo Goc34 Fix` → PSB GameObject (Attack)

**API:**
```csharp
display.SetAvatar(int avatarId);   // Bật đúng skin, tắt các skin còn lại trên cả 3 PSB
display.GetCurrentAvatarId();
```

**Logic `SetAvatar()`:**
```
avatarId=0 → bật mascost1/Meo1,  tắt mascost2/3/4 và Meo2/3/4
avatarId=1 → bật mascost2/Meo2,  tắt còn lại
avatarId=2 → bật mascost3/Meo3,  tắt còn lại
avatarId=3 → bật mascost4/Meo4,  tắt còn lại
```

---

## 4. Cấu trúc GameObject trong scene

```
PlayerCharacter (AvatarCharacterDisplay)
├── Character Meo       ← PSB, Animator + controller riêng (Idle/Happy)
│   ├── mascost1–4      ← 4 skin, dùng chung xương PSB này
│   └── root
├── Character Meo_Sad   ← PSB, Animator + controller riêng (Sad)
│   ├── mascost1–4
│   └── root
└── MeoGoc34 Fix        ← PSB, Animator + controller riêng (Attack)
    ├── Meo1–4
    └── Root
```

---

## 5. Sync AvatarId qua NGO

`NetworkedPlayerState` có `NetworkVariable<int> AvatarId`:

```csharp
public NetworkVariable<int> AvatarId = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Owner
);
```

Set trong `InitializeBattle()` khi `IsOwner`:
```csharp
if (IsOwner)
    AvatarId.Value = AvatarManager.Instance?.GetCurrentAvatarId() ?? 0;
```

---

## 6. Tích hợp vào UIMultiplayerBattleController

```csharp
[Header("Avatar Characters")]
[SerializeField] private AvatarCharacterDisplay player1Character;
[SerializeField] private AvatarCharacterDisplay player2Character;

private void InitAvatarCharacters()
{
    var p1 = battleManager?.GetPlayer1State();
    var p2 = battleManager?.GetPlayer2State();

    if (p1 != null && player1Character != null)
    {
        player1Character.SetAvatar(p1.AvatarId.Value);
        p1.AvatarId.OnValueChanged += (_, newId) => player1Character.SetAvatar(newId);
    }

    if (p2 != null && player2Character != null)
    {
        player2Character.SetAvatar(p2.AvatarId.Value);
        p2.AvatarId.OnValueChanged += (_, newId) => player2Character.SetAvatar(newId);
    }
}
```

---

## 7. Checklist

- [ ] 4 `AvatarData` assets trong `Resources/Avatars/` (id 0–3), **không có** `animatorController`
- [ ] 3 PSB đã import đúng trong Unity, skin con đúng tên
- [ ] Container GameObject có `AvatarCharacterDisplay`, 3 PSB gán đúng trong Inspector
- [ ] `NetworkedPlayerState` có `NetworkVariable<int> AvatarId`
- [ ] `UIMultiplayerBattleController` gán `player1Character` và `player2Character`
- [ ] `SetAvatar()` hoạt động đúng: đúng skin bật, 3 skin còn lại tắt

---

## Logic Attack Animation (Chỉnh lại)

✅ **ĐÃ IMPLEMENT** — Logic hiển thị PSB theo timeline battle:

### Quy tắc mất máu:
- **CHỈ khi 1 đúng, 1 sai** → người sai mất máu
- **Cả 2 đúng** (kể cả hòa) → KHÔNG ai mất máu
- **Cả 2 sai** → KHÔNG ai mất máu (theo logic cũ)

### Timeline Battle (bám theo GameRulesConfig)

**1. Question Time** (thời gian: `gameRules.questionTimeLimit`, mặc định 10s)
- **Hiển thị:** `Character Meo.psb` (active)
- **Animation:** `Idle` (đứng chờ)
- **Ẩn:** `Character Meo_Sad.psb`, `MeoGoc34 Fix.psb` (inactive)
- **Trigger:** `HandleQuestionGenerated()` → `ShowIdle()`

**2. Summary Time** (thời gian: `gameRules.delayBetweenQuestions`, mặc định 3s)

Dựa trên `winnerId` và đáp án đúng/sai:

| Trường hợp | Người thắng | Người thua | Mất máu? |
|------------|-------------|------------|----------|
| **Cả 2 đúng, so sánh tốc độ** | ShowAttack() (tấn công) | ShowSad() (buồn vì chậm) | ❌ KHÔNG |
| **1 đúng, 1 sai** | ShowAttack() (tấn công) | ShowSad() (bị tấn công) | ✅ CÓ (người sai) |
| **Cả 2 sai** | ShowSad() | ShowSad() | ❌ KHÔNG |
| **Hòa (cả 2 đúng cùng lúc)** | ShowSad() | ShowSad() | ❌ KHÔNG |

**Chi tiết theo winnerId:**

| winnerId | Player 1 | Player 2 | Mô tả |
|----------|----------|----------|-------|
| `-1` | ShowSad() | ShowSad() | Cả 2 sai, KHÔNG mất máu |
| `-2` | ShowSad() | ShowSad() | Hòa (cả 2 đúng cùng lúc), KHÔNG mất máu |
| `0` (P1 thắng) | ShowAttack() | ShowSad() | P1 đúng/nhanh hơn. Mất máu nếu P2 sai |
| `1` (P2 thắng) | ShowSad() | ShowAttack() | P2 đúng/nhanh hơn. Mất máu nếu P1 sai |

**3. Sau Summary Time → Quay lại Question Time**
- Tự động quay về `ShowIdle()` khi câu hỏi mới được generate

### API Methods trong AvatarCharacterDisplay

```csharp
// Hiển thị Character Meo + trigger Idle (Question Time)
public void ShowIdle()

// Hiển thị Character Meo + trigger Happy (không dùng nữa - thay bằng Attack)
public void ShowHappy()

// Hiển thị Character Meo_Sad + trigger Sad (Summary Time - thua/buồn)
public void ShowSad()

// Hiển thị MeoGoc34 Fix + trigger Attack (Summary Time - thắng, tấn công)
// Animation này có hành động ném (sau này spawn projectile)
public void ShowAttack()
```

### Thời gian tuỳ chỉnh

Thời gian animation tự động theo `DefaultGameRules.asset`:
- **Question Time**: `questionTimeLimit` (10s mặc định)
- **Summary Time**: `delayBetweenQuestions` (3s mặc định)

Người dùng có thể tuỳ chỉnh trong Inspector của `DefaultGameRules.asset` → animation sẽ tự động theo.

### Ghi chú về Attack Animation

- **Attack animation** (MeoGoc34 Fix) có hành động **ném**
- Sau này có thể spawn **projectile** (quả bóng) khi animation ném
- Projectile sẽ bay về phía đối thủ và trigger hiệu ứng va chạm
- **Hiện tại**: Chỉ có animation, chưa có projectile và va chạm (làm sau)
