using Fusion;
using UnityEngine;

public class Knockback : NetworkBehaviour
{
    // ===== Networked Properties =====
    [Networked] private NetworkBool _isKnockedBack { get; set; }
    [Networked] private float _knockbackTimer { get; set; }
    [Networked] private Vector2 _knockbackVelocity { get; set; }

    // ===== Private Variables =====
    private Rigidbody2D _rb;

    public bool IsKnockedBack => _isKnockedBack;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!_isKnockedBack) return;

        _knockbackTimer -= Runner.DeltaTime;
        if (_knockbackTimer <= 0) {
            EndKnockback();
            return;
        }

        _rb.linearVelocity = _knockbackVelocity;
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration) {
        if (!HasStateAuthority) return;

        _knockbackVelocity = direction.normalized * force;
        _knockbackTimer = duration;
        _isKnockedBack = true;
    }

    private void EndKnockback() {
        _isKnockedBack = false;
        _knockbackTimer = 0;
        _knockbackVelocity = Vector2.zero;
        _rb.linearVelocity = Vector2.zero;
    }
}
