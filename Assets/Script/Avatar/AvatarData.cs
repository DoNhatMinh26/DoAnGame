using UnityEngine;

/// <summary>
/// ScriptableObject chứa thông tin 1 avatar/nhân vật.
/// Đặt tất cả assets trong Assets/Resources/Avatars/ để AvatarManager tự load.
///
/// Cấu trúc nhân vật gồm 3 PSB riêng biệt, mỗi PSB có bộ xương riêng và controller riêng:
///   - Character Meo.psb      → controller riêng → Idle + Happy
///   - Character Meo_Sad.psb  → controller riêng → Sad
///   - MeoGoc34 Fix.psb       → controller riêng → Attack (góc 3/4)
///
/// Bên trong mỗi PSB có 4 skin (mascost1–4 / Meo1–4) dùng chung xương của PSB đó.
/// avatarId xác định skin nào được bật (0→skin1, 1→skin2, 2→skin3, 3→skin4).
///
/// Controller đã gán sẵn trong từng PSB — không cần swap, không lưu ở đây.
/// AvatarData chỉ lưu thông tin UI (thumbnail, fullAvatar) và id/tên.
/// </summary>
[CreateAssetMenu(menuName = "Game/AvatarData", fileName = "AvatarData")]
public class AvatarData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public int avatarId;        // ID duy nhất (0, 1, 2, 3)
    public string avatarName;   // Tên hiển thị (VD: "Mèo Trắng")
    public bool isDefault;      // Avatar mặc định khi chưa chọn

    [Header("Sprites — UI only")]
    public Sprite thumbnail;    // Ảnh nhỏ ~128px — dùng trong danh sách chọn
    public Sprite fullAvatar;   // Ảnh lớn ~256px — dùng ở Profile & MainMenu

    // animatorController KHÔNG có ở đây.
    // Lý do: 3 PSB có 3 bộ xương khác nhau → mỗi PSB phải có controller riêng
    // và controller đó đã được gán sẵn trong PSB, không cần swap từ code.
}
