using UnityEngine;
using TMPro;

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
        ApplyMagnetEffect();

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
        if (other.CompareTag(gateTag))
        {
            canMove = false;
            isLockedByMagnet = false;

            TextMeshProUGUI gateText = other.GetComponentInChildren<TextMeshProUGUI>();

            if (gateText != null && SpaceShipManager.Instance != null)
            {
                // Đồng bộ: Lấy đáp án đúng đã được lưu trong Manager
                string correctAnswer = SpaceShipManager.Instance.currentCorrectAnswer;
                bool isCorrect = (gateText.text == correctAnswer);

                // Lấy phần câu hỏi từ UI (loại bỏ dấu ? để ghép với đáp án đúng)
                // Manager mới sử dụng hàm UpdateStaticQuestionUI để quản lý text này
                SpaceShipPhysics playerScript = FindObjectOfType<SpaceShipPhysics>();

                // Gọi hàm hiển thị kết quả cuối cùng lên UI
                // Lưu ý: Chúng ta lấy text hiện tại từ Manager thay vì gọi hàm không tồn tại
                SpaceShipManager.Instance.SetCauHoiDungYenResult("Kết quả:", correctAnswer);

                gateText.color = isCorrect ? Color.green : Color.red;
            }
            other.enabled = false;
        }
    }

    public void ResetMovement()
    {
        canMove = true;
        isLockedByMagnet = false;
    }
}