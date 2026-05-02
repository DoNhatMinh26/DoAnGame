# FIX: Host không hiển thị màu đáp án

## VẤN ĐỀ
- **Client**: Hiển thị màu đáp án bình thường (xanh = đúng, đỏ = sai)
- **Host**: KHÔNG hiển thị màu đáp án (kể cả khi không kéo đáp án vào slot)

## NGUYÊN NHÂN
Phân tích log:
- **Client log**: `[BattleController] [CLIENT] HandleAnswerResult CALLED` ✅
- **Host log**: `[BattleManager] Client received answer result` nhưng **KHÔNG có** `[BattleController] [HOST] HandleAnswerResult CALLED` ❌

**Root cause:**
```csharp
[ClientRpc]
private void NotifyAnswerResultClientRpc(...)
{
    OnAnswerResult?.Invoke(...);  // ❌ CHỈ chạy trên CLIENT, KHÔNG chạy trên HOST!
}
```

- `[ClientRpc]` chỉ execute trên **Clients**, không execute trên **Host/Server**
- Host gọi `NotifyAnswerResultClientRpc()` để gửi message đến Clients
- Nhưng Host **KHÔNG tự nhận** message này → Event không được invoke trên Host
- → `HandleAnswerResult()` không được gọi trên Host
- → Không hiển thị màu đáp án trên Host

## GIẢI PHÁP
Thêm **direct event invocation** trên Host/Server side ngay sau khi gọi ClientRpc:

### File: `NetworkedMathBattleManager.cs`

**Trước (SAI):**
```csharp
// Notify clients với winnerId chính xác
NotifyAnswerResultClientRpc(winnerId, true, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);
// ❌ Host KHÔNG nhận được event!
```

**Sau (ĐÚNG):**
```csharp
// Notify clients với winnerId chính xác
NotifyAnswerResultClientRpc(winnerId, true, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);

// ✅ FIX: Invoke event trên Host (ClientRpc không chạy trên Host)
OnAnswerResult?.Invoke(winnerId, true, player1ResponseTime);
OnAnswerResultReceived?.Invoke(winnerId, true, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);
```

**Áp dụng cho 3 trường hợp:**
1. Có người thắng (`winnerId >= 0`)
2. Hòa (`winnerId == -2`)
3. Cả 2 sai (`winnerId == -1`)

## KẾT QUẢ
- ✅ Host hiển thị màu đáp án: Xanh = đúng, Đỏ = sai
- ✅ Client hiển thị màu đáp án: Xanh = đúng, Đỏ = sai
- ✅ Đồng bộ cho cả Host và Client
- ✅ Compile thành công: 0 errors, 41 warnings (warnings là của Unity packages, không ảnh hưởng)

## KIẾN THỨC RPC
### ClientRpc
- Gọi từ **Server/Host**
- Execute trên **TẤT CẢ Clients** (không bao gồm Host)
- Dùng để gửi data từ Server → Clients

### ServerRpc
- Gọi từ **Client**
- Execute trên **Server/Host**
- Dùng để gửi data từ Client → Server

### Pattern đúng cho Host + Clients
```csharp
// Trên Server/Host
void SomeServerMethod()
{
    // 1. Gửi đến Clients
    NotifySomethingClientRpc(data);
    
    // 2. Xử lý trên Host (vì ClientRpc không chạy trên Host)
    OnSomethingHappened?.Invoke(data);
}
```

## TEST
1. Chạy Host và Client (ParrelSync)
2. Cả 2 player trả lời câu hỏi
3. Kiểm tra:
   - ✅ Host thấy màu đáp án (xanh/đỏ)
   - ✅ Client thấy màu đáp án (xanh/đỏ)
   - ✅ Màu hiển thị đúng: Đáp án đúng = xanh, đáp án sai = đỏ
   - ✅ Không phụ thuộc vào player chọn gì

## FILES MODIFIED
- `Assets/Script/Script_multiplayer/1Code/Multiplay/NetworkedMathBattleManager.cs`
  - Thêm Host-side event invocation sau mỗi `NotifyAnswerResultClientRpc()` call (3 chỗ)
