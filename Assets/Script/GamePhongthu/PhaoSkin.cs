using UnityEngine;

[CreateAssetMenu(fileName = "New Phao", menuName = "Skins/PhaoSkin")]
public class PhaoSkin : ScriptableObject
{
    public string phaoName;
    public Sprite phaoSprite; // Hình ảnh khẩu pháo
    public int price;
}