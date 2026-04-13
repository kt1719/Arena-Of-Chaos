using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Transform activeHighlight;
    [SerializeField] private WeaponInfo weaponInfo;

    public void ToggleOff()
    {
        activeHighlight.gameObject.SetActive(false);
    }

    public void ToggleOn()
    {
        activeHighlight.gameObject.SetActive(true);
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }
}
