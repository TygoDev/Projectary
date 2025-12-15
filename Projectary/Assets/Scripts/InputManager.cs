using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Tilemaps;

public class InputManager : MonoBehaviour
{
    private InputAction tapAction;
    private InputAction positionAction;

    [SerializeField] private Tilemap groundTileMap;

    private void Awake()
    {
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
        HandlePointerAt(screenPos);
    }

    private void HandlePointerAt(Vector2 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            float distance = Mathf.Abs(cam.transform.position.z - groundTileMap.transform.position.z);
            Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
            Vector3Int cellPos = groundTileMap.WorldToCell(worldPoint);

            TileBase tile = groundTileMap.GetTile(cellPos);

            if (tile != null)
            {
                Debug.Log($"Tile clicked at {cellPos}: {tile.name}");

                // Try cast to ResourceTile
                if (tile is ResourceTile resTile)
                {
                    InventoryManager.instance.AddItem(resTile.itemToGive, resTile.amount);
                }

                return;
            }
        }

        // UI raycast fallback (optional)
        if (EventSystem.current != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            if (results.Count > 0)
            {
                Debug.Log(results[0].gameObject.name);
                return;
            }
        }

        Debug.Log($"No GameObject or Tile hit at {screenPosition}");
    }

}
