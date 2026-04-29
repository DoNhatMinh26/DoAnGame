# Fix Timer Management - AnswerSummaryUI Quản Lý Toàn Bộ Timer

## 🐛 Vấn Đề

Sau khi bỏ Timer Text ở MultiplayerHealthUI (set = None), timer bị lỗi:
- Question Time không hiển thị đếm ngược 10s → 0s
- Summary Time không hiển thị đúng
- Trạng thái text không đổi đúng lúc

**Nguyên nhân**: 
- MultiplayerHealthUI trước đây quản lý timer trong Question Time
- AnswerSummaryUI chỉ quản lý timer trong Summary Time
- Khi bỏ MultiplayerHealthUI timer → Không ai quản lý Question Time timer

---

## ✅ Giải Pháp

**AnswerSummaryUI giờ quản lý TOÀN BỘ timer** cho cả 2 giai đoạn:

1. **Question Time (10s)**: 
   - Trạng thái: "Thời gian trả lời câu hỏi"
   - Timer: 10s → 9s → ... → 1s → 0s
   - TextTrangThaiDapAn1/2: HIDDEN

2. **Summary Time (3s)**:
   - Trạng thái: "Thời gian thống kê đáp án"
   - Timer: 3s → 2s → 1s → 0s
   - TextTrangThaiDapAn1/2: VISIBLE + hiển thị đáp án + thời gian

---

## 🔧 Các Thay Đổi

### 1. Thêm Question Timer Coroutine

**Thêm biến**:
```csharp
private Coroutine questionTimerCoroutine;
private bool isQuestionActive = false;
```

**Thêm method**:
```csharp
/// <summary>
/// Đếm ngược thời gian Question Time (10s → 0s)
/// </summary>
private IEnumerator QuestionTimerRoutine()
{
    isQuestionActive = true;
    float questionTime = 10f; // 10 giây
    float elapsed = 0f;

    while (elapsed < questionTime)
    {
        elapsed += Time.deltaTime;
        float remaining = Mathf.Max(0, questionTime - elapsed);

        if (timerText != null)
        {
            timerText.text = $"{Mathf.CeilToInt(remaining)}s";
        }

        yield return null;
    }

    // Hết thời gian Question Time
    if (timerText != null)
    {
        timerText.text = "0s";
    }

    isQuestionActive = false;
}
```

---

### 2. Subscribe OnQuestionGenerated Event

**Trong Start()**:
```csharp
// Subscribe to events
battleManager.OnAnswerResultReceived += HandleAnswerResult;
battleManager.OnQuestionGenerated += HandleQuestionGenerated; // ← MỚI
```

**Trong OnDestroy()**:
```csharp
battleManager.OnAnswerResultReceived -= HandleAnswerResult;
battleManager.OnQuestionGenerated -= HandleQuestionGenerated; // ← MỚI
```

---

### 3. Handle Question Generated

**Thêm method mới**:
```csharp
/// <summary>
/// Gọi khi có câu hỏi mới
/// </summary>
private void HandleQuestionGenerated(string question, int[] choices)
{
    Debug.Log($"[AnswerSummaryUI] New question generated: {question}");

    // Dừng summary coroutine nếu đang chạy
    if (summaryCoroutine != null)
    {
        StopCoroutine(summaryCoroutine);
        summaryCoroutine = null;
    }

    // Dừng question timer cũ nếu có
    if (questionTimerCoroutine != null)
    {
        StopCoroutine(questionTimerCoroutine);
    }

    // Ẩn đáp án
    HideAnswerTexts();

    // Set trạng thái Question Time
    SetTrangThaiQuestionTime();

    // Bắt đầu đếm ngược Question Time
    questionTimerCoroutine = StartCoroutine(QuestionTimerRoutine());
}
```

---

### 4. Update HandleAnswerResult

**Thêm logic dừng question timer**:
```csharp
private void HandleAnswerResult(...)
{
    // Dừng question timer
    if (questionTimerCoroutine != null)
    {
        StopCoroutine(questionTimerCoroutine);
        questionTimerCoroutine = null;
    }

    // Dừng summary coroutine cũ nếu có
    if (summaryCoroutine != null)
    {
        StopCoroutine(summaryCoroutine);
    }

    // Bắt đầu giai đoạn tổng kết
    summaryCoroutine = StartCoroutine(ShowSummaryRoutine(...));
}
```

---

### 5. Update ClearSummary

**Reset timer về trạng thái ban đầu**:
```csharp
private void ClearSummary()
{
    // Ẩn đáp án
    HideAnswerTexts();
    
    // Xóa text đáp án
    if (textTrangThaiDapAn1 != null)
        textTrangThaiDapAn1.text = "";
    
    if (textTrangThaiDapAn2 != null)
        textTrangThaiDapAn2.text = "";
    
    // Xóa kết quả
    if (resultText != null)
        resultText.text = "";
    
    // Khôi phục trạng thái Question Time (sẵn sàng cho câu hỏi tiếp theo)
    SetTrangThaiQuestionTime();
    
    // Reset timer text
    if (timerText != null)
        timerText.text = "10s";
}
```

---

## 📋 Flow Hoàn Chỉnh

### 1. Câu Hỏi Mới Được Sinh

```
BattleManager.GenerateQuestion()
    ↓
OnQuestionGenerated event fired
    ↓
AnswerSummaryUI.HandleQuestionGenerated()
    ↓
- Dừng summary coroutine (nếu có)
- Dừng question timer cũ (nếu có)
- Ẩn TextTrangThaiDapAn1/2
- Set "Thời gian trả lời câu hỏi"
- Bắt đầu QuestionTimerRoutine()
    ↓
Timer đếm: 10s → 9s → ... → 1s → 0s
```

---

### 2. Hết Thời Gian / Có Kết Quả

```
BattleManager.EvaluateAnswers()
    ↓
OnAnswerResultReceived event fired
    ↓
AnswerSummaryUI.HandleAnswerResult()
    ↓
- Dừng question timer
- Dừng summary coroutine cũ (nếu có)
- Bắt đầu ShowSummaryRoutine()
    ↓
- Set "Thời gian thống kê đáp án"
- Hiển thị TextTrangThaiDapAn1/2
- Hiển thị đáp án + thời gian
- Hiển thị kết quả
- Timer đếm: 3s → 2s → 1s → 0s
    ↓
ClearSummary()
    ↓
- Ẩn TextTrangThaiDapAn1/2
- Xóa text
- Set "Thời gian trả lời câu hỏi"
- Reset timer = "10s"
```

---

## 🎯 Kết Quả Mong Đợi

### Question Time
```
┌─────────────────────────────────────┐
│     Kéo đáp án vào ô!               │
│                                     │
│  Thời gian trả lời câu hỏi          │
│                                     │
│  10s → 9s → 8s → ... → 1s → 0s     │
│                                     │
│  [TextTrangThaiDapAn1: HIDDEN]     │
│  [TextTrangThaiDapAn2: HIDDEN]     │
└─────────────────────────────────────┘
```

### Summary Time
```
┌─────────────────────────────────────┐
│     Kéo đáp án vào ô!               │
│                                     │
│  Thời gian thống kê đáp án    3s   │
│                                     │
│  Đáp án người chơi 1 chọn là:      │
│  10 (2.5000s)                      │
│                                     │
│  Đáp án người chơi 2 chọn là:      │
│  10 (4.8000s)                      │
│                                     │
│  Cả 2 đều đúng!                    │
│  Người chơi 1 nhanh hơn!           │
└─────────────────────────────────────┘
```

---

## 🔍 Cách Kiểm Tra

### Test 1: Question Time Timer

1. Chạy multiplayer battle
2. Xem "Thời gian trả lời câu hỏi" hiển thị
3. Xem timer đếm: 10s → 9s → ... → 1s → 0s
4. Kiểm tra TextTrangThaiDapAn1/2 bị ẩn

**Kết quả mong đợi**:
- ✅ "Thời gian trả lời câu hỏi" hiển thị
- ✅ Timer đếm ngược từ 10s
- ✅ TextTrangThaiDapAn1/2 không hiển thị

---

### Test 2: Summary Time Timer

1. Trả lời câu hỏi (hoặc đợi hết thời gian)
2. Xem "Thời gian thống kê đáp án" hiển thị NGAY LẬP TỨC
3. Xem timer đếm: 3s → 2s → 1s → 0s
4. Kiểm tra TextTrangThaiDapAn1/2 hiển thị với đáp án + thời gian

**Kết quả mong đợi**:
- ✅ "Thời gian thống kê đáp án" hiển thị ngay lập tức
- ✅ Timer đếm ngược từ 3s
- ✅ TextTrangThaiDapAn1/2 hiển thị đáp án + thời gian
- ✅ Kết quả hiển thị đúng

---

### Test 3: Chuyển Câu Hỏi Mới

1. Đợi Summary Time kết thúc
2. Câu hỏi mới được sinh
3. Kiểm tra timer reset về "Thời gian trả lời câu hỏi" + 10s

**Kết quả mong đợi**:
- ✅ "Thời gian trả lời câu hỏi" hiển thị
- ✅ Timer reset về 10s
- ✅ TextTrangThaiDapAn1/2 bị ẩn

---

## 📝 Ghi Chú

### Tại Sao Không Dùng MultiplayerHealthUI?

**Trước đây**:
- MultiplayerHealthUI quản lý Question Time timer
- AnswerSummaryUI quản lý Summary Time timer
- → 2 script quản lý 2 giai đoạn khác nhau

**Vấn đề**:
- User muốn bỏ Timer Text ở MultiplayerHealthUI (set = None)
- → Question Time timer không còn ai quản lý
- → Timer bị lỗi

**Giải pháp**:
- AnswerSummaryUI quản lý TOÀN BỘ timer (cả 2 giai đoạn)
- → Chỉ cần 1 script quản lý timer
- → Đơn giản hơn, dễ maintain hơn

---

### Tại Sao Không Thêm 1 Field Timer Text Nữa?

User hỏi: "hay là ở AnswerSummaryUI thêm 1 trường public như TimerText nữa để chia ra hiển thị cho đúng?"

**Trả lời**: KHÔNG CẦN!

**Lý do**:
- Chỉ có 1 timer text duy nhất trong UI
- Timer text này hiển thị cả Question Time VÀ Summary Time
- Chỉ cần đổi nội dung text theo giai đoạn:
  - Question Time: "10s" → "0s"
  - Summary Time: "3s" → "0s"
- → Không cần 2 field riêng biệt

**Nếu thêm 2 field**:
- Phải có 2 text objects trong UI
- Phải ẩn/hiện 2 objects
- Phức tạp hơn, không cần thiết

---

## ✅ Kết Luận

**Đã fix**:
- ✅ AnswerSummaryUI giờ quản lý toàn bộ timer (cả Question Time và Summary Time)
- ✅ Question Time: "Thời gian trả lời câu hỏi" + 10s → 0s
- ✅ Summary Time: "Thời gian thống kê đáp án" + 3s → 0s
- ✅ Timer chuyển đổi mượt mà giữa 2 giai đoạn
- ✅ Không cần Timer Text ở MultiplayerHealthUI nữa

**File đã thay đổi**:
- `Assets/Script/Script_multiplayer/1Code/Multiplay/AnswerSummaryUI.cs`
- `HUONG_DAN_GAN_ANSWER_SUMMARY_UI.md`

**Hãy test và cho tôi biết kết quả!** 🚀
