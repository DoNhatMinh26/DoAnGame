# 🔐 Firestore Security Rules - Hướng Dẫn Setup

## 📋 Tổng Quan

File này chứa **Firestore Security Rules** cho toàn bộ dự án DoAnGame. Rules này đảm bảo:

✅ **Forgot Password** hoạt động (đọc email từ `users`)  
✅ **Leaderboard** hoạt động (đọc `playerData`)  
✅ **Character Name Validation** hoạt động (đọc `users`)  
✅ **Bảo mật**: User chỉ có thể sửa data của chính mình  

---

## 🚀 Cách Setup (5 Phút)

### Bước 1: Mở Firebase Console
1. Vào https://console.firebase.google.com
2. Chọn project của bạn
3. Sidebar → **Firestore Database**
4. Tab → **Rules**

### Bước 2: Copy Rules
1. Mở file `firestore.rules` trong dự án Unity này
2. Copy **TOÀN BỘ** nội dung
3. Paste vào Firebase Console (thay thế rules cũ)

### Bước 3: Publish
1. Click **Publish** ở góc trên bên phải
2. Đợi vài giây để rules được áp dụng
3. ✅ **DONE!**

---

## 📊 Collections & Permissions

| Collection | Read | Write | Mục Đích |
|---|---|---|---|
| `users` | ✅ Tất cả | 🔒 Chỉ chính user | Email, characterName, age, avatar |
| `playerData` | ✅ Tất cả | 🔒 Chỉ chính user | **Leaderboard**: level, totalScore, rank, winRate |
| `gameModeProgress` | 🔒 Đã đăng nhập | 🔒 Chỉ chính user | Tiến độ chế độ game (currentLevel, maxLevelUnlocked) |
| `levelProgress` | 🔒 Đã đăng nhập | 🔒 Chỉ chính user | Điểm từng level (bestScore, attempts) |

---

## 🔍 Giải Thích Chi Tiết

### 1. Collection `users`
```javascript
allow read: if true;  // ✅ Cho phép TẤT CẢ đọc
```

**Tại sao?**
- **Forgot Password**: Cần kiểm tra email có tồn tại không (không cần đăng nhập)
- **Character Name Validation**: Cần kiểm tra tên nhân vật có trùng không

**An toàn không?**
- ✅ **AN TOÀN**: Chỉ lưu thông tin công khai (email, characterName, age)
- ❌ **KHÔNG LƯU**: Password (lưu ở Firebase Auth), token, thông tin nhạy cảm

---

### 2. Collection `playerData`
```javascript
allow read: if true;  // ✅ Cho phép TẤT CẢ đọc
```

**Tại sao?**
- **Leaderboard**: Cần đọc tất cả player data để hiển thị bảng xếp hạng
- Không cần đăng nhập để xem leaderboard (guest mode cũng xem được)

**An toàn không?**
- ✅ **AN TOÀN**: Chỉ lưu stats game (level, totalScore, rank, winRate)
- Không có thông tin cá nhân nhạy cảm

---

### 3. Collection `gameModeProgress` & `levelProgress`
```javascript
allow read: if request.auth != null;  // 🔒 Chỉ user đã đăng nhập
```

**Tại sao?**
- Tiến độ game là dữ liệu riêng tư
- Chỉ user đã đăng nhập mới cần xem/sửa

---

## 🧪 Test Rules

### Test 1: Forgot Password (Không Đăng Nhập)
```
✅ PASS: Đọc collection `users` để kiểm tra email
✅ PASS: Hiển thị "Email chưa đăng ký" nếu không tìm thấy
```

### Test 2: Leaderboard (Không Đăng Nhập)
```
✅ PASS: Đọc collection `playerData` để hiển thị top players
✅ PASS: Sắp xếp theo totalScore giảm dần
```

### Test 3: Character Name Validation (Đăng Ký)
```
✅ PASS: Đọc collection `users` để kiểm tra tên trùng
✅ PASS: Hiển thị "Tên nhân vật đã tồn tại" nếu trùng
```

### Test 4: Update Player Data (Đã Đăng Nhập)
```
✅ PASS: User A có thể sửa playerData của chính mình
❌ FAIL: User A KHÔNG thể sửa playerData của User B
```

---

## ⚠️ Lưu Ý Bảo Mật

### ✅ Làm Đúng
- Chỉ lưu thông tin công khai trong `users` và `playerData`
- Password luôn lưu ở **Firebase Authentication** (không lưu trong Firestore)
- Token/session luôn lưu ở **PlayerPrefs** (local)

### ❌ KHÔNG Làm
- ❌ Lưu password trong Firestore
- ❌ Lưu token/API key trong Firestore
- ❌ Lưu thông tin thanh toán trong Firestore
- ❌ Cho phép `allow write: if true` (ai cũng sửa được)

---

## 🐛 Troubleshooting

| Lỗi | Nguyên Nhân | Giải Pháp |
|---|---|---|
| "Missing or insufficient permissions" | Rules chưa publish | Publish rules trong Firebase Console |
| Leaderboard không load | `playerData` không có quyền read | Kiểm tra rule: `allow read: if true` |
| Forgot password không hoạt động | `users` không có quyền read | Kiểm tra rule: `allow read: if true` |
| User A sửa được data của User B | Rules sai | Kiểm tra rule: `request.auth.uid == userId` |

---

## 📁 Files Liên Quan

- **Rules file**: `firestore.rules` (file này)
- **Setup guide**: `FIRESTORE_RULES_SETUP.md` (file này)
- **Forgot Password**: `Assets/Script/Script_multiplayer/1Code/CODE/UIForgotPasswordController.cs`
- **Leaderboard**: `Assets/Script/Script_multiplayer/1Code/CODE/UILeaderboardPanelController.cs`
- **Firebase Manager**: `Assets/Script/Script_multiplayer/FirebaseManager.cs`

---

## 🔄 Cập Nhật Rules

Nếu thêm collection mới trong tương lai:

1. Mở file `firestore.rules`
2. Thêm rule mới theo format:
```javascript
match /newCollection/{docId} {
  allow read: if <điều kiện>;
  allow write: if <điều kiện>;
}
```
3. Publish lại trong Firebase Console

---

**✅ Hoàn thành!** Rules đã được cấu hình đầy đủ cho toàn bộ dự án.
