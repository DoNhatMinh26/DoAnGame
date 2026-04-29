using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DoAnGame.Multiplayer;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Script để auto-fix drag-drop setup issues
/// Menu: Tools → Fix Drag-Drop Setup
/// </summary>
public class FixDragDropSetup
{
    [MenuItem("Tools/Fix Drag-Drop Setup")]
    public static void FixSetup()
    {
        Debug.Log("=== FIXING DRAG-DROP SETUP ===");
        
        int fixCount = 0;
        
        // 1. Check EventSystem
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            Debug.Log("✅ Created EventSystem");
            fixCount++;
        }
        else
        {
            Debug.Log("✅ EventSystem exists");
        }
        
        // 2. Check Canvas GraphicRaycaster
        var canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"✅ Added GraphicRaycaster to {canvas.name}");
                fixCount++;
            }
        }
        
        // 3. Fix Answer objects
        var answers = Object.FindObjectsOfType<MultiplayerDragAndDrop>(true);
        Debug.Log($"Found {answers.Length} MultiplayerDragAndDrop components");
        
        foreach (var answer in answers)
        {
            bool wasFixed = false;
            
            // Check CanvasGroup
            var canvasGroup = answer.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = answer.gameObject.AddComponent<CanvasGroup>();
                Debug.Log($"✅ Added CanvasGroup to {answer.name}");
                wasFixed = true;
            }
            
            // Fix CanvasGroup settings
            if (canvasGroup.alpha != 1f)
            {
                canvasGroup.alpha = 1f;
                wasFixed = true;
            }
            if (!canvasGroup.blocksRaycasts)
            {
                canvasGroup.blocksRaycasts = true;
                wasFixed = true;
            }
            if (!canvasGroup.interactable)
            {
                canvasGroup.interactable = true;
                wasFixed = true;
            }
            
            // Check Image
            var image = answer.GetComponent<Image>();
            if (image == null)
            {
                image = answer.gameObject.AddComponent<Image>();
                Debug.Log($"✅ Added Image to {answer.name}");
                wasFixed = true;
            }
            
            // Fix Image raycastTarget
            if (!image.raycastTarget)
            {
                image.raycastTarget = true;
                wasFixed = true;
            }
            
            if (wasFixed)
            {
                Debug.Log($"✅ Fixed {answer.name}");
                fixCount++;
            }
        }
        
        // 4. Fix Slot tag
        var slot = GameObject.Find("Slot");
        if (slot != null)
        {
            if (slot.tag != "Slot")
            {
                // Check if "Slot" tag exists
                try
                {
                    slot.tag = "Slot";
                    Debug.Log("✅ Set Slot tag");
                    fixCount++;
                }
                catch
                {
                    Debug.LogError("❌ Tag 'Slot' không tồn tại! Tạo tag 'Slot' trong Tags & Layers");
                }
            }
            else
            {
                Debug.Log("✅ Slot tag is correct");
            }
            
            // Check Slot Image
            var slotImage = slot.GetComponent<Image>();
            if (slotImage != null && !slotImage.raycastTarget)
            {
                slotImage.raycastTarget = true;
                Debug.Log("✅ Enabled Slot raycastTarget");
                fixCount++;
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Slot GameObject not found");
        }
        
        // 5. Release global lock
        MultiplayerDragAndDrop.SetGlobalLock(false);
        Debug.Log("✅ Released global lock");
        
        Debug.Log($"\n=== DONE: Fixed {fixCount} issues ===");
        
        if (fixCount > 0)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                EditorUtility.SetDirty(canvas.gameObject);
            }
            Debug.Log("⚠️ Nhớ SAVE SCENE (Ctrl+S)!");
        }
    }
    
    [MenuItem("Tools/Debug Drag-Drop Status")]
    public static void DebugStatus()
    {
        Debug.Log("=== DRAG-DROP STATUS ===");
        
        // EventSystem
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        Debug.Log($"EventSystem: {(eventSystem != null ? "✅ EXISTS" : "❌ MISSING")}");
        
        // Canvas
        var canvases = Object.FindObjectsOfType<Canvas>();
        Debug.Log($"Canvas count: {canvases.Length}");
        foreach (var canvas in canvases)
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            Debug.Log($"  - {canvas.name}: GraphicRaycaster = {(raycaster != null ? "✅" : "❌")}");
        }
        
        // Answers
        var answers = Object.FindObjectsOfType<MultiplayerDragAndDrop>(true);
        Debug.Log($"MultiplayerDragAndDrop count: {answers.Length}");
        foreach (var answer in answers)
        {
            var canvasGroup = answer.GetComponent<CanvasGroup>();
            var image = answer.GetComponent<Image>();
            
            Debug.Log($"  - {answer.name}:");
            Debug.Log($"      Active: {answer.gameObject.activeInHierarchy}");
            Debug.Log($"      CanvasGroup: {(canvasGroup != null ? "✅" : "❌")}");
            if (canvasGroup != null)
            {
                Debug.Log($"        alpha={canvasGroup.alpha}, blocksRaycasts={canvasGroup.blocksRaycasts}, interactable={canvasGroup.interactable}");
            }
            Debug.Log($"      Image: {(image != null ? "✅" : "❌")}");
            if (image != null)
            {
                Debug.Log($"        raycastTarget={image.raycastTarget}");
            }
        }
        
        // Slot
        var slot = GameObject.Find("Slot");
        if (slot != null)
        {
            Debug.Log($"Slot: ✅ EXISTS");
            Debug.Log($"  Tag: {slot.tag}");
            var slotImage = slot.GetComponent<Image>();
            if (slotImage != null)
            {
                Debug.Log($"  Image raycastTarget: {slotImage.raycastTarget}");
            }
        }
        else
        {
            Debug.Log("Slot: ❌ NOT FOUND");
        }
    }
    
    [MenuItem("Tools/Find Blocking UI Elements")]
    public static void FindBlockingElements()
    {
        Debug.Log("=== FINDING BLOCKING UI ELEMENTS ===");
        
        // Find all Graphics with raycastTarget = true
        var allGraphics = Object.FindObjectsOfType<Graphic>(true);
        
        var raycastGraphics = allGraphics.Where(g => g.raycastTarget).ToArray();
        Debug.Log($"Total Graphics with raycastTarget: {raycastGraphics.Length}");
        Debug.Log("\nPotential blockers (raycastTarget=true):");
        
        foreach (var graphic in raycastGraphics)
        {
            // Check if it's above Answer objects in hierarchy
            var rectTransform = graphic.GetComponent<RectTransform>();
            if (rectTransform == null) continue;
            
            // Get sibling index (higher = rendered on top)
            int siblingIndex = rectTransform.GetSiblingIndex();
            
            Debug.Log($"  - {GetFullPath(graphic.transform)}");
            Debug.Log($"      Type: {graphic.GetType().Name}");
            Debug.Log($"      Active: {graphic.gameObject.activeInHierarchy}");
            Debug.Log($"      Alpha: {graphic.color.a}");
            Debug.Log($"      Sibling Index: {siblingIndex}");
            
            // Check if it's a transparent blocker
            if (graphic.color.a < 0.01f)
            {
                Debug.LogWarning($"      ⚠️ TRANSPARENT BLOCKER! (alpha={graphic.color.a})");
            }
            
            // Check CanvasGroup
            var canvasGroup = graphic.GetComponent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.blocksRaycasts)
            {
                Debug.Log($"      CanvasGroup: blocksRaycasts=true, alpha={canvasGroup.alpha}");
            }
        }
        
        Debug.Log("\n=== RECOMMENDATIONS ===");
        Debug.Log("1. Tìm objects có alpha gần 0 nhưng raycastTarget=true");
        Debug.Log("2. Check sibling index - số càng cao càng nằm trên");
        Debug.Log("3. Disable raycastTarget cho background/decoration images");
    }
    
    [MenuItem("Tools/Disable Raycast on Non-Interactive UI")]
    public static void DisableNonInteractiveRaycast()
    {
        Debug.Log("=== DISABLING RAYCAST ON NON-INTERACTIVE UI ===");
        
        int disabledCount = 0;
        var allGraphics = Object.FindObjectsOfType<Graphic>(true);
        
        foreach (var graphic in allGraphics)
        {
            if (!graphic.raycastTarget) continue;
            
            // Skip if it's an Answer or Slot
            if (graphic.GetComponent<MultiplayerDragAndDrop>() != null) continue;
            if (graphic.CompareTag("Slot")) continue;
            
            // Skip if it has Button/Toggle/InputField
            if (graphic.GetComponent<Button>() != null) continue;
            if (graphic.GetComponent<Toggle>() != null) continue;
            if (graphic.GetComponent<InputField>() != null) continue;
            if (graphic.GetComponent<TMPro.TMP_InputField>() != null) continue;
            
            // Check if it's likely a background/decoration
            string name = graphic.name.ToLower();
            if (name.Contains("background") || 
                name.Contains("bg") || 
                name.Contains("panel") ||
                name.Contains("decoration") ||
                name.Contains("border") ||
                graphic.color.a < 0.01f)
            {
                graphic.raycastTarget = false;
                Debug.Log($"✅ Disabled raycast on: {GetFullPath(graphic.transform)}");
                disabledCount++;
                EditorUtility.SetDirty(graphic);
            }
        }
        
        Debug.Log($"\n=== DONE: Disabled {disabledCount} raycast targets ===");
        
        if (disabledCount > 0)
        {
            Debug.Log("⚠️ Nhớ SAVE SCENE (Ctrl+S)!");
        }
    }
    
    private static string GetFullPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
#endif
