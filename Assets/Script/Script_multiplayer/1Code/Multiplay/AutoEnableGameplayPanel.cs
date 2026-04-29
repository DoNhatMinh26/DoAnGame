using UnityEngine;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Tự động kích hoạt GameplayPanel khi battle bắt đầu
    /// </summary>
    public class AutoEnableGameplayPanel : MonoBehaviour
    {
        private void Start()
        {
            // Tìm GameplayPanel
            GameObject gameplayPanel = GameObject.Find("GameplayPanel");
            
            if (gameplayPanel == null)
            {
                Debug.LogError("❌ Không tìm thấy GameplayPanel!");
                return;
            }

            // Kích hoạt nó
            if (!gameplayPanel.activeInHierarchy)
            {
                gameplayPanel.SetActive(true);
                Debug.Log("✅ Kích hoạt GameplayPanel");
            }
            else
            {
                Debug.Log("✅ GameplayPanel đã active");
            }

            // Kiểm tra Slot
            GameObject slot = GameObject.FindGameObjectWithTag("Slot");
            if (slot != null)
            {
                Debug.Log($"✅ Tìm thấy Slot: {slot.name}");
                Debug.Log($"   Active: {slot.activeInHierarchy}");
            }
            else
            {
                Debug.LogError("❌ Không tìm thấy Slot!");
            }
        }
    }
}
