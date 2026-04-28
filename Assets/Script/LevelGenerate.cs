using System.Collections.Generic;
using UnityEngine;

// ──────────────────────────────────────────────
// Enums
// ──────────────────────────────────────────────

public enum MathCategory
{
    TypeA, // Dạng A
    TypeB, // Dạng B
    TypeC, // Dạng C (Hỗn hợp)
    TypeD  // Dạng D
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

    [Header("ĐIỀU CHỈNH XUẤT HIỆN")]
    [Tooltip("Trục X: 0->1 (Màn 1->100). Nếu giá trị > 0 tại điểm đó, dạng toán sẽ xuất hiện.")]
    public AnimationCurve unlockCurve;

    [Header("Đồ thị độ khó (X: 0→1 = màn 1→100)")]
    public AnimationCurve minNumberCurve;
    public AnimationCurve maxNumberCurve;
    public AnimationCurve timeLimitCurve;
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

    [Header("4 Dạng toán")]
    public MathTypeConfig typeA;
    public MathTypeConfig typeB;
    public MathTypeConfig typeC;
    public MathTypeConfig typeD;

    public MathTypeConfig GetMathConfig(MathCategory cat) => cat switch
    {
        MathCategory.TypeA => typeA,
        MathCategory.TypeB => typeB,
        MathCategory.TypeC => typeC,
        _ => typeD
    };
}

// ──────────────────────────────────────────────
// Kết quả trả về khi load màn
// ──────────────────────────────────────────────

public class LevelParameters
{
    public int GradeIndex;
    public int LevelIndex;
    public float Progress;
    public MathCategory MathType;
    public int MinNumber;
    public int MaxNumber;
    public float TimeLimit;
    public int QuestionCount;
    public byte[] AllowedOperators;
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

        // --- SỬA LOGIC KIỂM TRA ĐỒ THỊ XUẤT HIỆN ---
        List<MathCategory> unlockedCategories = new List<MathCategory>();

        // Chỉ cần giá trị Y trên đồ thị > 0.1 (để tránh sai số nhỏ) là cho phép xuất hiện
        if (rule.typeA.unlockCurve.Evaluate(t) > 0.1f) unlockedCategories.Add(MathCategory.TypeA);
        if (rule.typeB.unlockCurve.Evaluate(t) > 0.1f) unlockedCategories.Add(MathCategory.TypeB);
        if (rule.typeC.unlockCurve.Evaluate(t) > 0.1f) unlockedCategories.Add(MathCategory.TypeC);
        if (rule.typeD.unlockCurve.Evaluate(t) > 0.1f) unlockedCategories.Add(MathCategory.TypeD);

        // Nếu không có dạng nào được mở khóa, mặc định lấy TypeA
        MathCategory mathCat = (unlockedCategories.Count > 0)
            ? unlockedCategories[Random.Range(0, unlockedCategories.Count)]
            : MathCategory.TypeA;

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
            BuildGrade1(), BuildGrade2(), BuildGrade3(), BuildGrade4(), BuildGrade5(),
        };
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static AnimationCurve Lin(float y0, float y1) => AnimationCurve.Linear(0f, y0, 1f, y1);

    // Đồ thị mặc định: Luôn xuất hiện từ màn 1 (Y = 1 toàn bộ)
    private static AnimationCurve Always() => AnimationCurve.Linear(0f, 1f, 1f, 1f);

    private static GradeRule BuildGrade1() => new()
    {
        gradeName = "Lớp 1",

        // TypeA: Phép Cộng (Luôn có từ màn 1)
        typeA = new MathTypeConfig
        {
            mathTypeName = "Cộng",
            allowedOperators = new byte[] { 0 },
            unlockCurve = Always(),
            minNumberCurve = Lin(1, 10),
            maxNumberCurve = Lin(10, 50)
        },

        // TypeB: Phép Trừ (Mở khóa từ màn 10)
        typeB = new MathTypeConfig
        {
            mathTypeName = "Trừ",
            allowedOperators = new byte[] { 1 },
            unlockCurve = StartAt(0.1f),
            minNumberCurve = Lin(1, 10),
            maxNumberCurve = Lin(10, 50)
        },

        // TypeC: Tìm x Cộng/Trừ (Mở khóa từ màn 50)
        typeC = new MathTypeConfig
        {
            mathTypeName = "Tìm x +-",
            allowedOperators = new byte[] { 4 },
            unlockCurve = StartAt(0.5f),
            minNumberCurve = Lin(1, 5),
            maxNumberCurve = Lin(5, 20)
        },

        // TypeD: Chuỗi tính toán 3 số (Mở khóa từ màn 80)
        typeD = new MathTypeConfig
        {
            mathTypeName = "Chuỗi tính +-",
            allowedOperators = new byte[] { 7 },
            unlockCurve = StartAt(0.8f),
            minNumberCurve = Lin(5, 15),
            maxNumberCurve = Lin(15, 40)
        }
    };

    // Các hàm BuildGrade khác cũng cập nhật tương tự với unlockCurve = Always()
    private static GradeRule BuildGrade2() => new()
    {
        gradeName = "Lớp 2",

        // TypeA: Cộng/Trừ phạm vi 100 (Luôn có)
        typeA = new MathTypeConfig
        {
            mathTypeName = "Cộng/Trừ 100",
            allowedOperators = new byte[] { 0, 1, 7 },
            unlockCurve = Always(),
            minNumberCurve = Lin(10, 30),
            maxNumberCurve = Lin(30, 100)
        },

        // TypeB: Bảng Nhân 2-5 (Mở khóa từ màn 20)
        typeB = new MathTypeConfig
        {
            mathTypeName = "Nhân 2-5",
            allowedOperators = new byte[] { 2 },
            unlockCurve = StartAt(0.2f),
            minNumberCurve = Lin(1, 3),
            maxNumberCurve = Lin(3, 5)
        },

        // TypeC: Bảng Chia 2-5 (Mở khóa từ màn 50)
        typeC = new MathTypeConfig
        {
            mathTypeName = "Chia 2-5",
            allowedOperators = new byte[] { 3 },
            unlockCurve = StartAt(0.3f),
            minNumberCurve = Lin(1, 3),
            maxNumberCurve = Lin(3, 5)
        },

        // TypeD: Tìm x trong phép nhân (Mở khóa từ màn 80)
        typeD = new MathTypeConfig
        {
            mathTypeName = "Tìm x nhân",
            allowedOperators = new byte[] { 5 },
            unlockCurve = StartAt(0.5f),
            minNumberCurve = Lin(1, 3),
            maxNumberCurve = Lin(3, 5)
        }
    };
    private static GradeRule BuildGrade3() => new()
    {
        gradeName = "Lớp 3",

        // TypeA: Nhân/Chia nâng cao (Luôn có)
        typeA = new MathTypeConfig
        {
            mathTypeName = "Nhân/Chia 6-9",
            allowedOperators = new byte[] { 2, 3 },
            unlockCurve = StartAt(0.2f),
            minNumberCurve = Lin(1, 5),
            maxNumberCurve = Lin(5, 10)
        },

        // TypeB: Chuỗi tính toán lớn (Mở khóa từ màn 30)
        typeB = new MathTypeConfig
        {
            mathTypeName = "Chuỗi tính lớn + -",
            allowedOperators = new byte[] { 0,1,7 },
            unlockCurve = Always(),
            minNumberCurve = Lin(10, 100),
            maxNumberCurve = Lin(30, 500)
        },

        // TypeC: Tìm x trong phép chia (Mở khóa từ màn 60)
        typeC = new MathTypeConfig
        {
            mathTypeName = "Tìm x chia",
            allowedOperators = new byte[] { 6 },
            unlockCurve = StartAt(0.4f),
            minNumberCurve = Lin(1, 5),
            maxNumberCurve = Lin(5, 10)
        },

        // TypeD: Tìm x Cộng/Trừ số lớn (Mở khóa từ màn 80)
        typeD = new MathTypeConfig
        {
            mathTypeName = "Tìm x +- lớn",
            allowedOperators = new byte[] { 4 },
            unlockCurve = StartAt(0.3f),
            minNumberCurve = Lin(50, 100),
            maxNumberCurve = Lin(100, 500)
        }
    };
    private static GradeRule BuildGrade4() => new()
    {
        gradeName = "Lớp 4",

        // TypeA: Nhân/Chia số có nhiều chữ số (Luôn có)
        typeA = new MathTypeConfig
        {
            mathTypeName = "Nhân/Chia lớn",
            allowedOperators = new byte[] { 2, 3 },
            unlockCurve = StartAt(0.3f),
            minNumberCurve = Lin(10, 30),
            maxNumberCurve = Lin(30, 90)
        },

        // TypeB: Tìm x tổng hợp các dạng (Mở khóa từ màn 30)
        typeB = new MathTypeConfig
        {
            mathTypeName = "Tìm x tổng hợp",
            allowedOperators = new byte[] { 4, 5, 6 },
            unlockCurve = StartAt(0.4f),
            minNumberCurve = Lin(10, 20),
            maxNumberCurve = Lin(20, 80)
        },

        // TypeC: Chuỗi tính toán nâng cao (Mở khóa từ màn 70)
        typeC = new MathTypeConfig
        {
            mathTypeName = "Chuỗi tính nâng cao",
            allowedOperators = new byte[] { 7 },
            unlockCurve = StartAt(0.2f),
            minNumberCurve = Lin(10, 100),
            maxNumberCurve = Lin(100, 500)
        },

        // TypeD: Cộng/Trừ số lớn (Củng cố kiến thức)
        typeD = new MathTypeConfig
        {
            mathTypeName = "Cộng/Trừ lớn",
            allowedOperators = new byte[] { 0, 1 },
            unlockCurve = Always(),
            minNumberCurve = Lin(10, 500),
            maxNumberCurve = Lin(30, 2000)
        }
    };
    private static GradeRule BuildGrade5() => new()
    {
        gradeName = "Lớp 5",

        // TypeA: Nhân/Chia/Tìm x nâng cao (Luôn có)
        typeA = new MathTypeConfig
        {
            mathTypeName = "Nhân/Chia/Tìm x",
            allowedOperators = new byte[] { 2, 3, 5, 6 },
            unlockCurve = StartAt(0.1f),
            minNumberCurve = Lin(2, 50),
            maxNumberCurve = Lin(10, 100)
        },

        // TypeB: Biểu thức 3 số phức tạp (Mở khóa từ màn 40)
        typeB = new MathTypeConfig
        {
            mathTypeName = "Biểu thức 3 số",
            allowedOperators = new byte[] { 7 },
            unlockCurve = StartAt(0.3f),
            minNumberCurve = Lin(10, 500),
            maxNumberCurve = Lin(100, 1000)
        },

        // TypeC: Cộng/Trừ phạm vi cực lớn (Củng cố)
        typeC = new MathTypeConfig
        {
            mathTypeName = "Cộng/Trừ nâng cao",
            allowedOperators = new byte[] { 0, 1 },
            unlockCurve = Always(),
            minNumberCurve = Lin(10, 1000),
            maxNumberCurve = Lin(30, 5000)
        },

        // TypeD: Tìm x Cộng/Trừ số lớn (Mở khóa từ màn 70)
        typeD = new MathTypeConfig
        {
            mathTypeName = "Tìm x cộng trừ",
            allowedOperators = new byte[] { 4 },
            unlockCurve = StartAt(0.5f),
            minNumberCurve = Lin(10, 500),
            maxNumberCurve = Lin(30, 1000)
        }
    };
    private static AnimationCurve StartAt(float startTime)
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(startTime - 0.01f, 0f),
            new Keyframe(startTime, 1f),
            new Keyframe(1f, 1f)
        );
    }
#endif
}