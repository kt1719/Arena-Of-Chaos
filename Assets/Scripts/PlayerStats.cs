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
        ValidateAndClampDefaults();

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

    private void ValidateAndClampDefaults()
    {
        if (_defaultMoveSpeed <= 0f)
        {
            Debug.LogWarning($"[PlayerStats] _defaultMoveSpeed is {_defaultMoveSpeed}, clamping to 0.1", this);
            _defaultMoveSpeed = 0.1f;
        }

        if (_defaultDashSpeedMultiplier <= 0f)
        {
            Debug.LogWarning($"[PlayerStats] _defaultDashSpeedMultiplier is {_defaultDashSpeedMultiplier}, clamping to 0.1", this);
            _defaultDashSpeedMultiplier = 0.1f;
        }

        if (_defaultDashTotalDuration <= 0f)
        {
            Debug.LogWarning($"[PlayerStats] _defaultDashTotalDuration is {_defaultDashTotalDuration}, clamping to 0.01", this);
            _defaultDashTotalDuration = 0.01f;
        }

        if (_defaultDashTotalCooldown < 0f)
        {
            Debug.LogWarning($"[PlayerStats] _defaultDashTotalCooldown is {_defaultDashTotalCooldown}, clamping to 0", this);
            _defaultDashTotalCooldown = 0f;
        }

        if (_defaultMaxHealth <= 0f)
        {
            Debug.LogWarning($"[PlayerStats] _defaultMaxHealth is {_defaultMaxHealth}, clamping to 1", this);
            _defaultMaxHealth = 1f;
        }

        if (_defaultBaseDamage <= 0)
        {
            Debug.LogWarning($"[PlayerStats] _defaultBaseDamage is {_defaultBaseDamage}, clamping to 1", this);
            _defaultBaseDamage = 1;
        }
    }
}
