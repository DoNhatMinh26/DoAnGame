using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện bắt buộc để chuyển scene

public class MenuManager : MonoBehaviour
{
    // Hàm này dùng để chuyển đến một Scene dựa trên tên của nó
    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Hàm bổ sung nếu bạn muốn thoát game
    /*
    public void QuitGame()
    {
        Debug.Log("Đang thoát game...");
        Application.Quit();
    }
    */
}