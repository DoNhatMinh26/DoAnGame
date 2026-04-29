using UnityEngine;

[System.Serializable]
public class SpaceGradeConfig
{
    public string gradeName;

    [Header("Số cổng để thắng (X: 0→1 = màn 1→100)")]
    public AnimationCurve gateCountCurve;

    [Header("Tốc độ bay - World Speed (X: 0→1 = màn 1→100)")]
    public AnimationCurve worldSpeedCurve;

    [Header("Khoảng cách giữa các cổng (X: 0→1 = màn 1→100)")]
    public AnimationCurve distanceCurve;
}

[CreateAssetMenu(fileName = "SpaceDifficultyConfig", menuName = "Math Game/Space Difficulty Config")]
public class SpaceDifficultyConfig : ScriptableObject
{
    [Header("Cấu hình độ khó 5 lớp - Chế độ Space")]
    public SpaceGradeConfig[] spaceGrades = new SpaceGradeConfig[5];

    public (int gateCount, float speed, float distance) GetDifficulty(int gradeIndex, int levelIndex)
    {
        gradeIndex = Mathf.Clamp(gradeIndex, 1, 5);
        levelIndex = Mathf.Clamp(levelIndex, 1, 100);
        float t = (levelIndex - 1) / 99f;

        var cfg = spaceGrades[gradeIndex - 1];

        return (
            Mathf.RoundToInt(cfg.gateCountCurve.Evaluate(t)),
            cfg.worldSpeedCurve.Evaluate(t),
            cfg.distanceCurve.Evaluate(t)
        );
    }

#if UNITY_EDITOR
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        for (int i = 0; i < 5; i++)
        {
            spaceGrades[i] = new SpaceGradeConfig
            {
                gradeName = "Lớp " + (i + 1),

                // Số cổng: Cố định cho cả 5 lớp, chạy từ 5 cổng (màn 1) đến 20 cổng (màn 100)
                gateCountCurve = AnimationCurve.Linear(0, 5, 1, 20),

                // Tốc độ: Chạy từ 3 (màn 1) đến 6 (màn 100)
                worldSpeedCurve = AnimationCurve.Linear(0, 3f, 1, 6f),

                // Khoảng cách: Chạy từ 45 (màn 1) giảm dần về 35 (màn 100)
                distanceCurve = AnimationCurve.Linear(0, 45f, 1, 35f)
            };
        }

        // Đánh dấu object đã thay đổi để Unity lưu lại dữ liệu Asset
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
#endif
}