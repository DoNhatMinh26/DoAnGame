using UnityEngine;
using TMPro;
using System.Collections;

public class SpaceShipPhysics : MonoBehaviour
{
    [Header("Cài đặt di chuyển bằng Chuột")]
    public float followSpeed = 10f;
    public float minY = -6f, maxY = 3f;

    [Header("Cấu hình Hút Tuyệt Đối")]
    public float magnetDistance = 3f;
    public float magnetStrength = 15f;
    public float lockThresholdX = 6f;
    public string gateTag = "Gate";

    private bool canMove = true;
    private bool isLockedByMagnet = false;

    private GameObject lastHitGate;
    private Rigidbody2D rb;
    private Vector3 startPosition;
    private Vector3 originalScale; // Kính thước gốc thực tế của phi thuyền

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Lưu lại kích thước chỉnh ở Inspector (ví dụ: 0.3) để dùng xuyên suốt game
        originalScale = transform.localScale;
        // Lưu vị trí gốc ngay từ Awake để tránh case ResetPosition được gọi
        // trước Start khi GameObject ban đầu đang inactive.
        startPosition = transform.position;
    }

    public void ResetPosition()
    {
        // SỬA: Trả về kích thước gốc đã lưu thay vì ép về 1
        transform.localScale = originalScale;

        // Luôn về đúng vị trí gốc đã đặt cho Player_Holder trong scene.
        transform.position = startPosition;

        // Đưa vận tốc về 0 để không bị trôi tiếp
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 1f;
        }
    }

    void Update()
    {
        if (canMove)
        {
            ApplyMagnetEffect();
        }

        if (canMove && !isLockedByMagnet)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float targetY = Mathf.Clamp(mousePos.y, minY, maxY);
            float newY = Mathf.MoveTowards(transform.position.y, targetY, followSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, 0);
        }
    }

    private IEnumerator HitGateScaleEffect(float duration)
    {
        // SỬA: Thu nhỏ dựa trên kích thước originalScale thực tế (ví dụ: 0.3 * 0.5 = 0.15)
        Vector3 shrunkScale = originalScale * 0.5f;

        float elapsed = 0f;

        // Giai đoạn 1: Thu nhỏ lại
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, shrunkScale, elapsed / (duration / 2));
            yield return null;
        }

        elapsed = 0f;

        // Giai đoạn 2: Trở lại bình thường
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(shrunkScale, originalScale, elapsed / (duration / 2));
            yield return null;
        }

        transform.localScale = originalScale; // Đảm bảo trả về đúng kích thước gốc ban đầu
    }

    public void BoostForwardOnWin()
    {
        if (rb != null)
        {
            rb.velocity = new Vector2(5f, 0f);
        }
    }

    void ApplyMagnetEffect()
    {
        GameObject[] gates = GameObject.FindGameObjectsWithTag(gateTag);
        GameObject closestGate = null;
        float minDistY = magnetDistance;
        bool foundGateInRange = false;

        foreach (GameObject gate in gates)
        {
            if (!gate.GetComponent<Collider2D>().enabled) continue;

            float distY = Mathf.Abs(transform.position.y - gate.transform.position.y);
            float distX = gate.transform.position.x - transform.position.x;

            if (distX > -0.5f && distX < lockThresholdX)
            {
                foundGateInRange = true;
                if (distY < minDistY)
                {
                    minDistY = distY;
                    closestGate = gate;
                }
            }
        }

        isLockedByMagnet = foundGateInRange;

        if (closestGate != null)
        {
            float targetY = closestGate.transform.position.y;
            float newY = Mathf.Lerp(transform.position.y, targetY, magnetStrength * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (canMove && other.CompareTag(gateTag))
        {
            canMove = false;
            StartCoroutine(HitGateScaleEffect(1.5f));
            lastHitGate = other.transform.parent.gameObject;

            TextMeshProUGUI gateText = other.GetComponentInChildren<TextMeshProUGUI>();
            if (gateText != null && SpaceShipManager.Instance != null)
            {
                string correctAnswer = SpaceShipManager.Instance.currentCorrectAnswer;
                bool isCorrect = (gateText.text == correctAnswer);

                SpaceShipManager.Instance.SetCauHoiDungYenResult("Kết quả:", correctAnswer);

                if (isCorrect)
                {
                    var am = AudioManager.Instance;
                    if (am != null) am.PlaySFX(am.soundCorrect);

                    if (UiSp.Instance != null)
                    {
                        UiSp.Instance.AddScore(10);
                    }
                    gateText.color = Color.green;
                    SpaceShipManager.Instance.CountCorrectAnswer();
                }
                else
                {
                    var am = AudioManager.Instance;
                    if (am != null) am.PlaySFX(am.soundWrong);

                    gateText.color = Color.red;
                    if (Enemy.Instance != null) Enemy.Instance.MoveCloser();
                }

                if (SpaceShipManager.Instance.CorrectAnswersCount < SpaceShipManager.Instance.TotalGatesToWin)
                {
                    StartCoroutine(UpdateNextQuestionRoutine());
                }
            }
            QuestionZone zone = other.GetComponentInParent<QuestionZone>();
            if (zone != null) zone.FadeOut(1.5f);

            other.enabled = false;
        }
    }

    private IEnumerator UpdateNextQuestionRoutine()
    {
        yield return new WaitForSeconds(2f);

        if (lastHitGate != null)
        {
            Destroy(lastHitGate);
        }

        ResetMovement();

        if (SpaceShipManager.Instance != null)
        {
            QuestionZone nextQ = SpaceShipManager.Instance.GetNextQuestionZone();
            if (nextQ != null)
            {
                SpaceShipManager.Instance.currentCorrectAnswer = nextQ.dapAnDung;
                SpaceShipManager.Instance.UpdateStaticQuestionUI(nextQ.cauHoiLuuTru);
            }
        }
    }

    private void OnEnable()
    {
        ResetMovement();
    }

    public void ResetMovement()
    {
        StopAllCoroutines();
        // SỬA: Trả về kích thước originalScale thay vì Vector3.one
        transform.localScale = originalScale;
        canMove = true;
        isLockedByMagnet = false;
    }
}
