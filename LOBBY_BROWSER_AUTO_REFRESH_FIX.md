# Lobby Browser Auto-Refresh Fix

## Vấn đề
Khi client join phòng từ LobbyBrowserPanel, host vẫn ở LobbyBrowserPanel nhưng không thấy player count thay đổi. Host phải click "Refresh" button thủ công để thấy update.

## Nguyên nhân
- LobbyBrowserController chỉ refresh danh sách phòng khi:
  1. OnShow (lần đầu)
  2. User click "Refresh" button
- Không có auto-polling để refresh liên tục
- Khi client join, host không thấy vì host không refresh lại danh sách

## Giải pháp
Thêm auto-polling vào UILobbyBrowserController để refresh danh sách phòng mỗi 2 giây.

## Thay đổi

### UILobbyBrowserController.cs

**Thêm field:**
```csharp
[SerializeField] private float autoRefreshIntervalSeconds = 2f;
private Coroutine autoRefreshRoutine;
private float nextRefreshTime;
```

**Cập nhật OnShow():**
- Bắt đầu AutoRefreshRoutine khi panel hiển thị

**Cập nhật OnHide():**
- Dừng AutoRefreshRoutine khi panel ẩn

**Cập nhật OnDestroy():**
- Dừng AutoRefreshRoutine khi object bị destroy

**Thêm AutoRefreshRoutine():**
```csharp
private IEnumerator AutoRefreshRoutine()
{
    while (true)
    {
        yield return new WaitForSeconds(Mathf.Max(1f, autoRefreshIntervalSeconds));
        
        if (Time.unscaledTime >= nextRefreshTime && !isBusy)
        {
            _ = RefreshLobbyListAsync();
            nextRefreshTime = Time.unscaledTime + autoRefreshIntervalSeconds;
        }
    }
}
```

## Kết quả
- Host ở LobbyBrowserPanel sẽ tự động thấy player count thay đổi mỗi 2 giây
- Không cần click "Refresh" button
- Client join → Host thấy ngay (trong vòng 2 giây)

## Tuning
Có thể điều chỉnh `autoRefreshIntervalSeconds` trong Inspector:
- Nhỏ hơn = update nhanh hơn (nhưng tốn API quota)
- Lớn hơn = update chậm hơn (tiết kiệm API quota)
- Mặc định: 2 giây

## Files Modified
- `Assets/Script/Script_multiplayer/1Code/CODE/UILobbyBrowserController.cs`
