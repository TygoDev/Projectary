using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Buildings/Coal Power Plant")]
public class CoalPowerPlantTile : Tile
{
    public float power = 2f; // seconds per extraction
}
