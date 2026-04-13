using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 13: Profile Panel
    /// </summary>
    public class UIProfilePanelController : BasePanelController
    {
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text winRateText;
        [SerializeField] private Slider expSlider;
        [SerializeField] private UIFlowManager flowManager;

        private AuthManager authManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;
            backButton?.onClick.AddListener(() => flowManager.Back());
        }

        protected override void OnShow()
        {
            base.OnShow();
            UpdateDisplay();
        }

        private void OnDestroy()
        {
            backButton?.onClick.RemoveAllListeners();
        }

        private void UpdateDisplay()
        {
            var data = authManager?.GetCurrentPlayerData();
            if (data == null)
            {
                usernameText?.SetText("Khách");
                levelText?.SetText("Lv 1");
                expText?.SetText("0/100 XP");
                scoreText?.SetText("Score: 0");
                winRateText?.SetText("Win Rate: 0%");
                expSlider?.SetValueWithoutNotify(0);
                return;
            }

            usernameText?.SetText(data.characterName);
            levelText?.SetText($"Level {data.level}");
            var xpForNext = 100;
            float progress = (data.totalXp % xpForNext) / (float)xpForNext;
            expText?.SetText($"XP: {data.totalXp % xpForNext}/{xpForNext}");
            scoreText?.SetText($"Score: {data.totalScore:N0}");
            winRateText?.SetText($"Win Rate: {data.winRate:P0}");
            expSlider?.SetValueWithoutNotify(progress);
        }
    }
}
