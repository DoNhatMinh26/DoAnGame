using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Một dòng trong danh sách lobby đang mở.
    /// </summary>
    public class LobbyBrowserEntryWidget : MonoBehaviour
    {
        private const float CompactRowHeight = 64f;
        private const float HorizontalInset = 14f;

        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text hostText;
        [SerializeField] private TMP_Text playerCountText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button joinButton;

        private Action joinAction;
        private RectTransform rectTransform;
        private LayoutElement layoutElement;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            ApplyCompactTextStyle(lobbyNameText);
            ApplyCompactTextStyle(hostText);
            ApplyCompactTextStyle(playerCountText);
            ApplyCompactTextStyle(statusText);
        }

        public void Bind(string lobbyName, string hostName, string countText, string lobbyStatus, Action onJoinClicked)
        {
            if (lobbyNameText != null)
            {
                lobbyNameText.SetText(lobbyName);
            }

            if (hostText != null)
            {
                hostText.SetText(hostName);
            }

            if (playerCountText != null)
            {
                playerCountText.SetText(countText);
            }

            if (statusText != null)
            {
                statusText.SetText(lobbyStatus);
            }

            joinAction = onJoinClicked;
            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(HandleJoinClicked);
                joinButton.interactable = onJoinClicked != null;
            }

            EnsureValidRowHeight();
        }

        private void OnDestroy()
        {
            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
            }
        }

        private void HandleJoinClicked()
        {
            joinAction?.Invoke();
        }

        private void EnsureValidRowHeight()
        {
            if (layoutElement == null)
                return;

            layoutElement.ignoreLayout = false;
            layoutElement.minHeight = CompactRowHeight;
            layoutElement.preferredHeight = CompactRowHeight;
            layoutElement.flexibleHeight = 0f;

            if (rectTransform != null)
            {
                if (Mathf.Abs(rectTransform.anchorMin.x) < 0.001f && Mathf.Abs(rectTransform.anchorMax.x - 1f) < 0.001f)
                {
                    rectTransform.offsetMin = new Vector2(HorizontalInset, rectTransform.offsetMin.y);
                    rectTransform.offsetMax = new Vector2(-HorizontalInset, rectTransform.offsetMax.y);
                }
                else if (rectTransform.parent is RectTransform parentRect)
                {
                    float width = Mathf.Max(320f, parentRect.rect.width - (HorizontalInset * 2f));
                    rectTransform.sizeDelta = new Vector2(width, CompactRowHeight);
                }
                else
                {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, CompactRowHeight);
                }
            }
        }

        private static void ApplyCompactTextStyle(TMP_Text text)
        {
            if (text == null)
                return;

            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.maxVisibleLines = 1;
        }
    }
}