using Fusion;
using UnityEngine;
public class PlayerColorTest : NetworkBehaviour {
    [Networked] private Color _color { get; set; }
    public MeshRenderer _meshRenderer;
}