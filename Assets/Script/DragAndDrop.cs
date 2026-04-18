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
    private DragQuizManager qm; // Cache lại Manager

    public TextMeshProUGUI myText;

    [Header("Cài đặt màu sắc")]
    public Color colorCorrect = Color.green;
    public Color colorWrong = Color.red;
    [SerializeField] private float thoiGianDoiMau = 0.5f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        originalColor = image.color;

        // Tìm Manager ngay từ đầu để tiết kiệm tài nguyên
        qm = FindObjectOfType<DragQuizManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        StopAllCoroutines(); // Dừng mọi hiệu ứng đổi màu/di chuyển đang chạy

        startPosition = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        image.color = originalColor;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        GameObject droppedOn = eventData.pointerEnter;

        // Kiểm tra nếu thả trúng ô đích
        if (droppedOn != null && droppedOn.CompareTag("Slot"))
        {
            // SỬA LỖI: Chuyển sang float để nhận dữ liệu từ qm.GetCurrentCorrectAnswer()
            float correctAnswer = qm.GetCurrentCorrectAnswer();

            // SỬA LỖI: Dùng float.TryParse và Mathf.Abs để so sánh số thực (tránh sai số float)
            if (float.TryParse(myText.text, out float myValue) && Mathf.Abs(myValue - correctAnswer) < 0.01f)
            {
                // ĐÚNG
                image.color = colorCorrect;
                rectTransform.anchoredPosition = droppedOn.GetComponent<RectTransform>().anchoredPosition;
                StartCoroutine(ResetQuestionAfterDelay());
            }
            else
            {
                // SAI
                image.color = colorWrong;
                StartCoroutine(SmoothReturn()); // Trượt về vị trí cũ
                StartCoroutine(WaitAndRestoreColor(thoiGianDoiMau));
            }
        }
        else
        {
            // Thả trượt ra ngoài
            StartCoroutine(SmoothReturn());
            image.color = originalColor;
        }
    }

    // Hiệu ứng trượt về vị trí cũ mượt mà hơn
    IEnumerator SmoothReturn()
    {
        float time = 0;
        Vector2 currentPos = rectTransform.anchoredPosition;
        while (time < 0.2f) // Trượt về trong 0.2 giây
        {
            rectTransform.anchoredPosition = Vector2.Lerp(currentPos, startPosition, time / 0.2f);
            time += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = startPosition;
    }

    IEnumerator WaitAndRestoreColor(float delay)
    {
        yield return new WaitForSeconds(delay);
        image.color = originalColor;
    }

    IEnumerator ResetQuestionAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        image.color = originalColor;
        rectTransform.anchoredPosition = startPosition;

        // Cập nhật câu hỏi mới dựa trên cấu hình độ khó hiện tại
        qm.UpdateDifficulty();
    }
}