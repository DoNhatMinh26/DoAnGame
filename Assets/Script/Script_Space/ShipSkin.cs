using UnityEngine;

[CreateAssetMenu(fileName = "NewShip", menuName = "SpaceGame/ShipSkin")]
public class ShipSkin : ScriptableObject
{
    public string shipName;
    public Sprite shipSprite;
    public int price;
    public float speedBonus = 0f; // Tốc độ di chuyển cộng thêm
}