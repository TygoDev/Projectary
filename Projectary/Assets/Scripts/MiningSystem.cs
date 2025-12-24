using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Manages mining drills placed on the building tilemap.
/// Keeps an explicit set of active drill cell positions and advances per-drill timers each Update.
/// This avoids scanning the entire tilemap every frame and allows deterministic register/unregister when drills are placed/removed.
/// </summary>
public class MiningSystem : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap buildingTilemap;

    // Active drill cell positions — maintained when drills are placed/removed.
    // Iterate this set in Update instead of scanning the whole tilemap bounds.
    private readonly HashSet<Vector3Int> activeDrillCells = new();

    // Per-drill timers keyed by cell position.
    private readonly Dictionary<Vector3Int, float> drillTimers = new();

    private void Awake()
    {
        // Tighten the tilemap bounds if possible to reduce any future full-scans.
        if (buildingTilemap != null)
            buildingTilemap.CompressBounds();

        // Populate the active set from the tilemap at startup so existing drills are tracked.
        RebuildActiveDrillSetFromTilemap();
    }

    private void Update()
    {
        if (buildingTilemap == null || groundTilemap == null)
            return;

        // Iterate a snapshot because Register/Unregister might be called externally during the Update loop.
        var snapshot = new List<Vector3Int>(activeDrillCells);

        foreach (var cell in snapshot)
        {
            // If the tile at this cell is no longer a MiningDrillTile, unregister it.
            if (!buildingTilemap.HasTile(cell))
            {
                UnregisterDrill(cell);
                continue;
            }

            TileBase buildTile = buildingTilemap.GetTile(cell);
            if (buildTile is not MiningDrillTile drill)
            {
                UnregisterDrill(cell);
                continue;
            }

            // If there is no resource tile under this cell, skip this drill for now.
            TileBase groundTile = groundTilemap.GetTile(cell);
            if (groundTile is not ResourceTile resource)
                continue;

            // Advance timer (get-or-create pattern).
            if (!drillTimers.TryGetValue(cell, out float timer))
                timer = 0f;

            timer += Time.deltaTime;

            if (timer >= drill.interval)
            {
                timer = 0f;

                // Replace to check for connected belts, etc. in future.
                if (InventoryManager.instance != null)
                    InventoryManager.instance.AddItem(resource.itemToGive, resource.amount);
            }

            drillTimers[cell] = timer;
        }
    }

    /// <summary>
    /// Register a drill at the given cell position so it will be processed each Update.
    /// Call this when placing a drill tile (for example from a placement manager).
    /// </summary>
    /// <param name="cell">Cell position of the drill in tilemap coordinates.</param>
    public void RegisterDrill(Vector3Int cell)
    {
        activeDrillCells.Add(cell);
        if (!drillTimers.ContainsKey(cell))
            drillTimers[cell] = 0f;
    }

    /// <summary>
    /// Unregister a drill at the given cell position. This also removes any timer state.
    /// Call this when removing a drill tile.
    /// </summary>
    /// <param name="cell">Cell position to remove.</param>
    public void UnregisterDrill(Vector3Int cell)
    {
        activeDrillCells.Remove(cell);
        drillTimers.Remove(cell);
    }

    /// <summary>
    /// Rebuilds the active drill set by scanning the building tilemap.
    /// Useful after loading a scene or when the tilemap is modified in bulk.
    /// </summary>
    public void RebuildActiveDrillSetFromTilemap()
    {
        activeDrillCells.Clear();
        drillTimers.Clear();

        if (buildingTilemap == null)
            return;

        // Compress bounds and scan current tiles once.
        buildingTilemap.CompressBounds();
        BoundsInt bounds = buildingTilemap.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!buildingTilemap.HasTile(cell))
                continue;

            if (buildingTilemap.GetTile(cell) is MiningDrillTile)
                activeDrillCells.Add(cell);
        }
    }
}
