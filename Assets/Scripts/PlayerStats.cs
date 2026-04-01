using System;
using Fusion;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    // ===== Movement Base Attributes =====
    [Networked] public float MoveSpeed { get; set; }
    [Networked] public float DashSpeedMultiplier { get; set; }
    [Networked] public float DashTotalDuration { get; set; }
    [Networked] public float DashTotalCooldown { get; set; }

    // ===== Combat Base Attributes =====
    [Networked] public float MaxHealth { get; set; }
    [Networked] public float CurrentHealth { get; set; }
    [Networked] public int BaseDamage { get; set; }

    // ===== Configurable Defaults (Inspector) =====
    [SerializeField] private float _defaultMoveSpeed = 5f;
    [SerializeField] private float _defaultDashSpeedMultiplier = 4f;
    [SerializeField] private float _defaultDashTotalDuration = 0.5f;
    [SerializeField] private float _defaultDashTotalCooldown = 0.1f;
    [SerializeField] private float _defaultMaxHealth = 100f;
    [SerializeField] private int _defaultBaseDamage = 10;

    // ===== Change Detection =====
    private ChangeDetector _changeDetector;

    // ===== Events =====
    public event Action<float, float> OnHealthChanged; // (newHealth, maxHealth)

    public override void Spawned()
    {
        MoveSpeed = _defaultMoveSpeed;
        DashSpeedMultiplier = _defaultDashSpeedMultiplier;
        DashTotalDuration = _defaultDashTotalDuration;
        DashTotalCooldown = _defaultDashTotalCooldown;
        MaxHealth = _defaultMaxHealth;
        CurrentHealth = MaxHealth;
        BaseDamage = _defaultBaseDamage;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
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


}
