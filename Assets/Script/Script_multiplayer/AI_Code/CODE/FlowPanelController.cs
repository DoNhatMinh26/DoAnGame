using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Base controller hỗ trợ binding điều hướng cho từng Button thông qua FlowButtonConfig.
    /// </summary>
    public abstract class FlowPanelController : BasePanelController
    {
        [Header("Navigation Configs (Cấu hình điều hướng)")]
        [SerializeField] private FlowButtonConfig[] navigationButtons;

        [Header("Common Buttons (Nút dùng chung)")]
        [SerializeField] private Button backButton;

        private readonly Dictionary<Button, UnityAction> navHandlers = new Dictionary<Button, UnityAction>();
        private UnityAction backHandler;

        /// <summary>
        /// Mọi panel con phải chỉ ra FlowManager tương ứng.
        /// </summary>
        protected abstract UIFlowManager FlowManager { get; }

        protected override void Awake()
        {
            base.Awake();
            RegisterNavigationButtons();
            RegisterBackButton();
        }

        protected virtual void RegisterNavigationButtons()
        {
            if (navigationButtons == null || navigationButtons.Length == 0) return;

            foreach (var config in navigationButtons)
            {
                if (!config.IsValid) continue;

                var cachedConfig = config;
                UnityAction handler = () => HandleNavigation(cachedConfig);
                cachedConfig.Button.onClick.AddListener(handler);
                navHandlers[cachedConfig.Button] = handler;
            }
        }

        protected virtual void HandleNavigation(FlowButtonConfig config)
        {
            if (TryHandleNavigationOverride(config))
            {
                return;
            }

            var manager = FlowManager;
            if (manager == null)
            {
                Debug.LogWarning($"{GetType().Name}: UIFlowManager chưa được gán nên không thể điều hướng.");
                return;
            }

            ExecuteNavigation(config, manager);
        }

        private void RegisterBackButton()
        {
            if (backButton == null)
                return;

            if (navHandlers.ContainsKey(backButton))
                return;

            backHandler = HandleBackClicked;
            backButton.onClick.AddListener(backHandler);
        }

        private void HandleBackClicked()
        {
            var manager = FlowManager;
            if (manager == null)
            {
                Debug.LogWarning($"{GetType().Name}: thiếu FlowManager nên nút Back không hoạt động.");
                return;
            }

            manager.Back();
        }

        /// <summary>
        /// Cho phép panel override để thêm logic đặc biệt trước/để thay thế điều hướng mặc định.
        /// </summary>
        protected virtual bool TryHandleNavigationOverride(FlowButtonConfig config)
        {
            return false;
        }

        protected void ExecuteNavigation(FlowButtonConfig config, UIFlowManager manager)
        {
            switch (config.Action)
            {
                case FlowButtonAction.ShowScreen:
                    var pushHistory = config.OverridePushHistory ? config.PushHistoryValue : true;
                    manager.ShowScreen(config.TargetScreen, pushHistory);
                    break;
                case FlowButtonAction.Back:
                    manager.Back();
                    break;
                case FlowButtonAction.ShowSettings:
                    manager.ShowSettings(config.OriginScreen);
                    break;
                case FlowButtonAction.ReturnFromSettings:
                    manager.ReturnFromSettings();
                    break;
                default:
                    Debug.LogWarning($"{GetType().Name}: action {config.Action} chưa được hỗ trợ.");
                    break;
            }
        }

        protected virtual void OnDestroy()
        {
            if (backButton != null && backHandler != null)
            {
                backButton.onClick.RemoveListener(backHandler);
                backHandler = null;
            }

            if (navHandlers.Count == 0) return;

            foreach (var entry in navHandlers)
            {
                entry.Key?.onClick.RemoveListener(entry.Value);
            }

            navHandlers.Clear();
        }
    }
}
