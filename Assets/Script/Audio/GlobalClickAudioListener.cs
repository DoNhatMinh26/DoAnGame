using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

        [Tooltip("Prevent global click from firing right after a UI click.")]
        public float uiClickSuppressWindow = 0.06f;

        private static float lastUIClickTime = -999f;
        private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>(16);
        private static GlobalClickAudioListener activeInstance;

        private bool hasPendingMouseClick;

        private void Awake()
        {
            if (activeInstance != null && activeInstance != this)
            {
                enabled = false;
                return;
            }

            activeInstance = this;
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
                activeInstance = null;
        }

        public static void NotifyUIClick()
        {
            lastUIClickTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (!enableGlobalClick) return;
            if (!Input.GetMouseButtonDown(0)) return;
            hasPendingMouseClick = true;
        }

        private void LateUpdate()
        {
            if (!enableGlobalClick) return;
            if (!hasPendingMouseClick) return;
            hasPendingMouseClick = false;

            if (Time.unscaledTime - lastUIClickTime <= uiClickSuppressWindow)
                return;

            if (onlyNonUIClicks && IsPointerOverUI(Input.mousePosition))
                return;

            PlayClickSound();
        }

        private static bool IsPointerOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;

            if (es.IsPointerOverGameObject()) return true;

            var pointer = new PointerEventData(es) { position = screenPos };
            RaycastResults.Clear();
            es.RaycastAll(pointer, RaycastResults);
            return RaycastResults.Count > 0;
        }

        private void PlayClickSound()
        {
            var am = AudioManager.Instance;
            if (am == null || am.soundClick == null) return;

            am.PlaySFX(am.soundClick);
        }
    }
}
