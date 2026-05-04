using System;
using System.Collections.Generic;
using UnityEngine;

public class ActiveInventory : MonoBehaviour
{
    public Action<WeaponInfo> OnChangeActiveWeapon;

    [SerializeField] private List<InventorySlot> inventorySlots;

    public ActiveInventory Instance;

    private InventorySlot currentActiveSlot;

    private void Awake()
    {
        if (Instance == null) {
            Instance = this;
        }

        currentActiveSlot = inventorySlots[0]; // Set the first as the default
    }

    private void Start()
    {
        GameInput.Instance.OnPlayerInventory += GameInput_OnPlayerInventory;

        // Reset
        OnChangeActiveWeapon?.Invoke(currentActiveSlot.GetWeaponInfo());
    }

    private void GameInput_OnPlayerInventory(GameInput.OnPlayerInventoryArgs e)
    {
        int pressedNum = e.pressedButton;
        ToggleActiveHighlight(pressedNum);
    }

    private void ToggleActiveHighlight(int indexNum)
    {
        if (indexNum < 0 || indexNum >= inventorySlots.Count) return;
        if (inventorySlots[indexNum].GetWeaponInfo() == null) return;
        if (currentActiveSlot == inventorySlots[indexNum]) return;

        currentActiveSlot.ToggleOff();

        currentActiveSlot = inventorySlots[indexNum];
        currentActiveSlot.ToggleOn();

        // OnChangeActiveWeapon.Invoke(currentActiveSlot.GetWeaponInfo());
    }
}
