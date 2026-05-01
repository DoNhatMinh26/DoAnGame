using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestionZone : MonoBehaviour
{
    [Header("UI nội bộ (Chỉ còn các cổng)")]
    public TextMeshProUGUI[] gateTexts;

    [HideInInspector] public string cauHoiLuuTru;
    [HideInInspector] public string dapAnDung;

    [SerializeField] private CanvasGroup canvasGroup;
    private Coroutine currentFadeCoroutine; // Biến lưu Coroutine hiện tại

    public void Setup(string cauHoi, List<string> choices, string correct)
    {
        // 1. DỪNG NGAY Coroutine cũ nếu nó vẫn đang chạy ngầm
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }
        
        // 2. ÉP Alpha về 1 và bật lại tương tác
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        this.cauHoiLuuTru = cauHoi;
        this.dapAnDung = correct;

        // Logic xáo trộn đáp án
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
                gateTexts[i].color = Color.white;
            }
        }
    }

    public void FadeOut(float duration)
    {
        // Dừng cái cũ trước khi bắt đầu cái mới để tránh xung đột
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeRoutine(duration));
    }

    private IEnumerator FadeRoutine(float duration)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, currentTime / duration);
            yield return null;
        }
        currentFadeCoroutine = null; // Kết thúc Coroutine
    }
}