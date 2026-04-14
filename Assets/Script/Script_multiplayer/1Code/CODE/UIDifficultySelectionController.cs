using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 8: Difficulty Selection
    /// </summary>
    public class UIDifficultySelectionController : BasePanelController
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private UIFlowManager flowManager;

        protected override void Awake()
        {
            base.Awake();
            backButton?.onClick.AddListener(() => flowManager.Back());
            easyButton?.onClick.AddListener(() => SelectDifficulty("Easy"));
            normalButton?.onClick.AddListener(() => SelectDifficulty("Normal"));
            hardButton?.onClick.AddListener(() => SelectDifficulty("Hard"));
        }

        private void OnDestroy()
        {
            backButton?.onClick.RemoveAllListeners();
            easyButton?.onClick.RemoveAllListeners();
            normalButton?.onClick.RemoveAllListeners();
            hardButton?.onClick.RemoveAllListeners();
        }

        private void SelectDifficulty(string difficulty)
        {
            GameModeContext.SetDifficulty(difficulty);
            flowManager.ShowScreen(UIFlowManager.Screen.InGame);
        }
    }
}
