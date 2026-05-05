# Fix: Host không thấy Client join vào phòng

## Vấn đề
- **HOST** luôn chỉ thấy 1 player trong roster, không bao giờ thấy client join
- **CLIENT** thấy đầy đủ 2 players (host + client)
- Nguyên nhân: Unity Lobby Service **không tự động push update** đến host khi client join
- Host phải **chủ động gọi `GetLobbyAsync()`** để lấy snapshot mới

## Giải pháp đã implement

### 1. Thêm nút "Làm mới" thủ công
- **File**: `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerRoomController.cs`
- **Thay đổi**:
  - Thêm field `[SerializeField] private Button refreshButton;`
  - Thêm listener trong `Awake()`: `refreshButton?.onClick.AddListener(() => _ = HandleRefreshButton());`
  - Thêm method `HandleRefreshButton()`:
    - Bypass rate limit bằng cách reset `nextLobbyReadAt = 0f`
    - Force refresh lobby: `await LobbyService.Instance.GetLobbyAsync(currentLobby.Id)`
    - Refresh toàn bộ UI: `RefreshAuthState()`
  - Thêm public method `RequestRefresh()` để gọi từ Inspector
  - Update `SetActionButtonsInteractable()` để show/hide nút Làm mới:
    - Chỉ hiển thị khi đang ở trong room (`isInLobbySession`)
    - Ẩn khi chưa tạo/join phòng

### 2. Tăng tần suất auto-polling khi chờ player 2
- **File**: `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerRoomController.cs`
- **Thay đổi**:
  - Update `PollLobbyRoutine()`:
    - Poll **0.5s** khi chỉ có 1 player (đang chờ player 2)
    - Poll `pollIntervalSeconds` (1.5s) khi đã đủ 2 players
  - Logic:
    ```csharp
    int playerCount = currentLobby?.Players?.Count ?? 0;
    float interval = (playerCount < MaxPlayers) ? 0.5f : pollIntervalSeconds;
    yield return new WaitForSeconds(Mathf.Max(0.5f, interval));
    ```

## Cách sử dụng

### Trong Unity Inspector:
1. Mở scene `Test_FireBase_multi.unity`
2. Chọn GameObject có `UIMultiplayerRoomController`
3. Trong Inspector, tìm field **"Refresh Button"**
4. Gán Button "Làm mới" vào field này
5. Button sẽ tự động show/hide khi vào/rời phòng

### Khi chạy game:
- **Host tạo phòng** → Nút "Làm mới" xuất hiện
- **Client join** → Host có thể click "Làm mới" để force refresh roster
- **Auto-refresh** cũng chạy nhanh hơn (0.5s) khi chờ player 2

## Kết quả mong đợi
- Host click "Làm mới" → Thấy client trong roster ngay lập tức
- Auto-refresh nhanh hơn → Host thấy client sau tối đa 0.5s (không cần click)
- Không còn tình trạng host chỉ thấy 1 player mãi

## Testing checklist
- [ ] Host tạo phòng → Nút "Làm mới" hiển thị
- [ ] Client join → Host click "Làm mới" → Thấy 2 players
- [ ] Client join → Đợi 0.5s → Host tự động thấy 2 players (không cần click)
- [ ] Host rời phòng → Nút "Làm mới" ẩn đi
- [ ] Nút "Làm mới" không hiển thị khi chưa tạo/join phòng

## Compile status
✅ Build thành công, không có errors (chỉ có warnings từ Unity packages)
