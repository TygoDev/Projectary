using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject menu;

    private void Start()
    {
        // Ensure menu starts hidden (optional)
        if (menu != null)
            menu.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (menu == null)
            return;

        menu.SetActive(!menu.activeSelf);
    }
}
