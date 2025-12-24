using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // Singleton instance (property-style for safer access)
    public static InventoryManager Instance { get; private set; }

    [SerializeField]
    private List<InventoryItem> items = new List<InventoryItem>();

    // Safe, read-only view for external code
    public IReadOnlyList<InventoryItem> Items => items.AsReadOnly();

    // Event fired whenever inventory changes
    public event Action OnInventoryChanged;

    private void Awake()
    {
        // If another instance exists, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: keeps inventory across scenes
    }

    public void AddItem(ItemSO itemSO, int amount = 1)
    {
        if (itemSO == null) throw new ArgumentNullException(nameof(itemSO));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        // Try to merge with existing stack
        var stack = items.Find(s => s.item == itemSO);
        if (stack != null)
        {
            checked { stack.quantity += amount; }
        }
        else
        {
            // No stack existed → create new one
            items.Add(new InventoryItem(itemSO, amount));
        }

        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemSO itemSO, int amount = 1)
    {
        if (itemSO == null) throw new ArgumentNullException(nameof(itemSO));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        var stack = items.Find(s => s.item == itemSO);
        if (stack == null || stack.quantity < amount)
            return false;

        stack.quantity -= amount;
        if (stack.quantity == 0)
            items.Remove(stack);

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int Count(ItemSO itemSO)
    {
        if (itemSO == null) return 0;
        var stack = items.Find(s => s.item == itemSO);
        return stack?.quantity ?? 0;
    }

    public void Clear()
    {
        if (items.Count == 0) return;
        items.Clear();
        OnInventoryChanged?.Invoke();
    }
}
