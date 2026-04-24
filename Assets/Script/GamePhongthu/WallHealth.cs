using UnityEngine;
using UnityEngine.UI;

public class WallHealth : MonoBehaviour
{
    public static WallHealth Instance;

    [Header("Cấu hình Máu")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI Component")]
    public Slider healthSlider; // Kéo Slider máu vào đây

    private void Awake()
    {
        // Khởi tạo Instance để các script khác (như EnemyMovement) có thể gọi TakeDamage
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        currentHealth = maxHealth;

        // Thiết lập giá trị ban đầu cho thanh máu
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(float damage)
    {
        // Trừ máu khi bị địch tấn công
        currentHealth -= damage;

        // Cập nhật thanh hiển thị
        if (healthSlider != null) healthSlider.value = currentHealth;

        // Kiểm tra điều kiện thua cuộc
        if (currentHealth <= 0)
        {
            currentHealth = 0; // Đảm bảo máu không bị âm
            GameOver();
        }
    }

    void GameOver()
    {
        // Đổi UITp thành GameUIManager nếu đó là tên lớp bạn đang dùng
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowLose();
        }
        else
        {
            Time.timeScale = 0f;
            Debug.Log("Thành đã thất thủ!");
        }
    }

    // Hàm gọi khi nhấn "Chơi lại" từ bảng kết quả
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        if (healthSlider != null) healthSlider.value = currentHealth;

        // Đảm bảo thời gian chạy lại bình thường
        Time.timeScale = 1f;
    }
}