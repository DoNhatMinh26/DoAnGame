using UnityEngine;
using TMPro;

public class ProfileUI : MonoBehaviour
{
    [Header("UI Thông tin người chơi")]
    public TextMeshProUGUI levelUserTxt;
    public TextMeshProUGUI diemUserTxt;
    public TextMeshProUGUI totalCoinTxt;
    public TextMeshProUGUI currentGradeTxt;

    [Header("UI Tiến độ các chế độ")]
    public TextMeshProUGUI lopHocLevelTxt;
    public TextMeshProUGUI phongThuLevelTxt;
    public TextMeshProUGUI phiThuyenLevelTxt;

    // Hàm này tự chạy mỗi khi Profile được bật (SetActive = true)
    private void OnEnable()
    {
        UpdateProfileDisplay();
    }

    public void UpdateProfileDisplay()
    {
        // Đọc dữ liệu từ PlayerPrefs và hiện lên UI
        if (levelUserTxt != null) levelUserTxt.text = PlayerPrefs.GetInt("UserLevel", 1).ToString();
        if (diemUserTxt != null) diemUserTxt.text = PlayerPrefs.GetInt("UserScore", 0).ToString();
        if (totalCoinTxt != null) totalCoinTxt.text = PlayerPrefs.GetInt("TotalCoins", 0).ToString();

        // Hiển thị lớp học hiện tại (Dùng UIManager cũ của bạn)
        if (currentGradeTxt != null)
            currentGradeTxt.text = UIManager.SelectedGrade.ToString();

        // Cập nhật các tiến độ cao nhất
        if (lopHocLevelTxt != null) lopHocLevelTxt.text = PlayerPrefs.GetInt("Class_HighestLevel", 1).ToString();
        if (phongThuLevelTxt != null) phongThuLevelTxt.text = PlayerPrefs.GetInt("HighestLevelReached", 1).ToString();
        if (phiThuyenLevelTxt != null) phiThuyenLevelTxt.text = PlayerPrefs.GetInt("Space_HighestLevel", 1).ToString();
    }
}