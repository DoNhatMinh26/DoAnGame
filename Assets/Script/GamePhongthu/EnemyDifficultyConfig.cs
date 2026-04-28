using UnityEngine;

[System.Serializable]
public class EnemyGradeConfig
{
    public string gradeName;

    [Header("Số lượng Enemy (X: 0→1 = màn 1→100)")]
    public AnimationCurve enemyCountCurve;

    [Header("Tốc độ Enemy (X: 0→1 = màn 1→100)")]
    public AnimationCurve enemySpeedCurve;

    [Header("Tốc độ tạo Enemy - Spawn Rate (X: 0→1 = màn 1→100)")]
    public AnimationCurve spawnRateCurve;
}

[CreateAssetMenu(fileName = "EnemyDifficultyConfig", menuName = "Math Game/Enemy Difficulty Config")]
public class EnemyDifficultyConfig : ScriptableObject
{
    [Header("Cấu hình độ khó 5 lớp")]
    public EnemyGradeConfig[] enemyGrades = new EnemyGradeConfig[5];

    public (int count, float speed, float spawnRate) GetDifficulty(int gradeIndex, int levelIndex)
    {
        gradeIndex = Mathf.Clamp(gradeIndex, 1, 5);
        levelIndex = Mathf.Clamp(levelIndex, 1, 100);
        float t = (levelIndex - 1) / 99f;

        var cfg = enemyGrades[gradeIndex - 1];

        return (
            Mathf.RoundToInt(cfg.enemyCountCurve.Evaluate(t)),
            cfg.enemySpeedCurve.Evaluate(t),
            cfg.spawnRateCurve.Evaluate(t)
        );
    }

#if UNITY_EDITOR
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        for (int i = 0; i < 5; i++)
        {
            // Tính toán bước nhảy (step) cho từng lớp để đảm bảo phân cấp độ khó
            float countStart = 10 + (i * 2); // Lớp 1 bắt đầu từ 10, mỗi lớp sau tăng thêm 2
            float countEnd = 30 + (i * 2);   // Kết thúc ở khoảng 30+

            float speedStart = 1.0f + (i * 0.2f); // Tốc độ lớp 1 là 1.0
            float speedEnd = 3.0f + (i * 0.2f);   // Kết thúc ở khoảng 3.0

            enemyGrades[i] = new EnemyGradeConfig
            {
                gradeName = "Lớp " + (i + 1),

                // Số lượng quái: Từ 10 đến 30
                enemyCountCurve = AnimationCurve.Linear(0, countStart, 1, countEnd),

                // Tốc độ di chuyển: Từ 1 đến 3
                enemySpeedCurve = AnimationCurve.Linear(0, speedStart, 1, speedEnd),

                // Tốc độ tạo (giây): Từ 3 giây xuống còn 2 giây (càng thấp càng nhanh)
                spawnRateCurve = AnimationCurve.Linear(0, 3.0f, 1, 2.0f)
            };
        }
        // Đánh dấu asset đã thay đổi để Unity lưu lại dữ liệu
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("Đã Reset cấu hình 5 lớp: Quái (10-30), Tốc độ (1-3), Tạo (3s-2s)");
    }
#endif
}