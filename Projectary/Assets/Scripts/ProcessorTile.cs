using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Buildings/Processor")]
public class ProcessorTile : Tile
{
    public float interval = 2f; // seconds per extraction
}
