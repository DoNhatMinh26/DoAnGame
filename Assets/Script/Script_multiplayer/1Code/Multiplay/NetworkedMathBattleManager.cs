using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Controller chính cho multiplayer math battle.
    /// Quản lý flow trận đấu, sinh câu hỏi, xử lý đáp án.
    /// </summary>
    public class NetworkedMathBattleManager : NetworkBehaviour
    {
        public static NetworkedMathBattleManager Instance { get; private set; }

        [Header("=== CONFIGURATION ===")]
        [SerializeField] private GameRulesConfig gameRules;
        [SerializeField] private LevelGenerate levelData;

        [Header("=== PLAYER REFERENCES ===")]
        [Tooltip("Prefab của NetworkedPlayerState (phải có NetworkObject)")]
        [SerializeField] private GameObject playerStatePrefab;

        [Tooltip("References đến 2 player states (tự động assign khi spawn)")]
        private NetworkedPlayerState player1State;
        private NetworkedPlayerState player2State;

        [Header("=== QUESTION SYSTEM ===")]
        public NetworkVariable<FixedString512Bytes> CurrentQuestion = new NetworkVariable<FixedString512Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<int> CorrectAnswer = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<int> CurrentDifficulty = new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<int> CurrentGrade = new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [Header("=== TIMER SYSTEM ===")]
        public NetworkVariable<long> QuestionStartTimestamp = new NetworkVariable<long>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private Coroutine timerRoutine;

        [Header("=== MATCH STATE ===")]
        public NetworkVariable<bool> MatchStarted = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<bool> MatchEnded = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<int> WinnerId = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [Header("=== ANSWER CHOICES ===")]
        public NetworkVariable<int> Choice1 = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<int> Choice2 = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<int> Choice3 = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        public NetworkVariable<int> Choice4 = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Internal tracking
        private Dictionary<ulong, (int answer, long timestamp)> playerAnswers = new Dictionary<ulong, (int, long)>();
        private int correctAnswersInRow = 0;

        // Events
        public event Action<string, int[]> OnQuestionGenerated; // (question, choices)
        public event Action<int, bool, long> OnAnswerResult; // (playerId, isCorrect, responseTimeMs)
        public event Action<int, int> OnMatchEnded; // (winnerId, winnerHealth)
        public event Action<int, bool, long, long, int, int> OnAnswerResultReceived; // (winnerId, correct, player1ResponseTimeMs, player2ResponseTimeMs, player1Answer, player2Answer)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Validate references
            if (gameRules == null)
            {
                Debug.LogError("[BattleManager] GameRulesConfig chưa được gán!");
            }

            if (levelData == null)
            {
                Debug.LogError("[BattleManager] LevelGenerate chưa được gán!");
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                Debug.Log("[BattleManager] Server spawned, waiting for initialization...");
            }
            else
            {
                Debug.Log("[BattleManager] Client spawned, waiting for player states...");
                // Client sẽ gửi tên sau khi player states đã spawn
                // (được trigger từ StartMatch hoặc khi detect player states)
            }
        }

        /// <summary>
        /// Client gửi tên của mình lên server
        /// </summary>
        private void SendPlayerNameToServer()
        {
            if (IsServer) return;

            string playerName = "Player 2"; // Default

            var authManager = AuthManager.Instance;
            if (authManager != null)
            {
                var playerData = authManager.GetCurrentPlayerData();
                if (playerData != null && !string.IsNullOrWhiteSpace(playerData.characterName))
                {
                    playerName = playerData.characterName;
                }
                else
                {
                    string charName = authManager.GetCharacterName();
                    if (!string.IsNullOrWhiteSpace(charName) && charName != "Unknown")
                    {
                        playerName = charName;
                    }
                }
            }

            Debug.Log($"[BattleManager] Client sending name: {playerName} (ClientID: {NetworkManager.Singleton.LocalClientId})");
            UpdatePlayerNameServerRpc(playerName);
        }

        /// <summary>
        /// Update tên player (gọi từ client)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void UpdatePlayerNameServerRpc(string playerName, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            Debug.Log($"[BattleManager] Server received name update from client {clientId}: {playerName}");
            
            // Tìm player state của client này
            NetworkedPlayerState playerState = null;
            if (clientId == 0)
            {
                playerState = player1State;
                Debug.Log($"[BattleManager] Updating Player 1 name (client 0)");
            }
            else
            {
                playerState = player2State;
                Debug.Log($"[BattleManager] Updating Player 2 name (client {clientId})");
            }

            if (playerState != null)
            {
                playerState.PlayerName.Value = playerName;
                Debug.Log($"[BattleManager] ✅ Updated player name for client {clientId}: {playerName}");
            }
            else
            {
                Debug.LogWarning($"[BattleManager] ⚠️ Player state not found for client {clientId}! Retrying in 1 second...");
                // Retry sau 1 giây
                StartCoroutine(RetryUpdatePlayerName(clientId, playerName));
            }
        }

        private System.Collections.IEnumerator RetryUpdatePlayerName(ulong clientId, string playerName)
        {
            yield return new WaitForSeconds(1f);
            
            NetworkedPlayerState playerState = clientId == 0 ? player1State : player2State;
            
            if (playerState != null)
            {
                playerState.PlayerName.Value = playerName;
                Debug.Log($"[BattleManager] ✅ [RETRY] Updated player name for client {clientId}: {playerName}");
            }
            else
            {
                Debug.LogError($"[BattleManager] ❌ [RETRY FAILED] Player state still not found for client {clientId}!");
            }
        }

        /// <summary>
        /// Khởi tạo trận đấu (gọi từ UIMultiplayerRoomController sau khi vào battle scene)
        /// </summary>
        public void InitializeBattle(int grade)
        {
            // Kiểm tra NetworkManager thay vì IsServer (vì BattleManager không có NetworkObject)
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[BattleManager] InitializeBattle chỉ được gọi trên Server!");
                return;
            }

            CurrentGrade.Value = Mathf.Clamp(grade, 1, 5);
            CurrentDifficulty.Value = 1; // Bắt đầu từ level 1
            correctAnswersInRow = 0;

            Debug.Log($"[BattleManager] ✅ Initializing battle: Grade {grade}, Difficulty {CurrentDifficulty.Value}");

            // Spawn player states
            SpawnPlayerStates();

            // Bắt đầu trận đấu sau 2 giây
            Invoke(nameof(StartMatch), 2f);
        }

        /// <summary>
        /// Spawn NetworkedPlayerState cho 2 người chơi
        /// </summary>
        private void SpawnPlayerStates()
        {
            // Kiểm tra NetworkManager thay vì IsServer
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            if (playerStatePrefab == null)
            {
                Debug.LogError("[BattleManager] playerStatePrefab chưa được gán!");
                return;
            }

            Debug.Log("[BattleManager] 🎮 Spawning player states...");

            // Lấy tên người chơi từ AuthManager
            string player1Name = "Player 1"; // Default
            string player2Name = "Player 2"; // Default

            // Lấy tên Player 1 (Host)
            var authManager = AuthManager.Instance;
            if (authManager != null)
            {
                var playerData = authManager.GetCurrentPlayerData();
                if (playerData != null && !string.IsNullOrWhiteSpace(playerData.characterName))
                {
                    player1Name = playerData.characterName;
                }
                else
                {
                    string charName = authManager.GetCharacterName();
                    if (!string.IsNullOrWhiteSpace(charName) && charName != "Unknown")
                    {
                        player1Name = charName;
                    }
                }
            }

            Debug.Log($"[BattleManager] Player 1 name: {player1Name}");

            // Spawn Player 1 (Host)
            GameObject p1Obj = Instantiate(playerStatePrefab);
            NetworkObject p1NetObj = p1Obj.GetComponent<NetworkObject>();
            p1NetObj.SpawnWithOwnership(0); // Owner = Host
            player1State = p1Obj.GetComponent<NetworkedPlayerState>();
            player1State.InitializeServerRpc(0, player1Name, gameRules.startingHealth);

            Debug.Log("[BattleManager] ✅ Player 1 spawned");

            // Spawn Player 2 (Client)
            GameObject p2Obj = Instantiate(playerStatePrefab);
            NetworkObject p2NetObj = p2Obj.GetComponent<NetworkObject>();
            
            // Tìm client ID (thường là 1)
            ulong clientId = NetworkManager.Singleton.ConnectedClientsIds.FirstOrDefault(id => id != 0);
            p2NetObj.SpawnWithOwnership(clientId);
            player2State = p2Obj.GetComponent<NetworkedPlayerState>();
            
            // Player 2 name sẽ được set từ client (vì chỉ client biết tên của mình)
            player2State.InitializeServerRpc(1, player2Name, gameRules.startingHealth);

            Debug.Log($"[BattleManager] ✅ Player 2 spawned (ClientID: {clientId})");

            Debug.Log($"[BattleManager] Spawned player states: P1={player1State.OwnerClientId}, P2={player2State.OwnerClientId}");
        }

        /// <summary>
        /// Bắt đầu trận đấu
        /// </summary>
        private void StartMatch()
        {
            // Kiểm tra NetworkManager thay vì IsServer
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            MatchStarted.Value = true;
            Debug.Log("[BattleManager] ⚔️ Match started!");

            // Notify clients để gửi tên (sau khi player states đã spawn)
            NotifyPlayerStatesReadyClientRpc();

            // Sinh câu hỏi đầu tiên
            GenerateQuestion();
        }

        /// <summary>
        /// Notify clients rằng player states đã sẵn sàng
        /// </summary>
        [ClientRpc]
        private void NotifyPlayerStatesReadyClientRpc()
        {
            if (IsServer) return; // Server không cần gửi tên (đã set trong SpawnPlayerStates)

            Debug.Log("[BattleManager] Client received player states ready notification");
            
            // Delay nhỏ để đảm bảo NetworkObjects đã replicate
            Invoke(nameof(SendPlayerNameToServer), 0.5f);
        }

        /// <summary>
        /// Sinh câu hỏi mới (chỉ chạy trên Server)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void GenerateQuestionServerRpc()
        {
            GenerateQuestion();
        }

        private void GenerateQuestion()
        {
            // Kiểm tra NetworkManager thay vì IsServer
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            Debug.Log("[BattleManager] 📝 Generating new question...");

            // Reset answer tracking
            playerAnswers.Clear();
            player1State?.ResetAnswerState();
            player2State?.ResetAnswerState();

            // Lấy config từ LevelGenerate
            var config = levelData.GetConfigForLevel(CurrentGrade.Value, CurrentDifficulty.Value);

            if (config == null || config.AllowedOperators == null || config.AllowedOperators.Length == 0)
            {
                Debug.LogError($"[BattleManager] Không lấy được config cho Grade {CurrentGrade.Value}, Level {CurrentDifficulty.Value}");
                return;
            }

            // Sinh câu hỏi (logic từ MathManager)
            string questionText = GenerateQuestionFromConfig(config, out int correctAnswer);

            // Sinh các đáp án sai
            int[] choices = GenerateChoices(correctAnswer, config.MinNumber, config.MaxNumber);

            // Broadcast câu hỏi
            CurrentQuestion.Value = questionText;
            CorrectAnswer.Value = correctAnswer;
            Choice1.Value = choices[0];
            Choice2.Value = choices[1];
            Choice3.Value = choices[2];
            Choice4.Value = choices[3];

            // Set timestamp
            QuestionStartTimestamp.Value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            TimeRemaining.Value = gameRules.questionTimeLimit;

            Debug.Log($"[BattleManager] Generated question: {questionText} = {correctAnswer}");

            // Bắt đầu đếm ngược
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
            }
            timerRoutine = StartCoroutine(QuestionTimerRoutine());

            // Notify clients
            NotifyQuestionGeneratedClientRpc(questionText, choices);
        }

        /// <summary>
        /// Sinh câu hỏi từ config (logic từ MathManager.cs)
        /// </summary>
        private string GenerateQuestionFromConfig(LevelParameters config, out int correctAnswer)
        {
            // Chọn phép toán ngẫu nhiên từ AllowedOperators
            byte opCode = config.AllowedOperators[UnityEngine.Random.Range(0, config.AllowedOperators.Length)];

            int n1, n2, n3;
            string questionText = "";
            correctAnswer = 0;

            switch (opCode)
            {
                case 0: // Cộng
                    n1 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    n2 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    correctAnswer = n1 + n2;
                    questionText = $"{n1} + {n2} = ?";
                    break;

                case 1: // Trừ
                    n1 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    n2 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                    correctAnswer = n1 - n2;
                    questionText = $"{n1} - {n2} = ?";
                    break;

                case 2: // Nhân
                    if (CurrentGrade.Value >= 4)
                    {
                        n1 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                        n2 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    }
                    else
                    {
                        n1 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                        n2 = UnityEngine.Random.Range(2, 10);
                    }
                    correctAnswer = n1 * n2;
                    questionText = $"{n1} x {n2} = ?";
                    break;

                case 3: // Chia
                    int divisor, quotient;
                    if (CurrentGrade.Value >= 4)
                    {
                        divisor = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                        quotient = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                        if (divisor == 0) divisor = 1;
                    }
                    else
                    {
                        quotient = UnityEngine.Random.Range(2, 10);
                        divisor = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    }
                    correctAnswer = quotient;
                    questionText = $"{divisor * quotient} : {divisor} = ?";
                    break;

                case 4: // Tìm x trong cộng/trừ
                    int x = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    int b = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        int sum = x + b;
                        correctAnswer = x;
                        questionText = (UnityEngine.Random.value > 0.5f) ? $"? + {b} = {sum}" : $"{b} + ? = {sum}";
                    }
                    else
                    {
                        correctAnswer = x;
                        if (UnityEngine.Random.value > 0.5f)
                        {
                            int diff = x - b;
                            if (x < b) { x = b; b = x - diff; }
                            questionText = $"? - {b} = {diff}";
                        }
                        else
                        {
                            int a = x + b;
                            questionText = $"{a} - ? = {b}";
                        }
                    }
                    break;

                case 5: // Tìm x trong nhân
                    int xMul = (CurrentGrade.Value >= 4) ? UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1) : UnityEngine.Random.Range(2, 10);
                    int bMul = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    int product = xMul * bMul;
                    correctAnswer = xMul;
                    questionText = (UnityEngine.Random.value > 0.5f) ? $"? x {bMul} = {product}" : $"{bMul} x ? = {product}";
                    break;

                case 6: // Tìm x trong chia
                    int xDiv = (CurrentGrade.Value >= 4) ? UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1) : UnityEngine.Random.Range(2, 10);
                    int bDiv = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    if (bDiv == 0) bDiv = 1;
                    int dividend = xDiv * bDiv;
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        correctAnswer = dividend;
                        questionText = $"? : {bDiv} = {xDiv}";
                    }
                    else
                    {
                        correctAnswer = bDiv;
                        questionText = $"{dividend} : ? = {xDiv}";
                    }
                    break;

                case 7: // Hai phép tính +-
                    n1 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    n2 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    n3 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        correctAnswer = n1 + n2 - n3;
                        questionText = $"{n1} + {n2} - {n3} = ?";
                        if (correctAnswer < 0)
                        {
                            correctAnswer = n1 + n2 + n3;
                            questionText = $"{n1} + {n2} + {n3} = ?";
                        }
                    }
                    else
                    {
                        if (n1 < n2) n1 = n2 + UnityEngine.Random.Range(1, 10);
                        correctAnswer = n1 - n2 + n3;
                        questionText = $"{n1} - {n2} + {n3} = ?";
                    }
                    break;

                default:
                    n1 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    n2 = UnityEngine.Random.Range(config.MinNumber, config.MaxNumber + 1);
                    correctAnswer = n1 + n2;
                    questionText = $"{n1} + {n2} = ?";
                    break;
            }

            return questionText;
        }

        /// <summary>
        /// Sinh các đáp án (1 đúng + 3 sai)
        /// </summary>
        private int[] GenerateChoices(int correctAnswer, int minVal, int maxVal)
        {
            List<int> choices = new List<int> { correctAnswer };

            int maxOffset = (int)(CurrentGrade.Value * gameRules.wrongAnswerOffsetMultiplier);

            int safety = 0;
            while (choices.Count < 4 && safety < 100)
            {
                safety++;
                int offset = UnityEngine.Random.Range(-maxOffset, maxOffset + 1);
                if (offset == 0) offset = UnityEngine.Random.value > 0.5f ? 1 : -1;

                int wrong = Mathf.Abs(correctAnswer + offset);
                if (!choices.Contains(wrong))
                {
                    choices.Add(wrong);
                }
            }

            // Shuffle
            for (int i = 0; i < choices.Count; i++)
            {
                int temp = choices[i];
                int r = UnityEngine.Random.Range(i, choices.Count);
                choices[i] = choices[r];
                choices[r] = temp;
            }

            return choices.ToArray();
        }

        /// <summary>
        /// Đếm ngược thời gian câu hỏi
        /// </summary>
        private IEnumerator QuestionTimerRoutine()
        {
            float elapsed = 0f;
            float timeLimit = gameRules.questionTimeLimit;

            while (elapsed < timeLimit)
            {
                elapsed += Time.deltaTime;
                TimeRemaining.Value = timeLimit - elapsed;
                yield return null;
            }

            // Hết thời gian → Evaluate answers (dù có bao nhiêu người trả lời)
            TimeRemaining.Value = 0f;
            EvaluateAnswers();
        }

        /// <summary>
        /// Xử lý khi hết thời gian
        /// </summary>
        private void HandleTimeout()
        {
            // Kiểm tra NetworkManager thay vì IsServer
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            Debug.Log($"[BattleManager] ⏰ Timeout! Answers received: {playerAnswers.Count}/2");

            // Người không trả lời mất máu
            if (playerAnswers.Count == 0)
            {
                // Cả 2 không trả lời → Cả 2 mất máu
                player1State?.TakeDamage(gameRules.damageOnTimeout);
                player2State?.TakeDamage(gameRules.damageOnTimeout);
                Debug.Log("[BattleManager] Both players timed out!");
            }
            else if (playerAnswers.Count == 1)
            {
                // 1 người trả lời, 1 người không
                ulong answeredPlayer = playerAnswers.Keys.First();
                if (answeredPlayer == 0)
                {
                    player2State?.TakeDamage(gameRules.damageOnTimeout);
                    Debug.Log("[BattleManager] Player 2 timed out!");
                }
                else
                {
                    player1State?.TakeDamage(gameRules.damageOnTimeout);
                    Debug.Log("[BattleManager] Player 1 timed out!");
                }
            }

            correctAnswersInRow = 0;

            // Kiểm tra kết thúc trận
            if (CheckMatchEnd())
            {
                EndMatch();
            }
            else
            {
                // Sinh câu mới sau delay
                Invoke(nameof(GenerateQuestion), gameRules.delayBetweenQuestions);
            }
        }

        /// <summary>
        /// Player submit đáp án
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SubmitAnswerServerRpc(int answer, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            long submitTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long elapsedMs = submitTime - QuestionStartTimestamp.Value;

            // Kiểm tra timeout
            if (elapsedMs > gameRules.GetTimeoutMilliseconds())
            {
                Debug.Log($"[BattleManager] Client {clientId} submitted too late ({elapsedMs}ms)");
                return;
            }

            // Kiểm tra đã submit chưa
            if (playerAnswers.ContainsKey(clientId))
            {
                Debug.Log($"[BattleManager] Client {clientId} already submitted!");
                return;
            }

            // Lưu đáp án
            playerAnswers[clientId] = (answer, submitTime);
            Debug.Log($"[BattleManager] Client {clientId} submitted answer {answer} at {elapsedMs}ms");

            // KHÔNG evaluate ngay - đợi hết thời gian hoặc cả 2 đã trả lời
            // Timer sẽ tự động gọi HandleTimeout() hoặc evaluate khi cả 2 submit
        }

        /// <summary>
        /// Đánh giá đáp án của 2 người chơi
        /// 
        /// Logic so sánh theo miligiây:
        /// 1. Cả 2 đúng → So sánh ms (ai nhanh hơn thắng)
        /// 2. 1 đúng 1 sai → Người đúng thắng
        /// 3. Cả 2 sai → Cả 2 mất máu
        /// 4. 1 timeout → Người còn lại thắng
        /// </summary>
        private void EvaluateAnswers()
        {
            // Kiểm tra NetworkManager thay vì IsServer
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            Debug.Log("[BattleManager] 🎯 Evaluating answers...");

            // Stop timer
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }

            // Lấy đáp án của cả 2 người chơi
            int player1Answer = playerAnswers.ContainsKey(0) ? playerAnswers[0].answer : -1;
            int player2Answer = playerAnswers.ContainsKey(1) ? playerAnswers[1].answer : -1;

            // Lấy response time của cả 2 (tính từ QuestionStartTimestamp)
            long player1ResponseTime = playerAnswers.ContainsKey(0) ? playerAnswers[0].timestamp - QuestionStartTimestamp.Value : long.MaxValue;
            long player2ResponseTime = playerAnswers.ContainsKey(1) ? playerAnswers[1].timestamp - QuestionStartTimestamp.Value : long.MaxValue;

            bool player1Correct = player1Answer == CorrectAnswer.Value;
            bool player2Correct = player2Answer == CorrectAnswer.Value;

            int winnerId = -1;
            bool correct = false;
            long responseTimeMs = 0;

            Debug.Log($"[BattleManager] P1: answer={player1Answer}, correct={player1Correct}, time={player1ResponseTime}ms");
            Debug.Log($"[BattleManager] P2: answer={player2Answer}, correct={player2Correct}, time={player2ResponseTime}ms");

            // SCENARIO 1: Cả 2 đều đúng → So sánh thời gian (ms)
            if (player1Correct && player2Correct)
            {
                if (player1ResponseTime < player2ResponseTime)
                {
                    winnerId = 0; // Player 1 thắng (nhanh hơn)
                    responseTimeMs = player1ResponseTime;
                    Debug.Log($"[BattleManager] Both correct! Player 1 faster: {player1ResponseTime}ms < {player2ResponseTime}ms");
                }
                else if (player2ResponseTime < player1ResponseTime)
                {
                    winnerId = 1; // Player 2 thắng (nhanh hơn)
                    responseTimeMs = player2ResponseTime;
                    Debug.Log($"[BattleManager] Both correct! Player 2 faster: {player2ResponseTime}ms < {player1ResponseTime}ms");
                }
                else
                {
                    winnerId = -2; // Hòa (cùng thời gian)
                    responseTimeMs = player1ResponseTime;
                    Debug.Log($"[BattleManager] Both correct! Draw: {player1ResponseTime}ms == {player2ResponseTime}ms");
                }
                correct = true;
            }
            // SCENARIO 2: Player 1 đúng, Player 2 sai
            else if (player1Correct && !player2Correct)
            {
                winnerId = 0;
                responseTimeMs = player1ResponseTime;
                correct = true;
                Debug.Log($"[BattleManager] Player 1 correct, Player 2 wrong");
            }
            // SCENARIO 3: Player 2 đúng, Player 1 sai
            else if (!player1Correct && player2Correct)
            {
                winnerId = 1;
                responseTimeMs = player2ResponseTime;
                correct = true;
                Debug.Log($"[BattleManager] Player 2 correct, Player 1 wrong");
            }
            // SCENARIO 4: Cả 2 đều sai
            else
            {
                winnerId = -1;
                responseTimeMs = 0;
                correct = false;
                Debug.Log($"[BattleManager] Both answered incorrectly");
            }

            // Xử lý kết quả
            if (winnerId >= 0)
            {
                // Có người thắng
                NetworkedPlayerState winnerState = (winnerId == 0) ? player1State : player2State;
                NetworkedPlayerState loserState = (winnerId == 0) ? player2State : player1State;

                // Kiểm tra xem cả 2 có đúng không
                if (player1Correct && player2Correct)
                {
                    // Cả 2 đúng: Người thắng +10, người thua +5 (khuyến khích)
                    winnerState?.MarkCorrectAnswer();
                    winnerState?.AddScore(gameRules.pointsPerCorrectAnswer); // +10

                    loserState?.MarkCorrectAnswer();
                    loserState?.AddScore(gameRules.pointsPerCorrectAnswer / 2); // +5 (khuyến khích)

                    Debug.Log($"[BattleManager] Both correct! Winner gets {gameRules.pointsPerCorrectAnswer}, loser gets {gameRules.pointsPerCorrectAnswer / 2} (encouragement)");
                }
                else
                {
                    // Chỉ 1 người đúng: Người thắng +10, người thua -1 HP
                    winnerState?.MarkCorrectAnswer();
                    winnerState?.AddScore(gameRules.pointsPerCorrectAnswer);

                    loserState?.TakeDamage(gameRules.damageOnWrongAnswer);
                    loserState?.MarkWrongAnswer();
                }

                correctAnswersInRow++;

                // Tăng độ khó
                if (gameRules.ShouldIncreaseDifficulty(correctAnswersInRow))
                {
                    CurrentDifficulty.Value = gameRules.CalculateNewDifficulty(CurrentDifficulty.Value);
                    correctAnswersInRow = 0;
                    Debug.Log($"[BattleManager] Difficulty increased to level {CurrentDifficulty.Value}");
                }

                // Notify clients với winnerId chính xác
                NotifyAnswerResultClientRpc(winnerId, true, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);
                
                // ✅ FIX: Invoke event trên Host (ClientRpc không chạy trên Host)
                OnAnswerResult?.Invoke(winnerId, true, player1ResponseTime);
                OnAnswerResultReceived?.Invoke(winnerId, true, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);
            }
            else if (winnerId == -2)
            {
                // Hòa - cả 2 đúng cùng thời gian
                player1State?.MarkCorrectAnswer();
                player2State?.MarkCorrectAnswer();
                
                // Cả 2 được nửa điểm
                player1State?.AddScore(gameRules.pointsPerCorrectAnswer / 2);
                player2State?.AddScore(gameRules.pointsPerCorrectAnswer / 2);

                correctAnswersInRow++;

                // Tăng độ khó
                if (gameRules.ShouldIncreaseDifficulty(correctAnswersInRow))
                {
                    CurrentDifficulty.Value = gameRules.CalculateNewDifficulty(CurrentDifficulty.Value);
                    correctAnswersInRow = 0;
                    Debug.Log($"[BattleManager] Difficulty increased to level {CurrentDifficulty.Value}");
                }

                // Notify clients với winnerId = -2 (draw)
                NotifyAnswerResultClientRpc(-2, true, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);
                
                // ✅ FIX: Invoke event trên Host (ClientRpc không chạy trên Host)
                OnAnswerResult?.Invoke(-2, true, player1ResponseTime);
                OnAnswerResultReceived?.Invoke(-2, true, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);
            }
            else
            {
                // Cả 2 đều sai
                Debug.Log("[BattleManager] Both players answered incorrectly");

                player1State?.MarkWrongAnswer();
                player2State?.MarkWrongAnswer();

                if (gameRules.damageMode == GameRulesConfig.DamageMode.BothWrongBothDamaged)
                {
                    player1State?.TakeDamage(gameRules.damageOnWrongAnswer);
                    player2State?.TakeDamage(gameRules.damageOnWrongAnswer);
                }

                correctAnswersInRow = 0;

                // Notify clients với winnerId = -1 (both wrong)
                NotifyAnswerResultClientRpc(-1, false, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);
                
                // ✅ FIX: Invoke event trên Host (ClientRpc không chạy trên Host)
                OnAnswerResult?.Invoke(-1, false, player1ResponseTime);
                OnAnswerResultReceived?.Invoke(-1, false, player1ResponseTime, player2ResponseTime, player1Answer, player2Answer);
            }

            // Kiểm tra kết thúc trận
            if (CheckMatchEnd())
            {
                EndMatch();
            }
            else
            {
                // Sinh câu mới
                Invoke(nameof(GenerateQuestion), gameRules.delayBetweenQuestions);
            }
        }

        /// <summary>
        /// Kiểm tra xem trận đấu đã kết thúc chưa
        /// </summary>
        private bool CheckMatchEnd()
        {
            if (player1State == null || player2State == null) return false;

            return !player1State.IsAlive() || !player2State.IsAlive();
        }

        /// <summary>
        /// Kết thúc trận đấu
        /// </summary>
        private void EndMatch()
        {
            // Kiểm tra NetworkManager thay vì IsServer
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            MatchEnded.Value = true;
            Debug.Log("[BattleManager] 🏁 Match ended!");

            // Xác định người thắng
            int winnerId = -1;
            int winnerHealth = 0;

            if (player1State != null && player1State.IsAlive())
            {
                winnerId = 0;
                winnerHealth = player1State.CurrentHealth.Value;
            }
            else if (player2State != null && player2State.IsAlive())
            {
                winnerId = 1;
                winnerHealth = player2State.CurrentHealth.Value;
            }

            WinnerId.Value = winnerId;

            Debug.Log($"[BattleManager] Match ended! Winner: Player {winnerId + 1}");

            // Notify clients
            ShowMatchResultClientRpc(winnerId, winnerHealth);
        }

        #region CLIENT RPCs

        [ClientRpc]
        private void NotifyQuestionGeneratedClientRpc(FixedString512Bytes question, int[] choices)
        {
            Debug.Log($"[BattleManager] Client received question: {question}");
            OnQuestionGenerated?.Invoke(question.ToString(), choices);
        }

        [ClientRpc]
        private void NotifyAnswerResultClientRpc(int winnerId, bool correct, long player1ResponseTimeMs, long player2ResponseTimeMs, int player1Answer, int player2Answer)
        {
            Debug.Log($"[BattleManager] Client received answer result: Winner={winnerId}, Correct={correct}, P1Time={player1ResponseTimeMs}ms, P2Time={player2ResponseTimeMs}ms, P1Answer={player1Answer}, P2Answer={player2Answer}");
            OnAnswerResult?.Invoke(winnerId, correct, player1ResponseTimeMs);
            OnAnswerResultReceived?.Invoke(winnerId, correct, player1ResponseTimeMs, player2ResponseTimeMs, player1Answer, player2Answer);
        }

        [ClientRpc]
        private void ShowMatchResultClientRpc(int winnerId, int winnerHealth)
        {
            Debug.Log($"[BattleManager] Client received match result: Winner={winnerId}, Health={winnerHealth}");
            OnMatchEnded?.Invoke(winnerId, winnerHealth);
        }

        #endregion

        #region PUBLIC GETTERS

        public NetworkedPlayerState GetPlayer1State()
        {
            // Nếu reference bị mất, tìm lại từ scene
            if (player1State == null)
            {
                var allStates = FindObjectsOfType<NetworkedPlayerState>();
                foreach (var state in allStates)
                {
                    if (state.PlayerId.Value == 0)
                    {
                        player1State = state;
                        Debug.Log("[BattleManager] Re-found Player1State from scene");
                        break;
                    }
                }
            }
            return player1State;
        }

        public NetworkedPlayerState GetPlayer2State()
        {
            // Nếu reference bị mất, tìm lại từ scene
            if (player2State == null)
            {
                var allStates = FindObjectsOfType<NetworkedPlayerState>();
                foreach (var state in allStates)
                {
                    if (state.PlayerId.Value == 1)
                    {
                        player2State = state;
                        Debug.Log("[BattleManager] Re-found Player2State from scene");
                        break;
                    }
                }
            }
            return player2State;
        }

        public int[] GetCurrentChoices()
        {
            return new int[] { Choice1.Value, Choice2.Value, Choice3.Value, Choice4.Value };
        }

        #endregion

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            // Cleanup
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }
    }
}
