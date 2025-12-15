[System.Serializable]
public class InventoryItem
{
    public ItemSO item;
    public int quantity;

    public InventoryItem(ItemSO itemSO, int qty)
    {
        item = itemSO;
        quantity = qty;
    }
}
