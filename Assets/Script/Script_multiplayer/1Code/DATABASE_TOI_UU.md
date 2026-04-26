# 🎯 Database Tối Ưu - Chỉ Lưu Dữ Liệu Quan Trọng

## ✅ CHỈ 4 TABLES (Giảm từ 6 → 4)

### 1️⃣ **users** (6 fields)
Thông tin tài khoản cơ bản.

| Field | Mô tả | Ví dụ |
|-------|-------|-------|
| uid | ID Firebase Auth | `abc123xyz` |
| email | Email đăng nhập | `player@gmail.com` |
| characterName | Tên nhân vật | `MeoNinja` |
| age | Tuổi | `10` |
| createdAt | Timestamp tạo | `1714089600000` |
| lastLogin | Timestamp login | `1714089600000` |

**Đã xóa:** avatar, emailVerified, isActive (không quan trọng)

---

### 2️⃣ **playerData** (7 fields)
Thống kê tổng quan - Dùng cho **bảng xếp hạng**.

| Field | Mô tả | Ví dụ |
|-------|-------|-------|
| uid | ID người chơi | `abc123xyz` |
| characterName | Tên nhân vật | `MeoNinja` |
| **totalScore** | **Tổng điểm** (xếp hạng) | `8750` |
| totalXp | Tổng XP | `1500` |
| level | Level hiện tại | `15` |
| gamesPlayed | Số trận đã chơi | `42` |
| gamesWon | Số trận thắng | `28` |
| lastUpdated | Timestamp cập nhật | `1714089600000` |

**Đã xóa:** rank (tính từ totalScore), winRate (tính từ gamesWon/gamesPlayed)

---

### 3️⃣ **gameModeProgress** (8 fields) ⭐ MỚI
Tiến độ 3 chế độ game x 5 khối lớp = **15 records/user**.

| Field | Mô tả | Ví dụ |
|-------|-------|-------|
| progressId | ID tiến độ | `abc123_chonda_1` |
| uid | ID người chơi | `abc123xyz` |
| **gameMode** | **Chế độ game** | `chonda`, `keothada`, `phithuyen` |
| **grade** | **Khối lớp** | `1`, `2`, `3`, `4`, `5` |
| currentLevel | Level đang chơi | `10` |
| **maxLevelUnlocked** | **Level đã mở khóa** | `11` |
| totalScore | Tổng điểm chế độ này | `9500` |
| bestScore | Điểm cao nhất 1 level | `1000` |
| lastPlayed | Timestamp chơi | `1714089600000` |

**3 chế độ game:**
- `chonda` - Chọn đáp án (Scene: ChonDA.unity)
- `keothada` - Kéo thả (Scene: KeoThaDA.unity)
- `phithuyen` - Phi thuyền (Scene: PhiThuyen.unity)

**Mỗi chế độ có 5 khối lớp, mỗi khối có 100 level.**

---

### 4️⃣ **levelProgress** (7 fields)
Điểm từng level - Hiển thị màn hình chọn level.

| Field | Mô tả | Ví dụ |
|-------|-------|-------|
| progressId | ID | `abc123_chonda_1_5` |
| uid | ID người chơi | `abc123xyz` |
| gameMode | Chế độ game | `chonda` |
| grade | Khối lớp | `1` |
| levelNumber | Số level | `5` |
| **bestScore** | **Điểm cao nhất** | `960` |
| attempts | Số lần chơi | `3` |

**Chỉ lưu level đã chơi** (không lưu level chưa chơi).

---

## 🔄 Luồng dữ liệu

### Khi đăng ký:
```
1. Tạo users (1 record)
2. Tạo playerData (1 record)
3. Tạo gameModeProgress (15 records)
   - chonda: lớp 1-5 (5 records)
   - keothada: lớp 1-5 (5 records)
   - phithuyen: lớp 1-5 (5 records)
4. KHÔNG tạo levelProgress (tạo khi chơi)
```

### Khi chơi level:
```
1. Kiểm tra gameModeProgress
   → Nếu level > maxLevelUnlocked → KHÔNG CHO CHƠI
   
2. Chơi xong → Tính điểm

3. Cập nhật/Tạo levelProgress
   → Lưu bestScore (nếu điểm mới > điểm cũ)
   → attempts += 1

4. Cập nhật gameModeProgress
   → currentLevel = level vừa chơi
   → maxLevelUnlocked = level + 1 (nếu hoàn thành)
   → totalScore += điểm
   → bestScore = max(bestScore, điểm level này)

5. Cập nhật playerData
   → totalScore += điểm
   → totalXp += xp
   → gamesPlayed += 1
   → gamesWon += 1 (nếu hoàn thành)
```

### Khi hiển thị màn hình chọn level:
```
1. Load gameModeProgress (gameMode, grade)
   → Biết maxLevelUnlocked

2. Load levelProgress (gameMode, grade)
   → Hiển thị điểm từng level

3. Render UI:
   - Level 1 → maxLevelUnlocked: Hiển thị + điểm
   - Level > maxLevelUnlocked: Khóa (màu xám)
```

---

## 🎮 Ví dụ cụ thể

### User: MeoNinja

**playerData:**
```
totalScore: 8750
level: 15
gamesPlayed: 42
gamesWon: 28
```

**gameModeProgress:**
```
chonda, lớp 1: currentLevel=10, maxLevelUnlocked=11, totalScore=9500
chonda, lớp 2: currentLevel=1, maxLevelUnlocked=1, totalScore=0
keothada, lớp 1: currentLevel=5, maxLevelUnlocked=6, totalScore=4500
phithuyen, lớp 1: currentLevel=1, maxLevelUnlocked=1, totalScore=0
... (11 records nữa)
```

**levelProgress:**
```
chonda, lớp 1, level 1: 950 điểm, 1 lần chơi
chonda, lớp 1, level 2: 920 điểm, 2 lần chơi
chonda, lớp 1, level 3: 980 điểm, 1 lần chơi
chonda, lớp 1, level 4: 850 điểm, 3 lần chơi
chonda, lớp 1, level 5: 960 điểm, 1 lần chơi
... (chỉ lưu level đã chơi)
```

---

## 📊 So sánh

| Trước | Sau |
|-------|-----|
| 6 tables | **4 tables** ✅ |
| gameHistory (lưu mọi trận) | **Xóa** (không cần) |
| gameSessions (multiplayer) | **Xóa** (tạm thời) |
| achievements | **Xóa** (tính tự động) |
| Hệ thống sao (0-3 sao) | **Xóa** (chỉ tính điểm) |

---

## 💾 Dung lượng ước tính

**1 user:**
- users: 1 record
- playerData: 1 record
- gameModeProgress: 15 records (3 chế độ x 5 lớp)
- levelProgress: ~50-500 records (tùy tiến độ)

**Tổng:** ~70-520 records/user

**1000 users:** ~70,000-520,000 records

**Firestore free tier:** 50,000 reads/day → Đủ dùng! ✅

---

## ✅ Lợi ích

1. **Sync cross-device hoàn hảo**
   - Đăng nhập máy khác → Load hết tiến độ
   - Không mất dữ liệu

2. **Tối ưu dung lượng**
   - Chỉ lưu dữ liệu quan trọng
   - Không lưu lịch sử chi tiết
   - Không dùng hệ thống sao (đơn giản hơn)

3. **Query nhanh**
   - Ít tables → Ít joins
   - Indexes tối ưu

4. **Dễ maintain**
   - Cấu trúc đơn giản
   - Dễ hiểu, dễ debug

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
    
    // playerData - public read (leaderboard)
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

## 📁 Files

- `firestore_schema_optimized.sql` - Schema tối ưu (import vào DrawSQL)
- `DATABASE_TOI_UU.md` - File này
- `GIAI_THICH_DATABASE.md` - Giải thích chi tiết từng trường

---

✅ **Database tối ưu, chỉ lưu dữ liệu quan trọng nhất! Không dùng hệ thống sao, chỉ tính điểm.**
