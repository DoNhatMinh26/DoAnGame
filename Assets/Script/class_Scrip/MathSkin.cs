using UnityEngine;

[CreateAssetMenu(fileName = "NewMath", menuName = "ClassGame/MathSkin")]
public class MathSkin : ScriptableObject
{
    public string shipName;
    public Sprite shipIcon; // Đảm bảo tên biến là shipIcon hoặc shipSprite
    public int price;
}