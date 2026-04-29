using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

public class DisableGameplayPanelRaycast
{
    [MenuItem("Tools/Fix GameplayPanel Blocking Raycasts")]
    public static void FixGameplayPanel()
    {
        Debug.Log("=== FIXING GAMEPLAYPANEL RAYCAST BLOCKING ===");
        
        // Find GameplayPanel
        var gameplayPanel = GameObject.Find("GameplayPanel");
        if (gameplayPanel == null)
        {
            Debug.LogError("❌ GameplayPanel not found!");
            return;
        }
        
        Debug.Log($"Found GameplayPanel: {gameplayPanel.name}");
        
        // Disable raycastTarget on GameplayPanel's Image
        var image = gameplayPanel.GetComponent<Image>();
        if (image != null)
        {
            if (image.raycastTarget)
            {
                image.raycastTarget = false;
                EditorUtility.SetDirty(gameplayPanel);
                Debug.Log("✅ Disabled raycastTarget on GameplayPanel Image");
            }
            else
            {
                Debug.Log("✅ GameplayPanel Image raycastTarget already disabled");
            }
        }
        else
        {
            Debug.Log("ℹ️ GameplayPanel has no Image component");
        }
        
        // Also disable on Background if exists
        var background = gameplayPanel.transform.Find("Background");
        if (background != null)
        {
            var bgImage = background.GetComponent<Image>();
            if (bgImage != null && bgImage.raycastTarget)
            {
                bgImage.raycastTarget = false;
                EditorUtility.SetDirty(background.gameObject);
                Debug.Log("✅ Disabled raycastTarget on Background");
            }
        }
        
        // Disable on Back button background (not the button itself)
        var back = gameplayPanel.transform.Find("Back");
        if (back != null)
        {
            var backImage = back.GetComponent<Image>();
            // Only disable if it's NOT a button
            if (backImage != null && backImage.raycastTarget && back.GetComponent<UnityEngine.UI.Button>() == null)
            {
                backImage.raycastTarget = false;
                EditorUtility.SetDirty(back.gameObject);
                Debug.Log("✅ Disabled raycastTarget on Back");
            }
        }
        
        Debug.Log("\n=== DONE ===");
        Debug.Log("Now save scene (Ctrl+S) and test drag-drop!");
    }
}
#endif
