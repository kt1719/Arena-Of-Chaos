using System;
using Fusion;
using UnityEngine;

public class SlimeCombat : NetworkBehaviour, IHittable
{
    // ===== Networked Fields =====
    [Networked] public float MaxHealth { get; set; }
    [Networked] public float CurrentHealth { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private float _defaultMaxHealth = 50f;
    [SerializeField] private Knockback _knockback;
    [SerializeField] private SlimeVisual _slimeVisual;

    // ===== Change Detection =====
    private ChangeDetector _changeDetector;

    // ===== Events =====
    public event Action<float, float> OnHealthChanged; // (newHealth, maxHealth)

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            MaxHealth = _defaultMaxHealth;
            CurrentHealth = MaxHealth;
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentHealth):
                    OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
                    break;
            }
        }
    }

    public void ApplyHit(int damage, Vector2 hitDirection, float knockbackForce, float knockbackDuration)
    {
        if (!HasStateAuthority) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        RPC_TriggerHitFlash();

        _knockback.ApplyKnockback(hitDirection, knockbackForce, knockbackDuration);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    private void RPC_TriggerHitFlash()
    {
        _slimeVisual.TriggerHitFlash();
    }

    private void Die()
    {
        Runner.Despawn(Object);
    }
}
