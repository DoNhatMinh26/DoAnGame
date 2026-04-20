using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestionZone : MonoBehaviour
{
    [Header("UI nội bộ (Chỉ còn các cổng)")]
    public TextMeshProUGUI[] gateTexts;

    [HideInInspector] public string cauHoiLuuTru;
    [HideInInspector] public string dapAnDung;

    public void Setup(string cauHoi, List<string> choices, string correct)
    {
        this.cauHoiLuuTru = cauHoi;
        this.dapAnDung = correct;

        for (int i = 0; i < choices.Count; i++)
        {
            string temp = choices[i];
            int r = Random.Range(i, choices.Count);
            choices[i] = choices[r];
            choices[r] = temp;
        }

        for (int i = 0; i < gateTexts.Length; i++)
        {
            if (i < gateTexts.Length && i < choices.Count)
            {
                gateTexts[i].text = choices[i];
                gateTexts[i].color = Color.black;
            }
        }
    }
}