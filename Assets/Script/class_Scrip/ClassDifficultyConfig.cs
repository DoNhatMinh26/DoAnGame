using UnityEngine;

[System.Serializable]
public class ClassGradeConfig
{
    public string gradeName;

    [Header("Số câu hỏi để thắng (X: 0→1 = màn 1→100)")]
    public AnimationCurve questionCountCurve;
}

[CreateAssetMenu(fileName = "ClassDifficultyConfig", menuName = "Math Game/Class Difficulty Config")]
public class ClassDifficultyConfig : ScriptableObject
{
    [Header("Cấu hình độ khó 5 lớp - Chế độ Lớp Học")]
    public ClassGradeConfig[] classGrades = new ClassGradeConfig[5];

    /// <summary>
    /// Lấy số lượng câu hỏi cần trả lời đúng dựa trên khối lớp và màn chơi.
    /// </summary>
    public int GetTargetQuestions(int gradeIndex, int levelIndex)
    {
        // Đảm bảo gradeIndex từ 1-5 và levelIndex từ 1-100
        gradeIndex = Mathf.Clamp(gradeIndex, 1, 5);
        levelIndex = Mathf.Clamp(levelIndex, 1, 100);

        // Chuẩn hóa level về dải 0 -> 1 để dùng cho AnimationCurve
        float t = (levelIndex - 1) / 99f;

        var cfg = classGrades[gradeIndex - 1];

        // Trả về số nguyên được làm tròn từ biểu đồ
        return Mathf.RoundToInt(cfg.questionCountCurve.Evaluate(t));
    }

#if UNITY_EDITOR
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        for (int i = 0; i < 5; i++)
        {
            classGrades[i] = new ClassGradeConfig
            {
                gradeName = "Lớp " + (i + 1),

                // Mặc định: Màn 1 cần đúng 5 câu, Màn 100 cần đúng 20 câu[cite: 6]
                questionCountCurve = AnimationCurve.Linear(0, 5, 1, 20)
            };
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}