using System;
using Fusion;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour, IHittable
{

    // ===== Networked Fields =====
    [Networked] private BaseWeapon CurrentWeapon { get; set; }
    [Networked] public NetworkBool IsDead { get; private set; }
    [Networked] private TickTimer RespawnTimer { get; set; }
    [Networked] private TickTimer InvincibilityTimer { get; set; }

    // ===== Serialized Fields =====
    [SerializeField] private WeaponInfo _testWeapon;
    [SerializeField] private NetworkObject _weaponParent;
    [SerializeField] private PlayerVisual _playerVisual;
    [SerializeField] private Knockback _playerKnockback;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private Collider2D _hurtBoxCollider;
    [SerializeField] private float _respawnDelay = 3f;
    [SerializeField] private float _invincibilityDuration = 1.5f;

    // ===== Events =====
    public event Action OnDied;
    public event Action OnRespawned;

    // ===== Change Detection =====
    private ChangeDetector _changeDetector;

    // ===== Private Variables =====
    private PlayerStats _stats;
    private PlayerRef _lastAttacker;

    // ===== Lifecycle =====

    public override void Spawned() {
        _stats = GetComponent<PlayerStats>();

        if (_stats == null)
        {
            Debug.LogError($"[PlayerCombat] PlayerStats component not found on {gameObject.name}. Combat will not function correctly.", this);
            return;
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            _playerKnockback.OnKnockbackEnd += CheckDeath;
        }

        SpawnWeapon(_testWeapon);
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (HasStateAuthority && _playerKnockback != null)
        {
            _playerKnockback.OnKnockbackEnd -= CheckDeath;
        }

        if (!HasInputAuthority) return;

        Runner.Despawn(CurrentWeapon.Object);
    }

    public override void Render() {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsDead):
                    HandleDeathStateChanged();
                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_stats == null || CurrentWeapon == null) return;

        if (HasStateAuthority && IsDead)
        {
            if (RespawnTimer.Expired(Runner))
            {
                Respawn();
            }
            return;
        }

        if (GetInput(out NetworkInputData data))
        {
            // Input
            bool attackPressed = data.buttons.IsSet(NetworkInputData.ATTACK);
            Vector2 weaponAimDirection = data.weaponAimDirection;

            // Action
            UpdatePlayerFacingDirection(weaponAimDirection);
            Attack(attackPressed);
        }
    }

    // ===== Public API =====

    /// <summary>The hurtbox collider used by client-side hit prediction.</summary>
    public Collider2D HurtBox => _hurtBoxCollider;

    /// <summary>
    /// True while the post-respawn invincibility window is active. Mirrors
    /// the gate at <see cref="ApplyHit"/> so client-side predictors can
    /// skip targets that authority would reject.
    /// </summary>
    public bool IsInvincible =>
        Runner != null && !InvincibilityTimer.ExpiredOrNotRunning(Runner);

    public void ApplyHit(int damage, Vector2 hitDirection, float knockbackForce, float knockbackDuration, PlayerRef attacker) {
        if (!HasStateAuthority) return;
        if (IsDead) return;
        if (_playerMovement.IsDashing) return;
        if (!InvincibilityTimer.ExpiredOrNotRunning(Runner)) return;

        _lastAttacker = attacker;
        _stats.CurrentHealth = Mathf.Max(0, _stats.CurrentHealth - damage);

        RPC_TriggerHitFlash();
        _playerKnockback.ApplyKnockback(hitDirection, knockbackForce, knockbackDuration);
    }

    /// <summary>
    /// Called by GameManager on round start to cleanly reset a player who is still dead.
    /// </summary>
    public void ForceResetDeathState() {
        if (!HasStateAuthority) return;

        IsDead = false;
        RespawnTimer = default;
        InvincibilityTimer = default;
    }

    // ===== Death & Respawn =====

    private void CheckDeath() {
        if (_stats.CurrentHealth <= 0)
            Die();
    }

    private void Die() {
        if (!HasStateAuthority || IsDead) return;

        IsDead = true;
        RespawnTimer = TickTimer.CreateFromSeconds(Runner, _respawnDelay);

        ReportDeathToScore();
        PlayerManager.Instance.DisablePlayer(Object.InputAuthority);
    }

    private void Respawn() {
        if (!HasStateAuthority) return;

        IsDead = false;
        _stats.CurrentHealth = _stats.MaxHealth;
        transform.position = GameManager.Instance.GetPlayerSpawnPoint(Object.InputAuthority);
        InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, _invincibilityDuration);

        PlayerManager.Instance.EnablePlayer(Object.InputAuthority);
    }

    // ===== Private =====

    private void SpawnWeapon(WeaponInfo weaponInfo) {
        if (HasStateAuthority) {
            BaseWeapon weaponPrefab = weaponInfo.weaponPrefab;
            CurrentWeapon = Runner.Spawn(weaponPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority, (runner, o) =>
            {
                o.GetComponent<BaseWeapon>().Init(weaponInfo.instantiationOffset, _weaponParent, Object);
            });
        }
    }

    private void Attack(bool attackPressed) {
        if (!attackPressed) return;

        CurrentWeapon.Attack();
    }

    // Reactive — runs on every peer when IsDead flips.
    private void HandleDeathStateChanged() {
        SetHurtBoxEnabled(!IsDead);

        if (IsDead) OnDied?.Invoke();
        else        OnRespawned?.Invoke();
    }

    private void SetHurtBoxEnabled(bool enabled) {
        if (_hurtBoxCollider != null) _hurtBoxCollider.enabled = enabled;
    }

    private void ReportDeathToScore() {
        if (ScoreManager.Instance == null) return;

        ScoreManager.Instance.AddPlayerKill(_lastAttacker);
        ScoreManager.Instance.AddDeath(Object.InputAuthority);
    }

    private void UpdatePlayerFacingDirection(Vector2 weaponAimDirection)
    {
        float offset = CalculateMouseFollowWithOffset(weaponAimDirection);
        float yRotation = weaponAimDirection.x < 0 ? 180f : 0f;
        CurrentWeapon.transform.localRotation = Quaternion.Euler(0, yRotation, offset);
    }

    private float CalculateMouseFollowWithOffset(Vector2 weaponAimDirection)
    {
        return Mathf.Atan2(weaponAimDirection.y, Mathf.Abs(weaponAimDirection.x)) * Mathf.Rad2Deg;
    }

    // ===== RPC =====

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    private void RPC_TriggerHitFlash() {
        _playerVisual.TriggerHitFlash();
    }
}
