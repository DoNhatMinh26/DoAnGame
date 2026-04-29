# Answer Summary UI - Setup Guide

## 📋 Tổng Quan

Hệ thống tổng kết đáp án hiển thị:
1. **Đáp án người chơi chọn** - Khi kéo thả vào Slot
2. **Kết quả đúng/sai** - Sau khi server xử lý
3. **Timer đếm ngược** - Thời gian tổng kết (1-10s tuỳ admin)

## 🔧 Setup

### Bước 1: Thêm AnswerSummaryUI Component

1. Chọn **GameplayPanel** trong Hierarchy
2. Thêm component: `AnswerSummaryUI`
3. Assign các TextMeshProUGUI:
   - **textTrangThaiDapAn1** - Text hiển thị đáp án Player 1
   - **textTrangThaiDapAn2** - Text hiển thị đáp án Player 2
   - **timerText** - Dùng chung từ TimerPanel (optional - tự động tìm)

### Bước 2: Tạo UI Elements (nếu chưa có)

Nếu chưa có TextMeshProUGUI, tạo mới:

1. **Tạo TextTrangThaiDapAn1:**
   - Right-click GameplayPanel → UI → Text - TextMeshPro
   - Đặt tên: `TextTrangThaiDapAn1`
   - Position: Phía trên bên trái (Player 1)
   - Font size: 30-40

2. **Tạo TextTrangThaiDapAn2:**
   - Right-click GameplayPanel → UI → Text - TextMeshPro
   - Đặt tên: `TextTrangThaiDapAn2`
   - Position: Phía trên bên phải (Player 2)
   - Font size: 30-40

3. **TimerText (Dùng Chung):**
   - ✅ Dùng TimerText ở TimerPanel (không cần tạo mới)
   - Hoặc tạo mới nếu muốn riêng

### Bước 3: Assign References

1. Chọn **GameplayPanel**
2. Trong Inspector, tìm **AnswerSummaryUI** component
3. Assign:
   - **textTrangThaiDapAn1** → TextTrangThaiDapAn1 object
   - **textTrangThaiDapAn2** → TextTrangThaiDapAn2 object
   - **timerText** → TimerText object (từ TimerPanel)

### Bước 4: Tuỳ Chỉnh Thời Gian

Trong AnswerSummaryUI Inspector:
- **Summary Duration Min**: 1s (tối thiểu)
- **Summary Duration Max**: 10s (tối đa)
- **Default Summary Duration**: 3s (mặc định)

## 📊 Flow Hoạt Động

```
1. Người chơi kéo đáp án vào Slot
   ↓
2. MultiplayerDragAndDrop gọi battleController.OnAnswerDropped()
   ↓
3. BattleManager nhận đáp án từ cả 2 người chơi
   ↓
4. Server xử lý và gọi NotifyAnswerResultClientRpc()
   ↓
5. Client nhận event OnAnswerResultReceived
   ↓
6. AnswerSummaryUI hiển thị:
   - Đáp án người chơi 1: [số]
   - Đáp án người chơi 2: [số]
   - Kết quả: Người chơi X đúng/sai
   - Timer: 3s đếm ngược
   ↓
7. Sau 3s, xóa tổng kết
   ↓
8. Sinh câu hỏi mới
```

## 🎯 Hiển Thị

### Khi Kéo Thả Đáp Án
```
Đáp án người chơi 1 chọn là: 5
Đáp án người chơi 2 chọn là: 7
```

### Kết Quả
```
Người chơi 1 đúng! (1234ms)
hoặc
Người chơi 2 đúng! (1567ms)
hoặc
Cả 2 đều sai!
```

### Timer
```
3s
2s
1s
0s
```

## 🔧 Tuỳ Chỉnh Thời Gian (Admin)

Để thay đổi thời gian tổng kết:

```csharp
// Trong code
AnswerSummaryUI summaryUI = GetComponent<AnswerSummaryUI>();
summaryUI.SetSummaryDuration(5f); // 5 giây
```

Hoặc thay đổi trực tiếp trong Inspector:
- **Default Summary Duration**: 1-10s

## 🐛 Debugging

### Xem Log
```
[AnswerSummaryUI] Answer result: Winner=0, P1Answer=5, P2Answer=7
[AnswerSummaryUI] P1 answer: 5
[AnswerSummaryUI] P2 answer: 7
[AnswerSummaryUI] Result: Người chơi 1 đúng! (1234ms)
[AnswerSummaryUI] Summary ended, ready for next question
```

### Nếu Không Hiển Thị
1. Kiểm tra AnswerSummaryUI component có được thêm không
2. Kiểm tra TextMeshProUGUI references có được assign không
3. Kiểm tra Console có error không
4. Kiểm tra GameplayPanel có active không

## ⚙️ Cấu Hình

### GameRulesConfig
Đảm bảo có:
- `delayBetweenQuestions` - Thời gian chờ trước câu hỏi mới (nên > 3s)

### Ví Dụ
```
delayBetweenQuestions = 4s
defaultSummaryDuration = 3s
→ Tổng: 7s giữa các câu hỏi
```

## 📝 Ghi Chú

- **Không ảnh hưởng đến logic game** - Chỉ hiển thị UI
- **Hoạt động trên cả Host và Client** - Đồng bộ qua NetworkVariable
- **Tự động xóa sau timer** - Không cần manual cleanup
- **Có thể tuỳ chỉnh thời gian** - Admin có thể thay đổi

## 🎓 Kiến Trúc

```
NetworkedMathBattleManager
  ├─ OnAnswerResultReceived event
  │  └─ (winnerId, correct, responseTimeMs, player1Answer, player2Answer)
  │
AnswerSummaryUI
  ├─ Subscribe OnAnswerResultReceived
  ├─ ShowSummaryRoutine()
  │  ├─ DisplayAnswers()
  │  ├─ DisplayResult()
  │  └─ Timer countdown
  └─ ClearSummary()
```

## ✅ Checklist

- [ ] Thêm AnswerSummaryUI component vào GameplayPanel
- [ ] Tạo 3 TextMeshProUGUI (TextTrangThaiDapAn1, TextTrangThaiDapAn2, TimerText)
- [ ] Assign references trong Inspector
- [ ] Test kéo đáp án vào Slot
- [ ] Xem tổng kết hiển thị đúng
- [ ] Xem timer đếm ngược
- [ ] Xem câu hỏi mới sinh ra sau tổng kết
- [ ] Test trên cả Host và Client (ParrelSync)

---

**Status**: ✅ Ready
**Last Updated**: 2026-04-29
