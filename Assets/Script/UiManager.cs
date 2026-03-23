using UnityEngine;

public class LevelManager : MonoBehaviour
{
   
    [Header("Các Canvas")]
    public GameObject mainMenuCanvas; // Canvas Menu chính
    public GameObject M1Canvas;
    public GameObject M2Canvas;
    public GameObject M3Canvas;

    private void Start()
    {
        // Lúc đầu game, chỉ hiện Menu, ẩn màn chơi
        mainMenuCanvas.SetActive(true);
        M1Canvas.SetActive(false);
        M2Canvas.SetActive(false);
        M3Canvas.SetActive(false);
    }
    // Hàm này gắn vào Nút Chọn Màn
    public void ChonM1()
    {
        mainMenuCanvas.SetActive(false);
        M1Canvas.SetActive(true);
        M2Canvas.SetActive(false);
        M3Canvas.SetActive(false);
    }
    public void ChonM2()
    {
        mainMenuCanvas.SetActive(false);
        M1Canvas.SetActive(false);
        M2Canvas.SetActive(true);
        M3Canvas.SetActive(false);
    }
    public void ChonM3()
    {
        mainMenuCanvas.SetActive(false);
        M1Canvas.SetActive(false);
        M2Canvas.SetActive(false);
        M3Canvas.SetActive(true);
    }// Hàm để quay lại Menu chính
    public void QuayLaiChonMan()
    {
        mainMenuCanvas.SetActive(true);
        M1Canvas.SetActive(false);
        M2Canvas.SetActive(false);
        M3Canvas.SetActive(false);
    }
}