Read `docs/game-design/gdd-v1.md` — this is the Game Design Document for Arena of Chaos.

Focus on the **"Prototype Priority (Build Order)"** section. Each numbered item is a task that needs to be created on my Trello board.

**Trello Board:** https://trello.com/b/qdVuE2Al/programming-tasks

Use the Trello REST API to create cards. You'll need to:

1. First, list the board's lists to find the right list ID (use the "To Do" or "Backlog" list — ask me if unclear)
2. Create one card per prototype priority item, in order
3. For each card:
   - **Title**: The task name from the build order (e.g., "Wire up player damage reception")
   - **Description**: Pull relevant context from the GDD — what exists already, what needs to be built, and any design constraints. Include the relevant GDD sections as reference.
   - **Labels**: If the board has labels, ask me which to apply
   - **Position**: Maintain the priority order (item 1 at top)

The 9 prototype tasks from the GDD are:
1. Wire up player damage reception (existing stub)
2. Player death and respawn with delay
3. Round timer with start/end flow
4. Basic scoring system (points for kills)
5. Bounty system (bounty increases with kills, displayed to all players)
6. Gear drops from enemies (even placeholder pickups)
7. Gear steal on player kill
8. Enemy escalation (spawn harder enemies over time)
9. Multi-round series with score tracking

**API credentials**: I'll provide my Trello API key and token when you ask. Do NOT hardcode them.

Start by reading the GDD, then ask me for my API credentials and which list to target.
