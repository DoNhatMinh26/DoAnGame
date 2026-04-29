using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;

public class TestEventSystem
{
    [MenuItem("Tools/Test EventSystem")]
    public static void TestEvents()
    {
        Debug.Log("=== TESTING EVENTSYSTEM ===");
        
        // Check EventSystem
        var eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ NO EVENTSYSTEM FOUND!");
            Debug.LogError("→ Create one: GameObject → UI → Event System");
            return;
        }
        
        Debug.Log($"✅ EventSystem found: {eventSystem.name}");
        Debug.Log($"  Enabled: {eventSystem.enabled}");
        Debug.Log($"  GameObject Active: {eventSystem.gameObject.activeInHierarchy}");
        
        // Check StandaloneInputModule
        var inputModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (inputModule == null)
        {
            Debug.LogWarning("⚠️ No StandaloneInputModule!");
        }
        else
        {
            Debug.Log($"✅ StandaloneInputModule found");
            Debug.Log($"  Enabled: {inputModule.enabled}");
        }
        
        // Check current selected object
        if (eventSystem.currentSelectedGameObject != null)
        {
            Debug.Log($"  Current Selected: {eventSystem.currentSelectedGameObject.name}");
        }
        
        Debug.Log("\n=== INSTRUCTIONS ===");
        Debug.Log("1. Run game");
        Debug.Log("2. Click on Answer object");
        Debug.Log("3. Check Console for '[MultiplayerDragAndDrop] OnBeginDrag CALLED'");
        Debug.Log("4. If NO log → EventSystem not detecting clicks!");
    }
}
#endif
