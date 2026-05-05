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
    private Canvas canvas;
    private Color originalColor;
    private DragQuizManager qm;

    // QUAN TRỌNG: Dùng biến này để lưu vị trí chuẩn lúc thiết kế
    private Vector2 originalPosition;

    private static bool isLocked = false;

    private Coroutine punishmentCoroutine;

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

        // Lưu vị trí gốc ngay khi vừa khởi động để tránh lỗi reset về trung tâm
        originalPosition = rectTransform.anchoredPosition;
    }

    // Tự động mở khóa khi màn hình Gameplay được kích hoạt lại
    private void OnEnable()
    {
        isLocked = false;
        if (image != null) image.color = originalColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked)
        {
            eventData.pointerDrag = null;
            return;
        }

        StopAllCoroutines();
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
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.AddScore(10); // Cộng 5 điểm ngay lập tức vào Profile
                }
                rectTransform.anchoredPosition = droppedOn.GetComponent<RectTransform>().anchoredPosition;
                StartCoroutine(ResetQuestionAfterDelay());
            }
            else
            {
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

    private void ApplyGlobalWrongEffect()
    {
        DragAndDrop[] allChoices = FindObjectsOfType<DragAndDrop>();
        foreach (DragAndDrop choice in allChoices)
        {
            // Nếu đang có hình phạt cũ thì dừng lại để bắt đầu cái mới
            if (choice.punishmentCoroutine != null) choice.StopCoroutine(choice.punishmentCoroutine);
            choice.punishmentCoroutine = choice.StartCoroutine(choice.LockAndColorRoutine());
        }
    }

    IEnumerator LockAndColorRoutine()
    {
        isLocked = true;
        image.color = colorWrong;

        float elapsed = 0f;
        // Vòng lặp này sẽ tự "đứng im" khi Time.timeScale = 0 (do DeltaTime = 0)
        while (elapsed < thoiGianKhoa)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        image.color = originalColor;

        // Chỉ mở khóa khi thực sự không còn menu nào che khuất
        if (GameUIManager.Instance != null &&
            !GameUIManager.Instance.panelSetting.activeSelf &&
            !GameUIManager.Instance.panelWin.activeSelf &&
            !GameUIManager.Instance.panelLose.activeSelf)
        {
            isLocked = false;
        }
    }

    IEnumerator SmoothReturn()
    {
        float time = 0;
        Vector2 currentPos = rectTransform.anchoredPosition;
        while (time < 0.2f)
        {
            // Trả về vị trí original chuẩn xác
            rectTransform.anchoredPosition = Vector2.Lerp(currentPos, originalPosition, time / 0.2f);
            time += Time.deltaTime;
            yield return null;
        }
        rectTransform.anchoredPosition = originalPosition;
    }

    IEnumerator ResetQuestionAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);
        image.color = originalColor;
        rectTransform.anchoredPosition = originalPosition;
        qm.UpdateDifficulty();
        isLocked = false;
    }

    public static void ReleaseAllLocks()
    {
        isLocked = false;
    }

    public void ForceResetPosition()
    {
        StopAllCoroutines();
        isLocked = false;
        if (image != null) image.color = originalColor;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Luôn trả về vị trí original dù có chuyện gì xảy ra
        rectTransform.anchoredPosition = originalPosition;
    }
    public static void SetGlobalLock(bool locked)
    {
        isLocked = locked;
    }
}