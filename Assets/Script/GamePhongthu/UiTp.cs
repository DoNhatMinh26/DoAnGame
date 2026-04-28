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

    [Header("Quản lý Tiền")]
    public TextMeshProUGUI totalCoinTxt; // Text hiển thị ở Shop/Menu
    public TextMeshProUGUI levelCoinTxt; // Text hiển thị trong trận đấu (Gameplay)
    public Transform coinTarget;

    [Header("Quản lý Skin")]
    public SpriteRenderer catRenderer;
    public SpriteRenderer gameplayCatRenderer;// Kéo SpriteRenderer của con mèo vào đây
    public CatSkin[] allSkins; // Danh sách các Skin bạn tạo ra
    [Header("Giao diện Shop")]
    public Image[] skinButtonImages;
    public TextMeshProUGUI[] skinPriceTexts;
    private int pendingSkinIndex = -1;

    private int totalCoins = 0;
    private int levelCoins = 0;

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
        // Tải tiền tổng từ máy
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        // Tải và mặc skin đã lưu
        LoadCurrentSkin();
        UpdateShopUI();
        UpdateCoinUI();
        
        GenerateLevelButtons();

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
    void UpdateCoinUI()
    {
        if (totalCoinTxt != null) totalCoinTxt.text = totalCoins.ToString();
        if (levelCoinTxt != null) levelCoinTxt.text = levelCoins.ToString();
    }
    public void UpdateCoinsFromShop(int newTotal)
    {
        totalCoins = newTotal;
        UpdateCoinUI();
    }
    public void AddCoins(int amount)
    {
        levelCoins += amount;
        totalCoins += amount;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();
        UpdateCoinUI();
    }
    public void LoadCurrentSkin()
    {
        int currentSkinID = PlayerPrefs.GetInt("SelectedSkinID", 0);

        if (allSkins != null && currentSkinID < allSkins.Length && allSkins[currentSkinID] != null)
        {
            Sprite skinSprite = allSkins[currentSkinID].skinSprite;

            // Gắn cho mèo ở Menu
            if (catRenderer != null) catRenderer.sprite = skinSprite;

            // Gắn cho mèo trong trận đấu
            if (gameplayCatRenderer != null) gameplayCatRenderer.sprite = skinSprite;

            Debug.Log("Đã cập nhật skin cho cả Menu và Gameplay.");
        }
    }
    public bool IsSkinUnlocked(int index)
    {
        // Skin 0 luôn mở
        if (index == 0) return true;

        // Kiểm tra PlayerPrefs, trả về 0 nếu chưa từng lưu (mặc định là khóa)
        return PlayerPrefs.GetInt("SkinUnlocked_" + index, 0) == 1;
    }



    public void UpdateShopUI()
    {
        if (allSkins == null || skinButtonImages == null) return;

        for (int i = 0; i < allSkins.Length; i++)
        {
            if (i >= skinButtonImages.Length || skinButtonImages[i] == null) continue;

            bool isUnlocked = IsSkinUnlocked(i);

            // 1. Cập nhật màu sắc (sáng/tối)
            if (isUnlocked)
            {
                skinButtonImages[i].color = Color.white;

                // 2. Cập nhật chữ hiển thị thành "Đã sở hữu" nếu mảng Text tồn tại
                if (i < skinPriceTexts.Length && skinPriceTexts[i] != null)
                {
                    skinPriceTexts[i].text = "Đã sở hữu";
                }
            }
            else
            {
                skinButtonImages[i].color = new Color(0.3f, 0.3f, 0.3f, 1f);

                // Hiện lại giá gốc từ ScriptableObject nếu chưa mua
                if (i < skinPriceTexts.Length && skinPriceTexts[i] != null)
                {
                    skinPriceTexts[i].text = allSkins[i].price.ToString();
                }
            }
        }
    }
    public void SelectSkinToPreview(int index)
    {
        // Đảm bảo index nằm trong mảng hợp lệ
        if (allSkins == null || index < 0 || index >= allSkins.Length) return;

        pendingSkinIndex = index; // Ghi nhớ skin đang chọn

        // Đổi hình ảnh mèo để người chơi xem thử
        CatSkin skin = allSkins[index];
        catRenderer.sprite = skin.skinSprite;

        Debug.Log("Đang chọn xem thử Skin Index: " + index);
    }

    public void Click_ConfirmPurchase()
    {
        // Nếu chưa chọn skin nào để xem thì không làm gì cả
        if (pendingSkinIndex == -1) return;

        // Nếu skin đã mở khóa rồi thì không cần mua nữa
        if (IsSkinUnlocked(pendingSkinIndex)) return;

        CatSkin skin = allSkins[pendingSkinIndex];

        if (totalCoins >= skin.price)
        {
            // 1. Trừ tiền
            totalCoins -= skin.price;
            PlayerPrefs.SetInt("TotalCoins", totalCoins);
            // 2. Mở khóa skin
            PlayerPrefs.SetInt("SkinUnlocked_" + pendingSkinIndex, 1);
            // 3. Mặc luôn skin vừa mua
            PlayerPrefs.SetInt("SelectedSkinID", pendingSkinIndex);
            PlayerPrefs.Save();
            // 4. Cập nhật giao diện
            UpdateCoinUI();
            UpdateShopUI(); // Làm sáng nút skin trong shop

            Debug.Log("Mua thành công: " + skin.skinName);
        }
        else
        {
            Debug.Log("Không đủ tiền mua skin này!");
        }
    }
    public void BuyAndApplySkin(int index)
    {
        if (allSkins == null || index < 0 || index >= allSkins.Length) return;

        pendingSkinIndex = index;
        CatSkin skin = allSkins[index];

        // Cho xem thử trên cả hai renderer
        if (catRenderer != null) catRenderer.sprite = skin.skinSprite;
        if (gameplayCatRenderer != null) gameplayCatRenderer.sprite = skin.skinSprite;

        if (IsSkinUnlocked(index))
        {
            PlayerPrefs.SetInt("SelectedSkinID", index);
            PlayerPrefs.Save();
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
        UpdateShopUI();
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
    public void ResetAllGameData()
    {
        // Xóa tiền
        PlayerPrefs.DeleteKey("TotalCoins");
        totalCoins = 0;
        UpdateCoinUI();

        // Xóa skin (gọi hàm vừa tạo ở trên)
        ResetSkins();

        // Xóa tiến trình màn chơi
        PlayerPrefs.DeleteKey("HighestLevelReached");

        PlayerPrefs.Save();

        // Tải lại danh sách nút chọn màn để khóa các màn đã mở
        GenerateLevelButtons();

        Debug.Log("Dữ liệu game đã được xóa sạch hoàn toàn!");
    }
    // Thêm hàm này vào class GameUIManager trong file UiTp.cs
    public void ResetSkins()
    {
        // 1. Reset ID skin đang mặc về 0 (Mặc định)
        PlayerPrefs.SetInt("SelectedSkinID", 0);

        // 2. Khóa lại tất cả các skin (trừ skin index 0)
        // Giả sử bạn có 3 skin (0, 1, 2), vòng lặp sẽ chạy từ 1 trở đi
        for (int i = 1; i < allSkins.Length; i++)
        {
            PlayerPrefs.DeleteKey("SkinUnlocked_" + i);
        }

        PlayerPrefs.Save();

        // 3. Cập nhật lại hình ảnh con mèo ngay lập tức
        LoadCurrentSkin();

        // 4. Cập nhật lại giao diện Shop (làm tối các skin vừa bị khóa)
        UpdateShopUI();

        Debug.Log("Đã reset toàn bộ Skin về trạng thái ban đầu!");
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
        LoadCurrentSkin();
        levelCoins = 0;
        UpdateCoinUI();
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