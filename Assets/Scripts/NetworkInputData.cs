using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public const byte DASH_BUTTON = 0;

    public Vector2 movementDirection;
    public NetworkButtons buttons; // Bitmask of buttons pressed - 32 max
}