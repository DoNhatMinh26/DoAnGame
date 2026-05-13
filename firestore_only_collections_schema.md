# Firestore Only Collections Schema (Multiplayer-Study)

Tài liệu mô tả **5 collection** chỉ lưu trên **Google Cloud Firestore** (không có quan hệ enforcement như SQL). Nội dung dựa trực tiếp trên file:
`Assets/Script/Script_multiplayer/1Code/firestore_only_collections_schema.sql`.

> Lưu ý: Firestore document id chính là **PRIMARY KEY** trong file mô phỏng SQL.

---

## 1) Collection: `users`
**Document:** `users/{uid}`

**Mục đích:** Thông tin tài khoản Firebase Auth và profile người chơi.
- Tạo/cập nhật bởi: `FirebaseManager`, `AuthManager`, `SessionGuardService`.

### Fields
| Thuộc tính | Kiểu dữ liệu (mô phỏng SQL) | Mô tả thuộc tính | Giá trị mặc định |
|---|---:|---|---|
| `uid` | `VARCHAR(128)` | Firebase Auth UID, đồng thời là document id | Không (bắt buộc, PRIMARY KEY) |
| `email` | `VARCHAR(255)` | Email đăng nhập | Không (NOT NULL) |
| `characterName` | `VARCHAR(100)` | Tên nhân vật, thường dùng query unique khi đăng ký | Không (NOT NULL) |
| `grade` | `INT` | Lớp học: `1..5` | Không (NOT NULL) |
| `avatar` | `VARCHAR(255)` | Chuỗi avatar cũ/dự phòng | `''` |
| `avatarId` | `INT` | Avatar đang chọn | `0` |
| `createdAt` | `BIGINT` | Unix timestamp milliseconds | Không (NOT NULL) |
| `lastLogin` | `BIGINT` | Unix timestamp milliseconds | Không (NOT NULL) |
| `isActive` | `BOOLEAN` | Trạng thái hoạt động | `TRUE` |
| `emailVerified` | `BOOLEAN` | Email đã verify hay chưa | `FALSE` |
| `activeSessionId` | `VARCHAR(128)` | Chống đăng nhập cùng lúc trên nhiều máy | `NULL` |

---

## 2) Collection: `playerData`
**Document:** `playerData/{uid}`

**Mục đích:** Thống kê tổng của người chơi, dùng cho profile và bảng xếp hạng.
- Tạo/cập nhật bởi: `FirebaseManager`, `CloudSyncService`, `multiplayer result`.

### Fields
| Thuộc tính | Kiểu dữ liệu (mô phỏng SQL) | Mô tả thuộc tính | Giá trị mặc định |
|---|---:|---|---|
| `uid` | `VARCHAR(128)` | Document id, đồng bộ với `users.uid` | Không (bắt buộc, PRIMARY KEY) |
| `characterName` | `VARCHAR(100)` | Tên nhân vật (dup với `users.characterName` để hiển thị nhanh) | Không (NOT NULL) |
| `level` | `INT` | Level hiện tại | `1` |
| `totalXp` | `INT` | Tổng XP | `0` |
| `totalScore` | `INT` | Tổng điểm | `0` |
| `rank` | `INT` | Hạng (có thể tính từ `totalScore`) | `0` |
| `coins` | `INT` | Số coin | `0` |
| `gamesPlayed` | `INT` | Số ván đã chơi | `0` |
| `gamesWon` | `INT` | Số ván thắng | `0` |
| `winRate` | `FLOAT` | Tỉ lệ thắng | `0.0` |
| `lastUpdated` | `BIGINT` | Unix timestamp milliseconds thời điểm cập nhật gần nhất | Không (NOT NULL) |

---

## 3) Collection: `gameModeProgress`
**Document:** `gameModeProgress/{uid}_{gameMode}_{grade}`

**Mục đích:** Tiến độ theo **từng game mode** và **từng lớp (grade)**.
- Mỗi user có **15 document** khởi tạo: `3 gameMode x 5 grade`.

### Fields
| Thuộc tính | Kiểu dữ liệu (mô phỏng SQL) | Mô tả thuộc tính | Giá trị mặc định |
|---|---:|---|---|
| `progressId` | `VARCHAR(180)` | Document id: `{uid}_{gameMode}_{grade}` | Không (bắt buộc, PRIMARY KEY) |
| `uid` | `VARCHAR(128)` | Liên kết tới `users.uid` | Không (NOT NULL) |
| `gameMode` | `VARCHAR(50)` | Loại game: `chonda`  `keothada`  `phithuyen` | Không (NOT NULL) |
| `grade` | `INT` | Lớp `1..5` | Không (NOT NULL) |
| `currentLevel` | `INT` | Level đang ở | `1` |
| `maxLevelUnlocked` | `INT` | Level cao nhất đã mở khóa | `1` |
| `totalScore` | `INT` | Tổng điểm trong gameMode/grade | `0` |
| `bestScore` | `INT` | Điểm tốt nhất | `0` |
| `lastPlayed` | `BIGINT` | Thời gian chơi gần nhất (NULL nếu chưa chơi) | `NULL` |

---

## 4) Collection: `levelProgress`
**Document:** `levelProgress/{uid}_{gameMode}_{grade}_{levelNumber}`

**Mục đích:** Điểm cao nhất và số lần chơi của **từng level**.
- **Không tạo khi đăng ký**.
- Chỉ tạo khi người chơi hoàn thành/chơi level đó lần đầu.

### Fields
| Thuộc tính | Kiểu dữ liệu (mô phỏng SQL) | Mô tả thuộc tính | Giá trị mặc định |
|---|---:|---|---|
| `progressId` | `VARCHAR(200)` | Document id: `{uid}_{gameMode}_{grade}_{levelNumber}` | Không (bắt buộc, PRIMARY KEY) |
| `uid` | `VARCHAR(128)` | Liên kết tới `users.uid` | Không (NOT NULL) |
| `gameMode` | `VARCHAR(50)` | `chonda`  `keothada`  `phithuyen` | Không (NOT NULL) |
| `grade` | `INT` | Lớp `1..5` | Không (NOT NULL) |
| `levelNumber` | `INT` | Số thứ tự level (ví dụ `1..100`) | Không (NOT NULL) |
| `bestScore` | `INT` | Điểm cao nhất ở level đó | `0` |
| `attempts` | `INT` | Số lần chơi level đó | `0` |

---

## 5) Collection: `playerShop`
**Document:** `playerShop/{uid}_{shopType}`

**Mục đích:** Trạng thái skin/item đã mua và đang trang bị.
- `shopType`:
  - `chonda_skin`
  - `keothada_skin`
  - `keothada_phao`
  - `phithuyen_ship`

### Fields
| Thuộc tính | Kiểu dữ liệu (mô phỏng SQL) | Mô tả thuộc tính | Giá trị mặc định |
|---|---:|---|---|
| `docId` | `VARCHAR(180)` | Document id: `{uid}_{shopType}` | Không (bắt buộc, PRIMARY KEY) |
| `uid` | `VARCHAR(128)` | Lấy từ docId để suy ra quan hệ tới `users.uid` | Không (NOT NULL) |
| `shopType` | `VARCHAR(50)` | Loại shop (suy ra từ docId) | Không (NOT NULL) |
| `selectedId` | `INT` | Id item/skin đang trang bị | `0` |
| `unlockedIds` | `VARCHAR(255)` | Lưu danh sách ids đã mở dưới dạng chuỗi: `"0,1,2"` | `'0'` |

---

## Tóm tắt quan hệ logic (FireStore không enforce)
- `users` (1)  (1) `playerData` : `users.uid == playerData.uid`
- `users` (1)  (N) `gameModeProgress` : `users.uid == gameModeProgress.uid`
- `users` (1)  (N) `levelProgress` : `users.uid == levelProgress.uid`
- `gameModeProgress` (1)  (N) `levelProgress` : khớp `uid + gameMode + grade`
- `users` (1)  (N) `playerShop` : `docId = {uid}_{shopType}`

---

## Default documents khi đăng ký user mới (theo file SQL)
- `users/{uid}`: avatar `''`, avatarId `0`, isActive `TRUE`, emailVerified `FALSE`, activeSessionId `NULL`.
- `playerData/{uid}`: level `1`, totalXp `0`, totalScore `0`, rank `0`, coins `0`, gamesPlayed `0`, gamesWon `0`, winRate `0.0`.
- `gameModeProgress`: tạo 15 docs (3 gameMode x 5 grade), mỗi doc có `currentLevel=1`, `maxLevelUnlocked=1`, `totalScore=0`, `bestScore=0`, `lastPlayed=NULL`.
- `levelProgress`: không tạo lúc đăng ký; chỉ tạo khi chơi level.
- `playerShop`: tạo 4 docs (4 shopType), `selectedId=0`, `unlockedIds='0'`.

