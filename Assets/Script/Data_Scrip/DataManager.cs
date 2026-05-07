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
        }
        if (UiClass.Instance != null)
        {
            UiClass.Instance.AddLevelScore(amount);
        }
        
        // ✅ Sync score + level lên Firebase nếu đã đăng nhập
        // CloudSyncService sẽ cập nhật PlayerPrefs trước khi sync Firebase
        CloudSyncService.Instance?.OnScoreChanged(currentScore, newLevel);
    }

    public void Click_ResetAllGameData()
    {
        // Lưu grade trước khi xóa (không reset độ khó lớp)
        int savedGrade = UIManager.SelectedGrade;
        if (savedGrade < 1 || savedGrade > 5)
            savedGrade = DoAnGame.UI.UIQuickPlayNameController.GetSelectedGrade();
        if (savedGrade < 1 || savedGrade > 5) savedGrade = 1;

        // 1. Xóa sạch mọi thứ trong máy
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. Restore lại grade (không bị reset)
        UIManager.SelectedGrade = savedGrade;
        DoAnGame.UI.UIQuickPlayNameController.SaveSelectedGrade(savedGrade);

        // 3. Sync reset lên Firebase (nếu đã đăng nhập)
        var cloudSync = DoAnGame.Auth.CloudSyncService.Instance;
        if (cloudSync != null)
        {
            _ = cloudSync.ResetPlayerDataOnFirebase();
        }

        // 4. Làm mới các hệ thống đang chạy
        RefreshAllSystems();

        ProfileUI profile = FindObjectOfType<ProfileUI>();
        if (profile != null)
        {
            profile.UpdateProfileDisplay();
        }

        Debug.Log($"[DataManager] Đã reset toàn bộ dữ liệu. Giữ nguyên lớp: {savedGrade}");
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