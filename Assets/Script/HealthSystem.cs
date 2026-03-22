using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [SerializeField]private float maxHealthPlayer = 100f;
    private float currentHealthPlayer;
    [SerializeField] private float maxHealthEnemy = 100f;
    private float currentHealthEnemy;

    [Header("UI Kết nối")]
    public Slider healthPlayer;
    public Slider healthEnemy; // Kéo cái Slider tương ứng vào đây

    void Start()
    {
        currentHealthPlayer = maxHealthPlayer;
        currentHealthEnemy = maxHealthEnemy;
        UpdateUI();
    }

    public void TakeDamagePlayer(float damage)
    {
        currentHealthPlayer -= damage;
        // Đảm bảo máu không xuống dưới 0
        currentHealthPlayer = Mathf.Clamp(currentHealthPlayer, 0, maxHealthPlayer);

        UpdateUI();

        if (currentHealthPlayer <= 0)
        {
            Die();
        }
    }
    public void TakeDamageEnemy(float damage)
    {
        currentHealthEnemy -= damage;
        // Đảm bảo máu không xuống dưới 0
        currentHealthEnemy = Mathf.Clamp(currentHealthEnemy, 0, maxHealthEnemy);

        UpdateUI();

        if (currentHealthPlayer <= 0)
        {
            //Die();
        }
    }


    void UpdateUI()
    {
        if (healthPlayer != null)
        {
            // Slider trong Unity chạy từ 0 đến 1 nên ta chia tỉ lệ
            healthPlayer.value = currentHealthPlayer / maxHealthPlayer;
        }
        if (healthEnemy != null)
        {
            // Slider trong Unity chạy từ 0 đến 1 nên ta chia tỉ lệ
            healthEnemy.value = currentHealthEnemy / maxHealthEnemy;
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " đã thua!");
        // Bạn có thể thêm animation chết ở đây
    }
}