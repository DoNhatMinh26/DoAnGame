using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 6: Mode Selection
    /// </summary>
    public class UIModeSelectionController : FlowPanelController
    {
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button multiplayerButton;
        [SerializeField] private UIFlowManager flowManager;

        protected override UIFlowManager FlowManager => flowManager;

        protected override bool TryHandleNavigationOverride(FlowButtonConfig config)
        {
            if (config.Button == null)
                return false;

            if (config.Button == singlePlayerButton)
            {
                GameModeContext.SetMode(false);
            }
            else if (config.Button == multiplayerButton)
            {
                GameModeContext.SetMode(true);
            }

            return false;
        }
    }
}
