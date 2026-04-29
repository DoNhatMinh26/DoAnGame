# Answer Summary - Quick Setup (5 phút)

## 🚀 Setup Nhanh

### Bước 1: Thêm Component (1 phút)
1. Chọn **GameplayPanel** trong Hierarchy
2. Inspector → Add Component → `AnswerSummaryUI`

### Bước 2: Tạo UI Elements (2 phút)

**TextTrangThaiDapAn1:**
- Right-click GameplayPanel → UI → Text - TextMeshPro
- Đặt tên: `TextTrangThaiDapAn1`
- Position: Trên trái (Player 1)

**TextTrangThaiDapAn2:**
- Right-click GameplayPanel → UI → Text - TextMeshPro
- Đặt tên: `TextTrangThaiDapAn2`
- Position: Trên phải (Player 2)

**TimerText:**
- ✅ Dùng chung từ TimerPanel (không cần tạo mới)

### Bước 3: Assign References (2 phút)
1. Chọn **GameplayPanel**
2. Trong AnswerSummaryUI component:
   - Drag TextTrangThaiDapAn1 → textTrangThaiDapAn1
   - Drag TextTrangThaiDapAn2 → textTrangThaiDapAn2
   - Drag TimerText (từ TimerPanel) → timerText

## ✅ Done!

Test ngay:
1. Play game
2. Kéo đáp án vào Slot
3. Xem tổng kết hiển thị

## 🎯 Hiển Thị

```
Đáp án người chơi 1 chọn là: 5
Đáp án người chơi 2 chọn là: 7
Người chơi 1 đúng! (1234ms)
3s
```

## ⚙️ Tuỳ Chỉnh

Trong AnswerSummaryUI Inspector:
- **Default Summary Duration**: 1-10s (mặc định 3s)

## 🐛 Nếu Không Hoạt Động

1. Kiểm tra Console có error không
2. Kiểm tra references có NULL không
3. Kiểm tra GameplayPanel có active không
4. Kiểm tra TextMeshProUGUI có được tạo đúng không

---

**Time**: 5 phút
**Difficulty**: Dễ
**Status**: ✅ Ready
