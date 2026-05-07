# Scene Structure & Script Mapping

Tài liệu này mô tả cấu trúc GameObject và script được gán trong từng scene của dự án DoAnGame.
Dùng làm tài liệu tham chiếu khi làm việc với UI, thêm tính năng, hoặc debug.

---

## Tổng quan các Scene

| Scene | File | Mục đích | Số GameObject |
|---|---|---|---|
| `GameUIPlay 1` | `Assets/Scenes/GameUIPlay 1.unity` | Hub chính: Auth, Menu, Profile, BXH | 258 |
| `Test_FireBase_multi` | `Assets/Scenes/Test_FireBase_multi.unity` | Multiplayer battle (Lobby → Battle → Kết quả) | 149 |
| `ChonDA` | `Assets/Scenes/ChonDA.unity` | Mini-game Chọn Đáp Án (single-player) | 202 |
| `KeoThaDA` | `Assets/Scenes/KeoThaDA.unity` | Mini-game Kéo Thả Đáp Án + Phòng Thủ | 109 |
| `PhiThuyen` | `Assets/Scenes/PhiThuyen.unity` | Mini-game Phi Thuyền (Space shooter) | 78 |

---

## Scene: GameUIPlay 1

**Mục đích:** Scene hub chính. Chứa toàn bộ luồng Auth (đăng nhập/đăng ký/chơi nhanh), Main Menu, Profile, Bảng xếp hạng. Điểm vào của ứng dụng.

### Root GameObjects

| GameObject | Script(s) gán | Ghi chú |
|---|---|---|
| `Main Camera` | `Camera`, `AudioListener`, `UniversalAdditionalCameraData` | Camera chính |
| `AuthServices` | `UserValidationService`, `SessionManager`, `PlayerDataService`, `UILoadingIndicator`, `AuthManager`, `CloudSyncService`, `SessionGuardService` | **DontDestroyOnLoad** — singleton services cho auth |
| `GameUICanvas` | `Canvas`, `CanvasScaler`, `GraphicRaycaster`, `UIStartupController` | Canvas chính chứa toàn bộ UI panels |
| `EventSystem` | `EventSystem`, `StandaloneInputModule` | Input system |
| `DataManager` | `DataManager`, `ButtonHandler` | Quản lý điểm số local |

### Panels trong GameUICanvas (con của GameUICanvas)

| Panel (GameObject) | Script Controller | Trạng thái mặc định | Mô tả |
|---|---|---|---|
| `WELCOMESCREEN` | `UIManager` (legacy, gán trực tiếp trên WELCOMESCREEN) | **Inactive** | Màn chọn lớp học. Hiển thị đầu tiên cho user mới (quyết định bởi `UIStartupController`) |
| `WellcomePanel` | `UIWelcomeIntroController` | Inactive | Màn chào cho returning user. Có nút Chơi Tiếp, Chơi Mới, Đăng Ký, Đăng Nhập |
| `NhapTen_choiNhanh` | `UIQuickPlayNameController` | Inactive | Nhập tên + **chọn Lớp 1–5** (guest mode). Có Dropdown grade, nút BatDau, ContinueButton |
| `ModSelectionPanel` | `UIModSelectionPanelController` | Inactive | Chọn chế độ: Chơi Đơn hoặc Multiplayer. Kiểm tra login trước khi vào Multiplayer |
| `LoginPanel` | `UILoginPanelController` | Inactive | Form đăng nhập email/password |
| `ForgotPasswordPanel` | `UIForgotPasswordController` | Inactive | Quên mật khẩu — gửi email reset qua Firebase |
| `RegisterPanel` | `UIRegisterPanelController` | Inactive | Form đăng ký: email, tên nhân vật, password, tuổi, điều khoản |
| `ChonMan_` | *(không có controller riêng)* | Inactive | Chọn chế độ chơi: Lớp Học (ChonDA), Phòng Thủ (KeoThaDA), Phi Thuyền, Kéo Thả |
| `LoadingIndicator` | `UILoadingIndicator` | Inactive | Spinner loading dùng chung cho Login/Register |
| `MainMenuPanel` | `UIMainMenuController` | **Active** | Menu chính sau đăng nhập: hiển thị tên, level, score, **grade (lớp)**, avatar. Nút Play, BXH, Hồ Sơ, Settings, Đăng Xuất. Chứa `LogoutConfirmPopup`. Dữ liệu đồng bộ cho cả guest và logged-in users |
| `BangXepHang` | `UILeaderboardPanelController` | Inactive | Bảng xếp hạng từ Firebase (top theo totalScore) |
| `LoginRequiredPopup` | `UILoginRequiredPopupController` | Inactive | Popup yêu cầu đăng nhập khi guest cố vào Multiplayer |
| `SettingsPopup` | `SettingsPopupController` | Inactive | Popup cài đặt: volume slider, thoát game |
| `Profile` | `ProfileUI` | **Active** | Hiển thị thống kê người chơi: level, score, grade, tiến độ 3 chế độ, coins. **Tính năng mới:** Xóa tài khoản + Đổi độ khó (lớp 1-5) |

### Lưu ý quan trọng — GameUIPlay 1

- `UIStartupController` trên `GameUICanvas` quyết định panel nào hiển thị đầu tiên (WELCOMESCREEN / WellcomePanel / MainMenuPanel) dựa trên trạng thái session.
- `UIManager` (legacy) gán **trực tiếp trên `WELCOMESCREEN`** — chứa `SelectedGrade` static field dùng khắp dự án. `ChonTuoi` (TMP_Dropdown chọn năm sinh) đã bị xóa khỏi scene — grade selection chuyển sang `NhapTen_choiNhanh`.
- `NhapTen_choiNhanh` chỉ có **1 lần** `UIQuickPlayNameController` (bug gán 2 lần đã được sửa).
- `MainMenuPanel` chứa `LogoutConfirmPopup` (con trực tiếp) với `UIConfirmPopupController`. Hiển thị **Grade (Lớp)** cho cả guest và logged-in users.
- `WELCOMESCREEN`, `WellcomePanel`, `MainMenuPanel` đều có `UISettingsOpenButton` trên nút Setting.
- `Profile` có 2 tính năng mới:
  - **Xóa tài khoản** (`DeleteAccountBtn` + `DeleteAccountPopup`): Xóa Firestore → Firebase Auth → Local data → Logout → WELCOMESCREEN
  - **Đổi độ khó** (`DifficultySection` + `ChangeDifficultyPopup`): Chọn lớp 1-5 → Reset tiến độ 3 chế độ → Sync Firebase → Auto-refresh MainMenuPanel
- Nút Play trên `WELCOMESCREEN` luôn enabled (không còn bị disable chờ chọn năm sinh).
- `CloudSyncService` và `SessionGuardService` là services mới thêm vào `AuthServices`.
- **Guest mode data preservation**: Khi reset game data, guest mode flag + name + score + level được bảo toàn (không bị xóa)

### Profile Panel — Chi tiết tính năng mới

#### 1. Xóa Tài Khoản (Delete Account)

**Thành phần:**
- `DeleteAccountBtn`: Nút xóa tài khoản (chỉ hiển thị khi đã đăng nhập bằng email)
- `DeleteAccountPopup`: Popup xác nhận (Canvas: ScreenSpaceOverlay)

**Flow:**
1. Click `DeleteAccountBtn` → Hiện popup xác nhận
2. Click `ConfirmDeleteBtn` → Thực hiện xóa:
   - Xóa Firestore documents (playerData, users, gameModeProgress, playerShop)
   - Xóa Firebase Auth user
   - Xóa toàn bộ local data (PlayerPrefs.DeleteAll)
   - Reset grade về 1
   - Logout + clear session
   - Điều hướng về WELCOMESCREEN

**Lưu ý:**
- Chỉ hiển thị cho email users (không phải guest)
- Nếu session hết hạn → yêu cầu đăng nhập lại
- Không block UI nếu Firestore xóa thất bại (local đã xóa rồi)

#### 2. Đổi Độ Khó (Change Difficulty)

**Thành phần:**
- `DifficultySection`: Dropdown chọn lớp 1-5 + nút Áp Dụng
- `ChangeDifficultyPopup`: Popup xác nhận (Canvas: ScreenSpaceOverlay)

**Flow:**
1. Chọn lớp từ Dropdown → Click `ApplyDifficultyBtn`
2. Nếu lớp khác hiện tại → Hiện popup xác nhận
3. Click `ConfirmChangeBtn` → Thực hiện đổi:
   - Cập nhật `UIManager.SelectedGrade`
   - Lưu grade vào PlayerPrefs (`SelectedGrade`)
   - Reset tiến độ 3 chế độ về màn 1 (local):
     - `Class_HighestLevel` = 1
     - `HighestLevelReached` = 1
     - `Space_HighestLevel` = 1
   - **Giữ nguyên:** level, score, coins
   - Sync Firebase (nếu đã đăng nhập):
     - `users/{uid}.grade` = newGrade
     - `gameModeProgress/{uid}_{mode}_{newGrade}` reset về màn 1 (3 chế độ)
   - **Auto-refresh MainMenuPanel** → Hiển thị grade mới

**Lưu ý:**
- Áp dụng cho cả guest và logged-in users
- Chỉ reset tiến độ, không reset score/level/coins
- Nếu lớp không thay đổi → Hiện thông báo "Đây là lớp hiện tại"
- Popup có Canvas ScreenSpaceOverlay để click được button

---

## Scene: Test_FireBase_multi

**Mục đích:** Scene multiplayer hoàn chỉnh. Luồng: Lobby → Tìm phòng → Phòng chờ → Battle → Kết quả.

### Root GameObjects

| GameObject | Script(s) gán | Ghi chú |
|---|---|---|
| `Main Camera` | `Camera`, `AudioListener` | Camera chính (không có UniversalAdditionalCameraData) |
| `NetworkManager` | `NetworkManager`, `UnityTransport` | NGO NetworkManager — quản lý kết nối Relay |
| `RelayManager` | `RelayManager` | **DontDestroyOnLoad** — tạo/join Relay allocation |
| `BattleManager` | `NetworkObject`, `NetworkedMathBattleManager` | **NetworkBehaviour** — server-authoritative, quản lý trận đấu |
| `Canvas` | `Canvas`, `CanvasScaler`, `GraphicRaycaster`, `NetworkObject` | Canvas chính chứa tất cả panels multiplayer |
| `EventSystem` | `EventSystem`, `StandaloneInputModule` | Input system |
| `GameLogger` | `GameLogger` | Ghi log ra file txt (HOST/CLIENT riêng) |
| `MatchEndNavigator` | `Button`, `UIButtonScreenNavigator` | Nút điều hướng đến WinsPanel khi trận kết thúc |

### Panels trong Canvas (con của Canvas)

| Panel (GameObject) | Script Controller | Trạng thái mặc định | Mô tả |
|---|---|---|---|
| `LobbyPanel` | `UIMultiplayerRoomController` | **Active** | Phòng chờ: tạo phòng, quick join, join by code, danh sách người chơi, nút `SanSangButton` (client) / `StartButton` (host) |
| `GameplayPanel` | `UI16ButtonActionHub`, `UIMultiplayerBattleController`, `AnswerSummaryUI`, `BasePanelController` | Inactive | Màn chơi battle: câu hỏi, 4 đáp án kéo thả, thanh máu, timer, nút Quit |
| `Wins` | `UIWinsController` | Inactive | Kết quả trận: hiển thị Winner/Loser với tên, điểm, máu còn lại |
| `LobbyBrowserPanel` | `UILobbyBrowserController` | Inactive | Danh sách phòng đang mở (auto-refresh 2s) |
| `LoadingPanel` | `UIMultiplayerLoadingController`, `BasePanelController` | Inactive | Loading bar khi chờ kết nối và player states spawn |
| `BattleQuitConfirmPopup` | `UIBattleQuitConfirmPopup` | Inactive | Popup xác nhận thoát trận — có nút `confirmQuitButton` và `CancelButton` |

### Cấu trúc GameplayPanel (chi tiết)

```
GameplayPanel
├── Slot (Tag: Slot)                    — Vùng thả đáp án
├── ANSWER_0                            — MultiplayerDragAndDrop
├── ANSWER_0 (1)                        — MultiplayerDragAndDrop
├── ANSWER_0 (2)                        — MultiplayerDragAndDrop
├── ANSWER_0 (3)                        — MultiplayerDragAndDrop
├── cauhoiText                          — TMP_Text hiển thị câu hỏi
├── Text (TMP) TrangThai                — TMP_Text trạng thái battle
├── Quit                                — Button thoát trận (mở BattleQuitConfirmPopup)
├── TrangThaiWin                        — TMP_Text kết quả (đúng/sai)
└── HealthBarContainer                  — MultiplayerHealthUI
    ├── Player1HealthBar
    │   ├── NamePL1, Player1Score, healText1
    │   ├── healFill1
    │   │   └── TimerFill1              — Image fill timer P1
    │   └── TextTrangThaiDapAn1         — Đáp án P1 (ẩn trong Question Time)
    ├── Player2HealthBar
    │   ├── NamePL2, Player2Score, healText2
    │   ├── healFill2
    │   │   └── TimerFill2              — Image fill timer P2
    │   └── TextTrangThaiDapAn2         — Đáp án P2 (ẩn trong Question Time)
    └── TimerPanel
        ├── TimerState                  — Text trạng thái timer
        └── Timertext                   — Text đếm ngược (10s → 0s)
```

### Lưu ý quan trọng — Test_FireBase_multi

- `BattleManager` có `NetworkObject` → phải được spawn bởi server trước khi dùng.
- `GameplayPanel` có `BasePanelController` gán trực tiếp (không phải subclass) — dùng `Show()`/`Hide()` bình thường.
- `AnswerSummaryUI` gán trên `GameplayPanel` (cùng GameObject với `UIMultiplayerBattleController`).
- `MatchEndNavigator` là GameObject riêng ở root — không phải con của Canvas.
- `LobbyPanel` có `Dropdown` để chọn lớp (Lớp 1–5) → `UIMultiplayerRoomController.GetSelectedGrade()`.
- Tên button thực tế trong Inspector: **`SanSangButton`** (client ready) và **`StartButton`** (host start) — không phải `readyButton`/`startMatchButton`.
- `LobbyPanel` có thêm `RefreshButton` riêng (ngoài `DS Room` button dẫn sang `LobbyBrowserPanel`).
- `BattleQuitConfirmPopup` xử lý forfeit/thoát giữa trận — cần gán trong Inspector của `UIMultiplayerBattleController`.
- `Main Camera` trong scene này **không có** `UniversalAdditionalCameraData`.

---

## Scene: ChonDA

**Mục đích:** Mini-game Chọn Đáp Án (single-player). Người chơi click button để chọn đáp án đúng.

### Root GameObjects

| GameObject | Script(s) gán | Ghi chú |
|---|---|---|
| `Main Camera` | `Camera`, `AudioListener`, `UniversalAdditionalCameraData` | Camera chính |
| `EventSystem` | `EventSystem`, `StandaloneInputModule` | Input system |
| `UIClassManager` | `UiClass` | Manager chính: quản lý panel, tiền, skin mèo, sinh nút màn |
| `Character Meo` | `Animator` | Root GO nhân vật mèo (happy) — 4 skin (mascost1–4) + skeleton bones |
| `Character Meo_Sad` | `Animator` | Root GO nhân vật mèo (sad) — 4 skin + skeleton bones |
| `GamePlay` | *(không có script trực tiếp)* | Chứa gameplay objects (DoHoa + QuesUi) |
| `Ui` | `Canvas` | **Inactive** — Canvas chứa Shop, ChonMan, Setting, Win, Lose panels |

### Cấu trúc GamePlay (khi active)

```
GamePlay
├── DoHoa                               — Sprites (Background, Bang, mèo, tim×3, coinSpawn)
└── QuesUi (Canvas ScreenSpaceCamera)
    ├── questionManager                 — MathManager (sinh câu hỏi, check đáp án)
    ├── soManTxt                        — TMP_Text số màn
    ├── quesTionTxt                     — TMP_Text câu hỏi
    ├── oTrongRect                      — RectTransform ô trống (đè lên dấu ?)
    │   ├── dapAnImage                  — Image ô trống (xanh/đỏ khi đúng/sai)
    │   └── DapAnTxt (1)                — TMP_Text hiển thị đáp án đã chọn
    ├── bnt1, bnt2, bnt3                — Button đáp án × 3
    ├── StBnt                           — UIButtonScreenNavigator (nút Setting)
    ├── SoCauWinTxt                     — TMP_Text tiến độ (X/Y câu đúng)
    └── CoinTxt                         — TMP_Text số coin
```

### Panels trong Ui Canvas

| Panel | Mô tả |
|---|---|
| `ShopPanel` | Shop skin mèo (3 skin: meoT1Bnt, meoV2Bnt, meoC3Bnt). Nút Play → vào gameplay, Back → về GameUIPlay 1, MuaBtn → mua skin |
| `ChonManPanel` | Chọn màn (ScrollView, sinh nút bởi `UiClass`) |
| `SettingPanel` | Tạm dừng: nút Shop, Tiếp Tục |
| `WinPanel` | Thắng: nút Shop, Next |
| `LosePanel` | Thua: nút Shop, Retry |

### Lưu ý — ChonDA

- `MathManager` đọc `UIManager.SelectedGrade` và `LevelManager.CurrentLevel` để lấy config từ `LevelGenerate`.
- `UiClass` quản lý toàn bộ state: `isGameOver`, `currentCorrectCount`, `targetCorrectAnswers`, hearts (3 trái tim).
- Không có `UIFlowManager` — navigation dùng `UIButtonScreenNavigator` và `UiClass` trực tiếp.

---

## Scene: KeoThaDA

**Mục đích:** Mini-game Kéo Thả Đáp Án kết hợp Phòng Thủ (Tower Defense). Kéo đáp án đúng vào slot để bắn quái.

### Root GameObjects

| GameObject | Script(s) gán | Ghi chú |
|---|---|---|
| `Main Camera` | `Camera`, `AudioListener`, `UniversalAdditionalCameraData` | Camera chính |
| `Global Light 2D` | `Light2D` | Inactive — ánh sáng 2D |
| `EventSystem` | `EventSystem`, `StandaloneInputModule` | Input system |
| `UiManager` | `GameUIManager`, `AppQuitting` | Manager chính: quản lý panel, tiền, skin mèo+pháo, sinh nút màn |
| `GamePlay` | *(không có script trực tiếp)* | Inactive — chứa gameplay objects |
| `Ui` | `Canvas` | Canvas chứa Shop, ChonMan, Setting, Win, Lose panels |

### Cấu trúc GamePlay (khi active)

```
GamePlay
├── DoHoa
│   ├── EnemySpawner                    — EnemySpawner (sinh quái theo config)
│   │   └── D1, D1(1), D1(2)           — Spawn points
│   ├── phaoGamePlay                    — SpriteRenderer pháo
│   │   └── FirePoint                  — CannonDefenseManager (bắn đạn)
│   ├── meo                             — SpriteRenderer mèo
│   ├── tuong1 (Tag: Tuong)             — SpriteRenderer + BoxCollider2D + WallHealth
│   └── ...sprites khác (vanGo, bangGo, cco, Troi)
└── QuesUI (Canvas ScreenSpaceCamera)
    ├── DragQuestionManager             — DragQuizManager (sinh câu hỏi kéo thả)
    ├── soManTxt                        — TMP_Text số màn
    ├── cauhoiText                      — TMP_Text câu hỏi
    ├── setting                         — Button Setting (không có UIButtonScreenNavigator)
    ├── Answer_0, Answer_1, Answer_2    — DragAndDrop × 3 (kéo thả đáp án)
    ├── Slot (Tag: Slot)                — Vùng thả đáp án
    ├── Heal                            — Slider thanh máu tường
    └── CoinTxt                         — TMP_Text số coin
```

### Panels trong Ui Canvas

| Panel | Mô tả |
|---|---|
| `ShopPanel` | Shop skin mèo (3 skin) + skin pháo (3 loại). Nút Play, Back, Mua |
| `ChonManPanel` | Chọn màn (ScrollView) |
| `SettingPanel` | Tạm dừng: nút Shop, Tiếp Tục |
| `WinPanel` | Thắng: nút Shop, Next |
| `LosePanel` | Thua: nút Shop, Retry |

### Lưu ý — KeoThaDA

- `DragAndDrop` dùng `static bool isLocked` — lock toàn bộ khi trả lời sai (3 giây phạt).
- `WallHealth` singleton — quái tấn công tường, hết máu → `GameUIManager.ShowLose()`.
- `EnemySpawner` đọc `EnemyDifficultyConfig` theo `UIManager.SelectedGrade` + `LevelManager.CurrentLevel`.
- `CannonDefenseManager` bắn đạn (`DanVaCham`) khi kéo đúng đáp án vào slot.
- `AppQuitting` flag tránh spawn coin khi thoát game.

---

## Scene: PhiThuyen

**Mục đích:** Mini-game Phi Thuyền. Điều khiển phi thuyền bằng chuột, bay qua cổng đúng đáp án.

### Root GameObjects

| GameObject | Script(s) gán | Ghi chú |
|---|---|---|
| `Main Camera` | `Camera`, `AudioListener`, `UniversalAdditionalCameraData` | Camera chính |
| `EventSystem` | `EventSystem`, `StandaloneInputModule` | Input system |
| `UiSpaceManager` | `UiSp` | Manager chính: quản lý panel, tiền, skin phi thuyền, sinh nút màn |
| `GamePlay` | *(không có script trực tiếp)* | Inactive — chứa gameplay objects |
| `Ui` | `Canvas` | Canvas chứa Shop, ChonMan, Setting, Win, Lose panels |

### Cấu trúc GamePlay (khi active)

```
GamePlay
├── DoHoa
│   ├── Canvas (ScreenSpaceCamera)
│   │   └── RawImage                   — Script_Speed_backGround (cuộn nền)
│   ├── PhiThuyen1 (Tag: Player)       — SpriteRenderer + SpaceShipPhysics + CircleCollider2D + Rigidbody2D
│   ├── Enemy1                         — SpriteRenderer + Enemy (tiến lại khi sai)
│   └── BRchonman (Inactive)           — Sprite chọn màn
└── QuesUI (Canvas ScreenSpaceCamera)
    ├── SpaceShipManager               — SpaceShipManager (sinh cổng câu hỏi, quản lý worldSpeed)
    ├── soManTxt                       — TMP_Text số màn
    ├── CauHoiTxt                      — TMP_Text câu hỏi (typing effect)
    ├── StBnt                          — Button Setting
    ├── DkWin                          — TMP_Text điều kiện thắng (X/Y cổng)
    └── CoinTxt                        — TMP_Text coin
```

### Panels trong Ui Canvas

| Panel | Mô tả |
|---|---|
| `ShopPanel` | Shop skin phi thuyền (3 loại: Pt1, Pt2, Pt3). Nút Play, Back, Mua |
| `ChonManPanel` | Chọn màn (ScrollView) |
| `SettingPanel` | Tạm dừng: nút Shop, Tiếp Tục |
| `WinPanel` | Thắng: nút Shop, Next |
| `LosePanel` | Thua: nút Shop, Retry |

### Lưu ý — PhiThuyen

- `SpaceShipPhysics` điều khiển phi thuyền theo chuột, có magnet effect hút vào cổng gần nhất.
- `SpaceShipManager` spawn `QuestionZone` prefab (cổng câu hỏi) di chuyển từ phải sang trái.
- `Enemy` tiến lại gần phi thuyền mỗi lần sai (3 lần sai → thua).
- `CoinSpace` (trên coin prefab) bay về `UiSp.coinTarget` khi phi thuyền chạm vào.
- `SpaceDifficultyConfig` điều chỉnh số cổng, tốc độ, khoảng cách theo grade/level.

---

## Luồng điều hướng giữa các Scene

```
GameUIPlay 1 (Hub)
    ├── WELCOMESCREEN → chọn lớp → WellcomePanel
    ├── WellcomePanel → Đăng Nhập → LoginPanel → MainMenuPanel
    ├── WellcomePanel → Chơi Nhanh → NhapTen_choiNhanh → MainMenuPanel
    ├── MainMenuPanel → Play → ChonMan_ → chọn chế độ:
    │   ├── Lớp Học (ChonDA)     → Load scene ChonDA
    │   ├── Phòng Thủ (KeoThaDA) → Load scene KeoThaDA
    │   ├── Phi Thuyền           → Load scene PhiThuyen
    │   └── Kéo Thả              → Load scene KeoThaDA (dùng chung)
    ├── MainMenuPanel → Multiplayer → ModSelectionPanel → Test_FireBase_multi
    ├── MainMenuPanel → BXH → BangXepHang
    └── MainMenuPanel → Hồ Sơ → Profile

Test_FireBase_multi (Multiplayer)
    ├── LobbyPanel → tạo/join phòng
    ├── LobbyPanel → DS Room → LobbyBrowserPanel
    ├── LobbyPanel → Bắt Đầu → LoadingPanel → GameplayPanel
    ├── GameplayPanel → kết thúc trận → Wins
    └── Wins → Tiếp Tục → LobbyPanel

ChonDA / KeoThaDA / PhiThuyen (Mini-games)
    └── Back button → GameUIPlay 1 (UIButtonScreenNavigator)
```

---

## Script → Scene mapping (tóm tắt nhanh)

| Script | Scene(s) | Namespace |
|---|---|---|
| `AuthManager` | GameUIPlay 1 | global |
| `FirebaseManager` | GameUIPlay 1 (tạo tự động) | global |
| `SessionManager` | GameUIPlay 1 | `DoAnGame.Auth` |
| `PlayerDataService` | GameUIPlay 1 | `DoAnGame.Auth` |
| `UserValidationService` | GameUIPlay 1 | `DoAnGame.Auth` |
| `CloudSyncService` | GameUIPlay 1 | `DoAnGame.Auth` |
| `SessionGuardService` | GameUIPlay 1 | `DoAnGame.Auth` |
| `UIStartupController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIWelcomeIntroController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UILoginPanelController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIRegisterPanelController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIForgotPasswordController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIMainMenuController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIConfirmPopupController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UILeaderboardPanelController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIModSelectionPanelController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIQuickPlayNameController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UILoginRequiredPopupController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UISettingsOpenButton` | GameUIPlay 1 | `DoAnGame.UI` |
| `SettingsPopupController` | GameUIPlay 1 | `DoAnGame.UI` |
| `ProfileUI` | GameUIPlay 1 | global (legacy) |
| `UIManager` | GameUIPlay 1 | global (legacy) |
| `DataManager` | GameUIPlay 1 | global |
| `RelayManager` | Test_FireBase_multi | global |
| `NetworkedMathBattleManager` | Test_FireBase_multi | `DoAnGame.Multiplayer` |
| `UIMultiplayerRoomController` | Test_FireBase_multi | `DoAnGame.UI` |
| `UIMultiplayerBattleController` | Test_FireBase_multi | `DoAnGame.UI` |
| `UIMultiplayerLoadingController` | Test_FireBase_multi | `DoAnGame.UI` |
| `UILobbyBrowserController` | Test_FireBase_multi | `DoAnGame.UI` |
| `UIWinsController` | Test_FireBase_multi | `DoAnGame.UI` |
| `UI16ButtonActionHub` | Test_FireBase_multi | `DoAnGame.UI` |
| `MultiplayerHealthUI` | Test_FireBase_multi | `DoAnGame.UI` |
| `AnswerSummaryUI` | Test_FireBase_multi | `DoAnGame.UI` |
| `UIBattleQuitConfirmPopup` | Test_FireBase_multi | `DoAnGame.UI` |
| `MultiplayerDragAndDrop` | Test_FireBase_multi | `DoAnGame.Multiplayer` |
| `NetworkedPlayerState` | Test_FireBase_multi (spawn runtime) | `DoAnGame.Multiplayer` |
| `GameLogger` | Test_FireBase_multi | `DoAnGame.Multiplayer` |
| `UiClass` | ChonDA | global |
| `MathManager` | ChonDA | global |
| `GameUIManager` | KeoThaDA | global |
| `DragQuizManager` | KeoThaDA | global |
| `DragAndDrop` | KeoThaDA | global |
| `EnemySpawner` | KeoThaDA | global |
| `EnemyMovement` | KeoThaDA (runtime) | global |
| `WallHealth` | KeoThaDA | global |
| `CannonDefenseManager` | KeoThaDA | global |
| `DanVaCham` | KeoThaDA (runtime) | global |
| `UiSp` | PhiThuyen | global |
| `SpaceShipManager` | PhiThuyen | global |
| `SpaceShipPhysics` | PhiThuyen | global |
| `Enemy` | PhiThuyen | global |
| `QuestionZone` | PhiThuyen (runtime) | global |
| `CoinSpace` | PhiThuyen (runtime) | global |
| `Script_Speed_backGround` | PhiThuyen | global |
