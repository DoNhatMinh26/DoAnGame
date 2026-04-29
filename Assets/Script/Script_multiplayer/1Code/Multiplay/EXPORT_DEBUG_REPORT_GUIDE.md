# 📄 HƯỚNG DẪN EXPORT DEBUG REPORT

**Mục đích**: Ghi toàn bộ thông tin debug ra file TXT để gửi cho developer phân tích

---

## ✅ CÁCH SỬ DỤNG (30 giây)

### Cách 1: Tự động export khi validate (Khuyến nghị)

1. **Play game** → Tạo phòng → Join → Start
2. **Bấm F3** → Validate All References
3. **File tự động được tạo**: `BattleDebugReport.txt` trong thư mục project root

### Cách 2: Export thủ công

1. **Play game** → Tạo phòng → Join → Start
2. **Bấm F4** → Export Debug Report
3. Hoặc click button **"📄 Export Debug Report (F4)"** trong Debug GUI

### Cách 3: Export từ Inspector (không cần Play)

1. Chọn **BattleManager** GameObject
2. Component `MultiplayerBattleDebugger`
3. Right-click → **Export Debug Report to TXT**

---

## 📂 VỊ TRÍ FILE

File được lưu tại:
```
D:\app\GameDoan\DoAnGame\BattleDebugReport.txt
```

(Cùng cấp với folder `Assets/`)

---

## 📄 NỘI DUNG FILE

File TXT chứa:

### 1. Network Status
```
--- NETWORK STATUS ---
NetworkManager: EXISTS
  IsServer: True
  IsClient: True
  IsHost: True
  IsListening: True
  Connected Clients: 2
  Client IDs: [0, 1]
```

### 2. Battle Manager Status
```
--- BATTLE MANAGER STATUS ---
BattleManager GameObject: BattleManager
  Active: True
  Enabled: True
  Has NetworkObject: True ❌ SAI! PHẢI XÓA!  ← Quan trọng!
  Instance: False
  IsSpawned: False
  Match Started: False
  Current Grade: 1
  Current Difficulty: 1
  Current Question: ''
  Time Remaining: 0.00s
  Player 1 State: NULL
  Player 2 State: NULL
  GameRules Reference: DefaultGameRules
  LevelData Reference: LevelDataConfig
  PlayerStatePrefab Reference: NetworkedPlayerState
    - Has NetworkObject: True
    - Has NetworkedPlayerState: True
```

### 3. Battle Controller Status
```
--- BATTLE CONTROLLER STATUS ---
UIMultiplayerBattleController: FOUND on GameplayPanel
  Active: True
  Enabled: True
  BattleManager Reference: NULL (will auto-find)
  QuestionText Reference: cauhoiText
  AnswerSlot Reference: Slot
  AnswerChoices Array: 3 elements
    [0]: Answer_0
    [1]: Answer_1
    [2]: Answer_2
```

### 4. Drag Drop Adapter Status
```
--- DRAG DROP ADAPTER STATUS ---
MultiplayerDragDropAdapter: FOUND on Slot
  Active: True
  Enabled: True
  BattleController Reference: NULL (will auto-find)
```

### 5. Validation Results
```
--- VALIDATION RESULTS ---
Total Success: 15
Total Warnings: 2
Total Errors: 1

ERRORS LIST:
  ❌ BattleManager: Có NetworkObject component (SAI! Phải xóa đi)
```

### 6. Final Verdict
```
--- FINAL VERDICT ---
❌ Có 1 lỗi PHẢI SỬA trước khi test!
```

### 7. Recommended Actions
```
--- RECOMMENDED ACTIONS ---
1. XÓA NetworkObject khỏi BattleManager GameObject
   - Chọn BattleManager trong Hierarchy
   - Inspector → NetworkObject component
   - Right-click → Remove Component
```

---

## 🔧 CẤU HÌNH

Trong Inspector của `MultiplayerBattleDebugger`:

```
Export Settings:
├─ Export File Name: BattleDebugReport.txt  ← Tên file
└─ Auto Export On Validate: ✅ true  ← Tự động export khi bấm F3
```

**Khuyến nghị**: Để `Auto Export On Validate = true` để tự động tạo file mỗi khi validate

---

## 📤 GỬI FILE CHO DEVELOPER

### Cách 1: Copy nội dung file

1. Mở file `BattleDebugReport.txt`
2. Ctrl+A → Ctrl+C (copy toàn bộ)
3. Paste vào chat

### Cách 2: Gửi file trực tiếp

1. Tìm file: `D:\app\GameDoan\DoAnGame\BattleDebugReport.txt`
2. Kéo thả file vào chat

---

## 🔍 PHÂN TÍCH FILE

Developer sẽ xem:

### ✅ Kiểm tra Network
- NetworkManager có tồn tại không?
- IsServer/IsClient đúng không?
- Connected Clients = 2?

### ✅ Kiểm tra BattleManager
- **Has NetworkObject**: PHẢI = False!
- **Instance**: PHẢI = True!
- **Match Started**: PHẢI = True sau khi Start!
- **Current Question**: PHẢI có câu hỏi!
- **Player States**: PHẢI = FOUND!

### ✅ Kiểm tra References
- GameRules: PHẢI có tên asset
- LevelData: PHẢI có tên asset
- PlayerStatePrefab: PHẢI có tên prefab

### ✅ Kiểm tra BattleController
- Active: PHẢI = True
- QuestionText: PHẢI có tên GameObject
- AnswerChoices: PHẢI = 3 elements

### ✅ Kiểm tra Errors
- Total Errors: PHẢI = 0!
- Nếu > 0 → Đọc ERRORS LIST và RECOMMENDED ACTIONS

---

## 🎯 VÍ DỤ PHÂN TÍCH

### Trường hợp 1: Lỗi NetworkObject

```
Has NetworkObject: True ❌ SAI! PHẢI XÓA!
Instance: False
```

**Nguyên nhân**: BattleManager có NetworkObject component  
**Giải pháp**: Xóa NetworkObject  
**Kết quả**: Instance sẽ = True, hệ thống hoạt động

---

### Trường hợp 2: Thiếu References

```
GameRules Reference: NULL ❌
```

**Nguyên nhân**: Chưa gán DefaultGameRules.asset  
**Giải pháp**: Gán asset vào field Game Rules  
**Kết quả**: Có thể sinh câu hỏi

---

### Trường hợp 3: Player States NULL

```
Player 1 State: NULL
Player 2 State: NULL
```

**Nguyên nhân**: 
- PlayerStatePrefab chưa gán
- Hoặc prefab thiếu NetworkObject
- Hoặc InitializeBattle() chưa được gọi

**Giải pháp**: Kiểm tra prefab và references

---

## 📊 CHECKLIST TRƯỚC KHI GỬI

Trước khi gửi file cho developer, đảm bảo:

- [ ] Đã chạy game và vào được phòng (Connected: 2/2)
- [ ] Đã bấm Start để bắt đầu battle
- [ ] Đã bấm F3 để validate
- [ ] File `BattleDebugReport.txt` đã được tạo
- [ ] File có kích thước > 0 bytes (kiểm tra trong Console log)

---

## 🚀 WORKFLOW HOÀN CHỈNH

1. **Add debug script** vào BattleManager
2. **Play game** → Tạo phòng → Join → Start
3. **Bấm F3** → Validate (file tự động được tạo)
4. **Mở file** `BattleDebugReport.txt`
5. **Đọc phần ERRORS LIST** và **RECOMMENDED ACTIONS**
6. **Fix lỗi** theo hướng dẫn
7. **Bấm F3 lại** → Kiểm tra còn lỗi không
8. **Nếu vẫn lỗi** → Gửi file cho developer

---

**Cập nhật**: 2026-04-29  
**Version**: 1.0 - Export Debug Report

