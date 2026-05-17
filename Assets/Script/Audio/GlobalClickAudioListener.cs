using UnityEngine;
using UnityEngine.EventSystems;

namespace DoAnGame.Audio
{
    /// <summary>
    /// Plays the shared button click sound when the user clicks outside UI.
    /// Button clicks are handled by UIButtonAudioHelper, using the same AudioManager.soundClick clip.
    /// </summary>
    [DisallowMultipleComponent]
    public class GlobalClickAudioListener : MonoBehaviour
    {
        [Tooltip("Play shared button click sound when clicking outside UI")]
        public bool enableGlobalClick = true;

        [Tooltip("If true, only play when clicking outside UI to avoid double sound on buttons")]
        public bool onlyNonUIClicks = true;

        private void Update()
        {
            if (!enableGlobalClick) return;
            if (!Input.GetMouseButtonDown(0)) return;

            if (onlyNonUIClicks && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            PlayClickSound();
        }

        private void PlayClickSound()
        {
            var am = AudioManager.Instance;
            if (am == null || am.soundClick == null) return;

            am.PlaySFX(am.soundClick);
        }
    }
}
