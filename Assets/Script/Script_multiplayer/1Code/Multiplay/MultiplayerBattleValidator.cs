using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using DoAnGame.UI;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Script tổng hợp để validate và fix tất cả vấn đề multiplayer battle
    /// Chạy tự động và có thể trigger manual
    /// </summary>
    public class MultiplayerBattleValidator : MonoBehaviour
    {
        [Header("Auto Validation")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool runPeriodically = true;
        [SerializeField] private float validationInterval = 2f;

        [Header("Fix Options")]
        [SerializeField] private bool autoFixIssues = true;
        [SerializeField] private bool showDetailedLogs = true;

        private float nextValidationTime;
        private bool hasRunInitialValidation;

        private void Start()
        {
            if (runOnStart)
            {
                Invoke("RunFullValidation", 1f); // Delay để đảm bảo setup xong
            }
        }

        private void Update()
        {
            if (!runPeriodically || hasRunInitialValidation == false)
                return;

            if (Time.time < nextValidationTime)
                return;

            nextValidationTime = Time.time + validationInterval;
            RunQuickValidation();
        }

        /// <summary>
        /// Chạy validation đầy đủ (gọi từ Context Menu hoặc Start)
        /// </summary>
        [ContextMenu("Run Full Validation")]
        public void RunFullValidation()
        {
            Log("========== FULL MULTIPLAYER BATTLE VALIDATION ==========");

            var issues = new List<string>();

            // 1. Validate Network Setup
            ValidateNetworkSetup(issues);

            // 2. Validate Battle Manager
            ValidateBattleManager(issues);

            // 3. Validate UI Components
            ValidateUIComponents(issues);

            // 4. Validate Drag-Drop System
            ValidateDragDropSystem(issues);

            // 5. Validate Health System
            ValidateHealthSystem(issues);

            // Summary
            if (issues.Count == 0)
            {
                Log("ALL VALIDATIONS PASSED! Multiplayer battle is ready.");
            }
            else
            {
                LogWarning("FOUND " + issues.Count + " ISSUES:");
                foreach (var issue in issues)
                {
                    LogWarning("  • " + issue);
                }

                if (autoFixIssues)
                {
                    Log("Attempting to auto-fix issues...");
                    AutoFixIssues();
                }
            }

            Log("=========================================================");
            hasRunInitialValidation = true;
        }

        /// <summary>
        /// Chạy validation nhanh (chỉ kiểm tra các vấn đề quan trọng)
        /// </summary>
        public void RunQuickValidation()
        {
            var issues = new List<string>();

            // Quick checks
            var net = NetworkManager.Singleton;
            if (net == null || !net.IsConnectedClient)
                return; // Skip nếu không connected

            var battleManager = NetworkedMathBattleManager.Instance;
            if (battleManager == null)
            {
                issues.Add("BattleManager is NULL");
            }
            else
            {
                // Check player states
                var p1 = battleManager.GetPlayer1State();
                var p2 = battleManager.GetPlayer2State();
                
                if (p1 == null || p2 == null)
                {
                    issues.Add("Player states not spawned");
                }
                else if (p1.MaxHealth.Value == 0 || p2.MaxHealth.Value == 0)
                {
                    issues.Add("Player health not initialized");
                }
            }

            // Check drag-drop
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            foreach (var answer in answers)
            {
                var image = answer.GetComponent<Image>();
                if (image != null && !image.raycastTarget)
                {
                    issues.Add("Answer " + answer.name + " raycast disabled");
                    break; // Chỉ báo 1 lần
                }
            }

            // Auto-fix nếu có issues
            if (issues.Count > 0 && autoFixIssues)
            {
                AutoFixIssues();
            }
        }

        private void ValidateNetworkSetup(List<string> issues)
        {
            Log("--- Validating Network Setup ---");

            var net = NetworkManager.Singleton;
            if (net == null)
            {
                issues.Add("NetworkManager is NULL");
                return;
            }

            Log("NetworkManager: IsHost=" + net.IsHost + ", IsClient=" + net.IsClient);
            Log("   Connected clients: " + net.ConnectedClientsIds.Count);

            if (!net.IsConnectedClient)
            {
                issues.Add("Not connected to network");
            }

            // Check EventSystem
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                issues.Add("EventSystem is NULL");
            }
            else if (!es.enabled)
            {
                issues.Add("EventSystem is disabled");
            }
            else
            {
                Log("EventSystem is ready");
            }

            // Check Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                issues.Add("Canvas not found");
            }
            else
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    issues.Add("Canvas missing GraphicRaycaster");
                }
                else if (!raycaster.enabled)
                {
                    issues.Add("GraphicRaycaster is disabled");
                }
                else
                {
                    Log("Canvas and GraphicRaycaster are ready");
                }
            }
        }

        private void ValidateBattleManager(List<string> issues)
        {
            Log("--- Validating Battle Manager ---");

            var battleManager = NetworkedMathBattleManager.Instance;
            if (battleManager == null)
            {
                issues.Add("BattleManager is NULL");
                return;
            }

            Log("BattleManager found");

            // Check NetworkObject
            var netObj = battleManager.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                issues.Add("BattleManager missing NetworkObject component");
            }
            else if (!netObj.IsSpawned)
            {
                issues.Add("BattleManager NetworkObject not spawned");
            }
            else
            {
                Log("BattleManager NetworkObject is spawned");
            }

            // Check player states
            var p1 = battleManager.GetPlayer1State();
            var p2 = battleManager.GetPlayer2State();

            if (p1 == null)
            {
                issues.Add("Player1State is NULL");
            }
            else
            {
                Log("Player1State: HP=" + p1.CurrentHealth.Value + "/" + p1.MaxHealth.Value);
                if (p1.MaxHealth.Value == 0)
                {
                    issues.Add("Player1 health not initialized");
                }
            }

            if (p2 == null)
            {
                issues.Add("Player2State is NULL");
            }
            else
            {
                Log("Player2State: HP=" + p2.CurrentHealth.Value + "/" + p2.MaxHealth.Value);
                if (p2.MaxHealth.Value == 0)
                {
                    issues.Add("Player2 health not initialized");
                }
            }
        }

        private void ValidateUIComponents(List<string> issues)
        {
            Log("--- Validating UI Components ---");

            // Check UIMultiplayerBattleController
            var battleController = FindObjectOfType<UIMultiplayerBattleController>(true);
            if (battleController == null)
            {
                issues.Add("UIMultiplayerBattleController not found");
            }
            else
            {
                Log("UIMultiplayerBattleController found");
            }

            // Check MultiplayerHealthUI
            var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
            if (healthUI == null)
            {
                issues.Add("MultiplayerHealthUI not found");
            }
            else
            {
                Log("MultiplayerHealthUI found");
            }

            // Check question text
            var questionTexts = FindObjectsOfType<TMP_Text>(true);
            bool foundQuestionText = false;
            foreach (var text in questionTexts)
            {
                if (text.name.Contains("Question") || text.name.Contains("Cau_hoi"))
                {
                    foundQuestionText = true;
                    Log("Question text found: " + text.name);
                    break;
                }
            }

            if (!foundQuestionText)
            {
                issues.Add("Question text component not found");
            }
        }

        private void ValidateDragDropSystem(List<string> issues)
        {
            Log("--- Validating Drag-Drop System ---");

            // Check Answer objects
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            Log("Found " + answers.Length + " Answer objects");

            if (answers.Length == 0)
            {
                issues.Add("No MultiplayerDragAndDrop components found");
                return;
            }

            int brokenAnswers = 0;
            foreach (var answer in answers)
            {
                bool isValid = true;

                var image = answer.GetComponent<Image>();
                if (image == null)
                {
                    issues.Add("Answer " + answer.name + " missing Image component");
                    isValid = false;
                }
                else if (!image.raycastTarget)
                {
                    issues.Add("Answer " + answer.name + " raycastTarget disabled");
                    isValid = false;
                }

                var canvasGroup = answer.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    issues.Add("Answer " + answer.name + " missing CanvasGroup component");
                    isValid = false;
                }
                else if (!canvasGroup.blocksRaycasts)
                {
                    issues.Add("Answer " + answer.name + " blocksRaycasts disabled");
                    isValid = false;
                }

                var textComponent = answer.GetComponent<TMP_Text>();
                if (textComponent == null)
                {
                    // Check myText field
                    if (answer.myText == null)
                    {
                        issues.Add("Answer " + answer.name + " missing text component");
                        isValid = false;
                    }
                }

                if (!isValid)
                {
                    brokenAnswers++;
                }
            }

            if (brokenAnswers == 0)
            {
                Log("All Answer objects are valid");
            }

            // Check Slot
            var slots = GameObject.FindGameObjectsWithTag("Slot");
            if (slots.Length == 0)
            {
                issues.Add("No Slot objects found (missing 'Slot' tag)");
            }
            else
            {
                bool hasSlotIssue = false;
                foreach (var slot in slots)
                {
                    var image = slot.GetComponent<Image>();
                    if (image != null && image.raycastTarget)
                    {
                        issues.Add("Slot " + slot.name + " has raycastTarget enabled (will block drag-drop)");
                        hasSlotIssue = true;
                    }
                }

                if (!hasSlotIssue)
                {
                    Log("Slot objects are valid");
                }
            }
        }

        private void ValidateHealthSystem(List<string> issues)
        {
            Log("--- Validating Health System ---");

            var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
            if (healthUI == null)
            {
                issues.Add("MultiplayerHealthUI component not found");
                return;
            }

            // Check if health UI is properly initialized
            var battleManager = NetworkedMathBattleManager.Instance;
            if (battleManager != null)
            {
                var p1 = battleManager.GetPlayer1State();
                var p2 = battleManager.GetPlayer2State();

                if (p1 != null && p2 != null)
                {
                    if (p1.MaxHealth.Value > 0 && p2.MaxHealth.Value > 0)
                    {
                        Log("Health system is initialized");
                    }
                    else
                    {
                        issues.Add("Player health values not initialized");
                    }
                }
            }
        }

        /// <summary>
        /// Tự động fix các vấn đề phổ biến
        /// </summary>
        [ContextMenu("Auto Fix Issues")]
        public void AutoFixIssues()
        {
            Log("Auto-fixing common issues...");

            // Fix 1: Slot raycast
            var slots = GameObject.FindGameObjectsWithTag("Slot");
            foreach (var slot in slots)
            {
                var image = slot.GetComponent<Image>();
                if (image != null && image.raycastTarget)
                {
                    image.raycastTarget = false;
                    Log("Fixed Slot raycast: " + slot.name);
                }
            }

            // Fix 2: Answer raycast
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            foreach (var answer in answers)
            {
                var image = answer.GetComponent<Image>();
                if (image != null && !image.raycastTarget)
                {
                    image.raycastTarget = true;
                    Log("Fixed Answer raycast: " + answer.name);
                }

                var canvasGroup = answer.GetComponent<CanvasGroup>();
                if (canvasGroup != null && !canvasGroup.blocksRaycasts)
                {
                    canvasGroup.blocksRaycasts = true;
                    Log("Fixed Answer blocksRaycasts: " + answer.name);
                }
            }

            // Fix 3: Health UI
            var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
            if (healthUI != null)
            {
                healthUI.SendMessage("RetryInit", SendMessageOptions.DontRequireReceiver);
                Log("Triggered HealthUI re-initialization");
            }

            // Fix 4: EventSystem
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                var go = new GameObject("EventSystem (Auto-Created)");
                go.AddComponent<UnityEngine.EventSystems.EventSystem>();
                go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Log("Created EventSystem");
            }
            else if (!es.enabled)
            {
                es.enabled = true;
                Log("Enabled EventSystem");
            }

            // Fix 5: GraphicRaycaster
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Log("Added GraphicRaycaster to Canvas");
                }
                else if (!raycaster.enabled)
                {
                    raycaster.enabled = true;
                    Log("Enabled GraphicRaycaster");
                }
            }

            Log("Auto-fix completed!");
        }

        private void Log(string message)
        {
            if (!showDetailedLogs)
                return;

            Debug.Log("[BattleValidator] " + message);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning("[BattleValidator] " + message);
        }
    }
}