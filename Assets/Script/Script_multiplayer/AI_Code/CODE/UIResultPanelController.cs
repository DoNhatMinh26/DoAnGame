using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 11: Game Result
    /// </summary>
    public class UIResultPanelController : BasePanelController
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button backMenuButton;
        [SerializeField] private UIFlowManager flowManager;

        public UnityEvent OnReplayRequested = new UnityEvent();
        public UnityEvent OnContinueRequested = new UnityEvent();

        protected override void Awake()
        {
            base.Awake();
            replayButton?.onClick.AddListener(() => { OnReplayRequested?.Invoke(); flowManager.ShowScreen(UIFlowManager.Screen.LevelSelection); });
            continueButton?.onClick.AddListener(() => { OnContinueRequested?.Invoke(); flowManager.ShowScreen(UIFlowManager.Screen.LevelSelection); });
            backMenuButton?.onClick.AddListener(() => flowManager.ShowScreen(UIFlowManager.Screen.MainMenu));
        }

        private void OnDestroy()
        {
            replayButton?.onClick.RemoveAllListeners();
            continueButton?.onClick.RemoveAllListeners();
            backMenuButton?.onClick.RemoveAllListeners();
        }

        public void SetResult(bool win, int correctAnswers, int totalQuestions, int expEarned)
        {
            titleText?.SetText(win ? "✓ CHÍNH XÁC HOÀN HẢO!" : "Thử lại nhé!");
            statsText?.SetText($"Câu trả lời: {correctAnswers}/{totalQuestions}");
            rewardText?.SetText($"Điểm thưởng: +{expEarned} XP");
        }
    }
}
