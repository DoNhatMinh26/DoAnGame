# Fix: Host không thấy Client join khi back từ LobbyBrowserPanel

## Vấn đề gốc
- **Host** tạo phòng → click "DS Room" → xem danh sách phòng ở `LobbyBrowserPanel`
- **Client** join vào phòng của host
- **Host** click Back → về `LobbyPanel` → **VẪN KHÔNG THẤY CLIENT**
- Nguyên nhân: `ForceRefreshOnShow()` được gọi với `_ = ` (fire-and-forget) → không chờ refresh xong → UI refresh với data cũ

## Giải pháp

### 1. Thay đổi `OnShow()` thành dùng Coroutine
**File**: `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerRoomController.cs`

**Trước (SAI):**
```csharp
protected override void OnShow()
{
    // ...
    if (currentLobby != null)
    {
        _ = ForceRefreshOnShow();  // ❌ Fire-and-forget - không chờ
    }
    
    RefreshAuthState();  // ❌ Chạy ngay, data vẫn cũ
    // ...
}
```

**Sau (ĐÚNG):**
```csharp
protected override void OnShow()
{
    // ...
    if (currentLobby != null)
    {
        StopRoutines();
        StartCoroutine(OnShowRefreshRoutine());  // ✅ Chờ refresh xong
    }
    else
    {
        RefreshAuthState();
        // ...
        pollingRoutine = StartCoroutine(PollLobbyRoutine());
    }
}

private IEnumerator OnShowRefreshRoutine()
{
    yield return StartCoroutine(ForceRefreshOnShowCoroutine());
    
    // Sau khi refresh xong, init polling
    EnsureInitialized();
    UpdateReadyButtonState();
    pollingRoutine = StartCoroutine(PollLobbyRoutine());
}

private IEnumerator ForceRefreshOnShowCoroutine()
{
    var task = ForceRefreshOnShow();
    while (!task.IsCompleted)
    {
        yield return null;
    }
    
    if (task.IsFaulted)
    {
        Debug.LogError($"[UIRoom] ForceRefreshOnShow failed: {task.Exception}");
    }
}
```

### 2. `ForceRefreshOnShow()` vẫn giữ nguyên
- Bypass rate limit: `nextLobbyReadAt = 0f`
- Gọi `GetLobbyAsync()` để lấy snapshot mới
- Refresh UI: `RefreshAuthState()` → `RefreshRoomRoster()` → `UpdateReadyButtonState()`

## Luồng chạy sau fix

```
Host back từ LobbyBrowserPanel → LobbyPanel
  ↓
OnShow() được gọi
  ↓
StartCoroutine(OnShowRefreshRoutine())
  ↓
ForceRefreshOnShowCoroutine() chạy
  ↓
await ForceRefreshOnShow() → GetLobbyAsync() → lấy snapshot mới
  ↓
currentLobby.Players.Count = 2 ✅
  ↓
RefreshAuthState() → RefreshRoomRoster() → HOST THẤY 2 PLAYERS ✅
  ↓
Polling chạy lại (0.5s interval)
  ↓
Client click "Sẵn sàng" → UpdatePlayerAsync(Player2ReadyKey = "1")
  ↓
Polling detect → UpdateStartMatchButtonState() → check IsClientReady() → TRUE ✅
  ↓
NÚT "BẮT ĐẦU" HIỂN THỊ ✅
```

## Kết quả mong đợi
- ✅ Host back từ `LobbyBrowserPanel` → thấy client đã join
- ✅ Host thấy nút "Bắt đầu" khi client click "Sẵn sàng"
- ✅ Không còn tình trạng host chỉ thấy 1 player

## Compile status
✅ Build thành công, không có errors

## Testing checklist
- [ ] Host tạo phòng → click "DS Room" → xem danh sách
- [ ] Client join vào phòng của host
- [ ] Host click Back → về LobbyPanel → thấy 2 players ✅
- [ ] Client click "Sẵn sàng"
- [ ] Host thấy nút "Bắt đầu" ✅
- [ ] Host click "Bắt đầu" → vào battle ✅
