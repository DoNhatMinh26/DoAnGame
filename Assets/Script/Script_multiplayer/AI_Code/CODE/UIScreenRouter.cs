using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// Router dùng chung cho các controller UI auth.
    /// Tránh để controller phụ thuộc trực tiếp vào navigator của button submit.
    /// </summary>
    public static class UIScreenRouter
    {
        public static bool TryShowWelcome(ref UIFlowManager flowManager)
        {
            if (TryShow(ref flowManager, UIFlowManager.Screen.WelcomeAuth, false))
                return true;

            Transform screensRoot = FindScreensRoot();
            if (screensRoot == null)
                return false;

            Transform welcomeRoot = FindByName(screensRoot, "WELCOMESCREEN");
            if (welcomeRoot == null)
            {
                welcomeRoot = FindByName(screensRoot, "WelcomePanel");
            }

            return TryShowRoot(welcomeRoot != null ? welcomeRoot.gameObject : null);
        }

        public static bool TryShowRoot(GameObject targetRoot)
        {
            if (targetRoot == null)
                return false;

            Transform screensRoot = FindScreensRoot();
            if (screensRoot == null)
            {
                targetRoot.SetActive(true);
                return true;
            }

            for (int i = 0; i < screensRoot.childCount; i++)
            {
                Transform child = screensRoot.GetChild(i);
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

            var targetPanel = targetRoot.GetComponent<BasePanelController>();
            if (targetPanel != null)
            {
                targetPanel.Show();
            }
            else
            {
                targetRoot.SetActive(true);
            }

            var rect = targetRoot.transform as RectTransform;
            if (rect != null)
            {
                NormalizeRect(rect);
            }

            targetRoot.transform.SetAsLastSibling();
            return true;
        }

        public static bool TryShow(ref UIFlowManager flowManager, UIFlowManager.Screen targetScreen, bool pushHistory = true)
        {
            Debug.Log($"[UIScreenRouter] TryShow => target:{targetScreen}, hasFlowManager:{(flowManager != null)}");

            if (flowManager == null)
            {
                flowManager = Object.FindObjectOfType<UIFlowManager>(true);
            }

            if (flowManager != null)
            {
                bool routed = flowManager.TryShowScreen(targetScreen, pushHistory);
                Debug.Log($"[UIScreenRouter] FlowManager route result => {routed}");
                return routed;
            }

            bool fallbackRouted = TryShowWithoutFlowManager(targetScreen);
            Debug.Log($"[UIScreenRouter] Fallback route result => {fallbackRouted}");
            return fallbackRouted;
        }

        private static bool TryShowWithoutFlowManager(UIFlowManager.Screen targetScreen)
        {
            Transform screensRoot = FindScreensRoot();
            if (screensRoot == null)
            {
                Debug.LogWarning($"[UIScreenRouter] Không tìm thấy UIFlowManager hoặc screensRoot để chuyển tới {targetScreen}.");
                return false;
            }

            Transform target = ResolveFallbackTarget(screensRoot, targetScreen);
            if (target == null)
            {
                Debug.LogWarning($"[UIScreenRouter] Không tìm thấy panel fallback cho {targetScreen} dưới '{screensRoot.name}'.");
                return false;
            }

            for (int i = 0; i < screensRoot.childCount; i++)
            {
                Transform child = screensRoot.GetChild(i);
                if (child != null)
                {
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
            }

            var targetPanel = target.GetComponent<BasePanelController>();
            if (targetPanel != null)
            {
                targetPanel.Show();
            }
            else
            {
                target.gameObject.SetActive(true);
            }
            target.SetAsLastSibling();
            NormalizeRect(target as RectTransform);
            return true;
        }

        private static Transform ResolveFallbackTarget(Transform screensRoot, UIFlowManager.Screen screen)
        {
            switch (screen)
            {
                case UIFlowManager.Screen.Login:
                    return FindByController<UILoginPanelController>(screensRoot);
                case UIFlowManager.Screen.Register:
                    return FindByController<UIRegisterPanelController>(screensRoot);
                case UIFlowManager.Screen.MainMenu:
                    return FindByNameOrController<UIMainMenuController>(screensRoot, "MainMenuPanel");
                case UIFlowManager.Screen.ModeSelection:
                    return FindByNameOrController<UIModeSelectionController>(screensRoot, "ModSelectionPanel");
                case UIFlowManager.Screen.WelcomeAuth:
                    {
                        Transform welcome = FindByNameOrController<UIWelcomeIntroController>(screensRoot, "WelcomePanel");
                        if (welcome == null)
                        {
                            welcome = FindByName(screensRoot, "WELCOMESCREEN");
                        }

                        return welcome;
                    }
                default:
                    return FindByName(screensRoot, GetLegacyFallbackPanelName(screen));
            }
        }

        private static Transform FindByController<T>(Transform root) where T : Component
        {
            T controller = root.GetComponentInChildren<T>(true);
            return controller != null ? controller.transform : null;
        }

        private static Transform FindByNameOrController<T>(Transform root, string legacyName) where T : Component
        {
            Transform byController = FindByController<T>(root);
            if (byController != null)
                return byController;

            return FindByName(root, legacyName);
        }

        private static Transform FindByName(Transform root, string panelName)
        {
            if (string.IsNullOrEmpty(panelName))
                return null;

            return root.Find(panelName);
        }

        private static Transform FindScreensRoot()
        {
            GameObject canvasRoot = GameObject.Find("GameUICanvas");
            if (canvasRoot != null)
            {
                return canvasRoot.transform;
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>(true);
            return canvas != null ? canvas.transform : null;
        }

        private static string GetLegacyFallbackPanelName(UIFlowManager.Screen screen)
        {
            switch (screen)
            {
                case UIFlowManager.Screen.WelcomeAuth:
                    return "WELCOMESCREEN";
                case UIFlowManager.Screen.Login:
                    return "LoginPanel";
                case UIFlowManager.Screen.Register:
                    return "RegisterPanel";
                case UIFlowManager.Screen.MainMenu:
                    return "MainMenuPanel";
                case UIFlowManager.Screen.ModeSelection:
                    return "ModSelectionPanel";
                default:
                    return null;
            }
        }

        private static void NormalizeRect(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}