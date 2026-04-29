using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using DoAnGame.UI;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Script kiểm tra và đảm bảo đồng bộ UI giữa Host và Client
    /// Attach vào GameplayPanel hoặc BattleManager
    /// </summary>
    public class MultiplayerSyncValidator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkedMathBattleManager battleManager;
        [SerializeField] private UIMultiplayerBattleController battleController;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private MultiplayerDragAndDrop[] answerChoices;

        [Header("Settings")]
        [SerializeField] private float syncCheckInterval = 1f;
        [SerializeField] private bool autoFixRaycast = true;
        [SerializeField] private bool enableDebugLogs = true;

        private float nextSyncCheck;
        private bool hasLoggedInitialState;

        private void Start()
        {
            // Auto-resolve references
            if (battleManager == null)
                battleManager = FindObjectOfType<NetworkedMathBattleManager>();

            if (battleController == null)
                battleController = FindObjectOfType<UIMultiplayerBattleController>();

            if (questionText == null)
            {
                var texts = FindObjectsOfType<TMP_Text>(true);
                foreach (var text in texts)
                {
                    if (text.name.Contains("Question") || text.name.Contains("Cau_hoi"))
                    {
                        questionText = text;
                        break;
                    }
                }
            }

            if (answerChoices == null || answerChoices.Length == 0)
            {
                answerChoices = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            }

            Log("✅ MultiplayerSyncValidator initialized");
        }

        private void Update()
        {
            if (Time.time < nextSyncCheck)
                return;

            nextSyncCheck = Time.time + syncCheckInterval;

            // Check sync state
            CheckSyncState();

            // Auto-fix raycast if enabled
            if (autoFixRaycast)
            {
                FixAnswerRaycast();
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái đồng bộ
        /// </summary>
        private void CheckSyncState()
        {
            if (battleManager == null)
            {
                LogWarning("BattleManager is NULL!");
                return;
            }

            var net = NetworkManager.Singleton;
            if (net == null || !net.IsConnectedClient)
            {
                if (!hasLoggedInitialState)
                {
                    LogWarning("Not connected to network");
                    hasLoggedInitialState = true;
                }
                return;
            }

            // Log initial state once
            if (!hasLoggedInitialState)
            {
                LogInitialState();
                hasLoggedInitialState = true;
            }

            // Check NetworkVariable values
            string currentQuestion = battleManager.CurrentQuestion.Value.ToString();
            int[] choices = new int[]
            {
                battleManager.Choice1.Value,
                battleManager.Choice2.Value,
                battleManager.Choice3.Value,
                battleManager.Choice4.Value
            };

            // Verify UI matches NetworkVariables
            bool uiMatches = VerifyUISync(currentQuestion, choices);

            if (!uiMatches)
            {
                LogWarning("⚠️ UI NOT SYNCED with NetworkVariables!");
                LogWarning($"  Question NV: '{currentQuestion}'");
                LogWarning($"  Question UI: '{questionText?.text}'");
                LogWarning($"  Choices NV: [{choices[0]}, {choices[1]}, {choices[2]}, {choices[3]}]");
                
                if (answerChoices != null)
                {
                    for (int i = 0; i < answerChoices.Length && i < 4; i++)
                    {
                        if (answerChoices[i] != null && answerChoices[i].myText != null)
                        {
                            LogWarning($"  Choice {i} UI: '{answerChoices[i].myText.text}'");
                        }
                    }
                }

                // Try to force sync
                ForceSyncUI();
            }
        }

        /// <summary>
        /// Log trạng thái ban đầu
        /// </summary>
        private void LogInitialState()
        {
            var net = NetworkManager.Singleton;
            string role = net.IsHost ? "HOST" : "CLIENT";
            ulong clientId = net.LocalClientId;

            Log($"========== INITIAL STATE ({role}, ClientID={clientId}) ==========");
            Log($"BattleManager: {(battleManager != null ? battleManager.name : "NULL")}");
            Log($"BattleController: {(battleController != null ? battleController.name : "NULL")}");
            Log($"QuestionText: {(questionText != null ? questionText.name : "NULL")}");
            Log($"AnswerChoices: {(answerChoices != null ? answerChoices.Length.ToString() : "NULL")}");

            if (battleManager != null)
            {
                Log($"NetworkVariables:");
                Log($"  CurrentQuestion: '{battleManager.CurrentQuestion.Value}'");
                Log($"  CorrectAnswer: {battleManager.CorrectAnswer.Value}");
                Log($"  Choices: [{battleManager.Choice1.Value}, {battleManager.Choice2.Value}, {battleManager.Choice3.Value}, {battleManager.Choice4.Value}]");
                Log($"  TimeRemaining: {battleManager.TimeRemaining.Value}");
                Log($"  MatchStarted: {battleManager.MatchStarted.Value}");
                Log($"  MatchEnded: {battleManager.MatchEnded.Value}");

                var p1 = battleManager.GetPlayer1State();
                var p2 = battleManager.GetPlayer2State();
                Log($"Player1: {(p1 != null ? $"HP={p1.CurrentHealth.Value}/{p1.MaxHealth.Value}, Score={p1.Score.Value}" : "NULL")}");
                Log($"Player2: {(p2 != null ? $"HP={p2.CurrentHealth.Value}/{p2.MaxHealth.Value}, Score={p2.Score.Value}" : "NULL")}");
            }

            Log($"UI State:");
            Log($"  QuestionText: '{questionText?.text}'");
            if (answerChoices != null)
            {
                for (int i = 0; i < answerChoices.Length; i++)
                {
                    if (answerChoices[i] != null && answerChoices[i].myText != null)
                    {
                        Log($"  Choice {i}: '{answerChoices[i].myText.text}', raycast={answerChoices[i].GetComponent<Image>()?.raycastTarget}");
                    }
                }
            }

            Log($"==========================================================");
        }

        /// <summary>
        /// Verify UI có khớp với NetworkVariables không
        /// </summary>
        private bool VerifyUISync(string expectedQuestion, int[] expectedChoices)
        {
            // Skip check if no question yet
            if (string.IsNullOrEmpty(expectedQuestion))
                return true;

            // Check question text
            if (questionText != null && questionText.text != expectedQuestion)
            {
                return false;
            }

            // Check answer choices
            if (answerChoices != null)
            {
                for (int i = 0; i < answerChoices.Length && i < expectedChoices.Length; i++)
                {
                    if (answerChoices[i] == null || answerChoices[i].myText == null)
                        continue;

                    string expectedText = expectedChoices[i].ToString();
                    if (answerChoices[i].myText.text != expectedText)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Force sync UI với NetworkVariables
        /// </summary>
        [ContextMenu("Force Sync UI")]
        public void ForceSyncUI()
        {
            if (battleManager == null)
            {
                LogWarning("Cannot force sync: BattleManager is NULL");
                return;
            }

            string question = battleManager.CurrentQuestion.Value.ToString();
            int[] choices = new int[]
            {
                battleManager.Choice1.Value,
                battleManager.Choice2.Value,
                battleManager.Choice3.Value,
                battleManager.Choice4.Value
            };

            // Update question text
            if (questionText != null)
            {
                questionText.text = question;
                Log($"✅ Synced question: {question}");
            }

            // Update answer choices
            if (answerChoices != null)
            {
                for (int i = 0; i < answerChoices.Length && i < choices.Length; i++)
                {
                    if (answerChoices[i] == null || answerChoices[i].myText == null)
                        continue;

                    answerChoices[i].myText.text = choices[i].ToString();
                    answerChoices[i].ForceResetPosition();
                    Log($"✅ Synced choice {i}: {choices[i]}");
                }
            }

            // Enable dragging
            DragAndDrop.SetGlobalLock(false);
            MultiplayerDragAndDrop.SetGlobalLock(false);

            Log("✅ Force sync completed");
        }

        /// <summary>
        /// Fix raycastTarget trên Answer objects
        /// </summary>
        [ContextMenu("Fix Answer Raycast")]
        public void FixAnswerRaycast()
        {
            if (answerChoices == null)
                return;

            int fixedCount = 0;

            foreach (var answer in answerChoices)
            {
                if (answer == null)
                    continue;

                var image = answer.GetComponent<Image>();
                if (image != null && !image.raycastTarget)
                {
                    image.raycastTarget = true;
                    fixedCount++;
                }

                var canvasGroup = answer.GetComponent<CanvasGroup>();
                if (canvasGroup != null && !canvasGroup.blocksRaycasts)
                {
                    canvasGroup.blocksRaycasts = true;
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                Log($"🔧 Fixed raycast on {fixedCount} components");
            }
        }

        /// <summary>
        /// Kiểm tra tất cả NetworkVariable subscriptions
        /// </summary>
        [ContextMenu("Check NetworkVariable Subscriptions")]
        public void CheckNetworkVariableSubscriptions()
        {
            if (battleManager == null)
            {
                LogWarning("BattleManager is NULL");
                return;
            }

            Log("========== NetworkVariable Subscriptions ==========");
            
            // Check if NetworkObject is spawned
            var netObj = battleManager.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                Log($"NetworkObject IsSpawned: {netObj.IsSpawned}");
                Log($"NetworkObject NetworkObjectId: {netObj.NetworkObjectId}");
            }
            else
            {
                LogWarning("NetworkObject component NOT FOUND on BattleManager!");
            }

            // Check NetworkVariable values
            Log($"CurrentQuestion: '{battleManager.CurrentQuestion.Value}'");
            Log($"CorrectAnswer: {battleManager.CorrectAnswer.Value}");
            Log($"Choice1: {battleManager.Choice1.Value}");
            Log($"Choice2: {battleManager.Choice2.Value}");
            Log($"Choice3: {battleManager.Choice3.Value}");
            Log($"Choice4: {battleManager.Choice4.Value}");
            Log($"TimeRemaining: {battleManager.TimeRemaining.Value}");
            Log($"MatchStarted: {battleManager.MatchStarted.Value}");
            Log($"MatchEnded: {battleManager.MatchEnded.Value}");

            Log("===================================================");
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[SyncValidator] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SyncValidator] {message}");
        }
    }
}
