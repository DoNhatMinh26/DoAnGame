using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Giao diện chính")]
    [SerializeField] private GameObject chonMan;

    [Header("Danh sách các màn chơi")]
    // Bạn chỉ cần kéo tất cả Canvas màn chơi vào danh sách này trong Inspector
    public GameObject[] levelCanvases;

    private void Start()
    {
        // Khi bắt đầu, quay về Menu chính
        QuayLaiChonMan();
    }

    /// <summary>
    /// Hàm mở một màn chơi theo số thứ tự (Index)
    /// </summary>
    /// <param name="levelIndex">Số thứ tự màn chơi (bắt đầu từ 0)</param>
    public void OpenLevel(int levelIndex)
    {
        // 1. Ẩn menu chính
        chonMan.SetActive(false);

        // 2. Duyệt qua mảng để bật màn được chọn và tắt các màn còn lại
        for (int i = 0; i < levelCanvases.Length; i++)
        {
            // Nếu i bằng index truyền vào thì bật, ngược lại thì tắt
            levelCanvases[i].SetActive(i == levelIndex);
        }
    }

    /// <summary>
    /// Hàm quay lại Menu chính
    /// </summary>
    public void QuayLaiChonMan()
    {
        chonMan.SetActive(true);

        // Tắt tất cả các màn chơi đang có trong danh sách
        foreach (GameObject level in levelCanvases)
        {
            level.SetActive(false);
        }
    }
}