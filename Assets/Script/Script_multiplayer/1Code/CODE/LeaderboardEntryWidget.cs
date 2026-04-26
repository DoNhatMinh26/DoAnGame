using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DoAnGame.UI
{
    /// <summary>
    /// Widget hiển thị một entry trong bảng xếp hạng
    /// </summary>
    public class LeaderboardEntryWidget : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image background;
        [SerializeField] private Image highlightBorder;

        [Header("Highlight Colors")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.1f); // Trắng mờ
        [SerializeField] private Color top3Color = new Color(1f, 0.84f, 0f, 0.3f); // Vàng gold cho top 3
        [SerializeField] private Color currentPlayerColor = new Color(0f, 0.8f, 1f, 0.4f); // Xanh dương cho player hiện tại

        private bool isCurrentPlayer = false;

        /// <summary>
        /// Set dữ liệu cho entry
        /// </summary>
        public void SetData(int rank, string playerName, int score, int level = 1)
        {
            if (rankText != null)
                rankText.text = rank.ToString();
            
            if (nameText != null)
                nameText.text = playerName;
            
            if (scoreText != null)
                scoreText.text = score.ToString("N0"); // Format với dấu phẩy: 1,000
            
            if (levelText != null)
                levelText.text = $"Lv.{level}";
        }

        /// <summary>
        /// Bật/tắt highlight cho top 3
        /// </summary>
        public void SetHighlight(bool isTop3)
        {
            if (isTop3 && !isCurrentPlayer)
            {
                // Top 3 nhưng không phải player hiện tại
                ApplyHighlight(top3Color, true);
            }
        }

        /// <summary>
        /// Đánh dấu entry này là player hiện tại
        /// </summary>
        public void SetCurrentPlayer(bool isCurrent)
        {
            isCurrentPlayer = isCurrent;
            
            if (isCurrent)
            {
                // Player hiện tại - ưu tiên màu xanh dương
                ApplyHighlight(currentPlayerColor, true);
            }
        }

        /// <summary>
        /// Apply màu highlight
        /// </summary>
        private void ApplyHighlight(Color color, bool showBorder)
        {
            if (background != null)
            {
                background.color = color;
            }

            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(showBorder);
            }
        }

        /// <summary>
        /// Reset về trạng thái normal
        /// </summary>
        public void ResetHighlight()
        {
            isCurrentPlayer = false;
            ApplyHighlight(normalColor, false);
        }
    }
}
