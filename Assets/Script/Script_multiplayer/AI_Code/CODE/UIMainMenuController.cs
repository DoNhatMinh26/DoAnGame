using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Auth;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 5: Main Menu Hub
    /// </summary>
    public class UIMainMenuController : FlowPanelController
    {
        [Header("Buttons (Nút)")]
        [SerializeField] private Button logoutButton;
        [SerializeField] private GameObject welcomeScreenRoot;

        [Header("Texts (Thông tin người chơi)")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text characterNameText;

        [SerializeField] private UIFlowManager flowManager;

        private AuthManager authManager;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;

            if (characterNameText == null)
            {
                Transform nameNode = transform.Find("TenNhanVat");
                if (nameNode != null)
                {
                    characterNameText = nameNode.GetComponent<TMP_Text>();
                }
            }

            if (logoutButton == null)
            {
                Transform logoutNode = transform.Find("DangXuat");
                if (logoutNode == null)
                {
                    logoutNode = transform.Find("DangXuatBtn");
                }

                if (logoutNode != null)
                {
                    logoutButton = logoutNode.GetComponent<Button>();
                }
            }

            if (welcomeScreenRoot == null)
            {
                Transform canvas = transform.parent;
                if (canvas != null)
                {
                    var welcomeNode = canvas.Find("WELCOMESCREEN");
                    if (welcomeNode != null)
                    {
                        welcomeScreenRoot = welcomeNode.gameObject;
                    }
                }
            }

            if (logoutButton != null)
            {
                var logoutNavigator = logoutButton.GetComponent<UIButtonScreenNavigator>();
                if (logoutNavigator != null)
                {
                    logoutNavigator.enabled = false;
                }

                logoutButton.onClick.RemoveAllListeners();
                logoutButton.onClick.AddListener(HandleLogout);
            }

        }

        protected override void OnShow()
        {
            base.OnShow();
            if (authManager == null)
            {
                authManager = AuthManager.Instance;
            }
            UpdatePlayerInfo();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            logoutButton?.onClick.RemoveAllListeners();
        }

        private void UpdatePlayerInfo()
        {
            var data = authManager?.GetCurrentPlayerData();

            string characterName = data?.characterName;
            if (string.IsNullOrWhiteSpace(characterName))
            {
                characterName = authManager?.GetCharacterName();
            }

            if (string.IsNullOrWhiteSpace(characterName) || characterName == "Unknown")
            {
                var firebaseUser = FirebaseManager.Instance?.GetCurrentUser();
                if (firebaseUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(firebaseUser.DisplayName))
                    {
                        characterName = firebaseUser.DisplayName;
                    }
                    else if (!string.IsNullOrWhiteSpace(firebaseUser.Email))
                    {
                        characterName = firebaseUser.Email;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(characterName) && characterName != "Unknown")
            {
                characterNameText?.SetText($"Tên Nhân Vật: {characterName}");
            }
            else
            {
                characterNameText?.SetText("Tên Nhân Vật: -----");
            }

            if (data == null)
            {
                levelText?.SetText("Lv: 1");
                scoreText?.SetText("Score: 0");
                return;
            }

            levelText?.SetText($"Lv: {data.level}");
            scoreText?.SetText($"Score: {data.totalScore}");
        }

        private void HandleLogout()
        {
            authManager?.Logout();

            if (welcomeScreenRoot != null)
            {
                UIScreenRouter.TryShowRoot(welcomeScreenRoot);
                return;
            }

            UIScreenRouter.TryShowWelcome(ref flowManager);
        }
    }
}
