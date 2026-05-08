using UnityEngine;
using DoAnGame.Auth;
using DoAnGame.UI;

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
        // ✅ Đọc từ đúng key tuỳ theo chế độ
        string scoreKey = UIQuickPlayNameController.IsGuestMode() ? "LocalGuestScore" : "UserScore";
        string levelKey = UIQuickPlayNameController.IsGuestMode() ? "LocalGuestLevel" : "UserLevel";
        
        int currentScore = PlayerPrefs.GetInt(scoreKey, 0);
        int currentLevel = PlayerPrefs.GetInt(levelKey, 1);

        currentScore += amount;
        Debug.Log($"[DataManager] Diem hien tai: {currentScore} (key: {scoreKey})");

        // Tính level từ tổng score (nhất quán với công thức Firebase)
        // totalXp = totalScore / 10
        // level   = 1 + (totalXp / 100)
        int newLevel = 1 + (currentScore / 10 / 100); // = 1 + (currentScore / 1000)

        // Thăng cấp → thưởng tiền (chỉ khi level thực sự tăng)
        if (newLevel > currentLevel)
        {
            int levelsGained = newLevel - currentLevel;
            if (UiClass.Instance != null)
            {
                UiClass.Instance.AddCoins(50 * levelsGained);
            }
            PlayerPrefs.SetInt(levelKey, newLevel);
            Debug.Log($"[DataManager] Thăng cấp! {currentLevel} → {newLevel}, thưởng {50 * levelsGained} tiền");
        }
       
        // ✅ Ghi vào đúng key
        PlayerPrefs.SetInt(scoreKey, currentScore);
        PlayerPrefs.SetInt(levelKey, newLevel);
        PlayerPrefs.Save();
        
        Debug.Log($"[DataManager] Saved to PlayerPrefs: {scoreKey}={currentScore}, {levelKey}={newLevel}");
        
        if (UiClass.Instance != null)
        {
            UiClass.Instance.UpdateShopProfileUI();
            UiClass.Instance.AddLevelScore(amount);
        }
     
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateShopProfileUI();
        }
        if (UiSp.Instance != null)
        {
            UiSp.Instance.UpdateShopProfileUI();
        }
        // ✅ Sync score + level lên Firebase nếu đã đăng nhập
        // CloudSyncService sẽ cập nhật PlayerPrefs trước khi sync Firebase
        CloudSyncService.Instance?.OnScoreChanged(currentScore, newLevel);
    }

}