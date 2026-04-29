using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Debug script để hiển thị vị trí Slot trên màn hình
    /// </summary>
    public class SlotPositionDebugger : MonoBehaviour
    {
        private GameObject slot;
        private RectTransform slotRect;
        private Canvas canvas;

        private void Start()
        {
            // Tìm Slot
            slot = GameObject.FindGameObjectWithTag("Slot");
            if (slot == null)
            {
                slot = GameObject.Find("AnswerSlot");
            }

            if (slot == null)
            {
                Debug.LogError("❌ Không tìm thấy Slot!");
                return;
            }

            slotRect = slot.GetComponent<RectTransform>();
            canvas = FindObjectOfType<Canvas>();

            Debug.Log($"✅ Tìm thấy Slot: {slot.name}");
            PrintSlotInfo();
        }

        private void Update()
        {
            if (slot == null) return;

            // In thông tin Slot mỗi frame
            if (Time.frameCount % 30 == 0) // Mỗi 30 frame
            {
                PrintSlotInfo();
            }
        }

        private void PrintSlotInfo()
        {
            if (slotRect == null) return;

            // Lấy vị trí screen của Slot
            Vector3[] corners = new Vector3[4];
            slotRect.GetWorldCorners(corners);

            // Chuyển sang screen position
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);

            Debug.Log($"\n=== SLOT POSITION ===");
            Debug.Log($"Slot Name: {slot.name}");
            Debug.Log($"Anchored Position: {slotRect.anchoredPosition}");
            Debug.Log($"Screen Position Min: {screenMin}");
            Debug.Log($"Screen Position Max: {screenMax}");
            Debug.Log($"Screen Center: {(screenMin + screenMax) / 2}");
            Debug.Log($"Size: {slotRect.sizeDelta}");
            Debug.Log($"Active: {slot.activeInHierarchy}");
            
            Image img = slot.GetComponent<Image>();
            if (img != null)
            {
                Debug.Log($"Image.raycastTarget: {img.raycastTarget}");
            }

            CanvasGroup cg = slot.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                Debug.Log($"CanvasGroup.blocksRaycasts: {cg.blocksRaycasts}");
            }
        }

        [ContextMenu("Print Slot Info")]
        public void PrintSlotInfoManual()
        {
            PrintSlotInfo();
        }
    }
}
