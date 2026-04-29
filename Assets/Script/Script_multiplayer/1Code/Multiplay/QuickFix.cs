using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script đơn giản nhất để fix raycast issues
/// Không có namespace, không có dependency phức tạp
/// </summary>
public class QuickFix : MonoBehaviour
{
    [Header("Auto Fix")]
    public bool fixOnStart = true;
    public bool fixContinuously = true;
    public float checkInterval = 1f;

    private float nextCheckTime;

    private void Start()
    {
        if (fixOnStart)
        {
            FixRaycastIssues();
        }
    }

    private void Update()
    {
        if (!fixContinuously)
            return;

        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;
        FixRaycastIssues();
    }

    [ContextMenu("Fix Raycast Issues")]
    public void FixRaycastIssues()
    {
        FixSlots();
        FixAnswers();
        Debug.Log("[QuickFix] Fixed raycast issues");
    }

    [ContextMenu("Fix Slots")]
    public void FixSlots()
    {
        // Tìm tất cả objects có tag "Slot"
        GameObject[] slots = GameObject.FindGameObjectsWithTag("Slot");
        
        foreach (GameObject slot in slots)
        {
            Image image = slot.GetComponent<Image>();
            if (image != null && image.raycastTarget)
            {
                image.raycastTarget = false;
                Debug.Log("[QuickFix] Fixed Slot: " + slot.name);
            }
        }

        // Fallback: Tìm theo tên
        if (slots.Length == 0)
        {
            Image[] allImages = FindObjectsOfType<Image>(true);
            foreach (Image img in allImages)
            {
                if (img.name.Contains("Slot") && img.raycastTarget)
                {
                    img.raycastTarget = false;
                    Debug.Log("[QuickFix] Fixed Slot by name: " + img.name);
                }
            }
        }
    }

    [ContextMenu("Fix Answers")]
    public void FixAnswers()
    {
        // Tìm tất cả Answer objects
        DoAnGame.Multiplayer.MultiplayerDragAndDrop[] answers = FindObjectsOfType<DoAnGame.Multiplayer.MultiplayerDragAndDrop>(true);
        
        foreach (DoAnGame.Multiplayer.MultiplayerDragAndDrop answer in answers)
        {
            // Fix Image raycastTarget
            Image image = answer.GetComponent<Image>();
            if (image != null && !image.raycastTarget)
            {
                image.raycastTarget = true;
                Debug.Log("[QuickFix] Fixed Answer Image: " + answer.name);
            }

            // Fix CanvasGroup blocksRaycasts
            CanvasGroup canvasGroup = answer.GetComponent<CanvasGroup>();
            if (canvasGroup != null && !canvasGroup.blocksRaycasts)
            {
                canvasGroup.blocksRaycasts = true;
                Debug.Log("[QuickFix] Fixed Answer CanvasGroup: " + answer.name);
            }
        }
    }

    [ContextMenu("Check Status")]
    public void CheckStatus()
    {
        Debug.Log("========== RAYCAST STATUS ==========");

        // Check Slots
        GameObject[] slots = GameObject.FindGameObjectsWithTag("Slot");
        Debug.Log("Slots found: " + slots.Length);
        foreach (GameObject slot in slots)
        {
            Image image = slot.GetComponent<Image>();
            if (image != null)
            {
                string status = image.raycastTarget ? "BAD (BLOCKING)" : "GOOD (OK)";
                Debug.Log("  " + slot.name + ": raycastTarget=" + image.raycastTarget + " - " + status);
            }
        }

        // Check Answers
        DoAnGame.Multiplayer.MultiplayerDragAndDrop[] answers = FindObjectsOfType<DoAnGame.Multiplayer.MultiplayerDragAndDrop>(true);
        Debug.Log("Answer objects found: " + answers.Length);
        foreach (DoAnGame.Multiplayer.MultiplayerDragAndDrop answer in answers)
        {
            Image image = answer.GetComponent<Image>();
            CanvasGroup canvasGroup = answer.GetComponent<CanvasGroup>();
            
            bool imageOK = image != null && image.raycastTarget;
            bool canvasGroupOK = canvasGroup != null && canvasGroup.blocksRaycasts;
            
            string status = (imageOK && canvasGroupOK) ? "GOOD (OK)" : "BAD (BROKEN)";
            Debug.Log("  " + answer.name + ": Image=" + imageOK + ", CanvasGroup=" + canvasGroupOK + " - " + status);
        }

        Debug.Log("====================================");
    }
}