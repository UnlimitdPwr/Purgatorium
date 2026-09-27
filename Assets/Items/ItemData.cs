using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string itemName;

    [TextArea]
    public string description;

    [Header("Sprites")]
    public Sprite worldSprite;
    public Sprite inventoryIcon;

    [Header("Inventory")]
    public int maxStack = 99;
}
