# AUDIO SETUP PER SCENE - HUONG DAN GAN DUNG OBJECT

Tai lieu nay dua theo cac file export:
- `Assets/Editor/ChonDA_Export.txt`
- `Assets/Editor/GameUIPlay 1_Export.txt`
- `Assets/Editor/KeoThaDA_Export.txt`
- `Assets/Editor/PhiThuyen_Export.txt`
- `Assets/Editor/Test_FireBase_multi_Export.txt`

Muc tieu: biet chinh xac scene nao gan `AudioEventBridge`, Canvas nao gan `UIButtonAudioHelper`, va 3 field manual reference trong Inspector nen keo object nao vao.

---

## 0. Hieu dung 3 field trong AudioEventBridge

Chon GameObject co `AudioEventBridge` thuong la `AudioManager`, nhin group `Manual references (optional)`:

| Field | Dung de bat su kien nao | Nen keo object nao vao |
|---|---|---|
| `Battle Status Text Object` | Dem nguoc `3`, `2`, `1`, `Ready`, `GO` | Text/TMP hien countdown hoac trang thai tran dau |
| `Answer Timer Text Object` | Canh bao timer khi text con `5`, `4`, `3`, `2`, `1` | Text/TMP hien so giay con lai |
| `Answer Result Text Object` | Phat dung/sai khi text doi sang `Correct`, `Wrong`, `Dung`, `Sai`, `true`, `false` | Text/TMP hien ket qua tra loi dung/sai |

Luu y quan trong:
- Button click: khong keo tung button vao `AudioEventBridge`. Chi can `UIButtonAudioHelper` tren Canvas, no se tu hook tat ca `Button` con.
- Click ngoai UI: dung `GlobalClickAudioListener` tren `AudioManager`, de `enableGlobalClick = true`, `onlyNonUIClicks = true`. No dung chung `AudioManager.soundClick`.
- `GlobalClickAudioListener` khong co clip rieng. Am click ngoai man hinh va am click button deu dung `AudioManager.soundClick`.
- `sfxCorrect/sfxWrong/sfxWin/sfxLose` trong `AudioEventBridge` se override truc tiep vao `AudioManager.soundCorrect/soundWrong/soundWin/soundLose` khi scene load. Vi vay cac script gameplay goi `AudioManager.Instance.soundCorrect` van tu dong an theo clip rieng cua scene neu ban co gan trong `AudioEventBridge`.
- `sfxCoin` trong `AudioEventBridge` se override truc tiep vao `AudioManager.soundCoin` khi scene load.
- Neu scene khong co text dung/sai rieng thi de trong `Answer Result Text Object`; luc do muon dung/sai chuan thi nen goi truc tiep `AudioManager.Instance.PlaySFX(AudioManager.Instance.soundCorrect)` / `AudioManager.Instance.PlaySFX(AudioManager.Instance.soundWrong)` trong script gameplay.
- De doi nhac menu <-> battle trong cung 1 scene: co the gan them `menuPanelNames` va `battlePanelNames` trong `AudioEventBridge`. Neu de trong, bridge se tu auto-do theo ten panel pho bien cua tung scene.
- De xu ly Win/Lose: co the gan them `resultPanelNames` (WinPanel/LosePanel/Wins). Khi panel nay active, bridge se tam dung nhac nen; khi panel dong va flow quay lai menu hoac battle, nhac se tu phat lai theo panel dang active.

---

## 1. Setup chung cho tat ca scene

### AudioManager

Trong moi scene, can co `AudioManager` hoac mot AudioManager singleton ton tai tu scene truoc.

Gan clip vao `AudioManager`:
- `musicSource`: AudioSource phat nhac nen.
- `sfxSource`: AudioSource phat SFX.
- `soundClick`: `Assets/AmThanh/button_click.mp3`
- `soundCorrect`: `Assets/AmThanh/correct_answer.mp3`
- `soundWrong`: `Assets/AmThanh/wrong_answer.mp3`
- `soundWin`: `Assets/AmThanh/victory_music.mp3` hoac clip win ngan neu co.
- `soundLose`: `Assets/AmThanh/defeat_music.mp3` hoac clip lose ngan neu co.
- `soundCoin`: clip an tien tren map (dung cho `PhiThuyen`).
- `countdown3Clip`, `countdown2Clip`, `countdown1Clip`: clip dem nguoc.
- `countdownGoClip`: clip GO.
- `musicMenu`, `musicClassMode`, `musicDefenseMode`, `musicSpaceMode`, `musicMultiplayer`: gan dung nhac nen tung mode.

### AudioEventBridge

Gan `AudioEventBridge` tren cung GameObject voi `AudioManager`.

Bat:
- `autoHookButtons = false`
- `autoHookBattleStatusText = true`
- `autoHookAnswerTimerWarning = true`
- `autoHookModePanels = true`

Ly do `autoHookButtons = false`: project da co `UIButtonAudioHelper` de phat click button. Neu bat them hook button trong `AudioEventBridge`, cung mot button co the phat click 2 lan.

`modePanelNames` dung cho scene menu `GameUIPlay 1`:
- Element 0: `UiClass`
- Element 1: `UiTp`
- Element 2: `UiSp`

Neu scene gameplay khong co 3 panel nay, van co the de nguyen, khong gay loi.

### UIButtonAudioHelper

Gan `UIButtonAudioHelper` vao tat ca GameObject co component `Canvas` trong scene.

De:
- `autoSetupOnStart = true`
- `hookChildrenOnly = true`

Khi Canvas inactive luc Start, neu button duoc tao/bat sau runtime, goi lai `SetupAllButtons()` hoac chay lai menu `Audio -> Setup Bridge For Current Scene` khi scene dang mo trong Editor.

### Menu setup da duoc cap nhat

Menu `Audio -> Setup Bridge For Current Scene` hien se tu lam cac viec sau:
- Gan/tao `AudioEventBridge`.
- Dat `autoHookButtons = false` de khong trung voi `UIButtonAudioHelper`.
- Dat `autoHookBattleStatusText = true`, `autoHookAnswerTimerWarning = true`, `autoHookModePanels = true`.
- Gan/tao `UIButtonAudioHelper` cho moi Canvas va dat `autoSetupOnStart = true`, `hookChildrenOnly = true`.
- Gan/tao `GlobalClickAudioListener` va dat `enableGlobalClick = true`, `onlyNonUIClicks = true`.
- Map `sceneMusicType`: `GameUIPlay 1 = MainMenu`, `ChonDA = Class`, `KeoThaDA = Defense`, `PhiThuyen = Space`, `Test_FireBase_multi = Multiplayer`.
- Tu gan panel doi nhac:
  - `ChonDA/KeoThaDA/PhiThuyen`: menu = `ShopPanel, ChonManPanel`; battle = `GamePlay, QuesUi/QuesUI, WinPanel, LosePanel`.
  - `Test_FireBase_multi`: menu = `LobbyPanel, LobbyBrowserPanel`; battle = `GameplayPanel, Wins`.
- Tu gan panel ket qua:
  - `ChonDA/KeoThaDA/PhiThuyen`: result = `WinPanel, LosePanel`.
  - `Test_FireBase_multi`: result = `Wins`.
- Rieng multiplayer: tu gan `Battle Status Text Object = Text (TMP) TrangThai`, `Answer Timer Text Object = Timertext`, va de trong `Answer Result Text Object`.

Sau khi chay menu setup, van can gan clip vao `AudioManager` neu clip chua co.

---

## 2. Scene `GameUIPlay 1` - menu, dang nhap, chon mode, profile

Export cho thay scene co:
- Root Canvas: `GameUICanvas`
- Audio: `AudioManager`
- Cac panel con: `WELCOMESCREEN`, `WellcomePanel`, `NhapTen_choiNhanh`, `ModSelectionPanel`, `LoginPanel`, `RegisterPanel`, `ChonMan_`, `MainMenuPanel`, `BangXepHang`, `SettingsPopup`, `Profile`, ...

### Gan component

Tren `AudioManager`:
- `AudioManager`
- `AudioEventBridge`
- `GlobalClickAudioListener`

Tren `GameUICanvas`:
- `UIButtonAudioHelper`

Tren cac popup Canvas world-space neu co:
- `Profile/ChangeDifficultyPopup`
- `Profile/DeleteAccountPopup`
- `Profile/AvatarSelectionPopup`

Neu cac popup nay co `Canvas` rieng, gan them `UIButtonAudioHelper` truc tiep vao popup Canvas do de nut trong popup co click sound khi popup active.

### AudioEventBridge settings

`sceneMusicType`:
- Chon `MainMenu`

Manual references:
- `Battle Status Text Object`: de trong. Scene menu khong co countdown battle.
- `Answer Timer Text Object`: de trong. Scene menu khong co timer tra loi.
- `Answer Result Text Object`: de trong. Cac `StatusText` trong menu la trang thai dang nhap/loading, khong nen gan vao day vi no khong phai ket qua dung/sai.

Mode Panel Names:
- `UiClass`
- `UiTp`
- `UiSp`

Ghi chu:
- Scene export hien khong thay object `UiClass`, `UiTp`, `UiSp` nam trong `GameUIPlay 1`; chung co the nam o scene gameplay hoac duoc bat/tai sau. De nguyen list nay neu project dang dung de doi nhac theo mode.
- Tat ca button nhu `PlayBtn`, `Setting`, `DangNhap`, `DangKy`, `BackBtn`, `ChonDA`, `MeoPhongThuBnt`, `PhiThuyen`, `HoSoBtn`, `BXHBtn`, `CloseButton`, `ConfirmButton`, `CancelButton` se duoc `UIButtonAudioHelper` hook tu dong.

---

## 3. Scene `ChonDA` - game chon dap an lop hoc

Export cho thay scene co:
- Root `UIClassManager`
- Root `GamePlay` inactive, trong do co Canvas `QuesUi`
- Root Canvas `Ui`
- Panel ket thuc: `Ui/WinPanel`, `Ui/LosePanel`
- Setting: `Ui/SettingPanel`
- Shop/chon man: `Ui/ShopPanel`, `Ui/ChonManPanel`

Code lien quan:
- `Assets/Script/class_Scrip/MathManager.cs`
- `Assets/Script/Data_Scrip/UiClass.cs`

Luong code that:
- Button dap an `bnt1/bnt2/bnt3` goi `MathManager.CheckDapAn(int val)`.
- Neu dung: `MathManager` doi `oTrongImage` sang xanh, animation happy, goi `UiClass.Instance.SpawnAndFlyCoin(1)` va `UiClass.Instance.OnCorrectAnswer()`.
- `UiClass.OnCorrectAnswer()` tang `currentCorrectCount`, update `progressTxt`, neu du muc tieu thi goi `WinGame()`.
- `UiClass.WinGame()` doi mot khoang `delayTime`, cap nhat text trong `WinPanel`, roi `panelWin.SetActive(true)`.
- Neu sai: `MathManager` doi `oTrongImage` sang do, animation sad, goi `UiClass.Instance.OnWrongAnswer()`.
- `UiClass.OnWrongAnswer()` tru mau/heart, neu het mau thi `LoseGame()`.
- `UiClass.LoseGame()` doi `delayTime`, cap nhat text trong `LosePanel`, roi `panelLose.SetActive(true)`.

### Gan component

Tren `AudioManager` hoac `AudioBridge`:
- `AudioEventBridge`
- `GlobalClickAudioListener`

Tren Canvas:
- `GamePlay/QuesUi` -> gan `UIButtonAudioHelper`
- `Ui` -> gan `UIButtonAudioHelper`
- `Ui/ShopPanel/ThongBaoCv` neu day la Canvas rieng -> gan `UIButtonAudioHelper`

### AudioEventBridge settings

`sceneMusicType`:
- Chon `Class`

Manual references:
- `Battle Status Text Object`: de trong. Scene nay khong co countdown `3/2/1/GO` trong export.
- `Answer Timer Text Object`: de trong. Khong thay timer text trong export.
- `Answer Result Text Object`: khong gan `progressTxt`, `oTrongText`, `WinTxt`, `LoseTxt`. Cac text nay khong doi sang chu `Dung/Sai`, nen bridge khong bat dung/sai on-time.

Mode Panel Names:
- Co the de mac dinh `UiClass`, `UiTp`, `UiSp`, nhung scene nay khong can.

### Cac object button duoc hook tu dong

Trong `GamePlay/QuesUi`:
- `bnt1`
- `bnt2`
- `bnt3`
- `StBnt`

Trong `Ui`:
- `ShopPanel/Play`
- `ShopPanel/Back`
- `ShopPanel/MuaBtn`
- `ShopPanel/MeoHocBa`
- `ShopPanel/MeoThoXay`
- `ShopPanel/MeoDauBep`
- `ShopPanel/MeoSiQuan`
- `ChonManPanel/Back`
- `SettingPanel/Reload`
- `SettingPanel/ThoatBtn`
- `SettingPanel/TiepTucBtn`
- `WinPanel/ShopBtn`
- `WinPanel/NextBnt`
- `LosePanel/ShopBtn`
- `LosePanel/RetryBnt`

### Dung/sai/win/lose

Voi scene `ChonDA`, cach dung nhat la chen audio vao code dang co:
- Trong `MathManager.CheckDapAn`, ngay trong nhanh `if (val == dapAnDung)`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundCorrect);`
- Trong `MathManager.CheckDapAn`, ngay trong nhanh `else`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundWrong);`
- Trong `UiClass.ShowWinPanelWithDelay`, ngay truoc `panelWin.SetActive(true)`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundWin);`
- Trong `UiClass.ShowLosePanelWithDelay`, ngay truoc `panelLose.SetActive(true)`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundLose);`

Khong nen co gang gan `Answer Result Text Object` cho scene nay, vi code khong co text ket qua dung/sai rieng.

---

## 4. Scene `KeoThaDA` - game phong thu keo tha dap an

Export cho thay scene co:
- Root `UiManager`
- Root `GamePlay`
- Canvas gameplay: `GamePlay/QuesUI`
- Root Canvas menu/shop: `Ui`
- Panel ket thuc: `Ui/WinPanel`, `Ui/LosePanel`
- Setting: `Ui/SettingPanel`

Code lien quan:
- `Assets/Script/GamePhongthu/DragAndDrop.cs`
- `Assets/Script/GamePhongthu/DragQuizManager.cs`
- `Assets/Script/Data_Scrip/UiTp.cs`
- `Assets/Script/GamePhongthu/WallHealth.cs`

Luong code that:
- Nguoi choi keo `Answer_0/1/2` vao object tag `Slot`.
- `DragAndDrop.OnEndDrag()` doc dap an dung tu `DragQuizManager.GetCurrentCorrectAnswer()`.
- Neu dung: doi mau answer sang xanh, trigger animation, goi `CannonDefenseManager.Instance.FireAtClosestEnemy()`, roi reset cau hoi.
- Neu sai: `ApplyGlobalWrongEffect()` trigger sad, khoa/tô do tat ca answer trong mot khoang.
- Win cua scene phong thu khong nam o text, ma o `UiTp.CheckWinCondition()` -> `WaitAndShowWin()` -> `ShowWin()` -> `panelWin.SetActive(true)`.
- Lose co the tu `WallHealth` goi `GameUIManager.Instance.ShowLose()` hoac tu `UiTp.ShowLose()`/`ExecuteShowLosePanel()` -> `panelLose.SetActive(true)`.

### Gan component

Tren `AudioManager` hoac `AudioBridge`:
- `AudioEventBridge`
- `GlobalClickAudioListener`

Tren Canvas:
- `GamePlay/QuesUI` -> gan `UIButtonAudioHelper`
- `Ui` -> gan `UIButtonAudioHelper`
- `Ui/ShopPanel/ThongBaoCv` neu la Canvas rieng -> gan `UIButtonAudioHelper`

### AudioEventBridge settings

`sceneMusicType`:
- Chon `Defense`

Manual references:
- `Battle Status Text Object`: de trong. Export khong co countdown battle text.
- `Answer Timer Text Object`: de trong. Export khong co timer text.
- `Answer Result Text Object`: de trong. Export khong co text dung/sai rieng.

### Cac object quan trong trong Canvas

Trong `GamePlay/QuesUI`:
- `DragQuestionManager`: quan ly cau hoi keo tha.
- `cauhoiText`: text cau hoi, khong gan vao AudioEventBridge.
- `Answer_0`, `Answer_1`, `Answer_2`: day la object keo tha, khong phai Button. `UIButtonAudioHelper` khong hook duoc vi khong co `Button`. Neu muon keo dap an co am click/drag, can them audio vao script `DragAndDrop`.
- `Slot`: noi tha dap an.
- `Heal`: thanh mau, khong gan vao bridge.

Trong `Ui`:
- `ShopPanel/Play`
- `ShopPanel/Back`
- `ShopPanel/MuaBtn`
- `Skin_0_HB`, `Skin_1_TX`, `Skin_2_DB`, `Skin_3_SQ`
- `phaoGo_0`, `phaoSat_1`, `phaoVang_2`
- `ChonManPanel/Back`
- `SettingPanel/ThoatBtn`
- `SettingPanel/TiepTucBtn`
- `SettingPanel/ChoiLai`
- `WinPanel/ShopBtn`
- `WinPanel/NextBnt`
- `LosePanel/ShopBtn`
- `LosePanel/RetryBnt`

### Dung/sai/win/lose

Voi scene `KeoThaDA`, chen audio vao code:
- Trong `DragAndDrop.OnEndDrag()`, nhanh dung sau `image.color = colorCorrect;`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundCorrect);`
- Trong `DragAndDrop.ApplyGlobalWrongEffect()`, dau ham hoac ngay sau trigger sad: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundWrong);`
- Trong `UiTp.ShowWin()`, truoc `panelWin.SetActive(true)`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundWin);`
- Trong `UiTp.WaitAndShowLose()` va `UiTp.ExecuteShowLosePanel()`, truoc `panelLose.SetActive(true)`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundLose);`

Khong gan `Answer Result Text Object` cho `Answer_0/1/2` vi do la object keo tha, khong phai text ket qua.

---

## 5. Scene `PhiThuyen` - game phi thuyen

Export cho thay scene co:
- Root `UiSpaceManager`
- Root `GamePlay`
- Canvas background: `GamePlay/DoHoa/Canvas`
- Canvas gameplay: `GamePlay/QuesUI`
- Root Canvas menu/shop: `Ui`
- Panel ket thuc: `Ui/WinPanel`, `Ui/LosePanel`
- Setting: `Ui/SettingPanel`

Code lien quan:
- `Assets/Script/Script_Space/SpaceShipPhysics.cs`
- `Assets/Script/Script_Space/SpaceShipManager.cs`
- `Assets/Script/Data_Scrip/UiSp.cs`
- `Assets/Script/Script_Space/Enemy.cs`

Luong code that:
- Khi phi thuyen qua gate, `SpaceShipPhysics` so sanh `gateText.text` voi `SpaceShipManager.Instance.currentCorrectAnswer`.
- Dung: `SpaceShipManager.CountCorrectAnswer()`. Du so cong thi `UiSp.Instance.ShowWin()`.
- Sai: code hien tai van goi `SetCauHoiDungYenResult("Ket qua:", correctAnswer)` de hien dap an dung, nhung khong co text `Dung/Sai` rieng.
- Thua: `Enemy.WaitAndShowLose()` goi `UiSp.Instance.ShowLose()`.
- `UiSp.ShowWin()`/`ShowLose()` cap nhat panel va bat `panelWin`/`panelLose`.
- Tien tren map: `SpaceShipManager.TrySpawnCoin()` sinh coin, `CoinSpace.OnTriggerEnter2D()` bat va cham voi `Player`, coin bay ve `UiSp.coinTarget`, sau do `UiSp.AddCoins(1)`.

### Gan component

Tren `AudioManager` hoac `AudioBridge`:
- `AudioEventBridge`
- `GlobalClickAudioListener`

Tren Canvas:
- `GamePlay/DoHoa/Canvas` -> khong bat buoc gan `UIButtonAudioHelper` vi chi thay background UI, khong co Button.
- `GamePlay/QuesUI` -> gan `UIButtonAudioHelper`
- `Ui` -> gan `UIButtonAudioHelper`
- `Ui/ShopPanel/ThongBaoCv` neu la Canvas rieng -> gan `UIButtonAudioHelper`

### AudioEventBridge settings

`sceneMusicType`:
- Chon `Space`

Manual references:
- `Battle Status Text Object`: de trong. Khong co countdown battle text trong export.
- `Answer Timer Text Object`: de trong. Khong co timer text.
- `Answer Result Text Object`: de trong. `DkWin` chi hien tien do `correctAnswersCount/totalGatesToWin`, khong phai dung/sai.

### Cac object button duoc hook tu dong

Trong `GamePlay/QuesUI`:
- `StBnt`

Trong `Ui`:
- `ShopPanel/Play`
- `ShopPanel/Back`
- `ShopPanel/MuaBtn`
- `Pt1Bnt`
- `Pt2Bnt`
- `Pt3Bnt`
- `ChonManPanel/Back`
- `SettingPanel/ThoatBtn`
- `SettingPanel/ChoiLai`
- `SettingPanel/TiepTucBtn`
- `WinPanel/ShopBtn`
- `WinPanel/NextBnt`
- `LosePanel/ShopBtn`
- `LosePanel/RetryBnt`

### Dung/sai/win/lose

Scene `PhiThuyen` nen chen audio vao code:
- Trong `SpaceShipPhysics`, sau khi tinh `bool isCorrect = ...`, neu `isCorrect`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundCorrect);`
- Trong `SpaceShipPhysics`, nhanh `else` cua `isCorrect`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundWrong);`
- Trong `UiSp.ShowWinRoutine`, truoc khi bat `panelWin.SetActive(true)`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundWin);`
- Trong `UiSp.ShowLoseRoutine` va `ShowLosePanelDirectly`, truoc khi bat `panelLose.SetActive(true)`: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundLose);`
- Trong `CoinSpace.OnTriggerEnter2D`, khi player nhat coin: `AudioManager.Instance?.PlaySFX(AudioManager.Instance.soundCoin);`

### Setup them cho coin scene `PhiThuyen`

- Tren `AudioManager`: gan clip vao field `soundCoin`.
- Tren `AudioEventBridge` scene `PhiThuyen`: gan clip vao `sfxCoin` neu muon coin cua scene nay dung clip rieng.
- Dam bao coin prefab (`CoidSp.prefab`) co `CoinSpace` + `Collider2D (isTrigger=true)`, va phi thuyen co tag `Player`.

---

## 6. Scene `Test_FireBase_multi` - multiplayer lobby + battle

Export cho thay scene co:
- Root `Canvas`
- `LobbyPanel`
- `GameplayPanel`
- `Wins`
- `LobbyBrowserPanel`
- `LoadingPanel`
- `BattleQuitConfirmPopup`
- `BattleManager`
- `NetworkManager`

Day la scene co day du text de `AudioEventBridge` bat su kien tot nhat.

Code lien quan:
- `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerBattleController.cs`
- `Assets/Script/Script_multiplayer/1Code/Multiplay/AnswerSummaryUI.cs`
- `Assets/Script/Script_multiplayer/1Code/Multiplay/NetworkedMathBattleManager.cs`
- `Assets/Script/Script_multiplayer/1Code/CODE/UIWinsController.cs`

Luong code that:
- Countdown nam trong `UIMultiplayerBattleController.CountdownRoutine()`, set `battleStatusText` lan luot `3`, `2`, `1`, `Ready`, `GO!`.
- Sau countdown, `battleStatusText` doi thanh text trang thai tra loi, nen bridge chi nen dung no de bat countdown.
- Timer nam trong `AnswerSummaryUI.timerText`, object export la `Timertext`.
- Ket qua tung cau nam trong `AnswerSummaryUI.resultText`, export la `TrangThaiWin`.
- `TextTrangThaiDapAn1/2` chi hien cau tra loi cua tung player va response time, khong phai text tong ket dung/sai tot nhat cho audio.
- Ket qua tran nam trong `UIWinsController`, text title la `Wins/trangThai_Thang_Thua`.

### Gan component

Tren `AudioManager` hoac `AudioBridge`:
- `AudioEventBridge`
- `GlobalClickAudioListener`

Tren Canvas:
- `Canvas` -> gan `UIButtonAudioHelper`

Neu cac popup/panel la Canvas rieng trong ban scene cua ban thi gan them `UIButtonAudioHelper`, nhung export hien tai chi thay root `Canvas` la Canvas chinh.

### AudioEventBridge settings

`sceneMusicType`:
- Chon `Multiplayer`

Manual references:
- `Battle Status Text Object`: keo `Canvas/GameplayPanel/Text (TMP) TrangThai`
- `Answer Timer Text Object`: keo `Canvas/GameplayPanel/HealthBarContainer/TimerPanel/Timertext`
- `Answer Result Text Object`: de trong. Dung/sai multiplayer da duoc goi truc tiep trong `UIMultiplayerBattleController.HandleAnswerResultDetailed()` de tinh theo local player, tranh phat trung voi bridge.

Khong uu tien gan `TextTrangThaiDapAn1/2` vao `Answer Result Text Object`: code `AnswerSummaryUI` set 2 text nay thanh chu kieu `Dap an nguoi choi 1 chon la...`, khong on dinh de phan biet dung/sai audio.

### Cac object button duoc hook tu dong

Trong `LobbyPanel`:
- `HostButton`
- `JoinButton`
- `Back`
- `DS Room`
- `QuickButton`
- `QuitButton`
- `SanSangButton`
- `StartButton`
- `RefreshButton`
- `BackToModSelectionNavigator`

Trong `GameplayPanel`:
- `Quit`

Trong `Wins`:
- `TiepTucBtn`

Trong `LobbyBrowserPanel`:
- `RefreshButton`
- `BackButton`

Trong `BattleQuitConfirmPopup`:
- `confirmQuitButton`
- `CancelButton`

### Countdown

`Battle Status Text Object = Text (TMP) TrangThai`.

Bridge se phat:
- Text thanh `3` -> `AudioManager.PlayCountdown3()`
- Text thanh `2` -> `AudioManager.PlayCountdown2()`
- Text thanh `1` -> `AudioManager.PlayCountdown1()`
- Text co `Ready` -> `AudioManager.PlayCountdownReady()`
- Text co `Go` -> `AudioManager.PlayCountdownGo()`

### Timer warning

`Answer Timer Text Object = Timertext`.

Bridge chi phat warning neu text parse duoc so giay, vi du:
- `5`
- `5s`
- `4`
- `4s`

Neu text la `00:05`, bridge hien tai khong parse duoc. Khi do can sua script parse timer hoac doi format text timer.

### Dung/sai

`Answer Result Text Object` can la text doi sang mot trong cac chu:
- Dung: `correct`, `dung`, `true`
- Sai: `wrong`, `sai`, `false`

Trong scene multiplayer, khong can gan object vao field nay nua. Audio dung/sai da duoc chen truc tiep vao code theo dap an cua local player.

Luu y: `AnswerSummaryUI.DisplayResult()` hien text co color tag va tieng Viet. Doc text bang bridge de phat dung/sai rat de sai local player, nen hien tai dung code truc tiep trong `UIMultiplayerBattleController.HandleAnswerResultDetailed()`.

### Win/Lose

Panel ket qua:
- `Canvas/Wins`
- Text ket qua: `Canvas/Wins/trangThai_Thang_Thua`

Win/lose da duoc chen truc tiep trong `UIWinsController.DisplayMatchResult()`:
- Local player thang: phat `soundWin`.
- Local player thua: phat `soundLose`.

---

## 7. Checklist test nhanh

Sau khi setup xong tung scene:

1. Play scene.
2. Bam mot button bat ky -> nghe `soundClick`.
3. Click vung ngoai UI/button -> nghe cung `soundClick`.
4. Bam button khong bi phat 2 lan. Neu bi double, kiem tra `GlobalClickAudioListener.onlyNonUIClicks = true`.
5. Multiplayer: khi `Text (TMP) TrangThai` doi `3/2/1/GO` -> nghe countdown.
6. Multiplayer: khi `Timertext` con `5` -> nghe timer warning neu da co clip warning trong AudioManager.
7. Dung/sai/win/lose: neu khong nghe, kiem tra scene co result text dung/sai hay chua. Voi scene don, nen goi truc tiep trong script gameplay.

---

## 8. Bang tom tat keo object vao Manual references

| Scene | `sceneMusicType` | `Battle Status Text Object` | `Answer Timer Text Object` | `Answer Result Text Object` |
|---|---|---|---|---|
| `GameUIPlay 1` | `MainMenu` | de trong | de trong | de trong |
| `ChonDA` | `Class` | de trong | de trong | de trong |
| `KeoThaDA` | `Defense` | de trong | de trong | de trong |
| `PhiThuyen` | `Space` | de trong | de trong | de trong |
| `Test_FireBase_multi` | `Multiplayer` | `Canvas/GameplayPanel/Text (TMP) TrangThai` | `Canvas/GameplayPanel/HealthBarContainer/TimerPanel/Timertext` | de trong vi dung/sai da goi truc tiep trong code |
