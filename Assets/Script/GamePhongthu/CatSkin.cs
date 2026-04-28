using UnityEngine;

[CreateAssetMenu(fileName = "New Skin", menuName = "Skins/CatSkin")]
public class CatSkin : ScriptableObject
{
    public string skinName;
    public Sprite skinSprite; // Hình ảnh con mèo
    public int price;
    public bool isUnlocked; // Đã mua chưa
}