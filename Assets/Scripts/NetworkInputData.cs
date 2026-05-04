using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    // ===== Constants =====
    public const byte DASH = 0;
    public const byte ATTACK = 1;
    public const byte NO_INVENTORY_PRESS = byte.MaxValue;

    // ===== Properties =====
    public Vector2 movementDirection;
    public Vector2 weaponAimDirection;
    public NetworkButtons buttons; // Bitmask of buttons pressed - 32 max
    public byte inventorySlotPressed;
}