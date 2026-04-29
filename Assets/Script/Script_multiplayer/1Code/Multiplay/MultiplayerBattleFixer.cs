using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using DoAnGame.UI;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Script để FORCE ENABLE raycastTarget trên Answer objects
    /// Chạy SAU KHI các script khác đã disable
    /// Attach vào GameplayPanel
    /// </summary>
    public class MultiplayerBattleFixer : MonoBehaviour
    {
        [Header("Auto Fix Settings")]
        [SerializeField] private bool autoFixSlotRaycast = true;
        [SerializeField] private bool autoFixAnswerRaycast = true;
        [SerializeField] private bool autoFixHealthUI = true;
        [SerializeField] private float fixInterval = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private float nextFixTime;

        private void Update()
        {
            if (Time.time < nextFixTime)
                return;

            nextFixTime = Time.time + fixInterval;

            if (autoFixSlotRaycast)
                FixSlotRaycast();

            if (autoFixAnswerRaycast)
                FixAnswerRaycast();

            if (autoFixHealthUI)
                CheckHealthUISync();
        }

        /// <summary>
        /// FIX 1: Tắt raycastTarget trên Slot
        /// Slot KHÔNG NÊN có raycastTarget = true vì sẽ chặn drag-drop
        /// </summary>
        [ContextMenu("Fix Slot Raycast")]
        public void FixSlotRaycast()
        {
            // Tìm tất cả objects có tag "Slot"
            var slots = GameObject.FindGameObjectsWithTag("Slot");

            if (slots == null || slots.Length == 0)
            {
                // Fallback: Tìm theo tên
                var allObjects = FindObjectsOfType<GameObject>(true);
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("Slot") || obj.name.Contains("slot"))
                    {
                        FixSlotObject(obj);
                    }
                }
                return;
            }

            foreach (var slot in slots)
            {
                FixSlotObject(slot);
            }
        }

        private void FixSlotObject(GameObject slot)
        {
            if (slot == null)
                return;

            var image = slot.GetComponent<Image>();
            if (image != null && image.raycastTarget)
            {
                image.raycastTarget = false;
                Log("Fixed Slot raycast: " + slot.name);
            }

            // Tắt raycast trên tất cả children của Slot
            var childImages = slot.GetComponentsInChildren<Image>(true);
            foreach (var img in childImages)
            {
                if (img != null && img.raycastTarget)
                {
                    // Kiểm tra xem có phải Answer object không
                    if (img.GetComponent<MultiplayerDragAndDrop>() != null)
                        continue; // Giữ raycast cho Answer

                    img.raycastTarget = false;
                    Log("Fixed Slot child raycast: " + img.name);
                }
            }
        }

        /// <summary>
        /// FIX 2: Bật raycastTarget trên Answer objects
        /// </summary>
        [ContextMenu("Fix Answer Raycast")]
        public void FixAnswerRaycast()
        {
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);

            foreach (var answer in answers)
            {
                if (answer == null)
                    continue;

                bool fixedSomething = false;

                var image = answer.GetComponent<Image>();
                if (image != null && !image.raycastTarget)
                {
                    image.raycastTarget = true;
                    fixedSomething = true;
                }

                var canvasGroup = answer.GetComponent<CanvasGroup>();
                if (canvasGroup != null && !canvasGroup.blocksRaycasts)
                {
                    canvasGroup.blocksRaycasts = true;
                    fixedSomething = true;
                }

                if (fixedSomething)
                {
                    Log("Fixed Answer raycast: " + answer.name);
                }
            }
        }

        /// <summary>
        /// FIX 3: Kiểm tra Health UI sync
        /// </summary>
        [ContextMenu("Check Health UI Sync")]
        public void CheckHealthUISync()
        {
            var battleManager = NetworkedMathBattleManager.Instance;
            if (battleManager == null)
                return;

            var net = NetworkManager.Singleton;
            if (net == null || !net.IsConnectedClient)
                return;

            var p1 = battleManager.GetPlayer1State();
            var p2 = battleManager.GetPlayer2State();

            // Kiểm tra xem player states đã spawn chưa
            if (p1 == null || p2 == null)
            {
                LogWarning("Player states chưa spawn!");
                LogWarning("  Player1State: " + (p1 != null ? "OK" : "NULL"));
                LogWarning("  Player2State: " + (p2 != null ? "OK" : "NULL"));
                return;
            }

            // Kiểm tra health values
            int p1Health = p1.CurrentHealth.Value;
            int p1MaxHealth = p1.MaxHealth.Value;
            int p2Health = p2.CurrentHealth.Value;
            int p2MaxHealth = p2.MaxHealth.Value;

            if (p1MaxHealth == 0 || p2MaxHealth == 0)
            {
                LogWarning("Player health chưa được khởi tạo!");
                LogWarning("  Player1: " + p1Health + "/" + p1MaxHealth);
                LogWarning("  Player2: " + p2Health + "/" + p2MaxHealth);

                // Try to force re-init HealthUI
                var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
                if (healthUI != null)
                {
                    Log("Forcing HealthUI re-init...");
                    healthUI.SendMessage("RetryInit", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        /// <summary>
        /// Kiểm tra toàn bộ setup
        /// </summary>
        [ContextMenu("Check Full Setup")]
        public void CheckFullSetup()
        {
            Log("========== CHECKING MULTIPLAYER BATTLE SETUP ==========");

            // 1. Check NetworkManager
            var net = NetworkManager.Singleton;
            if (net == null)
            {
                LogWarning("NetworkManager is NULL!");
            }
            else
            {
                Log("NetworkManager: IsHost=" + net.IsHost + ", IsClient=" + net.IsClient + ", IsServer=" + net.IsServer);
                Log("   Connected clients: " + net.ConnectedClientsIds.Count);
            }

            // 2. Check BattleManager
            var battleManager = NetworkedMathBattleManager.Instance;
            if (battleManager == null)
            {
                LogWarning("BattleManager is NULL!");
            }
            else
            {
                Log("BattleManager found");
                var netObj = battleManager.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    Log("   NetworkObject IsSpawned: " + netObj.IsSpawned);
                }
                else
                {
                    LogWarning("   NO NetworkObject component!");
                }

                var p1 = battleManager.GetPlayer1State();
                var p2 = battleManager.GetPlayer2State();
                Log("   Player1State: " + (p1 != null ? ("HP=" + p1.CurrentHealth.Value + "/" + p1.MaxHealth.Value) : "NULL"));
                Log("   Player2State: " + (p2 != null ? ("HP=" + p2.CurrentHealth.Value + "/" + p2.MaxHealth.Value) : "NULL"));
            }

            // 3. Check EventSystem
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                LogWarning("EventSystem is NULL!");
            }
            else
            {
                Log("EventSystem: " + es.name + ", enabled=" + es.enabled);
            }

            // 4. Check Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                LogWarning("Canvas not found!");
            }
            else
            {
                Log("Canvas: " + canvas.name + ", renderMode=" + canvas.renderMode);
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    LogWarning("   NO GraphicRaycaster!");
                }
                else
                {
                    Log("   GraphicRaycaster: enabled=" + raycaster.enabled);
                }
            }

            // 5. Check Slot
            var slots = GameObject.FindGameObjectsWithTag("Slot");
            Log("Slots found: " + slots.Length);
            foreach (var slot in slots)
            {
                var image = slot.GetComponent<Image>();
                if (image != null)
                {
                    string status = image.raycastTarget ? "BLOCKING" : "OK";
                    Log("   " + slot.name + ": raycastTarget=" + image.raycastTarget + " " + status);
                }
            }

            // 6. Check Answer objects
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            Log("Answer objects found: " + answers.Length);
            foreach (var answer in answers)
            {
                var image = answer.GetComponent<Image>();
                var canvasGroup = answer.GetComponent<CanvasGroup>();
                
                bool raycastOK = image != null && image.raycastTarget;
                bool blocksRaycastsOK = canvasGroup != null && canvasGroup.blocksRaycasts;
                
                string status = (raycastOK && blocksRaycastsOK) ? "OK" : "BROKEN";
                Log("   " + answer.name + ": raycast=" + raycastOK + ", blocksRaycasts=" + blocksRaycastsOK + " " + status);
            }

            // 7. Check HealthUI
            var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
            if (healthUI == null)
            {
                LogWarning("MultiplayerHealthUI not found!");
            }
            else
            {
                Log("MultiplayerHealthUI: " + healthUI.name);
            }

            Log("=======================================================");
        }

        /// <summary>
        /// Fix tất cả vấn đề
        /// </summary>
        [ContextMenu("Fix All Issues")]
        public void FixAllIssues()
        {
            Log("========== FIXING ALL ISSUES ==========");
            
            FixSlotRaycast();
            FixAnswerRaycast();
            CheckHealthUISync();
            
            Log("All fixes applied!");
            Log("=======================================");
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log("[BattleFixer] " + message);
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning("[BattleFixer] " + message);
        }
    }
}