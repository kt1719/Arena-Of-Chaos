# Skill: Interface (Ch. 9)

## Metaphor
Interfaces communicate through metaphor — visual/audio representations that map to game concepts.
- **Metaphor Sources**: Real-world objects, genre conventions, cultural symbols, spatial relationships
- **Metaphor Vocabulary**: The set of symbols players must learn. Smaller = more accessible. Reuse conventions when possible.

## Signal and Noise
- **Signal**: Information the player needs to make decisions
- **Noise**: Visual/audio clutter that obscures signal
- **Art complexity** adds noise. Beautiful environments can make enemies invisible. Detailed UI can hide critical info.

**Rule**: Every visual element should either convey useful information or create intended emotion. If it does neither, it's noise.

## Visual Hierarchy
Not all information is equally important. Use size, color, contrast, position, and animation to create a hierarchy:
1. Most critical info (health, immediate threats) — largest, most contrasting
2. Important context (minimap, ammo) — visible but not dominant
3. Background info (score, time) — available but unobtrusive

## Redundancy
Communicate important information through multiple channels simultaneously:
- Visual + audio (enemy appears AND makes a sound)
- Color + shape (red AND skull icon = danger)
- Spatial + UI (enemy visible in world AND on minimap)

Redundancy ensures information reaches players even when one channel is missed.

## Indirect Control
Guiding player behavior without explicit instructions:

### Nudging
Environmental design that makes the desired path feel natural. Light draws attention. Wide paths invite exploration. Narrow paths create tension.

### Priming
Exposing players to concepts before they need them. Show a mechanic in a safe context before requiring it in a dangerous one.

### Social Imitation
Players copy what they see others do. NPCs, other players, and even environmental storytelling can model desired behavior.

## Input Design
- **Control Arrangement**: Map frequent actions to comfortable inputs. Related actions on nearby controls.
- **Control Feel**: Responsiveness, weight, feedback. The *feel* of pressing a button matters as much as what it does.
- **Input Assistance**: Aim assist, auto-targeting, input buffering. Invisible help that makes players feel skilled.
- **Control Latency**: Even tiny delays (>100ms) feel sluggish. Responsiveness is non-negotiable for action games.

## Teaching Questions
- "What's the signal-to-noise ratio in your interface? What can be removed?"
- "Can a new player understand your interface in 30 seconds? What's confusing?"
- "Are you communicating critical information through multiple channels?"
- "How are you guiding the player without explicit instructions?"
- "Does your game *feel* good to control, independent of what's happening on screen?"
