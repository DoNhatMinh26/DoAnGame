using UnityEngine;

/// <summary>
/// ScriptableObject chứa thông tin 1 avatar/nhân vật.
/// Đặt tất cả assets trong Assets/Resources/Avatars/ để AvatarManager tự load.
/// </summary>
[CreateAssetMenu(menuName = "Game/AvatarData", fileName = "AvatarData")]
public class AvatarData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public int avatarId;                                    // ID duy nhất (0, 1, 2, ...)
    public string avatarName;                               // Tên hiển thị (VD: "Mèo Trắng")
    public bool isDefault = false;                          // Avatar mặc định khi chưa chọn

    [Header("Sprites")]
    public Sprite thumbnail;                                // Ảnh nhỏ ~128px — dùng trong danh sách chọn
    public Sprite fullAvatar;                               // Ảnh lớn ~256px — dùng ở Profile & MainMenu

    [Header("Animation")]
    public RuntimeAnimatorController animatorController;    // Animator Controller cho nhân vật trong game
}
