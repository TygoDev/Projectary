using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles touch input for harvesting and tile placement with a preview.
/// Refactored for clarity, small allocations reduction and bug fixes (duplicate preview clear).
/// </summary>
public class InputManager : MonoBehaviour
{
    // Input actions
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

    private Camera mainCamera;
    private Vector3Int previewCellPos;
    private bool hasPreview;

    private int rotationIndex; // 0..3 -> 0°, 90°, 180°, 270°

    // Reusable list to avoid allocating on every UI raycast
    private static readonly List<RaycastResult> s_RaycastResults = new List<RaycastResult>();

    #region Unity Lifecycle

    private void Awake()
    {
        mainCamera = Camera.main;

        CreateInputActions();

        if (placementCanvas != null)
            placementCanvas.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (tapAction != null)
        {
            tapAction.performed += OnTapPerformed;
            tapAction.Enable();
        }

        positionAction?.Enable();
    }

    private void OnDisable()
    {
        if (tapAction != null)
        {
            tapAction.performed -= OnTapPerformed;
            tapAction.Disable();
        }

        positionAction?.Disable();
    }

    private void OnDestroy()
    {
        tapAction?.Dispose();
        positionAction?.Dispose();
    }

    private void OnValidate()
    {
        // keep reference up-to-date in editor when possible
        if (Application.isPlaying == false)
            mainCamera = Camera.main;
    }

    #endregion

    #region Input

    private void CreateInputActions()
    {
        // Only create actions once
        if (tapAction != null && positionAction != null)
            return;

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

    private void OnTapPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = positionAction != null ? positionAction.ReadValue<Vector2>() : Vector2.zero;
        HandleTap(screenPos);
    }

    private void HandleTap(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return;

        if (IsPointerOverUI(screenPosition))
            return;

        if (!TryGetCellFromScreen(screenPosition, out Vector3Int cellPos))
            return;

        if (isPlacing)
            UpdatePreview(cellPos);
        else
            TryHarvest(cellPos);
    }

    private bool TryGetCellFromScreen(Vector2 screenPosition, out Vector3Int cell)
    {
        // Use camera z distance to convert screen to world (same approach as original)
        float zDistance = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        cell = groundTileMap.WorldToCell(worldPoint);
        return true;
    }

    #endregion

    #region Placement Preview

    private void UpdatePreview(Vector3Int cellPos)
    {
        if (selectedBuildingTile == null)
            return;

        if (groundTileMap == null || buildingTileMap == null || previewTileMap == null)
            return;

        if (groundTileMap.GetTile(cellPos) == null)
            return;

        if (buildingTileMap.GetTile(cellPos) != null)
            return;

        // Clear previous preview if different cell
        if (hasPreview && previewCellPos != cellPos)
            ClearPreviewInternal();

        // Set new preview
        previewTileMap.SetTile(cellPos, selectedBuildingTile);
        previewTileMap.SetTransformMatrix(cellPos, GetRotationMatrix());

        previewCellPos = cellPos;
        hasPreview = true;

        UpdatePlacementCanvasPosition();
    }

    private void UpdatePlacementCanvasPosition()
    {
        if (!hasPreview || placementCanvas == null || previewTileMap == null)
            return;

        Vector3 tileWorldPos = previewTileMap.CellToWorld(previewCellPos);
        tileWorldPos += previewTileMap.cellSize / 2f;

        placementCanvas.transform.position = tileWorldPos + canvasWorldOffset;
        placementCanvas.gameObject.SetActive(true);
    }

    public void ConfirmPlacement()
    {
        if (!hasPreview || buildingTileMap == null)
            return;

        // Place into building tilemap with rotation
        buildingTileMap.SetTile(previewCellPos, selectedBuildingTile);
        buildingTileMap.SetTransformMatrix(previewCellPos, GetRotationMatrix());

        if (selectedBuildingTile is MiningDrillTile && miningSystem != null)
            miningSystem.RegisterDrill(previewCellPos);

        // Finalize and reset placing state (CancelPlacing will clear preview once)
        CancelPlacing();
    }

    public void CancelPlacing()
    {
        // Clear any preview and reset placing state
        ClearPreviewInternal();

        isPlacing = false;
        selectedBuildingTile = null;
        rotationIndex = 0;

        if (placementCanvas != null)
            placementCanvas.gameObject.SetActive(false);
    }

    private void ClearPreviewInternal()
    {
        if (!hasPreview || previewTileMap == null)
            return;

        previewTileMap.SetTile(previewCellPos, null);
        previewTileMap.SetTransformMatrix(previewCellPos, Matrix4x4.identity);
        hasPreview = false;

        if (placementCanvas != null)
            placementCanvas.gameObject.SetActive(false);
    }

    #endregion

    #region Rotation

    public void RotateRight()
    {
        rotationIndex = (rotationIndex + 1) % 4;
        ApplyRotationToPreview();
    }

    public void RotateLeft()
    {
        rotationIndex = (rotationIndex + 3) % 4;
        ApplyRotationToPreview();
    }

    private void ApplyRotationToPreview()
    {
        if (!hasPreview || previewTileMap == null)
            return;

        previewTileMap.SetTransformMatrix(previewCellPos, GetRotationMatrix());
    }

    private Matrix4x4 GetRotationMatrix()
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, -90f * rotationIndex);
        return Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
    }

    #endregion

    #region Harvesting

    private void TryHarvest(Vector3Int cellPos)
    {
        if (groundTileMap == null)
            return;

        TileBase tile = groundTileMap.GetTile(cellPos);
        if (tile == null)
            return;

        if (tile is ResourceTile resTile)
        {
            InventoryManager.Instance.AddItem(resTile.itemToGive, resTile.amount);
        }
    }

    #endregion

    #region UI Helpers

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };

        s_RaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, s_RaycastResults);
        return s_RaycastResults.Count > 0;
    }

    #endregion

    #region UI Buttons

    public void StartPlacing(Tile buildingTile)
    {
        selectedBuildingTile = buildingTile;
        isPlacing = true;
        rotationIndex = 0;
    }

    #endregion
}
