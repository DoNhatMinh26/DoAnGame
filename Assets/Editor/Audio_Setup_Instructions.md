# Hướng dẫn nhanh setup Audio (AudioManager + AudioEventBridge)

Mục tiêu: thiết lập nhanh `AudioManager` global và per-scene `AudioEventBridge` + auto hook button click.

1) Chuẩn bị chung
- Đảm bảo chỉ có 1 `AudioManager` (GameObject tên `AudioManager`) trong project hoặc scene bootstrap. `AudioManager` chứa các field chính:
  - `musicMenu`, `musicMultiplayer`, `musicClassMode`, `musicDefenseMode`, `musicSpaceMode`
  - `soundClick`, `soundCorrect`, `soundWrong`, `soundWin`, `soundLose`
  - `countdown3Clip`, `countdown2Clip`, `countdown1Clip`, `countdownReadyClip`, `countdownGoClip`

2) Tự động tạo/gắn Bridge cho scene hiện tại
- Mở scene muốn setup (ví dụ: `GameUIPlay 1`, `KeoThaDA`, `PhiThuyen`, `Test_FireBase_multi`).
- Trong Unity Editor menu: `Audio -> Setup Bridge For Current Scene`.
  - Hành động này:
    - Thêm `AudioEventBridge` vào `AudioManager` (nếu có) hoặc tạo GameObject `AudioBridge` và thêm component.
    - Thiết lập `sceneMusicType` theo tên scene (MainMenu/Defense/Space/Multiplayer/...).
    - Thêm `UIButtonAudioHelper` vào mọi `Canvas` (auto hook nút click). Nếu một Canvas đang bị disabled/inactive, script Editor sẽ vẫn tìm và gắn helper bằng cách duyệt các root objects trong scene; nếu Canvas chỉ được bật sau khi runtime bắt đầu, bật `autoHookButtons` hoặc chạy `UIButtonAudioHelper.SetupAllButtons()` sau khi Canvas active để hook các nút.

3) Kiểm tra và gán clip trong Inspector
- Chọn `AudioManager` GameObject:
  - Gán các clip music và sfx ở bước (1).
- Chọn `AudioBridge` (hoặc `AudioManager` nếu bridge được gắn lên đó):
  - `sceneMusicType`: chọn đúng loại (MainMenu/Defense/Space/Multiplayer).
  - `backgroundMusicOverride` (tuỳ): kéo 1 clip để override music mặc định.
  - Nếu scene cần feedback: kéo `sfxCorrect`, `sfxWrong`, `sfxWin`, `sfxLose` (tuỳ).
  - Nếu scene có countdown: kéo 5 clip countdown vào các ô tương ứng.
  - `autoHookButtons`: bật (bridge hoặc `UIButtonAudioHelper` sẽ hook nút).
  - `battleStatusTextObject`, `answerTimerTextObject`, `answerResultTextObject`: (tuỳ) kéo object Text/TMP của scene nếu bạn muốn bridge detect chính xác.

4) Test nhanh trong Editor
- Play scene.
- Test nút: bấm vài button trên Canvas → nghe `soundClick`.
- Test music: khi scene load, music theo `sceneMusicType` hoặc `backgroundMusicOverride` sẽ phát.
- Test countdown: nếu scene có countdown, hiển thị text `3`,`2`,`1`,`Ready`,`GO!` → bridge sẽ phát countdown clip ngay khi text đổi.
- Test answer: simulate correct/wrong (hoặc trigger in-game) → nghe `soundCorrect`/`soundWrong`.
- Test win/lose: trigger Win/Lose UI → nghe `soundWin`/`soundLose`.

5) Troubleshooting (nhanh)
- Không nghe gì: kiểm tra `AudioManager` có instance không, volume không 0, và clip đã gán.
- Nhiều lần hook (button phát nhiều lần): mở Inspector của Canvas, chạy `UIButtonAudioHelper.SetupAllButtons()` một lần để reset listeners.
- Countdown không phát: kéo `battleStatusTextObject` chính xác vào bridge hoặc sửa controller để gọi `AudioManager.Instance.PlayCountdownX()` trực tiếp (ổn định hơn).

6) Gợi ý nâng cao
- Sau khi kiểm thử ổn, chuyển dần những gọi timing nhạy (countdown, answer result) từ bridge sang controller (thêm `AudioManager.Instance?.Play...()` trong `UIMultiplayerBattleController` / `AnswerSummaryUI`) — sẽ chính xác hơn.

7) Checklist nhanh
- [ ] `AudioManager` có clip cơ bản (music + soundClick + soundCorrect/wrong/win/lose).
- [ ] Mỗi scene chạy `Audio -> Setup Bridge For Current Scene`.
- [ ] Kiểm tra `AudioEventBridge` Inspector (sceneMusicType, overrides, references).
- [ ] Play và test: Button click, Music, Countdown, Correct/Wrong, Win/Lose.

📋 **Chi tiết per-scene:** xem file `Assets/Editor/AUDIO_SETUP_PER_SCENE.md` để có hướng dẫn từng scene cụ thể (ChonDA, GameUIPlay 1, KeoThaDA, PhiThuyen, Test_FireBase_multi) với các event cần gán và UI objects cần kéo vào.
