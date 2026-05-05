---
inclusion: manual
---

# Avatar System — Kế hoạch tương lai

## Mục tiêu
Người chơi chọn avatar ở Profile → avatar liên kết với 1 model nhân vật 2D riêng biệt → hiển thị trong `Test_FireBase_multi` (LobbyPanel, GameplayPanel).

---

## Thiết kế dự kiến

### Avatar ScriptableObject
```csharp
[CreateAssetMenu(menuName = "Game/AvatarData")]
public class AvatarData : ScriptableObject
{
    public int avatarId;
    public string avatarName;
    public Sprite thumbnail;          // Ảnh nhỏ hiển thị ở Profile/Shop
    public RuntimeAnimatorController animatorController; // Chứa idle, happy, sad, jump
    public int price;                 // 0 = miễn phí
}
```

### Animations cần có cho mỗi avatar
| State | Trigger | Dùng khi |
|---|---|---|
| `idle` | (default) | Đứng chờ trong lobby |
| `happy` | `TriggerHappy` | Trả lời đúng |
| `sad` | `TriggerSad` | Trả lời sai / thua |
| `jump` | `TriggerJump` | Thắng trận |

### Firestore — thêm vào `users/{uid}`
```
avatarId: int  (ID avatar đã chọn, default = 0)
```

### PlayerPrefs key
```
SelectedAvatarID  (int, sync với Firebase)
```

---

## Luồng hoạt động

```
Profile Panel
    ↓ Người chơi chọn avatar thumbnail
    ↓ Lưu PlayerPrefs["SelectedAvatarID"] = avatarId
    ↓ CloudSyncService.OnShopPurchased("avatar", avatarId, unlockedIds[])
    ↓ Firestore users/{uid}.avatarId = avatarId

Test_FireBase_multi (LobbyPanel)
    ↓ Khi vào phòng, đọc avatarId từ PlayerPrefs hoặc Firebase
    ↓ Load AvatarData ScriptableObject tương ứng
    ↓ Gán animatorController vào Animator của model nhân vật
    ↓ Hiển thị Player1 / Player2 với model riêng

GameplayPanel
    ↓ Khi trả lời đúng → animator.SetTrigger("TriggerHappy")
    ↓ Khi trả lời sai  → animator.SetTrigger("TriggerSad")
    ↓ Khi thắng trận   → animator.SetTrigger("TriggerJump")
```

---

## Files cần tạo/sửa khi implement

| File | Việc cần làm |
|---|---|
| `AvatarData.cs` | Tạo mới ScriptableObject |
| `AvatarManager.cs` | Singleton quản lý avatar hiện tại, load/save |
| `UIProfilePanelController.cs` | Thêm avatar selection UI |
| `UIMultiplayerRoomController.cs` | Đọc avatarId, gán model cho Player1/Player2 |
| `UIMultiplayerBattleController.cs` | Trigger animation theo kết quả đáp án |
| `NetworkedPlayerState.cs` | Thêm `NetworkVariable<int> AvatarId` để sync qua NGO |
| `CloudSyncService.cs` | Thêm sync/restore avatarId |
| `FirebaseManager.cs` | Lưu `avatarId` vào `users/{uid}` khi đăng ký/cập nhật |

---

## Lưu ý kỹ thuật

- `AvatarId` cần sync qua **NGO NetworkVariable** để cả Host và Client thấy avatar của nhau đúng.
- Model nhân vật 2D nên dùng **Spine** hoặc **Unity 2D Animation** (PSD Importer).
- Animator Controller mỗi avatar có cùng parameter names (`TriggerHappy`, `TriggerSad`, `TriggerJump`) để code dùng chung.
- Avatar slot trong GameplayPanel: `Player1` và `Player2` GameObject (đã có trong scene export) — thêm `Animator` + `SpriteRenderer` vào đó.
- Khi multiplayer: Host gửi `AvatarId` của mình qua `NetworkedPlayerState`, Client cũng gửi `AvatarId` của mình.
