using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DoAnGame.Multiplayer;

namespace DoAnGame.UI
{
    /// <summary>
    /// Quản lý UI hiển thị máu (thanh slider) và timer cho multiplayer battle.
    /// Đơn giản - chỉ dùng thanh máu.
    /// </summary>
    public class MultiplayerHealthUI : MonoBehaviour
    {
        [Header("=== PLAYER 1 ===")]
        [SerializeField] private Image player1HealthFill;
        [SerializeField] private TMP_Text player1HealthText;
        [SerializeField] private TMP_Text player1NameText;
        [SerializeField] private TMP_Text player1ScoreText;

        [Header("=== PLAYER 2 ===")]
        [SerializeField] private Image player2HealthFill;
        [SerializeField] private TMP_Text player2HealthText;
        [SerializeField] private TMP_Text player2NameText;
        [SerializeField] private TMP_Text player2ScoreText;

        [Header("=== TIMER (OPTIONAL) ===")]
        [Tooltip("Optional - Timer text (có thể để None nếu dùng AnswerSummaryUI)")]
        [SerializeField] private TMP_Text timerText;
        [Tooltip("Optional - Timer fill image")]
        [SerializeField] private Image timerFillImage;

        [Header("=== COLORS ===")]
        [SerializeField] private Color healthHighColor = new Color(0f, 1f, 0f); // Xanh > 50%
        [SerializeField] private Color healthMediumColor = new Color(1f, 1f, 0f); // Vàng 25-50%
        [SerializeField] private Color healthLowColor = new Color(1f, 0f, 0f); // Đỏ < 25%
        
        [SerializeField] private Color timerNormalColor = new Color(0f, 1f, 0f); // Xanh > 5s
        [SerializeField] private Color timerWarningColor = new Color(1f, 1f, 0f); // Vàng 3-5s
        [SerializeField] private Color timerDangerColor = new Color(1f, 0f, 0f); // Đỏ < 3s

        private NetworkedMathBattleManager battleManager;
        private NetworkedPlayerState player1State;
        private NetworkedPlayerState player2State;
        private int initRetryCount = 0;
        private const int MAX_INIT_RETRIES = 10; // Retry tối đa 10 lần (10 giây)

        private void Start()
        {
            // Delay để đảm bảo NetworkObjects đã spawn
            Invoke(nameof(InitializeUI), 1f);
        }

        private void InitializeUI()
        {
            battleManager = NetworkedMathBattleManager.Instance;

            if (battleManager == null)
            {
                Debug.LogError("[HealthUI] BattleManager is NULL! Cannot initialize.");
                return;
            }

            // Lấy player states
            player1State = battleManager.GetPlayer1State();
            player2State = battleManager.GetPlayer2State();

            Debug.Log($"[HealthUI] Player1State: {(player1State != null ? "FOUND" : "NULL")}");
            Debug.Log($"[HealthUI] Player2State: {(player2State != null ? "FOUND" : "NULL")}");

            // Nếu không tìm được, retry sau 1 giây
            if (player1State == null || player2State == null)
            {
                initRetryCount++;
                if (initRetryCount < MAX_INIT_RETRIES)
                {
                    Debug.LogWarning($"[HealthUI] Player states not found yet (retry {initRetryCount}/{MAX_INIT_RETRIES}). Retrying in 1s...");
                    Invoke(nameof(InitializeUI), 1f);
                    return;
                }
                else
                {
                    Debug.LogError("[HealthUI] Player states not found after max retries! Skipping initialization.");
                    return;
                }
            }

            // Reset retry count
            initRetryCount = 0;

            // Subscribe Player 1
            Debug.Log($"[HealthUI] Subscribing to Player1: HP={player1State.CurrentHealth.Value}/{player1State.MaxHealth.Value}");
            
            player1State.CurrentHealth.OnValueChanged += (old, val) => 
            {
                UpdateHealth(player1HealthFill, player1HealthText, val, player1State.MaxHealth.Value);
            };
            
            player1State.Score.OnValueChanged += (old, val) => 
            {
                UpdateScore(player1ScoreText, val);
            };
            
            player1State.PlayerName.OnValueChanged += (old, val) => 
                UpdatePlayerName(player1NameText, val.ToString(), "Player 1", player1State);

            // Initial update Player 1
            UpdateHealth(player1HealthFill, player1HealthText, player1State.CurrentHealth.Value, player1State.MaxHealth.Value);
            UpdateScore(player1ScoreText, player1State.Score.Value);
            UpdatePlayerName(player1NameText, player1State.PlayerName.Value.ToString(), "Player 1", player1State);

            // Subscribe Player 2
            Debug.Log($"[HealthUI] Subscribing to Player2: HP={player2State.CurrentHealth.Value}/{player2State.MaxHealth.Value}");
            
            player2State.CurrentHealth.OnValueChanged += (old, val) => 
            {
                UpdateHealth(player2HealthFill, player2HealthText, val, player2State.MaxHealth.Value);
            };
            
            player2State.Score.OnValueChanged += (old, val) => 
            {
                UpdateScore(player2ScoreText, val);
            };
            
            player2State.PlayerName.OnValueChanged += (old, val) => 
                UpdatePlayerName(player2NameText, val.ToString(), "Player 2", player2State);

            // Initial update Player 2
            UpdateHealth(player2HealthFill, player2HealthText, player2State.CurrentHealth.Value, player2State.MaxHealth.Value);
            UpdateScore(player2ScoreText, player2State.Score.Value);
            UpdatePlayerName(player2NameText, player2State.PlayerName.Value.ToString(), "Player 2", player2State);

            // Subscribe Timer
            if (battleManager != null)
            {
                battleManager.TimeRemaining.OnValueChanged += (old, val) => UpdateTimer(val);
            }

            Debug.Log("[HealthUI] ✅ Successfully initialized!");
        }

        private void Update()
        {
            // Update timer mỗi frame
            if (battleManager != null && battleManager.MatchStarted.Value && !battleManager.MatchEnded.Value)
            {
                UpdateTimer(battleManager.TimeRemaining.Value);
            }
        }

        /// <summary>
        /// Cập nhật thanh máu
        /// </summary>
        private void UpdateHealth(Image fill, TMP_Text text, int current, int max)
        {
            if (fill == null && text == null) return;

            float percent = max > 0 ? (float)current / max : 0f;

            // Update fill
            if (fill != null)
            {
                fill.fillAmount = percent;
                Debug.Log($"[HealthUI] UpdateHealth: {fill.gameObject.name} fillAmount={percent:F2} ({current}/{max}), FillMethod={fill.fillMethod}, Color={fill.color}");

                // Đổi màu theo %
                if (percent > 0.5f)
                    fill.color = healthHighColor;
                else if (percent > 0.25f)
                    fill.color = healthMediumColor;
                else
                    fill.color = healthLowColor;
            }
            else
            {
                Debug.LogWarning($"[HealthUI] ⚠️ UpdateHealth: fill is NULL! Cannot update fillAmount. current={current}, max={max}");
            }

            // Update text
            if (text != null)
            {
                text.text = $"{current}/{max}";
            }
        }

        /// <summary>
        /// Cập nhật điểm
        /// </summary>
        private void UpdateScore(TMP_Text text, int score)
        {
            if (text != null)
            {
                text.text = $"Score: {score}";
            }
        }

        /// <summary>
        /// Cập nhật tên (cũ - không dùng nữa)
        /// </summary>
        private void UpdateName(TMP_Text text, string name, string defaultName)
        {
            if (text != null)
            {
                text.text = string.IsNullOrEmpty(name) ? defaultName : name;
            }
        }

        /// <summary>
        /// Cập nhật tên player với "(Bạn)" nếu là local player
        /// </summary>
        private void UpdatePlayerName(TMP_Text text, string name, string defaultName, NetworkedPlayerState playerState)
        {
            if (text == null) return;

            // Lấy tên
            string displayName = string.IsNullOrEmpty(name) ? defaultName : name;

            // Kiểm tra xem có phải local player không
            if (playerState != null && IsLocalPlayer(playerState))
            {
                displayName += " (Bạn)";
            }

            text.text = displayName;
        }

        /// <summary>
        /// Kiểm tra xem player có phải local player không
        /// </summary>
        private bool IsLocalPlayer(NetworkedPlayerState state)
        {
            if (state == null)
                return false;

            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null)
                return false;

            // Host = Player 0, Client = Player 1
            bool isHost = nm.IsHost;
            int playerId = state.PlayerId.Value;

            return (isHost && playerId == 0) || (!isHost && playerId == 1);
        }

        /// <summary>
        /// Cập nhật timer
        /// </summary>
        private void UpdateTimer(float time)
        {
            // Update text
            if (timerText != null)
            {
                int sec = Mathf.CeilToInt(time);
                timerText.text = $"{sec}s";

                // Đổi màu text
                if (time > 5f)
                    timerText.color = timerNormalColor;
                else if (time > 3f)
                    timerText.color = timerWarningColor;
                else
                    timerText.color = timerDangerColor;
            }

            // Update fill
            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = time / 10f;

                // Đổi màu fill
                if (time > 5f)
                    timerFillImage.color = timerNormalColor;
                else if (time > 3f)
                    timerFillImage.color = timerWarningColor;
                else
                    timerFillImage.color = timerDangerColor;
            }
        }

        #region DEBUG
#if UNITY_EDITOR
        [ContextMenu("Debug: P1 -1 HP")]
        private void DebugP1Damage()
        {
            if (player1State != null && Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
                player1State.TakeDamage(1);
            else
                Debug.LogWarning("Server only!");
        }

        [ContextMenu("Debug: P2 -1 HP")]
        private void DebugP2Damage()
        {
            if (player2State != null && Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
                player2State.TakeDamage(1);
            else
                Debug.LogWarning("Server only!");
        }
#endif
        #endregion
    }
}
