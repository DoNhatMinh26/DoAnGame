# Hướng Dẫn Setup Nhanh Audio Theo Scene

Tài liệu này gom nhanh các object chính trong 5 scene đã export để bạn setup audio nhanh, không phải mở lại từng scene để đoán object nào cần gắn.

## 1. Setup chung cho toàn project

1. Chỉ giữ 1 `AudioManager` duy nhất trong project.
2. Nếu dùng `AudioEventBridge`, mỗi scene nên có 1 bridge riêng.
3. `AudioManager` nên có:
   - `soundClick`
   - `soundCorrect`
   - `soundWrong`
   - `soundWin`
   - `soundLose`
   - `countdown3Clip`
   - `countdown2Clip`
   - `countdown1Clip`
   - `countdownReadyClip`
   - `countdownGoClip`
   - `musicMenu`
   - `musicClassMode`
   - `musicDefenseMode`
   - `musicSpaceMode`
   - `musicMultiplayer`

4. Button click nên đi qua một điểm trung gian duy nhất, ưu tiên `UIButtonScreenNavigator` hoặc `AudioEventBridge`.

## 2. Scene `ChonDA`

### Mục tiêu
- Chọn chế độ chơi.
- Gán âm click cho các nút menu.
- Phát nhạc menu.

### Object nên gắn audio
- `UiClass`
- `StBnt`
- `Back`
- `Play`
- `MuaBtn`
- Các button trong panel `Ui`

### Gợi ý setup nhanh
- Khi scene mở: phát `musicClassMode` hoặc `musicMenu` tùy flow menu của bạn.
- Khi bấm `StBnt`, `Back`, `Play`, `MuaBtn`: phát `soundClick`.
- Nếu mở panel/shop: có thể dùng cùng click sound, chưa cần âm khác.

## 3. Scene `GameUIPlay 1`

### Mục tiêu
- Menu chính, đăng nhập, đăng ký, quick play, setting.

### Object nên gắn audio
- `PlayBtn`
- `Setting`
- `Back`
- `ChoiNhanh_ChoiMoi`
- `DangKy`
- `DangNhap`
- `ChoiDonBtn`
- `multiplayerBtn`
- `BackBtn`
- `LoginButton`
- `CancelButton`
- `GuiEmailBtn`
- `HoanThanhBtn (1)`
- `DangKyBtn`
- `QuenMatKhauBtn`
- `ChoiLai` nếu panel nào có.

### Gợi ý setup nhanh
- Scene này nên phát `musicMenu`.
- Tất cả nút UI dùng `soundClick`.
- Khi mở `SettingPanel`, slider chỉ điều chỉnh volume, không cần thêm sound riêng.
- Nếu `StatusText` hoặc `ErrorText` đổi nội dung, không cần gắn audio.

## 4. Scene `KeoThaDA`

### Mục tiêu
- Game kéo thả, trả lời đúng/sai, win/lose.

### Object nên gắn audio
- `setting`
- `Back`
- `Play`
- `MuaBtn`
- `Pt1Bnt`
- `Pt2Bnt`
- `Pt3Bnt`
- `ThoatBtn`
- `ChoiLai`
- `TiepTucBtn`
- `MuaBtn` trong shop panel
- `Answer_0`
- `Answer_1`
- `Answer_2`

### Gợi ý setup nhanh
- Khi vào gameplay: phát `musicDefenseMode`.
- Khi bắt đầu màn chơi: nếu có countdown, dùng `countdown3/2/1/Ready/Go`.
- Khi trả lời đúng: `soundCorrect`.
- Khi trả lời sai: `soundWrong`.
- Khi thắng: `soundWin`.
- Khi thua: `soundLose`.
- Khi bấm nút UI: `soundClick`.

## 5. Scene `PhiThuyen`

### Mục tiêu
- Game phi thuyền, shop, win panel.

### Object nên gắn audio
- `Play`
- `Back`
- `MuaBtn`
- `Pt1Bnt`
- `Pt2Bnt`
- `Pt3Bnt`
- `reSet`
- `ThoatBtn`
- `ChoiLai`
- `TiepTucBtn`
- `StBnt`

### Gợi ý setup nhanh
- Khi vào gameplay: phát `musicSpaceMode`.
- Khi vào shop/menu: dùng lại `musicMenu` nếu cần.
- Nút UI: `soundClick`.
- Nếu có trạng thái thắng: `soundWin`.
- Nếu có trạng thái thua: `soundLose`.

## 6. Scene `Test_FireBase_multi`

### Mục tiêu
- Multiplayer lobby + gameplay + wins/lost + timer.

### LobbyPanel
Object nên gắn audio:
- `HostButton`
- `JoinButton`
- `Back`
- `QuickButton`
- `QuitButton`
- `DS Room`
- `SanSangButton`
- `StartButton`
- `RefreshButton`
- `BackToModSelectionNavigator`

### GameplayPanel
Object nên gắn audio:
- `Quit`
- `TiepTucBtn`
- `ANSWER_0`
- `ANSWER_0 (1)`
- `ANSWER_0 (2)`
- `ANSWER_0 (3)`

### Wins panel
Object nên gắn audio:
- `TiepTucBtn`

### Gợi ý setup nhanh
- Khi ở lobby: phát `musicMultiplayer` hoặc `musicMenu`.
- Khi vào trận: phát `musicMultiplayer`.
- Countdown của trận: `countdown3/2/1/Ready/Go`.
- Câu trả lời đúng/sai: `soundCorrect` và `soundWrong`.
- Kết thúc trận: `soundWin` hoặc `soundLose` tùy kết quả local.
- Button click toàn bộ lobby/gameplay: `soundClick`.

## 7. Cách setup nhanh nhất nếu dùng `AudioEventBridge`

1. Mỗi scene thêm 1 GameObject `AudioBridge`.
2. Add component `AudioEventBridge`.
3. Kéo đúng object text của scene đó vào nếu bridge yêu cầu.
4. Bật `autoHookButtons` để tự gắn click cho toàn bộ Button.
5. Với scene multiplayer, ưu tiên đặt đúng object:
   - Countdown text: `Text (TMP) TrangThai`
   - Timer text: `Timertext` hoặc `TimerState`
   - Result text: `TrangThaiWin` hoặc text hiển thị win/lose

## 8. Thứ tự ưu tiên nên làm

1. Menu/scene chính: `musicMenu` + `soundClick`
2. Multiplayer: `musicMultiplayer` + countdown + correct/wrong
3. Defense/PhiThuyen: `musicDefenseMode` / `musicSpaceMode`
4. Win/Lose: `soundWin` / `soundLose`

## 9. Checklist cực ngắn

- [ ] Có 1 `AudioManager` duy nhất.
- [ ] Có `soundClick`, `soundCorrect`, `soundWrong`, `soundWin`, `soundLose`.
- [ ] Có đủ 5 clip countdown.
- [ ] Scene nào cũng có `AudioBridge` nếu muốn tự bắt event.
- [ ] Mỗi scene gắn đúng object text/button của scene đó.
- [ ] Test click, countdown, đúng/sai, win/lose trong Editor.
