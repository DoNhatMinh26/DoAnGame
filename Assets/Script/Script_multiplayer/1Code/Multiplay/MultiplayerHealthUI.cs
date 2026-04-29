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

            // Nếu không tìm được, không init
            if (player1State == null || player2State == null)
            {
                Debug.LogError("[HealthUI] Player states not found! Skipping initialization.");
                return;
            }

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
                UpdateName(player1NameText, val.ToString(), "Player 1");

            // Initial update Player 1
            UpdateHealth(player1HealthFill, player1HealthText, player1State.CurrentHealth.Value, player1State.MaxHealth.Value);
            UpdateScore(player1ScoreText, player1State.Score.Value);
            UpdateName(player1NameText, player1State.PlayerName.Value.ToString(), "Player 1");

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
                UpdateName(player2NameText, val.ToString(), "Player 2");

            // Initial update Player 2
            UpdateHealth(player2HealthFill, player2HealthText, player2State.CurrentHealth.Value, player2State.MaxHealth.Value);
            UpdateScore(player2ScoreText, player2State.Score.Value);
            UpdateName(player2NameText, player2State.PlayerName.Value.ToString(), "Player 2");

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

                // Đổi màu theo %
                if (percent > 0.5f)
                    fill.color = healthHighColor;
                else if (percent > 0.25f)
                    fill.color = healthMediumColor;
                else
                    fill.color = healthLowColor;
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
        /// Cập nhật tên
        /// </summary>
        private void UpdateName(TMP_Text text, string name, string defaultName)
        {
            if (text != null)
            {
                text.text = string.IsNullOrEmpty(name) ? defaultName : name;
            }
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
