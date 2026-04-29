using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DoAnGame.Multiplayer;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

public class FindRaycastBlockers
{
    [MenuItem("Tools/Find What's Blocking Answer Objects")]
    public static void FindBlockers()
    {
        Debug.Log("=== FINDING RAYCAST BLOCKERS ===");
        
        // Find all Answer objects
        var answers = Object.FindObjectsOfType<MultiplayerDragAndDrop>(true);
        
        if (answers.Length == 0)
        {
            Debug.LogError("❌ No Answer objects found!");
            return;
        }
        
        Debug.Log($"Found {answers.Length} Answer objects");
        
        // Get Canvas
        var canvas = answers[0].GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Canvas not found!");
            return;
        }
        
        // Find ALL Graphics with raycastTarget in the same Canvas
        var allGraphics = canvas.GetComponentsInChildren<Graphic>(true);
        var raycastGraphics = allGraphics.Where(g => g.raycastTarget).ToList();
        
        Debug.Log($"\nTotal Graphics with raycastTarget=true: {raycastGraphics.Count}");
        
        // Check sibling index of Answer objects
        Debug.Log("\n--- ANSWER OBJECTS SIBLING INDEX ---");
        foreach (var answer in answers)
        {
            int siblingIndex = answer.transform.GetSiblingIndex();
            Debug.Log($"  {answer.name}: Sibling Index = {siblingIndex}");
        }
        
        // Find objects with HIGHER sibling index (rendered on top)
        Debug.Log("\n--- OBJECTS RENDERED ON TOP OF ANSWERS ---");
        int maxAnswerIndex = answers.Max(a => a.transform.GetSiblingIndex());
        
        var blockersFound = 0;
        foreach (var graphic in raycastGraphics)
        {
            // Skip if it's an Answer object itself
            if (graphic.GetComponent<MultiplayerDragAndDrop>() != null) continue;
            
            // Check if same parent
            if (graphic.transform.parent == answers[0].transform.parent)
            {
                int siblingIndex = graphic.transform.GetSiblingIndex();
                if (siblingIndex > maxAnswerIndex)
                {
                    Debug.LogWarning($"⚠️ BLOCKER: {GetFullPath(graphic.transform)}");
                    Debug.LogWarning($"    Sibling Index: {siblingIndex} (higher than Answer max: {maxAnswerIndex})");
                    Debug.LogWarning($"    Type: {graphic.GetType().Name}");
                    Debug.LogWarning($"    Alpha: {graphic.color.a}");
                    blockersFound++;
                }
            }
        }
        
        if (blockersFound == 0)
        {
            Debug.Log("✅ No obvious blockers found based on sibling index");
        }
        
        // Check for CanvasGroup blockers
        Debug.Log("\n--- CHECKING CANVASGROUP BLOCKERS ---");
        var canvasGroups = canvas.GetComponentsInChildren<CanvasGroup>(true);
        foreach (var cg in canvasGroups)
        {
            if (cg.blocksRaycasts && cg.gameObject != answers[0].gameObject)
            {
                // Check if this CanvasGroup is parent of Answer objects
                bool isParentOfAnswer = false;
                foreach (var answer in answers)
                {
                    if (answer.transform.IsChildOf(cg.transform) && cg.transform != answer.transform)
                    {
                        isParentOfAnswer = true;
                        break;
                    }
                }
                
                if (isParentOfAnswer)
                {
                    Debug.LogWarning($"⚠️ CanvasGroup on parent: {GetFullPath(cg.transform)}");
                    Debug.LogWarning($"    blocksRaycasts: {cg.blocksRaycasts}");
                    Debug.LogWarning($"    alpha: {cg.alpha}");
                    Debug.LogWarning($"    interactable: {cg.interactable}");
                }
            }
        }
        
        // List ALL raycast targets for manual inspection
        Debug.Log("\n--- ALL RAYCAST TARGETS (for manual inspection) ---");
        foreach (var graphic in raycastGraphics)
        {
            Debug.Log($"  {GetFullPath(graphic.transform)} (Type: {graphic.GetType().Name}, Alpha: {graphic.color.a})");
        }
        
        Debug.Log("\n=== RECOMMENDATIONS ===");
        Debug.Log("1. Move Answer objects to END of parent's children list (highest sibling index)");
        Debug.Log("2. Disable raycastTarget on decorative UI (backgrounds, borders, etc.)");
        Debug.Log("3. Check CanvasGroup on parents - should NOT block raycasts");
        Debug.Log("4. Use Unity's EventSystem Debugger: Window → Analysis → Event System Debugger");
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
