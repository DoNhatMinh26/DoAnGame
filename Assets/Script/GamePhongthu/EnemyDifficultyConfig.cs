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
            enemyGrades[i] = new EnemyGradeConfig
            {
                gradeName = "Lớp " + (i + 1),
                enemyCountCurve = AnimationCurve.Linear(0, 5 + i * 2, 1, 15 + i * 5),
                enemySpeedCurve = AnimationCurve.Linear(0, 1.5f + i * 0.5f, 1, 3.0f + i * 1f),
                spawnRateCurve = AnimationCurve.Linear(0, 4.0f, 1, 2.0f) // Màn càng cao tạo địch càng nhanh
            };
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}