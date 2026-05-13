# Tài Liệu Schema Firestore - Game Toán Học Multiplayer

## Tổng Quan

Dự án: **Multiplayer-Study / Cloud Firestore (default)**

Mục đích: Lưu trữ dữ liệu người chơi, tiến độ game, và thông tin shop trên Cloud Firestore.

**Lưu ý**: Firestore không chạy file SQL trực tiếp. File schema SQL chỉ dùng để mô phỏng cấu trúc collection/document/field giống màn hình Firebase Console.

---

## Danh Sách Collections

Hệ thống có **5 collections** chính:

1. `users` - Thông tin tài khoản và profile người chơi
2. `playerData` - Thống kê tổng hợp của người chơi
3. `gameModeProgress` - Tiến độ theo từng chế độ game và lớp học
4. `levelProgress` - Điểm cao nhất và số lần chơi từng level
5. `playerShop` - Trạng thái skin/item đã mua và đang trang bị

---

## Tóm Tắt Nhanh Các Thuộc Tính

### Collection `users`
**Thuộc tính**: `uid`, `email`, `characterName`, `grade`, `avatar`, `avatarId`, `createdAt`, `lastLogin`, `isActive`, `emailVerified`, `activeSessionId`

### Collection `playerData`
**Thuộc tính**: `uid`, `characterName`, `level`, `totalXp`, `totalScore`, `rank`, `coins`, `gamesPlayed`, `gamesWon`, `winRate`, `lastUpdated`

### Collection `gameModeProgress`
**Thuộc tính**: `progressId`, `uid`, `gameMode`, `grade`, `currentLevel`, `maxLevelUnlocked`, `totalScore`, `bestScore`, `lastPlayed`

### Collection `levelProgress`
**Thuộc tính**: `progressId`, `uid`, `gameMode`, `grade`, `levelNumber`, `bestScore`, `attempts`

### Collection `playerShop`
**Thuộc tính**: `docId`, `uid`, `shopType`, `selectedId`, `unlockedIds`

---

## 1. Collection: `users`

**Thuộc tính**: `uid`, `email`, `characterName`, `grade`, `avatar`, `avatarId`, `createdAt`, `lastLogin`, `isActive`, `emailVerified`, `activeSessionId`

### Mô Tả
Lưu trữ thông tin tài khoản Firebase Auth và profile người chơi.

**Document Path**: `users/{uid}`

**Tạo/Cập nhật bởi**: FirebaseManager, AuthManager, SessionGuardService

### Cấu Trúc Thuộc Tính

| Tên Thuộc Tính | Kiểu Dữ Liệu | Mô Tả | Giá Trị Mặc Định |
|----------------|--------------|-------|------------------|
| `uid` | VARCHAR(128) | Firebase Auth UID, cũng là document ID (Primary Key) | *Bắt buộc* |
| `email` | VARCHAR(255) | Email đăng nhập | *Bắt buộc* |
| `characterName` | VARCHAR(100) | Tên nhân vật, phải unique khi đăng ký | *Bắt buộc* |
| `grade` | INT | Lớp học (1-5) | *Bắt buộc* |
| `avatar` | VARCHAR(255) | Chuỗi avatar cũ/dự phòng | `''` (chuỗi rỗng) |
| `avatarId` | INT | ID avatar đang chọn | `0` |
| `createdAt` | BIGINT | Thời gian tạo tài khoản (Unix timestamp milliseconds) | *Bắt buộc* |
| `lastLogin` | BIGINT | Thời gian đăng nhập gần nhất (Unix timestamp milliseconds) | *Bắt buộc* |
| `isActive` | BOOLEAN | Trạng thái tài khoản có hoạt động không | `TRUE` |
| `emailVerified` | BOOLEAN | Email đã được xác thực chưa | `FALSE` |
| `activeSessionId` | VARCHAR(128) | ID phiên đăng nhập hiện tại (chống đăng nhập cùng lúc trên nhiều máy) | `NULL` |

### Index
- `idx_users_characterName` trên `characterName` - Dùng cho query kiểm tra tên nhân vật unique

### Ví Dụ Document
```json
{
  "uid": "55nKozksIjhBfEaLNCinctdMFx43",
  "email": "player@gmail.com",
  "characterName": "PlayerName",
  "grade": 1,
  "avatar": "",
  "avatarId": 0,
  "createdAt": 1714089600000,
  "lastLogin": 1714089600000,
  "isActive": true,
  "emailVerified": false,
  "activeSessionId": null
}
```

---

## 2. Collection: `playerData`

**Thuộc tính**: `uid`, `characterName`, `level`, `totalXp`, `totalScore`, `rank`, `coins`, `gamesPlayed`, `gamesWon`, `winRate`, `lastUpdated`

### Mô Tả
Lưu trữ thống kê tổng hợp của người chơi, dùng cho profile và bảng xếp hạng.

**Document Path**: `playerData/{uid}`

**Tạo/Cập nhật bởi**: FirebaseManager, CloudSyncService, kết quả multiplayer

### Cấu Trúc Thuộc Tính

| Tên Thuộc Tính | Kiểu Dữ Liệu | Mô Tả | Giá Trị Mặc Định |
|----------------|--------------|-------|------------------|
| `uid` | VARCHAR(128) | Document ID, tham chiếu đến `users.uid` (Primary Key, Foreign Key) | *Bắt buộc* |
| `characterName` | VARCHAR(100) | Tên nhân vật (duplicate từ users để hiển thị nhanh) | *Bắt buộc* |
| `level` | INT | Cấp độ người chơi | `1` |
| `totalXp` | INT | Tổng điểm kinh nghiệm | `0` |
| `totalScore` | INT | Tổng điểm số | `0` |
| `rank` | INT | Xếp hạng (có thể tính từ totalScore) | `0` |
| `coins` | INT | Số xu hiện có | `0` |
| `gamesPlayed` | INT | Tổng số trận đã chơi | `0` |
| `gamesWon` | INT | Tổng số trận thắng | `0` |
| `winRate` | FLOAT | Tỷ lệ thắng (%) | `0.0` |
| `lastUpdated` | BIGINT | Thời gian cập nhật gần nhất (Unix timestamp milliseconds) | *Bắt buộc* |

### Index
- `idx_playerData_totalScore` trên `totalScore DESC` - Dùng cho leaderboard (sắp xếp giảm dần)

### Ví Dụ Document
```json
{
  "uid": "55nKozksIjhBfEaLNCinctdMFx43",
  "characterName": "PlayerName",
  "level": 5,
  "totalXp": 1250,
  "totalScore": 8500,
  "rank": 42,
  "coins": 350,
  "gamesPlayed": 45,
  "gamesWon": 32,
  "winRate": 71.1,
  "lastUpdated": 1714089600000
}
```

---

## 3. Collection: `gameModeProgress`

**Thuộc tính**: `progressId`, `uid`, `gameMode`, `grade`, `currentLevel`, `maxLevelUnlocked`, `totalScore`, `bestScore`, `lastPlayed`

### Mô Tả
Lưu trữ tiến độ theo từng chế độ game và từng lớp học. Mỗi user có **15 documents** ban đầu (3 gameMode × 5 grade).

**Document Path**: `gameModeProgress/{uid}_{gameMode}_{grade}`

**Ví dụ**: `gameModeProgress/55nKozksIjhBfEaLNCinctdMFx43_chonda_1`

### Cấu Trúc Thuộc Tính

| Tên Thuộc Tính | Kiểu Dữ Liệu | Mô Tả | Giá Trị Mặc Định |
|----------------|--------------|-------|------------------|
| `progressId` | VARCHAR(180) | Document ID theo format `{uid}_{gameMode}_{grade}` (Primary Key) | *Bắt buộc* |
| `uid` | VARCHAR(128) | User ID, tham chiếu đến `users.uid` (Foreign Key) | *Bắt buộc* |
| `gameMode` | VARCHAR(50) | Chế độ game: `chonda`, `keothada`, hoặc `phithuyen` | *Bắt buộc* |
| `grade` | INT | Lớp học (1-5) | *Bắt buộc* |
| `currentLevel` | INT | Level hiện tại đang chơi | `1` |
| `maxLevelUnlocked` | INT | Level cao nhất đã mở khóa | `1` |
| `totalScore` | INT | Tổng điểm trong chế độ này | `0` |
| `bestScore` | INT | Điểm cao nhất đạt được | `0` |
| `lastPlayed` | BIGINT | Thời gian chơi gần nhất (Unix timestamp milliseconds), `NULL` nếu chưa chơi | `NULL` |

### Index
- `idx_gameModeProgress_uid` trên `uid`
- `idx_gameModeProgress_unique` trên `(uid, gameMode, grade)` - Đảm bảo unique

### Game Modes
- **chonda** - Chọn đáp án
- **keothada** - Kéo thả đáp án
- **phithuyen** - Phi thuyền

### Ví Dụ Document
```json
{
  "progressId": "55nKozksIjhBfEaLNCinctdMFx43_chonda_1",
  "uid": "55nKozksIjhBfEaLNCinctdMFx43",
  "gameMode": "chonda",
  "grade": 1,
  "currentLevel": 5,
  "maxLevelUnlocked": 5,
  "totalScore": 2850,
  "bestScore": 980,
  "lastPlayed": 1714089600000
}
```

---

## 4. Collection: `levelProgress`

**Thuộc tính**: `progressId`, `uid`, `gameMode`, `grade`, `levelNumber`, `bestScore`, `attempts`

### Mô Tả
Lưu trữ điểm cao nhất và số lần chơi của từng level đã chơi. **Chỉ tạo document khi người chơi chơi level đó lần đầu**.

**Document Path**: `levelProgress/{uid}_{gameMode}_{grade}_{levelNumber}`

**Ví dụ**: `levelProgress/55nKozksIjhBfEaLNCinctdMFx43_chonda_1_5`

### Cấu Trúc Thuộc Tính

| Tên Thuộc Tính | Kiểu Dữ Liệu | Mô Tả | Giá Trị Mặc Định |
|----------------|--------------|-------|------------------|
| `progressId` | VARCHAR(200) | Document ID theo format `{uid}_{gameMode}_{grade}_{levelNumber}` (Primary Key) | *Bắt buộc* |
| `uid` | VARCHAR(128) | User ID, tham chiếu đến `users.uid` (Foreign Key) | *Bắt buộc* |
| `gameMode` | VARCHAR(50) | Chế độ game: `chonda`, `keothada`, hoặc `phithuyen` | *Bắt buộc* |
| `grade` | INT | Lớp học (1-5) | *Bắt buộc* |
| `levelNumber` | INT | Số thứ tự level (1-100) | *Bắt buộc* |
| `bestScore` | INT | Điểm cao nhất đạt được ở level này | `0` |
| `attempts` | INT | Số lần đã chơi level này | `0` |

### Index
- `idx_levelProgress_by_mode_grade` trên `(uid, gameMode, grade)` - Dùng cho query lấy tất cả level của một chế độ

### Ví Dụ Document
```json
{
  "progressId": "55nKozksIjhBfEaLNCinctdMFx43_chonda_1_5",
  "uid": "55nKozksIjhBfEaLNCinctdMFx43",
  "gameMode": "chonda",
  "grade": 1,
  "levelNumber": 5,
  "bestScore": 980,
  "attempts": 3
}
```

---

## 5. Collection: `playerShop`

**Thuộc tính**: `docId`, `uid`, `shopType`, `selectedId`, `unlockedIds`

### Mô Tả
Lưu trữ trạng thái skin/item đã mua và đang trang bị. Mỗi user có **4 documents** (4 shopType).

**Document Path**: `playerShop/{uid}_{shopType}`

**Ví dụ**: `playerShop/55nKozksIjhBfEaLNCinctdMFx43_chonda_skin`

### Cấu Trúc Thuộc Tính

| Tên Thuộc Tính | Kiểu Dữ Liệu | Mô Tả | Giá Trị Mặc Định |
|----------------|--------------|-------|------------------|
| `docId` | VARCHAR(180) | Document ID theo format `{uid}_{shopType}` (Primary Key) | *Bắt buộc* |
| `uid` | VARCHAR(128) | User ID, tham chiếu đến `users.uid` (Foreign Key) | *Bắt buộc* |
| `shopType` | VARCHAR(50) | Loại shop (xem danh sách bên dưới) | *Bắt buộc* |
| `selectedId` | INT | ID item đang trang bị | `0` |
| `unlockedIds` | VARCHAR(255) | Danh sách ID items đã mở khóa (format: "0,1,2") | `'0'` |

### Shop Types
- **chonda_skin** - Skin cho game Chọn Đáp Án
- **keothada_skin** - Skin cho game Kéo Thả
- **keothada_phao** - Pháo cho game Kéo Thả
- **phithuyen_ship** - Tàu vũ trụ cho game Phi Thuyền

### Ví Dụ Document
```json
{
  "docId": "55nKozksIjhBfEaLNCinctdMFx43_chonda_skin",
  "uid": "55nKozksIjhBfEaLNCinctdMFx43",
  "shopType": "chonda_skin",
  "selectedId": 2,
  "unlockedIds": "0,1,2,5"
}
```

---

## Mối Quan Hệ Giữa Các Collections

### 1. users ↔ playerData (1:1)
- Mỗi user có đúng 1 playerData
- Join: `users.uid == playerData.uid`

### 2. users ↔ gameModeProgress (1:N)
- Mỗi user có 15 gameModeProgress documents (3 gameMode × 5 grade)
- Join: `users.uid == gameModeProgress.uid`

### 3. users ↔ levelProgress (1:N)
- Mỗi user có nhiều levelProgress (chỉ tạo khi chơi level lần đầu)
- Join: `users.uid == levelProgress.uid`

### 4. users ↔ playerShop (1:N)
- Mỗi user có 4 playerShop documents (4 shopType)
- Join: `docId` bắt đầu với `{uid}_`

### 5. gameModeProgress ↔ levelProgress (1:N)
- Mỗi gameModeProgress có nhiều levelProgress (levelNumber 1-100)
- Join: `uid + gameMode + grade`

### 6. playerData ↔ gameModeProgress/levelProgress (Tổng hợp)
- playerData là tổng hợp điểm/coins/xp từ các chế độ và trận đấu
- Firestore chỉ lưu giá trị tổng, không lưu bảng lịch sử

---

## Dữ Liệu Mặc Định Khi Đăng Ký User Mới

### 1. Tạo Document `users/{uid}`
```json
{
  "uid": "{uid}",
  "email": "player@gmail.com",
  "characterName": "PlayerName",
  "grade": 1,
  "avatar": "",
  "avatarId": 0,
  "createdAt": 1714089600000,
  "lastLogin": 1714089600000,
  "isActive": true,
  "emailVerified": false,
  "activeSessionId": null
}
```

### 2. Tạo Document `playerData/{uid}`
```json
{
  "uid": "{uid}",
  "characterName": "PlayerName",
  "level": 1,
  "totalXp": 0,
  "totalScore": 0,
  "rank": 0,
  "coins": 0,
  "gamesPlayed": 0,
  "gamesWon": 0,
  "winRate": 0.0,
  "lastUpdated": 1714089600000
}
```

### 3. Tạo 15 Documents `gameModeProgress`
Tạo cho mỗi tổ hợp (gameMode, grade):
- chonda: grade 1-5
- keothada: grade 1-5
- phithuyen: grade 1-5

Ví dụ:
```json
{
  "progressId": "{uid}_chonda_1",
  "uid": "{uid}",
  "gameMode": "chonda",
  "grade": 1,
  "currentLevel": 1,
  "maxLevelUnlocked": 1,
  "totalScore": 0,
  "bestScore": 0,
  "lastPlayed": null
}
```

### 4. Không Tạo `levelProgress` Lúc Đăng Ký
- Chỉ tạo khi người chơi hoàn thành level lần đầu

### 5. Tạo 4 Documents `playerShop`
```json
{
  "docId": "{uid}_chonda_skin",
  "uid": "{uid}",
  "shopType": "chonda_skin",
  "selectedId": 0,
  "unlockedIds": "0"
}
```
(Tương tự cho: keothada_skin, keothada_phao, phithuyen_ship)

---

## Queries Firestore Thường Dùng

### 1. Kiểm tra tên nhân vật unique
```javascript
users.whereEqualTo("characterName", value).limit(1)
```

### 2. Lấy bảng xếp hạng
```javascript
playerData.orderBy("totalScore", "desc").limit(10)
```

### 3. Lấy tất cả tiến độ của user theo chế độ và lớp
```javascript
levelProgress
  .whereEqualTo("uid", uid)
  .whereEqualTo("gameMode", gameMode)
  .whereEqualTo("grade", grade)
```

---

## Lưu Ý Quan Trọng

1. **Firestore không chạy file SQL**: File schema này chỉ để tham khảo cấu trúc
2. **Không có Foreign Key thực tế**: Firestore không enforce relationships, phải quản lý trong code
3. **Document ID có ý nghĩa**: Nhiều collection dùng composite ID (`{uid}_{gameMode}_{grade}`)
4. **Lazy creation**: `levelProgress` chỉ tạo khi cần, không tạo trước 100 documents
5. **Denormalization**: `characterName` được duplicate trong `playerData` để query nhanh

---

## Collections Không Sử Dụng

Các bảng sau **KHÔNG** được lưu trên Firestore (có thể từ schema cũ):
- `game_history`
- `leaderboard_score`
- `leaderboard_level`
- `game_sessions`
- `achievements`

---

**Ngày tạo tài liệu**: 2026-05-13  
**Phiên bản**: 1.0  
**Tác giả**: Auto-generated from firestore_only_collections_schema.sql
