using UnityEngine;
using UnityEngine.UI;

public partial class BackgroundFollow : MonoBehaviour
{
    public RectTransform content; // Kéo cái 'Content' vào đây
    public RectTransform background; // Kéo cái 'BRchonman1' vào đây

    void Update()
    {
        if (content != null && background != null)
        {
            // Cập nhật vị trí X của nền theo vị trí X của Content
            Vector2 bgPos = background.anchoredPosition;
            bgPos.x = content.anchoredPosition.x;
            background.anchoredPosition = bgPos;
        }
    }
}