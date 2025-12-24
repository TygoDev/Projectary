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
    [SerializeField] private Tilemap previewTileMap;

    [SerializeField] private MiningSystem miningSystem;

    [Header("Placement UI (World Space)")]
    [SerializeField] private Canvas placementCanvas;
    [SerializeField] private Vector3 canvasWorldOffset = new Vector3(0f, -0.6f, 0f);

    [Header("Placement State")]
    public bool isPlacing;
    public Tile selectedBuildingTile;

    private Camera cam;
    private Vector3Int previewCellPos;
    private bool hasPreview;

    #region Unity Lifecycle

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

        if (placementCanvas != null)
            placementCanvas.gameObject.SetActive(false);
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

    private void Start()
    {
        StartPlacing(selectedBuildingTile);
    }

    #endregion

    #region Input Handling

    private void OnTapPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = positionAction.ReadValue<Vector2>();
        HandleTap(screenPos);
    }

    private void HandleTap(Vector2 screenPosition)
    {
        if (cam == null)
            return;

        if (IsPointerOverUI(screenPosition))
            return;

        Vector3 worldPoint = cam.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(cam.transform.position.z))
        );

        Vector3Int cellPos = groundTileMap.WorldToCell(worldPoint);

        if (isPlacing)
            UpdatePreview(cellPos);
        else
            TryHarvest(cellPos);
    }

    #endregion

    #region Placement Preview

    private void UpdatePreview(Vector3Int cellPos)
    {
        if (selectedBuildingTile == null)
            return;

        if (groundTileMap.GetTile(cellPos) == null)
            return;

        if (buildingTileMap.GetTile(cellPos) != null)
            return;

        if (hasPreview)
            previewTileMap.SetTile(previewCellPos, null);

        previewTileMap.SetTile(cellPos, selectedBuildingTile);
        previewCellPos = cellPos;
        hasPreview = true;

        UpdatePlacementCanvasPosition();
    }

    private void UpdatePlacementCanvasPosition()
    {
        if (!hasPreview || placementCanvas == null)
            return;

        Vector3 tileWorldPos = previewTileMap.CellToWorld(previewCellPos);
        tileWorldPos += previewTileMap.cellSize / 2f;

        placementCanvas.transform.position = tileWorldPos + canvasWorldOffset;
        placementCanvas.gameObject.SetActive(true);
    }

    public void ConfirmPlacement()
    {
        if (!hasPreview)
            return;

        buildingTileMap.SetTile(previewCellPos, selectedBuildingTile);

        if (selectedBuildingTile is MiningDrillTile && miningSystem != null)
            miningSystem.RegisterDrill(previewCellPos);

        ClearPreview();
        CancelPlacing();
    }

    public void CancelPlacing()
    {
        ClearPreview();
        isPlacing = false;
        selectedBuildingTile = null;

        if (placementCanvas != null)
            placementCanvas.gameObject.SetActive(false);
    }

    private void ClearPreview()
    {
        if (!hasPreview)
            return;

        previewTileMap.SetTile(previewCellPos, null);
        hasPreview = false;

        if (placementCanvas != null)
            placementCanvas.gameObject.SetActive(false);
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
        }
    }

    #endregion

    #region UI Helpers

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

    #region UI Buttons

    public void StartPlacing(Tile buildingTile)
    {
        selectedBuildingTile = buildingTile;
        isPlacing = true;
    }

    #endregion
}
