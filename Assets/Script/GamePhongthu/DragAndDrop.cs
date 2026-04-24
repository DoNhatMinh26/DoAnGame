using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class DragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image image;
    private Vector2 startPosition;
    private Canvas canvas;
    private Color originalColor;
    private DragQuizManager qm;

    private static bool isLocked = false; // Khóa dùng chung cho tất cả các ô

    public TextMeshProUGUI myText;

    [Header("Cài đặt màu sắc")]
    public Color colorCorrect = Color.green;
    public Color colorWrong = Color.red;
    [SerializeField] private float thoiGianKhoa = 3.0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        originalColor = image.color;
        qm = FindObjectOfType<DragQuizManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked)
        {
            eventData.pointerDrag = null;
            return;
        }

        StopAllCoroutines();
        startPosition = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        image.color = originalColor;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        GameObject droppedOn = eventData.pointerEnter;

        if (droppedOn != null && droppedOn.CompareTag("Slot"))
        {
            float correctAnswer = qm.GetCurrentCorrectAnswer();

            if (float.TryParse(myText.text, out float myValue) && Mathf.Abs(myValue - correctAnswer) < 0.01f)
            {
                // ĐÚNG
                image.color = colorCorrect;
                if (CannonDefenseManager.Instance != null)
                {
                    CannonDefenseManager.Instance.FireAtClosestEnemy();
                }
                rectTransform.anchoredPosition = droppedOn.GetComponent<RectTransform>().anchoredPosition;
                StartCoroutine(ResetQuestionAfterDelay());
            }
            else
            {
                // SAI -> Gọi hàm đổi màu toàn bộ các ô đáp án
                ApplyGlobalWrongEffect();
                StartCoroutine(SmoothReturn());
            }
        }
        else
        {
            StartCoroutine(SmoothReturn());
            image.color = originalColor;
        }
    }

    // Hàm tĩnh để tất cả các ô cùng đổi màu đỏ và bị khóa
    private void ApplyGlobalWrongEffect()
    {
        DragAndDrop[] allChoices = FindObjectsOfType<DragAndDrop>();
        foreach (DragAndDrop choice in allChoices)
        {
            choice.StartCoroutine(choice.LockAndColorRoutine());
        }
    }

    IEnumerator LockAndColorRoutine()
    {
        isLocked = true;
        image.color = colorWrong; // Cả bảng phía sau sẽ đỏ lên

        yield return new WaitForSeconds(thoiGianKhoa);

        image.color = originalColor; // Trả lại màu cũ sau khi hết phạt
        isLocked = false;
    }

    IEnumerator SmoothReturn()
    {
        float time = 0;
        Vector2 currentPos = rectTransform.anchoredPosition;
        while (time < 0.2f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(currentPos, startPosition, time / 0.2f);
            time += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = startPosition;
    }

    IEnumerator ResetQuestionAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        image.color = originalColor;
        rectTransform.anchoredPosition = startPosition;
        qm.UpdateDifficulty();
        isLocked = false;
    }
}