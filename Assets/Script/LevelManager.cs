using UnityEngine;
using UnityEngine.SceneManagement; // Thêm thư viện này để chuyển Scene

public class LevelManager : MonoBehaviour
{
    [Header("UI Màn Chơi")]
    public GameObject panelChonMan;
    public GameObject panelGameplay;

    public static int CurrentLevel = 1;

    void Start()
    {
        panelChonMan.SetActive(true);
        panelGameplay.SetActive(false);
    }

    public void BatDauChoiMan(int levelIndex)
    {
        CurrentLevel = levelIndex;
        panelChonMan.SetActive(false);
        panelGameplay.SetActive(true);

        MathManager math = FindObjectOfType<MathManager>();
        if (math != null) math.UpdateDifficultyFromJSON();

        DragQuizManager drag = FindObjectOfType<DragQuizManager>();
        if (drag != null) drag.UpdateDifficultyFromJSON();
    }

    public void QuayLaiChonMan()
    {
        panelChonMan.SetActive(true);
        panelGameplay.SetActive(false);
    }

    // PHƯƠNG THỨC MỚI: Quay về Scene Menu và mở bảng Chọn cách chơi
    public void QuayVeMenuChonCachChoi()
    {
        MenuManager.QuayLaiTuGame = true; // Đánh dấu trạng thái quay về
        SceneManager.LoadScene("Menu"); // Đảm bảo tên Scene Menu của bạn chính xác là "Menu"
    }
}