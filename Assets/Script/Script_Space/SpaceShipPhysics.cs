using UnityEngine;
using TMPro;

public class SpaceShipPhysics : MonoBehaviour
{
    [Header("Cài đặt di chuyển bằng Chuột")]
    public float followSpeed = 25f;
    public float minY = -6f, maxY = 3f; // Khớp với Inspector của bạn

    [Header("Cấu hình Hút Tuyệt Đối")]
    public float magnetDistance = 4f;   // Tăng thêm phạm vi quét
    public float magnetStrength = 20f;  // Tăng vọt lực hút để không thể lọt khe
    public float lockThresholdX = 6f;   // Khoảng cách X bắt đầu "khóa" chuột
    public string gateTag = "Gate";

    private bool canMove = true;
    private bool isLockedByMagnet = false; // Trạng thái cưỡng bức hút

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

            // Nếu sắp chạm cổng (trong khoảng lockThresholdX)
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

        // Nếu ở rất gần cổng theo trục X, khóa điều khiển chuột để tránh lọt khe
        isLockedByMagnet = foundGateInRange;

        if (closestGate != null)
        {
            float targetY = closestGate.transform.position.y;
            // Ép phi thuyền vào tâm cổng ngay lập tức
            float newY = Mathf.Lerp(transform.position.y, targetY, magnetStrength * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(gateTag))
        {
            canMove = false;
            isLockedByMagnet = false; // Giải phóng khóa để hiện kết quả

            TextMeshProUGUI gateText = other.GetComponentInChildren<TextMeshProUGUI>();

            if (gateText != null && SpaceShipManager.Instance != null)
            {
                string selectedVal = gateText.text;
                string correctAnswer = SpaceShipManager.Instance.currentCorrectAnswer;
                bool isCorrect = (selectedVal == correctAnswer);

                string questionPart = SpaceShipManager.Instance.GetCauHoiDungYenText().Replace("?", "").Trim();
                SpaceShipManager.Instance.SetCauHoiDungYenResult(questionPart, correctAnswer);

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