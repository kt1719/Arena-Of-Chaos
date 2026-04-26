# Arena of Chaos — Game Design Document v1

**Author:** Kyel
**Date:** 2026-04-26
**Status:** Pre-prototype — decisions marked ❓ require playtesting to resolve

---

## Core Concept

Arena of Chaos is a **2D top-down multiplayer PvPvE action game**. Players compete in a shared arena, fighting AI enemies for gear and points while hunting each other for bounties. Short rounds, escalating chaos, and a bounty system that turns the strongest player into the biggest target.

**Emotional Target:** Short-burst competitive showmanship with repeated chances to prove yourself. Players feel excited to show what they can do and want to win the next round.

**Experience Chain:**
```
Mechanics (combat, loot, bounty) → Events (kills, gear steals, bounty hunts)
→ Emotions (thrill, rivalry, comeback hope) → Experience (chaotic competitive mastery)
```

---

## Game Pillars

1. **Expressive Mastery** — Individual skill is visible and legible. The gap between a novice and a skilled player shows in how they move, aim, dodge, and choose engagements.
2. **Escalating Chaos** — Rounds start scrappy and build to explosive, high-power fights. The game gets crazier as it goes.
3. **Risk-Reward Tension** — Every moment presents a choice: play safe for small gains or go aggressive for big payoffs with real consequences.

---

## Core Gameplay Loop

```
MATCH (best of 3-5 rounds, gear resets between rounds)
│
└── ROUND (90-120s, continuous — no phase splits)
    ├── Enemies spawn continuously, escalating over time
    ├── Players farm enemies for gear and points
    ├── Killing players steals gear + awards bounty points
    ├── Bounties rise as players get stronger
    └── Round ends on timer → highest score wins the round
```

### Within a Round

- **Early round:** Players are weak, farming basic enemies. Encounters are scrappy.
- **Mid round:** Harder enemies appear with better drops. Players start hunting each other.
- **Late round:** Strong players have high bounties. Everyone converges on high-value targets. Maximum chaos.

The phase structure (setup → clash) **emerges organically** from the escalation and bounty system rather than being enforced by the game.

---

## Scoring System

| Action | Points | Design Rationale |
|--------|--------|-----------------|
| Kill basic enemy (Slime) | Low | Safe, slow scoring — viable but not optimal |
| Kill harder enemy | Higher (scaled by difficulty) | Rewards PvE skill and risk-taking |
| Kill player | High (base + bounty bonus) | Encourages PvP engagement |
| Kill high-bounty player | Very high | Natural balancing — strongest player becomes biggest target |

**Bounty mechanic:** A player's bounty increases with kills and gear acquired. Higher bounty = more points awarded to whoever kills them. This creates a self-balancing feedback loop: dominance paints a target on your back.

---

## Death and Respawning

- **Respawns enabled** — death is tempo loss, not elimination
- **Respawn delay:** 3-5 seconds ❓ (exact timing needs playtesting)
- **On death:** Player drops their gear pickups. Killer can steal them.
- **No direct point loss** — the cost of death is **opportunity cost** (missed scoring time while respawning, lost gear)

**Design rationale:** Opportunity cost punishes dying without rewarding passivity. A player who hides doesn't lose points but doesn't gain any either. Aggressive play with occasional deaths still outscores passive play.

---

## Weapons

Built on the existing abstract `BaseWeapon` system with `ScriptableObject` definitions.

| Weapon | Role | Behavior |
|--------|------|----------|
| **Sword** (exists) | Melee, close-range burst | Arc-based attack, alternating swipes, slash VFX |
| **Bow** (exists) | Ranged, precision | Networked arrow projectiles, client-side hit prediction |
| **Staff** (planned) | Area/magic ❓ | Not yet implemented — role TBD during prototyping |

- Players start each round with a **basic default weapon**
- Better weapon variants drop from harder enemies (e.g., "Fire Sword" = Sword with +damage, same mechanic)
- Picking up a weapon **swaps** your current one — no inventory management during combat
- The existing 5-slot inventory UI may be repurposed or simplified ❓

---

## Gear and Power-Ups

**Gear drops from enemies. Gear is stolen when a player is killed.**

Two categories, both applied immediately on pickup:

### Weapons
- Sword, Bow, Staff variants
- Better versions from harder enemies
- One weapon held at a time — swap on pickup

### Power-Ups (temporary, last until end of round)
- Damage up, Speed up, Defense up, Dash cooldown reduction
- Number modifiers on existing player stats — no new systems needed
- Stack with diminishing returns to prevent runaway snowballing

**No inventory management. No crafting. No skill trees.** A player's "build" for any moment is: which weapon they're holding + which power-ups they've accumulated.

---

## Enemy Roster

All enemy types are **parameter variations on the existing Slime AI** (A* pathfinding, state machine, health, knockback, death VFX).

| Enemy | Behavior | Drops | Player Decision |
|-------|----------|-------|-----------------|
| **Slime** (exists) | Roam → Chase → Lunge | Common pickup (small heal, minor stat boost) | Easy, low reward. Farm when nothing better is available. |
| **Armored Slime** | Slower, more HP, knockback-resistant | Weapon pickup | Time investment — spend 5s killing this while others grab easy drops? |
| **Swarm Spawner** | Stationary, spawns small slimes until destroyed | Rare power-up | High value but dangerous — swarm can overwhelm, noise attracts players |
| **Elite** (late round only) | Fast, aggressive, high damage, relentless chase | Premium drop (strongest power-up) | High risk, high reward. Fighting this makes you vulnerable to player attacks. |

### Escalation
- **Early round:** Mostly Slimes, occasional Armored Slime
- **Mid round:** Armored Slimes common, Swarm Spawners appear
- **Late round:** Elites spawn, all enemy types present, density increases

---

## Player Mechanics (Existing)

| Mechanic | Status | Notes |
|----------|--------|-------|
| WASD movement | ✅ Working | Server-authoritative with lag compensation |
| Mouse-aimed attacks | ✅ Working | |
| Dash with cooldown | ✅ Working | Trail VFX, cooldown-based |
| Knockback | ✅ Working | On hit, affects both players and enemies |
| Hit flash feedback | ✅ Working | Visual feedback on damage |
| Health (networked) | ⚠️ Partial | Networked but player damage reception is stubbed out |
| Player death/respawn | ❌ Not built | Required for core loop |
| Weapon switching | ⚠️ Partial | Inventory UI exists, switching logic commented out |

---

## Match Structure

1. Players join a room (currently hardcoded "TestRoom")
2. Match begins: best of **3-5 rounds** ❓
3. Each round: 90-120s ❓ continuous play
4. Between rounds: scores shown, gear resets, brief anticipation period
5. Series winner: highest total score across all rounds

---

## Open Questions (Resolve Through Prototyping)

These are decisions that **cannot be resolved through analysis** — they require building and playtesting (per the Planning Horizon principle).

| Question | Options to Test | How to Test |
|----------|----------------|-------------|
| Does the bounty system alone create enough PvP? | Bounty-only vs. bounty + forced objective (King of the Hill) | Play both versions, observe player behavior |
| Should there be a spatial objective? | No objective / King of the Hill / Shrinking arena / Enemy spawn concentration | Add King of the Hill as a mid-round bonus event, toggle on/off |
| How long should rounds be? | 60s / 90s / 120s | Try each, measure when engagement drops |
| How fast should enemies escalate? | Slow ramp / fast ramp / wave-based | Tune and observe pacing feel |
| Respawn delay duration? | 2s / 3s / 5s | Test what feels punishing enough without being frustrating |
| How many rounds per match? | 3 / 5 | Test session length vs. comeback potential |
| Power-up diminishing returns curve? | Linear / logarithmic / hard cap | Balance testing |
| Staff weapon role? | Area denial / DoT / crowd control | Prototype each, see what fills a non-overlapping role |

---

## Environment (Existing)

- Tilemap-based outdoor arena with rule tiles
- Water, paths, canopy layers
- Destructible bushes
- Trees with transparency when player walks behind
- Parallax backgrounds
- Torches with idle animations
- Ambient leaf particles
- Cinemachine camera following local player

---

## Technical Foundation (Existing)

- **Engine:** Unity
- **Networking:** Photon Fusion 2, Host/Client architecture
- **Authority:** Server-authoritative with lag compensation
- **Room:** Currently hardcoded "TestRoom" — lobby/matchmaking is out of scope for v1

---

## Art Direction

- **Style:** Pixel art, 2D top-down
- **Tone:** Chaotic, colorful, readable — visual clarity is critical for competitive play
- **Audio:** Not yet implemented ❓

---

## Out of Scope (v1)

- Lobby system / matchmaking / room management
- Multiple arenas or maps
- Persistent progression between matches (unlocks, ranks)
- Audio system
- Tutorial or onboarding
- Spectator mode
- More than one arena layout

---

## Prototype Priority (Build Order)

Based on the dependency stack principle — prove core gameplay first:

1. **Wire up player damage reception** (existing stub)
2. **Player death and respawn** with delay
3. **Round timer** with start/end flow
4. **Basic scoring system** (points for kills)
5. **Bounty system** (bounty increases with kills, displayed to all players)
6. **Gear drops from enemies** (even placeholder pickups)
7. **Gear steal on player kill**
8. **Enemy escalation** (spawn harder enemies over time)
9. **Multi-round series** with score tracking

Then playtest. Then iterate.
