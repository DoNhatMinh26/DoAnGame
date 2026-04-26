using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public EnemyDifficultyConfig difficultyConfig;
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;

    private int maxEnemiesInLevel;
    private float currentSpeed;
    private float spawnRate;
    private int spawnedCount = 0;
    private float nextSpawnTime;

    void Start()
    {
        // Lấy dữ liệu độ khó dựa trên Lớp và Màn hiện tại
        var settings = difficultyConfig.GetDifficulty(UIManager.SelectedGrade, LevelManager.CurrentLevel);

        maxEnemiesInLevel = settings.count;
        currentSpeed = settings.speed;
        spawnRate = settings.spawnRate;
        spawnedCount = 0;
    }

    void Update()
    {
        // Chỉ tạo địch nếu chưa đủ số lượng của màn đó
        if (spawnedCount < maxEnemiesInLevel && Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnEnemy()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        GameObject enemy = Instantiate(enemyPrefab, spawnPoints[randomIndex].position, Quaternion.identity);

        // Gán tốc độ cho kẻ địch vừa tạo
        if (enemy.TryGetComponent(out EnemyMovement move))
        {
            move.speed = currentSpeed;
        }

        spawnedCount++;
    }
    public void ResetSpawner()
    {
        spawnedCount = 0; // Reset số lượng quái đã sinh
        nextSpawnTime = Time.time + 1.0f; // Để bắt đầu tạo quái ngay lập tức

        // Nếu bạn cần cập nhật lại thông số từ Config
        var settings = difficultyConfig.GetDifficulty(UIManager.SelectedGrade, LevelManager.CurrentLevel);
        maxEnemiesInLevel = settings.count;
        currentSpeed = settings.speed;
    }
    public bool IsAllEnemiesSpawned()
    {
        // Kiểm tra xem số lượng đã sinh đã chạm mốc giới hạn của màn chơi chưa
        return spawnedCount >= maxEnemiesInLevel;
    }
}