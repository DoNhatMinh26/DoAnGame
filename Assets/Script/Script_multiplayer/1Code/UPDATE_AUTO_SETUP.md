# ✅ Cập nhật: Tự động setup FirebaseManager

## 🎯 Thay đổi

### 1. **enablePlayerDataSync = true** (Mặc định)
- Trước: `enablePlayerDataSync = false` → Phải tick thủ công
- Sau: `enablePlayerDataSync = true` → **Tự động BẬT**

### 2. **Tự động tạo FirebaseManager**
- Trước: Phải thêm FirebaseManager component thủ công
- Sau: **Tự động tạo** nếu chưa có

---

## 🔧 Cách hoạt động

### Khi game chạy:

```
AuthManager.Start()
  ↓
Tìm FirebaseManager
  ↓
NẾU CHƯA CÓ:
  ├─ Tìm GameObject "AuthServices"
  │  ├─ NẾU CÓ → Thêm FirebaseManager vào AuthServices
  │  └─ NẾU KHÔNG → Tạo GameObject "FirebaseManager" mới
  ↓
FirebaseManager.enablePlayerDataSync = true (mặc định)
  ↓
Khi đăng ký → Tự động tạo:
  ├─ users (1 doc)
  ├─ playerData (1 doc)
  └─ gameModeProgress (15 docs)
```

---

## ✅ Lợi ích

### 1. Không cần setup thủ công
- ❌ Trước: Phải tạo GameObject → Add Component → Tick checkbox
- ✅ Sau: Chỉ cần Play game → Tự động setup

### 2. Tự động gắn vào AuthServices
- Nếu có GameObject "AuthServices" → Gắn vào đó (gọn gàng)
- Nếu không có → Tạo GameObject mới

### 3. Luôn tạo đầy đủ database
- `enablePlayerDataSync = true` mặc định
- Đăng ký → Tự động tạo 17 records (1 users + 1 playerData + 15 gameModeProgress)

---

## 🧪 Test

### 1. Xóa FirebaseManager cũ (nếu có)
Trong Unity Editor:
- Hierarchy → Tìm GameObject có FirebaseManager component
- Xóa component hoặc xóa GameObject

### 2. Play game
Console sẽ log:
```
[Auth] 🔧 Tự động tạo FirebaseManager...
[Auth] ✅ Đã thêm FirebaseManager vào AuthServices
[Firebase] 🔄 Đang khởi tạo...
[Firebase] ✅ Khởi tạo thành công!
```

### 3. Đăng ký tài khoản
Console sẽ log:
```
[Auth] 📝 Đăng ký: [character name]
[Firebase] ✅ Lưu user data thành công
[Firebase] 🔄 Đang tạo 15 gameModeProgress records...
[Firebase] ✅ Đã tạo 15 gameModeProgress records thành công!
[Firebase] ✅ Lưu player data thành công
```

### 4. Kiểm tra Firestore
Phải có 3 collections:
- ✅ `users` (1 doc)
- ✅ `playerData` (1 doc)
- ✅ `gameModeProgress` (15 docs)

---

## 🎨 Cấu trúc Hierarchy sau khi auto-setup

### Nếu có AuthServices:
```
AuthServices
   • UserValidationService
   • SessionManager
   • PlayerDataService
   • UILoadingIndicator
   • FirebaseManager  ← Tự động thêm vào đây
     └─ Enable Player Data Sync: ✅ true (mặc định)
```

### Nếu không có AuthServices:
```
FirebaseManager  ← Tự động tạo GameObject mới
   • FirebaseManager
     └─ Enable Player Data Sync: ✅ true (mặc định)
```

---

## ⚙️ Tùy chỉnh (Nếu cần)

### Tắt auto-create database (không khuyến nghị)
Nếu muốn tắt tính năng tự động tạo database:

1. Hierarchy → Chọn GameObject có FirebaseManager
2. Inspector → FirebaseManager component
3. ❌ Bỏ tick **"Enable Player Data Sync"**

**Lưu ý:** Nếu tắt, khi đăng ký chỉ tạo `users`, không tạo `playerData` và `gameModeProgress`!

---

## 📝 Files đã cập nhật

1. ✅ `FirebaseManager.cs` - Đổi `enablePlayerDataSync = true` mặc định
2. ✅ `AuthManager.cs` - Thêm logic tự động tạo FirebaseManager
3. ✅ `UPDATE_AUTO_SETUP.md` - File này

---

## 🚀 Kết luận

**Không cần setup thủ công nữa!**

Chỉ cần:
1. Xóa users cũ trong Firebase (Authentication + Firestore)
2. Xóa PlayerPrefs (Tools → Clear All PlayerPrefs)
3. Restart Unity
4. Play game → Đăng ký → **Tự động tạo đầy đủ database!** ✅

---

✅ **Setup tự động, không cần can thiệp thủ công!**
