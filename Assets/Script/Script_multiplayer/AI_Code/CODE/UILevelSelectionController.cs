using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 7: Level Selection (Candy Crush style - logic phần core).
    /// </summary>
    public class UILevelSelectionController : BasePanelController
    {
        [SerializeField] private UIFlowManager flowManager;
        [SerializeField] private RectTransform levelContainer;
        [SerializeField] private LevelButtonWidget levelButtonPrefab;
        [SerializeField] private TMP_Text currentLevelText;
        [SerializeField] private int totalLevels = 12;

        private readonly List<LevelButtonWidget> spawnedButtons = new List<LevelButtonWidget>();
        private bool initialized;

        protected override void OnShow()
        {
            base.OnShow();
            if (!initialized)
            {
                BuildLevelButtons();
                initialized = true;
            }
            UpdateCurrentLevelLabel();
        }

        private void BuildLevelButtons()
        {
            if (levelButtonPrefab == null || levelContainer == null)
            {
                Debug.LogError("[UILevel] Thiếu prefab hoặc container");
                return;
            }

            for (int i = 1; i <= totalLevels; i++)
            {
                var button = Instantiate(levelButtonPrefab, levelContainer);
                int levelIndex = i;
                bool unlocked = IsLevelUnlocked(levelIndex);
                int stars = PlayerPrefs.GetInt($"LevelStars_{levelIndex}", 0);
                button.SetData(levelIndex, unlocked, Mathf.Clamp(stars, 0, 3));
                button.Button.onClick.AddListener(() => OnLevelClicked(levelIndex, unlocked));
                spawnedButtons.Add(button);
            }
        }

        private bool IsLevelUnlocked(int level)
        {
            if (level == 1) return true;
            return PlayerPrefs.GetInt($"UnlockedLevel_{level}", 0) == 1;
        }

        private void OnLevelClicked(int levelIndex, bool unlocked)
        {
            if (!unlocked)
            {
                Debug.Log("[UILevel] Level khóa, hoàn thành level trước");
                return;
            }

            GameModeContext.SetLevel(levelIndex);
            flowManager.ShowScreen(UIFlowManager.Screen.Difficulty);
        }

        private void UpdateCurrentLevelLabel()
        {
            currentLevelText?.SetText($"Current Level: {GameModeContext.SelectedLevel}");
        }
    }
}
