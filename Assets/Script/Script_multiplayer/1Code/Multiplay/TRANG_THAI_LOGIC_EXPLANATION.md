# Trạng Thái Logic - Giải Thích Chi Tiết

## 🎯 Ý Tưởng

Thay vì dùng TimerBG, sử dụng Text "Trạng Thái" để hiển thị 2 kiểu:
1. **Kiểu 1 (Question Time):** "Thời gian trả lời câu hỏi"
2. **Kiểu 2 (Summary Time):** "Thời gian thống kê đáp án"

## 📊 Flow Hoạt Động

```
Câu hỏi mới sinh ra
    ↓
Trạng Thái: "Thời gian trả lời câu hỏi"
TimerText: "10s" → "9s" → ... → "1s"
    ↓
Người chơi kéo đáp án
    ↓
Server xử lý
    ↓
Gửi kết quả + đáp án cả 2
    ↓
AnswerSummaryUI nhận event
    ↓
Lưu text gốc:
  - originalTimerText = "1s" (thời gian còn lại)
  - originalTrangThaiText = "Thời gian trả lời câu hỏi"
    ↓
Thay đổi trạng thái:
  - Trạng Thái: "Thời gian thống kê đáp án"
  - TimerText: "3s" → "2s" → "1s" → "0s"
    ↓
Hiển thị:
  - Đáp án P1: 5
  - Đáp án P2: 7
  - Kết quả: Người chơi 1 đúng! (1234ms)
    ↓
Sau 3s: Khôi phục
  - Trạng Thái: "Thời gian trả lời câu hỏi"
  - TimerText: "1s" (khôi phục)
    ↓
Câu hỏi mới
```

## 🔧 Code Logic

### Enum Trạng Thái
```csharp
private enum TimerState
{
    QuestionTime,      // Thời gian trả lời câu hỏi
    SummaryTime        // Thời gian thống kê đáp án
}

private TimerState currentState = TimerState.QuestionTime;
```

### Set Trạng Thái Question Time
```csharp
private void SetTrangThaiQuestionTime()
{
    currentState = TimerState.QuestionTime;
    if (trangThaiText != null)
    {
        trangThaiText.text = "Thời gian trả lời câu hỏi";
    }
}
```

### Set Trạng Thái Summary Time
```csharp
private void SetTrangThaiSummaryTime()
{
    currentState = TimerState.SummaryTime;
    if (trangThaiText != null)
    {
        trangThaiText.text = "Thời gian thống kê đáp án";
    }
}
```

### Lưu & Khôi Phục
```csharp
// Lưu text gốc
originalTimerText = timerText.text;
originalTrangThaiText = trangThaiText.text;

// Thay đổi trạng thái
SetTrangThaiSummaryTime();

// ... hiển thị tổng kết ...

// Khôi phục
timerText.text = originalTimerText;
SetTrangThaiQuestionTime();
```

## 📋 Các Sự Kiện

### 1. Khi Câu Hỏi Mới
```
Event: OnQuestionGenerated
Action:
  - Trạng Thái = "Thời gian trả lời câu hỏi"
  - TimerText = "10s"
  - currentState = QuestionTime
```

### 2. Khi Kéo Đáp Án
```
Event: OnAnswerDropped
Action:
  - Hiển thị đáp án người chơi chọn
  - Gửi lên server
```

### 3. Khi Server Xử Lý
```
Event: OnAnswerResultReceived
Action:
  - Lưu text gốc
  - Thay đổi trạng thái → SummaryTime
  - Hiển thị tổng kết
  - Đếm ngược timer
```

### 4. Khi Tổng Kết Kết Thúc
```
Event: ClearSummary
Action:
  - Khôi phục text gốc
  - Thay đổi trạng thái → QuestionTime
  - Xóa đáp án hiển thị
```

## 🎨 Hiển Thị UI

### Khi Trả Lời Câu Hỏi
```
┌──────────────────────────────┐
│ Trạng Thái: Thời gian trả    │
│ lời câu hỏi                  │
│                              │
│ Câu hỏi: 2 + 3 = ?           │
│ [0] [1] [2] [3]              │
│                              │
│ TimerText: 10s               │
└──────────────────────────────┘
```

### Khi Thống Kê Đáp Án
```
┌──────────────────────────────┐
│ Trạng Thái: Thời gian thống  │
│ kê đáp án                    │
│                              │
│ Đáp án P1: 5                 │
│ Đáp án P2: 7                 │
│ Người chơi 1 đúng! (1234ms)  │
│                              │
│ TimerText: 3s                │
└──────────────────────────────┘
```

## 🔄 Timeline Chi Tiết

```
T=0s: Câu hỏi mới
  Trạng Thái: "Thời gian trả lời câu hỏi"
  TimerText: "10s"
  currentState: QuestionTime

T=5s: Người chơi kéo đáp án
  Event: OnAnswerDropped
  Action: Gửi lên server

T=5.1s: Server xử lý
  Event: OnAnswerResultReceived
  Action: 
    - Lưu: originalTimerText = "5s"
    - Lưu: originalTrangThaiText = "Thời gian trả lời câu hỏi"
    - Thay: Trạng Thái = "Thời gian thống kê đáp án"
    - Thay: currentState = SummaryTime

T=5.1s - 8.1s: Tổng kết
  Trạng Thái: "Thời gian thống kê đáp án"
  TimerText: "3s" → "2s" → "1s" → "0s"
  Hiển thị: Đáp án + Kết quả

T=8.1s: Tổng kết kết thúc
  Event: ClearSummary
  Action:
    - Khôi phục: TimerText = "5s"
    - Khôi phục: Trạng Thái = "Thời gian trả lời câu hỏi"
    - Khôi phục: currentState = QuestionTime

T=8.1s+: Câu hỏi mới
  Trạng Thái: "Thời gian trả lời câu hỏi"
  TimerText: "10s"
```

## ✅ Lợi Ích

✅ **Rõ ràng** - Người chơi biết đang ở trạng thái nào
✅ **Không xung đột** - Tự động khôi phục trạng thái
✅ **Linh hoạt** - Dễ thêm trạng thái mới
✅ **Dễ debug** - Có thể kiểm tra currentState

## 🔐 Logic Đảm Bảo

✅ **Lưu/Khôi phục đúng:**
- Lưu text gốc trước khi thay đổi
- Khôi phục sau khi tổng kết kết thúc

✅ **Trạng thái đồng bộ:**
- Cả Host và Client thấy cùng trạng thái
- Dùng NetworkVariable để đồng bộ

✅ **Không lỗi:**
- Kiểm tra null trước khi access
- Tự động cleanup

## 📝 Ghi Chú

- **Trạng Thái** là Text mới, thay thế TimerBG
- **TimerText** vẫn dùng chung từ TimerPanel
- **currentState** giúp quản lý logic
- **originalTimerText** & **originalTrangThaiText** lưu trạng thái gốc

---

**Status**: ✅ Complete
**Last Updated**: 2026-04-29
