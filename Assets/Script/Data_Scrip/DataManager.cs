using UnityEngine;

public class DataManager : MonoBehaviour
{
    private static DataManager _instance;
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DataManager>();
            }
            return _instance;
        }
    }

    [Header("Cấu hình Level")]
    [SerializeField] private int pointsPerLevel = 200;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Luôn sống để nhận điểm từ màn chơi
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void AddScore(int amount)
    {
        int currentScore = PlayerPrefs.GetInt("UserScore", 0);
        int currentLevel = PlayerPrefs.GetInt("UserLevel", 1);

        currentScore += amount;
        Debug.Log("Diem hien tai: " + currentScore);

        // Kiểm tra thăng cấp
        if (currentScore >= currentLevel * pointsPerLevel)
        {
            currentLevel++;
            PlayerPrefs.SetInt("UserLevel", currentLevel);

            if (UiClass.Instance != null)
            {
                UiClass.Instance.AddCoins(50);
            }
        }

        PlayerPrefs.SetInt("UserScore", currentScore);
        PlayerPrefs.Save(); // Dữ liệu đã vào máy an toàn
    }

    public void Click_ResetAllGameData()
    {
        // 1. Xóa sạch mọi thứ trong máy
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. Làm mới các hệ thống đang chạy
        RefreshAllSystems();

        ProfileUI profile = FindObjectOfType<ProfileUI>();
        if (profile != null)
        {
            profile.UpdateProfileDisplay();
        }

        Debug.Log("Dữ liệu toàn bộ game đã được reset về 0.");
    }

    private void RefreshAllSystems()
    {
        if (UiClass.Instance != null)
        {
            UiClass.Instance.AddCoins(0);
            UiClass.Instance.LoadCurrentSkin();
            UiClass.Instance.UpdateSkinShopUI();
            UiClass.Instance.GenerateLevelButtons();
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateCoinsFromShop(0);
            GameUIManager.Instance.LoadCurrentSkin();
            GameUIManager.Instance.UpdateShopUI();
        }

        if (UiSp.Instance != null)
        {
            UiSp.Instance.AddCoins(0);
            UiSp.Instance.LoadCurrentShip();
            UiSp.Instance.UpdateShipShopUI();
        }
    }
}