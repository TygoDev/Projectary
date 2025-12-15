using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Buildings/Mining Drill")]
public class MiningDrillTile : Tile
{
    public float interval = 2f; // seconds per extraction
}
