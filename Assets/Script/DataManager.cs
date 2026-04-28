using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    [Header("Dữ liệu Cách chơi 2 (Mèo phòng thủ)")]
    public int totalCoins;
    public int currentLevelMode2;

    private void Awake()
    {
        // Khởi tạo Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Hàm lưu toàn bộ dữ liệu
    public void SaveData()
    {
        // Lưu tiền
        PlayerPrefs.SetInt("TotalCoins", totalCoins);

        // Lưu số màn chơi của cách chơi số 2
        PlayerPrefs.SetInt("CurrentLevelMode2", currentLevelMode2);

        PlayerPrefs.Save();
        Debug.Log("Dữ liệu đã được lưu thành công!");
    }

    // Hàm tải dữ liệu
    public void LoadData()
    {
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        currentLevelMode2 = PlayerPrefs.GetInt("CurrentLevelMode2", 1); // Mặc định bắt đầu từ màn 1
    }

    // Hàm cộng thêm tiền
    public void AddCoins(int amount)
    {
        totalCoins += amount;
        SaveData();
    }

    // Hàm cập nhật số màn chơi khi thắng
    public void UpdateLevelMode2(int newLevel)
    {
        if (newLevel > currentLevelMode2)
        {
            currentLevelMode2 = newLevel;
            SaveData();
        }
    }

    // Hàm reset dữ liệu (Dùng khi bạn muốn chơi lại từ đầu)
    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        LoadData();
        Debug.Log("Đã xóa toàn bộ dữ liệu game!");
    }
}