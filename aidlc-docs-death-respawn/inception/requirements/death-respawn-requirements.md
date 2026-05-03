# Requirements — Player Death & Respawn: Disable Visuals and Colliders

## Intent Analysis
- **User Request**: Finish implementing player death and respawn with delay. Currently the player stops moving but remains visible and its hitbox is still active. Visuals and colliders should be disabled on death.
- **Request Type**: Enhancement (completing partially implemented feature)
- **Scope**: Single Component (PlayerCombat + PlayerController, minor touches to related scripts)
- **Complexity**: Simple — clear implementation path, well-defined existing patterns

## Functional Requirements

### FR-1: Disable Player Visuals on Death
- When a player dies, the `PlayerVisuals` child GameObject must be deactivated (hiding sprite, animator, shadow, trail)
- The weapon (under `WeaponParent`) must also be hidden on death
- This must replicate across all clients (networked state)

### FR-2: Disable All Colliders on Death
- The main `CapsuleCollider2D` on the root Player object must be disabled on death (already implemented)
- The `HurtBox` child's `BoxCollider2D` (trigger) must also be disabled on death
- Both colliders must be re-enabled on respawn

### FR-3: Re-enable Visuals and Colliders on Respawn
- On respawn (after `RespawnTimer` expires), all visuals and colliders must be re-enabled
- Player is repositioned to spawn point before visuals are shown
- Weapon visibility is restored

### FR-4: Post-Respawn Invincibility
- After respawning, the player has a brief invincibility window (1-2 seconds)
- During invincibility, `ApplyHit()` calls are ignored (same pattern as dash i-frames)
- Invincibility expires automatically after the configured duration

### FR-5: Fix PlayerController.ChangePlayerEnable Bug
- `ChangePlayerEnable(bool active)` currently always sets `playerVisuals.gameObject.SetActive(true)` regardless of the `active` parameter
- Must be fixed to pass the `active` parameter through: `playerVisuals.gameObject.SetActive(active)`

### FR-6: Round Start Reset
- `ForceResetDeathState()` must also re-enable visuals and all colliders
- Ensures clean state when a new round starts while a player is still dead

## Non-Functional Requirements

### NFR-1: Network Consistency
- All visual/collider state changes must be driven by networked state (Fusion `[Networked]` properties)
- Remote clients must see the same death/respawn visuals as the host

### NFR-2: Existing Pattern Conformance
- Use existing Fusion patterns: `[Networked]` properties, `ChangeDetector`, RPCs where appropriate
- Follow the established code style in PlayerCombat.cs, PlayerController.cs

## Technical Notes
- The `PlayerVisuals` GameObject contains: SpriteRenderer, Animator, PlayerVisual script, and children (PlayerTrail, PlayerShadow, WeaponParent)
- WeaponParent is a child of PlayerVisuals, so deactivating PlayerVisuals will also hide the weapon
- The `IsDead` networked property already exists and can drive visual state via `ChangeDetector` in `Render()`
- Invincibility can use a `TickTimer` similar to the existing `RespawnTimer` pattern
