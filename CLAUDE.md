# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Arena of Chaos** — a 2D top-down multiplayer PvPvE action game built in **Unity 6 (6000.3.6f1)** with the **Universal Render Pipeline (URP)**. Networking uses **Photon Fusion 2** (Host/Client, server-authoritative with lag compensation). Pathfinding uses the **A\* Pathfinding Project** (vendored under `Assets/AstarPathfindingProject`). Input uses Unity's new Input System.

The full game design is in `docs/game-design/gdd-v1.md`. Read that first when in doubt about gameplay intent — it includes the prototype build order, items marked ❓ that require playtesting, and which mechanics are working / partial / not-built.

## Building, Running, and Testing

This is a Unity project — there is no command-line build script. All work happens through:

- **Open the project** in Unity Hub at the repo root (the `Assets/`, `Packages/`, `ProjectSettings/` triple).
- **Main scenes:** `Assets/Scenes/GameScene.unity` (primary gameplay scene) and `Assets/Scenes/RoundTimer.unity`. There is also a sandbox scene at `Assets/Test/Test.unity`.
- **Multiplayer testing:** the project uses `com.unity.multiplayer.playmode` so you can run multiple Editor instances. Sessions hardcode the Photon room name `"TestRoom"` (see `NetworkManager.StartGameAsync`). Click **Start Host** in one instance, **Start Client** in others.
- **Tests:** `com.unity.test-framework` is installed but no automated tests have been authored yet — `Assets/Test/` contains manual sandbox prefabs/scripts (`BasicSpawner.cs`, `BallTest.cs`, etc.), not unit tests. Run them via `Window → General → Test Runner` if/when tests are added.

## High-Level Architecture

### Networking model (Photon Fusion 2)

Everything that affects gameplay lives on a `NetworkBehaviour`. The pattern used throughout this codebase:

- **`[Networked]` properties** are the source of truth and only mutated on the **state authority** (`HasStateAuthority` checks gate every write). Clients see synced values.
- **`FixedUpdateNetwork()`** is the simulation tick. Input is pulled via `GetInput(out NetworkInputData data)` and is only available where `HasInputAuthority` or on the host.
- **`Render()`** + `ChangeDetector` is how visuals/events react to networked-state changes (e.g., `PlayerMovement` fires `OnDashStart`/`OnDashEnd` from a `_isDashing` change). Use this instead of polling.
- **RPCs** are used sparingly for one-shot visual effects (e.g., `RPC_TriggerHitFlash` in `PlayerCombat`).
- **Lag compensation:** melee uses `Runner.LagCompensation.OverlapSphere(... HitOptions.SubtickAccuracy)` (see `SwordWeapon.DetectHits`).

`NetworkInputData` (in `NetworkInputData.cs`) is the entire per-tick input payload: `movementDirection`, `weaponAimDirection`, and a `NetworkButtons` bitmask with `DASH=0` / `ATTACK=1`. To add a new button, add a `const byte` and set/read it via `buttons.Set/IsSet`.

### Singleton "managers" wired in the scene

These are NOT auto-spawned — they live as objects in the scene and use `public static Instance` references. Anything that calls them assumes they exist:

- **`NetworkManager`** (MonoBehaviour, `INetworkRunnerCallbacks`) — owns the `NetworkRunner`, the player prefab, the host/client buttons, the `PlayerRef → NetworkObject` map (`SpawnedCharacters`), and pumps `OnInput`. Spawns the player prefab on `OnPlayerJoined` (server only).
- **`GameManager`** (NetworkBehaviour) — round/match flow: `RoundTimer`, `RoundStarted`, `CurrentRound`, `MaxRounds`, `IsGameOver`. Owns `defaultPlayerSpawnPoints` and the spawn-index allocation map. Drives the menu/player camera swap on `RoundStarted` change. Match logic (start round → spawn/reset/enable players → wait for timer → end round → check game-over) lives in `StartRound`/`EndRound`.
- **`PlayerManager`** (NetworkBehaviour) — thin wrapper around per-player enable/disable. Calls `PlayerController.ChangePlayerEnable(bool)` for a given `PlayerRef`. Used by `GameManager` (round transitions) and `PlayerCombat` (death/respawn).
- **`GameInput`** (MonoBehaviour) — wraps the generated `PlayerInputActions`, exposes events (`OnPlayerAttack`, `OnPlayerDash`, `OnPlayerInventory`) and polled values (`GetMovementInput`, `GetWeaponAimDirection`). Local-only — networking is not its concern.
- **`CameraFollow`** — Cinemachine target swap; `NetworkManager.RegisterLocalPlayer` points it at the local player on spawn.

### Player composition

The player prefab is a bag of `NetworkBehaviour` siblings, each with one job. They communicate via `GetComponent<>` lookups (cached in `Spawned()`) and C# events:

- **`PlayerController`** — input authority entry point. Holds `[Networked] PlayerEnabled`, gates input consumption on it, registers/unregisters with `NetworkManager`, subscribes to `GameInput` for press-events. `ConsumeInput()` builds the per-tick `PlayerControllerLocalInputData` snapshot the runner reads.
- **`PlayerMovement`** — reads input, drives `Rigidbody2D.linearVelocity`, owns dash state (`_isDashing`/`_dashCurrentDuration`/`_dashCurrentCooldown`). Yields to `Knockback.IsKnockedBack`.
- **`PlayerCombat`** (`IHittable`) — owns `CurrentWeapon` (a `BaseWeapon` networked reference, spawned via `Runner.Spawn` on the host), `IsDead`, `RespawnTimer`. Implements damage reception (`ApplyHit`), death/respawn flow, and aim-derived weapon orientation. Death disables the collider and the player via `PlayerManager`.
- **`PlayerStats`** — networked floats/ints for movement (`MoveSpeed`, `DashSpeedMultiplier`, `DashTotalDuration`, `DashTotalCooldown`), combat (`MaxHealth`, `CurrentHealth`, `BaseDamage`). Inspector defaults seed these in `Spawned()`. Health change fires `OnHealthChanged(current, max)` via `ChangeDetector`.
- **`PlayerVisual`**, **`PlayerTrail`** — purely cosmetic; subscribe to events from `PlayerCombat`/`PlayerMovement`.
- **`Knockback`** — shared component used by both players and enemies. Drives `Rigidbody2D.linearVelocity` for `_knockbackTimer` seconds, fires `OnKnockbackEnd`. Other systems must check `IsKnockedBack` and skip movement while it is true.

### Weapons

Abstract base + ScriptableObject definition:

- **`BaseWeapon : NetworkBehaviour`** — abstract. Owns `weaponState`, `weaponParent`, `playerCombat` (a back-reference NetworkObject), `weaponInstantiationOffset`, `weaponCooldown`. Public `Attack()` enforces cooldown then calls subclass `AttackAction()`. `Init(...)` is called inside the `Runner.Spawn` callback **before** `Spawned()` to stamp parenting/offset/owner.
- **`WeaponInfo` (ScriptableObject)** — `weaponPrefab`, cooldown, damage, range, knockback force/duration, instantiation offset. Definitions live under `Assets/ScriptableObjects/Weapons/`.
- **`SwordWeapon`** — melee. Uses `Runner.LagCompensation.OverlapSphere` + `SwordHitbox.IsInsideArc` to filter to a semi-circle arc. Dedupes hits per-attack with `_hitCache`. Alternates `SwordSwipe.UP`/`DOWN` so visuals can pick the right animation.
- **`BowWeapon` + `ArrowSimulation` + `ArrowVisualManager` + `ArrowHitDetection` + `ArrowData`** — non-trivial. Arrows live in a **`NetworkArray<ArrowData>` ring buffer** with `BUFFER_CAPACITY = 32` indexed by `_fireCount % capacity`. The buffer is the only network state — there are no per-arrow `NetworkObject`s. State authority ticks the simulation in `FixedUpdateNetwork`; every client (including the shooter) reconstructs visuals from the buffer in `Render`. The shooter additionally runs **client-side hit prediction** via `ArrowVisualManager`. Trail emission is delayed `TRAIL_START_DELAY = 0.1f` to avoid resimulation snap artifacts. **If you change the buffer capacity, update both `BowWeapon` and the size passed into `ArrowVisualManager.Init`.**

To damage anything, call `IHittable.ApplyHit(damage, hitDirection, knockbackForce, knockbackDuration)`. Both `PlayerCombat` and the enemy combat scripts implement it.

### Enemies

Currently one type — Slime — composed of `EnemyAI` (state machine: Roaming / Chasing / Lunging / KnockedBack), `EnemyPathfinding` (A\* wrapper), `SlimeLunge`, `SlimeCombat`, `SlimeVisual`, `EnemyDeathVFX`, plus a shared `Knockback`. Roam targets are validated against `AstarPath.active` so slimes don't try to walk into unwalkable tiles. Per the GDD, all future enemy types are intended to be **parameter variations on this same AI**, not new state machines.

### Scene/environment

Tilemap-based outdoor arena (`Assets/Tilemap/`). `TransparentDetection` fades trees when the player walks behind them. `Destructible` + `EnvironmentInteractible` handle bushes the sword can break. `Parallax` + `RandomIdleAnimation` are ambient.

## Conventions seen across the codebase

- Files use **region comments** (`// ===== Networked Fields =====`, `// ===== Serialized Fields =====`, `// ===== Lifecycle =====`, etc.) to group members. Match this when editing — don't dump new fields at the bottom.
- Authority guards are explicit: `if (!HasStateAuthority) return;` at the top of any mutation. Replicate this pattern; don't trust state on a non-authority.
- Cross-component wiring: `[SerializeField]` references for siblings on the same prefab (e.g. `_playerKnockback`, `_playerMovement` on `PlayerCombat`). `GetComponent<>` is used in `Spawned()` only when wiring is implicit. Both styles coexist — prefer explicit serialized references for clarity.
- C# events (`event Action<...>`) are the standard cross-component signal. `ChangeDetector` is the standard signal *from* networked state *to* render-side reactions.
- `Debug.Log`/`Debug.LogWarning` messages are tagged with the source class in brackets, e.g. `"[GameManager] Round X ended."`.

## Out-of-scope tooling in the repo

The repo includes meta-tooling directories that are **not** part of the Unity project: `_bmad/` (BMAD method config + scripts), `.kiro/` (Kiro IDE specs/steering — `.kiro/specs/` is gitignored), and `docs/`. Don't move design docs into `Assets/` or Unity will try to import them. Personal BMAD config (`_bmad/custom/*.user.toml`, `_bmad-output/`) is gitignored.

## Things that are partial or stubbed (per GDD)

When working on combat or round flow, be aware that several systems are wired but incomplete:

- Player damage reception path exists (`PlayerCombat.ApplyHit` + networked `CurrentHealth`) but the full death-drops-gear / scoring / bounty pipeline from the GDD is not built.
- Weapon switching: the 5-slot inventory UI (`ActiveInventory`, `InventorySlot`) and `GameInput.OnPlayerInventory` exist, but the swap logic is commented out.
- The Staff weapon listed in the GDD does not yet have an implementation.

The intended prototype build order is in `docs/game-design/gdd-v1.md` under **"Prototype Priority"** — follow that order when adding core-loop features unless the user explicitly says otherwise.
