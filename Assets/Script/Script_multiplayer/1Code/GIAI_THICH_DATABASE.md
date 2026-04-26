# 📖 Giải thích chi tiết Database Schema - Tối Ưu

> **Lưu ý:** Database này CHỈ lưu dữ liệu quan trọng để sync cross-device. Không dùng hệ thống sao, chỉ tính điểm.

---

## 1️⃣ TABLE: users (Thông tin tài khoản)

Lưu thông tin cơ bản của người dùng khi đăng ký.

| Trường | Kiểu | Mô tả | Ví dụ |
|--------|------|-------|-------|
| **uid** | VARCHAR(128) | ID duy nhất từ Firebase Auth (khóa chính) | `8fmJ5oe0ghdbJkfwK8bp48irnX2` |
| **email** | VARCHAR(255) | Email đăng nhập (duy nhất) | `player1@gmail.com` |
| **characterName** | VARCHAR(100) | Tên nhân vật trong game (duy nhất) | `MeoNinja` |
| **age** | INT | Tuổi người chơi (6-12) | `10` |
| **createdAt** | BIGINT | Timestamp tạo tài khoản (milliseconds) | `1714089600000` |
| **lastLogin** | BIGINT | Timestamp đăng nhập gần nhất | `1714089600000` |

**Khi nào tạo:** Khi user đăng ký tài khoản mới

**Khi nào cập nhật:** 
- `lastLogin`: Mỗi lần đăng nhập

**Đã xóa:** avatar, emailVerified, isActive (không quan trọng cho cross-device sync)

---

## 2️⃣ TABLE: playerData (Dữ liệu game - Quan trọng nhất!)

Lưu thống kê tổng quan của người chơi, dùng cho **bảng xếp hạng**.

| Trường | Kiểu | Mô tả | Ví dụ | Cách tính |
|--------|------|-------|-------|-----------|
| **uid** | VARCHAR(128) | ID người chơi (khóa chính, liên kết với users) | `8fmJ5oe0ghdbJkfwK8bp48irnX2` | Từ Firebase Auth |
| **characterName** | VARCHAR(100) | Tên nhân vật (sync từ users) | `MeoNinja` | Copy từ users |
| **totalScore** | INT | **Tổng điểm** (dùng xếp hạng) ⭐ | `8750` | Cộng dồn mỗi trận |
| **totalXp** | INT | Tổng kinh nghiệm tích lũy | `1500` | Cộng dồn mỗi trận |
| **level** | INT | Level hiện tại | `15` | `1 + (totalXp / 100)` |
| **gamesPlayed** | INT | Số trận đã chơi | `42` | Tăng 1 mỗi trận |
| **gamesWon** | INT | Số trận thắng | `28` | Tăng 1 khi thắng |
| **lastUpdated** | BIGINT | Timestamp cập nhật gần nhất | `1714089600000` | Mỗi lần cập nhật |

**Khi nào tạo:** Khi user đăng ký (giá trị mặc định: level=1, score=0)

**Khi nào cập nhật:** Sau mỗi trận chơi
- `totalScore` += điểm đạt được
- `totalXp` += XP nhận được
- `gamesPlayed` += 1
- `gamesWon` += 1 (nếu hoàn thành level)
- `level` = 1 + (totalXp / 100)

**Dùng cho:** Bảng xếp hạng (sắp xếp theo `totalScore` giảm dần)

**Đã xóa:** rank (tính tự động từ totalScore), winRate (tính từ gamesWon/gamesPlayed)

---

## 3️⃣ TABLE: gameModeProgress (Tiến độ 3 chế độ game) ⭐ QUAN TRỌNG

Lưu tiến độ của 3 chế độ game x 5 khối lớp = **15 records/user**.

| Trường | Kiểu | Mô tả | Ví dụ | Giải thích |
|--------|------|-------|-------|------------|
| **progressId** | VARCHAR(128) | ID tiến độ (khóa chính) | `abc123_chonda_1` | Format: `{uid}_{gameMode}_{grade}` |
| **uid** | VARCHAR(128) | ID người chơi | `8fmJ5oe0ghdbJkfwK8bp48irnX2` | Liên kết với users |
| **gameMode** | VARCHAR(50) | Chế độ game | `chonda` | `chonda`, `keothada`, `phithuyen` |
| **grade** | INT | Khối lớp (1-5) | `1` | Lớp 1, 2, 3, 4, 5 |
| **currentLevel** | INT | Level đang chơi | `10` | Level hiện tại |
| **maxLevelUnlocked** | INT | **Level đã mở khóa** | `11` | **Kiểm soát level nào được chơi** |
| **totalScore** | INT | Tổng điểm chế độ này | `9500` | Tổng điểm tất cả level đã chơi |
| **bestScore** | INT | Điểm cao nhất 1 level | `1000` | Điểm cao nhất trong 1 level bất kỳ |
| **lastPlayed** | BIGINT | Timestamp chơi gần nhất | `1714089600000` | NULL nếu chưa chơi |

### 3 Chế độ game:

| gameMode | Tên | Scene | Mô tả |
|----------|-----|-------|-------|
| `chonda` | Chọn đáp án | ChonDA.unity | Chọn đáp án đúng từ 4 lựa chọn |
| `keothada` | Kéo thả | KeoThaDA.unity | Kéo thả đáp án vào vị trí đúng |
| `phithuyen` | Phi thuyền | PhiThuyen.unity | Điều khiển phi thuyền trả lời câu hỏi |

**Mỗi chế độ có 5 khối lớp, mỗi khối có 100 level.**

**Khi nào tạo:** Khi user đăng ký → Tạo 15 records (3 chế độ x 5 lớp)

**Khi nào cập nhật:** Sau mỗi lần chơi level
- `currentLevel` = level vừa chơi
- `maxLevelUnlocked` = level + 1 (nếu hoàn thành)
- `totalScore` += điểm
- `bestScore` = max(bestScore, điểm level này)
- `lastPlayed` = timestamp hiện tại

**Dùng cho:**
- Kiểm tra level unlock (level > maxLevelUnlocked → KHÔNG CHO CHƠI)
- Hiển thị tiến độ từng chế độ
- Thống kê điểm theo chế độ

---

## 4️⃣ TABLE: levelProgress (Điểm từng level)

Lưu điểm cao nhất và số lần chơi từng level. **Chỉ lưu level đã chơi.**

| Trường | Kiểu | Mô tả | Ví dụ | Giải thích |
|--------|------|-------|-------|------------|
| **progressId** | VARCHAR(128) | ID (khóa chính) | `abc123_chonda_1_5` | Format: `{uid}_{gameMode}_{grade}_{levelNumber}` |
| **uid** | VARCHAR(128) | ID người chơi | `8fmJ5oe0ghdbJkfwK8bp48irnX2` | Liên kết với users |
| **gameMode** | VARCHAR(50) | Chế độ game | `chonda` | `chonda`, `keothada`, `phithuyen` |
| **grade** | INT | Khối lớp (1-5) | `1` | Lớp 1, 2, 3, 4, 5 |
| **levelNumber** | INT | Số level (1-100) | `5` | Level thứ 5 |
| **bestScore** | INT | **Điểm cao nhất** | `960` | Điểm cao nhất đạt được |
| **attempts** | INT | Số lần chơi | `3` | Đã chơi level này 3 lần |

**Khi nào tạo:** Lần đầu chơi level đó

**Khi nào cập nhật:** Mỗi lần chơi lại level
- `bestScore`: Nếu điểm mới > điểm cũ
- `attempts` += 1

**Dùng cho:**
- Hiển thị điểm từng level trên màn hình chọn level
- Thống kê số lần chơi
- Hiển thị level đã hoàn thành

**Lưu ý:** Chỉ lưu level đã chơi, không tạo record cho level chưa chơi.

---

## 🔄 Luồng dữ liệu chi tiết

### 1. Khi đăng ký tài khoản:

```
1. Tạo users
   uid: (Firebase Auth tự động)
   email: (từ form)
   characterName: (từ form)
   age: (từ form)
   createdAt: timestamp hiện tại
   lastLogin: timestamp hiện tại

2. Tạo playerData
   uid: (từ users)
   characterName: (từ users)
   totalScore: 0
   totalXp: 0
   level: 1
   gamesPlayed: 0
   gamesWon: 0
   lastUpdated: timestamp hiện tại

3. Tạo gameModeProgress (15 records)
   Chế độ chonda:
   - abc123_chonda_1: grade=1, maxLevelUnlocked=1
   - abc123_chonda_2: grade=2, maxLevelUnlocked=1
   - abc123_chonda_3: grade=3, maxLevelUnlocked=1
   - abc123_chonda_4: grade=4, maxLevelUnlocked=1
   - abc123_chonda_5: grade=5, maxLevelUnlocked=1
   
   Chế độ keothada:
   - abc123_keothada_1: grade=1, maxLevelUnlocked=1
   - abc123_keothada_2: grade=2, maxLevelUnlocked=1
   - abc123_keothada_3: grade=3, maxLevelUnlocked=1
   - abc123_keothada_4: grade=4, maxLevelUnlocked=1
   - abc123_keothada_5: grade=5, maxLevelUnlocked=1
   
   Chế độ phithuyen:
   - abc123_phithuyen_1: grade=1, maxLevelUnlocked=1
   - abc123_phithuyen_2: grade=2, maxLevelUnlocked=1
   - abc123_phithuyen_3: grade=3, maxLevelUnlocked=1
   - abc123_phithuyen_4: grade=4, maxLevelUnlocked=1
   - abc123_phithuyen_5: grade=5, maxLevelUnlocked=1

4. KHÔNG tạo levelProgress (tạo khi chơi)
```

---

### 2. Khi chơi level:

```
BẮT ĐẦU:
1. Load gameModeProgress (gameMode, grade)
2. Kiểm tra: level <= maxLevelUnlocked?
   - NẾU KHÔNG → Hiển thị "Level bị khóa"
   - NẾU CÓ → Cho phép chơi

CHƠI XONG:
3. Tính điểm (score) và XP

4. Cập nhật/Tạo levelProgress:
   - Tìm record (uid, gameMode, grade, levelNumber)
   - NẾU CHƯA CÓ → Tạo mới:
     * bestScore = score
     * attempts = 1
   - NẾU ĐÃ CÓ → Cập nhật:
     * bestScore = max(bestScore, score)
     * attempts += 1

5. Cập nhật gameModeProgress:
   - currentLevel = levelNumber
   - maxLevelUnlocked = levelNumber + 1 (nếu hoàn thành)
   - totalScore += score
   - bestScore = max(bestScore, score)
   - lastPlayed = timestamp hiện tại

6. Cập nhật playerData:
   - totalScore += score
   - totalXp += xp
   - level = 1 + (totalXp / 100)
   - gamesPlayed += 1
   - gamesWon += 1 (nếu hoàn thành)
   - lastUpdated = timestamp hiện tại
```

---

### 3. Khi hiển thị màn hình chọn level:

```
1. Load gameModeProgress (gameMode, grade)
   → Biết maxLevelUnlocked

2. Load levelProgress (gameMode, grade)
   → Biết điểm từng level đã chơi

3. Render UI:
   FOR level = 1 TO 100:
     NẾU level <= maxLevelUnlocked:
       - Hiển thị button (có thể chơi)
       - Hiển thị bestScore (nếu đã chơi)
     NẾU level > maxLevelUnlocked:
       - Hiển thị button khóa (màu xám)
       - Không cho click
```

---

### 4. Khi đăng nhập máy khác:

```
1. Firebase Auth → Lấy uid

2. Load users (uid)
   → Lấy characterName, age

3. Load playerData (uid)
   → Lấy totalScore, level, gamesPlayed

4. Load gameModeProgress (uid)
   → Lấy 15 records tiến độ

5. Load levelProgress (uid)
   → Lấy điểm từng level đã chơi

→ Tất cả dữ liệu quan trọng đã sync!
```

---

## 📊 Ví dụ cụ thể

### User: MeoNinja (uid: abc123)

#### users:
```
uid: abc123
email: meoninja@gmail.com
characterName: MeoNinja
age: 10
createdAt: 1714089600000
lastLogin: 1714089600000
```

#### playerData:
```
uid: abc123
characterName: MeoNinja
totalScore: 8750
totalXp: 1500
level: 15
gamesPlayed: 42
gamesWon: 28
lastUpdated: 1714089600000
```

#### gameModeProgress (15 records):
```
chonda, lớp 1: currentLevel=10, maxLevelUnlocked=11, totalScore=9500, bestScore=1000
chonda, lớp 2: currentLevel=1, maxLevelUnlocked=1, totalScore=0, bestScore=0
chonda, lớp 3-5: chưa chơi
keothada, lớp 1: currentLevel=5, maxLevelUnlocked=6, totalScore=4500, bestScore=950
keothada, lớp 2-5: chưa chơi
phithuyen, lớp 1-5: chưa chơi
```

#### levelProgress (chỉ level đã chơi):
```
chonda, lớp 1, level 1: bestScore=950, attempts=1
chonda, lớp 1, level 2: bestScore=920, attempts=2
chonda, lớp 1, level 3: bestScore=980, attempts=1
chonda, lớp 1, level 4: bestScore=850, attempts=3
chonda, lớp 1, level 5: bestScore=960, attempts=1
chonda, lớp 1, level 6: bestScore=940, attempts=2
chonda, lớp 1, level 7: bestScore=820, attempts=4
chonda, lớp 1, level 8: bestScore=970, attempts=1
chonda, lớp 1, level 9: bestScore=990, attempts=1
chonda, lớp 1, level 10: bestScore=1000, attempts=1

keothada, lớp 1, level 1-5: (5 records)
```

---

## ⚠️ Lưu ý quan trọng

### 1. Timestamp
Tất cả timestamp đều là **milliseconds** (BIGINT):
- **JavaScript:** `Date.now()`
- **C#:** `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`

### 2. UID
Luôn dùng Firebase Auth UID (128 ký tự)

### 3. Firestore
Đây là NoSQL, không có foreign keys thật:
- File SQL chỉ để document
- Relationships được quản lý trong code

### 4. Sync characterName
characterName phải sync giữa users và playerData

### 5. Không dùng hệ thống sao
Game chỉ tính điểm (score), không có sao (0-3 sao)

### 6. Level unlock
`maxLevelUnlocked` kiểm soát level nào được chơi:
- Level <= maxLevelUnlocked: Cho phép chơi
- Level > maxLevelUnlocked: Khóa

### 7. Chỉ lưu level đã chơi
`levelProgress` chỉ tạo record khi chơi level đó lần đầu

---

## 🔧 Firestore Rules

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    
    // users - chỉ user tự đọc/ghi
    match /users/{uid} {
      allow read, write: if request.auth != null && request.auth.uid == uid;
    }
    
    // playerData - public read (leaderboard), chỉ user tự ghi
    match /playerData/{uid} {
      allow read: if true;
      allow write: if request.auth != null && request.auth.uid == uid;
    }
    
    // gameModeProgress - chỉ user tự đọc/ghi
    match /gameModeProgress/{progressId} {
      allow read, write: if request.auth != null && 
                           resource.data.uid == request.auth.uid;
    }
    
    // levelProgress - chỉ user tự đọc/ghi
    match /levelProgress/{progressId} {
      allow read, write: if request.auth != null && 
                           resource.data.uid == request.auth.uid;
    }
  }
}
```

---

## 📁 Files liên quan

- `firestore_schema_optimized.sql` - Schema SQL (import vào DrawSQL)
- `DATABASE_TOI_UU.md` - Tóm tắt 4 tables
- `GIAI_THICH_DATABASE.md` - File này (giải thích chi tiết)

---

✅ **Database tối ưu, chỉ lưu dữ liệu quan trọng nhất! Không dùng hệ thống sao, chỉ tính điểm.**
