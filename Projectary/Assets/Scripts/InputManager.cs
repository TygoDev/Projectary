using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class InputManager : MonoBehaviour
{
    private InputAction tapAction;
    private InputAction positionAction;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap buildingTileMap;

    [SerializeField] private MiningSystem miningSystem;

    [Header("Placement")]
    public bool isPlacing;
    public Tile selectedBuildingTile;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;

        tapAction = new InputAction(
            name: "Tap",
            type: InputActionType.Button,
            binding: "<Touchscreen>/primaryTouch/press",
            interactions: "tap"
        );

        positionAction = new InputAction(
            name: "TouchPosition",
            type: InputActionType.Value,
            binding: "<Touchscreen>/primaryTouch/position"
        );
    }

    private void OnEnable()
    {
        tapAction.performed += OnTapPerformed;
        tapAction.Enable();
        positionAction.Enable();
    }

    private void OnDisable()
    {
        tapAction.performed -= OnTapPerformed;
        tapAction.Disable();
        positionAction.Disable();
    }

    private void OnDestroy()
    {
        tapAction.Dispose();
        positionAction.Dispose();
    }

    private void OnTapPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = positionAction.ReadValue<Vector2>();
        HandleTap(screenPos);
    }

    private void HandleTap(Vector2 screenPosition)
    {
        if (cam == null)
            return;

        // Ignore UI taps
        if (IsPointerOverUI(screenPosition))
            return;

        Vector3 worldPoint = cam.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(cam.transform.position.z))
        );

        Vector3Int cellPos = groundTileMap.WorldToCell(worldPoint);

        if (isPlacing)
        {
            TryPlaceBuilding(cellPos);
        }
        else
        {
            TryHarvest(cellPos);
        }
    }

    #region Placement

    private void TryPlaceBuilding(Vector3Int cellPos)
    {
        if (selectedBuildingTile == null)
            return;

        // Don't place if something already exists
        if (buildingTileMap.GetTile(cellPos) != null)
            return;

        // Optional: only allow placement on valid ground
        if (groundTileMap.GetTile(cellPos) == null)
            return;

        // Place the tile
        buildingTileMap.SetTile(cellPos, selectedBuildingTile);

        Debug.Log($"Placed {selectedBuildingTile.name} at {cellPos}");

        // Check if it's a MiningDrillTile and register it
        if (selectedBuildingTile is MiningDrillTile)
        {
            if (miningSystem != null)
                miningSystem.RegisterDrill(cellPos);
        }
    }


    #endregion

    #region Harvesting

    private void TryHarvest(Vector3Int cellPos)
    {
        TileBase tile = groundTileMap.GetTile(cellPos);
        if (tile == null)
            return;

        if (tile is ResourceTile resTile)
        {
            InventoryManager.instance.AddItem(
                resTile.itemToGive,
                resTile.amount
            );

            Debug.Log($"Harvested {resTile.name} at {cellPos}");
        }
    }

    #endregion

    #region UI

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Count > 0;
    }

    #endregion

    // Called by UI buttons
    public void StartPlacing(Tile buildingTile)
    {
        selectedBuildingTile = buildingTile;
        isPlacing = true;
    }

    public void CancelPlacing()
    {
        isPlacing = false;
        selectedBuildingTile = null;
    }
}
