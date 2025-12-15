using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // Singleton instance
    public static InventoryManager instance;

    public List<InventoryItem> items = new List<InventoryItem>();

    private void Awake()
    {
        // If another instance exists, destroy this one
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // Optional: keeps inventory across scenes
    }

    public void AddItem(ItemSO itemSO, int amount = 1)
    {
        // Try to merge with existing stack
        foreach (var stack in items)
        {
            if (stack.item == itemSO)
            {
                stack.quantity += amount;
                Debug.Log($"Added {amount} to {itemSO.itemID} (total {stack.quantity})");
                return;
            }
        }

        // No stack existed → create new one
        InventoryItem newStack = new InventoryItem(itemSO, amount);
        items.Add(newStack);

        Debug.Log($"Created new stack {itemSO.itemID} with quantity {amount}");
    }

    public int Count(ItemSO itemSO)
    {
        foreach (var stack in items)
        {
            if (stack.item == itemSO)
                return stack.quantity;
        }
        return 0;
    }
}
