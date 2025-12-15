using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventoryManager inventory;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotParent;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
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
