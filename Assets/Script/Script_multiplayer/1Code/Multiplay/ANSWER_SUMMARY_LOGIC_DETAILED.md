# Answer Summary Logic - Chi Tiết Theo Miligiây

## 📋 Tổng Quan

Hệ thống so sánh kết quả dựa trên **miligiây (ms)** để đảm bảo độ chính xác cao nhất.

---

## 🎯 Giai Đoạn 1: Trả Lời Câu Hỏi (Question Time)

**Thời Gian:** 10 giây (10000ms)

**Trạng Thái:**
```
Trạng Thái Text: "Thời gian trả lời câu hỏi"
TimerText: "10s" → "9s" → ... → "1s" → "0s"
```

**Hiển Thị:**
- ✅ Câu hỏi
- ✅ 4 đáp án
- ❌ TextTrangThaiDapAn1 = ẩn
- ❌ TextTrangThaiDapAn2 = ẩn
- ❌ Kết quả = ẩn

**Dữ Liệu Lưu Trữ:**
```
QuestionStartTimestamp = T (miligiây khi câu hỏi sinh ra)

Khi P1 submit:
  P1_SubmitTime = T1 (miligiây khi P1 submit)
  P1_ResponseTime = T1 - T (miligiây)
  P1_Answer = 5

Khi P2 submit:
  P2_SubmitTime = T2 (miligiây khi P2 submit)
  P2_ResponseTime = T2 - T (miligiây)
  P2_Answer = 7
```

---

## 🎯 Giai Đoạn 2: Kết Toán Đáp Án (Summary Time)

**Kích Hoạt Khi:**
- Cả 2 người chơi đã trả lời, HOẶC
- Hết 10 giây (timeout)

**Thời Gian Kết Toán:** 3 giây (3000ms)

**Trạng Thái:**
```
Trạng Thái Text: "Thời gian thống kê đáp án"
TimerText: "3s" → "2s" → "1s" → "0s"
```

**Hiển Thị:**
- ✅ TextTrangThaiDapAn1 = hiển thị
- ✅ TextTrangThaiDapAn2 = hiển thị
- ✅ Kết quả = hiển thị

---

## 📊 Logic So Sánh (Theo Miligiây)

### Scenario 1: Cả 2 Đều Đúng

```
P1_Answer = 5 (Đúng)
P2_Answer = 5 (Đúng)
P1_ResponseTime = 3000ms
P2_ResponseTime = 5000ms

So sánh:
if (P1_ResponseTime < P2_ResponseTime)
    Winner = P1 (3000ms < 5000ms)
else if (P2_ResponseTime < P1_ResponseTime)
    Winner = P2
else
    Winner = Draw (cùng thời gian)

Kết Quả:
  Đáp án P1 chọn là: 5
  Đáp án P2 chọn là: 5
  Người chơi 1 đúng! (3000ms) ← Thắng
  Người chơi 2 đúng! (5000ms)
  
  P1: +10 điểm
  P2: -1 máu
```

### Scenario 2: 1 Người Đúng, 1 Người Sai

```
P1_Answer = 5 (Đúng)
P2_Answer = 7 (Sai)
P1_ResponseTime = 3000ms
P2_ResponseTime = 4000ms

Logic:
if (P1_correct && !P2_correct)
    Winner = P1
else if (!P1_correct && P2_correct)
    Winner = P2

Kết Quả:
  Đáp án P1 chọn là: 5
  Đáp án P2 chọn là: 7
  Người chơi 1 đúng! (3000ms) ← Thắng
  Người chơi 2 sai!
  
  P1: +10 điểm
  P2: -1 máu
```

### Scenario 3: Cả 2 Đều Sai

```
P1_Answer = 5 (Sai)
P2_Answer = 7 (Sai)
P1_ResponseTime = 3000ms
P2_ResponseTime = 4000ms

Logic:
if (!P1_correct && !P2_correct)
    Winner = None

Kết Quả:
  Đáp án P1 chọn là: 5
  Đáp án P2 chọn là: 7
  Cả 2 đều sai!
  
  P1: -1 máu
  P2: -1 máu
```

### Scenario 4: 1 Người Trả Lời, 1 Người Timeout

```
P1_Answer = 5 (Đúng)
P2_Answer = -1 (Không trả lời)
P1_ResponseTime = 3000ms
P2_ResponseTime = 10000ms (timeout)

Logic:
if (P1_correct && P2_timeout)
    Winner = P1

Kết Quả:
  Đáp án P1 chọn là: 5
  Đáp án P2 chọn là: (Không trả lời)
  Người chơi 1 đúng! (3000ms) ← Thắng
  Người chơi 2 không trả lời!
  
  P1: +10 điểm
  P2: -1 máu
```

### Scenario 5: Cả 2 Timeout

```
P1_Answer = -1 (Không trả lời)
P2_Answer = -1 (Không trả lời)
P1_ResponseTime = 10000ms (timeout)
P2_ResponseTime = 10000ms (timeout)

Logic:
if (P1_timeout && P2_timeout)
    Winner = None

Kết Quả:
  Đáp án P1 chọn là: (Không trả lời)
  Đáp án P2 chọn là: (Không trả lời)
  Cả 2 đều không trả lời!
  
  P1: -1 máu
  P2: -1 máu
```

### Scenario 6: Cả 2 Đúng Cùng Thời Gian

```
P1_Answer = 5 (Đúng)
P2_Answer = 5 (Đúng)
P1_ResponseTime = 3000ms
P2_ResponseTime = 3000ms

So sánh:
if (P1_ResponseTime == P2_ResponseTime)
    Winner = Draw

Kết Quả:
  Đáp án P1 chọn là: 5
  Đáp án P2 chọn là: 5
  Người chơi 1 đúng! (3000ms)
  Người chơi 2 đúng! (3000ms)
  Hòa! Cả 2 trả lời cùng lúc!
  
  P1: +5 điểm (nửa điểm)
  P2: +5 điểm (nửa điểm)
```

---

## 🔧 Code Logic

```csharp
// Lấy thời gian trả lời (miligiây)
long P1_ResponseTime = P1_SubmitTime - QuestionStartTimestamp;
long P2_ResponseTime = P2_SubmitTime - QuestionStartTimestamp;

// So sánh kết quả
int winnerId = -1;
bool correct = false;
long responseTimeMs = 0;

if (P1_correct && P2_correct)
{
    // Cả 2 đúng → So sánh thời gian (ms)
    if (P1_ResponseTime < P2_ResponseTime)
    {
        winnerId = 0; // P1 thắng
        responseTimeMs = P1_ResponseTime;
    }
    else if (P2_ResponseTime < P1_ResponseTime)
    {
        winnerId = 1; // P2 thắng
        responseTimeMs = P2_ResponseTime;
    }
    else
    {
        winnerId = -2; // Hòa
        responseTimeMs = P1_ResponseTime;
    }
    correct = true;
}
else if (P1_correct && !P2_correct)
{
    // P1 đúng, P2 sai
    winnerId = 0;
    responseTimeMs = P1_ResponseTime;
    correct = true;
}
else if (!P1_correct && P2_correct)
{
    // P1 sai, P2 đúng
    winnerId = 1;
    responseTimeMs = P2_ResponseTime;
    correct = true;
}
else
{
    // Cả 2 sai
    winnerId = -1;
    responseTimeMs = 0;
    correct = false;
}

// Gửi kết quả về client
NotifyAnswerResultClientRpc(winnerId, correct, responseTimeMs, P1_Answer, P2_Answer);
```

---

## 📊 Hiển Thị Kết Quả (Miligiây)

### Ví Dụ 1: P1 Thắng
```
Đáp án P1 chọn là: 5
Đáp án P2 chọn là: 5
Người chơi 1 đúng! (3245ms) ← Thời gian chính xác
Người chơi 2 đúng! (5678ms)
```

### Ví Dụ 2: P2 Thắng
```
Đáp án P1 chọn là: 5
Đáp án P2 chọn là: 7
Người chơi 1 sai!
Người chơi 2 đúng! (4123ms) ← Thời gian chính xác
```

### Ví Dụ 3: Hòa
```
Đáp án P1 chọn là: 5
Đáp án P2 chọn là: 5
Hòa! Cả 2 trả lời cùng lúc! (3000ms)
```

---

## ✅ Độ Chính Xác

| Đơn Vị | Độ Chính Xác | Ví Dụ |
|--------|-------------|-------|
| Giây (s) | ±1s | "3s" (có thể 2.5-3.5s) |
| Miligiây (ms) | ±1ms | "3245ms" (chính xác) |

**Kết Luận:** Dùng **miligiây (ms)** đảm bảo độ chính xác cao nhất, không có tranh cãi.

---

## 🔐 Đảm Bảo Logic

✅ **Chính xác** - Tính theo miligiây
✅ **Công bằng** - So sánh thời gian khi cả 2 đúng
✅ **Rõ ràng** - Hiển thị thời gian chính xác
✅ **Bảo mật** - Ẩn đáp án trong giai đoạn trả lời

---

## 📝 Timeline Chi Tiết (Miligiây)

```
T=0ms: Câu hỏi sinh ra
  QuestionStartTimestamp = 0

T=3245ms: P1 submit
  P1_SubmitTime = 3245
  P1_ResponseTime = 3245 - 0 = 3245ms
  P1_Answer = 5

T=5678ms: P2 submit
  P2_SubmitTime = 5678
  P2_ResponseTime = 5678 - 0 = 5678ms
  P2_Answer = 5
  
  → Server: P1_ResponseTime (3245ms) < P2_ResponseTime (5678ms)
  → Winner = P1

T=5678ms+: Gửi kết quả
  NotifyAnswerResultClientRpc(
    winnerId: 0,
    correct: true,
    responseTimeMs: 3245,
    player1Answer: 5,
    player2Answer: 5
  )

T=5678ms+: Hiển thị kết quả
  Trạng Thái: "Thời gian thống kê đáp án"
  Đáp án P1: 5
  Đáp án P2: 5
  Kết quả: "Người chơi 1 đúng! (3245ms)"
  TimerText: "3s" → "2s" → "1s" → "0s"

T=8678ms: Kết toán kết thúc
  Trạng Thái: "Thời gian trả lời câu hỏi"
  TimerText: "10s" (khôi phục)
```

---

**Status**: ✅ Logic Chính Xác Theo Miligiây
**Last Updated**: 2026-04-29
