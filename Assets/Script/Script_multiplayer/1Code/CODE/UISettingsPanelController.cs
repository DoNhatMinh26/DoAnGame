using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 14: Settings Panel
    /// </summary>
    public class UISettingsPanelController : FlowPanelController
    {
        private const string SoundKey = "Setting_Sound";
        private const string MusicKey = "Setting_Music";
        private const string LanguageKey = "Setting_Language";
        private const string GraphicsKey = "Setting_Graphics";
        private const string NotificationsKey = "Setting_Notifications";

        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Dropdown languageDropdown;
        [SerializeField] private Dropdown graphicsDropdown;
        [SerializeField] private Toggle notificationToggle;
        [SerializeField] private UIFlowManager flowManager;

        protected override UIFlowManager FlowManager => flowManager;

        protected override void Awake()
        {
            base.Awake();

            soundToggle?.onValueChanged.AddListener(value => PlayerPrefs.SetInt(SoundKey, value ? 1 : 0));
            musicToggle?.onValueChanged.AddListener(value => PlayerPrefs.SetInt(MusicKey, value ? 1 : 0));
            languageDropdown?.onValueChanged.AddListener(value => PlayerPrefs.SetInt(LanguageKey, value));
            graphicsDropdown?.onValueChanged.AddListener(value => PlayerPrefs.SetInt(GraphicsKey, value));
            notificationToggle?.onValueChanged.AddListener(value => PlayerPrefs.SetInt(NotificationsKey, value ? 1 : 0));
        }

        protected override void OnShow()
        {
            base.OnShow();
            LoadValues();
        }

        private void LoadValues()
        {
            soundToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt(SoundKey, 1) == 1);
            musicToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt(MusicKey, 1) == 1);
            languageDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt(LanguageKey, 0));
            graphicsDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt(GraphicsKey, 0));
            notificationToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt(NotificationsKey, 1) == 1);
        }
    }
}
