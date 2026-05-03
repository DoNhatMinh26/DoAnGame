using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private LevelGenerate levelData;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI cauHoiText;
    [SerializeField] private TextMeshProUGUI manHienTaiText;
    [SerializeField] private Button[] dapAnBnt;
    [Header("Ô Trống Hiển Thị")]
    [SerializeField] private RectTransform oTrongRect; // Kéo cụm Ô Trống (Image) vào đây
    [SerializeField] private Image oTrongImage;
    [SerializeField] private TextMeshProUGUI oTrongText;

    private int minVal, maxVal, dapAnDung;
    private List<string> activeOps = new List<string>();

    public void UpdateDifficulty()
    {
        if (levelData == null) return;

        // Lấy dữ liệu dựa trên Lớp và Màn hiện tại
        var config = levelData.GetConfigForLevel(UIManager.SelectedGrade, LevelManager.CurrentLevel);

        if (config != null)
        {
            if (UiClass.Instance != null)
            {
                UiClass.Instance.SetupLevelDifficulty(UIManager.SelectedGrade, LevelManager.CurrentLevel);
            }
            if (manHienTaiText != null) manHienTaiText.text = "Màn " + config.LevelIndex;

            minVal = config.MinNumber;
            maxVal = config.MaxNumber;

            activeOps.Clear();
            // Đồng bộ 8 dạng toán theo ảnh image_1dd911.png
            foreach (byte op in config.AllowedOperators)
            {
                if (op == 0) activeOps.Add("+");
                if (op == 1) activeOps.Add("-");
                if (op == 2) activeOps.Add("x");
                if (op == 3) activeOps.Add(":");
                if (op == 4) activeOps.Add("find_+-");
                if (op == 5) activeOps.Add("find x");
                if (op == 6) activeOps.Add("find :");
                if (op == 7) activeOps.Add("hai phép tính +-");
            }

            TaoCauHoi();
        }
    }

    public void TaoCauHoi()
    {
        StopAllCoroutines();
        SetButtonsInteractable(true);
        ResetColorButtons();

        if (activeOps.Count == 0) activeOps.Add("+");
        string phepToan = activeOps[Random.Range(0, activeOps.Count)];
        // Reset ô trống
        if (oTrongText != null) oTrongText.text = "";
        if (oTrongImage != null) oTrongImage.color = Color.white;
        int n1, n2, n3;

        switch (phepToan)
        {
            case "+":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} =  ?"; break;

            case "-":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                dapAnDung = n1 - n2; cauHoiText.text = $"{n1} - {n2} =  ?"; break;

            case "x":
                if (UIManager.SelectedGrade >= 4)
                {
                    n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1);
                }
                else
                {
                    n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(2, 10);
                }
                dapAnDung = n1 * n2; cauHoiText.text = $"{n1} x {n2} =  ?"; break;

            case ":":
                int sc, kq;
                if (UIManager.SelectedGrade >= 4)
                {
                    sc = Random.Range(minVal, maxVal + 1); kq = Random.Range(minVal, maxVal + 1);
                    if (sc == 0) sc = 1;
                }
                else
                {
                    kq = Random.Range(2, 10); sc = Random.Range(minVal, maxVal + 1);
                }
                dapAnDung = kq; cauHoiText.text = $"{sc * kq} : {sc} =  ?"; break;

            case "find_+-":
                int x = Random.Range(minVal, maxVal + 1);
                int b = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                { // Cộng
                    int tong = x + b; dapAnDung = x;
                    cauHoiText.text = (Random.value > 0.5f) ? $"?  + {b} = {tong}" : $"{b} +  ?  = {tong}";
                }
                else
                { // Trừ
                    dapAnDung = x;
                    if (Random.value > 0.5f)
                    {
                        int hieu = x - b; if (x < b) { x = b; b = x - hieu; }
                        cauHoiText.text = $"?  - {b} = {hieu}";
                    }
                    else
                    {
                        int a = x + b; cauHoiText.text = $"{a} -  ?  = {b}";
                    }
                }
                break;

            case "find x": // Tìm x trong phép nhân
                int vX = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int vB = Random.Range(minVal, maxVal + 1);
                int tich = vX * vB; dapAnDung = vX;
                cauHoiText.text = (Random.value > 0.5f) ? $"?  x {vB} = {tich}" : $"{vB} x  ?  = {tich}";
                break;

            case "find :": // Tìm x trong phép chia
                int vXc = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int vBc = Random.Range(minVal, maxVal + 1);
                if (vBc == 0) vBc = 1;
                int sbc = vXc * vBc;
                if (Random.value > 0.5f) { dapAnDung = sbc; cauHoiText.text = $"?  : {vBc} = {vXc}"; }
                else { dapAnDung = vBc; cauHoiText.text = $"{sbc} :  ?  = {vXc}"; }
                break;

            case "hai phép tính +-": // Dạng tính 3 số
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                n3 = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                {
                    dapAnDung = n1 + n2 - n3; cauHoiText.text = $"{n1} + {n2} - {n3} =  ?";
                    if (dapAnDung < 0) { dapAnDung = n1 + n2 + n3; cauHoiText.text = $"{n1} + {n2} + {n3} =  ?"; }
                }
                else
                {
                    if (n1 < n2) n1 = n2 + Random.Range(1, 10);
                    dapAnDung = n1 - n2 + n3; cauHoiText.text = $"{n1} - {n2} + {n3} =  ?";
                }
                break;

            default:
                n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} =  ?"; break;
        }
        StartCoroutine(UpdateOTrongPosition());
        GenerateChoices();
    }

    private void GenerateChoices()
    {
        List<int> choices = new List<int> { dapAnDung };

        // Đồng bộ sai số biến động theo khối lớp
        int gradeFactor = UIManager.SelectedGrade;
        int maxOffset = gradeFactor * 5;

        while (choices.Count < dapAnBnt.Length)
        {
            int offset = Random.Range(-maxOffset, maxOffset + 1);
            if (offset == 0) offset = Random.value > 0.5f ? 1 : -1;

            int wrong = Mathf.Abs(dapAnDung + offset);
            if (!choices.Contains(wrong)) choices.Add(wrong);
        }

        ShuffleList(choices);
        for (int i = 0; i < dapAnBnt.Length; i++)
        {
            dapAnBnt[i].GetComponentInChildren<TextMeshProUGUI>().text = choices[i].ToString();
            dapAnBnt[i].onClick.RemoveAllListeners();
            int val = choices[i];
            dapAnBnt[i].onClick.AddListener(() => CheckDapAn(val));
        }
    }

    IEnumerator UpdateOTrongPosition()
    {
        yield return new WaitForEndOfFrame();

        // Tìm vị trí ký tự '?'[cite: 5]
        int charIndex = cauHoiText.text.IndexOf('?');

        if (charIndex != -1 && cauHoiText.textInfo.characterCount > charIndex)
        {
            TMP_CharacterInfo charInfo = cauHoiText.textInfo.characterInfo[charIndex];
            // Lấy tâm của ký tự để ô trống đè lên chuẩn nhất
            Vector3 charCenterLocal = (charInfo.bottomLeft + charInfo.topRight) / 2f;
            Vector3 worldPos = cauHoiText.transform.TransformPoint(charCenterLocal);
            oTrongRect.position = worldPos;
        }
    }

    private void CheckDapAn(int val)
    {
        SetButtonsInteractable(false);
        if (oTrongText != null) oTrongText.text = val.ToString();

        if (val == dapAnDung)
        {
            if (oTrongImage != null) oTrongImage.color = Color.green;
            if (UiClass.Instance != null)
            {
                UiClass.Instance.AddCoins(5);
                UiClass.Instance.OnCorrectAnswer();
            }
            StartCoroutine(ActionAfterDelay(1f, true));
        }
        else
        {
            if (oTrongImage != null) oTrongImage.color = Color.red;
            StartCoroutine(ActionAfterDelay(0.5f, false));
        }
    }

    IEnumerator ActionAfterDelay(float d, bool win)
    {
        yield return new WaitForSeconds(d);
        if (win) TaoCauHoi();
        else
        {
            if (oTrongText != null) oTrongText.text = "";
            if (oTrongImage != null) oTrongImage.color = Color.white;
            ResetColorButtons();
            SetButtonsInteractable(true);
        }
    }
    private void HighlightButton(string label, Color c)
    {
        foreach (var b in dapAnBnt)
            if (b.GetComponentInChildren<TextMeshProUGUI>().text == label) b.image.color = c;
    }

    private void ResetColorButtons() { foreach (var b in dapAnBnt) b.image.color = Color.white; }
    private void SetButtonsInteractable(bool s) { foreach (var b in dapAnBnt) b.interactable = s; }
    private void ShuffleList(List<int> l) { for (int i = 0; i < l.Count; i++) { int t = l[i]; int r = Random.Range(i, l.Count); l[i] = l[r]; l[r] = t; } }
}