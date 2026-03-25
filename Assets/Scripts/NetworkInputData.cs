using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    // ===== Constants =====
    public const byte DASH = 0;
    public const byte ATTACK = 1;
    public const byte DEBUG_ORBIT = 2;

    // ===== Properties =====
    public Vector2 movementDirection;
    public Vector2 weaponAimDirection;
    public Vector2 debugOrbitCenter;
    public float debugOrbitRadius;
    public NetworkButtons buttons; // Bitmask of buttons pressed - 32 max
}