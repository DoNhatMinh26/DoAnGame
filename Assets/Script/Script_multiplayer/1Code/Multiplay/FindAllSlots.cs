using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.Multiplayer
{
    public class FindAllSlots : MonoBehaviour
    {
        [ContextMenu("Find All Slots")]
        public void FindSlots()
        {
            Debug.Log("\n=== TÌM TẤT CẢ SLOT ===");

            // Tìm theo tag
            GameObject[] slotsByTag = GameObject.FindGameObjectsWithTag("Slot");
            Debug.Log($"Tìm theo tag 'Slot': {slotsByTag.Length} objects");
            foreach (var slot in slotsByTag)
            {
                PrintSlotInfo(slot);
            }

            // Tìm theo tên
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            int count = 0;
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Slot") || obj.name.Contains("slot"))
                {
                    count++;
                    Debug.Log($"\nTìm theo tên: {obj.name}");
                    PrintSlotInfo(obj);
                }
            }
            Debug.Log($"Tìm theo tên: {count} objects");

            // Tìm tất cả Image có raycastTarget = true
            Image[] allImages = FindObjectsOfType<Image>();
            Debug.Log($"\n=== TẤT CẢ IMAGE OBJECTS ===");
            Debug.Log($"Tổng Image: {allImages.Length}");
            foreach (var img in allImages)
            {
                if (img.raycastTarget)
                {
                    Debug.Log($"• {img.gameObject.name} (raycastTarget: true, tag: {img.gameObject.tag})");
                }
            }
        }

        private void PrintSlotInfo(GameObject slot)
        {
            Debug.Log($"  Name: {slot.name}");
            Debug.Log($"  Tag: {slot.tag}");
            Debug.Log($"  Active: {slot.activeInHierarchy}");
            Debug.Log($"  Parent: {(slot.transform.parent != null ? slot.transform.parent.name : "None")}");

            Image img = slot.GetComponent<Image>();
            if (img != null)
            {
                Debug.Log($"  Image.raycastTarget: {img.raycastTarget}");
            }

            CanvasGroup cg = slot.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                Debug.Log($"  CanvasGroup.blocksRaycasts: {cg.blocksRaycasts}");
            }

            RectTransform rt = slot.GetComponent<RectTransform>();
            if (rt != null)
            {
                Debug.Log($"  Position: {rt.anchoredPosition}");
                Debug.Log($"  Size: {rt.sizeDelta}");
            }
        }
    }
}
