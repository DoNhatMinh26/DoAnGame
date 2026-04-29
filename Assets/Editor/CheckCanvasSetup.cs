using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public class CheckCanvasSetup
{
    [MenuItem("Tools/Check Canvas Setup")]
    public static void CheckCanvas()
    {
        Debug.Log("=== CHECKING CANVAS SETUP ===");
        
        var canvases = Object.FindObjectsOfType<Canvas>();
        
        foreach (var canvas in canvases)
        {
            Debug.Log($"\n--- Canvas: {canvas.name} ---");
            Debug.Log($"  Render Mode: {canvas.renderMode}");
            Debug.Log($"  Scale Factor: {canvas.scaleFactor}");
            Debug.Log($"  Reference Pixels Per Unit: {canvas.referencePixelsPerUnit}");
            
            // Check GraphicRaycaster
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            Debug.Log($"  Has GraphicRaycaster: {raycaster != null}");
            if (raycaster != null)
            {
                Debug.Log($"    Ignore Reversed Graphics: {raycaster.ignoreReversedGraphics}");
                Debug.Log($"    Blocking Objects: {raycaster.blockingObjects}");
            }
            
            // Check CanvasScaler
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Debug.Log($"  Has CanvasScaler: TRUE");
                Debug.Log($"    UI Scale Mode: {scaler.uiScaleMode}");
                Debug.Log($"    Reference Resolution: {scaler.referenceResolution}");
                Debug.Log($"    Screen Match Mode: {scaler.screenMatchMode}");
                Debug.Log($"    Match: {scaler.matchWidthOrHeight}");
            }
            else
            {
                Debug.Log($"  Has CanvasScaler: FALSE");
            }
            
            // Check if canvas contains Answer objects
            var answers = canvas.GetComponentsInChildren<DoAnGame.Multiplayer.MultiplayerDragAndDrop>(true);
            if (answers.Length > 0)
            {
                Debug.Log($"  ✅ Contains {answers.Length} Answer objects");
                Debug.Log($"  → This is the MAIN CANVAS for drag-drop!");
            }
        }
        
        Debug.Log("\n=== RECOMMENDATIONS ===");
        Debug.Log("For best drag-drop performance:");
        Debug.Log("1. Canvas Render Mode: Screen Space - Overlay (recommended)");
        Debug.Log("2. Or Screen Space - Camera with proper camera setup");
        Debug.Log("3. CanvasScaler: Constant Pixel Size or Scale With Screen Size");
        Debug.Log("4. GraphicRaycaster: MUST be present");
    }
}
#endif
