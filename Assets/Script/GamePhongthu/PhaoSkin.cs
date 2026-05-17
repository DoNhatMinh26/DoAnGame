using UnityEngine;

[CreateAssetMenu(fileName = "New Phao", menuName = "Skins/PhaoSkin")]
public class PhaoSkin : ScriptableObject
{
    public string phaoName;
    public Sprite phaoSprite; // Icon hiển thị đại diện trong các ô của Shop UI
    public int price;
}