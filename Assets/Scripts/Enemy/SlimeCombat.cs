using System;
using Fusion;
using UnityEngine;

public class SlimeCombat : NetworkBehaviour, IHittable
{
    // ===== Networked Fields =====
    [HideInInspector][Networked] public float MaxHealth { get; set; }
    [HideInInspector][Networked] public float CurrentHealth { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private float _defaultMaxHealth = 50f;
    [SerializeField] private Knockback _knockback;
    [SerializeField] private SlimeVisual _slimeVisual;

    // ===== Events =====
    public event Action<float, float> OnHealthChanged; // (newHealth, maxHealth)

    // ===== Change Detection =====
    private ChangeDetector _changeDetector;

    // ===== Lifecycle =====

    public override void Spawned() {
        if (HasStateAuthority)
        {
            MaxHealth = _defaultMaxHealth;
            CurrentHealth = MaxHealth;
            _knockback.OnKnockbackEnd += CheckDeath;
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (_knockback != null) _knockback.OnKnockbackEnd -= CheckDeath;
    }

    public override void Render() {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CurrentHealth))
                OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
    }

    // ===== Public API =====

    public void ApplyHit(int damage, Vector2 hitDirection, float knockbackForce, float knockbackDuration) {
        if (!HasStateAuthority) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        RPC_TriggerHitFlash();
        _knockback.ApplyKnockback(hitDirection, knockbackForce, knockbackDuration);
    }

    // ===== Private =====

    private void CheckDeath() {
        if (CurrentHealth <= 0)
            Die();
    }

    private void Die() {
        Runner.Despawn(Object);
    }

    // ===== RPC =====

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    private void RPC_TriggerHitFlash() {
        _slimeVisual.TriggerHitFlash();
    }
}
