using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestionZone : MonoBehaviour
{
    public TextMeshProUGUI cauHoiText;
    public TextMeshProUGUI[] gateTexts;

    // Lưu đáp án đúng dưới dạng chuỗi để so sánh chính xác mọi loại số
    [HideInInspector] public string dapAnDung;

    public void Setup(string cauHoi, List<string> choices, string correct)
    {
        cauHoiText.text = cauHoi;
        dapAnDung = correct;

        // Trộn ngẫu nhiên vị trí các đáp án
        for (int i = 0; i < choices.Count; i++)
        {
            string temp = choices[i];
            int r = Random.Range(i, choices.Count);
            choices[i] = choices[r];
            choices[r] = temp;
        }

        // Đổ dữ liệu chữ vào các cổng
        for (int i = 0; i < gateTexts.Length; i++)
        {
            if (i < gateTexts.Length && i < choices.Count)
            {
                gateTexts[i].text = choices[i];
            }
        }
    }
}