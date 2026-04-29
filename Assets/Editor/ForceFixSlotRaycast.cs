using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor script để force fix Slot raycast
/// Chạy ngay lập tức trong Editor
/// </summary>
public class ForceFixSlotRaycast
{
    [MenuItem("Tools/Fix Slot Raycast")]
    public static void FixSlotRaycast()
    {
        // Tìm tất cả Slot objects trong scene hiện tại
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int fixedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            // Kiểm tra tag hoặc tên
            if (obj.CompareTag("Slot") || obj.name.Contains("Slot"))
            {
                Image image = obj.GetComponent<Image>();
                if (image != null && image.raycastTarget)
                {
                    image.raycastTarget = false;
                    EditorUtility.SetDirty(obj);
                    Debug.Log("[ForceFixSlotRaycast] Fixed: " + obj.name);
                    fixedCount++;
                }
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log("[ForceFixSlotRaycast] Fixed " + fixedCount + " Slot objects");
            // Save scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        else
        {
            Debug.Log("[ForceFixSlotRaycast] No Slot objects found or already fixed");
        }
    }

    [MenuItem("Tools/Fix Answer Raycast")]
    public static void FixAnswerRaycast()
    {
        // Tìm tất cả Answer objects
        DoAnGame.Multiplayer.MultiplayerDragAndDrop[] answers = Object.FindObjectsOfType<DoAnGame.Multiplayer.MultiplayerDragAndDrop>();
        int fixedCount = 0;

        foreach (DoAnGame.Multiplayer.MultiplayerDragAndDrop answer in answers)
        {
            bool didFix = false;

            // Fix Image raycastTarget
            Image image = answer.GetComponent<Image>();
            if (image != null && !image.raycastTarget)
            {
                image.raycastTarget = true;
                didFix = true;
            }

            // Fix CanvasGroup blocksRaycasts
            CanvasGroup canvasGroup = answer.GetComponent<CanvasGroup>();
            if (canvasGroup != null && !canvasGroup.blocksRaycasts)
            {
                canvasGroup.blocksRaycasts = true;
                didFix = true;
            }

            if (didFix)
            {
                EditorUtility.SetDirty(answer.gameObject);
                Debug.Log("[ForceFixSlotRaycast] Fixed Answer: " + answer.name);
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log("[ForceFixSlotRaycast] Fixed " + fixedCount + " Answer objects");
            // Save scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
        else
        {
            Debug.Log("[ForceFixSlotRaycast] No Answer objects found or already fixed");
        }
    }

    [MenuItem("Tools/Check Raycast Status")]
    public static void CheckRaycastStatus()
    {
        Debug.Log("========== RAYCAST STATUS CHECK ==========");

        // Check Slots
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int slotCount = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag("Slot") || obj.name.Contains("Slot"))
            {
                slotCount++;
                Image image = obj.GetComponent<Image>();
                if (image != null)
                {
                    string status = image.raycastTarget ? "BAD (BLOCKING)" : "GOOD (OK)";
                    Debug.Log("Slot " + obj.name + ": raycastTarget=" + image.raycastTarget + " " + status);
                }
            }
        }

        if (slotCount == 0)
        {
            Debug.LogWarning("No Slot objects found!");
        }

        // Check Answers
        DoAnGame.Multiplayer.MultiplayerDragAndDrop[] answers = Object.FindObjectsOfType<DoAnGame.Multiplayer.MultiplayerDragAndDrop>();
        Debug.Log("Answer objects found: " + answers.Length);

        foreach (DoAnGame.Multiplayer.MultiplayerDragAndDrop answer in answers)
        {
            Image image = answer.GetComponent<Image>();
            CanvasGroup canvasGroup = answer.GetComponent<CanvasGroup>();
            
            bool imageOK = image != null && image.raycastTarget;
            bool canvasGroupOK = canvasGroup != null && canvasGroup.blocksRaycasts;
            
            string status = (imageOK && canvasGroupOK) ? "GOOD (OK)" : "BAD (BROKEN)";
            Debug.Log("Answer " + answer.name + ": Image=" + imageOK + ", CanvasGroup=" + canvasGroupOK + " " + status);
        }

        Debug.Log("==========================================");
    }
}