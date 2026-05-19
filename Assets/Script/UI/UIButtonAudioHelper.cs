using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DoAnGame.Audio;

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
                var relay = b.GetComponent<UIButtonClickSfxRelay>();
                if (relay == null) relay = b.gameObject.AddComponent<UIButtonClickSfxRelay>();
                relay.enabled = true;
            }

            Debug.Log($"[UIButtonAudioHelper] Ensured relay on {buttons.Length} buttons");
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class UIButtonClickSfxRelay : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            PlaySharedClick();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            // Keyboard/controller submit should behave like button click.
            PlaySharedClick();
        }

        private static void PlaySharedClick()
        {
            GlobalClickAudioListener.NotifyUIClick();

            var am = AudioManager.Instance;
            if (am == null || am.soundClick == null) return;
            am.PlaySFX(am.soundClick);
        }
    }
}
