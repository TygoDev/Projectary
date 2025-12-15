using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Resource Tile")]
public class ResourceTile : Tile
{
    public ItemSO itemToGive;     // what item to collect
    public int amount = 1;        // how much to gather
}
