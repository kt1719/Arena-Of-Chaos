# Auto-Battler Economy — Design Exploration

**Status:** Exploration document. **Multiple viable options presented; no single recommendation locked in.**
**Companion to:** `auto-battler-v1.md` (which specifies one economy option; this doc spans the space)
**Date:** 2026-05-15

---

## 0. What this document is, and isn't

This is a focused exploration of the **economy** subsystem alone for the 3v3 async auto-battler. It was produced through two rounds of conversation with a designer-sparring-partner subagent, deliberately weighing multiple approaches rather than picking one.

It exists because the first pass of `auto-battler-v1.md` chose a single conventional answer (TFT/Balatro-style bank-and-interest) without explicitly surveying the design space. This document opens that space back up.

**The reader's job after this doc:** decide which of the three viable options matches the team's risk profile and production budget. Each option is tractable; none is "obviously best" without playtesting.

---

## 1. The exploration arc, briefly

**Round 1** generated 13 candidate economy models, ranging from conventional (TFT, SAP, Balatro lineages) to creative (rerolls-as-currency, HP-as-price, action-points, synergy-as-income). Six candidates were cut for structural reasons (rubber-banding, anti-puzzle incentives, async-incompatibility, runtime mismatch). The subagent proposed a three-mechanic hybrid (forecasting + shared-pool + sacrifice).

**Round 2** stress-tested the survivors with sample R3 prep walkthroughs. The three-mechanic hybrid lost on elegance — too many FTUE concepts for the emotional payoff. Three lighter hybrids beat it. The subagent's final position: prototype **B+S** or **B+S+R-lite** first; treat **H+R** as the differentiated alternative.

The three viable options below are the survivors of that process, adapted from the subagent's framing into terms specific to this game.

---

## 2. The 13 candidates considered (with verdicts)

| # | Model | Lineage | Verdict | One-line reason |
|---|-------|---------|---------|-----------------|
| A | Bank + interest | Balatro (the true TFT is dual-currency, see C) | Cut (regression) | Interest cannot compound in 5 rounds; the decision collapses to one binary save/spend per match. |
| B | Per-round expiration | Super Auto Pets, Hearthstone Battlegrounds, Marvel Snap | **Viable** | Each round is its own contained puzzle. Snap-tested for retention. |
| C | True dual currency (gold + XP) | TFT | Cut | TFT runs 30 rounds; 5-round match doesn't give XP time to differentiate strategies. |
| D | Hybrid expiring + bank | (No clean genre lineage) | Cut | Hedges that pay neither — bank-side doesn't compound, expiration-side is diluted by the bank. |
| E | Rerolls AS the currency | (Closest: Inscryption-style token economies) | Honourable mention | Genuinely creative but has a dominant-strategy hazard. See §5. |
| F | HP/lives as price tag | Slay the Spire, Hades's Charon Well | Cut | Doubles HP's role — battles and shop both tax HP, making losses feel like double-punishment. |
| G | Modifier-sacrifice economy | Pokémon TCG, MtG sacrifice | Cut | Resists engagement at the wrong times (nothing to sacrifice early; refuses to sacrifice late). |
| H | Action points per round | XCOM, Into the Breach | **Viable** | Cleanest pure puzzle-generator. Each AP is fungible across multiple uses. |
| I | Damage-taken-becomes-currency | (Inverted comeback systems) | Cut | Embedded rubber band; players will intentionally underperform to bank currency. |
| J | Unused-prep-time as currency | (No real lineage) | Cut | Directly hostile to the puzzle target — pays players for *not engaging* with the puzzle. |
| K | Bid / auction economy | Catan trading, sealed-bid auctions | Cut | Wrong genre — async PvP means no live opponent to read, so bidding is solo number-guessing. |
| L | Crafting fragments | Hearthstone dust, MtG wildcards | Cut | 50-hour-progression mechanic; 5-round match doesn't accumulate fragments meaningfully. |
| M | Synergy-as-income | (Closest: deckbuilder economy-cards) | Viable as **layer** | Standalone, punishes pivots; as a +1 AP refund per synergy in option Y, self-limiting and clean. |
| N | Pact / debt economy | Hades keepsakes, StS Necronomicurse | Side-explore | Each buy carries a permanent debt. Generates greed-punctured-by-regret. Not pursued here. |
| O | Mancala / pool-redistribution | Wingspan, Catan, Splendor | Cut | Pool-sharing requires live or near-live opponents; async is wrong substrate. |
| P | Compounding-cost (each Nth buy costs more) | Dominion, 4X tech-tree | Side-explore | Built-in anti-degenerate. Worth revisiting as a *layer* on Option X. |
| Q | Lockout / mutual exclusion (no currency) | StS path choice, Inscryption totem | Side-explore | Maximally austere puzzle. Risk: removes accumulation dopamine. |
| R | Placement / spatial grid | Backpack Battles, Inscryption Act 2 | Viable as **layer** | The "fit" puzzle. In our game, naturally manifests as modifier proc-order (1×N line per ball). |
| S | Forecasting / next-round preview | MTG Arena draft, Magic Maze | Viable as **layer** | Anticipation in prep echoes anticipation in battle. Best paired with B. |

---

## 3. Three viable options

Each option is specified by mechanic + sample R3 prep walkthrough + Sylvester audit + pros/cons + production cost.

The sample R3 prep walkthroughs use a **shared fiction** for direct comparison: the player walks into R3 with a 1-1 record. Their team is `[Reaver, Sniper, Duelist]`. They lost R2 because their Sniper got rushed. They have whatever the option's currency model provides.

---

### 3.1 Option X — Snap-shape (B + S)

#### Mechanic specification

- **Per-round expiring Charge (B).** Each round prep starts with a fresh Charge stipend. Charge does NOT carry over. Unspent Charge at round end is lost.
- **Per-round Charge scales by round:** R1 = 5, R2 = 6, R3 = 7, R4 = 8, R5 = 10.
  - Scaling mirrors the increasing complexity (R5 has the most expensive options on offer).
- **Loss-bonus:** Losing player gets +2 Charge in the next round (so a R2 loser walks into R3 with 7+2=9 Charge).
- **No interest, no streak, no carryover.** Save-vs-spend tension is eliminated by design.
- **Next-round shop preview (S).** During prep, a strip at the top of the screen shows three icons representing *next round's* shop slots. Each icon shows what category will appear (ball / modifier / upgrade) and the highest rarity that could appear. Exact identity is hidden — only category and rarity tier.
- **Costs are familiar:** Common ball 3, Rare ball 5, Common modifier 2, Rare modifier 4, Reroll 1, etc. (Per Section 1.4 of the main GDD.)

#### Sample R3 prep walkthrough

The player opens R3 prep. Their Sniper died in R2, so they're prioritizing replacement or protection.

- **HUD:** "R3 Charge: 9 (7 base + 2 loss bonus). Expires at round end."
- **Shop:** 3 slots — `[Common Reaver, Rare Detonate modifier, Common Bow weapon]`.
- **S-preview strip:** R4 will offer `[Ball | Rare Modifier | Upgrade]`. R4 is the upgrade-tier round (per the GDD's intra-match ramp).
- **Decision tree:**
  - Buy the Rare Detonate (4) and stick it on the Duelist; spend 5 remaining on the Common Bow (3) to give Sniper-style ranged coverage. Total 7, 2 wasted.
  - OR buy the Detonate (4) + buy the Common Reaver (3) as a second tank; total 7, 2 wasted.
  - OR reroll (1) hoping for a better R3 option; 8 remaining; if reroll yields nothing better, buy whatever and waste less.
  - Wait — what does R4 promise? An Upgrade tier in slot 3. If I have a modifier, that upgrade slot becomes useful. If I have *no* modifiers, the upgrade slot is wasted next round.
- **Resolution:** Player buys the Detonate (4) to ensure R4's Upgrade slot has something to upgrade, then buys the Common Bow (3) to address the Sniper death. Total 7 spent, 2 lost. R4 will arrive with a fresh 8 Charge and a meaningful Upgrade option.
- **Time taken:** ~18 seconds of the 25s prep window.

#### Sylvester audit

| Smell | Rating | Note |
|-------|--------|------|
| Many interactions | Strong | Charge interacts with shop, rerolls, formation; preview interacts with current decision; loss bonus interacts with both. |
| Simplicity | Strong | Two new concepts (B + S). Charge expires; preview shows next round. Fits in a tweet. |
| Multiple uses | Strong | Same Charge buys balls, modifiers, rerolls — granular spend. |
| Non-overlapping roles | Strong | B is temporal pressure; S is informational. They reinforce without overlap. |
| Reuses conventions | Strong | SAP / HBG / Snap players need no teaching for B; MTG-draft / StS-encounter-preview players need no teaching for S. |
| Similar scale | Strong | Charge 5-10 / costs 1-8. Numbers align naturally. |
| High reuse | Strong | Charge spent 5× per match; preview consulted every prep. |
| No content restrictions | Acceptable | S requires every next round to have a *predictable category mix*, which the intra-match ramp already provides. No new restriction. |

#### Pros / Cons

| Pros | Cons |
|------|------|
| Lowest FTUE wall on the table — two concepts | Lowest differentiation from existing autobattlers |
| Cleanest contained-per-round puzzle | "Boring" answer from a marketing/uniqueness perspective |
| Cheapest to prototype | Eliminates save-vs-spend tension by design (the GDD's prior goal) |
| Snap-tested retention pattern | Some players will find per-round-expiration frustrating ("I had 1 Charge left and couldn't do anything with it") |
| Loss-bonus integrates comeback into the core economy without separate streak math | Decision per prep is narrower — no cross-round planning |

#### Unique emotional event

**"The threat-named buy."** S tells the player *what next round's options are*. Their current buy is informed by that knowledge — "I'm buying Detonate now because R4's Upgrade slot only matters if I have something to upgrade." This is the closest the autobattler genre has come to MTG draft's "wheel" feeling without actually being draft.

#### Production cost

- Implementation: ~3 days. Just remove the bank logic and add a preview strip.
- UI: One strip across the top of prep screen. Two icons + tier marker per slot. ~1 day of art/UX.
- Balance: 5 Charge values to tune (5/6/7/8/10), 1 loss-bonus value, ~10 item costs. Very tractable.

---

### 3.2 Option Y — Action-points economy (H + R)

#### Mechanic specification

- **Action points (AP) per round.** Each prep gives the player a discrete budget of atomic *actions*, not granular currency. AP doesn't carry over.
- **AP scales by round:** R1 = 3 AP, R2 = 4, R3 = 5, R4 = 6, R5 = 7.
- **Action costs:**
  - Buy a ball: 2 AP
  - Buy a modifier: 1 AP
  - Apply / swap modifier: 1 AP (separate from buying; "buy" puts it in inventory, "apply" places it on a ball)
  - Reroll shop: 1 AP
  - Upgrade a modifier: 2 AP
  - Swap weapon: 1 AP
  - Lock a slot: free, once per round
- **Synergy-as-layer (M).** Triggering an explicit synergy (Volley, Pack Hunters, etc.) in battle refunds **+1 AP** into the next prep. Cap: +2 AP per round from refunds.
- **Modifier inventory grid (R, lite version).** Each ball has a small **1×N proc-order list** for its applied modifiers. Order = trigger order in battle. Reordering modifiers is 1 AP. This is the "placement" layer — small, but mechanically real.
- **Charge is not used.** AP is the only resource. Items in the shop don't have variable rarity costs — they have variable AP costs (Common = 1 AP to buy, Rare = 2 AP, Epic = 3 AP). Rarity gates the *AP cost*, not a separate Charge pool.

#### Sample R3 prep walkthrough

Same fiction: 1-1 record, dead Sniper, R3 prep.

- **HUD:** "R3 AP: 5 / 5. (+1 carryover from Volley synergy triggered in R2.)"  — so really 6 AP.
- **Shop:** 3 slots — `[Common Reaver (1 AP), Rare Detonate (2 AP), Common Bow (1 AP)]`.
- **Inventory state:** Duelist has 1 modifier slot open; Reaver has 0 modifiers; Sniper is dead.
- **Decision tree:**
  - Path A: Buy Bow (1) + apply to Duelist (1) + buy Detonate (2) + apply to Reaver (1) = 5 AP. Leaves 1 AP for reorder.
  - Path B: Buy Bow + apply to Sniper-replacement Reaver (1+1) + buy Common Reaver (1) + apply Detonate to it (1+1) = 5 AP. Reaver vs Reaver — board redundancy.
  - Path C: Reroll (1) hoping for a Hunter-class ball to replace the Sniper role (1 AP), then buy. Risk: if reroll yields nothing useful, AP is burned.
- **Resolution:** Player picks Path A: buys Bow, applies to Duelist (now ranged), buys Detonate, applies to Reaver. Reorders Duelist's modifiers: existing Speed-up modifier *before* the Bow's slow-shot. Total 5 AP + 1 AP reorder = 6 AP exactly. Synergy refund consumed.
- **Time taken:** ~22 seconds — more than Option X because each AP spend is a discrete click and the player verifies AP remaining after each.

#### Sylvester audit

| Smell | Rating | Note |
|-------|--------|------|
| Many interactions | Strong | AP gates every prep action; synergy refund creates positive-feedback loop with build commitment; proc-order interacts with modifier composition. |
| Simplicity | Acceptable | Three concepts (AP, synergy refund, proc-order). Slightly higher than Option X. |
| Multiple uses | Strong | Same AP can buy, apply, reorder, reroll. |
| Non-overlapping roles | Strong | AP = budget axis; proc-order = sequencing axis; synergy refund = feedback layer. All orthogonal. |
| Reuses conventions | Acceptable | AP is XCOM/Into-the-Breach standard. Proc-order is less common but trivially learned. |
| Similar scale | Strong | AP 3-7, costs 1-3. Aligned. |
| High reuse | Strong | AP every prep, proc-order every modifier acquisition. |
| Content restrictions | Acceptable | Items must be balanced under "buy AP cost ≠ apply AP cost." Doubles the per-item balance work but enables interesting design ("expensive to acquire, cheap to use" vs reverse). |

#### Pros / Cons

| Pros | Cons |
|------|------|
| **The unequipped promise** — buying something you can't afford to apply this round creates a beat no commercial autobattler currently produces | Higher UI cost (per-ball proc-order lists) |
| Synergy-as-AP-refund makes synergies *economically* meaningful, not just combat-meaningful — feedback loop the genre lacks | AP discrete-ness can feel rigid ("why does my big spend cost the same as a reroll?") |
| Proc-order is genuine added depth at minimal UI cost (drag-reorder a small list) | Three concepts vs Option X's two — slightly higher FTUE wall |
| Most differentiated from competitor autobattlers | "AP budget" is more mechanical / less satisfying than "I have a pile of gold" — possible motivation/dopamine cost (Ch. 8) |
| Synergies create a *legible* mid-match incentive: "I'm committing to Volley because it pays me back" | Items must be priced in two axes (buy AP, apply AP) — more balance work |

#### Unique emotional event

**"The unequipped promise."** Buying a Rare Detonate (2 AP) when you have 1 AP left means the modifier sits in inventory unequipped until next round — visible on screen, taunting. Next round's first action is always "apply that thing I bought last time." That's a cross-round emotional thread no other option creates.

**Secondary event: "The synergy compounding."** Triggering Volley in R2 → +1 AP in R3 → AP cushion → more reorder freedom → tighter Volley in R3 → +1 AP into R4. The build pays for itself.

#### Production cost

- Implementation: ~5-7 days. AP system, refund hook in battle resolution, proc-order data model on each ball, drag-reorder UI.
- UI: Per-ball proc-order list (visible during prep, not battle). Inventory unequipped state. AP counter prominent.
- Balance: AP scaling 3-7, AP costs per action (~6 actions to balance), synergy refund rate, proc-order rules per modifier.

---

### 3.3 Option Z — Snap-shape with proc-order (B + S + modifier-order)

#### Mechanic specification

This is Option X (B + S) plus a single additional mechanic: **modifier proc-order on each ball matters**, and reordering is something the player does during prep (no Charge cost, just a click-drag in the prep UI).

The Charge model stays exactly as Option X. The shop preview stays exactly as Option X. The only addition is that modifiers on a ball have an **order list**, and order determines trigger sequence in battle.

#### Sample R3 prep walkthrough

Same fiction. Identical first half to Option X — player has 9 Charge, buys Detonate (4) and Bow (3) using 7 of 9 Charge, 2 lost.

- **NEW decision before locking in:** The Reaver now has two modifiers: a pre-existing Speed-up (acquired R2) and the newly-applied Detonate. Which triggers first?
  - **Speed-up → Detonate:** Reaver moves faster, gets into melee range faster, takes more damage before Detonate threshold (20% HP) is reached. Possibly bigger explosion.
  - **Detonate → Speed-up:** Reaver is slower, but its Detonate timing is delayed (so the explosion lands later in battle, after enemy positioning has clustered). Different combo potential.
- **Player drags Detonate above Speed-up in the proc-order list.** Detonate now triggers first.
- **Time taken:** ~22 seconds (X's 18 + a few seconds for proc-order).

#### Sylvester audit

| Smell | Rating | Note |
|-------|--------|------|
| Many interactions | Strong | Adds one more axis of interaction (proc-order × modifier kind × ball movement type) on top of Option X's already-strong interaction count. |
| Simplicity | Strong | Three concepts total. Proc-order is the most intuitive of any added concept — players intuitively understand "first in list goes first." |
| Multiple uses | Strong | Order matters strategically; reorder cost is zero (no AP/Charge), so it's a pure puzzle decision. |
| Non-overlapping roles | Strong | B = temporal; S = informational; proc-order = sequential. All orthogonal. |
| Reuses conventions | Strong | Order-list UI is standard from Slay the Spire, Inscryption, etc. |
| Similar scale | Strong | Inherits Option X's clean numerics. |
| High reuse | Strong | Every modifier acquisition triggers a reorder decision. |
| Content restrictions | Acceptable | Each modifier needs documented proc-order semantics ("triggers on 20% HP threshold"). Already required by the game's design. |

#### Pros / Cons

| Pros | Cons |
|------|------|
| Adds genuine puzzle depth on top of Option X without adding a currency or budget axis | Three concepts vs Option X's two — slightly higher FTUE |
| Proc-order requires no number-crunching; it's a pure "does X before Y feel better than Y before X" check | The proc-order decision can be ignored by new players without losing the game — risks being a "depth-only" mechanic that doesn't add accessibility |
| Naturally surfaces emergent synergies (Detonate-then-Split vs Split-then-Detonate are functionally different builds) | Adds load to battle visual clarity — players must *see* which modifier triggered first to learn from outcomes |
| Lowest production cost addition vs Option X | If proc-order doesn't visibly matter to outcomes, the mechanic feels fake |
| The same prep can be a 18s decision for casual players (skip reorder) or a 25s decision for theorycrafters (optimize reorder) — elastic |

#### Unique emotional event

**"The order discovery."** A player who never thinks about proc-order plays for 5 matches and notices their Detonate-first builds win more than their Split-first builds. They realize the order matters — *they discover the mechanic themselves through play*. This is exactly the emergent-discovery emotional event Sylvester praises in Ch. 1 (the *learning* trigger): a player making the epiphany themselves is more powerful than a tutorial teaching it.

#### Production cost

- Implementation: ~4 days = Option X's 3 days + ~1 day for proc-order data model and reorder UI.
- UI: Drag-reorder list per ball in prep UI. Visible only when ball has 2+ modifiers. Doesn't dominate screen.
- Balance: Inherit Option X. Plus per-modifier proc-order semantics need documentation (already required for combat anyway).
- Battle visual: Modifiers triggering in battle should have clear order-aware VFX (already part of the Earclacks-inspired battle phase).

---

## 4. Honorable mention — E (rerolls-as-currency)

The user explicitly raised "rerolls as the currency" as a creative direction. The exploration concluded it has a dominant-strategy hazard but is recoverable with the right pairing.

**The straight version** — "each buy costs N rerolls" — is gold by another name. The interesting version: **each reroll on a slot reduces its price by 1, capped at price 1.** This makes prep into a literal bargain-hunt: reroll the slot you want twice to chip the price, then buy.

**The dominant strategy hazard:** without counter-pressure, players reroll every slot to price 1 every round. Solutions:

- **Counter-pressure A:** rerolls themselves cost Charge (or AP). This pairs naturally with Option X — B's expiring Charge caps total rerolls per round.
- **Counter-pressure B:** rerolls also *change the item* in the slot (not just reduce price). So "reroll-to-1" risks rerolling away from what you wanted.
- **Counter-pressure C:** each reroll fills the slot with a *cheaper but lower-rarity* item. Price reduction comes from rarity reduction — economically neutral.

**Verdict:** E is a *secondary mechanic*, not a primary one. If we pursue Option X or Z, E could be layered as a future expansion. As the primary economy, it's structurally fragile.

---

## 5. Why Model A (bank + interest) was retired

The first draft of `auto-battler-v1.md` specified Model A: single currency with carryover, interest at +1 per 5 saved capped at +3, win/loss bonuses, streaks. The subagent's runtime argument convinced me to retire it.

**The argument:** TFT's interest mechanic works because TFT runs 25-30 rounds. Saving early to compound interest pays off over many rounds. In a 5-round Bo5, interest can only accrue 4 times. The maximum lifetime interest is +12 Charge. That's significant but *not strategically rich* — the decision degenerates to "save in R1 or don't."

**The counter-argument** (which I explored): make interest binary. A single threshold check at R2 pays a one-time +5 if hit. This reduces the math overhead to one decision per match. **But** — the decision is over after R2, leaving R3-R5 with no interest dynamics. Three rounds of dead mechanic for one round of strategic depth.

**Conclusion:** Model A is structurally underpowered for the 5-round runtime. Its goal (save-vs-spend tension) is real, but it's not achievable in this match length. Option Y's synergy refund and Option Z's proc-order are stronger expressions of strategic depth that *do* fit the runtime.

A small caveat: if the design later expands to **best-of-7** or longer matches, Model A becomes viable. It's not a wrong mechanic — it's wrong *for this runtime*.

---

## 6. Decision criteria — which option matches which team profile

| If the team wants... | The right option is... | Because |
|----------------------|-----------------------|---------|
| Minimum-viable prototype, fastest path to playtest | **Option X (B + S)** | 2 concepts, ~3 days to build, Snap-tested patterns |
| The strongest unique emotional event vs competitors | **Option Y (H + R)** | "The unequipped promise" and "synergy compounding" are autobattler-firsts |
| Maximum puzzle depth per FTUE concept | **Option Z (B + S + proc-order)** | Proc-order adds depth without a new currency |
| To match the GDD's original "save-vs-spend tension" goal | **Reconsider scope** | None of the three options fully delivers this; only longer match formats can |
| Async-PvP that *feels* asynchronous (not pretend-shared-pool) | Any of X / Y / Z | All three respect async; none requires a shared resource pool |
| Lowest balance burden | **Option X** | Single Charge axis, ~10 numbers to tune |
| Most flex-room for post-launch content | **Option Y** | AP system absorbs new action types cleanly; rarity-as-cost is content-neutral |

---

## 7. Prototype recommendation

If forced to commit to one prototype order:

1. **Prototype Option X first.** Cheapest, lowest risk, fastest signal. If X feels good in a graybox, the game is shippable on that economy. If X feels *flat*, escalate to Z.
2. **Prototype Option Z (X + proc-order) second.** Adds one mechanic on top of X. Tests whether the added depth justifies one more FTUE concept.
3. **Prototype Option Y (H+R) third, as the differentiated alternative.** If the team has strong opinions that the autobattler genre is saturated and a unique economy is needed for market positioning, Y is the bet.

**My personal lean:** Option Z. Option X is *probably right* but Option Z is *probably right with one more puzzle layer at minimal cost*. If proc-order doesn't work in playtest, fall back to X. If it does, the game has a distinctive prep phase.

But this is genuinely an open question. If you've read all three options and Option Y's "unequipped promise" is the emotional event that resonates most with the game's *fiction* (ball charge, kinetic energy stored across rounds), Y is the right choice and X/Z are the safe fallbacks. The fiction-mechanics fit might be the deciding factor.

---

## 8. Open questions (genuinely unresolved)

These cannot be answered from analysis alone:

- **Does per-round Charge expiration (B) frustrate players who finish prep with leftover Charge?** Playtest signal: surveys + early-quit rates.
- **Does the next-round preview (S) actually change current-round decisions, or is it visual noise?** Playtest signal: telemetry on shop decisions correlated with preview content.
- **Does proc-order (Z's added mechanic) feel discoverable, or do players need to be told it matters?** Playtest signal: % of players reordering modifiers in their first 5 matches.
- **Does AP feel like "budget" or "constraint"?** (Option Y) The emotional valence of these feels different — budgets feel empowering, constraints feel restrictive. Playtest signal: post-match survey on feelings.
- **Is the loss-bonus magnitude right?** All three options use a +2 loss bonus; this is unplaytested.
- **Does any option fail with new players?** The 3 personas in the main GDD (Theory / Climber / Vibe) need to be tested against each economy.

---

## 9. What this doc did *not* settle

- The numerical tuning of each option's parameters (Charge values, AP scaling, refund rates) — first-pass numbers are provided but require playtest.
- The shop content composition under each option (covered in main GDD).
- Cross-match progression and how Charge/AP interacts with the 10-win climb (covered in main GDD).
- Cosmetic/monetization layer on top of the economy.

The deliverable here is the **three viable shapes** and the **decision criteria** for choosing among them. The team picks one (or commissions a graybox A/B test between Options X and Z), then the chosen option gets fully specified in a future revision of the main GDD.

---

*This document represents the second pass on the economy. The first pass picked a single answer without surveying the space. This one surveyed the space and refused to pick. The team's choice between X, Y, and Z is genuinely a design-stance question, not a correctness question.*
