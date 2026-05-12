using UnityEngine;

[CreateAssetMenu(fileName = "New Skin", menuName = "Skins/CatSkin")]
public class CatSkin : ScriptableObject
{
    public string skinName;
    public Sprite skinSprite; // Icon hiển thị trong Shop
    public int price;
    // Không cần isUnlocked ở đây vì chúng ta dùng PlayerPrefs để lưu trữ trạng thái mua của từng người chơi
}