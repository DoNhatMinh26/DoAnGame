using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels (Cùng 1 Scene)")]
    public GameObject panelChonLop;
    public GameObject panelMainMenu;
    public GameObject panelChonCachChoi;

    // Biến static để lưu lựa chọn xuyên Scene
    public static int SelectedGrade = 1;
    public static string SelectedMode = "";

    // BIẾN QUAN TRỌNG: Đánh dấu để khi quay lại Menu sẽ mở thẳng bảng Chọn Cách Chơi
    public static bool QuayLaiTuGame = false;

    void Start()
    {
        // Kiểm tra xem có phải vừa quay lại từ Scene Gameplay không
        if (QuayLaiTuGame)
        {
            SwitchPanel(panelChonCachChoi);
            QuayLaiTuGame = false; // Reset lại trạng thái
        }
        else
        {
            // Khởi đầu bình thường thì hiện bảng chọn lớp
            SwitchPanel(panelChonLop);
        }
    }

    public void ChonLop(int grade)
    {
        SelectedGrade = grade;
        Debug.Log("Lớp đã chọn: " + SelectedGrade);
        SwitchPanel(panelMainMenu);
    }

    public void BamPlay()
    {
        SwitchPanel(panelChonCachChoi);
    }

    public void ChonCachChoi(string sceneName)
    {
        SelectedMode = sceneName;
        Debug.Log("Mode đã chọn: " + SelectedMode);
        SceneManager.LoadScene(sceneName);
    }

    public void QuayLaiChonLop() => SwitchPanel(panelChonLop);
    public void QuayLaiMenuChinh() => SwitchPanel(panelMainMenu);

    private void SwitchPanel(GameObject panelToShow)
    {
        if (panelChonLop != null) panelChonLop.SetActive(false);
        if (panelMainMenu != null) panelMainMenu.SetActive(false);
        if (panelChonCachChoi != null) panelChonCachChoi.SetActive(false);

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
        }
    }

    public void ThoatGame()
    {
        Application.Quit();
    }
}