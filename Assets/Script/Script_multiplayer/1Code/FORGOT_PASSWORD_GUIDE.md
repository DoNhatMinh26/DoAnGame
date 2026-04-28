# 📧 Forgot Password - Hướng Dẫn Đầy Đủ

## 📋 Tổng Quan
Panel **Quên Mật Khẩu** cho phép người dùng reset mật khẩu qua email bằng Firebase Authentication.

### Tính Năng
- ✅ Nhập email để nhận link reset password
- ✅ Validate email format
- ✅ **Kiểm tra email có tồn tại trong Firestore collection 'users'**
- ✅ **Báo lỗi nếu email chưa đăng ký**
- ✅ Gửi email qua Firebase Auth (chỉ cho email đã đăng ký)
- ✅ Hiển thị status (xanh) và error (đỏ)
- ✅ Auto-hide error sau 3 giây
- ✅ Nút "Quay lại" về LoginPanel (dùng UIButtonScreenNavigator)
- ✅ Disable button khi đang gửi email (tránh spam)
- ✅ Anti-spam cooldown 60 giây (chỉ áp dụng khi gửi thành công)

### Flow Hoạt Động
```
Game: LoginPanel
  ↓ Click "Quên Mật Khẩu?"
Game: ForgotPasswordPanel
  ↓ Nhập email → Click "Gửi Email"
  ↓ Firebase gửi email
Email: User nhận email → Click link
  ↓ Mở browser
Firebase Web: User nhập mật khẩu mới
  ↓ Submit
Game: LoginPanel → Đăng nhập với mật khẩu mới ✅
```

---

## 🛠️ Setup Trong Unity (5 Phút)

### Bước 1: Tạo ForgotPasswordPanel
```
GameUICanvas (Right-click)
  → UI → Panel
  → Rename: "ForgotPasswordPanel"
  → Set Active: INACTIVE ❌
  → Anchor: Stretch (full screen)
  → Color: #000000AA (đen trong suốt)
```

### Bước 2: Tạo UI Elements
Trong `ForgotPasswordPanel`, tạo các elements sau:

| Element | Type | Name | Text/Placeholder | Color | Size | Active |
|---|---|---|---|---|---|---|
| Content | Panel | `ContentPanel` | - | White | 600x400 | ✅ |
| Title | TextMeshPro | `TitleText` | "Quên Mật Khẩu" | Black, 36pt | - | ✅ |
| Input | InputField TMP | `EmailInputField` | "Nhập email của bạn" | 24pt | 500x60 | ✅ |
| Button | Button TMP | `GuiEmailBtn` | "Gửi Email" | #4CAF50 (green) | 250x60 | ✅ |
| Button | Button TMP | `BackBtn` | "Quay Lại" | #9E9E9E (gray) | 250x60 | ✅ |
| Status | TextMeshPro | `StatusText` | "" | #4CAF50 (green), 20pt | 500x40 | ❌ |
| Error | TextMeshPro | `ErrorText` | "" | #F44336 (red), 20pt | 500x40 | ❌ |

**Layout**:
```
ForgotPasswordPanel (full screen, black overlay)
└── ContentPanel (600x400, center)
    ├── TitleText (top)
    ├── EmailInputField (center)
    ├── GuiEmailBtn (below input)
    ├── BackBtn (below send button)
    ├── StatusText (bottom, inactive)
    └── ErrorText (bottom, inactive)
```

### Bước 3: Gắn Script
1. Select `ForgotPasswordPanel`
2. Add Component → `UIForgotPasswordController`
3. Assign references:

| Field | Drag GameObject |
|---|---|
| Email Input Field | `EmailInputField` |
| Send Button | `GuiEmailBtn` |
| Status Text | `StatusText` |
| Error Text | `ErrorText` |
| Back Panel Name | Type: `"LoginPanel"` |

### Bước 4: Setup BackBtn Navigator
1. Select `BackBtn`
2. Add Component → `UIButtonScreenNavigator`
3. Assign:

| Field | Value |
|---|---|
| Screens Root | Drag `GameUICanvas` |
| Target Screen | Drag `LoginPanel` |

### Bước 5: Kết Nối LoginPanel
1. Select `LoginPanel`
2. Inspector → `UILoginPanelController`
3. Find field: **Forgot Password Button**
4. Drag `QuenMatKhauBtn` vào field

✅ **DONE!** Script đã tự động xử lý navigation.

---

## 🧪 Test

### Test 1: Email Validation
- Để trống → Error: "Vui lòng nhập email!"
- Email sai format → Error: "Email không hợp lệ!"

### Test 2: Email Not Registered
- Nhập email chưa đăng ký (vd: `giangvo5322@gmail.com`)
- Click "Gửi Email"
- ✅ Expect: Error "❌ Email chưa đăng ký! Vui lòng đăng ký tài khoản trước."

### Test 3: Send Email Success
- Nhập email **ĐÃ ĐĂNG KÝ** (vd: `giangvo532@gmail.com`)
- Check trong Firebase Console → Firestore Database → Collection `users` → Tìm document có field `email` = email của bạn
- Click "Gửi Email"
- ✅ Expect: 
  - Status: "Đang gửi email..."
  - Status: "✅ Đã gửi email!"
  - Tự động quay về LoginPanel sau 2 giây
  - Check email inbox → Nhận được email từ Firebase

### Test 4: Anti-Spam Cooldown
- Gửi email thành công 1 lần
- Thử gửi lại ngay lập tức
- ✅ Expect: Error "❌ Vui lòng đợi 60 giây trước khi gửi lại!"

### Test 5: Back Button
- Click "Quay Lại" → Về LoginPanel

---

## 🐛 Troubleshooting

| Lỗi | Giải Pháp |
|---|---|
| "Cannot find ForgotPasswordPanel!" | Kiểm tra tên panel = `ForgotPasswordPanel` |
| Button không hoạt động | Kiểm tra references trong Inspector |
| "Email chưa đăng ký!" | Email chưa có trong Firestore collection `users` → Phải đăng ký trước |
| Không nhận được email | 1. Check spam folder<br>2. Đợi 5-10 phút<br>3. Check Firebase Console → Firestore → Collection `users` |
| BackBtn không navigate | Kiểm tra UIButtonScreenNavigator đã setup |
| "Không thể kiểm tra email!" | Lỗi kết nối Firestore → Kiểm tra internet hoặc Firebase config |

### 📧 Về Email Reset Password

**Tại sao không nhận được email?**

1. **Email chưa đăng ký**: Phải có trong Firestore collection `users` (field `email`)
2. **Email trong spam**: Check thư mục spam/junk
3. **Đợi vài phút**: Email có thể mất 5-10 phút mới đến
4. **Firebase chưa setup**: Check Firebase Console → Authentication → Sign-in method → Email/Password phải enabled

**Kiểm tra email đã đăng ký chưa:**
1. Firebase Console → Firestore Database → Collection `users`
2. Tìm document có field `email` = email của bạn
3. Nếu không có → Phải đăng ký trước (hoặc check Authentication → Users)

---

## 📁 Files
- **Script**: `Assets/Script/Script_multiplayer/1Code/CODE/UIForgotPasswordController.cs`
- **Scene**: `Assets/Scenes/GameUIPlay 1.unity`
- **Guide**: `Assets/Script/Script_multiplayer/1Code/FORGOT_PASSWORD_GUIDE.md` (file này)

---

## 🎨 Customize Email Template (Optional)
1. Firebase Console → Authentication → Templates
2. Select "Password reset"
3. Customize subject, body, sender name

---

**✅ Hoàn thành!** Panel Quên Mật Khẩu đã sẵn sàng.
