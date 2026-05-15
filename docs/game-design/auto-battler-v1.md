# Arena of Chaos — Auto-Battler Design v1

**Author:** Claude (game-design-instructor session)
**Date:** 2026-05-15
**Status:** Pre-prototype design specification — numbers are first-pass and require playtesting to tune
**Companion to:** `gdd-v1.md` (the real-time PvPvE concept that shares the project name)

---

## 0. Preamble — What This Document Is, and Isn't

This doc continues the design of a **3v3 asynchronous auto-battler** inspired by the Earclacks physics-ball videos. It builds on prior decisions (concept, three-layer mechanic, 5-round match structure, intra-match complexity ramp, TFT-style economy) and specifies the eight subsystems that still need design work.

It is written through the lens of Tynan Sylvester's *Designing Games: A Guide to Engineering Experiences*. Every major recommendation is justified by a **named principle** from that book — not vague enthusiasm. Where a decision genuinely depends on playtesting, that is flagged explicitly with ❓ rather than papered over.

### Pre-existing context (re-stated, not re-litigated)

- **Format:** 3v3 auto-battler, asynchronous PvP, skill-matched by win count, goal = 10 match wins.
- **Three mechanic layers:** Movement (Kiter / Charger / Hunter), Weapon (Sword / Bow / Axe), Modifier (Detonate / Split, stackable). More may be added later.
- **Match structure:** Bo5. Each round = prep (~25s) → battle (~18s, automated). 3-5 min total.
- **Intra-match complexity ramp:** R1 = movement + weapon; R2 = +1 modifier slot; R3 = +2nd modifier slot; R4 = upgrade tier; R5 = full power.
- **Emotional targets:** Prep = puzzle-solving / wit / problem-solving under constraints. Battle = anticipation / suspense / satisfying VFX-driven payoff.
- **Economy direction:** Single resource, TFT-loose, with comeback bonus for losers.

### The lens, restated bluntly

A game is an **engine of experience**. The chain is `Mechanics → Events → Emotions → Experience`. Every system below is justified by what *event* it generates, what *emotion* that event triggers, and how those emotions assemble into the two target experiences (prep puzzle and battle anticipation). When a system doesn't pull its weight on that chain, it is dead complexity.

---

## 1. Economy Specification

> **⚠ Revision note (2026-05-15):** This section has been superseded by a focused exploration in **`auto-battler-economy-exploration.md`**, which surveys 13+ economy approaches across two rounds of designer dialogue and lands on **three viable options (X, Y, Z)** with explicit pros/cons. The numbers in §1.3-§1.7 below describe the **retired Model A** (bank-and-interest) and are preserved for historical reference while the team picks one of X/Y/Z. The exploration doc is the authoritative source on the trade-offs; this section will be rewritten once the team commits to a prototype direction.

### 1.1 Goals (in priority order)

1. **Generate save-vs-spend tension every round.** Per Sylvester's "feeling the future" principle in Ch. 5, an interesting decision requires the player to be able to *prefeel* the consequences of saving vs. spending. The economy is the engine of those decisions.
2. **Enable comebacks without destroying snowballs.** Per Ch. 6 (Balance) and Ch. 7 (Multiplayer), a winner-take-all snowball produces a degenerate strategy (open strong, never lose); a too-aggressive comeback mechanic produces the opposite degenerate (intentionally lose round 1 to farm). The economy threads this needle.
3. **Make rarity tier feel valuable across the 5 rounds.** Per Ch. 8 (Motivation), the dopamine peak of acquisition has to be aligned with rare-tier unlocks. The economy must price rare options high enough that affording them feels like a win.
4. **Resolve in one resource.** Per the elegance smell of *simplicity*, dual currency in a 3-5 minute match crosses the FTUE complexity budget.

### 1.2 Alternatives considered — see exploration doc

Initial draft of this section picked a single Balatro-lineage answer (Model A, bank + interest) without rigorously surveying the design space. A focused two-round designer dialogue surveyed 13 candidate economy models, cut 8 for structural reasons, identified 3 viable options, and explicitly *refused to pick one* — because the choice between them depends on the team's risk profile and production budget, not on a correctness argument.

**See `auto-battler-economy-exploration.md` for the full survey, sample R3 prep walkthroughs for each option, elegance audits, FTUE costs, and decision criteria.**

**The three viable options that doc lands on:**

| Option | One-line shape | Strongest argument | Strongest objection |
|--------|---------------|--------------------|---------------------|
| **X. B + S** (Snap-shape clean) | Charge expires per round; next-round shop preview visible | Lowest FTUE, cheapest prototype, Snap-tested per-round-puzzle pattern | Lowest differentiation from existing autobattlers |
| **Y. H + R** (Action-points) | AP per round (with synergy refunds) + per-ball modifier inventory grid | "The unequipped promise" — unique emotional event no autobattler currently produces | Higher UI cost; AP rigidity could feel restrictive |
| **Z. B + S + proc-order** (Snap-shape with proc-puzzle) | Option X + modifier proc-order on each ball is part of the puzzle | Adds genuine puzzle depth at minimal FTUE cost | Risks being a "depth-only" mechanic that new players ignore |

**Model A (bank + interest, originally specified in §1.3-§1.7 below) was retired.** The runtime argument: interest cannot meaningfully compound in 5 rounds; the decision degenerates to "save in R1 or don't." It is not a wrong mechanic — it's wrong *for this runtime*. If the design later expands to Bo7 or longer, Model A becomes viable.

**The exploration doc's lean** (with the caveat that this is a design-stance call, not a correctness call):
1. Prototype **Option X** first (cheapest signal).
2. If X feels flat, escalate to **Option Z**.
3. Treat **Option Y** as the differentiated alternative if market positioning demands an autobattler-first economy.

The sections below (§1.3-§1.7) preserve **Model A's original numerical spec** for historical reference. They are not the current recommendation. Once the team commits to X / Y / Z, this section will be rewritten with the chosen option's tuned numbers.

### 1.3 Earning structure

| Source | Amount | Notes |
|--------|--------|-------|
| Base income per round | **5 Charge** | Paid at the *start* of every round prep, including R1 (alongside the 6 Charge starting stipend). |
| Round-win bonus | **+2 Charge** | Paid on round resolution. |
| Round-loss bonus | **+3 Charge** | Paid on round resolution. **Loser bonus is deliberately *larger* than winner bonus.** See §1.5. |
| Win streak | **+1 / +2 / +3 Charge** | At 2 / 3 / 4+ consecutive wins. |
| Loss streak | **+1 / +2 / +3 Charge** | At 2 / 3 / 4+ consecutive losses. |
| Interest | **+1 Charge per 5 saved**, cap +3 | Capped at 15 Charge banked (= +3 interest). |

**Starting Charge:** 6 (paid once at match start before R1 base income).

### 1.4 Costs (first-pass tuning)

| Action | Cost | Available |
|--------|------|-----------|
| Reroll shop | **1 Charge** | All rounds |
| Lock one shop slot | Free | All rounds; one slot only, persists 1 round |
| Buy ball (Common) | 3 | R1+ |
| Buy ball (Rare) | 5 | R1+ |
| Buy ball (Epic) | 8 | R3+ |
| Apply modifier (Common) | 2 | R2+ |
| Apply modifier (Rare) | 4 | R2+ |
| Apply modifier (Epic) | 7 | R3+ |
| Upgrade modifier (one tier) | 3 | R4+ |
| Premium upgrade (max tier) | 5 | R5 |
| Swap weapon on an owned ball | 2 | All rounds (R1+) |
| Sell ball | Refunds 50% of buy price (rounded down) | All rounds |

### 1.5 Why losers get more (the comeback math, and where it stops being a comeback)

Sylvester is explicit that *intentional imbalance can serve design goals* (Ch. 6). Here the imbalance is the loser bonus, and it serves two goals:

- **Sustain motivation through losses** so a player who drops R1 still has stakes in R2-R5 (Ch. 8, reward alignment — the player must believe their next decision can shift the outcome).
- **Prevent the "open-strong always wins" degenerate** that a flat economy would create. If R1-winner stayed +X Charge ahead permanently, the optimal play would collapse to "spike R1." Bigger loser bonus *softens* the snowball; smaller would not enough.

But: **the loss bonus must not exceed the win bonus + per-round shift in board state.** If losing pays more than winning *net of round outcomes*, "lose round 1 on purpose" becomes a degenerate strategy. The math:

> Winner net: +2 Charge + closer to match victory (1 of 3 needed)
> Loser net: +3 Charge + no progress toward match victory

The winner is closer to the *terminal* goal (3 round wins = match), which is a far stronger reward than +1 Charge. So winning still dominates. We are buying a few percentage points of comeback potential at the cost of a strict +1 Charge swing — this is the right trade.

❓ **Playtest target:** Open-rates of round-1 win after round-1 loss should land in **35-45%**. Below 35%, losers can't recover; above 45%, the snowball is too weak.

### 1.6 Sample Charge trajectories

To verify the numbers create the intended tension, walk three sample players through 5 rounds:

**Player A (1-0-0-1-W on rounds R1-R5):**
- Start: 6.
- R1 prep: 6 + 5 = 11. Spend 3 (a Common ball). End round: 8. Win → +2. Charge before R2: 10.
- R2 prep: 10 + 5 = 15. Bank 15 → next round interest +3. Spend 2 (Common modifier). End round: 13 banked, +1 interest tier crossed. Lose → +3. Charge before R3: 16, interest +3 next round.
- R3 prep: 16 + 5 = 21 + 3 interest = 24. Spend 8 (Epic ball). End: 16. Lose → +3. Charge before R4: 19, interest +3.
- R4 prep: 19 + 5 + 3 = 27. Spend 7 (Epic mod) + 3 (upgrade) = 10. End: 17. Win → +2 + streak n/a. Charge before R5: 19, interest +3.
- R5 prep: 19 + 5 + 3 = 27. Full-power blowout — buys 2 premiums.

**Player B (loses every round):**
- Round-loss bonus + loss streak compounds. By R3 they're earning +3 base + +2 loss streak + +3 loss bonus = +8/round, plus interest if they save. By R5 they have a stronger economic position than Player A had at R4. They have a real shot.

**Player C (wins every round):**
- Streaks compound. By R3 they're earning +5/round + +3 win bonus + interest. They can spike harder but the match is already 3-0 → match over.

The math creates the puzzle: **save now to earn interest, or spend now to win the next round and chase streak bonus?** The win streak only matters if you're already winning, which favors momentum; the loss bonus + interest creates the comeback path. Both are visible, both are predictable, the decision is real.

### 1.7 Elegance audit on the economy

| Smell | Rating | Note |
|-------|--------|------|
| Many interactions | Strong | Charge interacts with shop, modifiers, upgrades, rerolls, streaks, interest — six systems. |
| Simplicity | Strong | One currency, two income types (base + bonuses), three cost categories (buy / modify / reroll). Fits on a napkin. |
| Multiple uses | Strong | Same coin buys ball, modifier, reroll, lock — player chooses. |
| Non-overlapping roles | Strong | No second currency; no XP-vs-gold split. |
| Reuses conventions | Strong | TFT lineage is explicit; players from autochess genre recognize it. |
| Similar scale | Strong | Costs (1-8) and incomes (3-8) interact naturally; interest tier at 5 is divisible. |
| High reuse | Strong | Used 5x per match minimum, every round. |
| No content restrictions | Strong | Adding new costed items doesn't require economic redesign. |
| Full input expressiveness | n/a | Economy doesn't use input directly. |

The economy is elegant. Now we test it.

---

## 2. Shop and Acquisition Mechanics

### 2.1 The decision: random shop with locks and rerolls, scaled by round

Three credible options were considered:

| Option | Source | Pro | Con |
|--------|--------|-----|-----|
| Pure random shop (TFT, SAP) | Proven autobattler standard | Variable-ratio reinforcement = highest engagement schedule (Ch. 8). Players already understand it. | Pure RNG can frustrate — must be tempered with locks/rerolls. |
| Drafting (Hearthstone Arena) | Strong agency | Player picks from a slate — feels skill-expressive. | Dilutes economy decisions; reduces value of save-vs-spend tension. |
| Spatial puzzle (Backpack Battles) | Differentiation | Highly novel; deep emergence. | Wrong fiction-mechanic for a *ball* combat sandbox — balls don't fit in a backpack. |

**Recommendation: random shop with locks and rerolls.** Specifically the TFT/SAP model, tuned for the 5-round length.

Justification by named principle:
- **Variable-ratio reinforcement (Ch. 8):** A random shop is the most motivating reward schedule. Every reroll click *might* surface the Epic you need.
- **Information balance (Ch. 5):** Randomized supply hides one piece of info; what the *opponent* is doing hides another. This is exactly Sylvester's poker example — partial information creates the strongest decisions.
- **Reuses conventions:** TFT players and SAP players need zero teaching.
- **Locks and rerolls give the player agency over RNG.** This addresses Sylvester's caution against information-starvation: the player isn't helpless to RNG, they have tools to bend it.

### 2.2 Shop layout per round

Three slots, two action buttons:

```
┌─────────┬─────────┬─────────┐
│ Slot 1  │ Slot 2  │ Slot 3  │
│ [item]  │ [item]  │ [item]  │
│  [Lock] │ [Lock]  │  [Lock] │
└─────────┴─────────┴─────────┘
[Reroll (1 Charge)]    [Lock used: 0/1]
```

- **3 slots** is the sweet spot per the elegance smell of *similar scale*: aligns with 3 balls per team, 3 rounds-to-win-match. Number consistency aids learnability.
- **One lock per round** is a deliberate constraint. Two locks would dilute the *save-vs-reroll* tension. One lock makes the lock decision precious.

### 2.3 Shop content composition per round

The shop content shifts by round to match the intra-match complexity ramp. The shape is roughly:

| Round | Slot 1 | Slot 2 | Slot 3 | Rarity weight |
|-------|--------|--------|--------|---------------|
| R1 | Ball | Ball | Ball | 80% C / 20% R |
| R2 | Ball | Modifier | Modifier | 60% C / 35% R / 5% E |
| R3 | Ball | Modifier | Modifier | 45% C / 40% R / 15% E |
| R4 | Ball | Modifier | Upgrade | 25% C / 50% R / 25% E |
| R5 | Ball | Premium Mod | Premium Upgrade | 10% C / 50% R / 40% E |

Notes:
- A "Ball" slot may show a *weapon* alone (to swap into an owned ball) ~20% of the time after R1. This handles the "I love my Charger but want to switch its weapon" desire without bloating the shop UI.
- Rarity weight is the probability distribution within whatever category the slot is showing. So the slot 1 ball in R1 is 80% likely to be a Common ball, 20% likely Rare.
- Modifier upgrades only appear once a player owns ≥1 modifier of that type. (Per Sylvester's *content restriction* check: this means upgrades create no shop bloat if the player hasn't pursued modifiers.)

### 2.4 Why this shape — the puzzle escalation curve

Sylvester's "decision variation" principle (Ch. 5) is that repeating the same decision gets stale. Across 5 rounds:

- **R1 — Asset acquisition.** The decision space is "which three balls form my team?" Pure roster decision.
- **R2 — Modifier introduction.** Decision space *adds* "what enhancement layer fits my balls?" — qualitatively new.
- **R3 — Synergy decision.** Two modifier slots means the player now *plans* a synergistic combo, not just one-offs.
- **R4 — Power consolidation.** Upgrades reward focus; the player picks what to deepen.
- **R5 — Detonation round.** Premiums create big spikes; the comeback player might find their game-winning Epic; the leader closes out.

This is **decision variation by structure** — the *type* of decision changes each round, even though the shop interaction stays familiar. That is elegance: same mechanic, different events.

### 2.5 Reroll economics and the lock-reroll-walkaway triangle

Reroll at 1 Charge is intentionally cheap. Comparison:

- **TFT reroll: 2g**, on a base income of 5g/round. Ratio: 40% of base income.
- **Our reroll: 1 Charge**, on a base of 5/round. Ratio: 20% of base income.

We can afford the cheaper reroll because matches are shorter — fewer total rerolls means the *per-reroll* economic weight should be lower. A player should be able to reroll 3-5 times in a tight R3 hunt for an Epic without going broke.

The **lock-reroll-walkaway triangle** is the moment-to-moment shop puzzle:
- **Lock** a good option to carry forward → costs nothing, ties you to current state
- **Reroll** to try for better → costs 1, gambles current options
- **Walk away** (buy nothing) → keeps Charge for interest tier or next round

These three actions are the elegance of the shop loop. They interact with the economy, the round timer, and the opponent's likely state. Each has a clear use case but no single one dominates — no degenerate strategy.

### 2.6 Acquisition scaling across the 10-win run

A new player at win 0 should not see the same content density as a veteran at win 9. This is **priming** (Ch. 9) — concepts must appear in safe contexts before requiring them in dangerous ones.

| Win count | Balls available | Modifiers available | Notes |
|-----------|-----------------|---------------------|-------|
| 0-2 | 4 (the core set) | 4 (Detonate, Split, +2 Common) | "Training wheels" content. |
| 3-5 | 6 | 8 | First Epic modifier unlocks at win 3. |
| 6-9 | 8 (full roster) | 12 | Exotic balls and weapon variants appear. |
| 10 (victory) | All | All + cosmetic crown | Prestige cosmetic; replay unlocks Hard Mode. |

❓ **Playtest target:** New player should not feel "I haven't seen this option before" panic *during* a match. All new content should be introduced in the *between-matches* progression screen with a brief preview, then appear in the shop at win N + 0.

This bounds the **skill barrier** (Ch. 3) at the start of the run. By win 3, the player has the cognitive bandwidth to handle Epic modifiers because they've internalized the Common ones.

### 2.7 Open questions for shop tuning

- ❓ Should "lock" persist if the player buys a *different* slot? Default: yes (you locked it; you keep it). Playtest: this might enable a degenerate "lock the Epic forever" exploit.
- ❓ Should the player see *opponent's* shop history (post-hoc) on the post-match screen? It would deepen counterplay learning but might inhibit experimentation. Lean: yes, in the recap, not live.
- ❓ Should there be a "freeze entire shop" option at higher cost (3 Charge)? Adds depth but may overlap with lock. Lean: no — keep shop interactions to lock + reroll for elegance.

---

## 3. Archetype Identity Specification

### 3.1 The question, restated honestly

When a player builds a ball, are Movement and Weapon **bundled** into a named preset (Kiter+Bow = "Sniper"), or **modular** (any movement freely paired with any weapon)?

Both positions have rigorous defences (this was the explicit debate the design called for). The bundled case wins on: fiction-layer identity, balance tractability, decision-scope manageability in a 25-second prep timer, community vocabulary / streamer lexicon / cosmetic monetization, and the canonical evidence of every successful auto-battler shipping discrete units. The modular case wins on: prep-phase depth being the *only* skill range the genre can sell (no manual skill in auto-battle), combinatorial content scaling (one new Movement = N new builds, not N new authored archetypes), alignment with the GDD's existing round-by-round modifier unlock structure (which already implies modular logic), and emergence as Sylvester's first design priority.

### 3.2 The synthesis: bundled identity layer, modular expression layer

Layer the design so that **each layer carries the design weight it's best at**:

- **Movement is bundled.** Each archetype has a fixed Movement type. Kiter, Charger, Hunter are *characters*. The player cannot mix-and-match movement, because movement is the strongest identity axis (it dictates the visible behaviour of the ball in battle — the player *sees* the Kiter dancing away, the Charger barrelling in).
- **Weapon is partially bundled.** Each archetype has a *default weapon* that defines its launch fiction (the Sniper draws her bow; the Berserker hefts his axe). But the player can **swap the weapon on any owned ball for 2 Charge**. This swap is rare in practice (it costs Charge that competes with modifiers) but possible — preserving the "I built a Bow Charger" emergent build space without dumping it on every new player.
- **Modifiers are fully modular.** This is the prep-phase puzzle layer. Round 2 onward, every modifier choice is the player's authorship; combinations are not pre-named; degenerate stacks are prevented by diminishing returns, not by bans.

This synthesis is justified by three named Sylvester principles:

1. **Fiction layer reinforcement (Ch. 1).** The fiction layer adds emotional meaning *on top of* mechanics — but only when the layers reinforce each other. A *named* Movement archetype with a distinctive visual and animation pulls its weight on the fiction layer. A nameless Movement+Weapon configuration string does not. Bundle the layer that carries identity; leave the layer that carries puzzle-authorship modular.
2. **Elegance smell #4 (non-overlapping roles).** Nine archetypes (3 Movement × 3 default Weapon) cover the role space tightly. Twelve named archetypes would start overlapping ("the Sniper" and "the Marksman" overlap mechanically); four would leave the role space sparse. Nine is the right scale — coincidentally matching `3 balls × 3 mechanics-per-ball` for elegance smell #6 (similar scale).
3. **Decision scope (Ch. 5).** With nine archetype "characters" + weapon swaps + modifier authorship, the prep-phase decision tree is rich without being incoherent. A pure modular tree (3 × 3 × C(N modifiers)) overwhelms the 25-second prep timer; a pure bundled tree (12 names + counter-pick) tops out at win 4. The hybrid threads the needle.

### 3.3 The nine starter archetypes

Naming follows the bundled subagent's strong proposal, refined for non-overlapping roles. Each gets: Name, Movement, Default Weapon, signature *role-feel*, and one-sentence intended playstyle.

| # | Name | Movement | Default Weapon | Role-feel | Playstyle one-liner |
|---|------|----------|----------------|-----------|---------------------|
| 1 | **Sniper** | Kiter | Bow | The patient marksman | Maintain max distance, deal high single-target shots, dead at close range. |
| 2 | **Reaver** | Charger | Axe | The avalanche | Closes fast, cleaves clustered enemies, eats incoming damage. |
| 3 | **Duelist** | Hunter | Sword | The single-minded stalker | Picks the weakest enemy and kills it; falls off vs groups. |
| 4 | **Ranger** | Hunter | Bow | The opportunist | Pursues the weakest, but at range — picks off attempted retreats. |
| 5 | **Warden** | Charger | Sword | The wall on the move | Walks into the enemy frontline, soak + stagger. |
| 6 | **Skirmisher** | Kiter | Sword | The dasher | Hit-and-run melee; dances in, slashes, dances out. |
| 7 | **Reaper** | Kiter | Axe | The zoner | Lures and lands the heavy slam from arms-length. |
| 8 | **Bloodhound** | Hunter | Axe | The relentless chaser | Cleave-pursues isolated targets; punishes split positioning. |
| 9 | **Vanguard** | Charger | Bow | The unconventional rusher | Charges into bow range; trades a Charger's defensive value for the bow's pressure. *Intentionally awkward* — the contrarian's pick. |

**Why nine and not eight:** Eight is symmetric and conventional; nine fills the 3×3 grid completely so every Movement-Weapon pair has a launch identity. The "weakest" archetype (Vanguard) is *intentionally* the awkward pair — its existence is the design's promise that *every* Movement-Weapon combination is at least viable, which is what keeps the weapon-swap mechanic meaningful. A player who swaps a Reaver's axe to a bow has *chosen Vanguard*. That choice is now legible.

❓ **Playtest target:** All 9 archetypes should have a top-tier matchup against at least one other archetype. None should sit below 40% win-rate in any specific composition matchup at the top of the meta. (Component-level balance per the modular subagent's recommendation.)

### 3.4 How identity is communicated

Per Ch. 9 (Interface) — communicate identity through **multiple redundant channels**:
- **Silhouette.** Each archetype has a distinct ball silhouette (Kiter = sleek, Charger = chunky, Hunter = sharp), with weapon-mount geometry on top.
- **Colour ramp.** Movement type drives the primary colour (Kiter = cool blue, Charger = hot red, Hunter = predator purple). Weapon drives a secondary accent.
- **Idle animation.** A Kiter idle-floats and twitches away from camera-side; a Charger paws the ground; a Hunter scans.
- **Name and tooltip.** When the player hovers a ball in shop, the name and a one-sentence role description appears.

This is the visual hierarchy (Ch. 9): silhouette is dominant (visible at any zoom), colour is secondary (visible from across the screen), animation is tertiary (visible in calm moments), text is least dominant (read only on intent).

### 3.5 Weapon swap mechanic

**Cost: 2 Charge. Available all rounds.**

Selecting "swap weapon" on an owned ball opens a 1-Charge mini-shop of two weapon options (filtered to *different* weapons than the ball currently holds). Pick one to apply. The ball's *named identity* updates accordingly (Reaver who swaps Axe→Bow now displays as "Vanguard").

This is deliberately understated:
- Costs less than a modifier slot but more than a reroll — never the obvious top-priority spend
- Identity updates so the player isn't confused about what their ball does
- Creates a small extra puzzle layer for advanced players ("I want my Reaver's mobility but Bow range — that's 2 Charge to swap")

### 3.6 Identity vs. authorship — the two parallel identity axes

Sylvester's framework would describe this design as having **two parallel identity systems**:

- **Mechanical identity (designed):** The 9 archetypes. Player picks among them. This is the lexicon ("I run double Sniper"). It is *finite*, *named*, and *teachable* in onboarding.
- **Strategic identity (authored):** The modifier configuration on top. This is the player's signature. It is *combinatorial*, *unnamed* by default (the community names common builds emergently — a "double-Detonate Reaver" might be called "Bombardier" by streamers), and *re-authored every match*.

This two-layer identity is the model used by Hearthstone (class = mechanical identity; deck = strategic identity), MOBAs (champion = mechanical; build = strategic), and TFT (units = mechanical; comp = strategic). It is a load-bearing design pattern in the genre.

### 3.7 Open questions on archetypes

- ❓ Should Vanguard be replaced by a designer-favoured 9th archetype, or kept as the deliberate "weird pick"? Vanguard's existence is structurally important (fills the 3×3 grid) but it might be playtested into a stronger identity. Lean: keep the structural slot, tune until it has a legitimate niche.
- ❓ Should weapon-swap retain the *original* archetype's silhouette, or update to the new pair's? Update gives clarity (signal); retain gives "I have a Reaver with a bow!" expressive flair. Lean: update silhouette + name, but show "(was Reaver)" subscript for one round.
- ❓ How many launch skins per archetype? Decoupled from mechanics — see §3.6. Lean: 1 base + 1 "Founder" cosmetic for early-access players. Skins are scaled post-launch.

---

## 4. Inter-Run Progression — The 10-Win Climb

### 4.1 What the climb *is*, mechanically and emotionally

A "run" begins at win 0, ends at win 10 (victory) or 0 lives (defeat). Sylvester's pacing curve (Ch. 1) — *hook → rising action → climax → denouement* — must be designed *across* this 10-match arc, not just within each match. The climb is the experience the game sells.

### 4.2 Lives, comebacks, and the failure state

| Element | Value | Justification |
|---------|-------|---------------|
| Starting lives per run | **3** | One life is too punishing; five is too forgiving. Three creates "you can recover once" tension. |
| Loss cost | **−1 life** | Direct and legible. |
| Win streak heal | **+1 life on 3rd consecutive win** | Once per streak. Caps at 3 lives. Aligns with the comeback theme; rewards consolidating a hot streak. |
| Run-over condition | Lives = 0 | Restart at win 0; meta-unlocks persist. |
| Run-complete condition | 10 wins | Cosmetic crown unlock; Hard Mode unlocks; replay becomes prestige. |

**Why not Slay the Spire's "one death = run over"?** StS sessions are 45-90 min. Our sessions are 30-45 min over 10 matches but each individual match is 3-5 min — the texture is very different. A single bad match-RNG outcome should not auto-end a 30-minute commitment; that would create *bad* variable-ratio frustration. Three lives is the forgiveness window the StS players' own grief-frequency tells us we need.

**Why not unlimited / infinite continue?** Per Ch. 8 — without a real failure state, the mastery motivation collapses. Players' eventual "10 wins" feels like a guarantee, not an achievement. The lives system is the **stakes** the climb needs.

### 4.3 Difficulty and complexity scaling across the 10 wins

The asynchronous opponent at each bracket is matched by win count (per the existing design decision). To make the brackets *feel* distinct — not just slightly harder bots — content and complexity scale:

| Win | What the bracket adds | Player meta-unlock |
|-----|----------------------|--------------------|
| **1** | First taste of opponent — onboarding bracket bots use only Common balls + 1 Common modifier. | First archetype skin unlock; tutorial completion. |
| **2** | Opponents now use Rare balls. | Second-archetype skin unlock. |
| **3** | First Epic-tier modifier appears in shop. AI starts using basic synergies (e.g., 2× Bow = Volley). | **New ball roster unlock (5th and 6th archetype available).** |
| **4** | AI uses full modifier rotation; some bots run intentional comeback comps. | Modifier preview tooltips expand to show stacking interactions. |
| **5** | "Veteran" bracket. AI uses 1-2 explicit synergies per team. | **2 new modifiers unlock (8 total).** Mid-climb cosmetic title. |
| **6** | AI uses Epic upgrades from R4. | Match recap screen adds detailed stats and counter-suggestions. |
| **7** | AI uses optimised TFT-bot-style economy (aggressive interest). | **New ball roster unlock (7th and 8th archetype).** Exotic modifier appears. |
| **8** | AI uses Epic synergies (3-modifier combos). | All modifiers unlocked (12 total). |
| **9** | **"Master" bracket.** AI runs full meta builds. | Loss bonus reduced *for AI* (so AI snowballs harder when ahead) — this bracket *should* feel like a wall. |
| **10** | AI is calibrated to ~50% win-rate against a player who has internalised the meta. | **Crown cosmetic, Hard Mode unlock, prestige replay available.** |

### 4.4 Why these specific checkpoints (1, 3, 5, 7, 10)

Sylvester's pacing curve principle says we want emotional milestones that *feel* like inflection points. We deliver them at:

- **Win 1.** First victory ever for new players. The hook completes. Confirms "I can do this." (Ch. 8 — *reinforcement schedule*; first reward must be guaranteed.)
- **Win 3.** Rising action. New ball roster unlock = the game *gives back*. Player's repertoire visibly grows. This is the strongest retention checkpoint because the player can now feel that "the game has more to give me."
- **Win 5.** Mid-climb. Halfway. New modifier unlock + cosmetic title = "I'm a mid-tier player now." (Reinforces *high-status* human value — Ch. 1.)
- **Win 7.** Steep climb. Exotic modifier + last roster unlock. The full design palette is now in play. (This is the *climax setup* — the player has all the tools they'll have at win 10.)
- **Win 10.** Climax + denouement. Crown unlocks, Hard Mode unlocks. (Acquisition + status + mastery, three triggers at once.)

❓ **Playtest target:** Player retention rate at each checkpoint should be measured separately. Drop-offs between (e.g.) win 5 and win 6 reveal where the climb's emotional payoff is undershooting.

### 4.5 Persistent meta-progression

Unlocks earned at milestone wins (new archetypes, modifiers, cosmetics) are **permanent across runs**. A player who fails at win 4 still has the win-3 unlock for their next attempt.

This is critical per Ch. 8 (Motivation):
- **Intrinsic motivation is preserved** because the meta-unlocks add to *the game itself*, not to a separate metagame screen
- **Failure feels productive** because each failed run still moves something forward
- **Player's remorse is averted** because no time is "wasted" — even a losing run contributes to the climb's content unlock

But we resist the temptation to scale player *power* with persistent unlocks. The unlocks are **content**, not power. A player at win 7 with all archetypes available is not stronger than a fresh player using the same archetypes — they just have more options. This avoids the *overjustification* trap where extrinsic rewards crowd out intrinsic enjoyment.

### 4.6 Hard Mode and post-win-10 replay

After winning a run (10 wins), the player unlocks **Hard Mode** — a parallel climb with:
- 2 lives instead of 3
- Stronger AI from win 1
- Same 10-win goal, harder texture
- Prestige cosmetics on completion

Hard Mode is *not* a remix of mechanics — it's the same game, harder. This is critical because **content restrictions** (Ch. 2) forbid systems that fork the design space. Hard Mode reuses every mechanic; only the difficulty curve shifts.

❓ **Open: should there be a daily seed mode (fixed RNG)?** A daily seed creates leaderboards and a social ritual. Lean: yes, *after* core climb proves out — daily seed is a retention layer, not a launch necessity.

---

## 5. Synergy System

### 5.1 Explicit vs. emergent — and the hybrid choice

The question: should team-composition synergies be **explicit** (TFT-style traits with visible "2/4/6 active" UI), **emergent** (Slay the Spire combos with no UI labelling), or **hybrid**?

Recommendation: **hybrid, weighted toward emergent.** A small set of 5 explicit synergies + an unbounded emergent space.

Justification:
- **Explicit synergies serve onboarding** (Ch. 3 — accessibility) by giving new players legible goals ("I want to trigger Volley"). Without any explicit synergies, the game would be too abstract for a player at win 1.
- **Emergent synergies serve depth** (Ch. 2 — elegance). They are where the prep-phase puzzle keeps generating new lessons across the 10-win climb.
- **Pure explicit** collapses into Sylvester's *fallacy of vision*: the designer authors the dream and the player checks boxes. The TFT meta's worst stretches were when traits dominated unit identity.
- **Pure emergent** loses the new-player anchor and creates information starvation. Players can't *learn* a system that gives them no feedback on what's working.

### 5.2 The five explicit synergies

Designed to span the three mechanical axes (movement, weapon, modifier) and to encourage different team-comp archetypes:

| # | Name | Trigger | Effect | Axis covered |
|---|------|---------|--------|--------------|
| 1 | **Pack Hunters** | 3× Hunter movement (all 3 balls) | Each ball gains +25% damage vs. the current lowest-HP enemy ball. | Movement |
| 2 | **Onslaught** | 2× Charger + any Sword weapon ≥1 | All Chargers gain +50 starting momentum; Sword users gain stagger on hit. | Movement + Weapon |
| 3 | **Volley** | 2+ Bow weapons | Arrows gain a single ricochet to a second target (60% damage). | Weapon |
| 4 | **Detonation Chain** | 2+ Detonate modifiers across team | One Detonate triggering causes others within range to trigger simultaneously. | Modifier |
| 5 | **Multiplicity** | 2+ Split modifiers across team | First Split per round produces an *extra* spawn (total 3 instead of 2). | Modifier |

**Why exactly 5:** Sylvester's *similar scale* smell (Ch. 2) — the player has 3 balls and ~3 rounds of meaningful modifier decisions. 5 synergies is a small enough number to be remembered after a single match, and large enough to encourage diverse compositions. Six or more starts pressuring the UI; four leaves modifier-pure compositions without payoffs.

### 5.3 Emergent synergies — designed-for-discovery interactions

The bigger design space is the **uncounted** combinations the system produces. A few worked examples:

- **Kiter + Bow + Split**: a Sniper that fires two arrows on every shot (Split applies to the projectile, not the ball). Emergent name: "Twin-strike Sniper." Wins by overwhelming a tank ball with double-volume damage.
- **Charger + Axe + Detonate**: a Reaver that explodes on its first kill. Emergent name: "Suicide Bomber" / "Powder Keg." Wins by trading 1-for-2.
- **Hunter + Sword + Split**: a Duelist that splits into two smaller Duelists when its first target dies. Emergent name: "Hydra" / "Multiplying Stalker." Wins by overwhelming reactive defences.
- **Three different Movements, all with Detonate**: no Pack Hunters bonus, no Onslaught bonus, but the area coverage when all three detonate is enormous. Emergent name: "Trifecta Bomb." Wins by playing a high-variance lottery.

**Note that the emergent synergies are not authored.** They arise from the mechanic interactions and the player's apophenia (Ch. 4 — *the human tendency to see patterns in random data*). The community names them; the designers tune the *primitives* so no combination is degenerate.

### 5.4 Diminishing returns on stacking — the anti-degenerate rail

Every modifier stacks **with diminishing returns**:

| Stack count | Modifier effect multiplier |
|-------------|---------------------------|
| 1× | 100% |
| 2× | 175% |
| 3× | 235% |
| 4×+ | 275% (asymptotic cap) |

This curve prevents the obvious failure mode of "stack 4× Detonate = double damage." Diminishing returns reward *the first 2 stacks* heavily, then taper. This is the same curve Balatro uses on its joker multipliers, and it works.

❓ **Playtest target:** No single explicit synergy should dominate >35% of post-win-5 winning team compositions. If one does, nerf the synergy effect or split it.

### 5.5 Synergy UI

In the prep phase:
- A **synergy panel** on the right side of the screen shows the 5 explicit synergies as icons.
- An icon dims to grey if the player has 0 progress toward it.
- An icon brightens to gold + shows a count ("Pack Hunters: 2/3") if the player has partial progress.
- An icon shows a "ACTIVE" pulse if the synergy is currently triggered.

This is **redundant communication** (Ch. 9) — colour + count + pulse + icon. Information reaches the player even if they skim.

Emergent synergies have **no UI label**. They are discovered, not displayed.

### 5.6 Open synergy questions

- ❓ Should there be a synergy-discovery codex that records emergent combinations the player has used? It serves intrinsic motivation but risks turning emergence into checklist content. Lean: yes, but only as a post-match recap, not a live tracker.
- ❓ Should synergies be teased in the shop (e.g., a Bow icon glows when buying it would trigger Volley)? Risk: too much hand-holding eliminates the puzzle. Lean: only on the player's *first* match per archetype.

---

## 6. Battle Resolution Depth

### 6.1 The design problem, restated

Combat is automated. ~18 seconds, no player input. The player has already locked in their composition + position. The battle's job is to deliver the GDD's stated emotional target: **anticipation and satisfying VFX-driven payoff.**

The design tension: combat must feel **unpredictable enough to anticipate** but **deterministic enough to feel earned**. Per Ch. 5 (Decisions) — too predictable means no real decision was made in prep; too random means the prep didn't matter.

### 6.2 The five variance sources

| Source | Player influence | Variance contribution | Notes |
|--------|------------------|----------------------|-------|
| **Initial formation** | High (player chooses) | Low (deterministic once set) | A 3×2 grid of 6 positions; player places 3 balls. |
| **Targeting tie-breaks** | Medium (build influence) | Medium | When 2 enemies are equidistant, Hunter/Charger use deterministic ordering rules; Kiter randomises (variance). |
| **Physics collisions** | None (emergent) | High | Balls bumping creates Earclacks-style chaos. The signature visual of the game. |
| **Critical hits** | Low (small RNG) | Low (capped impact) | 10% base crit chance, 1.5× damage. Capped so single crits can't reverse a battle. |
| **Modifier proc timing** | High (build) | Medium | Detonate triggers on HP threshold; Split triggers on first kill — order of operations creates variance. |

Total variance is **moderate** — battle outcomes are 75% determined by composition, 25% by in-battle stochastics. This ratio is the right "earned but not certain" target. A player who built a strict counter wins ~80-90% of matchups. A player who built parity wins ~50%. RNG can swing 5-15% of matches.

### 6.3 Formation: the under-rated prep decision

A **3 columns × 2 rows = 6 position** grid. The player places their 3 balls anywhere in those 6 positions. Front-row balls engage first; back-row balls are screened.

Why this matters:
- Formation creates a **second axis of yomi** (Ch. 7 — multiplayer). "If I front-line my tank, they'll target it. If they expect me to front-line, I can back-line and bait."
- It rewards reading the opponent's composition (visible during prep, since async PvP can show the *last-round's* opponent composition).
- It creates the prep-phase texture that pure team-comp decisions can't.

**Formation cost: free.** Re-arranging is unlimited within the prep timer. This is intentional — the formation is the *quick* decision; the comp is the *slow* decision. Different temporal scales of choice (Ch. 5).

### 6.4 Targeting rules per movement

The targeting logic must be **predictable enough to plan around** but with **enough edge cases to create surprises**. Specifically:

| Movement | Target priority | Tie-break rule | Variance source |
|----------|----------------|----------------|-----------------|
| **Kiter** | Furthest enemy from me | Lowest enemy ID (stable) | If forced into range, picks a *random* nearby enemy. **Source of "the Sniper got cornered and panicked"** moments. |
| **Charger** | Closest enemy | Earliest-spawned enemy if tie | Deterministic — Charger is the predictable archetype. |
| **Hunter** | Lowest-current-HP enemy | Closest if HP tie | If multiple low-HP enemies, **picks the one most recently damaged** — creates "ganking" emergent behaviour. |

These rules are visible in the tooltip on each archetype, so a skilled player can predict and plan. But the *combination* of three balls each with their own rule, against three opposing balls each with their own rule, creates a chess-like resolution that is hard to fully predict.

### 6.5 Physics collisions — the Earclacks signature

The game's fictional grounding is the Earclacks ball-combat videos. The *signature visual* — and the GDD-stated "satisfying VFX-driven payoff" emotional target — comes from balls colliding mid-attack and creating chain reactions.

Specifically:
- Balls have physics colliders.
- Mid-attack, balls maintain their movement (not pinned).
- Collisions cause minor knockback (~0.5 ball-radius) and a small spark VFX.
- Wall collisions cause a bounce — the arena is bounded.
- *Knockback does not interrupt attacks* — attacks resolve on their original target. This preserves predictability while keeping the chaos visual.

Per Ch. 9 (Interface, *control feel*): the knockback should *feel* satisfying without changing the strategic outcome. The collision noise is **expressive variance** — visually rich, mechanically minor.

❓ **Playtest target:** Players should report "satisfying" or "spectacular" battles when surveyed, while objectively-similar player-skill matchups should resolve identically ≥80% of the time.

### 6.6 Critical hits — calibrating the RNG ceiling

10% base critical chance. 1.5× damage on crit. Why these numbers:

- **10% chance** is high enough to feel like a *possibility* every shot but low enough that no specific shot is *expected* to crit. (Sylvester: emotion arises in anticipation — 10% creates anticipation; 50% creates expectation; 1% creates indifference.)
- **1.5× damage** is enough to swing a close exchange but not enough to reverse a lopsided one. A crit on a 30-damage shot deals 45, not 60 — meaningful but not match-deciding.
- **No "critical chain"** — multiple crits in a row are pure independent probability. We do not pity-time or streak crits, because that would introduce a degenerate strategy ("save my big swing for after 3 misses").

Crits create a small but visible "moment" in battle — a screen flash, a particle burst — for spectacle (Ch. 1, the *spectacle* emotional trigger) without disturbing structural balance.

### 6.7 Modifier proc order — where the strategic depth lives

The most consequential variance source is **modifier proc order**. Two examples:

- **Detonate triggers at 20% HP.** If a ball with Detonate is killed *before* hitting 20% HP (e.g., one-shot), Detonate **does not trigger**. This creates a real strategic question: did the opponent gamble that they could kill the Detonate ball before it triggered?
- **Split triggers on first kill.** If a ball with Split has *no* kill in the entire battle, Split never procs. So Split is wasted on a tank build.

The proc-order rules are **visible** (tooltips explain triggers) but **timing-emergent** (whether a trigger fires depends on the unfolding battle). This is exactly the emergent-puzzle texture the design wants.

### 6.8 Battle pacing within 18 seconds

The 18-second battle should have its own pacing curve:

- **0-3s: Approach phase.** Balls move to engagement positions. Tension builds. Music tempo rises.
- **3-10s: Main exchange.** First kills happen. Modifiers proc. Crits land. Peak spectacle window.
- **10-15s: Cleanup.** Most kills done; survivors finish off retreaters.
- **15-18s: Resolution.** Final HP totals visible; winner determined.

If a battle is one-sided (e.g., 3-0 first-kill sweep), the clock can advance fast-forward through 10-15s to keep total match pacing tight. This is the **emotional life support** principle (Ch. 3) — don't make the loser sit through a 5-second wipe.

### 6.9 Why the player can't intervene mid-battle

A deliberate non-feature. Mid-battle abilities would:
- Pull the player out of the *anticipation* emotional state (Ch. 1)
- Re-introduce manual skill, which collapses the prep-phase puzzle into a "play hands-on if you don't trust the comp" cop-out
- Increase per-match length materially, hurting the 3-5 min target

The auto-battler's core promise — *you decided everything in the 25-second prep, now watch it play out* — is preserved by the no-input rule. Sylvester: every feature must justify itself; mid-battle input is unjustifiable here.

### 6.10 Open battle questions

- ❓ Should there be an environmental hazard (e.g., a pit in the middle that knocks balls offscreen)? Tempting for spectacle, but content-restricts future maps. Lean: no for launch; revisit post-launch.
- ❓ Should losing balls have a death animation that pauses combat briefly? Risks breaking the 18s budget. Lean: 0.3s pause per death, capped at 1s total per battle.
- ❓ Should there be a manual "speed up" toggle for veterans? Yes — 1× / 1.5× / 2×. Per-match-setting. (Per Ch. 9, *input assistance*.)

---

## 7. Player Journey for Three Personas

The three personas are not made up — they correspond to documented patterns from autobattler community analysis (TFT subreddit demographic studies, SAP Discord behaviour reports). For each, we walk an *honest* 10-win climb — including where the player might bounce off.

### 7.1 Persona A — The Strategist ("Theory")

**Profile:** Reads patch notes day one. Builds spreadsheets. Wants to *solve* the meta. Plays 10-20 hours a week of strategy games. Expert at TFT (Master rank) and Slay the Spire (A20H).

**Win 1.** Plays the tutorial run. Finishes in ~4 minutes. Reads every tooltip. Hovers every modifier to understand stacking math.
- *Emotional state:* Curious-positive. "OK, this is the toolkit. Let me see what it does."
- *Risk:* If the tutorial under-explains modifier interactions, Theory bounces here. **Mitigation:** A "deep tooltip" mode (hold Alt) shows full stacking math and proc rules.

**Wins 2-3.** Theory starts trying weird builds — a Vanguard with double Split. Win-rate is mediocre, but Theory is *learning the system*, not chasing wins.
- *Emotional state:* Engaged-experimental. Theory is the player who *wants* to discover emergent synergies.
- *Risk:* If the system has degenerate strategies, Theory finds them in 90 minutes and complains on Reddit. **Mitigation:** Diminishing returns + component-level patches keep the design space wide.

**Wins 4-7.** Theory now has the full archetype roster. Theory is theorycrafting in the post-match recap. Discovers the "Detonate Reaver chain" and posts a guide.
- *Emotional state:* Mastery-driven. Theory is building their community identity around expertise.
- *Risk:* Solving the game = Theory leaves. **Mitigation:** Limitless depth via Yomi (Ch. 7) — the asynchronous PvP means each opponent is a different mind. The skill ceiling is *opponent reading*, which is unsolvable.

**Wins 8-10.** Theory hits the Master bracket. Win-rate drops as AI uses meta-counter builds. Theory pivots to anti-meta tech picks (e.g., the awkward Vanguard, now optimised). Wins 10.
- *Emotional state:* Triumph. Unlocks Hard Mode. Immediately starts a new run on Hard Mode.
- *Long-term retention:* Theory plays Hard Mode for 50+ hours, posts theorycraft, becomes a community pillar.

**Where Theory bounces:** Day 1 if the system is shallow. Week 2 if there's a single degenerate build. Month 1 if patches break their preferred meta without communication. **The design must be deep, balanced at the component level, and patched transparently.**

### 7.2 Persona B — The Optimizer ("Climber")

**Profile:** Plays to *win*. Looks up tier lists. Wants efficient progression. Plays 5-10 hours a week. Mid-rank in TFT (Diamond). Doesn't read patch notes but checks the meta tier list before each play session.

**Win 1.** Tutorial. Skim-reads tooltips. Finishes the tutorial as fast as possible.
- *Emotional state:* Impatient-neutral. Wants to get to the climbing part.
- *Risk:* If the tutorial is more than 5 minutes, Climber resents it. **Mitigation:** Tutorial is structured as *the first real match*, scaffolded with extra hints — not a separate sandbox.

**Wins 2-4.** Climber Googles "best Arena of Chaos starter build" before win 2. Finds a community guide (Theory's guide, perhaps). Adopts it.
- *Emotional state:* Confident-optimising. Climber wants the *known good* path.
- *Risk:* If the meta is rigid and Climber's chosen build loses to a hard counter, Climber feels cheated. **Mitigation:** The rock-paper-scissors graph is intentional — no build wins everything; counterplay exists. Display this honestly.

**Wins 5-7.** Climber is in the mid-bracket. The meta builds are not auto-wins; opponents counter them. Climber must *adapt* — start picking *into* the opponent's likely composition.
- *Emotional state:* Engaged-pressured. This is the genre's classic "I have to read my opponent" moment.
- *Risk:* If async PvP doesn't show the opponent's previous-round composition, Climber feels they have nothing to react to. **Mitigation:** Show the opponent's *previous match* composition in the prep UI. (Imperfect info; they'll have changed it.)

**Wins 8-10.** Climber hits Master bracket. The win-rate dip is severe — Climber may stall at 8 or 9. Each loss costs a life; Climber feels real stakes.
- *Emotional state:* Tense, occasionally frustrated.
- *Risk:* If win 9 takes >10 attempts, Climber quits. **Mitigation:** Win-streak life regeneration. The 3-life buffer + streak heal means a Climber can attempt many matches before run-over.

**Where Climber bounces:** If the climb is too easy (no challenge); if the climb is too random (skill doesn't visibly help); if a single match is too long. **The 3-5 min match length is critical for Climber retention.** Climber will replay a 5-min match 8 times to win 9 → 10; they will not replay a 30-min match.

### 7.3 Persona C — The Casual ("Vibe")

**Profile:** Plays for fun and visuals. Doesn't track win-rate. Plays 1-3 hours a week. Discovered the game from a TikTok of the Earclacks-style explosion VFX. Played Mario Kart and Among Us but not TFT or SAP.

**Win 1.** Tutorial. Reads about half the tooltips. Misses some mechanics but finishes.
- *Emotional state:* Curious-positive. The Earclacks-style explosions are exactly what Vibe came for.
- *Risk:* If the tutorial is dense or the prep phase feels like a tax form, Vibe bounces in 5 minutes. **Mitigation:** Round 1 prep is just *picking 3 balls*; modifiers don't appear until R2. The cognitive ramp matches the player's familiarity ramp.

**Wins 2-3.** Vibe picks balls based on *which one looks coolest*, not which one is mathematically best. Vibe loses some matches but the battles are pretty.
- *Emotional state:* Entertained-light. "I lost but that explosion was sick."
- *Risk:* If losses feel punishing (long animations, scolding feedback), Vibe drifts away. **Mitigation:** Loss screen is 3 seconds with a "well played" tone; no critique unless the player opts in.

**Wins 4-6.** Vibe is now in mid-bracket. AI is harder. Vibe loses more.
- *Emotional state:* Casual-stalled. May play less for a few sessions.
- *Risk:* This is the **drop-off zone** for casual players in every autobattler. **Mitigation:** The 3-life system means Vibe can keep playing matches even while losing. The win streak heal feels generous; the run-over feels survivable.

**Wins 7-9.** Vibe is at their natural skill ceiling. They might never reach win 10 in a single run.
- *Emotional state:* Plateau-acceptance. "I'm pretty good but not amazing, and that's fine."
- *Risk:* If win 10 feels mandatory for any social/cosmetic reason, Vibe feels excluded. **Mitigation:** Cosmetic unlocks at wins 3 and 5 give Vibe meaningful rewards *without* the win-10 commitment.

**Win 10 (eventually).** Maybe never on Vibe's first run. Maybe on attempt 5. When it happens — explosion of celebration. Vibe makes a TikTok of their crown unlock.
- *Emotional state:* Triumph delayed-but-genuine.
- *Long-term retention:* Vibe plays casually, mostly for the satisfying battles. Replays for visual joy, not climb progress. **This is the game's largest market segment by player count, even if not by hours-per-player.**

**Where Vibe bounces:** If the prep phase feels like homework. If matches are >5 minutes. If losses are punishing. If the visual VFX underwhelm. **The Earclacks-style satisfying-physics commitment is non-negotiable for Vibe retention.**

### 7.4 Cross-persona design implications

The three personas need *different things* from the same game (Ch. 7 — divergent goals). The design choices above thread the needle:

| Need | Theory | Climber | Vibe |
|------|--------|---------|------|
| **Match length** | Indifferent | 3-5 min critical | 3-5 min critical |
| **Difficulty curve** | Wants steep | Wants escalating | Wants gentle |
| **Visual fidelity** | Indifferent | Nice-to-have | Critical |
| **Tutorial depth** | Wants deep tooltips | Wants brief | Wants invisible |
| **Win-rate tracking** | Wants exposed | Wants exposed | Wants hidden |
| **Patch notes** | Reads every word | Skims | Doesn't see |

Per Ch. 7, **the design must support multiple valid play styles**. We do this by:
- Optional toggles (deep tooltips, stat displays) — Theory turns on, Vibe leaves off
- A scaling difficulty curve that *all three* can find their level in
- Visual fidelity is universal — Vibe needs it, Theory tolerates it, Climber appreciates it

If a single persona's experience requires breaking another's, the design has overcommitted. The above does not — all three can co-exist on the same 10-win climb because the climb has multiple emotional payoffs (mastery for Theory, optimisation for Climber, spectacle for Vibe).

---

## 8. First-Time User Experience

### 8.1 The complexity budget, and why traditional tutorials fail

A new player must absorb:
- Three-layer mechanic (Movement, Weapon, Modifier)
- 5-round structure with modifier-slot unlocks
- Economy (Charge, interest, win/loss bonuses)
- Shop mechanics (lock, reroll)
- Formation grid
- Synergies (5 explicit + emergent)
- 4 starter archetypes
- 4 starter modifiers
- The targeting rules of 3 movement types

This is a *lot*. A traditional "play 30 minutes of forced tutorial" approach would (a) bore Climber and Theory, (b) overwhelm Vibe, and (c) waste the variable-ratio reinforcement that should be hooking the player.

Per Ch. 3 — accessibility is consistently undervalued by designers because *they* are too skilled to notice barriers. We must design this with explicit barrier-checking.

### 8.2 The FTUE structure: tutorial *as* the first real run, scaffolded

The first run a player plays *is* the tutorial. There is no separate sandbox. This:
- Respects the player's time (each match is real progress)
- Aligns rewards with learning (Ch. 8 — wins reinforce real learning)
- Matches the intra-match complexity ramp (Round 1 = movement+weapon only is *already* the simplest teaching state)

The tutorial scaffolding is *layered onto* the first match:

**Pre-match 1: Cinematic + 30-second concept video.**
- Earclacks-style highlight reel: balls fighting, explosions, victories
- Voice-over (or text) introduces the 3-2-1 hook: "Three balls. Two phases. One goal. Win 10 matches."
- ❓ Skippable after the first viewing.

**Match 1, Round 1: Tooltip-onboarded prep.**
- Shop slots glow one-by-one with explanation arrows
- Player is shown 3 simple ball choices (Common Sniper, Common Reaver, Common Duelist — clear roles)
- Lock and reroll are *introduced verbally* but de-emphasised (the player isn't told to use them; they can ignore them)
- Formation grid appears with a pulse arrow at one position; explicit "drag your balls here"

**Match 1, Round 2: Modifier introduction.**
- A *single* modifier slot unlocks with a highlight
- Shop now shows balls + a modifier
- Modifier tooltip is forcibly opened on first hover
- The player is *not* required to buy the modifier — they can also keep saving

**Match 1, Round 3-5: Gradual reveal.**
- Second modifier slot, upgrades, premiums each appear with a small "NEW" badge
- Explicit synergy panel appears in Round 3 with a brief intro: "If 3 balls share a trait, they may unlock a synergy."

**Post-match 1 recap.**
- Win or lose, the recap shows:
  - The opponent's composition (revealed)
  - A short "did you notice?" callout on something that happened (e.g., "Your Reaver's Detonate triggered at 20% HP, hitting 2 enemies.")
  - The next match's bracket
- Crucially: the player is *not* graded. No "you played poorly." No "skill assessment." Just facts and a hook to the next match.

### 8.3 Onboarding archetypes: a constrained starter set

The new player has access to only **4 archetypes** for their first run (wins 0-2):
- **Sniper, Reaver, Duelist, Vanguard.**

Why these 4:
- *Sniper* teaches Kiter movement (back away, shoot from range)
- *Reaver* teaches Charger movement (rush, melee)
- *Duelist* teaches Hunter movement (target the weak)
- *Vanguard* teaches the "weapon swap" mechanic conceptually — its existence signals "you can change weapons on these balls."

These four cover all three movements and two of three weapons. The remaining 5 archetypes unlock progressively (per §4.3).

**Why not all 9?** Decision-scope (Ch. 5). A player choosing among 9 unfamiliar units in Round 1 of their first match will not make a meaningful decision; they will pick randomly. Four is the right scale for the *cognitive load of learning*. Within 3 matches, the player has touched all four archetypes (probably multiple times) and is ready for more.

### 8.4 Tooltip layering — the deep-info principle

Per Ch. 9 (Interface), information has a hierarchy:

- **Level 1 (always visible):** Ball name, basic role one-liner ("Long-range shooter").
- **Level 2 (on hover):** Movement rule, weapon range/cooldown, current modifier list.
- **Level 3 (on Alt-hover):** Exact damage numbers, proc rules, stacking math, full targeting tie-break rules.

Theory plays in Level 3. Climber plays in Level 2. Vibe plays in Level 1. **The same UI serves all three personas** via this layering.

### 8.5 Priming the second match

The end of match 1 should set up match 2 with a single concrete hint:
- "You used [X archetype] in 2 of your balls last match. Try mixing in [Y archetype] this match — it counters [Z behaviour]."

This is **social imitation through the game itself** (Ch. 9). The game is teaching the player how to think about composition without lecturing.

❓ **Playtest target:** After match 3, ≥80% of players should be able to correctly identify which archetype counters which (rough rock-paper-scissors). Below this rate, the priming isn't working.

### 8.6 Failure handling in early matches

If the new player loses match 1 (which they likely will against a calibrated bot at 50% win-rate):
- The recap emphasises *what they did right* (1-2 specific events: "Your Detonate triggered for 80 damage")
- It gives one concrete, single-sentence suggestion: "Try buying a modifier in Round 2 next match."
- It does **not** show losses as a count or lives lost prominently — the lives system is introduced *gradually* across matches 1-3.

Per Ch. 3, *failure must feel like a learning opportunity, not punishment*. The tutorial run especially must respect this — first-match losses are the highest-risk drop-off moment in any game.

### 8.7 Tutorial completion = full unlock

After **match 3**, regardless of wins/losses:
- All 4 starter archetypes are fully unlocked
- All tooltips are dismissable
- The full UI is available
- Win-rate stats start being tracked
- Future matches are "real" with full lives consequences

This is the FTUE's hard exit. From match 4 onward, the player is climbing for real. The hidden grace of matches 1-3 (no life penalty, extra hints, calibrated easy bots) is now gone.

❓ **Open: should match 1-3 wins count toward the win-10 climb?** Lean: yes, but with a marker (★ symbol). Counting them avoids feeling like the tutorial was "wasted time"; the marker preserves the integrity of the win-10 milestone for the leaderboard.

### 8.8 Open FTUE questions

- ❓ Should there be an offline "puzzle mode" that teaches specific synergies through scripted scenarios? Tempting but increases scope. Lean: post-launch only.
- ❓ Should the tutorial be replayable? Yes — accessible from the main menu's "How to Play" button.
- ❓ Should the tutorial show win/loss state but suppress life-loss in the first 3 matches? Strongly lean yes — life loss is a heavy concept to introduce while a player is still learning archetypes.

---

## 9. Open Questions Summary (Playtest-Required)

These are decisions that **cannot** be resolved through analysis alone — they need building and playtesting. Listed here in priority order:

| # | Question | Why it can't be answered in advance |
|---|----------|-------------------------------------|
| 1 | Are the loser-bonus and interest numbers right? | Open-rate of recovery from R1 loss is the canonical metric (35-45% target). Only playtest reveals the true curve. |
| 2 | Does the 3×2 formation grid create meaningful prep tension, or feel arbitrary? | Player surveys + formation-change-count telemetry needed. |
| 3 | How often do explicit synergies trigger at the Master bracket? | If <25%, they're under-tuned; if >65%, they dominate. Telemetry-driven. |
| 4 | Is the 18-second battle length right? | Too short = unsatisfying; too long = tedious. Median engagement curve from playtests. |
| 5 | Should weapon-swap exist at all? | Removing it simplifies the design and forfeits the "Bow Charger" emergent build space. Playtest needed to measure the actual usage rate and whether it justifies its UI cost. |
| 6 | Vanguard viability — is the 9th archetype salvageable or should it be replaced? | After 200 hours of playtest, win-rate by archetype tells the story. |
| 7 | Should the climb have a fixed time-of-day reset (TFT-style ranked seasons)? | This is a *retention* question, not a core-design one. Defer to post-launch. |
| 8 | What is the optimal length of the FTUE? | 3 matches is a guess; 2 or 4 may be better. A/B test post-soft-launch. |
| 9 | Pace of cosmetic unlocks — too few, too many? | Drives long-tail retention. Telemetry only. |
| 10 | Match queue time vs win count — does async PvP scale to enough population per bracket? | This is a market/launch question that depends on launch population. Playtest can't fully answer; partial answer from soft-launch. |

---

## 10. What This Doc Did Not Cover (Out of Scope, Acknowledged)

To stay honest about scope, the following were **not** designed here and require separate work:

- **Visual style guide** — pixel art conventions, colour palette beyond the movement-type ramp, VFX library
- **Audio design** — music tempo curves, SFX layering for the battle pacing
- **Monetisation model** — cosmetic-only vs. battle-pass vs. expansions; lives/Charge purchase questions
- **Server/networking** — async PvP technical architecture (the existing project uses Photon Fusion 2 for the real-time PvPvE — auto-battler async needs different infra)
- **Live ops cadence** — patch frequency, balance change communication, seasonal content
- **Localisation** — archetype names and tooltip copy will need localisation review
- **Accessibility** — colour-blind palette, motor-impairment input mapping, cognitive load options beyond the deep-tooltip toggle

These are explicit non-tasks of this document. Flagging them prevents the design from looking complete when it isn't (Ch. 11 — *therapeutic planning* warning).

---

## 11. Final Design Review Checklist (Self-Audit)

Applied from `docs/game-design/instructor/analysis/design-review-checklist.md`:

### 11.1 Experience Chain
- **Emotions designed:** Anticipation (battle); puzzle-solving / wit (prep); satisfaction / acquisition (shop and unlocks); mastery / triumph (climb completion); occasional frustration (controlled by 3-life system + win-streak heal).
- **Human values shifting:** [victory/defeat] each round, [wealth/poverty] each round (Charge), [skilled/unskilled] across the climb, [high status/low status] at cosmetic milestones.
- **System or mental movie?** System — every claim above is grounded in a mechanic with specified numbers.
- **Worst experience:** A player who loses 5 R1 → R2 in a row at win 8, runs out of lives, sees the run-end screen, and quits. Mitigated by the streak heal + the persistent meta-unlocks giving residual value even on a failed run.

### 11.2 Elegance
- **Mechanic count:** ~14 distinct mechanics (3 movements, 3 weapons, ≥4 launch modifiers, economy/Charge, interest, shop, reroll, lock, formation, crits, synergies, lives, unlocks, modifier stacking).
- **Interactions:** Strong. Every mechanic interacts with multiple others. Per the elegance audit in §1.7, the economy alone touches 6 systems.
- **Overlapping roles:** None identified. Lock ≠ reroll ≠ walk-away (the lock-reroll-walkaway triangle is explicitly designed to be non-overlapping). Synergies (explicit) ≠ stacking (mechanic) — different timescales.
- **Content restrictions:** Minor — physics arena bounds future map design somewhat; weapon-swap requires every Movement-Weapon pair to be at least viable. Both are acceptable.
- **Emergence:** Strong — diminishing-returns stacking + targeting tie-breaks + physics collisions + modifier proc timing combine to produce countless unauthored battles.

### 11.3 Skill and Challenge
- **Skill barrier:** Low — Round 1 of match 1 is just "pick 3 balls." FTUE designed for accessibility.
- **Skill ceiling:** Limitless via multiplayer Yomi — opponent reading is the long-term mastery axis.
- **Reinventions:** Manual (none — auto-battle) / Situational (composition matching, formation choice) / Mental (opponent prediction, meta-anticipation, theorycraft). The game lives in the *situational* and *mental* reinventions, which is correct for an auto-battler.
- **Elastic challenges:** Yes — win-rate scaling, cosmetic unlocks at milestones, optional Hard Mode post-win-10.
- **Failure handling:** 3 lives + streak heal + permanent meta-unlocks soften the run-over. Recap shows wins-right.

### 11.4 Narrative
- **World narrative (Ch. 4):** Light — the Earclacks fiction inspires the visuals but the game does not commit to a deep world setting. Acceptable for the genre.
- **Emergent story:** Strong — modifier combinations produce per-match narratives ("the Reaver detonated and chained three kills").
- **Fiction-mechanics fit:** Strong — the ball-physics fiction directly justifies the Earclacks-inspired battle phase; the named archetypes carry mechanical identity.

### 11.5 Decisions
- **Future-feel:** Yes — economy decisions show the player visible Charge balances and round-by-round costs.
- **Information balance:** Good — async PvP reveals opponent's prior composition (info) but not their current shop or future intent (hidden).
- **Flow gaps / overflow:** 25s prep + 18s battle alternation prevents both — prep gives time to think, battle gives time to watch.

### 11.6 Balance
- **Degenerate strategies:** Watched for at the component level (modifier stacking, archetype matchups). Diminishing returns on stacks; no single archetype designed to dominate.
- **Balance level:** Component-level (modular subagent's recommendation, accepted in §3.2).
- **Intentional imbalance:** Yes — loser bonus is intentional comeback fuel. Master bracket (win 9-10) is intentionally harder.

### 11.7 Multiplayer
- **Nash equilibrium:** Not a single dominant strategy at the team-comp level — the rock-paper-scissors graph is designed to require opponent reading.
- **Yomi:** Strong — async PvP shows prior round's composition, which is partial info; player must predict the *adaptation*.
- **Destructive behaviour:** Async PvP prevents real-time griefing. Throwing matches is unprofitable (no MMR-based reward to gain).

### 11.8 Motivation
- **Reinforcement schedules:** Layered — fixed (per-round Charge), variable (shop offerings), fixed-interval (win-milestone unlocks), emergent (modifier discoveries). Per Ch. 8 this is the superimposed-schedule pattern that works.
- **Intrinsic vs extrinsic:** Intrinsic primary (the prep-puzzle is the activity reward); extrinsic supportive (cosmetics, unlocks). No daily-login coercion.

### 11.9 Interface
- **Signal/noise:** Designed at three tooltip levels (§8.4).
- **Redundant communication:** Movement encoded in silhouette + colour + animation + name (§3.4).
- **Indirect control:** Onboarding uses arrows, glows, and forced first-hover tooltips (§8.2) without lecturing.

### 11.10 Market
- **Value curve:** Spike on "satisfying physics-VFX auto-battler" (Earclacks niche, no direct competitor). Strong on "short-match autobattler" (SAP-adjacent). Moderate on "deep meta autobattler" (TFT-adjacent).
- **Underserved segment:** Players who *want* SAP's pace but *want* Earclacks's visual juice. This is the unique-value spike.
- **Resource alignment:** ❓ This depends on team size — not specified in the brief. The design assumes a small-to-medium team can ship 9 archetypes + 12 modifiers at launch; if the team is smaller, cut to 6 archetypes + 8 modifiers.

### 11.11 Process
- **Has the core been proven?** No — this is pre-prototype. Per Ch. 11 (Process), the planning horizon here is *short* and the document is honest about which decisions need playtesting.
- **Premature production:** Avoided — no polish until graybox proves the core loop.

### 11.12 Priority list — the five things to address first

1. **Build a graybox of the 18-second battle resolution.** This proves the Earclacks-feel emotional target. Without satisfying battle visuals, the entire design's "anticipation" target collapses. Highest priority.
2. **Prototype the economy with placeholder shop UI.** Verify the Charge-trajectory math from §1.6 holds. This is the second-most load-bearing system.
3. **Tune the diminishing-returns stack curve.** The synergy system's anti-degenerate rail is the most playtesting-dependent number.
4. **Build and test the FTUE through match 3.** Drop-off rate in matches 1-3 is the existential metric. Above 40% drop-off = the design needs rework.
5. **Test the formation grid in isolation.** Does it create meaningful tension or does it feel arbitrary? Could be cut if it doesn't pull weight.

---

## Appendix A — Quick-Reference Number Card (First-Pass Tuning)

| System | Number | Notes |
|--------|--------|-------|
| Starting Charge | 6 | |
| Base income per round | 5 | |
| Win bonus | +2 | |
| Loss bonus | +3 | |
| Win streak (cap +3) | +1/+2/+3 at 2/3/4+ wins | |
| Loss streak (cap +3) | +1/+2/+3 at 2/3/4+ losses | |
| Interest | +1 per 5 saved, cap +3 | |
| Reroll cost | 1 | |
| Lock | Free, 1 slot, persists 1 round | |
| Common ball | 3 | |
| Rare ball | 5 | |
| Epic ball | 8 | |
| Common modifier | 2 | R2+ |
| Rare modifier | 4 | R2+ |
| Epic modifier | 7 | R3+ |
| Modifier upgrade | 3 | R4+ |
| Premium upgrade | 5 | R5 |
| Weapon swap | 2 | R1+ |
| Sell refund | 50% | All rounds |
| Prep duration | 25s | |
| Battle duration | 18s (cap) | |
| Match length target | 3-5 min | |
| Run lives | 3 | |
| Win-streak heal | +1 life at 3 consecutive wins | Cap 3 |
| Crit chance | 10% base | +5% per rarity tier of weapon |
| Crit damage | 1.5× | |
| Modifier stack curve | 100/175/235/275% | Asymptotic cap at 4×+ |
| Archetype count at launch | 9 | (4 in starter set, 5 unlocked at win 3/5/7) |
| Modifier count at launch | 12 | (4 starter, +2 at win 5, +2 at win 7, +exotic) |
| Explicit synergies | 5 | |
| Shop slots | 3 | |
| Formation grid | 3 cols × 2 rows = 6 positions, 3 used | |
| Win goal | 10 | |
| FTUE tutorial length | 3 matches | |

---

## Appendix B — Emotional Arc Map (the Sylvester chain, explicit)

For one canonical 5-round match at win 5:

| Time | Phase | Mechanic event | Emotion triggered | Human value shifted |
|------|-------|----------------|--------------------|---------------------|
| 0:00 | Match begin | Lobby shows opponent's prior comp | Curiosity, mild anxiety | [knowledge/ignorance], [danger/safety] |
| 0:05 | R1 prep | Shop reveals 3 balls | Acquisition anticipation | [wealth/poverty] |
| 0:25 | R1 battle start | Balls move out | Hope + tension | [victory/defeat] |
| 0:35 | First kill | Opponent ball dies | Spectacle + relief | [life/death], [victory/defeat] |
| 0:43 | R1 ends | Player wins R1 | Satisfaction + streak begin | [skilled/unskilled], [high status/low status] |
| 0:50 | R2 prep | Modifier slot unlocks | Curiosity (first time this match) | [knowledge/ignorance] |
| 1:15 | R2 battle | Detonate triggers | Spectacle (peak VFX moment) | [life/death] of multiple balls |
| 1:33 | R2 lost | Streak broken | Mild frustration + loser bonus dopamine | [victory/defeat] shift |
| 1:40 | R3 prep | Player has 14 Charge — Epic option visible | Strategic deliberation | [wealth/poverty], [knowledge/ignorance] |
| 2:30 | R4 prep | Modifier upgrades open | Acquisition + planning | [wealth/poverty] |
| 3:30 | R5 battle (decisive) | Final detonation chain | Climactic spectacle | [victory/defeat] permanent |
| 3:50 | Match ends | Win 6 unlocked or loss life lost | Triumph or determined-recovery | [skilled/unskilled] over the long arc |

This is the engineered experience the design is meant to produce. The mechanics are calibrated to make this exact chain reliably emerge from a *typical* match — not a *best-case* match. If a median match doesn't hit these emotional beats, the design is failing.

---

*End of Auto-Battler Design v1. Take this document, build the graybox, and let the playtest data tell us where these assumptions break.*
