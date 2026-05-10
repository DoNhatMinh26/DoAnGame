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

## 8. Phần chưa làm — PSB nào hiển thị theo sự kiện

*(Tính sau)*

| Sự kiện | PSB hiển thị | PSB ẩn |
|---|---|---|
| Idle (đang chờ) | Character Meo | Meo_Sad, MeoGoc34 |
| Happy (đúng) | Character Meo | Meo_Sad, MeoGoc34 |
| Sad (sai) | Character Meo_Sad | Meo, MeoGoc34 |
| Attack | MeoGoc34 Fix | Meo, Meo_Sad |
