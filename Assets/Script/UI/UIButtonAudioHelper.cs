using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Auto-adds click sound to buttons under this Canvas (or globally if needed).
    /// Attach to Canvas root and enable `autoSetupOnStart`.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIButtonAudioHelper : MonoBehaviour
    {
        [Tooltip("If true, will auto hook buttons in children on Start")]
        public bool autoSetupOnStart = true;

        [Tooltip("If true, only hook buttons that are children of this GameObject; otherwise will hook all buttons in scene")]
        public bool hookChildrenOnly = true;

        void Start()
        {
            if (autoSetupOnStart) SetupAllButtons();
        }

        public void SetupAllButtons()
        {
            Button[] buttons = hookChildrenOnly ? GetComponentsInChildren<Button>(true) : FindObjectsOfType<Button>(true);

            foreach (var b in buttons)
            {
                if (b == null) continue;
                b.onClick.RemoveListener(PlayButtonClickSound);
                b.onClick.AddListener(PlayButtonClickSound);
            }

            Debug.Log($"[UIButtonAudioHelper] Hooked {buttons.Length} buttons for click sound");
        }

        private void PlayButtonClickSound()
        {
            var am = AudioManager.Instance;
            if (am == null) return;

            // prefer manager field if present
            try
            {
                var clip = am.soundClick;
                if (clip != null) am.PlaySFX(clip);
            }
            catch
            {
                // fallback: try PlaySFX(null) (no-op)
                try { am.PlaySFX(null); } catch { }
            }
        }
    }
}
