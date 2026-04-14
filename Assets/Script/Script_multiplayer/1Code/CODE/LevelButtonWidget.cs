using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Component gắn lên prefab level button.
    /// </summary>
    public class LevelButtonWidget : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text starLabel;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Button playButton;

        public Button Button => playButton;

        public void SetData(int levelIndex, bool unlocked, int stars)
        {
            if (levelLabel != null)
                levelLabel.text = $"Level {levelIndex}";

            if (starLabel != null)
                starLabel.text = new string('★', stars).PadRight(3, '☆');

            if (lockIcon != null)
                lockIcon.SetActive(!unlocked);

            if (playButton != null)
                playButton.interactable = unlocked;
        }
    }
}
