using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Quản lý trạng thái của 1 người chơi trong multiplayer battle.
    /// Mỗi player sẽ có 1 instance riêng (spawn bởi NetworkManager).
    /// Host và Client đều dùng chung script này.
    /// </summary>
    public class NetworkedPlayerState : NetworkBehaviour
    {
        [Header("=== PLAYER INFO ===")]
        [Tooltip("ID của player này (0 = Host, 1 = Client)")]
        public NetworkVariable<int> PlayerId = new NetworkVariable<int>();

        [Tooltip("Tên hiển thị của player")]
        public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>();

        [Header("=== HEALTH SYSTEM ===")]
        [Tooltip("Máu hiện tại của player")]
        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();

        [Tooltip("Máu tối đa (lấy từ GameRulesConfig)")]
        public NetworkVariable<int> MaxHealth = new NetworkVariable<int>();

        [Header("=== SCORING (Dự phòng) ===")]
        [Tooltip("Điểm số hiện tại")]
        public NetworkVariable<int> Score = new NetworkVariable<int>();

        [Tooltip("Số câu trả lời đúng")]
        public NetworkVariable<int> CorrectAnswers = new NetworkVariable<int>();

        [Tooltip("Số câu trả lời sai")]
        public NetworkVariable<int> WrongAnswers = new NetworkVariable<int>();

        [Header("=== ANSWER TRACKING ===")]
        [Tooltip("Đáp án hiện tại của player (chưa submit)")]
        private int pendingAnswer = -1;

        [Tooltip("Timestamp khi player submit đáp án (milliseconds)")]
        private long answerTimestamp = 0;

        [Tooltip("Player đã submit đáp án cho câu hỏi hiện tại chưa")]
        public NetworkVariable<bool> HasAnswered = new NetworkVariable<bool>();

        [Tooltip("Player đã sẵn sàng chưa")]
        public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Events để UI subscribe
        public event Action<int, int> OnHealthChanged; // (oldHealth, newHealth)
        public event Action<int> OnScoreChanged;
        public event Action OnPlayerDied;
        public event Action<bool> OnAnswerSubmitted; // (isCorrect)

        private void Awake()
        {
            // Subscribe vào NetworkVariable changes để trigger events
            CurrentHealth.OnValueChanged += (oldValue, newValue) =>
            {
                OnHealthChanged?.Invoke(oldValue, newValue);
                if (newValue <= 0)
                {
                    OnPlayerDied?.Invoke();
                }
            };

            Score.OnValueChanged += (oldValue, newValue) =>
            {
                OnScoreChanged?.Invoke(newValue);
            };
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Chỉ host mới được khởi tạo giá trị ban đầu
            if (IsServer)
            {
                // Giá trị sẽ được set bởi NetworkedMathBattleManager
                Debug.Log($"[PlayerState] Spawned player {OwnerClientId} on server");
            }
        }

        /// <summary>
        /// Khởi tạo player state (chỉ gọi trên Server)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void InitializeServerRpc(int playerId, string playerName, int maxHealth)
        {
            if (!IsServer) return;

            PlayerId.Value = playerId;
            PlayerName.Value = playerName;
            MaxHealth.Value = maxHealth;
            CurrentHealth.Value = maxHealth;
            Score.Value = 0;
            CorrectAnswers.Value = 0;
            WrongAnswers.Value = 0;
            HasAnswered.Value = false;

            Debug.Log($"[PlayerState] Initialized P{playerId}: {playerName}, HP={maxHealth}");
        }

        /// <summary>
        /// Gây sát thương cho player (chỉ gọi trên Server)
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!IsServer) return;

            int oldHealth = CurrentHealth.Value;
            CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - damage);

            Debug.Log($"[PlayerState] P{PlayerId.Value} took {damage} damage: {oldHealth} → {CurrentHealth.Value}");

            // Notify clients về damage
            NotifyDamageClientRpc(damage, CurrentHealth.Value);
        }

        /// <summary>
        /// Hồi máu cho player (chỉ gọi trên Server)
        /// </summary>
        public void Heal(int amount)
        {
            if (!IsServer) return;

            int oldHealth = CurrentHealth.Value;
            CurrentHealth.Value = Mathf.Min(MaxHealth.Value, CurrentHealth.Value + amount);

            Debug.Log($"[PlayerState] P{PlayerId.Value} healed {amount}: {oldHealth} → {CurrentHealth.Value}");
        }

        /// <summary>
        /// Cộng điểm cho player (chỉ gọi trên Server)
        /// </summary>
        public void AddScore(int points)
        {
            if (!IsServer) return;

            Score.Value += points;
            Debug.Log($"[PlayerState] P{PlayerId.Value} gained {points} points: {Score.Value}");
        }

        /// <summary>
        /// Đánh dấu player đã trả lời đúng (chỉ gọi trên Server)
        /// </summary>
        public void MarkCorrectAnswer()
        {
            if (!IsServer) return;

            CorrectAnswers.Value++;
            HasAnswered.Value = true;
        }

        /// <summary>
        /// Đánh dấu player đã trả lời sai (chỉ gọi trên Server)
        /// </summary>
        public void MarkWrongAnswer()
        {
            if (!IsServer) return;

            WrongAnswers.Value++;
            HasAnswered.Value = true;
        }

        /// <summary>
        /// Reset trạng thái trả lời cho câu hỏi mới (chỉ gọi trên Server)
        /// </summary>
        public void ResetAnswerState()
        {
            if (!IsServer) return;

            HasAnswered.Value = false;
            pendingAnswer = -1;
            answerTimestamp = 0;
        }

        /// <summary>
        /// Kiểm tra player còn sống không
        /// </summary>
        public bool IsAlive()
        {
            return CurrentHealth.Value > 0;
        }

        /// <summary>
        /// Lấy tỷ lệ máu hiện tại (0-1)
        /// </summary>
        public float GetHealthPercentage()
        {
            if (MaxHealth.Value <= 0) return 0f;
            return (float)CurrentHealth.Value / MaxHealth.Value;
        }

        /// <summary>
        /// Lấy tỷ lệ chính xác (%)
        /// </summary>
        public float GetAccuracy()
        {
            int total = CorrectAnswers.Value + WrongAnswers.Value;
            if (total == 0) return 0f;
            return (float)CorrectAnswers.Value / total * 100f;
        }

        /// <summary>
        /// Notify clients về damage (để hiển thị animation)
        /// </summary>
        [ClientRpc]
        private void NotifyDamageClientRpc(int damage, int newHealth)
        {
            // UI có thể subscribe vào event này để hiển thị damage number
            Debug.Log($"[PlayerState] Client received damage notification: -{damage} HP (now {newHealth})");
        }

        /// <summary>
        /// Debug info
        /// </summary>
        public override string ToString()
        {
            return $"Player {PlayerId.Value} ({PlayerName.Value}): HP={CurrentHealth.Value}/{MaxHealth.Value}, Score={Score.Value}, Correct={CorrectAnswers.Value}, Wrong={WrongAnswers.Value}";
        }

        #region EDITOR HELPERS
#if UNITY_EDITOR
        [ContextMenu("Debug: Take 1 Damage")]
        private void DebugTakeDamage()
        {
            if (IsServer)
            {
                TakeDamage(1);
            }
            else
            {
                Debug.LogWarning("Can only take damage on server!");
            }
        }

        [ContextMenu("Debug: Add 10 Score")]
        private void DebugAddScore()
        {
            if (IsServer)
            {
                AddScore(10);
            }
            else
            {
                Debug.LogWarning("Can only add score on server!");
            }
        }

        [ContextMenu("Debug: Print State")]
        private void DebugPrintState()
        {
            Debug.Log(ToString());
        }
#endif
        #endregion
    }
}
