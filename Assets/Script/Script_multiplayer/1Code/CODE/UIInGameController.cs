using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 9: In-Game Panel - hiển thị câu hỏi, đáp án, thời gian.
    /// Thực tế sẽ kết nối với GamePlayManager (chưa có), tạm thời expose events để designer gắn.
    /// </summary>
    public class UIInGameController : BasePanelController
    {
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text questionIndexText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private List<Button> answerButtons = new List<Button>();
        [SerializeField] private UIFlowManager flowManager;

        [System.Serializable]
        public class AnswerSelectedEvent : UnityEvent<int> { }

        public AnswerSelectedEvent OnAnswerSelected = new AnswerSelectedEvent();
        public UnityEvent OnPauseClicked = new UnityEvent();

        protected override void Awake()
        {
            base.Awake();
            pauseButton?.onClick.AddListener(HandlePause);
            for (int i = 0; i < answerButtons.Count; i++)
            {
                int index = i;
                answerButtons[i]?.onClick.AddListener(() => HandleAnswer(index));
            }
        }

        private void OnDestroy()
        {
            pauseButton?.onClick.RemoveListener(HandlePause);
            foreach (var button in answerButtons)
            {
                button?.onClick.RemoveAllListeners();
            }
        }

        public void UpdateQuestion(int questionIndex, int totalQuestions, string question, string[] answers)
        {
            questionIndexText?.SetText($"Câu {questionIndex}/{totalQuestions}");
            questionText?.SetText(question);

            for (int i = 0; i < answerButtons.Count; i++)
            {
                var label = answerButtons[i]?.GetComponentInChildren<TMP_Text>();
                if (label == null) continue;
                label.SetText(i < answers.Length ? answers[i] : "---");
            }
        }

        public void UpdateTimer(float seconds)
        {
            timerText?.SetText($"Time: {Mathf.CeilToInt(seconds)}s");
        }

        public void UpdateScore(int score)
        {
            scoreText?.SetText($"Score: {score}");
        }

        public void SetFeedback(string message, bool isCorrect)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.color = isCorrect ? Color.green : Color.red;
        }

        private void HandlePause()
        {
            OnPauseClicked?.Invoke();
            flowManager.ShowScreen(UIFlowManager.Screen.PauseMenu);
        }

        private void HandleAnswer(int index)
        {
            OnAnswerSelected?.Invoke(index);
        }
    }
}
