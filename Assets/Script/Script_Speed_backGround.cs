using UnityEngine;
using UnityEngine.UI;
public class Script_Speed_backGround : MonoBehaviour
{
    [SerializeField] RawImage Image;
    [SerializeField] private float MoveX;
    [SerializeField] private float MoveY;
    // Update is called once per frame
    void Update()
    {
        Image.uvRect = new Rect(Image.uvRect.position + new Vector2(MoveX, MoveY) * Time.deltaTime, Image.uvRect.size);
    }
}