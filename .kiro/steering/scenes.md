# Scene Structure & Script Mapping

Tài liệu này mô tả cấu trúc GameObject và script được gán trong từng scene của dự án DoAnGame.
Dùng làm tài liệu tham chiếu khi làm việc với UI, thêm tính năng, hoặc debug.

---

## Tổng quan các Scene

| Scene | File | Mục đích | Số GameObject |
|---|---|---|---|
| `GameUIPlay 1` | `Assets/Scenes/GameUIPlay 1.unity` | Hub chính: Auth, Menu, Profile, BXH | 238 |
| `Test_FireBase_multi` | `Assets/Scenes/Test_FireBase_multi.unity` | Multiplayer battle (Lobby → Battle → Kết quả) | 139 |
| `ChonDA` | `Assets/Scenes/ChonDA.unity` | Mini-game Chọn Đáp Án (single-player) | 85 |
| `KeoThaDA` | `Assets/Scenes/KeoThaDA.unity` | Mini-game Kéo Thả Đáp Án + Phòng Thủ | 109 |
| `PhiThuyen` | `Assets/Scenes/PhiThuyen.unity` | Mini-game Phi Thuyền (Space shooter) | 78 |

---

## Scene: GameUIPlay 1

**Mục đích:** Scene hub chính. Chứa toàn bộ luồng Auth (đăng nhập/đăng ký/chơi nhanh), Main Menu, Profile, Bảng xếp hạng. Điểm vào của ứng dụng.

### Root GameObjects

| GameObject | Script(s) gán | Ghi chú |
|---|---|---|
| `Main Camera` | `Camera`, `AudioListener`, `UniversalAdditionalCameraData` | Camera chính |
| `AuthServices` | `UserValidationService`, `SessionManager`, `PlayerDataService`, `UILoadingIndicator`, `AuthManager` | **DontDestroyOnLoad** — singleton services cho auth |
| `GameUICanvas` | `Canvas`, `CanvasScaler`, `GraphicRaycaster`, `UIStartupController` | Canvas chính chứa toàn bộ UI panels |
| `EventSystem` | `EventSystem`, `StandaloneInputModule` | Input system |
| `DataManager` | `DataManager`, `ButtonHandler` | **DontDestroyOnLoad** — quản lý điểm số local |

### Panels trong GameUICanvas (con của GameUICanvas)

| Panel (GameObject) | Script Controller | Trạng thái mặc định | Mô tả |
|---|---|---|---|
| `WELCOMESCREEN` | `UIManager` (legacy, trên `ChonTuoi`) | **Active** | Màn chọn năm sinh / lớp học. Hiển thị đầu tiên cho user mới |
| `WellcomePanel` | `UIWelcomeIntroController` | Inactive | Màn chào cho returning user. Có nút Chơi Tiếp, Chơi Mới, Đăng Ký, Đăng Nhập |
| `NhapTen_choiNhanh` | `UIQuickPlayNameController` (×2 — bug: gán 2 lần) | Inactive | Nhập tên + **chọn Lớp 1–5** (guest mode). Dropdown grade thay thế ChonTuoi cũ trên WELCOMESCREEN |
| `ModSelectionPanel` | `UIModSelectionPanelController` | Inactive | Chọn chế độ: Chơi Đơn hoặc Multiplayer. Kiểm tra login trước khi vào Multiplayer |
| `LoginPanel` | `UILoginPanelController` | Inactive | Form đăng nhập email/password |
| `ForgotPasswordPanel` | `UIForgotPasswordController` | Inactive | Quên mật khẩu — gửi email reset qua Firebase |
| `RegisterPanel` | `UIRegisterPanelController` | Inactive | Form đăng ký: email, tên nhân vật, password, tuổi, điều khoản |
| `ChonMan_` | *(không có controller riêng)* | Inactive | Chọn chế độ chơi: Lớp Học, Phòng Thủ, Phi Thuyền, Kéo Thả |
| `LoadingIndicator` | `UILoadingIndicator` | Inactive | Spinner loading dùng chung cho Login/Register |
| `MainMenuPanel` | `UIMainMenuController` | Inactive | Menu chính sau đăng nhập: hiển thị tên, level, score. Nút Play, BXH, Hồ Sơ, Settings, Đăng Xuất |
| `BangXepHang` | `UILeaderboardPanelController` | Inactive | Bảng xếp hạng từ Firebase Firestore (top 50 theo totalScore) |
| `LoginRequiredPopup` | `UILoginRequiredPopupController` | Inactive | Popup yêu cầu đăng nhập khi guest cố vào Multiplayer |
| `SettingsPopup` | `SettingsPopupController` | Inactive | Popup cài đặt: volume slider, thoát game |
| `Profile` | `ProfileUI` (legacy) | Inactive | Hiển thị thống kê người chơi từ PlayerPrefs |

### Lưu ý quan trọng — GameUIPlay 1

- `UIStartupController` trên `GameUICanvas` quyết định panel nào hiển thị đầu tiên (WELCOMESCREEN / WellcomePanel / MainMenuPanel) dựa trên trạng thái session.
- `UIManager` (legacy) gán trên `ChonTuoi` (con của WELCOMESCREEN) — chứa `SelectedGrade` static field dùng khắp dự án. **`ChonTuoi` (TMP_Dropdown chọn năm sinh) đã bị xóa** — grade selection chuyển sang `NhapTen_choiNhanh`.
- `NhapTen_choiNhanh` bị gán `UIQuickPlayNameController` **2 lần** — đây là bug cần sửa trong Inspector.
- `Profile` dùng `ProfileUI` (legacy, đọc PlayerPrefs) thay vì `UIProfilePanelController` (mới, đọc Firebase).
- Nút Play trên `WELCOMESCREEN` luôn enabled (không còn bị disable chờ chọn năm sinh).

---

## Scene: Test_FireBase_multi

**Mục đích:** Scene multiplayer hoàn chỉnh. Luồng: Lobby → Tìm phòng → Phòng chờ → Battle → Kết quả.

### Root GameObjects

| GameObject | Script(s) gán | Ghi chú |
|---|---|---|
| `Main Camera` | `Camera`, `AudioListener` | Camera chính |
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
| `LobbyPanel` | `UIMultiplayerRoomController` | **Active** | Phòng chờ: tạo phòng, quick join, join by code, danh sách người chơi, nút Sẵn Sàng / Bắt Đầu |
| `GameplayPanel` | `UI16ButtonActionHub`, `UIMultiplayerBattleController`, `AnswerSummaryUI`, `BasePanelController` | Inactive | Màn chơi battle: câu hỏi, 4 đáp án kéo thả, thanh máu, timer |
| `Wins` | `UIWinsController` | Inactive | Kết quả trận: hiển thị Winner/Loser với tên, điểm, máu còn lại |
| `LobbyBrowserPanel` | `UILobbyBrowserController` | Inactive | Danh sách phòng đang mở (auto-refresh 2s) |
| `LoadingPanel` | `UIMultiplayerLoadingController`, `BasePanelController` | Inactive | Loading bar khi chờ kết nối và player states spawn |

### Cấu trúc GameplayPanel (chi tiết)

```
GameplayPanel
├── Slot (Tag: Slot)                    — Vùng thả đáp án
├── ANSWER_0..3                         — MultiplayerDragAndDrop × 4
├── cauhoiText                          — TMP_Text hiển thị câu hỏi
├── Text (TMP) TrangThai                — TMP_Text trạng thái battle
├── TrangThaiWin                        — TMP_Text kết quả (đúng/sai)
└── HealthBarContainer                  — MultiplayerHealthUI
    ├── Player1HealthBar
    │   ├── NamePL1, Player1Score, healText1, healFill1
    │   └── TextTrangThaiDapAn1         — Đáp án P1 (ẩn trong Question Time)
    ├── Player2HealthBar
    │   ├── NamePL2, Player2Score, healText2, healFill2
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

---

## Scene: ChonDA

**Mục đích:** Mini-game Chọn Đáp Án (single-player). Người chơi click button để chọn đáp án đúng.

### Root GameObjects

| GameObject | Script(s) gán | Ghi chú |
|---|---|---|
| `Main Camera` | `Camera`, `AudioListener`, `UniversalAdditionalCameraData` | Camera chính |
| `EventSystem` | `EventSystem`, `StandaloneInputModule` | Input system |
| `UIClassManager` | `UiClass` | Manager chính: quản lý panel, tiền, skin mèo, sinh nút màn |
| `GamePlay` | *(không có script trực tiếp)* | Inactive — chứa gameplay objects |
| `Ui` | `Canvas` | Canvas chứa Shop, ChonMan, Setting, Win, Lose panels |

### Cấu trúc GamePlay (khi active)

```
GamePlay
├── DoHoa                               — Sprites (Background, Bang, mèo, tim×3, coinSpawn)
└── QuesUi (Canvas ScreenSpaceCamera)
    ├── questionManager                 — MathManager (sinh câu hỏi, check đáp án)
    ├── quesTionTxt                     — TMP_Text câu hỏi
    ├── oTrongRect                      — RectTransform ô trống (đè lên dấu ?)
    │   ├── dapAnImage                  — Image ô trống (xanh/đỏ khi đúng/sai)
    │   └── DapAnTxt                    — TMP_Text hiển thị đáp án đã chọn
    ├── bnt1, bnt2, bnt3                — Button đáp án × 3
    ├── StBnt                           — UIButtonScreenNavigator (nút Setting)
    ├── SoCauWinTxt                     — TMP_Text tiến độ (X/Y câu đúng)
    └── CoinTxt                         — TMP_Text số coin
```

### Panels trong Ui Canvas

| Panel | Mô tả |
|---|---|
| `ShopPanel` | Shop skin mèo (3 skin). Nút Play → vào gameplay, Back → về GameUIPlay 1 |
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
│   └── ...sprites khác
└── QuesUI (Canvas ScreenSpaceCamera)
    ├── DragQuestionManager             — DragQuizManager (sinh câu hỏi kéo thả)
    ├── cauhoiText                      — TMP_Text câu hỏi
    ├── Answer_0, Answer_1, Answer_2    — DragAndDrop × 3 (kéo thả đáp án)
    ├── Slot (Tag: Slot)                — Vùng thả đáp án
    └── Heal                            — Slider thanh máu tường
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
| `UIStartupController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIWelcomeIntroController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UILoginPanelController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIRegisterPanelController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIForgotPasswordController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIMainMenuController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UILeaderboardPanelController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIModSelectionPanelController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UIQuickPlayNameController` | GameUIPlay 1 | `DoAnGame.UI` |
| `UILoginRequiredPopupController` | GameUIPlay 1 | `DoAnGame.UI` |
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
