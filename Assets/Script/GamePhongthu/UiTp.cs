using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Cấu hình Danh sách Màn chơi")]
    public GameObject levelButtonPrefab;
    public RectTransform contentParent;
    [SerializeField] private float buttonSpacing = 160f;
    [SerializeField] private float waveAmplitude = 100f;
    [SerializeField] private float waveFrequency = 0.4f;

    [Header("Quản lý các Panel Chính")]
    public GameObject panelHome;
    public GameObject panelChonMan;
    public GameObject panelGameplay;
    public GameObject panelSetting;

    [Header("Quản lý Panel Kết Thúc")]
    public GameObject panelWin;   // Kéo Panel chiến thắng vào đây
    public GameObject panelLose;  // Kéo Panel thất bại vào đây
    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Chỉ kiểm tra khi đang trong màn chơi và chưa kết thúc game
        
        GenerateLevelButtons();

        // Đảm bảo các panel kết thúc ẩn lúc đầu
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
        if (panelSetting != null) panelSetting.SetActive(false);

        ShowHome();
    }
    private void Update()
    {
        if (panelGameplay.activeSelf && !isGameOver)
        {
            CheckWinCondition();
        }
    }

    #region CÁC HÀM ĐIỀU HƯỚNG
    public void Click_OpenChonMan()
    {
        DeactivateAll();
        if (panelChonMan != null) panelChonMan.SetActive(true);
        Time.timeScale = 1f;
    }

    public void Click_BackToHome()
    {
        DragAndDrop[] allAnswers = FindObjectsOfType<DragAndDrop>();
        foreach (DragAndDrop btn in allAnswers)
        {
            btn.ForceResetPosition();
        }
        Time.timeScale = 1f;

        // 1. Dọn dẹp quái và đạn
        ClearAllGameplayObjects();

        // 2. GIẢI QUYẾT LỖI: Giải phóng hoàn toàn các biến khóa static
        DragAndDrop.ReleaseAllLocks();
        DragAndDrop.SetGlobalLock(false);

        // 3. Ngừng Spawner
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        DeactivateAll();
        if (panelHome != null) panelHome.SetActive(true);
    }

    public void ShowGameplay()
    {
        DeactivateAll();
        if (panelGameplay != null) panelGameplay.SetActive(true);
        Time.timeScale = 1f;
    }

    private void DeactivateAll()
    {
        if (panelHome != null) panelHome.SetActive(false);
        if (panelChonMan != null) panelChonMan.SetActive(false);
        if (panelGameplay != null) panelGameplay.SetActive(false);
        if (panelSetting != null) panelSetting.SetActive(false);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
    }

    public void ShowHome()
    {
        DeactivateAll();
        if (panelHome != null) panelHome.SetActive(true);
    }
    #endregion

    #region HÀM XỬ LÝ THẮNG / THUA / SETTING
    public void ShowWin()
    {
        if (panelWin != null)
        {
            panelWin.SetActive(true);
            Time.timeScale = 0f;
            DragAndDrop.SetGlobalLock(true);

            // LƯU DỮ LIỆU MỞ KHÓA MÀN TIẾP THEO
            int currentHighest = PlayerPrefs.GetInt("HighestLevelReached", 1);
            int wonLevel = LevelManager.CurrentLevel;

            // Nếu thắng màn hiện tại là màn cao nhất, mở màn tiếp theo
            if (wonLevel == currentHighest && wonLevel < 100)
            {
                PlayerPrefs.SetInt("HighestLevelReached", wonLevel + 1);
                PlayerPrefs.Save(); // Đảm bảo dữ liệu được ghi xuống bộ nhớ

                // Cập nhật lại danh sách nút để hiển thị màn mới vừa mở
                GenerateLevelButtons();
            }
        }
    }

    public void ShowLose()
    {
        if (panelLose != null)
        {
            panelLose.SetActive(true);
            Time.timeScale = 0f; // Tạm dừng game khi thua
            DragAndDrop.SetGlobalLock(true); // KHÓA kéo thả khi thắng
        }

    }

    public void Click_Retry()
    {
        Time.timeScale = 1f;
        DragAndDrop.ReleaseAllLocks();
        BatDauChoiMan(LevelManager.CurrentLevel);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
    }

    public void Click_Next()
    {
        Time.timeScale = 1f;
        int nextLevel = LevelManager.CurrentLevel + 1;
        if (nextLevel > 100) nextLevel = 100;

        BatDauChoiMan(nextLevel); // Tự động reset nút bên trong hàm này
        if (panelWin != null) panelWin.SetActive(false);
    }
    public void Click_OpenSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(true);
            Time.timeScale = 0f; // Tự động làm bộ đếm bên DragAndDrop dừng lại
            DragAndDrop.SetGlobalLock(true);
        }
    }

    public void Click_CloseSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);

            if (!panelWin.activeSelf && !panelLose.activeSelf)
            {
                Time.timeScale = 1f; // Tự động làm bộ đếm chạy tiếp từ giây cũ

                // Hàm này cực kỳ quan trọng để kiểm tra xem có ai đang bị phạt không
                CheckPunishmentStatus();
            }
        }
    }

    private void CheckPunishmentStatus()
    {
        DragAndDrop[] allChoices = FindObjectsOfType<DragAndDrop>();
        bool isAnyoneRed = false;

        foreach (var choice in allChoices)
        {
            // Nếu vẫn còn nút màu đỏ, nghĩa là nó vẫn đang trong thời gian phạt (Coroutine chưa xong)
            if (choice.GetComponent<Image>().color == choice.colorWrong)
            {
                isAnyoneRed = true;
                break;
            }
        }

        // Nếu không ai bị đỏ thì mới mở khóa kéo thả
        if (!isAnyoneRed)
        {
            DragAndDrop.SetGlobalLock(false);
        }
    }

    private void ClearAllGameplayObjects()
    {
        // 1. Tìm và xóa sạch kẻ địch (Tag: Enemy)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies) Destroy(e);

        // 2. Tìm và xóa sạch các viên đạn đang bay (Tag: Dan)
        // Đảm bảo bạn đã đặt Tag cho Prefab viên đạn là "Dan" trong Inspector
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Dan");
        foreach (GameObject b in bullets) Destroy(b);
    }
    #endregion

    #region LOGIC SINH NÚT MÀN CHƠI
    private void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null || contentParent == null) return;
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Lấy màn cao nhất đã mở khóa (Mặc định là màn 1)
        int highestLevelReached = PlayerPrefs.GetInt("HighestLevelReached", 1);

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            Button btn = btnObj.GetComponent<Button>();

            // Tính toán vị trí nút (giữ nguyên logic của bạn)
            float posX = (i - 1) * buttonSpacing;
            float posY = Mathf.Sin(i * waveFrequency) * waveAmplitude;
            btnRect.anchoredPosition = new Vector2(posX + (buttonSpacing / 2f), posY);

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = i.ToString();

            int levelIndex = i;

            // KIỂM TRA MỞ KHÓA
            if (i <= highestLevelReached)
            {
                // Màn đã mở: Cho phép bấm và để màu bình thường
                btn.interactable = true;
                btn.image.color = Color.white;
                btn.onClick.AddListener(() => BatDauChoiMan(levelIndex));
            }
            else
            {
                // Màn bị khóa: Không cho bấm và làm mờ nút
                btn.interactable = false;
                btn.image.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Màu xám mờ
            }
        }
        contentParent.sizeDelta = new Vector2(100 * buttonSpacing, contentParent.sizeDelta.y);
    }

    public void BatDauChoiMan(int levelIndex)
    {
        // Luôn ưu tiên mở khóa và chạy lại thời gian đầu tiên
        Time.timeScale = 1f;
        DragAndDrop.ReleaseAllLocks();
        DragAndDrop.SetGlobalLock(false);

        // Reset vị trí các nút (đã thêm ở bước trước)
        DragAndDrop[] allAnswers = FindObjectsOfType<DragAndDrop>();
        foreach (DragAndDrop btn in allAnswers)
        {
            btn.ForceResetPosition();
        }

        isGameOver = false;
        ClearAllGameplayObjects();
        LevelManager.CurrentLevel = levelIndex;
        ShowGameplay();

        // Reset Spawner và Máu tường
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.enabled = true;
            spawner.ResetSpawner();
        }

        if (WallHealth.Instance != null)
        {
            WallHealth.Instance.ResetHealth();
        }

        DragQuizManager qm = FindObjectOfType<DragQuizManager>();
        if (qm != null) qm.UpdateDifficulty();
    }
    void CheckWinCondition()
    {
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();

        // 1. Kiểm tra Spawner đã sinh hết quái theo cấu hình chưa
        if (spawner != null && spawner.enabled && spawner.IsAllEnemiesSpawned())
        {
            // 2. Đếm số lượng Enemy còn sống trên màn hình
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            if (enemies.Length == 0)
            {
                isGameOver = true;
                ShowWin(); // Gọi hàm hiện bảng thắng đã viết ở turn trước
            }
        }
    }
    
    #endregion
}