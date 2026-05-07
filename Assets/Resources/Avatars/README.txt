Đặt tất cả AvatarData ScriptableObject assets vào thư mục này.
AvatarManager sẽ tự load bằng Resources.LoadAll<AvatarData>("Avatars").

Cách tạo:
1. Right-click trong thư mục này → Create → Game → AvatarData
2. Điền avatarId (0, 1, 2...), avatarName, gán thumbnail, fullAvatar, animatorController
3. Set isDefault = true cho avatar đầu tiên (avatarId = 0)
