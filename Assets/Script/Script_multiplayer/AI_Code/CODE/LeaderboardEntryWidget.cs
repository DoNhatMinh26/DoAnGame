using TMPro;
using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// Item đơn cho bảng xếp hạng.
    /// </summary>
    public class LeaderboardEntryWidget : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text scoreText;

        public void SetData(int rank, string name, int score)
        {
            if (rankText != null)
                rankText.text = $"#{rank}";
            if (nameText != null)
                nameText.text = name;
            if (scoreText != null)
                scoreText.text = score.ToString("N0");
        }
    }
}
