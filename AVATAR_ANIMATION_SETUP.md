# Hướng dẫn Setup Avatar Animation System

## Tổng quan

Nhân vật mèo gồm **3 PSB riêng biệt**, mỗi PSB có bộ xương riêng và AnimatorController riêng — không thể dùng chung controller vì xương khác nhau.

Bên trong mỗi PSB có 4 skin dùng chung xương của PSB đó.

| PSB | Controller | Animation |
|---|---|---|
| `Character Meo.psb` | `Character Meo.controller` | Idle + Happy (1 controller) |
| `Character Meo_Sad.psb` | `Character Meo_Sad.controller` | Sad |
| `MeoGoc34 Fix.psb` | `MeoGoc34 Fix.controller` | Attack (góc 3/4) |

---

## Phần 1: Setup AnimatorController

### 1.1 — Character Meo.controller (Idle + Happy)

File có sẵn tại: `Assets/Script/Script_multiplayer/1Code/Multiplay/Animation_Multiplayer/Character/Character Meo.controller`

1. Double-click `Character Meo.controller` → mở **Animator window**
2. Tab **Parameters** (góc trái) → nhấn **+** → **Trigger**, tạo 2 trigger:

| Tên | Loại |
|---|---|
| `TriggerIdle` | Trigger |
| `TriggerHappy` | Trigger |

3. Kéo `Idle.anim` từ Project vào Animator graph → right-click state `Idle` → **Set as Layer Default State** (chuyển màu cam)
4. Kéo `Happy.anim` vào graph

5. Tạo transitions từ **Any State**:

**Any State → Idle:**
- Right-click `Any State` → Make Transition → kéo đến `Idle`
- Inspector: `Has Exit Time` = **bỏ tick**, `Transition Duration` = `0.1`
- Conditions: nhấn **+** → chọn `TriggerIdle`

**Any State → Happy:**
- Right-click `Any State` → Make Transition → kéo đến `Happy`
- Inspector: `Has Exit Time` = **bỏ tick**, `Transition Duration` = `0.1`
- Conditions: nhấn **+** → chọn `TriggerHappy`

Kết quả graph:
```
[Any State] ──TriggerIdle──→  [Idle] ← default (cam)
[Any State] ──TriggerHappy──→ [Happy]
```

---

### 1.2 — Character Meo_Sad.controller (Sad)

File có sẵn tại: `Assets/Script/Script_multiplayer/1Code/Multiplay/Animation_Multiplayer/Character/Character Meo_Sad.controller`

1. Double-click `Character Meo_Sad.controller` → mở **Animator window**
2. Tab **Parameters** → nhấn **+** → **Trigger**:

| Tên | Loại |
|---|---|
| `TriggerSad` | Trigger |

3. Kéo `Sad.anim` vào graph → right-click → **Set as Layer Default State**
4. Tạo transition:

**Any State → Sad:**
- `Has Exit Time` = **bỏ tick**, `Transition Duration` = `0.1`
- Condition: `TriggerSad`

Kết quả graph:
```
[Any State] ──TriggerSad──→ [Sad] ← default (cam)
```

---

### 1.3 — MeoGoc34 Fix.controller (Attack)

File có sẵn tại: `Assets/Script/Script_multiplayer/1Code/Multiplay/Animation_Multiplayer/Character 3_4/MeoGoc34 Fix.controller`

*(Setup tương tự — tính sau khi implement phần Attack)*

---

## Phần 2: Gán controller vào PSB trong scene

Sau khi setup controller xong, gán vào Animator của từng PSB:

1. Mở scene `Test_FireBase_multi`
2. Tìm `Canvas/GameplayPanel/Player1Character`
3. Chọn child `Character Meo` → component **Animator** → kéo `Character Meo.controller` vào field **Controller**
4. Chọn child `Character Meo_Sad` → component **Animator** → kéo `Character Meo_Sad.controller` vào field **Controller**
5. Chọn child `MeoGoc34 Fix` → component **Animator** → kéo `MeoGoc34 Fix.controller` vào field **Controller**
6. Lặp lại cho `Player2Character`

---

## Phần 3: Kéo PSB vào scene

1. Mở scene `Test_FireBase_multi`
2. Tìm `Canvas/GameplayPanel`
3. Tạo Empty GameObject con của `GameplayPanel`, đặt tên `Player1Character`
4. Kéo 3 PSB từ `Assets/Script/Script_multiplayer/1Code/Multiplay/Animation_Multiplayer/` vào làm con của `Player1Character`:
   - `Character Meo.psb`
   - `Character Meo_Sad.psb`
   - `MeoGoc34 Fix.psb`
5. Đặt vị trí phù hợp (bên trái — Player 1)
6. Duplicate `Player1Character` → đổi tên `Player2Character`, đặt bên phải

---

## Phần 4: Gán AvatarCharacterDisplay

1. Chọn `Player1Character` → **Add Component → AvatarCharacterDisplay**
2. Trong Inspector gán:
   - `Character Meo` → child `Character Meo`
   - `Character Meo Sad` → child `Character Meo_Sad`
   - `Meo Goc34 Fix` → child `MeoGoc34 Fix`
3. Lặp lại cho `Player2Character`

---

## Phần 5: Kiểm tra tên skin con

Mở từng PSB trong Hierarchy và xác nhận tên con:

**Character Meo** và **Character Meo_Sad:**
```
mascost1, mascost2, mascost3, mascost4, root
```

**MeoGoc34 Fix:**
```
Meo1, Meo2, Meo3, Meo4, Root
```

> Tên phải khớp chính xác (phân biệt hoa thường) với `SkinNames` trong `AvatarCharacterDisplay.cs`.

---

## Phần 6: Gán vào UIMultiplayerBattleController

1. Chọn `GameplayPanel` → component **UIMultiplayerBattleController**
2. Gán:
   - `Player1 Character` → `Player1Character`
   - `Player2 Character` → `Player2Character`

---

## Phần 7: Setup Wins panel

1. Tìm `Canvas/Wins`
2. Tạo Empty GameObject `WinnerCharacter`, kéo 3 PSB vào làm con, gán controller như Phần 2
3. Tạo Empty GameObject `LoserCharacter`, kéo 3 PSB vào làm con, gán controller như Phần 2
4. Gán `AvatarCharacterDisplay` lên cả 2, gán 3 PSB vào Inspector
5. Chọn `Wins` → **UIWinsController** → gán `Winner Character` và `Loser Character`

---

## Phần 8: Tạo AvatarData assets

1. Vào `Assets/Resources/Avatars/`
2. Right-click → **Create → Game → AvatarData**
3. Tạo 4 assets:

| Asset | avatarId | avatarName | isDefault |
|---|---|---|---|
| `Avatar_0` | 0 | Mèo 1 | ✅ true |
| `Avatar_1` | 1 | Mèo 2 | false |
| `Avatar_2` | 2 | Mèo 3 | false |
| `Avatar_3` | 3 | Mèo 4 | false |

4. Gán `thumbnail` và `fullAvatar` từ `Assets/TaiNguyen/Character/Avatar/`:
   - `AVA_M1.png` → Avatar_0, `AVA_M2.png` → Avatar_1, v.v.

> **Không có field `animatorController`** — controller gán trực tiếp vào Animator của PSB trong scene.

---

## Phần 9: Kiểm tra

1. Chạy Play Mode
2. Gọi `player1Character.SetAvatar(0)` → `mascost1` / `Meo1` bật, còn lại tắt
3. Gọi `player1Character.TriggerHappy()` → `Character Meo` chạy animation Happy

Console log khi đúng:
```
[AvatarCharacterDisplay] ✅ Set avatar id=0 trên Player1Character
```

---

## Phần 10: Kiểm tra Animation Events đã hoạt động

### 10.1 — Kiểm tra code đã được update

✅ **Code đã được update tự động:**

1. **AvatarCharacterDisplay.cs** — Đã thêm 3 method mới:
   - `ShowIdle()` — Hiện Character Meo + trigger Idle
   - `ShowHappy()` — Hiện Character Meo + trigger Happy
   - `ShowSad()` — Hiện Character Meo_Sad + trigger Sad

2. **UIMultiplayerBattleController.cs** — Đã hook animation vào battle events:
   - `HandleQuestionGenerated()` → gọi `ShowIdle()` cho cả 2 player
   - `HandleAnswerResult()` → gọi `ShowHappy()` hoặc `ShowSad()` dựa trên winnerId

### 10.2 — Kiểm tra trong Unity Editor

1. Mở scene `Test_FireBase_multi`
2. Chọn `Canvas/GameplayPanel/Player1Character` → Component **AvatarCharacterDisplay**
3. Kiểm tra 3 PSB đã được gán đúng:
   - `Character Meo` → child `Character Meo`
   - `Character Meo Sad` → child `Character Meo_Sad`
   - `Meo Goc34 Fix` → child `MeoGoc34 Fix`
4. Lặp lại cho `Player2Character`

### 10.3 — Kiểm tra Animator Controllers

**Character Meo.controller:**
1. Mở `Assets/Script/Script_multiplayer/1Code/Multiplay/Animation_Multiplayer/Character/Character Meo.controller`
2. Kiểm tra có 2 triggers: `TriggerIdle`, `TriggerHappy`
3. Kiểm tra có 2 states: `Idle` (default - màu cam), `Happy`
4. Kiểm tra transitions từ `Any State`:
   - `Any State → Idle` (condition: `TriggerIdle`)
   - `Any State → Happy` (condition: `TriggerHappy`)

**Character Meo_Sad.controller:**
1. Mở `Assets/Script/Script_multiplayer/1Code/Multiplay/Animation_Multiplayer/Character/Character Meo_Sad.controller`
2. Kiểm tra có 1 trigger: `TriggerSad`
3. Kiểm tra có 1 state: `Sad` (default - màu cam)
4. Kiểm tra transition từ `Any State`:
   - `Any State → Sad` (condition: `TriggerSad`)

### 10.4 — Test trong Play Mode

1. **Chạy Play Mode** trong Unity Editor
2. **Tạo phòng multiplayer** (cần 2 client — dùng ParrelSync để test)
3. **Bắt đầu trận đấu**

**Kiểm tra timeline animation:**

| Thời điểm | Animation mong đợi | PSB hiển thị |
|-----------|-------------------|--------------|
| **Countdown "3, 2, 1, Ready, GO!"** | *(chưa có animation)* | *(chưa hiển thị)* |
| **Question Time (10s)** | `Idle` (đứng chờ) | `Character Meo` |
| **Summary Time - Thắng** | `Happy` (vui) | `Character Meo` |
| **Summary Time - Thua** | `Sad` (buồn) | `Character Meo_Sad` |
| **Summary Time - Cả 2 sai** | `Sad` (cả 2) | `Character Meo_Sad` |
| **Summary Time - Hòa** | `Happy` (cả 2) | `Character Meo` |
| **Câu hỏi mới** | `Idle` (quay lại) | `Character Meo` |

**Console logs mong đợi:**
```
[AvatarCharacterDisplay] Player1Character → ShowIdle()
[AvatarCharacterDisplay] Player2Character → ShowIdle()
[AvatarCharacterDisplay] Player1Character → ShowHappy()
[AvatarCharacterDisplay] Player2Character → ShowSad()
```

### 10.5 — Troubleshooting

**Vấn đề: Animation không chạy**
- ✅ Kiểm tra Animator component có controller được gán chưa
- ✅ Kiểm tra tên trigger khớp chính xác: `TriggerIdle`, `TriggerHappy`, `TriggerSad`
- ✅ Kiểm tra animation clips đã được gán vào states chưa

**Vấn đề: PSB không hiển thị/ẩn đúng**
- ✅ Kiểm tra 3 PSB đã được gán đúng trong Inspector của `AvatarCharacterDisplay`
- ✅ Kiểm tra tên skin con khớp: `mascost1`, `mascost2`, `mascost3`, `mascost4` (Character Meo/Sad)
- ✅ Kiểm tra tên skin con khớp: `Meo1`, `Meo2`, `Meo3`, `Meo4` (MeoGoc34 Fix)

**Vấn đề: Animation chạy nhưng không đúng timing**
- ✅ Kiểm tra `DefaultGameRules.asset`:
  - `questionTimeLimit` = 10s (Question Time)
  - `delayBetweenQuestions` = 3s (Summary Time)
- ✅ Animation tự động theo thời gian này — không cần code thêm

**Vấn đề: Cả 2 player đều hiển thị cùng animation**
- ✅ Kiểm tra `HandleAnswerResult()` có gọi đúng `ShowHappy()` / `ShowSad()` cho từng player
- ✅ Kiểm tra `winnerId` từ server (0 = Player1, 1 = Player2, -1 = cả 2 sai, -2 = hòa)

---

## Lưu ý quan trọng

- **3 PSB có 3 bộ xương khác nhau** → mỗi PSB có controller riêng, không dùng chung
- `Character Meo.controller` chứa **cả Idle lẫn Happy** trong 1 controller — dùng `TriggerIdle` / `TriggerHappy` để chuyển state
- `Character Meo_Sad.controller` chỉ có **Sad** — dùng `TriggerSad`
- Tên trigger phải khớp chính xác: `TriggerIdle`, `TriggerHappy`, `TriggerSad` (phân biệt hoa thường)
- Tên skin con phải khớp: `mascost1` (không phải `Mascost1`), `Meo1` (không phải `meo1`)
- ✅ **Logic PSB hiển thị theo sự kiện đã được implement** — animation tự động theo timeline battle
- ⏱️ **Thời gian animation bám theo GameRulesConfig** — người dùng có thể tuỳ chỉnh trong `DefaultGameRules.asset`
