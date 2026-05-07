using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script gán trên mỗi item trong danh sách chọn avatar (AvatarItem prefab).
/// Hiển thị thumbnail, tên, và viền "đang chọn".
/// </summary>
public class AvatarItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectedIndicator;  // Viền/checkmark khi đang chọn
    [SerializeField] private Button button;

    private AvatarData data;
    private System.Action<int> onSelected;

    /// <summary>
    /// Khởi tạo item với dữ liệu avatar và callback khi chọn.
    /// </summary>
    public void Setup(AvatarData avatarData, bool isSelected, System.Action<int> onSelectCallback)
    {
        data = avatarData;
        onSelected = onSelectCallback;

        if (thumbnailImage != null)
            thumbnailImage.sprite = avatarData.thumbnail;

        if (nameText != null)
            nameText.SetText(avatarData.avatarName);

        SetSelected(isSelected);

        if (button == null)
            button = GetComponent<Button>();

        button?.onClick.RemoveAllListeners();
        button?.onClick.AddListener(() => onSelected?.Invoke(data.avatarId));
    }

    /// <summary>
    /// Cập nhật trạng thái "đang chọn" (hiện/ẩn selectedIndicator).
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);
    }

    public int GetAvatarId() => data?.avatarId ?? -1;
}
