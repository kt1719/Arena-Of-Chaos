# Analysis Framework: Elegance Audit

Use this to evaluate the elegance of a game's mechanics.

## Step 1: List All Mechanics
Enumerate every distinct mechanic in the game or system being evaluated.

## Step 2: Interaction Matrix
For each pair of mechanics, ask: "Do these interact?" Build a mental (or actual) matrix.
- High interaction count = good sign
- Isolated mechanics with few connections = red flag

## Step 3: Apply the 9 Elegance Smells

For each mechanic, rate (Strong / Weak / Absent):

| Smell | Question | Rating |
|-------|----------|--------|
| Many Interactions | How many other mechanics does this interact with? | |
| Simplicity | Can you explain it in one sentence? | |
| Multiple Uses | Can it be used offensively, defensively, creatively, tactically? | |
| Non-Overlapping Roles | Does anything else in the game do the same thing? | |
| Reuses Conventions | Does it leverage knowledge players already have? | |
| Similar Scale | Do the numbers work naturally with other systems? | |
| High Reuse | Will this be used thousands of times? | |
| No Content Restrictions | Does this force future content into narrow constraints? | |
| Full Interface Expressiveness | Does it use the full range of the input device? | |

## Step 4: Identify Dead Weight
Mechanics that score poorly across most smells are candidates for removal or redesign. Ask: "What experiences would be lost if this mechanic were removed?"

## Step 5: Identify Emergence Opportunities
Look for mechanics that *could* interact but don't. These are missed opportunities for emergence. Ask: "What if mechanic A could affect mechanic B?"

## Output Format
Summarize findings as:
- **Elegant mechanics**: [list with reasons]
- **Dead weight**: [list with reasons]
- **Missed emergence**: [list of potential interactions]
- **Content restrictions**: [list of constraints imposed on future content]
- **Recommendations**: Prioritized list of changes
