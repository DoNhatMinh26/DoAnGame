using System.Collections.Generic;
using UnityEngine;

// ──────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────

/// <summary>3 dạng toán cho mỗi cấp độ.</summary>
public enum MathCategory
{
    TypeA, // Dạng A
    TypeB, // Dạng B
    TypeC  // Dạng C (Hỗn hợp)
}

// ──────────────────────────────────────────────
// Config một dạng toán – dùng AnimationCurve
// ──────────────────────────────────────────────

[System.Serializable]
public class MathTypeConfig
{
    [Header("Tên dạng toán")]
    public string mathTypeName;

    [Header("Phép tính cho phép (0=+  1=−  2=×  3=÷)")]
    public byte[] allowedOperators;

    [Header("Đồ thị độ khó (X: 0→1 = màn 1→100)")]
    [Tooltip("Số nhỏ nhất trong bài toán")]
    public AnimationCurve minNumberCurve;

    [Tooltip("Số lớn nhất trong bài toán")]
    public AnimationCurve maxNumberCurve;

    [Tooltip("Giới hạn thời gian (giây)")]
    public AnimationCurve timeLimitCurve;

    [Tooltip("Số câu hỏi mỗi màn")]
    public AnimationCurve questionCountCurve;
}

// ──────────────────────────────────────────────
// Quy tắc một lớp học
// ──────────────────────────────────────────────

[System.Serializable]
public class GradeRule
{
    [Header("Thông tin lớp")]
    public string gradeName;

    [Header("3 Dạng toán")]
    public MathTypeConfig typeA;
    public MathTypeConfig typeB;
    public MathTypeConfig typeC;

    public MathTypeConfig GetMathConfig(MathCategory cat) => cat switch
    {
        MathCategory.TypeA => typeA,
        MathCategory.TypeB => typeB,
        _ => typeC
    };
}

// ──────────────────────────────────────────────
// Kết quả trả về khi load màn
// ──────────────────────────────────────────────

public class LevelParameters
{
    public int GradeIndex;       // Lớp (1–5)
    public int LevelIndex;       // Màn (1–100)
    public float Progress;         // 0.0 → 1.0
    public MathCategory MathType;         // Dạng toán (ngẫu nhiên)
    public int MinNumber;        // Số nhỏ nhất
    public int MaxNumber;        // Số lớn nhất
    public float TimeLimit;        // Giới hạn thời gian
    public int QuestionCount;    // Số câu hỏi
    public byte[] AllowedOperators; // Phép tính cho phép
}

// ──────────────────────────────────────────────
// ScriptableObject chính
// ──────────────────────────────────────────────

[CreateAssetMenu(fileName = "LevelGenerate", menuName = "Math Game/Level Generate")]
public class LevelGenerate : ScriptableObject
{
    [Header("5 Lớp học (Grade 1 → Grade 5)")]
    public GradeRule[] gradeRules = new GradeRule[5];

    public LevelParameters GetConfigForLevel(int gradeIndex, int levelIndex)
    {
        gradeIndex = Mathf.Clamp(gradeIndex, 1, 5);
        levelIndex = Mathf.Clamp(levelIndex, 1, 100);

        var rule = gradeRules[gradeIndex - 1];
        float t = (levelIndex - 1) / 99f;

        // Vẫn giữ random MathCategory (Type A/B/C) để tạo sự đa dạng trong 1 màn chơi
        var mathCat = (MathCategory)Random.Range(0, 3);
        var cfg = rule.GetMathConfig(mathCat);

        return new LevelParameters
        {
            GradeIndex = gradeIndex,
            LevelIndex = levelIndex,
            Progress = t,
            MathType = mathCat,
            MinNumber = Mathf.RoundToInt(cfg.minNumberCurve.Evaluate(t)),
            MaxNumber = Mathf.RoundToInt(cfg.maxNumberCurve.Evaluate(t)),
            TimeLimit = cfg.timeLimitCurve.Evaluate(t),
            QuestionCount = Mathf.RoundToInt(cfg.questionCountCurve.Evaluate(t)),
            AllowedOperators = cfg.allowedOperators,
        };
    }

#if UNITY_EDITOR
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        gradeRules = new GradeRule[5]
        {
            BuildGrade1(),
            BuildGrade2(),
            BuildGrade3(),
            BuildGrade4(),
            BuildGrade5(),
        };
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static AnimationCurve Lin(float y0, float y1) => AnimationCurve.Linear(0f, y0, 1f, y1);

    private static GradeRule BuildGrade1() => new()
    {
        gradeName = "Lớp 1",
        typeA = new MathTypeConfig { mathTypeName = "Cộng", allowedOperators = new byte[] { 0 }, minNumberCurve = Lin(1, 5), maxNumberCurve = Lin(5, 20), timeLimitCurve = Lin(30, 15), questionCountCurve = Lin(5, 10) },
        typeB = new MathTypeConfig { mathTypeName = "Trừ", allowedOperators = new byte[] { 1 }, minNumberCurve = Lin(1, 5), maxNumberCurve = Lin(5, 20), timeLimitCurve = Lin(30, 15), questionCountCurve = Lin(5, 10) },
        typeC = new MathTypeConfig { mathTypeName = "Hỗn hợp", allowedOperators = new byte[] { 0, 1 }, minNumberCurve = Lin(1, 5), maxNumberCurve = Lin(5, 20), timeLimitCurve = Lin(25, 12), questionCountCurve = Lin(5, 12) }
    };

    // Các hàm BuildGrade2, 3, 4, 5 tương tự như cũ nhưng đã bỏ phần Mode
    private static GradeRule BuildGrade2() => new() { gradeName = "Lớp 2", typeA = new MathTypeConfig { mathTypeName = "Cộng (100)", allowedOperators = new byte[] { 0 }, minNumberCurve = Lin(5, 20), maxNumberCurve = Lin(20, 100), timeLimitCurve = Lin(25, 12), questionCountCurve = Lin(6, 12) }, typeB = new MathTypeConfig { mathTypeName = "Trừ (100)", allowedOperators = new byte[] { 1 }, minNumberCurve = Lin(5, 20), maxNumberCurve = Lin(20, 100), timeLimitCurve = Lin(25, 12), questionCountCurve = Lin(6, 12) }, typeC = new MathTypeConfig { mathTypeName = "Nhân 2-5", allowedOperators = new byte[] { 2 }, minNumberCurve = Lin(2, 5), maxNumberCurve = Lin(5, 10), timeLimitCurve = Lin(25, 10), questionCountCurve = Lin(6, 12) } };
    private static GradeRule BuildGrade3() => new() { gradeName = "Lớp 3", typeA = new MathTypeConfig { mathTypeName = "Nhân", allowedOperators = new byte[] { 2 }, minNumberCurve = Lin(2, 9), maxNumberCurve = Lin(9, 12), timeLimitCurve = Lin(20, 8), questionCountCurve = Lin(8, 15) }, typeB = new MathTypeConfig { mathTypeName = "Chia", allowedOperators = new byte[] { 3 }, minNumberCurve = Lin(2, 9), maxNumberCurve = Lin(9, 12), timeLimitCurve = Lin(20, 8), questionCountCurve = Lin(8, 15) }, typeC = new MathTypeConfig { mathTypeName = "Hỗn hợp 4 phép", allowedOperators = new byte[] { 0, 1, 2, 3 }, minNumberCurve = Lin(5, 50), maxNumberCurve = Lin(50, 200), timeLimitCurve = Lin(20, 8), questionCountCurve = Lin(8, 15) } };
    private static GradeRule BuildGrade4() => new() { gradeName = "Lớp 4", typeA = new MathTypeConfig { mathTypeName = "Nhân/Chia lớn", allowedOperators = new byte[] { 2, 3 }, minNumberCurve = Lin(10, 100), maxNumberCurve = Lin(100, 1000), timeLimitCurve = Lin(20, 8), questionCountCurve = Lin(8, 15) }, typeB = new MathTypeConfig { mathTypeName = "Phân số", allowedOperators = new byte[] { 0, 1 }, minNumberCurve = Lin(1, 5), maxNumberCurve = Lin(5, 12), timeLimitCurve = Lin(25, 10), questionCountCurve = Lin(6, 12) }, typeC = new MathTypeConfig { mathTypeName = "Hỗn hợp nâng cao", allowedOperators = new byte[] { 0, 1, 2, 3 }, minNumberCurve = Lin(10, 200), maxNumberCurve = Lin(200, 1000), timeLimitCurve = Lin(20, 8), questionCountCurve = Lin(8, 18) } };
    private static GradeRule BuildGrade5() => new() { gradeName = "Lớp 5", typeA = new MathTypeConfig { mathTypeName = "Phân số nâng cao", allowedOperators = new byte[] { 0, 1, 2, 3 }, minNumberCurve = Lin(1, 10), maxNumberCurve = Lin(10, 20), timeLimitCurve = Lin(25, 10), questionCountCurve = Lin(8, 15) }, typeB = new MathTypeConfig { mathTypeName = "Thập phân", allowedOperators = new byte[] { 0, 1, 2, 3 }, minNumberCurve = Lin(1, 10), maxNumberCurve = Lin(10, 100), timeLimitCurve = Lin(25, 10), questionCountCurve = Lin(8, 15) }, typeC = new MathTypeConfig { mathTypeName = "Tổng hợp", allowedOperators = new byte[] { 0, 1, 2, 3 }, minNumberCurve = Lin(5, 100), maxNumberCurve = Lin(100, 1000), timeLimitCurve = Lin(30, 12), questionCountCurve = Lin(5, 10) } };
#endif
}