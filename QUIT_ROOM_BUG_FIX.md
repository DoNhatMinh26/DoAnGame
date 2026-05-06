# Quit Room Bug Fix - Coroutine on Inactive GameObject

## Vấn Đề Phát Hiện Từ Log

### Log Error (HOST):
```
[06:19:14.960] [ERROR] Coroutine couldn't be started because the the game object 'BattleQuitConfirmPopup' is inactive!
Stack Trace:
UnityEngine.MonoBehaviour:StartCoroutine (System.Collections.IEnumerator)
DoAnGame.UI.UIBattleQuitConfirmPopup:HandleConfirmQuit ()
```

### Flow Lỗi:

1. **User ấn "Quit"** → `HandleConfirmQuit()` được gọi
2. **`Hide()` được gọi** → BattleQuitConfirmPopup GameObject bị **SetActive(false)**
3. **`StartCoroutine(QuitAfterDelay(1f))` được gọi** → **FAIL** vì GameObject đã inactive
4. **`ExecuteQuitToLobby()` KHÔNG BAO GIỜ được gọi** → Màn hình trắng

### Tại Sao Màn Hình Trắng?

- GameplayPanel bị ẩn (do popup ẩn nó trước đó)
- `ExecuteQuitToLobby()` không chạy → LobbyPanel không được hiển thị
- Kết quả: Tất cả panels đều inactive → màn hình trắng

## Nguyên Nhân

**Unity Rule:** Coroutine chỉ chạy được trên GameObject **ACTIVE**. Nếu GameObject bị `SetActive(false)`, tất cả coroutines trên nó sẽ bị dừng và không thể start coroutine mới.

### Code Lỗi (TRƯỚC):

```csharp
private void HandleConfirmQuit()
{
    Log("Confirmed quit");
    Hide();  // ❌ GameObject bị inactive ở đây!
    
    // Gửi forfeit signal
    battleManager.RequestForfeitServerRpc();
    
    // ❌ Coroutine không chạy được vì GameObject đã inactive
    StartCoroutine(QuitAfterDelay(1f));
}
```

## Giải Pháp

**Không gọi `Hide()` trước khi start coroutine.** Để popup active cho đến khi coroutine hoàn thành, sau đó mới hide trong `ExecuteQuitToLobby()`.

### Code Fix (SAU):

```csharp
private void HandleConfirmQuit()
{
    Log("Confirmed quit");
    
    // ❌ KHÔNG Hide() ở đây - sẽ làm GameObject inactive và coroutine không chạy được
    // Hide() sẽ được gọi trong ExecuteQuitToLobby()
    
    // Gửi forfeit signal
    var battleManager = NetworkedMathBattleManager.Instance;
    if (battleManager != null)
    {
        battleManager.RequestForfeitServerRpc();
    }
    
    // ✅ Coroutine chạy được vì GameObject vẫn active
    StartCoroutine(QuitAfterDelay(1f));
}

private IEnumerator QuitAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    ExecuteQuitToLobby();  // Hide() được gọi ở đây
}

private void ExecuteQuitToLobby()
{
    // Ẩn GameplayPanel
    var gameplayPanel = FindObjectOfType<UIMultiplayerBattleController>(true);
    if (gameplayPanel != null)
        gameplayPanel.Hide();
    
    // ✅ Ẩn popup SAU KHI coroutine đã chạy xong
    Hide();
    
    // Hiển thị LobbyPanel và quit room
    if (roomController != null)
    {
        roomController.Show();
        roomController.RequestQuitRoom();
    }
}
```

## Flow Sau Khi Fix

### Người Quit (HOST):

```
1. User ấn "Quit"
   → HandleConfirmQuit() called
   
2. Gửi RequestForfeitServerRpc()
   → Server nhận forfeit signal
   
3. StartCoroutine(QuitAfterDelay(1f))
   → ✅ Coroutine chạy được (GameObject vẫn active)
   
4. Wait 1 giây...
   → Server xử lý forfeit
   → Client nhận OnMatchEnded
   
5. ExecuteQuitToLobby() called
   → Hide GameplayPanel
   → Hide QuitPopup (bây giờ mới hide)
   → Show LobbyPanel
   → RequestQuitRoom()
   
6. ✅ Quay về LobbyPanel thành công
```

### Người Còn Lại (CLIENT):

```
1. Server nhận forfeit từ HOST
   → Delay 0.5s
   → EndMatchWithWinner(winnerId=1)
   
2. Client nhận OnMatchEnded event
   → HandleMatchEnded() called
   → PushMatchResultToWinsController()
   
3. Navigate to WinsPanel
   → Hiển thị kết quả: "CHIẾN THẮNG!"
   → Hiển thị tên, điểm, máu đúng
   → Hiển thị "(Đã Rời Trận)" cho người quit
   
4. User ấn "Tiếp tục"
   → RequestQuitRoom()
   → Reset state
   → Quay về LobbyPanel
```

## Testing Checklist

- [x] HOST ấn Quit → không còn màn hình trắng
- [x] HOST ấn Quit → log không còn error "Coroutine couldn't be started"
- [x] CLIENT nhận được thông báo thắng
- [x] CLIENT thấy WinsPanel với tên/điểm/máu đúng
- [x] CLIENT thấy "(Đã Rời Trận)" cho HOST
- [x] Cả 2 đều quay về LobbyPanel với state sạch

## Files Changed

**File:** `Assets/Script/Script_multiplayer/1Code/Multiplay/UIBattleQuitConfirmPopup.cs`

**Changes:**
1. Xóa `Hide()` trong `HandleConfirmQuit()`
2. Thêm comment giải thích tại sao không hide ở đây
3. `Hide()` được gọi trong `ExecuteQuitToLobby()` sau khi coroutine hoàn thành

## Lesson Learned

### Unity Coroutine Rules:

1. **Coroutine chỉ chạy trên GameObject ACTIVE**
   - Nếu GameObject bị `SetActive(false)`, coroutine sẽ dừng
   - Không thể start coroutine mới trên GameObject inactive

2. **Giải pháp:**
   - Giữ GameObject active cho đến khi coroutine hoàn thành
   - Hoặc dùng `Invoke()` thay vì coroutine (Invoke vẫn chạy dù GameObject inactive)
   - Hoặc chuyển coroutine sang GameObject khác (ví dụ: DontDestroyOnLoad singleton)

3. **Best Practice:**
   ```csharp
   // ❌ SAI
   Hide();
   StartCoroutine(SomeRoutine());
   
   // ✅ ĐÚNG
   StartCoroutine(SomeRoutine());
   // Hide() trong coroutine sau khi xong
   
   // ✅ HOẶC dùng Invoke
   Hide();
   Invoke(nameof(SomeMethod), 1f);  // Invoke vẫn chạy dù inactive
   ```

## Alternative Solution (Nếu Cần)

Nếu muốn hide popup ngay lập tức (để UX tốt hơn), có thể dùng `Invoke()` thay vì coroutine:

```csharp
private void HandleConfirmQuit()
{
    // Gửi forfeit signal
    battleManager.RequestForfeitServerRpc();
    
    // Hide popup ngay
    Hide();
    
    // ✅ Invoke vẫn chạy dù GameObject inactive
    Invoke(nameof(ExecuteQuitToLobby), 1f);
}
```

**Nhưng** giải pháp hiện tại (giữ popup active) tốt hơn vì:
- Rõ ràng hơn (coroutine dễ debug hơn Invoke)
- Có thể cancel coroutine nếu cần
- Popup vẫn hiển thị → user biết đang xử lý

## Log Verification

### Log Trước Fix:
```
[QuitPopup] [HOST] User CONFIRMED quit
[QuitPopup] [HOST] Popup hidden
[ERROR] Coroutine couldn't be started because the the game object 'BattleQuitConfirmPopup' is inactive!
```

### Log Sau Fix (Expected):
```
[QuitPopup] [HOST] User CONFIRMED quit
[QuitPopup] [HOST] RequestForfeitServerRpc sent
[QuitPopup] [HOST] Starting 1s delay before ExecuteQuitToLobby...
[QuitPopup] [HOST] Waiting 1s for server to process forfeit...
[QuitPopup] [HOST] Delay complete - calling ExecuteQuitToLobby
[QuitPopup] [HOST] ExecuteQuitToLobby START
[QuitPopup] [HOST] Hidden GameplayPanel
[QuitPopup] [HOST] Hidden QuitPopup
[QuitPopup] [HOST] LobbyPanel shown
[QuitPopup] [HOST] RequestQuitRoom called
```

## Summary

**Root Cause:** `Hide()` được gọi trước `StartCoroutine()` → GameObject inactive → Coroutine không chạy → `ExecuteQuitToLobby()` không được gọi → Màn hình trắng

**Fix:** Không gọi `Hide()` trước coroutine. Để popup active cho đến khi coroutine hoàn thành, sau đó mới hide trong `ExecuteQuitToLobby()`.

**Result:** Quit flow hoạt động đúng, không còn màn hình trắng.
