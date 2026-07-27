using UnityEngine;

/// <summary>What picking this item up actually does. One item, one meaning.</summary>
public enum ItemType
{
    Coin = 0,     // score only
    Cherry = 1,   // restores health
    Shuriken = 2  // restores ammo
}

public class Item : MonoBehaviour
{
    public ItemType itemType;
    public string itemName;
    public Sprite itemSprite;
}
