# Avatar Animation Setup Checklist

## ✅ Checklist Setup (Làm theo thứ tự)

### 1. Setup AnimatorController (trong Unity Editor)

- [ ] **Character Meo.controller**
  - [ ] Tạo 2 triggers: `TriggerIdle`, `TriggerHappy`
  - [ ] Thêm 2 states: `Idle` (default), `Happy`
  - [ ] Tạo transitions từ `Any State`:
    - [ ] `Any State → Idle` (condition: `TriggerIdle`, Has Exit Time = OFF)
    - [ ] `Any State → Happy` (condition: `TriggerHappy`, Has Exit Time = OFF)

- [ ] **Character Meo_Sad.controller**
  - [ ] Tạo 1 trigger: `TriggerSad`
  - [ ] Thêm 1 state: `Sad` (default)
  - [ ] Tạo transition từ `Any State`:
    - [ ] `Any State → Sad` (condition: `TriggerSad`, Has Exit Time = OFF)

- [ ] **MeoGoc34 Fix.controller** ⭐ MỚI
  - [ ] Tạo 1 trigger: `TriggerAttack`
  - [ ] Thêm 1 state: `Attack` (default)
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

### 4. Gán vào UIMultiplayerBattleController

- [ ] Chọn `Canvas/GameplayPanel`
  - [ ] Component **UIMultiplayerBattleController** → gán:
    - [ ] `Player1 Character` → `Player1Character`
    - [ ] `Player2 Character` → `Player2Character`

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
- [ ] **Kiểm tra animation:**
  - [ ] Question Time → `Idle` (Character Meo hiển thị)
  - [ ] **1 đúng, 1 sai:**
    - [ ] Người đúng → `Attack` (MeoGoc34 Fix hiển thị, ném bóng)
    - [ ] Người sai → `Sad` (Character Meo_Sad hiển thị, mất máu)
  - [ ] **Cả 2 đúng, so sánh tốc độ:**
    - [ ] Nhanh hơn → `Attack` (MeoGoc34 Fix hiển thị, KHÔNG gây sát thương)
    - [ ] Chậm hơn → `Sad` (Character Meo_Sad hiển thị, KHÔNG mất máu)
  - [ ] **Cả 2 sai** → `Sad` (cả 2, KHÔNG mất máu)
  - [ ] **Hòa** (cả 2 đúng cùng lúc) → `Sad` (cả 2, KHÔNG mất máu)
  - [ ] Câu hỏi mới → quay về `Idle`

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

### Cả 2 player cùng animation
- ✅ Kiểm tra `winnerId` từ server (0/1/-1/-2)
- ✅ Kiểm tra `HandleAnswerResult()` gọi đúng method

---

## 📝 Ghi chú

- **Code đã được update tự động** — không cần sửa code thêm
- **Animation tự động theo GameRules** — tuỳ chỉnh trong `DefaultGameRules.asset`
- **3 PSB có 3 controller riêng** — không dùng chung
- **Tên trigger và skin phải khớp chính xác** — phân biệt hoa thường
