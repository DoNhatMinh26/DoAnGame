using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DragQuizManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private TextAsset gameDataJson;
    private GameDataContainer gameData;

    [Header("UI References")]
    public TextMeshProUGUI cauHoiText;
    public TextMeshProUGUI manHienTaiText; // Hiển thị số màn chơi
    public TextMeshProUGUI[] answerTexts; // Mảng các text trên các vật kéo thả

    private int min, max, dapAnDung;
    private List<string> activeOps = new List<string>();

    void Awake()
    {
        if (gameDataJson != null)
            gameData = JsonUtility.FromJson<GameDataContainer>(gameDataJson.text);
    }

    public void UpdateDifficultyFromJSON()
    {
        if (gameData == null) return;

        var grade = gameData.grades.Find(g => g.gradeID == MenuManager.SelectedGrade);
        var mode = grade?.gameModes.Find(m => m.modeName == MenuManager.SelectedMode);
        var config = mode?.levels.Find(l => l.levelID == LevelManager.CurrentLevel);

        if (config != null)
        {
            // Cập nhật text số màn chơi
            if (manHienTaiText != null) manHienTaiText.text = "Màn " + config.levelID;

            min = config.minVal;
            max = config.maxVal;
            activeOps = new List<string>(config.allowOps);

            SetRandomQuestion();
        }
    }

    public void SetRandomQuestion()
    {
        if (activeOps.Count == 0) activeOps.Add("+");
        string op = activeOps[Random.Range(0, activeOps.Count)];

        int n1, n2, n3;

        switch (op)
        {
            case "+":
                n1 = Random.Range(min, max); n2 = Random.Range(min, max);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} = ?"; break;

            case "-":
                n1 = Random.Range(min, max); n2 = Random.Range(min, max);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                dapAnDung = n1 - n2; cauHoiText.text = $"{n1} - {n2} = ?"; break;

            case "x":
                // Giới hạn số nhân để không quá khó khi kéo thả
                n1 = Random.Range(min, Mathf.Max(min + 1, max / 2));
                n2 = Random.Range(2, 10);
                dapAnDung = n1 * n2; cauHoiText.text = $"{n1} x {n2} = ?"; break;

            case ":":
                int kq = Random.Range(min, max);
                int sc = Random.Range(2, 10);
                dapAnDung = kq; cauHoiText.text = $"{sc * kq} : {sc} = ?"; break;

            case "2": // Phép tính hỗn hợp
                n1 = Random.Range(min, max); n2 = Random.Range(min, max); n3 = Random.Range(min, max);
                dapAnDung = n1 + n2 - n3; cauHoiText.text = $"{n1} + {n2} - {n3} = ?"; break;

            case "find_x": // Tìm X
                int x = Random.Range(min, max);
                int v2 = Random.Range(min, max);
                int tong = x + v2;
                dapAnDung = x; cauHoiText.text = $"? + {v2} = {tong}"; break;

            case "bracket": // Biểu thức ngoặc
                n1 = Random.Range(min, max); n2 = Random.Range(min, max); n3 = Random.Range(2, 6);
                dapAnDung = (n1 + n2) * n3; cauHoiText.text = $"({n1} + {n2}) x {n3} = ?"; break;

            default:
                n1 = Random.Range(min, max); n2 = Random.Range(min, max);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} = ?"; break;
        }

        GenerateChoices();
    }

    private void GenerateChoices()
    {
        List<int> choices = new List<int> { dapAnDung };

        int safety = 0;
        while (choices.Count < answerTexts.Length && safety < 100)
        {
            safety++;
            int offset = Random.Range(-5, 6);
            if (offset == 0) offset = 1;
            int wrong = Mathf.Abs(dapAnDung + offset);

            if (!choices.Contains(wrong)) choices.Add(wrong);
        }

        // Trộn danh sách đáp án
        for (int i = 0; i < choices.Count; i++)
        {
            int temp = choices[i];
            int r = Random.Range(i, choices.Count);
            choices[i] = choices[r];
            choices[r] = temp;
        }

        // Gán vào UI
        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < choices.Count)
                answerTexts[i].text = choices[i].ToString();
        }
    }

    public int GetCurrentCorrectAnswer() => dapAnDung;
}