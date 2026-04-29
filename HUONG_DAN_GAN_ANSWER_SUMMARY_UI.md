# Hướng Dẫn Gán Answer Summary UI Components

## 📋 Cấu Trúc Hierarchy

```
Canvas
└── GameplayPanel
    ├── TextTrangThaiDapAn1 (Text - TextMeshPro UGUI)
    ├── TextTrangThaiDapAn2 (Text - TextMeshPro UGUI)
    └── TimerPanel
        ├── TimerState (Text - TextMeshPro UGUI)
        └── Timertext (Text - TextMeshPro UGUI)
```

---

## 🎯 Bước 1: Chọn GameplayPanel

1. Trong **Hierarchy**, tìm: `Canvas → GameplayPanel`
2. Click vào **GameplayPanel**
3. Trong **Inspector**, tìm component **Answer Summary UI (Script)**

---

## 🎯 Bước 2: Gán Components

### === ANSWER DISPLAY ===

#### Text Trang Thai Dap An 1
- **Tìm trong Hierarchy**: `GameplayPanel → TextTrangThaiDapAn1`
- **Kéo thả vào**: `Text Trang Thai Dap An 1` field
- **Mục đích**: Hiển thị đáp án + thời gian của Player 1

#### Text Trang Thai Dap An 2
- **Tìm trong Hierarchy**: `GameplayPanel → TextTrangThaiDapAn2`
- **Kéo thả vào**: `Text Trang Thai Dap An 2` field
- **Mục đích**: Hiển thị đáp án + thời gian của Player 2

---

### === STATUS & TIMER (SHARED) ===

#### Timer Text
- **Tìm trong Hierarchy**: `GameplayPanel → TimerPanel → Timertext`
- **Kéo thả vào**: `Timer Text` field
- **Mục đích**: Hiển thị đếm ngược (10s, 9s, ..., 5s, 4s, ...)

#### Trang Thai Text
- **Tìm trong Hierarchy**: `GameplayPanel → TimerPanel → TimerState`
- **Kéo thả vào**: `Trang Thai Text` field
- **Mục đích**: Hiển thị trạng thái ("Thời gian trả lời câu hỏi" / "Thời gian thống kê đáp án")

---

### === RESULT DISPLAY ===

#### Result Text
- **Tùy chọn** - Nếu bạn muốn hiển thị kết quả riêng
- Có thể để `None` nếu không cần
- **Mục đích**: Hiển thị "Người chơi 1 đúng!" / "Cả 2 đều sai!"

---

### === SETTINGS ===

#### Summary Duration Min
- **Giá trị**: `1` (giây)
- **Mục đích**: Thời gian tối thiểu cho Summary Time

#### Summary Duration Max
- **Giá trị**: `10` (giây)
- **Mục đích**: Thời gian tối đa cho Summary Time

#### Default Summary Duration
- **Giá trị**: `5` (giây)
- **Mục đích**: Thời gian mặc định cho Summary Time (có thể thay đổi từ 1-10s)

---

## ✅ Kiểm Tra Sau Khi Gán

### 1. Kiểm tra References
Trong Inspector của GameplayPanel, component **Answer Summary UI** phải có:
- ✅ `Text Trang Thai Dap An 1` → TextTrangThaiDapAn1
- ✅ `Text Trang Thai Dap An 2` → TextTrangThaiDapAn2
- ✅ `Timer Text` → Timertext
- ✅ `Trang Thai Text` → TimerState
- ⚪ `Result Text` → None (hoặc text object nếu có)

### 2. Kiểm tra Settings
- ✅ `Summary Duration Min` = 1
- ✅ `Summary Duration Max` = 10
- ✅ `Default Summary Duration` = 5

---

## 🎮 Cách Hoạt Động

### Question Time (10 giây)
```
TimerState: "Thời gian trả lời câu hỏi"
Timertext: "10s" → "9s" → ... → "1s" → "0s"
TextTrangThaiDapAn1: HIDDEN (ẩn)
TextTrangThaiDapAn2: HIDDEN (ẩn)
```

### Summary Time (5 giây)
```
TimerState: "Thời gian thống kê đáp án"
Timertext: "5s" → "4s" → "3s" → "2s" → "1s" → "0s"
TextTrangThaiDapAn1: "Đáp án người chơi 1 chọn là: 32 (3.2450s)" (hiển thị)
TextTrangThaiDapAn2: "Đáp án người chơi 2 chọn là: 32 (5.6780s)" (hiển thị)
Result: "Người chơi 1 đúng!" / "Người chơi 2 đúng!"
```

---

## 🔍 Troubleshooting

### Lỗi: "BattleManager not found!"
- **Nguyên nhân**: Chưa có NetworkedMathBattleManager trong scene
- **Giải pháp**: Đảm bảo BattleManager object tồn tại trong scene

### Lỗi: TextTrangThaiDapAn1/2 không ẩn
- **Nguyên nhân**: Chưa gán đúng reference
- **Giải pháp**: Kiểm tra lại reference trong Inspector

### Lỗi: TimerText không đếm ngược
- **Nguyên nhân**: Chưa gán Timertext từ TimerPanel
- **Giải pháp**: Kéo thả `TimerPanel → Timertext` vào `Timer Text` field

---

## 📸 Hình Ảnh Tham Khảo

### Inspector View
```
Answer Summary UI (Script)
├── === ANSWER DISPLAY ===
│   ├── Text Trang Thai Dap An 1: [TextTrangThaiDapAn1]
│   └── Text Trang Thai Dap An 2: [TextTrangThaiDapAn2]
├── === STATUS & TIMER (SHARED) ===
│   ├── Timer Text: [Timertext]
│   └── Trang Thai Text: [TimerState]
├── === RESULT DISPLAY ===
│   └── Result Text: [None]
└── === SETTINGS ===
    ├── Summary Duration Min: 1
    ├── Summary Duration Max: 10
    └── Default Summary Duration: 5
```

---

## ✅ Hoàn Thành!

Sau khi gán xong, bạn có thể test trong Play Mode:
1. Start multiplayer battle
2. Trả lời câu hỏi
3. Xem TextTrangThaiDapAn1/2 ẩn trong Question Time
4. Xem TextTrangThaiDapAn1/2 hiện trong Summary Time với đáp án + thời gian

**Chúc may mắn!** 🎉
