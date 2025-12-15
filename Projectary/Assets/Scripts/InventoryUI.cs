using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance;

    [SerializeField] private InventoryManager inventory;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotParent;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        inventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        inventory.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        Debug.Log("Refreshing inventory UI");
        // Clear old children
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        // Create UI slots
        foreach (var stack in inventory.items)
        {
            GameObject obj = Instantiate(slotPrefab, slotParent);
            UIInventorySlot slot = obj.GetComponent<UIInventorySlot>();
            slot.Set(stack.item, stack.quantity);
        }
    }
}
