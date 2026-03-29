using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 5: Main Menu Hub
    /// </summary>
    public class UIMainMenuController : FlowPanelController
    {
        [Header("Buttons (Nút)")]
        [SerializeField] private Button logoutButton;

        [Header("Texts (Thông tin người chơi)")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text scoreText;

        [SerializeField] private UIFlowManager flowManager;

        private AuthManager authManager;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;
            logoutButton?.onClick.AddListener(HandleLogout);
        }

        protected override void OnShow()
        {
            base.OnShow();
            UpdatePlayerInfo();
        }

        private void OnDestroy()
        {
            base.OnDestroy();
            logoutButton?.onClick.RemoveAllListeners();
        }

        private void UpdatePlayerInfo()
        {
            var data = authManager?.GetCurrentPlayerData();
            if (data == null)
            {
                levelText?.SetText("Lv: 1");
                scoreText?.SetText("Score: 0");
                return;
            }

            levelText?.SetText($"Lv: {data.currentLevel}");
            scoreText?.SetText($"Score: {data.totalScore}");
        }

        private void HandleLogout()
        {
            authManager?.Logout();
            flowManager.ShowScreen(UIFlowManager.Screen.WelcomeAuth, false);
        }
    }
}
