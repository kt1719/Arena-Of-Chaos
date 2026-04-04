---
inclusion: always
---

# Verification

- Scale verification to task size. Small tasks get lightweight checks; medium or larger tasks need defined success criteria.
- Prefer actual validation over guesswork: compile checks, MCP inspection, scene inspection, prefab validation, runtime reasoning, playtest guidance, or other appropriate checks.
- Do not claim code works unless it has been validated through available means.
- If verification fails, iterate and fix.

# Multiplayer Awareness

- When networking is involved, assume PUN2 or Fusion unless specified otherwise.
- During planning and review, call out: authority, ownership, synchronization, prediction, RPC/event flow, and scene/object lifecycle concerns.
