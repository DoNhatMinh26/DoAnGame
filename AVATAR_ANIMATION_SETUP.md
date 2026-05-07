# Hướng dẫn Setup Avatar Animation System

## Tổng quan

Bạn đã có sẵn:
- `Assets/Animation/Character/Idle.anim`
- `Assets/Animation/Character/Happy.anim`
- `Assets/Animation/Character/Sad.anim`

Cần tạo **1 AnimatorController cho mỗi avatar** dùng chung 3 clip này, rồi gán vào `AvatarData` và setup GameObject trong scene.

---

## Phần 1: Tạo AnimatorController

> Mỗi avatar cần 1 controller riêng. Nếu bạn có 4 avatar thì tạo 4 controller.
> Tất cả dùng chung 3 animation clip trên — chỉ khác nhau ở sprite/skin nếu có.

### Bước 1.1 — Tạo controller

1. Vào `Assets/Animation/Character/`
2. Right-click → **Create → Animator Controller**
3. Đặt tên: `Avatar_0.controller` (làm tương tự cho Avatar_1, Avatar_2, Avatar_3)

### Bước 1.2 — Mở Animator window

1. Double-click vào `Avatar_0.controller` để mở **Animator window**
2. Hoặc vào menu **Window → Animation → Animator**

### Bước 1.3 — Thêm Parameters

Trong Animator window, click tab **Parameters** (góc trái) → nhấn dấu **+** → chọn **Trigger**:

Tạo đúng 3 trigger với tên chính xác (phân biệt hoa thường):

| Tên | Loại |
|---|---|
| `TriggerIdle` | Trigger |
| `TriggerHappy` | Trigger |
| `TriggerSad` | Trigger |

### Bước 1.4 — Tạo States

Trong Animator window (vùng graph):

1. **Kéo** `Idle.anim` từ Project window vào Animator graph → Unity tự tạo state `Idle`
2. **Kéo** `Happy.anim` vào → tạo state `Happy`
3. **Kéo** `Sad.anim` vào → tạo state `Sad`
4. Right-click state `Idle` → **Set as Layer Default State** → state này chuyển màu cam (default)

Kết quả:
```
[Entry] → [Idle] (cam — default)
          [Happy]
          [Sad]
[Any State]
```

### Bước 1.5 — Tạo Transitions từ Any State

Right-click vào **Any State** → **Make Transition** → kéo đến `Idle`:

**Transition: Any State → Idle**
- Click vào mũi tên transition vừa tạo
- Trong Inspector:
  - `Has Exit Time`: **bỏ tick** (unchecked)
  - `Transition Duration`: `0.1`
  - Phần **Conditions**: nhấn **+** → chọn `TriggerIdle`

Lặp lại tương tự:

**Transition: Any State → Happy**
- `Has Exit Time`: bỏ tick
- `Transition Duration`: `0.1`
- Condition: `TriggerHappy`

**Transition: Any State → Sad**
- `Has Exit Time`: bỏ tick
- `Transition Duration`: `0.1`
- Condition: `TriggerSad`

### Bước 1.6 — Kiểm tra

Animator graph phải trông như sau:
```
[Any State] ──TriggerIdle──→  [Idle] ← default (cam)
[Any State] ──TriggerHappy──→ [Happy]
[Any State] ──TriggerSad──→   [Sad]
```

### Bước 1.7 — Lặp lại cho các avatar còn lại

Duplicate `Avatar_0.controller` (Ctrl+D) → đổi tên thành `Avatar_1.controller`, `Avatar_2.controller`, `Avatar_3.controller`.

> Vì tất cả dùng chung 3 clip, chỉ cần duplicate — không cần tạo lại từ đầu.

---

## Phần 2: Gán vào AvatarData

1. Mở từng asset trong `Assets/Resources/Avatars/`
2. Gán field `Animator Controller`:
   - `Avatar_0_...` → `Avatar_0.controller`
   - `Avatar_1_...` → `Avatar_1.controller`
   - v.v.

---

## Phần 3: Setup trong scene Test_FireBase_multi

### 3.1 — Tạo Player1Character trong GameplayPanel

1. Mở scene `Test_FireBase_multi`
2. Tìm `Canvas/GameplayPanel` trong Hierarchy
3. Right-click `GameplayPanel` → **Create Empty** → đặt tên `Player1Character`
4. Trên `Player1Character`, thêm components:
   - **Add Component → Animator**
   - **Add Component → Avatar Character Display** (script của bạn)
5. Trong Inspector của `AvatarCharacterDisplay`:
   - `Character Animator` → kéo component **Animator** trên cùng GameObject vào
   - `Character Sprite` → để trống (nếu không dùng SpriteRenderer)
6. Đặt vị trí phù hợp trên màn hình (bên trái — Player 1)

### 3.2 — Tạo Player2Character

1. Duplicate `Player1Character` (Ctrl+D) → đổi tên thành `Player2Character`
2. Đặt vị trí bên phải — Player 2
3. References trong `AvatarCharacterDisplay` đã tự copy — kiểm tra lại cho chắc

### 3.3 — Gán vào UIMultiplayerBattleController

1. Chọn `GameplayPanel` → component **UIMultiplayerBattleController**
2. Gán:
   - `Player1 Character` → `Player1Character`
   - `Player2 Character` → `Player2Character`

---

## Phần 4: Setup trong Wins panel

### 4.1 — Tạo WinnerCharacter

1. Tìm `Canvas/Wins` trong Hierarchy
2. Tìm (hoặc tạo) GameObject `Win_image_animation` — đây là chỗ hiển thị nhân vật người thắng
3. Thêm components lên `Win_image_animation`:
   - **Animator**
   - **AvatarCharacterDisplay**
4. Gán `Character Animator` trong Inspector

### 4.2 — Tạo LoserCharacter

1. Tìm (hoặc tạo) `Lost_image_animation` trong `Wins`
2. Thêm **Animator** + **AvatarCharacterDisplay**
3. Gán references

### 4.3 — Gán vào UIWinsController

1. Chọn `Wins` → component **UIWinsController**
2. Gán:
   - `Winner Character` → `Win_image_animation`
   - `Loser Character` → `Lost_image_animation`

---

## Phần 5: Kiểm tra hoạt động

### Test nhanh trong Play Mode

1. Chạy scene `Test_FireBase_multi`
2. Vào Profile → chọn avatar khác nhau cho 2 máy (dùng ParrelSync)
3. Vào battle → quan sát:
   - Khi câu hỏi xuất hiện → cả 2 nhân vật chạy **Idle**
   - Khi có người trả lời đúng → người thắng **Happy**, người thua **Sad**
   - Khi câu hỏi mới → reset về **Idle**
4. Khi trận kết thúc → Wins panel:
   - Người thắng → **Happy**
   - Người thua → **Sad**

### Kiểm tra Console

Tìm các log sau để xác nhận hoạt động đúng:
```
[AvatarManager] ✅ Loaded X avatars. Current: ...
[PlayerState] Owner set AvatarId=X
[BattleController] ✅ Avatar characters initialized: P1=X, P2=X
[AvatarCharacterDisplay] ✅ Set avatar: ... trên Player1Character
```

---

## Tóm tắt cấu trúc file sau khi hoàn thành

```
Assets/
├── Animation/
│   └── Character/
│       ├── Idle.anim          ← dùng chung cho tất cả avatar
│       ├── Happy.anim         ← dùng chung
│       ├── Sad.anim           ← dùng chung
│       ├── Avatar_0.controller  ← controller cho avatar 0
│       ├── Avatar_1.controller  ← controller cho avatar 1
│       ├── Avatar_2.controller  ← controller cho avatar 2
│       └── Avatar_3.controller  ← controller cho avatar 3
├── Resources/
│   └── Avatars/
│       ├── Avatar_0_xxx.asset   ← animatorController = Avatar_0.controller
│       ├── Avatar_1_xxx.asset   ← animatorController = Avatar_1.controller
│       ├── Avatar_2_xxx.asset
│       └── Avatar_3_xxx.asset
└── Script/
    └── Avatar/
        ├── AvatarData.cs
        ├── AvatarManager.cs
        ├── AvatarItemUI.cs
        └── AvatarCharacterDisplay.cs  ← gán lên Player1/2Character và Win/Lost
```

---

## Lưu ý quan trọng

- **Tên parameter phải chính xác:** `TriggerIdle`, `TriggerHappy`, `TriggerSad` — phân biệt hoa thường
- **Has Exit Time phải bỏ tick** trên tất cả transitions — nếu không animation sẽ không chuyển ngay lập tức
- **Any State → mỗi state** (không phải Idle → Happy → Sad) — để có thể trigger từ bất kỳ trạng thái nào
- Nếu `AvatarManager` chưa load xong khi `OnNetworkSpawn()` chạy → `AvatarId.Value` sẽ là 0 (default) — `OnValueChanged` sẽ tự cập nhật sau khi sync
