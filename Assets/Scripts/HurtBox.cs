using UnityEngine;

/// <summary>
/// Marker component placed on a child GameObject that acts as a damage receiver.
/// Holds an explicit reference to the owner that implements <see cref="IHittable"/>,
/// so hit-detection code can resolve the owner via a single GetComponent on the
/// hitbox without walking the parent hierarchy.
/// </summary>
[DisallowMultipleComponent]
public class HurtBox : MonoBehaviour
{
    // ===== Serialized Fields =====
    [SerializeField] private MonoBehaviour _owner;

    // ===== Public Properties =====
    public IHittable Owner { get; private set; }
    public Transform OwnerTransform { get; private set; }

    // ===== Lifecycle =====

    private void Awake()
    {
        Owner = _owner as IHittable;
        OwnerTransform = _owner != null ? _owner.transform : null;

        if (Owner == null)
        {
            Debug.LogError($"[HurtBox] Owner on {gameObject.name} does not implement IHittable.", this);
        }
    }
}
