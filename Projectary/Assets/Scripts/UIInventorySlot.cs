using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIInventorySlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text quantityText;

    public void Set(ItemSO item, int quantity)
    {
        icon.sprite = item.icon;
        itemName.text = item.displayName;
        quantityText.text = quantity.ToString();
    }
}
