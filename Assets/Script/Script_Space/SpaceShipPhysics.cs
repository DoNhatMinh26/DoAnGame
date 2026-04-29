using UnityEngine;
using TMPro;
using System.Collections;
public class SpaceShipPhysics : MonoBehaviour
{
    [Header("Cài đặt di chuyển bằng Chuột")]
    public float followSpeed = 25f;
    public float minY = -6f, maxY = 3f;

    [Header("Cấu hình Hút Tuyệt Đối")]
    public float magnetDistance = 4f;
    public float magnetStrength = 20f;
    public float lockThresholdX = 6f;
    public string gateTag = "Gate";

    private bool canMove = true;
    private bool isLockedByMagnet = false;

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
        
        // Chỉ xử lý nếu đang được phép di chuyển (không bị khóa)
        if (canMove && other.CompareTag(gateTag))
        {
            canMove = false; // KHÓA NGAY LẬP TỨC
            isLockedByMagnet = false;

            TextMeshProUGUI gateText = other.GetComponentInChildren<TextMeshProUGUI>();

            if (gateText != null && SpaceShipManager.Instance != null)
            {
                string correctAnswer = SpaceShipManager.Instance.currentCorrectAnswer;
                bool isCorrect = (gateText.text == correctAnswer);

                SpaceShipManager.Instance.SetCauHoiDungYenResult("Kết quả:", correctAnswer);

                if (isCorrect)
                {
                    gateText.color = Color.green;
                }
                else
                {
                    gateText.color = Color.red;
                    if (Enemy.Instance != null) Enemy.Instance.MoveCloser();
                }

                SpaceShipManager.Instance.CountGatePassed();

                // Sử dụng Coroutine để đợi 2 giây trước khi mở khóa
                if (SpaceShipManager.Instance.gatesPassed < SpaceShipManager.Instance.totalGatesToWin)
                {
                    StartCoroutine(UpdateNextQuestionRoutine());
                }
            }
            QuestionZone zone = other.GetComponentInParent<QuestionZone>();
            if (zone != null)
            {
                // Làm mờ cổng trong vòng 1.5 giây
                zone.FadeOut(1.5f);
            }
            // Vô hiệu hóa cổng vừa chạm để tránh va chạm lặp
            other.enabled = false;
        }
    }

    private IEnumerator UpdateNextQuestionRoutine()
    {
        // Đợi đúng 2 giây theo yêu cầu của bạn
        yield return new WaitForSeconds(2f);

        // Mở khóa cho phép di chuyển và chọn đáp án tiếp theo
        ResetMovement();

        // Cập nhật câu hỏi cho cổng tiếp theo
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

    public void ResetMovement()
    {
        canMove = true;
        isLockedByMagnet = false;
    }
}