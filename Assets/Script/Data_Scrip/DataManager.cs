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
        // ✅ Dùng LocalStorageKeyResolver để main/clone không đụng chung PlayerPrefs
        bool isGuest = UIQuickPlayNameController.IsGuestMode();
        string scoreKey = isGuest
            ? DoAnGame.Auth.LocalStorageKeyResolver.LocalGuestScore
            : DoAnGame.Auth.LocalStorageKeyResolver.UserScore;
        string levelKey = isGuest
            ? DoAnGame.Auth.LocalStorageKeyResolver.LocalGuestLevel
            : DoAnGame.Auth.LocalStorageKeyResolver.UserLevel;
        
        int currentScore = PlayerPrefs.GetInt(scoreKey, 0);
        int currentLevel = PlayerPrefs.GetInt(levelKey, 1);

        currentScore += amount;
        Debug.Log($"[DataManager] Diem hien tai: {currentScore} (key: {scoreKey})");

        // ✅ Tính level: dùng pointsPerLevel để có thể thay đổi từ Inspector
        int newLevel = 1 + (currentScore / pointsPerLevel);

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
       
        PlayerPrefs.SetInt(scoreKey, currentScore);
        PlayerPrefs.SetInt(levelKey, newLevel);
        PlayerPrefs.Save();
        
        Debug.Log($"[DataManager] Saved: {scoreKey}={currentScore}, {levelKey}={newLevel}");
        
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

        // Chỉ sync Firebase khi đã đăng nhập (không phải guest)
        if (!isGuest)
        {
            CloudSyncService.Instance?.OnScoreChanged(currentScore, newLevel);
        }
    }

}