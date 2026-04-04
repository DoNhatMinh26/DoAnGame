using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathManager : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Systems")]
    public HealthSystem Health;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI cauHoiText;
    [SerializeField] private Button[] dapAnBnt;

    [Header("Settings - Addition (+)")]
    [SerializeField] private bool phepCong = true;
    [SerializeField] private int CTu = 1, CDen = 20;

    [Header("Settings - Subtraction (-)")]
    [SerializeField] private bool phepTru = true;
    [SerializeField] private int TTu = 1, TDen = 20;

    [Header("Settings - Multiplication (x)")]
    [SerializeField] private bool phepNhan = false;
    [SerializeField] private int NTu = 1, NDen = 10;

    [Header("Settings - Division (:)")]
    [SerializeField] private bool phepChia = false;
    [SerializeField] private int CCTu = 1, CCDen = 10;

    [Header("Settings - Double Operators (2)")]
    [SerializeField] private bool haiPhepTinh = false;
    [SerializeField] private bool congHaiPhep = true;
    [SerializeField] private bool truHaiPhep = true;
    [SerializeField] private int Tu = 1, Den = 10;

    [Header("Wrong Answer Deviation")]
    [SerializeField] private int SaiTu = -5;
    [SerializeField] private int SaiDen = 5;

    private int dapAnDung;

    void Start()
    {
        TaoCauHoi();
    }

    public void TaoCauHoi()
    {
        // 1. Reset UI
        StopAllCoroutines();
        SetButtonsInteractable(true);
        ResetColorButtons();

        // 2. Chọn phép tính ngẫu nhiên từ danh sách được bật
        List<string> optionsPhepToan = new List<string>();
        if (phepCong) optionsPhepToan.Add("+");
        if (phepTru) optionsPhepToan.Add("-");
        if (phepNhan) optionsPhepToan.Add("x");
        if (phepChia) optionsPhepToan.Add(":");
        if (haiPhepTinh) optionsPhepToan.Add("2");

        if (optionsPhepToan.Count == 0) optionsPhepToan.Add("+");
        string phepToanHienTai = optionsPhepToan[Random.Range(0, optionsPhepToan.Count)];

        int n1, n2, n3;

        // 3. Logic tạo câu hỏi
        switch (phepToanHienTai)
        {
            case "+":
                n1 = Random.Range(CTu, CDen);
                n2 = Random.Range(CTu, CDen);
                dapAnDung = n1 + n2;
                cauHoiText.text = $"{n1} + {n2} = ?";
                break;
            case "-":
                n1 = Random.Range(TTu, TDen);
                n2 = Random.Range(TTu, TDen);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                dapAnDung = n1 - n2;
                cauHoiText.text = $"{n1} - {n2} = ?";
                break;
            case "x":
                n1 = Random.Range(NTu, NDen);
                n2 = Random.Range(NTu, NDen);
                dapAnDung = n1 * n2;
                cauHoiText.text = $"{n1} x {n2} = ?";
                break;
            case ":":
                int ketQua = Random.Range(CCTu, CCDen);
                int soChia = Random.Range(CCTu, CCDen);
                dapAnDung = ketQua;
                cauHoiText.text = $"{soChia * ketQua} : {soChia} = ?";
                break;
            case "2":
                n1 = Random.Range(Tu, Den);
                n2 = Random.Range(Tu, Den);
                n3 = Random.Range(Tu, Den);
                // 1. Lấy danh sách phép tính khả dụng
                List<string> optionsHaiPhep = new List<string>();
                if (congHaiPhep) optionsHaiPhep.Add("+");
                if (truHaiPhep) optionsHaiPhep.Add("-");
                //if (nhanHaiPhep) optionsHaiPhep.Add("x");
                //if (chiaHaiPhep) optionsHaiPhep.Add(":");
                string dau1 = optionsHaiPhep[Random.Range(0, optionsHaiPhep.Count)];
                string dau2 = optionsHaiPhep[Random.Range(0, optionsHaiPhep.Count)];
                // 2. Tính toán kết quả trung gian và kết quả cuối
                int ketQuaTrungGian = 0;
                // Tính bước 1: number1 [dau1] number2
                if (dau1 == "+") ketQuaTrungGian = n1 + n2;
                else
                {
                    // Đảm bảo kết quả trung gian không âm (nếu làm game cho trẻ em)
                    if (n1 < n2) { int tem = n1; n1 = n2; n2 = tem; }
                    ketQuaTrungGian = n1 - n2;
                }
                // Tính bước 2: ketQuaTrungGian [dau2] number3
                if (dau2 == "+") dapAnDung = ketQuaTrungGian + n3;
                else
                {
                    // Đảm bảo không ra đáp án âm
                    if (ketQuaTrungGian < n3) n3 = Random.Range(0, ketQuaTrungGian);
                    dapAnDung = ketQuaTrungGian - n3;
                }
                // 3. Hiển thị câu hỏi lên UI
                cauHoiText.text = $"{n1} {dau1} {n2} {dau2} {n3} = ";
                break;
        }

        // 4. Tạo danh sách đáp án không trùng lặp
        List<int> choices = new List<int>();
        choices.Add(dapAnDung);

        int safety = 0;
        while (choices.Count < dapAnBnt.Length && safety < 100)
        {
            safety++;
            int wrong = Mathf.Abs(dapAnDung + Random.Range(SaiTu, SaiDen + 1));
            if (!choices.Contains(wrong)) choices.Add(wrong);
        }

        // 5. Trộn và gán vào UI
        ShuffleList(choices);
        for (int i = 0; i < dapAnBnt.Length; i++)
        {
            dapAnBnt[i].GetComponentInChildren<TextMeshProUGUI>().text = choices[i].ToString();
            dapAnBnt[i].onClick.RemoveAllListeners();
            int selectedValue = choices[i];
            dapAnBnt[i].onClick.AddListener(() => CheckDapAn(selectedValue));
        }
    }

    private void CheckDapAn(int selectedValue)
    {
        SetButtonsInteractable(false);

        if (selectedValue == dapAnDung)
        {
            Debug.Log("Correct!");
            animator.SetTrigger("Win");
            Health.TakeDamageEnemy(20f);
            HighlightButton(selectedValue, Color.green);
            StartCoroutine(ActionAfterDelay(1.0f, true));
        }
        else
        {
            Debug.Log("Wrong!");
            animator.SetTrigger("Lose");
            Health.TakeDamagePlayer(20f);
            HighlightButton(selectedValue, Color.red);
            StartCoroutine(ActionAfterDelay(0.5f, false));
        }
    }

    IEnumerator ActionAfterDelay(float delay, bool isCorrect)
    {
        yield return new WaitForSeconds(delay);
        if (isCorrect)
        {
            TaoCauHoi();
        }
        else
        {
            ResetColorButtons();
            SetButtonsInteractable(true);
        }
    }

    private void HighlightButton(int value, Color color)
    {
        foreach (var bnt in dapAnBnt)
        {
            if (bnt.GetComponentInChildren<TextMeshProUGUI>().text == value.ToString())
            {
                bnt.image.color = color;
            }
        }
    }

    private void ResetColorButtons()
    {
        foreach (var bnt in dapAnBnt) bnt.image.color = Color.white;
    }

    private void SetButtonsInteractable(bool state)
    {
        foreach (var bnt in dapAnBnt) bnt.interactable = state;
    }

    private void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i];
            int r = Random.Range(i, list.Count);
            list[i] = list[r];
            list[r] = temp;
        }
    }
}