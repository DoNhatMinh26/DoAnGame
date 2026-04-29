using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class QuickFixSlotDetection : EditorWindow
{
    [MenuItem("Tools/Fix Slot Detection")]
    public static void FixSlot()
    {
        // Tìm Slot object
        GameObject slot = GameObject.FindGameObjectWithTag("Slot");
        
        if (slot == null)
        {
            // Thử tìm theo tên
            slot = GameObject.Find("AnswerSlot");
            if (slot == null)
            {
                slot = GameObject.Find("Slot");
            }
        }

        if (slot == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Slot object!\n\nHãy tạo object tên 'AnswerSlot' hoặc 'Slot'", "OK");
            return;
        }

        Debug.Log($"✅ Tìm thấy Slot: {slot.name}");

        // 1. Set tag
        if (slot.tag != "Slot")
        {
            slot.tag = "Slot";
            Debug.Log("✅ Set tag 'Slot'");
        }

        // 2. Fix Image raycastTarget
        Image image = slot.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            Debug.Log("✅ Set Image.raycastTarget = true");
        }
        else
        {
            Debug.LogWarning("⚠️ Slot không có Image component!");
        }

        // 3. Fix CanvasGroup
        CanvasGroup canvasGroup = slot.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            Debug.Log("✅ Set CanvasGroup.blocksRaycasts = true");
        }

        // 4. Đảm bảo Slot active
        if (!slot.activeInHierarchy)
        {
            slot.SetActive(true);
            Debug.Log("✅ Kích hoạt Slot");
        }

        EditorUtility.DisplayDialog("Thành Công", "✅ Đã fix Slot!\n\nHãy test kéo đáp án vào Slot", "OK");
    }

    [MenuItem("Tools/Debug Slot Setup")]
    public static void DebugSlot()
    {
        GameObject slot = GameObject.FindGameObjectWithTag("Slot");
        if (slot == null)
        {
            slot = GameObject.Find("AnswerSlot");
        }

        if (slot == null)
        {
            Debug.LogError("❌ Không tìm thấy Slot!");
            return;
        }

        Debug.Log($"\n=== SLOT DEBUG ===");
        Debug.Log($"Name: {slot.name}");
        Debug.Log($"Tag: {slot.tag}");
        Debug.Log($"Active: {slot.activeInHierarchy}");
        
        Image img = slot.GetComponent<Image>();
        if (img != null)
        {
            Debug.Log($"Image.raycastTarget: {img.raycastTarget}");
        }
        else
        {
            Debug.LogWarning("Không có Image component!");
        }

        CanvasGroup cg = slot.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            Debug.Log($"CanvasGroup.blocksRaycasts: {cg.blocksRaycasts}");
        }

        RectTransform rt = slot.GetComponent<RectTransform>();
        if (rt != null)
        {
            Debug.Log($"Position: {rt.anchoredPosition}");
            Debug.Log($"Size: {rt.sizeDelta}");
        }
    }
}
