# Skill: Multiplayer (Ch. 7)

## Game Theory Basics
Every multiplayer interaction is a strategic game where each player's best choice depends on what others do.

### Nash Equilibrium
A state where no player can improve their outcome by changing strategy alone. Games settle into Nash equilibria over time. If your game has a boring Nash equilibrium, skilled play will be boring.

### Mixed Strategies
When no pure strategy dominates, players randomize. Rock-Paper-Scissors has no pure Nash equilibrium — the optimal play is random 1/3 each. Games with mixed strategy equilibria create ongoing tension and unpredictability.

### Yomi (Reading)
The art of predicting your opponent's choice. Named by David Sirlin. Yomi creates depth in games with mixed strategies:
- Level 0: Play randomly
- Level 1: Predict opponent's likely choice, counter it
- Level 2: Predict that opponent will counter your likely choice, counter *that*
- Level N: Infinite recursion of prediction

**Yomi is why multiplayer games are limitlessly deep** — you can learn any system, but never fully understand another mind.

## Destructive Player Behavior
Players will grief, exploit, and abuse any system they can. Design for the worst-case player:
- Friendly fire abuse, spawn camping, team killing
- Exploiting communication systems for harassment
- Intentional losing to derank and stomp weaker players
- Market manipulation in economic games

**Don't rely on social norms.** Design mechanics that make destructive behavior unprofitable or impossible.

## Divergent Goals
Players in the same game may want different things:
- One wants to win; another wants to socialize
- One wants to explore; another wants to compete
- One wants to role-play; another wants to optimize

Design for this by supporting multiple valid play styles, or clearly communicating what the game is about.

## Skill Differentials
When players of very different skill levels play together:
- The skilled player is bored; the unskilled player is crushed
- **Solutions**: Handicapping, team-based play, asymmetric roles, matchmaking, catch-up mechanics

## Teaching Questions
- "What is the Nash equilibrium of your core interaction? Is it interesting?"
- "Does your game support Yomi — layers of prediction and counter-prediction?"
- "What's the worst thing a malicious player could do? How does your design prevent it?"
- "Can players with different goals coexist in your game?"
- "What happens when a veteran plays against a newcomer?"
