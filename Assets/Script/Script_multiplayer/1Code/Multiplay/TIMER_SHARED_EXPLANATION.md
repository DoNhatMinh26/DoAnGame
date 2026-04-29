# TimerText Dùng Chung - Giải Thích

## 🎯 Cách Hoạt Động

### Trước (Khi Không Có Tổng Kết)
```
TimerText hiển thị: "10s" (thời gian câu hỏi)
```

### Khi Có Tổng Kết
```
1. Người chơi kéo đáp án
2. Server xử lý
3. AnswerSummaryUI nhận event
4. Lưu text gốc: "10s"
5. Thay đổi TimerText: "3s" → "2s" → "1s" → "0s"
6. Xóa tổng kết
7. Khôi phục text gốc: "10s"
```

## 📝 Code Logic

```csharp
// Lưu text gốc
originalTimerText = timerText.text; // "10s"

// Hiển thị tổng kết
while (elapsed < duration)
{
    timerText.text = $"{Mathf.CeilToInt(remaining)}s"; // "3s", "2s", "1s"
    yield return null;
}

// Khôi phục text gốc
timerText.text = originalTimerText; // "10s"
```

## ✅ Lợi Ích

✅ **Tiết kiệm UI** - Không cần tạo TimerText mới
✅ **Không xung đột** - Tự động khôi phục sau tổng kết
✅ **Đơn giản** - Chỉ cần assign 1 reference

## 🔄 Timeline

```
Câu hỏi 1
├─ TimerText: "10s" → "9s" → ... → "1s"
├─ Người chơi kéo đáp án
├─ Tổng kết bắt đầu
│  └─ TimerText: "3s" → "2s" → "1s" → "0s"
├─ Tổng kết kết thúc
└─ TimerText: "10s" (khôi phục)

Câu hỏi 2
├─ TimerText: "10s" → "9s" → ...
```

## 🎨 Hiển Thị

### Khi Câu Hỏi
```
┌─────────────────────┐
│   Câu hỏi: 2 + 3 = ?│
│                     │
│   [0] [1] [2] [3]   │
│                     │
│   Timer: 10s        │ ← TimerText
└─────────────────────┘
```

### Khi Tổng Kết
```
┌─────────────────────────────────────┐
│ Đáp án P1: 5                        │
│ Đáp án P2: 7                        │
│ Người chơi 1 đúng! (1234ms)         │
│                                     │
│ Timer: 3s                           │ ← TimerText (thay đổi)
└─────────────────────────────────────┘
```

## 🔧 Setup

1. Chọn **GameplayPanel**
2. Thêm **AnswerSummaryUI** component
3. Assign **timerText** → TimerText từ TimerPanel
4. Done!

## 🐛 Nếu Có Vấn Đề

### TimerText Không Khôi Phục
- Kiểm tra `originalTimerText` có được lưu không
- Kiểm tra `ClearSummary()` có được gọi không

### TimerText Bị Che Khuất
- Kiểm tra z-order của UI elements
- Kiểm tra CanvasGroup.blocksRaycasts

### TimerText Không Cập Nhật
- Kiểm trap timerText reference có NULL không
- Kiểm tra TextMeshProUGUI có được assign đúng không

## 📊 Ví Dụ

### Scenario 1: Người chơi 1 đúng
```
Trước tổng kết:
  TimerText: "5s" (thời gian câu hỏi còn lại)

Tổng kết:
  originalTimerText = "5s"
  TimerText: "3s" → "2s" → "1s" → "0s"

Sau tổng kết:
  TimerText: "5s" (khôi phục)
```

### Scenario 2: Timeout
```
Trước tổng kết:
  TimerText: "0s" (hết thời gian)

Tổng kết:
  originalTimerText = "0s"
  TimerText: "3s" → "2s" → "1s" → "0s"

Sau tổng kết:
  TimerText: "0s" (khôi phục)
```

## ✨ Tính Năng

✅ Dùng chung TimerText
✅ Tự động lưu/khôi phục text
✅ Không xung đột với MultiplayerHealthUI
✅ Hoạt động trên cả Host và Client

---

**Status**: ✅ Complete
**Last Updated**: 2026-04-29
