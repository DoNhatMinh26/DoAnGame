# Avatar Animation Setup Checklist

## ✅ Checklist Setup (Làm theo thứ tự)

### 1. Setup AnimatorController (trong Unity Editor)

- [ ] **Character Meo.controller**
  - [ ] Tạo 2 triggers: `TriggerIdle`, `TriggerHappy`
  - [ ] Thêm 2 states: `Idle` (default), `Happy`
  - [ ] **Set Loop Time:**
    - [ ] `Idle.anim` → Loop Time = **ON**
    - [ ] `Happy.anim` → Loop Time = **ON**
  - [ ] Tạo transitions từ `Any State`:
    - [ ] `Any State → Idle` (condition: `TriggerIdle`, Has Exit Time = OFF)
    - [ ] `Any State → Happy` (condition: `TriggerHappy`, Has Exit Time = OFF)

- [ ] **Character Meo_Sad.controller**
  - [ ] Tạo 1 trigger: `TriggerSad`
  - [ ] Thêm 1 state: `Sad` (default)
  - [ ] **Set Loop Time:**
    - [ ] `Sad.anim` → Loop Time = **ON**
  - [ ] Tạo transition từ `Any State`:
    - [ ] `Any State → Sad` (condition: `TriggerSad`, Has Exit Time = OFF)

- [ ] **MeoGoc34 Fix.controller** ⭐ MỚI
  - [ ] Tạo 1 trigger: `TriggerAttack`
  - [ ] Thêm 1 state: `Attack` (default)
  - [ ] **Set Loop Time:**
    - [ ] `Attack.anim` → Loop Time = **OFF** (chỉ chạy 1 lần!)
  - [ ] Tạo transition từ `Any State`:
    - [ ] `Any State → Attack` (condition: `TriggerAttack`, Has Exit Time = OFF)

### 2. Gán Controller vào PSB (trong scene Test_FireBase_multi)

- [ ] Chọn `Canvas/GameplayPanel/Player1Character/Character Meo`
  - [ ] Component **Animator** → gán `Character Meo.controller`
- [ ] Chọn `Canvas/GameplayPanel/Player1Character/Character Meo_Sad`
  - [ ] Component **Animator** → gán `Character Meo_Sad.controller`
- [ ] Chọn `Canvas/GameplayPanel/Player1Character/MeoGoc34 Fix`
  - [ ] Component **Animator** → gán `MeoGoc34 Fix.controller`
- [ ] Lặp lại cho `Player2Character`

### 3. Gán AvatarCharacterDisplay (trong scene Test_FireBase_multi)

- [ ] Chọn `Canvas/GameplayPanel/Player1Character`
  - [ ] Component **AvatarCharacterDisplay** → gán 3 PSB:
    - [ ] `Character Meo` → child `Character Meo`
    - [ ] `Character Meo Sad` → child `Character Meo_Sad`
    - [ ] `Meo Goc34 Fix` → child `MeoGoc34 Fix`
- [ ] Lặp lại cho `Player2Character`

### 4. Gán vào UIMultiplayerBattleController & UIWinsController

**GameplayPanel:**
- [ ] Chọn `Canvas/GameplayPanel`
  - [ ] Component **UIMultiplayerBattleController** → gán:
    - [ ] `Player1 Character` → `Player1Character`
    - [ ] `Player2 Character` → `Player2Character`

**Wins Panel:**
- [ ] Chọn `Canvas/Wins`
  - [ ] Component **UIWinsController** → gán:
    - [ ] `Winner Character` → `WinnerCharacter` (GameObject chứa 3 PSB)
    - [ ] `Loser Character` → `LoserCharacter` (GameObject chứa 3 PSB)
- [ ] Setup `WinnerCharacter` và `LoserCharacter` giống như `Player1Character`:
  - [ ] Kéo 3 PSB vào làm con
  - [ ] Gán controller cho từng PSB
  - [ ] Gán `AvatarCharacterDisplay` component
  - [ ] Gán 3 PSB vào Inspector của `AvatarCharacterDisplay`

### 5. Kiểm tra tên skin con (trong Hierarchy)

- [ ] Mở `Character Meo` trong Hierarchy → kiểm tra tên con:
  - [ ] `mascost1`, `mascost2`, `mascost3`, `mascost4`, `root`
- [ ] Mở `Character Meo_Sad` trong Hierarchy → kiểm tra tên con:
  - [ ] `mascost1`, `mascost2`, `mascost3`, `mascost4`, `root`
- [ ] Mở `MeoGoc34 Fix` trong Hierarchy → kiểm tra tên con:
  - [ ] `Meo1`, `Meo2`, `Meo3`, `Meo4`, `Root`

### 6. Kiểm tra GameRulesConfig

- [ ] Mở `DefaultGameRules.asset` (hoặc GameRules đang dùng)
  - [ ] `questionTimeLimit` = 10 (Question Time - 10 giây)
  - [ ] `delayBetweenQuestions` = 3 (Summary Time - 3 giây)
  - [ ] *(Có thể tuỳ chỉnh theo ý muốn)*

### 7. Test trong Play Mode

- [ ] Chạy Play Mode
- [ ] Tạo phòng multiplayer (dùng ParrelSync để test 2 client)
- [ ] Bắt đầu trận đấu
- [ ] **Kiểm tra chỉ 1 character hiển thị tại 1 thời điểm:**
  - [ ] Question Time → Chỉ `Character Meo` active (2 PSB còn lại inactive)
  - [ ] Attack → Chỉ `MeoGoc34 Fix` active (2 PSB còn lại inactive)
  - [ ] Sad → Chỉ `Character Meo_Sad` active (2 PSB còn lại inactive)
- [ ] **Kiểm tra animation:**
  - [ ] Question Time → `Idle` (Character Meo hiển thị, loop)
  - [ ] **1 đúng, 1 sai:**
    - [ ] Người đúng → `Attack` (MeoGoc34 Fix hiển thị, chạy 1 lần, không loop)
    - [ ] Người sai → `Sad` (Character Meo_Sad hiển thị, loop, mất máu)
  - [ ] **Cả 2 đúng, so sánh tốc độ:**
    - [ ] Nhanh hơn → `Attack` (MeoGoc34 Fix hiển thị, KHÔNG gây sát thương)
    - [ ] Chậm hơn → `Sad` (Character Meo_Sad hiển thị, KHÔNG mất máu)
  - [ ] **Cả 2 sai** → `Sad` (cả 2, KHÔNG mất máu)
  - [ ] **Hòa** (cả 2 đúng cùng lúc) → `Sad` (cả 2, KHÔNG mất máu)
  - [ ] Câu hỏi mới → quay về `Idle`
- [ ] **Kiểm tra Wins panel:**
  - [ ] Winner → `Happy` (Character Meo hiển thị, loop)
  - [ ] Loser → `Sad` (Character Meo_Sad hiển thị, loop)

### 8. Kiểm tra Console Logs

- [ ] Thấy logs:
  ```
  [AvatarCharacterDisplay] Player1Character → ShowIdle()
  [AvatarCharacterDisplay] Player2Character → ShowIdle()
  [AvatarCharacterDisplay] Player1Character → ShowAttack()
  [AvatarCharacterDisplay] Player2Character → ShowSad()
  ```

---

## 📊 Logic Animation Summary

### Quy tắc mất máu:
- ✅ **CHỈ khi 1 đúng, 1 sai** → người sai mất máu
- ❌ **Cả 2 đúng** (kể cả hòa) → KHÔNG ai mất máu
- ❌ **Cả 2 sai** → KHÔNG ai mất máu

### Animation theo trường hợp:

| Trường hợp | Người thắng | Người thua | Mất máu? |
|------------|-------------|------------|----------|
| **Cả 2 đúng, so sánh tốc độ** | Attack (tấn công) | Sad (buồn) | ❌ KHÔNG |
| **1 đúng, 1 sai** | Attack (tấn công) | Sad (bị tấn công) | ✅ CÓ |
| **Cả 2 sai** | Sad | Sad | ❌ KHÔNG |
| **Hòa (cả 2 đúng cùng lúc)** | Sad | Sad | ❌ KHÔNG |

---

## 🔧 Troubleshooting

### Animation không chạy
- ✅ Kiểm tra Animator có controller được gán
- ✅ Kiểm tra tên trigger: `TriggerIdle`, `TriggerHappy`, `TriggerSad` (phân biệt hoa thường)
- ✅ Kiểm tra animation clips đã gán vào states

### PSB không hiển thị/ẩn
- ✅ Kiểm tra 3 PSB đã gán đúng trong `AvatarCharacterDisplay`
- ✅ Kiểm tra tên skin con khớp chính xác

### Animation không đúng timing
- ✅ Kiểm tra `DefaultGameRules.asset`:
  - `questionTimeLimit` (Question Time)
  - `delayBetweenQuestions` (Summary Time)

### Nhiều character hiển thị cùng lúc
- ✅ Code đã fix - `SetAvatar()` tự động gọi `ShowIdle()` để ẩn 2 PSB không dùng
- ✅ Kiểm tra trong Hierarchy khi Play Mode - chỉ 1 PSB active tại 1 thời điểm
- ✅ Xóa các GameObject duplicate nếu có

### Attack animation loop liên tục
- ✅ Chọn `Attack.anim` → Inspector → **Loop Time = OFF**

### Wins panel không hiển thị animation
- ✅ Kiểm tra `UIWinsController` đã gán `winnerCharacter` và `loserCharacter`
- ✅ Winner → `ShowHappy()`, Loser → `ShowSad()`

---

## 📝 Ghi chú

- **Code đã được update tự động** — không cần sửa code thêm
- **Animation tự động theo GameRules** — tuỳ chỉnh trong `DefaultGameRules.asset`
- **3 PSB có 3 controller riêng** — không dùng chung
- **Tên trigger và skin phải khớp chính xác** — phân biệt hoa thường
