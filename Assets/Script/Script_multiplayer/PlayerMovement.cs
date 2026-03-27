using Unity.Netcode; // BẮT BUỘC: Để dùng các tính năng mạng
using UnityEngine;

// Đổi MonoBehaviour thành NetworkBehaviour
public class PlayerMovement : NetworkBehaviour 
{
    public float moveSpeed = 5f;

    void Update()
    {
        // Kiểm tra xem đây có phải là nhân vật của người đang cầm máy không
        if (!IsOwner) return; 

        // Lấy dữ liệu phím bấm (WASD hoặc phím mũi tên)
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        // Di chuyển nhân vật
        Vector3 moveDir = new Vector3(x, y, 0);
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
}