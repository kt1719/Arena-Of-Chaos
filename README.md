# Arena of Chaos

A 2D top-down multiplayer PvPvE action game built in Unity. Players compete in a shared arena, fighting AI enemies for gear and points while hunting each other for bounties. Short rounds, escalating chaos, and a bounty system that turns the strongest player into the biggest target.

See [`docs/game-design/gdd-v1.md`](docs/game-design/gdd-v1.md) for the full game design document.

## Tech Stack

- **Engine:** Unity `6000.3.6f1` (Unity 6) with Universal Render Pipeline
- **Networking:** Photon Fusion (under `Assets/Photon`)
- **Pathfinding:** A* Pathfinding Project (under `Assets/AstarPathfindingProject`)
- **Input:** Unity Input System (`InputSystem_Actions.inputactions`)
- **Camera:** Cinemachine
- **Asset management:** Addressables
- **Language:** C#

## Getting Started

### Prerequisites

- Unity Hub
- Unity Editor `6000.3.6f1` (install via Unity Hub — see `ProjectSettings/ProjectVersion.txt`)
- Git

### Open the project

1. Clone the repository.
2. In Unity Hub, click **Add** and select the project root (`Arena Of Chaos`).
3. Open with Unity `6000.3.6f1`. First import will populate `Library/` and may take several minutes.
4. Open `Assets/Scenes/GameScene.unity` to play.

### Running multiplayer locally

The project uses Unity's Multiplayer Play Mode package alongside Photon Fusion. You can launch additional virtual players from **Window → Multiplayer Play Mode** to test networked play without separate builds.

## Project Layout

```
Assets/
├── Scenes/                 # GameScene, RoundTimer
├── Scripts/                # Gameplay code (see below)
├── Prefabs/                # Player, enemies, weapons, projectiles
├── ScriptableObjects/      # Weapon and data definitions
├── Animation/              # Animator controllers and clips
├── Sprites/, Tilemap/      # 2D art and tile assets
├── Materials/, Settings/   # URP materials and render settings
├── Resources/              # Runtime-loaded assets
├── AddressableAssetsData/  # Addressables configuration
├── Photon/                 # Photon Fusion SDK + addons
├── AstarPathfindingProject/# A* pathfinding
├── TextMesh Pro/           # TMP assets
└── Test/                   # Test assemblies
```

### Key scripts (`Assets/Scripts/`)

- `GameManager.cs`, `NetworkManager.cs`, `PlayerManager.cs` — core game and session orchestration
- `UIManager.cs`, `UI/` — HUD, inventory, fade transitions
- `Player/` — `PlayerController`, `PlayerMovement`, `PlayerCombat`, `PlayerStats`, `PlayerVisual`, `BaseWeapon`
- `Enemy/` — `EnemyAI`, `EnemyPathfinding`, slime combat/visual/lunge, death VFX
- Weapons — `SwordWeapon`, `BowWeapon`, arrow simulation/visuals/hit detection, sword hitbox/slash
- `CameraFollow.cs`, `GameInput.cs`, `NetworkInputData.cs`, `Knockback.cs`, `IHittable.cs`

## Documentation

- [`docs/game-design/gdd-v1.md`](docs/game-design/gdd-v1.md) — Game Design Document
- `_bmad/`, `.kiro/` — workflow and skill configuration for AI-assisted development

## License

No license has been declared for this repository.
