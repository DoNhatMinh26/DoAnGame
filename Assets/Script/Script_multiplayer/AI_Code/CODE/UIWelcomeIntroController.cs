using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 1: Welcome Screen (INTRO)
    /// </summary>
    public class UIWelcomeIntroController : FlowPanelController
    {
        [SerializeField] private UIFlowManager flowManager;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueStatusText;

        [Header("Bridge sang scene GameUIPlay (Chuyển scene)")]
        [SerializeField] private string gameplaySceneName = "GameUIPlay";
        [SerializeField] private LoadSceneMode loadMode = LoadSceneMode.Single;

        private AuthManager authManager;
        private bool isContinueBusy;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();
            authManager = AuthManager.Instance;

            if (continueButton == null)
            {
                var found = transform.Find("ChoiTiep") ?? transform.Find("ContinueButton") ?? transform.Find("TiepTuc");
                if (found != null)
                {
                    continueButton = found.GetComponent<Button>();
                }
            }

            SetupContinueButtonStrict();
        }

        private void OnEnable()
        {
            SetupContinueButtonStrict();
        }

        private void SetupContinueButtonStrict()
        {
            if (continueButton == null)
                return;

            var navigator = continueButton.GetComponent<UIButtonScreenNavigator>();
            if (navigator != null)
            {
                navigator.enabled = false;
            }

            // Chỉ cho phép 1 luồng duy nhất: kiểm tra session qua TryContinueAsync.
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(HandleContinueClicked);
        }

        private void HandleContinueClicked()
        {
            _ = TryContinueAsync(true);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            continueButton?.onClick.RemoveAllListeners();
        }

        protected override bool TryHandleNavigationOverride(FlowButtonConfig config)
        {
            if (config.Action == FlowButtonAction.ShowScreen)
            {
                if (flowManager != null)
                    return false;

                LoadGameScene(config.TargetScreen);
                return true;
            }

            if (config.Action == FlowButtonAction.ShowSettings)
            {
                if (flowManager != null)
                    return false;

                LoadGameScene(UIFlowManager.Screen.Settings);
                return true;
            }

            return false;
        }

        private void LoadGameScene(UIFlowManager.Screen targetScreen)
        {
            if (string.IsNullOrEmpty(gameplaySceneName))
            {
                Debug.LogError("[UIWelcomeIntro] gameplaySceneName chưa được cấu hình");
                return;
            }

            SceneFlowBridge.RequestScreen(targetScreen);
            SceneManager.LoadScene(gameplaySceneName, loadMode);
        }

        private async Task TryContinueAsync(bool showFailureMessage)
        {
            if (isContinueBusy)
                return;

            if (authManager == null)
            {
                authManager = AuthManager.Instance;
            }

            if (authManager == null)
            {
                if (showFailureMessage)
                {
                    SetContinueStatus("Auth chưa sẵn sàng");
                }
                return;
            }

            isContinueBusy = true;
            try
            {
                SetContinueStatus("Đang kiểm tra phiên đăng nhập...");
                bool autoLoaded = await authManager.CheckAndAutoLogin();
                if (!autoLoaded)
                {
                    if (showFailureMessage)
                    {
                        SetContinueStatus("Chưa có phiên lưu, vui lòng đăng nhập");
                    }

                    EnsureStayOnWelcome();
                    return;
                }

                SetContinueStatus("Đã khôi phục phiên. Đang vào menu...");
                await Task.Delay(120);
                NavigateToMainMenu();
            }
            finally
            {
                isContinueBusy = false;
            }
        }

        private void NavigateToMainMenu()
        {
            bool routed = UIScreenRouter.TryShow(ref flowManager, UIFlowManager.Screen.MainMenu);
            if (routed && IsMainMenuVisible())
            {
                return;
            }

            if (TryShowMainMenuByName())
            {
                return;
            }

            // Tránh reload lặp đúng scene đích gây trạng thái trắng khi panel map chưa sẵn.
            if (SceneManager.GetActiveScene().name == gameplaySceneName)
            {
                SetContinueStatus("Không tìm thấy MainMenu trong scene hiện tại");
                EnsureStayOnWelcome();
                return;
            }

            // Khi đang ở scene intro khác thì chuyển scene và yêu cầu mở MainMenu.
            LoadGameScene(UIFlowManager.Screen.MainMenu);
        }

        private bool IsMainMenuVisible()
        {
            Transform mainMenu = FindMainMenuTransform();
            return mainMenu != null && mainMenu.gameObject.activeInHierarchy;
        }

        private bool TryShowMainMenuByName()
        {
            Transform canvas = GameObject.Find("GameUICanvas")?.transform;
            if (canvas == null)
                return false;

            Transform mainMenu = FindMainMenuTransform();
            if (mainMenu == null)
                return false;

            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (child == null)
                    continue;

                var panel = child.GetComponent<BasePanelController>();
                if (panel != null)
                {
                    panel.Hide();
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }

            var menuPanel = mainMenu.GetComponent<BasePanelController>();
            if (menuPanel != null)
            {
                menuPanel.Show();
            }
            else
            {
                mainMenu.gameObject.SetActive(true);
            }

            mainMenu.SetAsLastSibling();
            return true;
        }

        private Transform FindMainMenuTransform()
        {
            Transform canvas = GameObject.Find("GameUICanvas")?.transform;
            if (canvas == null)
                return null;

            Transform byName = canvas.Find("MainMenuPanel");
            if (byName != null)
                return byName;

            var byController = canvas.GetComponentInChildren<UIMainMenuController>(true);
            return byController != null ? byController.transform : null;
        }

        private void SetContinueStatus(string message)
        {
            if (continueStatusText == null)
                return;

            continueStatusText.text = message;
        }

        private void EnsureStayOnWelcome()
        {
            if (flowManager != null)
            {
                flowManager.ShowScreen(UIFlowManager.Screen.WelcomeIntro, false);
                return;
            }

            Transform canvas = GameObject.Find("GameUICanvas")?.transform;
            if (canvas == null)
                return;

            Transform welcomePanel = canvas.Find("WelcomePanel") ?? canvas.Find("WELCOMESCREEN");
            if (welcomePanel != null)
            {
                var welcomeController = welcomePanel.GetComponent<BasePanelController>();
                if (welcomeController != null)
                {
                    welcomeController.Show();
                }
                else
                {
                    welcomePanel.gameObject.SetActive(true);
                }

                welcomePanel.SetAsLastSibling();
            }

            Transform mainMenu = FindMainMenuTransform();
            if (mainMenu == null)
                return;

            var panel = mainMenu.GetComponent<BasePanelController>();
            if (panel != null)
            {
                panel.Hide();
            }
            else
            {
                mainMenu.gameObject.SetActive(false);
            }
        }
    }
}
