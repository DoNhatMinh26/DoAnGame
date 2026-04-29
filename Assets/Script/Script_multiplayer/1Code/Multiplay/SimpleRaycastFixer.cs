using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Script đơn giản để fix raycast issues
    /// Attach vào GameplayPanel và chạy
    /// </summary>
    public class SimpleRaycastFixer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool runContinuously = true;
        [SerializeField] private float checkInterval = 1f;

        private float nextCheckTime;

        private void Start()
        {
            if (runOnStart)
            {
                FixAllRaycastIssues();
            }
        }

        private void Update()
        {
            if (!runContinuously)
                return;

            if (Time.time < nextCheckTime)
                return;

            nextCheckTime = Time.time + checkInterval;
            FixAllRaycastIssues();
        }

        [ContextMenu("Fix All Raycast Issues")]
        public void FixAllRaycastIssues()
        {
            FixSlotRaycast();
            FixAnswerRaycast();
            Debug.Log("[SimpleRaycastFixer] Fixed all raycast issues");
        }

        [ContextMenu("Fix Slot Raycast")]
        public void FixSlotRaycast()
        {
            // Tìm tất cả Slot objects
            var slots = GameObject.FindGameObjectsWithTag("Slot");
            
            foreach (var slot in slots)
            {
                var image = slot.GetComponent<Image>();
                if (image != null && image.raycastTarget)
                {
                    image.raycastTarget = false;
                    Debug.Log("[SimpleRaycastFixer] Fixed Slot: " + slot.name);
                }
            }

            // Fallback: Tìm theo tên
            if (slots.Length == 0)
            {
                var allImages = FindObjectsOfType<Image>(true);
                foreach (var img in allImages)
                {
                    if (img.name.Contains("Slot") && img.raycastTarget)
                    {
                        img.raycastTarget = false;
                        Debug.Log("[SimpleRaycastFixer] Fixed Slot by name: " + img.name);
                    }
                }
            }
        }

        [ContextMenu("Fix Answer Raycast")]
        public void FixAnswerRaycast()
        {
            // Tìm tất cả MultiplayerDragAndDrop components
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            
            foreach (var answer in answers)
            {
                // Fix Image raycastTarget
                var image = answer.GetComponent<Image>();
                if (image != null && !image.raycastTarget)
                {
                    image.raycastTarget = true;
                    Debug.Log("[SimpleRaycastFixer] Fixed Answer Image: " + answer.name);
                }

                // Fix CanvasGroup blocksRaycasts
                var canvasGroup = answer.GetComponent<CanvasGroup>();
                if (canvasGroup != null && !canvasGroup.blocksRaycasts)
                {
                    canvasGroup.blocksRaycasts = true;
                    Debug.Log("[SimpleRaycastFixer] Fixed Answer CanvasGroup: " + answer.name);
                }
            }
        }

        [ContextMenu("Check Current State")]
        public void CheckCurrentState()
        {
            Debug.Log("========== RAYCAST STATE CHECK ==========");

            // Check Slots
            var slots = GameObject.FindGameObjectsWithTag("Slot");
            Debug.Log("Slots found: " + slots.Length);
            foreach (var slot in slots)
            {
                var image = slot.GetComponent<Image>();
                if (image != null)
                {
                    string status = image.raycastTarget ? "BLOCKING (BAD)" : "OK (GOOD)";
                    Debug.Log("  " + slot.name + ": raycastTarget=" + image.raycastTarget + " - " + status);
                }
            }

            // Check Answers
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            Debug.Log("Answer objects found: " + answers.Length);
            foreach (var answer in answers)
            {
                var image = answer.GetComponent<Image>();
                var canvasGroup = answer.GetComponent<CanvasGroup>();
                
                bool imageOK = image != null && image.raycastTarget;
                bool canvasGroupOK = canvasGroup != null && canvasGroup.blocksRaycasts;
                
                string status = (imageOK && canvasGroupOK) ? "OK (GOOD)" : "BROKEN (BAD)";
                Debug.Log("  " + answer.name + ": Image=" + imageOK + ", CanvasGroup=" + canvasGroupOK + " - " + status);
            }

            Debug.Log("==========================================");
        }
    }
}