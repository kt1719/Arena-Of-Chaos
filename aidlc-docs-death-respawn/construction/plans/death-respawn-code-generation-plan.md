# Code Generation Plan — Player Death & Respawn: Disable Visuals and Colliders

## Unit Context
- **Unit**: Single unit — death/respawn visual and collider management
- **Files to Modify**: `Assets/Scripts/Player/PlayerCombat.cs`, `Assets/Scripts/Player/PlayerController.cs`
- **Requirements**: FR-1 through FR-6 from death-respawn-requirements.md

---

## Step 1: Fix PlayerController.ChangePlayerEnable Bug (FR-5)
- [x] In `PlayerController.cs`, change `playerVisuals.gameObject.SetActive(true)` to `playerVisuals.gameObject.SetActive(active)` in `ChangePlayerEnable()`
- [x] Remove unused `using Unity.VisualScripting;` directive

## Step 2: Add HurtBox Collider Reference to PlayerCombat (FR-2)
- [x] Add `[SerializeField] private Collider2D _hurtBoxCollider;` field to PlayerCombat.cs
- [x] This will be wired in the Unity Inspector to the HurtBox child's BoxCollider2D

## Step 3: Add Invincibility Timer to PlayerCombat (FR-4)
- [x] Add `[Networked] private TickTimer InvincibilityTimer { get; set; }` networked property
- [x] Add `[SerializeField] private float _invincibilityDuration = 1.5f;` configurable field
- [x] Add `ApplyHit()` guard: `if (!InvincibilityTimer.ExpiredOrNotRunning(Runner)) return;`

## Step 4: Update Die() to Disable Visuals and HurtBox (FR-1, FR-2)
- [x] In `Die()`, add `if (_hurtBoxCollider != null) _hurtBoxCollider.enabled = false;`
- [x] Visuals handled by existing `DisablePlayer()` → `ChangePlayerEnable(false)` → `SetActive(false)` (now fixed)

## Step 5: Update Respawn() to Re-enable Visuals, Colliders, and Start Invincibility (FR-3, FR-4)
- [x] In `Respawn()`, add `if (_hurtBoxCollider != null) _hurtBoxCollider.enabled = true;`
- [x] In `Respawn()`, add `InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, _invincibilityDuration);`
- [x] Visuals re-enabled via existing `PlayerManager.Instance.EnablePlayer()` call

## Step 6: Update ForceResetDeathState() for Round Reset (FR-6)
- [x] Add `if (_hurtBoxCollider != null) _hurtBoxCollider.enabled = true;`
- [x] Add `InvincibilityTimer = default;` to clear any lingering invincibility

## Step 7: Verify Compilation
- [x] Run diagnostics on both modified files — zero compile errors confirmed
- [x] Also fixed pre-existing `ApplyHit` signature mismatch with `IHittable` interface (added missing `PlayerRef attacker` parameter)
