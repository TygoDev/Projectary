using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public string itemID;         // Must be unique
    public Sprite icon;           // Optional
    public string displayName;    // Optional
}
