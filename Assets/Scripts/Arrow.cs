using Fusion;
using UnityEngine;

public class Arrow : NetworkBehaviour
{
    // ===== Constants =====
    [SerializeField] private float LIFETIME = 3f;
    [SerializeField] private float SPEED = 12f;

    // ===== Networked Fields =====
    [Networked] private Vector2 _direction { get; set; }
    [Networked] private int _damage { get; set; }
    [Networked] private float _knockbackForce { get; set; }
    [Networked] private float _knockbackDuration { get; set; }
    [Networked] private TickTimer _lifetimeTimer { get; set; }
    [Networked] private NetworkBool _isAlive { get; set; }

    // ===== Private Fields =====
    private Rigidbody2D _rb;

    public void Init(Vector2 direction, int damage, float knockbackForce, float knockbackDuration)
    {
        _direction = direction.normalized;
        _damage = damage;
        _knockbackForce = knockbackForce;
        _knockbackDuration = knockbackDuration;
        _isAlive = true;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        _lifetimeTimer = TickTimer.CreateFromSeconds(Runner, LIFETIME);

        // Rotate arrow sprite to face direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Set constant velocity — NetworkRigidbody2D handles interpolation
        _rb.linearVelocity = _direction * SPEED;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !_isAlive) return;

        if (_lifetimeTimer.Expired(Runner))
        {
            DespawnArrow();
            return;
        }

        // Maintain velocity (in case something interferes)
        _rb.linearVelocity = _direction * SPEED;

        // Lag-compensated hit detection
        DetectHits();
    }

    private void DetectHits()
    {
        var hits = new System.Collections.Generic.List<LagCompensatedHit>();

        Runner.LagCompensation.OverlapSphere(
            transform.position,
            0.3f,
            Object.InputAuthority,
            hits,
            options: HitOptions.SubtickAccuracy
        );

        foreach (var hit in hits)
        {
            if (hit.Hitbox == null) continue;

            NetworkObject targetNetObj = hit.Hitbox.Root.Object;
            if (targetNetObj == null || targetNetObj.InputAuthority == Object.InputAuthority) continue;

            IHittable hittable = targetNetObj.GetComponent<IHittable>();
            if (hittable == null) continue;

            hittable.ApplyHit(_damage, _direction, _knockbackForce, _knockbackDuration);
            DespawnArrow();
            return;
        }

        // Destroy environment objects but arrow keeps going
        DetectEnvironmentHits();
    }

    private void DetectEnvironmentHits()
    {
        var collider = GetComponent<Collider2D>();
        if (collider == null) return;

        var results = new System.Collections.Generic.List<Collider2D>();
        Physics2D.OverlapCollider(collider, ContactFilter2D.noFilter, results);

        foreach (var col in results)
        {
            Destructible destructible = col.GetComponent<Destructible>();
            if (destructible == null) continue;

            destructible.DestroyGameObject();
            // Arrow does NOT despawn — passes through environment
        }
    }

    private void DespawnArrow()
    {
        if (!_isAlive) return;

        _isAlive = false;
        _rb.linearVelocity = Vector2.zero;
        Runner.Despawn(Object);
    }
}
