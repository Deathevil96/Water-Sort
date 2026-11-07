# PAINT SORT
## Game Design Document & Claude Code Implementation Specification

**Document type:** Game Design Document (GDD) + Technical Implementation Brief  
**Project:** Paint Sort  
**Target:** Mobile-first casual sorting puzzle; desktop support for development and testing  
**Engine assumption:** Unity / C# unless the existing repository clearly uses a different engine  
**Initial content target:** at least 1,000 playable, validated levels; target 1,200  
**Reference game:** *Magic Sort!* by Grand Games A.Ş. (`com.grandgames.magicsort`)  
**Document status:** Initial production specification — configurable values are identified below

---

# 0. Instructions for Claude Code

You are acting as the lead Unity gameplay engineer, puzzle-system designer, mobile-game engineer, and technical producer for **Paint Sort**.

Your job is to implement a complete, playable game from this specification. Do not stop after producing a plan, mock-up, or design document.

## 0.1 Non-negotiable priorities

1. Preserve the familiar water-sort puzzle rules: the player moves the top colour group from one container to another, subject to colour and capacity restrictions.
2. Change the theme to paint and artist materials. Do not copy Magic Sort's branding, artwork, source code, level layouts, character designs, sounds, or exact UI assets.
3. Deliver at least 1,000 actual playable level definitions; target 1,200 for the initial catalog. Do not create empty placeholders or a level counter that generates arbitrary boards at runtime.
4. Every level must be verified as solvable. Hard and Super Hard levels must not be deliberately impossible or require a real-money purchase to progress.
5. Keep puzzle logic independent of rendering, animation, ads, gold, lives, and UI.
6. All economy values, booster prices, ad limits, difficulty settings, and reward schedules must be configurable without rewriting gameplay code.
7. Implement a development-only debug menu and an automated validator that can run through all launch-catalog levels (target 1,200).
8. Build systems behind interfaces so ads, in-app purchases, leaderboards, and cloud services can be enabled later without rewriting the game.
9. Do not report a feature as complete unless it has been implemented and tested.
10. Do not silently invent a third-party service, MCP server, SDK, API key, product ID, or backend. Inspect what is actually configured first.

## 0.2 First action: inspect the repository

Before editing files:

- Inspect the repository structure, project files, Unity version, packages, scenes, scripts, input system, rendering pipeline, and existing conventions.
- Identify whether this is a fresh Unity project or an existing game project.
- Inspect available Claude Code MCP integrations and identify which tools are genuinely available. Do not assume a Unity MCP, asset MCP, ad MCP, or store MCP exists.
- Check Git status and do not destroy uncommitted work.
- Identify the current build target and make Android portrait the initial mobile target unless the existing project specifies otherwise.
- Produce a concise implementation plan and then start implementing it.

If a project already exists, preserve its working architecture where reasonable. If it is empty, create a clean and maintainable Unity project structure.

## 0.3 How to use MCP tools

Use MCP tools only after discovering their actual names, capabilities, permissions, and argument requirements.

Good uses when a compatible MCP is available:

- Inspecting or controlling the Unity Editor.
- Creating or editing scenes and GameObjects.
- Reading Console errors and running Play Mode tests.
- Inspecting project assets and importing generated art.
- Running browser-based checks for a development build.

Do not make core gameplay correctness dependent on an MCP server. The C# source code, deterministic level data, automated tests, and normal build pipeline must remain usable without MCP.

Do not call an MCP tool with guessed arguments. If an MCP is unavailable, use normal project files, scripts, Unity Editor workflows, and command-line tests where supported.

---

# 1. Product Overview

## 1.1 Working title

**Paint Sort**

The name is a working title. Keep the displayed game title, package name, store description, and branding configurable so the name can change before publishing.

## 1.2 One-line concept

A relaxing, polished paint-sorting puzzle where players pour stacked colours between transparent artist containers until every colour is sorted into its own container.

## 1.3 Core promise

- Instantly understandable controls.
- Satisfying paint movement and pouring.
- Puzzles that gradually become more complex.
- Clear progress through at least 1,000 levels (target 1,200 at launch).
- Helpful tools when a player gets stuck.
- A gold economy, lives, weekly login rewards, achievements, and a leaderboard unlocked at Level 100.
- No timer in the standard puzzle mode.

## 1.4 What must stay the same

The core puzzle is still a colour-sorting game. The player must not be asked to paint pictures, mix colours, match three items, decorate a room, or complete a separate simulation task.

Paint is a visual theme, not a different puzzle mechanic.

## 1.5 What makes Paint Sort distinct

- Artist studio / paint workshop visual theme.
- Transparent acrylic paint-mixing containers rather than familiar test tubes or ordinary potion bottles.
- Thick, glossy paint surfaces and a satisfying stream when pouring.
- Paint-themed UI, sounds, container collections, and background environments.
- The user's requested progression, gold, lives, daily rewards, achievements, and debug systems.

---

# 2. Container / Vessel Design — Do Not Use Test Tubes or Ordinary Glass Bottles

The player must be able to clearly see stacked paint layers. Therefore, containers need a transparent or translucent body. Opaque paint tubes are not suitable as the main puzzle container because they hide the layer arrangement.

## 2.1 Vessel options

| Option | Visual identity | Gameplay readability | Recommendation |
|---|---|---:|---|
| **Clear acrylic paint-mixing pots** | Short, wide artist mixing pots with a narrow pouring lip | Excellent | **Recommended default** |
| Transparent paint-mixing cups | Tapered cups used by artists | Excellent | Strong alternative |
| Clear pigment jars | Small wide-mouth jars with rounded shoulders | Very good | Good cosmetic variant |
| Translucent squeeze-paint containers | Soft-sided artist squeeze containers | Good if the body remains clear | Good later skin |
| Clear ink / dye reservoirs | Compact rectangular or rounded reservoirs | Very good | Good modern/minimal variant |
| Palette wells | Small shallow wells in a palette | Poor for stacked vertical layers | Do not use as the main container |
| Opaque paint tubes | Familiar artist paint tubes | Poor; layers are hidden | Do not use as the main container |

## 2.2 Chosen default vessel

Use a **clear acrylic paint-mixing pot** as the default puzzle container.

Design characteristics:

- Not shaped like a laboratory test tube.
- Not shaped like a conventional narrow-neck potion bottle.
- Slightly wider body with gently tapered sides.
- Rounded-square or softly cylindrical silhouette.
- Transparent acrylic walls with subtle edge highlights.
- A small pouring lip or spout so a tilt animation reads clearly.
- Rounded base and enough interior space to show distinct paint layers.
- A small optional artist-style rim or grip detail, kept away from the liquid area.
- Clear visual separation between the paint and the container edges.

The container should have a distinctive silhouette even when displayed small on a phone.

## 2.3 Cosmetic container variants

Support skins without altering capacity or rules:

- Classic acrylic mixing pot.
- Rounded-square mixing cup.
- Mini pigment jar.
- Tall studio reservoir.
- Wide artist mixing cup.
- Premium translucent squeeze pot.

Cosmetic skins must never change gameplay capacity, pour rules, hitboxes, or difficulty.

---

# 3. Reference Research: Magic Sort!

The reference for this project is **Magic Sort! by Grand Games A.Ş.**, Google Play package `com.grandgames.magicsort`, not other unrelated games with similar names.

## 3.1 Publicly documented core features

The official Google Play and Apple App Store descriptions identify the following:

- Tap one bottle/container to select it and another to pour.
- Pour only when the destination's top colour matches or the destination is empty, and there is enough room.
- Sort the colours so that each container holds one colour.
- Undo to reverse moves.
- Shuffle to rearrange the puzzle.
- Increasingly challenging puzzle stages.
- Magical tools / boosts.
- A cauldron-based boost description.
- Collectible ingredients used to brew stronger magic.
- Ads and in-app purchases are listed by the stores.

The exact internal implementation of its boosts, ingredient recipes, economy, and level-generation system is not disclosed in the public store description.

## 3.2 Additional mechanics reported or visible in public gameplay material

The following are reported by walkthroughs, gameplay guides, store screenshots, and player reports. They are useful reference material, but should not all be treated as an official developer specification.

| Reference feature | Publicly reported / observed behaviour | Paint Sort decision |
|---|---|---|
| Hidden `?` layers | A layer is displayed dark with a question mark until the layers above it are removed | Implement from Level 21 |
| Covered containers | A container or group of containers is hidden behind cloth / a covering | Implement from Level 31; unlock after completing a colour container |
| Curtains | A section of the board is hidden behind curtains | Architecture should allow it later; not required in first release |
| Keys and locks | Clearing or exposing a key can unlock a locked object or area | Future obstacle candidate |
| Locked crates / boxes | Objects with coloured locks are shown in some gameplay material | Future obstacle candidate |
| Extra container / `+1` bottle | An additional empty container can be added as a rescue tool | Implement as a paid gold power-up |
| Tall containers | Containers can hold more layers than the standard layout | Support as a later level variant |
| Small numbered containers | Smaller containers with a number are shown in reference artwork; exact meaning is not clearly documented | Do not implement until the rule is verified and independently designed |
| Hard / Super Hard labels | Special difficulty labels appear on selected levels | Implement the exact Paint Sort schedule in this document |
| Coins / gold-like currency | Players use currency for tools and rescue options | Implement Paint Sort gold economy |
| Lives | Player reports describe a five-life cap and timed refill | Implement five lives, 30-minute refill per life |
| Events / rewards / streaks | Guides and player reports mention events, streak rewards, and timed bonuses | Keep interfaces extensible; not required for first launch |
| Teams / life requests | Player reports mention teams and requesting lives | Future social feature; not required for first launch |
| Daily puzzle | Reported in reference-game guides | Future feature; distinct from Paint Sort's daily login rewards |
| Rewarded ads | Player reports mention ad-based rewards for coins, lives, or an extra container | Implement opt-in reward ads with a five-minute ad opportunity limit |
| Banner / interstitial ads | Public listings say the game contains ads; third-party guides report banner and interstitial placements | Implement the Paint Sort ad policy in Section 11 |
| Cauldron / ingredients | Official store copy describes boosts and ingredients but does not specify exact recipes or tuning | Do not copy the fantasy system; Paint Sort can add its own boost crafting later |

## 3.3 Important research limitation

There is no complete public developer document explaining every Magic Sort level, obstacle trigger, booster formula, economy value, or server-side rule. Some mechanics are documented only by fan guides or player reports and may vary by app version or platform.

Claude must use this research as a feature reference, not claim that every reported mechanic is guaranteed to exist in every current Magic Sort build.

## 3.4 Originality requirement

Use the same broad puzzle genre and familiar sort rules, but create original:

- Paint Sort branding and logo.
- Paint containers and visual language.
- UI layout and iconography.
- Paint materials, effects, sound design, and backgrounds.
- Level layouts and level data.
- Achievement names and progression presentation.

Do not extract, copy, or reuse Magic Sort's proprietary assets, character art, level files, code, sounds, or exact screen layouts.

---

# 4. Core Puzzle Rules

This section is the source of truth for gameplay. Implement it as pure game logic, independent of Unity scene objects and visual effects.

## 4.1 Basic level structure

- A level contains multiple paint-mixing containers.
- Each container has a fixed capacity.
- The standard capacity is **4 paint units**.
- A paint unit has one colour ID.
- Container layers are stored from **bottom to top**.
- A standard colour is represented by exactly four units across the whole board.
- The player must consolidate each colour into one full container.
- Extra empty containers provide temporary working space.

## 4.2 Selecting a source

When the player taps a container:

- If no source is selected, select it only if it contains paint and is accessible.
- Raise it slightly and show a clear selected state.
- If the player taps the selected container again, deselect it.
- If the player taps a different non-empty container, change the source selection where appropriate.
- Covered / locked containers cannot be selected.
- Completed containers are marked complete and locked against further moves by default. Keep this behaviour configurable in case reference testing shows a different requirement.

## 4.3 Selecting a destination

A pour is valid only when all of the following are true:

1. Source and destination are different containers.
2. Source contains at least one paint unit.
3. Source is accessible and is not locked as complete.
4. Destination is accessible and is not locked as complete.
5. Destination has at least one free unit of capacity.
6. Destination is empty, or its topmost visible paint colour matches the source's topmost paint colour.

Invalid moves must not alter the puzzle state, gold, lives, move count, or undo history.

## 4.4 Contiguous top-colour group

Only the contiguous group of matching paint at the top of the source can move.

Example, stored bottom-to-top:

```text
Source container
Top
Blue  <- top unit
Blue
Blue
Red
Bottom
```

The top group is three Blue units. If the destination has room for all three, all three move in one pour. If the destination has room for only two, move two and leave one Blue unit in the source.

Never move a colour from beneath a different colour.

## 4.5 Destination matching

- An empty destination accepts any top colour.
- A non-empty destination accepts paint only when its top colour matches the source's top colour.
- Paint never mixes into a different colour.
- A destination can never exceed its configured capacity.

## 4.6 Pour amount

```text
sourceTopGroupSize = number of contiguous top units matching source top colour
destinationFreeSpace = destination.capacity - destination.layerCount
pourAmount = min(sourceTopGroupSize, destinationFreeSpace)
```

A valid partial pour is allowed when destination capacity is smaller than the entire source top group.

## 4.7 Completed container

A standard container is complete when:

- It contains exactly its capacity in units; and
- Every unit has the same colour ID.

A completed container:

- Receives a clear completed visual state.
- Plays a small success effect.
- Becomes locked against additional moves by default.
- Can trigger the covered-container unlock rule.

Do not treat an empty container as a completed colour container.

## 4.8 Win condition

The level is won when every non-empty container is complete and all paint units are accounted for. Empty containers may remain empty.

Do not require the player to fill the spare containers.

## 4.9 No timer

The standard mode has no countdown timer and no time-based move penalty.

## 4.10 Undo

Undo reverses the last valid pour exactly.

It must restore:

- Source layers.
- Destination layers.
- Hidden-layer visibility state.
- Container completion state.
- Covered-container unlock state, if the move triggered one.
- Move count.
- Any other puzzle-state property changed by that move.

Undo must use a complete state snapshot or a reliable reversible command. Do not try to reconstruct the old state from visual objects.

Invalid moves are not added to history.

**Initial pricing decision:** Undo is free and can be used while valid history exists. Keep its cost configurable for later economy experiments.

## 4.11 Restart

Restart restores the exact original level state and resets:

- Move count.
- Undo history.
- Current selection.
- Active pour animation.
- Completed-container flags.
- Hidden-layer visibility.
- Covered-container unlock progress.

A restart chosen as an abandonment of a failed attempt follows the life-consumption flow in Section 10. Do not consume a life just because an animation or app scene is reloaded.

---

# 5. Paint Sort Obstacles and Special Mechanics

## 5.1 Hidden paint layers — unlock from Level 21

The first hidden-colour level must be **Level 21**.

Rules:

- Some paint units have a hidden-colour flag.
- When hidden, render the unit as dark charcoal / nearly black with a clear `?` symbol.
- The true colour ID remains stored in the puzzle state.
- The hidden layer is not selectable while another unit is above it.
- When all units above it are moved away and it becomes the top unit, reveal its true colour before the next player action.
- The player must see the true colour once it becomes the top layer.
- A hidden unit must still obey normal colour-matching and capacity rules.
- Hidden status is visual information, not a different colour and not a wildcard.
- Multiple hidden units in one container may reveal one at a time.

Progressive introduction:

- Level 21: tutorial level with one hidden layer.
- Levels 22–30: one or two hidden layers in controlled positions.
- Later levels: allow multiple hidden layers and combinations with other obstacles.

Do not introduce hidden layers before Level 21.

## 5.2 Covered containers — unlock from Level 31

The first covered-container level must be **Level 31**.

Rules:

- A covered container is visually blocked by a paint-studio cover, canvas wrap, or opaque artist cloth.
- The player cannot select it or pour into it while covered.
- Its contents are not visible while covered.
- The true contents remain in level data and are used by the solver.
- When the player completes one full single-colour container, unlock one covered container.
- Each completed colour container unlocks one covered container until all covers are removed.
- A container is considered unlocked only after the completion event is committed to puzzle state.
- The reveal animation must not change the paint contents.

Progression:

- Levels 31–60: up to 1 covered container.
- Levels 61–150: 1–2 covered containers.
- Levels 151–1000: up to 3 covered containers, but only when the solver confirms the board is fair and solvable.

A covered container may contain paint or be empty, but level design must account for its hidden capacity and contents. Do not use covered containers to secretly make an impossible level.

## 5.3 Future obstacle architecture

Design the level data so more obstacles can be added later without rewriting the core pour logic. Possible future Paint Sort obstacles include:

- Key-colour / lock-colour gates.
- Curtain-covered groups of containers.
- Locked crates or storage cases.
- Corked or capped containers.
- Frozen containers that require a defined unlock event.
- Long-capacity artist reservoirs.
- Numbered mini-pots with a clearly designed, documented rule.

These are **not launch requirements**. Do not implement ambiguous obstacle behaviour just because it appears in reference-game artwork.

---

# 6. Level Progression and Difficulty

## 6.1 Initial level count

Create **at least 1,000 playable levels for the initial release; target 1,200** so the launch catalog is safely above the requested threshold.

Requirements:

- Level IDs are stable and sequential from 1 to the configured launch catalog count (target 1,200; never fewer than 1,000).
- Level data is deterministic and saved as project content (for example JSON, ScriptableObjects, or an equivalent version-controlled data asset).
- The same level number must not randomly change layout between app launches.
- Restart must restore the same original board for that level.
- The initial level pack must contain actual generated and validated boards, not placeholders. Levels 1001–1200 should continue the advanced progression curve in the final table row.
- The catalog architecture must support adding Levels 1001+ in future updates without rewriting progress data.

## 6.2 Difficulty labels — exact requested cadence

Apply these labels using the following precedence:

- **Super Hard:** every level divisible by 10 (10, 20, 30, 40, ... 1000).
- **Hard:** every level divisible by 5 but not by 10 (5, 15, 25, 35, ... 995).
- **Normal:** all other levels.

A Super Hard level is never also labelled Hard.

This produces a Hard level every five levels and a Super Hard level every ten levels.

## 6.3 Difficulty must come from puzzle design

Do not make a level difficult only by adding random colours. Difficulty can increase through a controlled combination of:

- More colours.
- More containers.
- Fewer empty containers.
- More fragmented colour groups.
- Deeper buried layers.
- Hidden `?` layers.
- Covered containers.
- More complex dependencies between completed containers and unlocks.
- Carefully constrained use of available space.

A higher level number does not automatically mean the level should be unfair or require spending gold.

## 6.4 Initial progression guide

Use the following as a configurable starting curve. The solver and playtesting may require adjustments, but the obstacle unlock levels and Hard/Super Hard cadence must remain exact.

| Level range | Typical colours | Total containers | Typical empty containers | Features |
|---|---:|---:|---:|---|
| 1–5 | 3 | 5 | 2 | Tutorial / basic sorting |
| 6–10 | 4 | 6 | 2 | First Hard and Super Hard labels |
| 11–20 | 4–5 | 6–7 | 1–2 | More mixed layers; introduce Undo and Shuffle prompts |
| 21–30 | 4–5 | 6–7 | 1–2 | Hidden `?` layers begin |
| 31–50 | 5–6 | 7–8 | 1–2 | Covered containers begin |
| 51–100 | 5–7 | 7–9 | 1–2 | More hidden layers; occasional 1–2 covers |
| 101–200 | 6–7 | 8–9 | 1–2 | More complex cover / reveal dependencies |
| 201–350 | 7–8 | 9–10 | 1–2 | Denser boards; longer planning sequences |
| 351–500 | 8–9 | 10–11 | 1–2 | More complex obstacle combinations |
| 501–750 | 9–10 | 11–12 | 1–2 | Advanced layouts with careful visual scaling |
| 751–1200 | 10–11 | 12–13 | 1–2 | Expert layouts; up to 3 covers where validated |

The number of containers must grow as the number of colours grows. Do not keep the game at four containers throughout.

The table is a target curve, not a rigid formula. If a level with a proposed count is not readable or solvable, adjust its individual data while keeping the difficulty label and unlock milestones intact.

## 6.5 Tutorial plan

- Level 1: select a container and pour into an empty one.
- Level 2: matching-colour destination.
- Level 3: contiguous top-colour group.
- Level 4: destination capacity and partial pour.
- Level 5: first Hard-labelled level; still teachable and fair.
- Level 6 onward: normal play without repeated blocking tutorials.
- First Undo use: short contextual explanation.
- First Shuffle use: explain the gold cost and effect.
- Level 21: explain hidden layers.
- Level 31: explain covered containers and unlock condition.
- Level 100: explain leaderboard access.

Tutorials should be dismissible after the mechanic is understood and must not repeat on every level.

---

# 7. Level Generation, Solvability, and Difficulty Validation

This is one of the most important technical sections.

## 7.1 No arbitrary random levels

Never create a level by randomly placing paint units and assuming that it will be solvable.

Do not generate new random layouts whenever a player opens a level.

Generate a deterministic, validated level catalog during development and include the resulting data in the build.

## 7.2 Recommended generation approach

Use a seeded, reproducible generator.

A recommended approach for standard levels:

1. Construct a solved board: one full container per colour, plus the required empty containers.
2. Apply a controlled sequence of legal paint transfers to scramble the board.
3. Record the inverse sequence as a known witness solution.
4. Reject trivial scrambles, immediately solved boards, and boards that begin with accidental complete containers unless intentionally designed.
5. Run the actual rule validator on the generated board.
6. Replay the witness solution using the same gameplay rules.
7. Confirm that the replay reaches the win condition.
8. Run a solver/search check for solution quality and difficulty where computationally practical.
9. Save the level, seed, metadata, and witness solution.

A reverse-scramble witness is a strong initial solvability guarantee, but special mechanics such as locked/covered containers must also be simulated in the correct order. Do not assume that a standard-level witness remains valid after adding an obstacle.

## 7.3 Special-level validation

For levels with hidden layers:

- Confirm that each hidden colour is revealed at the correct time.
- Confirm that the player can still solve the level after each reveal.
- Confirm that the hidden colour is not accidentally treated as a wildcard.

For levels with covered containers:

- Confirm that the required completed-container event is reachable.
- Confirm that each completion unlocks the correct number of covers.
- Confirm that no required move depends on using a covered container before it is unlocked.
- Confirm that the revealed board remains solvable.

## 7.4 Difficulty validation

Track at least:

- Minimum solution length found.
- Witness solution length.
- Number of containers.
- Number of colours.
- Number of hidden units.
- Number of covered containers.
- Number of free containers.
- Maximum consecutive dependency depth.
- Number of valid moves at the initial state.
- Whether a booster is required by the generated solution.

Hard and Super Hard levels should be meaningfully more demanding, not merely labelled as such.

## 7.5 Fairness requirement

A level may be difficult, but it must not be intentionally impossible to force an ad, gold purchase, or real-money purchase.

Preferred rule: every level is solvable using ordinary legal pours without spending a booster. Boosters are optional rescue tools.

If a special level is designed to require a booster, the all-level validator must prove that the booster path is available through a guaranteed free route (earned gold or a permitted rewarded-ad rescue), and the level must never require an in-app purchase.

Do not introduce hidden randomness that can make a previously validated level unwinnable.

---

# 8. Gold Economy

Gold is the soft currency used to buy puzzle assistance and, later, to access gold packs in the shop.

All values must come from a central EconomyConfig asset or equivalent configuration. Never scatter gold amounts through UI scripts.

## 8.1 Starting gold

**Initial wallet grant: 500 Gold.**

This is a proposed starting value to make the early game generous and is configurable for balancing.

Grant it once per new player profile. Do not grant it again on normal app restarts, level restarts, or save reloads.

## 8.2 Level completion reward

- Award **20 Gold for every successfully completed level**.
- Grant the reward exactly once per level completion event.
- Replaying a completed level must not repeatedly grant the first-clear reward unless a separate replay reward is explicitly configured.
- Do not grant the reward before the level is actually won.
- Persist the transaction so closing the app during the completion animation cannot grant it twice or lose it.

## 8.3 Milestone bonus

Award additional gold at every 10-level milestone.

| Milestone | Bonus |
|---|---:|
| Levels 10, 20, 30, 40, 50 | +50 Gold |
| Levels 60, 70, 80, 90, 100, and every later 10-level milestone | +20 Gold |

The player also receives the normal 20 Gold level reward. The milestone bonus is additional.

Examples:

- Complete Level 9: +20 Gold.
- Complete Level 10: +20 level reward +50 milestone bonus.
- Complete Level 50: +20 level reward +50 milestone bonus.
- Complete Level 60: +20 level reward +20 milestone bonus.

The transition from the early bonus to the later bonus must be a configuration value. Default transition: after Level 50.

## 8.4 Double-gold option

After a level win, offer:

- **Collect:** receive the normal reward.
- **Watch Ad — 2× Gold:** double the reward for that completed level.

The optional double reward applies to the current level reward and its milestone bonus, if present.

Examples:

- Normal Level 12: 20 Gold. Double option: 40 Gold.
- Normal Level 10: 20 + 50 = 70 Gold. Double option: 140 Gold.

Rules:

- One successful rewarded ad can double one level's payout only once.
- The player can always collect the normal reward without watching an ad.
- If the rewarded ad fails to load or complete, grant only the normal reward.
- Do not show an endless loop of double-reward ads.
- Store a unique reward-claim ID to prevent duplicate payouts.

## 8.5 Gold pricing for power-ups

Initial prices:

| Power-up | Gold price | Behaviour |
|---|---:|---|
| Undo | Free | Reverses the last valid pour |
| Targeted Shuffle | **900** | Player selects which eligible containers are included in the shuffle |
| Add One Empty Container | **900** | Adds one standard-capacity empty container to the current level |

Keep prices configurable. Do not hardcode 900 in individual buttons or scenes.

## 8.6 Targeted Shuffle behaviour

The user wants to choose which containers are affected by Shuffle.

Implement a targeted shuffle mode:

1. Player taps the Shuffle control.
2. Show a short instruction: “Select the containers to shuffle.”
3. Player taps at least two eligible, unlocked containers.
4. Selected containers receive a clear selection outline.
5. Player confirms the selection.
6. The shuffle algorithm redistributes the selected containers' paint units among the selected containers while preserving the total count of every colour and respecting each capacity.
7. The result is checked by the level-validity / solvability system before it is committed.
8. If a valid solvable rearrangement cannot be found, do not charge gold and show a clear message.
9. On success, deduct 900 Gold once and animate the rearrangement.

Do not simply randomize the containers' contents without checking capacity, colour counts, and solvability.

An optional “Shuffle All Eligible Containers” variant may be added as a separate configuration, but targeted selection is the required default.

## 8.7 Gold transaction safety

Every gold change must be recorded as a transaction with:

- Unique transaction ID.
- Amount (positive or negative).
- Reason (level reward, milestone, daily login, achievement, shuffle purchase, extra container, debug grant, IAP purchase).
- Related level ID where applicable.
- Timestamp.
- Claim / purchase ID for idempotency.

Gold must never become negative.

A player must not lose gold if a power-up fails to activate.

---

# 9. Daily Login Rewards

## 9.1 Weekly reward track

Implement the requested reward sequence:

| Weekly reward claim | Gold |
|---|---:|
| Claim 1 | 50 |
| Claim 2 | 100 |
| Claim 3 | 200 |
| Claim 4 | 300 |
| Claim 5 | 400 |
| Full-week completion bonus | **1,000** |

The five specified amounts are the five base rewards. Since no sixth or seventh base reward was provided, use Days 6 and 7 as grace / catch-up days within the same weekly cycle rather than inventing additional reward amounts.

## 9.2 Claim rules

- A player can claim at most one unclaimed reward per calendar login day.
- The five base rewards must all be claimed within the same seven-day reward week.
- The player does not have to claim on five consecutive days; the two extra days are a catch-up buffer.
- Once all five rewards are claimed in that weekly cycle, award the extra **1,000 Gold** completion bonus exactly once.
- Reset the weekly track at the configured weekly reset.
- Unclaimed rewards do not carry into the next week.
- A new weekly cycle starts with Claim 1 available.

Use server-authoritative time when a backend exists. During offline development, use a central time provider abstraction and test clock manipulation. Do not scatter device clock calculations through UI code.

The weekly reset boundary must be configurable.

## 9.3 Daily login UI

Show:

- Five reward tiles with the Gold amounts.
- Claimed / available / upcoming states.
- A weekly completion progress indicator.
- The +1,000 Gold full-track reward.
- A reset countdown.

Do not block puzzle access with a mandatory daily reward pop-up. The player may dismiss it and claim later in the same week.

---

# 10. Lives, Failure, and Rescue Flow

## 10.1 Lives system

- Maximum lives: **5**.
- Regeneration: **1 life every 30 minutes**.
- Regeneration stops at 5 lives.
- When the player is below 5, show the countdown to the next life.
- Persist the last regeneration timestamp so lives recover while the app is closed.
- Use a time provider abstraction so tests can advance time instantly.

## 10.2 What counts as a failed attempt

There is no timer and no move limit in the standard mode.

An attempt can fail when:

- The player reaches a dead-end with no legal move and no usable undo / rescue action; or
- The player chooses to abandon or restart the attempt through the failure confirmation flow.

Do not randomly declare a loss. Do not consume a life for an invalid move, app pause, animation interruption, or ordinary navigation.

## 10.3 Required two-step failure prompt

When the player is stuck and chooses to end the attempt:

**Prompt 1 — rescue / gold offer**

Show a clear offer with available options:

- Use an available power-up.
- Use 900 Gold for the relevant rescue power-up, if the wallet has enough.
- If the player does not have enough Gold, offer the Shop (when enabled) and an eligible rewarded ad rescue.
- “Not now” / “Continue without buying”.

Do not force a purchase. Do not hide the free option.

**Prompt 2 — life-loss confirmation**

If the player declines the rescue and chooses to leave the failed board, show:

> “Leave this level? This attempt will use 1 life.”

Buttons:

- “Keep Playing” — close the prompt and return to the board.
- “Leave Level” — consume exactly one life and restart / return to the level flow.

This is the second confirmation before the life is consumed. Do not repeatedly nag the player to buy Gold.

## 10.4 At zero lives

When lives are zero:

- Do not allow a new attempt to start.
- Show the time until the next life.
- Allow the player to leave the screen and return later.
- If an eligible rewarded ad is available, an optional +1 life offer may be shown.
- Do not require a real-money purchase to keep playing.

The exact behaviour of continuing a currently active board at zero lives must be consistent: a player may finish an already-started active board, but cannot start another attempt without a life.

## 10.5 Life-state testing

Debug tools must support:

- Set lives to 0–5.
- Advance time by 1 / 30 / 60 / 120 minutes.
- Set next-life timestamp.
- Test app closed / reopened regeneration.
- Test consuming a life once.
- Test that duplicate fail callbacks do not consume duplicate lives.

---

# 11. Advertising Policy

The user requests a persistent banner and an ad opportunity no more than once every five minutes.

## 11.1 Banner ad

- Reserve a permanent banner-ad region in the UI layout.
- The banner should be visible during normal play and menus whenever the ad SDK supplies an ad and the platform's consent requirements are satisfied.
- It must not overlap the puzzle board, container hitboxes, bottom controls, settings, or purchase controls.
- Use safe-area-aware layout on phones.
- In test builds, use the ad network's official test ad units only.
- If an ad fails to load, keep the reserved layout stable and do not block gameplay.

## 11.2 Five-minute ad limit

The requested limit is **no more than one non-banner ad opportunity per five minutes**.

Apply the cooldown centrally across rewarded and interstitial placements so different screens cannot accidentally show multiple ads in the same five-minute window.

- Store the last successful non-banner ad timestamp.
- Do not show an ad during a pour animation or active player input.
- Do not interrupt a level.
- Interstitial ads, if enabled, can only appear at natural breaks such as after a completed level or after leaving a level.
- Skip the interstitial if the five-minute limit has not elapsed.
- A failed ad load must not charge the player or consume a reward.

## 11.3 Rewarded ad uses

Rewarded ads are always voluntary and should be clearly labelled.

Initial rewarded placements:

1. Double the current level completion reward.
2. When the player lacks 900 Gold for a rescue power-up, watch an ad to receive one eligible free use of that rescue.
3. Optional +1 life offer at zero lives, if enabled in configuration.

Use the same five-minute non-banner ad cooldown. Do not allow a reward to be granted just because the player opened the ad; the reward is granted only after the SDK reports successful completion.

## 11.4 Ad fail states

Handle:

- No fill.
- Network unavailable.
- User closes before completion.
- SDK error.
- App goes to background during the ad.
- Reward callback arrives more than once.

Reward callbacks must be idempotent.

## 11.5 Consent and privacy

Before release, implement the relevant platform consent and privacy flows through the selected ad SDK. Do not enable personalised advertising or tracking without the required consent and platform configuration.

---

# 12. Shop and In-App Purchases

## 12.1 Timing

The Gold shop must be designed now, but actual store billing will be integrated and enabled **near closed testing**, as requested.

## 12.2 Before closed testing

Implement:

- Shop screen layout.
- Gold pack cards.
- Product configuration data structure.
- Purchase button states.
- Loading / success / failure UI.
- A purchase-service interface.
- A development-only fake purchase provider for testing UI flows.

Keep real purchases disabled behind a feature flag until the store products, SDK, and test environment are configured.

Do not pretend a fake purchase is a real purchase.

## 12.3 Suggested shop pack structure

Use configurable product definitions, not hardcoded prices. Initial pack tiers can be placeholders:

- Small Gold Pack.
- Medium Gold Pack.
- Large Gold Pack.
- Mega Gold Pack.
- Starter Gold Offer (optional, one-time).

Actual Gold quantities and prices will be balanced and configured before closed testing. Fetch platform prices from the store product catalogue; do not hardcode local currency prices into the UI.

## 12.4 Purchase safety

- Verify purchase results through the official platform billing layer.
- Grant Gold only after verified purchase success.
- Use purchase tokens / transaction IDs to prevent duplicate grants.
- Restore purchases where the platform and product type require it.
- Handle pending, cancelled, failed, and refunded purchases.
- Never grant Gold from a client-side button click alone.

---

# 13. Power-Ups

## 13.1 Undo

- Free by default.
- Reverses exactly one latest valid move per activation.
- Multiple consecutive undos are supported while history exists.
- Disabled when history is empty.

## 13.2 Targeted Shuffle — 900 Gold

- Player selects the containers to include.
- Require at least two eligible containers.
- Exclude covered, locked, and completed containers unless an explicit level rule permits them.
- Preserve total colour counts and capacity.
- The resulting board must pass validation.
- Deduct Gold only after a valid shuffle is ready to commit.
- Animate the change clearly.

The shuffle must not secretly alter the colour distribution or remove a required hidden layer.

## 13.3 Add One Empty Container — 900 Gold

- Adds one extra empty paint-mixing container of standard capacity.
- It appears in a valid board position and does not overlap any other container.
- It remains available for the rest of the current level attempt.
- It disappears when the level ends or restarts unless the level format explicitly includes it in the original state.
- If the board layout cannot fit another container, use an adaptive layout / scroll arrangement; do not silently fail.
- Deduct 900 Gold only after the new container is successfully added.

## 13.4 Power-up purchase UI

When a player selects a paid power-up:

- Show its name, effect, and price.
- Show the player's current Gold balance.
- If balance is sufficient, confirm once and execute.
- If balance is insufficient, offer Shop (when enabled), an eligible rewarded ad rescue, or Cancel.
- Never deduct Gold before confirming that the action can be completed.

---

# 14. Achievements

Implement an in-game achievement system with persistent unlock state.

Initial achievements:

| Achievement | Unlock condition | Suggested reward |
|---|---|---:|
| First Pour | Complete Level 1 | 20 Gold |
| Getting the Hang of It | Complete Level 10 | 50 Gold |
| Colour Collector | Complete Level 25 | 75 Gold |
| Paint Apprentice | Complete Level 50 | 100 Gold |
| Studio Regular | Complete Level 100 | 150 Gold |
| Master Mixer | Complete Level 250 | 250 Gold |
| Colour Expert | Complete Level 500 | 400 Gold |
| Grand Artist | Complete Level 1000 | 1,000 Gold |
| No Mistakes | Complete a level without an invalid move | 50 Gold |
| Careful Planner | Complete a level without using Undo | 50 Gold |
| Hard Work | Complete 10 Hard levels | 100 Gold |
| Super Artist | Complete 5 Super Hard levels | 150 Gold |
| Hidden Talent | Complete 10 levels with hidden layers | 100 Gold |
| Cover Breaker | Unlock 10 covered containers | 100 Gold |
| Daily Dedication | Complete one full weekly login reward track | Unique badge / cosmetic; no duplicate Gold reward |

Achievement rules:

- Each achievement rewards the player only once.
- Achievement state is saved.
- Gold rewards use the same transaction ledger as other Gold grants.
- Avoid achievements that require spending real money.
- The achievement list must be data-driven so more can be added later.

---

# 15. Leaderboard

## 15.1 Unlock condition

The leaderboard unlocks when the player reaches **Level 100**.

Before Level 100:

- Show a locked leaderboard entry or a short explanation.
- Do not show fake rankings.
- Do not send the player to an unavailable external page.

At Level 100 and above:

- Enable leaderboard access.
- Show the player's rank and the leaderboard list when the platform service is connected.

## 15.2 Initial leaderboard score

Use a fair, easily explained score:

1. Primary: highest level completed.
2. Tie-breaker: total levels completed.
3. Optional further tie-breaker: total Hard / Super Hard levels completed.

Do not rank players by purchases, Gold bought, ad views, or total money spent.

## 15.3 Platform abstraction

Create a `LeaderboardService` interface.

- Android: allow integration with Google Play Games Services when configured.
- iOS: allow integration with Game Center when configured.
- Editor / local development: use a local mock leaderboard.

Do not block local play if the user is offline or the service is unavailable.

## 15.4 Integrity

Use platform-verified or server-validated scores where supported. Do not trust an arbitrary client value for a public leaderboard.

---

# 16. User Interface and Visual Direction

## 16.1 Overall style

- Modern, polished, bright artist studio.
- Warm ivory / off-white background.
- Glossy paint colours with rounded surfaces.
- Transparent acrylic containers.
- Soft shadows and gentle highlights.
- Friendly, clean typography.
- A premium casual-puzzle feel.
- Avoid generic laboratory imagery and potion-fantasy styling.

## 16.2 Colour palette

Initial paint colours:

| Colour name | Suggested hex |
|---|---|
| Cherry Red | `#E84C5B` |
| Ocean Blue | `#399CE0` |
| Lemon Yellow | `#F5CE45` |
| Mint Green | `#54C99B` |
| Grape Purple | `#9867D5` |
| Coral Orange | `#F58D67` |
| Bubblegum Pink | `#E88DC3` |
| Ink Navy | `#4D536B` |
| Turquoise | `#35C6C5` |
| Warm Brown | `#9A694D` |
| Lavender | `#B5A1E8` |

Colours must be distinguishable. Do not rely on hue alone for accessibility; allow optional patterns / symbols or other accessibility cues.

## 16.3 Gameplay screen layout

Top region:

- Back button.
- Level number.
- Difficulty label: Normal / Hard / Super Hard.
- Settings button.
- Gold balance.
- Lives indicator and next-life timer where space allows.

Main region:

- Puzzle containers arranged in a responsive grid.
- Enough room around containers for touch targets.
- Selected container clearly raised.
- Covered containers have an obvious cover state.
- Hidden units show a dark `?` overlay.
- Completed containers have a subtle completion treatment.

Bottom region:

- Undo.
- Targeted Shuffle.
- Add One Empty Container.
- Move count.
- Reserved banner-ad area, with layout that does not overlap the puzzle.

The board is the main focus. Do not crowd it with promotional buttons.

## 16.4 Main menu

- Paint Sort logo / title.
- Play / Continue.
- Level progress.
- Gold balance.
- Lives.
- Daily login rewards.
- Achievements.
- Leaderboard entry (locked until Level 100).
- Shop entry (visible only when the shop feature flag is enabled, or displayed as disabled / coming soon during internal testing).
- Settings.

## 16.5 Level complete screen

- Clear level-complete animation.
- Gold earned.
- Milestone reward, if applicable.
- Collect button.
- Watch Ad — 2× Gold button if eligible.
- Next Level button.

Do not grant the reward twice when the panel is reopened.

## 16.6 Settings

- Music toggle.
- Sound effects toggle.
- Haptics toggle.
- Language placeholder / architecture for localisation.
- Privacy / consent entry where needed.
- Restore purchases where relevant.
- Reset progress (with strong confirmation and development-safe behaviour).

---

# 17. Animation and Feedback Requirements

## 17.1 Container selection

- Smooth lift of selected container.
- Small scale or rim highlight.
- No large bounce that moves other containers unexpectedly.
- Smooth return on deselection.

## 17.2 Paint pouring

A proper pour animation is essential.

- Source container tilts toward the destination.
- Paint flows in a continuous stream.
- Source paint level decreases as the stream moves.
- Destination paint level increases in sync.
- Paint colour and shape stay consistent.
- The stream must not teleport between containers.
- The paint surface should settle naturally after pouring.
- Container returns to its resting position.
- Input is disabled during the critical pour animation unless the state system safely supports queuing.

Do not make the final game look like coloured rectangles teleporting between pots.

## 17.3 Other feedback

- Invalid move: gentle shake / small rejection effect.
- Hidden layer: reveal effect when it becomes the top layer.
- Covered container: cloth / cover removal animation.
- Completed colour: soft shine / completion effect.
- Level complete: tasteful celebratory paint splash, not an intrusive long animation.
- Purchase: clear deduction / confirmation feedback.

All animations must be cancellable or safely completed during restart, level change, app pause, and scene unload.

---

# 18. Technical Architecture

Use a modular architecture. Names can follow existing project conventions, but responsibilities must remain separate.

## 18.1 Core puzzle layer (pure C# where practical)

Suggested types:

- `TubeState`
- `PaintLayerState`
- `PuzzleState`
- `LevelDefinition`
- `LevelCatalog`
- `MoveRequest`
- `MoveResult`
- `MoveValidator`
- `PourResolver`
- `WinConditionChecker`
- `PuzzleSolver`
- `MoveHistory`
- `LevelGenerator`
- `LevelAuditRunner`

The core logic must not depend on a Unity `Transform`, `GameObject`, `SpriteRenderer`, Animator, or UI Button.

## 18.2 Presentation layer

Suggested components:

- `TubeView`
- `PaintLayerView`
- `TubeInputController`
- `BoardLayoutController`
- `PourAnimationController`
- `SelectionAnimationController`
- `ObstacleViewController`
- `GameplayUIController`
- `LevelCompleteView`
- `MainMenuController`

The presentation layer reads puzzle state and displays it. It must not invent or independently alter puzzle rules.

## 18.3 Services

Suggested services / interfaces:

- `GoldWalletService`
- `GoldTransactionService`
- `LivesService`
- `DailyRewardService`
- `AchievementService`
- `LeaderboardService`
- `AdService`
- `PurchaseService`
- `SaveService`
- `TimeProvider`
- `AudioService`
- `AnalyticsService`
- `FeatureFlagService`

Use mock implementations in Edit Mode / local builds where third-party services are not configured.

## 18.4 State ownership

There must be one authoritative logical state for:

- Current level.
- Container contents.
- Hidden layer reveal state.
- Covered-container state.
- Completed-container state.
- Selected source.
- Move history.
- Lives.
- Gold.
- Daily reward claims.
- Achievements.
- Progress and leaderboard unlock.

UI widgets must not independently keep a second competing copy of important state.

---

# 19. Suggested Data Models

Use serializable, versioned data structures. The following is illustrative; adapt it to the project's conventions.

## 19.1 Paint layer

```csharp
[Serializable]
public class PaintLayerData
{
    public int colorId;
    public bool isHidden;
    public bool isRevealed;
}
```

## 19.2 Container definition

```csharp
[Serializable]
public class ContainerDefinition
{
    public string containerId;
    public int capacity = 4;
    public List<PaintLayerData> layersBottomToTop;
    public bool startsCovered;
    public bool isLocked;
}
```

## 19.3 Level definition

```csharp
[Serializable]
public class LevelDefinition
{
    public int levelId;
    public string difficulty; // Normal, Hard, SuperHard
    public int randomSeed;
    public List<ContainerDefinition> containers;
    public int coversToUnlock;
    public string[] enabledMechanics;
    public List<string> witnessSolutionMoveIds;
}
```

## 19.4 Level design rules

- Save layers bottom-to-top.
- Keep hidden metadata separate from the true colour ID.
- Keep covers separate from the paint layers.
- Keep original level state immutable.
- Runtime state is a clone of the level definition.
- Restart reloads the immutable original definition.
- `witnessSolutionMoveIds` is for editor / audit validation; do not expose it to normal players unless a hint feature is explicitly implemented.

## 19.5 Example level data

```json
{
  "levelId": 21,
  "difficulty": "Normal",
  "randomSeed": 210021,
  "containers": [
    {
      "containerId": "pot_01",
      "capacity": 4,
      "startsCovered": false,
      "isLocked": false,
      "layersBottomToTop": [
        { "colorId": 0, "isHidden": false, "isRevealed": true },
        { "colorId": 1, "isHidden": false, "isRevealed": true },
        { "colorId": 2, "isHidden": true, "isRevealed": false },
        { "colorId": 0, "isHidden": false, "isRevealed": true }
      ]
    }
  ],
  "coversToUnlock": 0,
  "enabledMechanics": ["HiddenLayers"],
  "witnessSolutionMoveIds": []
}
```

This is a schema example, not a final level. The generator must produce a complete board with valid colour counts, free containers, and a verified solution.

---

# 20. Persistence and Save Data

Persist at minimum:

- Save schema version.
- Highest level unlocked.
- Current level.
- Completed level IDs or a compact completion representation.
- Current in-progress board if resuming is supported.
- Gold balance and transaction identifiers.
- Lives and regeneration timestamp.
- Weekly login reward state and week identifier.
- Achievement unlock state.
- Audio / settings state.
- Leaderboard unlock state.
- One-time onboarding grants.

Save data must be versioned and migrated safely when new fields are added.

A corrupt save must not crash the game. Provide a safe recovery path and log enough diagnostic information for development.

For production, distinguish local progression from server-verified purchase transactions and public leaderboard submissions.

---

# 21. Debug Menu and QA Tools

A comprehensive debug menu is mandatory.

It must only be available in development builds / Editor and must be completely unavailable in release builds. Do not merely hide it behind an invisible tap sequence in production.

## 21.1 Debug menu controls

### Level controls

- Jump to any level 1–1000.
- Jump to the next Hard level.
- Jump to the next Super Hard level.
- Mark current level complete.
- Reset current level.
- Clear all level progress.
- Load a level by ID.
- Display level seed and mechanic list.

### Gold controls

- Set Gold to an exact value.
- Add / remove Gold.
- Reset Gold to default 500.
- View recent Gold transactions.
- Test 900-Gold Shuffle purchase.
- Test insufficient-Gold flow.
- Test duplicate reward prevention.

### Lives controls

- Set lives to 0, 1, 2, 3, 4, or 5.
- Add / remove one life.
- Advance the regeneration clock by 30 minutes.
- Set a custom next-life timestamp.
- Force the failure prompt.
- Force the two-step life-loss flow.

### Reward controls

- Grant Claim 1–5 daily rewards.
- Trigger the full-week +1,000 Gold reward.
- Reset weekly reward state.
- Simulate week boundary.
- Trigger / reset each achievement.
- Trigger Level 10 / 20 / 50 / 60 milestone reward.
- Test double-gold callback.

### Obstacle controls

- Toggle a hidden layer.
- Reveal the next hidden layer.
- Add or remove a covered container.
- Unlock a covered container.
- Force a completion event.
- Show / hide all special mechanic overlays.

### Ad / shop controls

- Simulate ad available.
- Simulate no fill.
- Simulate ad failure.
- Simulate rewarded completion.
- Simulate early ad close.
- Advance the five-minute ad cooldown.
- Simulate a fake purchase through the development-only purchase provider.
- Confirm no fake purchase provider is included in release builds.

### UI / animation controls

- Slow animations to 0.25x.
- Disable animations for logic testing.
- Force level-complete screen.
- Force failure screen.
- Force daily reward screen.
- Force shop screen.
- Force leaderboard screen.

## 21.2 Debug logging

Use structured, searchable logs with categories:

- `PUZZLE`
- `MOVE`
- `ANIMATION`
- `LEVEL`
- `SOLVER`
- `GOLD`
- `LIVES`
- `DAILY_REWARD`
- `ADS`
- `IAP`
- `ACHIEVEMENT`
- `LEADERBOARD`
- `SAVE`

Do not fill the production console with per-frame logs. Debug verbosity must be configurable.

---

# 22. Test All Levels Mode — Required

Implement a development-only **Test All Levels** mode.

The purpose is to prove that all launch-catalog levels (target 1,200) can be completed and that the economy / booster routes do not create impossible progression.

## 22.1 Test modes

### A. Validate All Levels

For every level from 1 to 1000:

- Load the level definition.
- Validate container capacity.
- Validate colour totals.
- Validate empty-container count.
- Validate hidden-layer metadata.
- Validate covered-container metadata.
- Validate difficulty label.
- Validate the saved witness solution.
- Simulate the witness solution using the real puzzle rules.
- Confirm that the final state meets the win condition.

### B. Solver Validation

Run the puzzle solver on each level where feasible.

- Confirm a solution exists.
- Record the solution length.
- Record the number of explored states.
- Record whether the board is trivial, normal, hard, or super hard according to configured thresholds.
- Use time / state caps so one pathological board cannot hang the entire audit.
- If a search cap is hit, mark it inconclusive and use the recorded witness plus a second validation rather than reporting it as fully solver-proven.

### C. Special Mechanics Validation

For every level with hidden layers or covers:

- Verify every trigger.
- Verify that hidden colours reveal at the correct moment.
- Verify that cover unlocks happen after the required completed colour container.
- Verify that no locked content is needed before it becomes accessible.
- Replay the known solution through the actual obstacle state machine.

### D. Economy Simulation

Simulate a player progressing from Level 1 to Level 1000.

- Start with 500 Gold.
- Grant 20 Gold for each first-time level win.
- Grant milestone rewards according to the exact schedule.
- Include daily rewards and achievement rewards as optional simulation settings.
- Simulate spending 900 Gold on Shuffle / Add One Empty Container.
- Record the wallet before and after every purchase.
- Test best-case, average-use, and heavy-booster-use scenarios.
- Confirm the player is not forced to buy Gold with real money to continue.

### E. Hard / Super Hard rescue validation

For every Hard and Super Hard level:

- Confirm that a normal solution exists, preferably without a booster.
- If a rescue path is part of the level's intended design, confirm that an allowed rescue power-up can produce a solvable state.
- Confirm that the required Gold is either already available through the simulated earnable economy or the player has the configured optional rewarded-ad route.
- Do not accept “buy Gold with real money” as the only successful solution.

## 22.2 Audit output

Generate:

- A readable summary in the Unity Console.
- A CSV report.
- A JSON report.
- A list of failed level IDs.
- A list of inconclusive solver checks.
- A list of levels that required a booster in the audit.
- A summary of simulated Gold balances.

Suggested columns:

`levelId,difficulty,colourCount,containerCount,emptyCount,hiddenCount,coveredCount,witnessValid,solverStatus,solutionMoves,boosterRequired,seed,error`

The audit must return a clear PASS / FAIL status. A failed level must not silently be skipped.

## 22.3 Build-time quality gate

Before a level catalog is accepted for release, all launch-catalog levels (target 1,200) must pass the deterministic witness replay and special-mechanic validation. Solver-inconclusive levels must be reviewed, not automatically approved.

---

# 23. Analytics and Economy Instrumentation

Create an analytics abstraction now, even if no analytics provider is configured yet.

Useful events:

- `level_started`
- `level_completed`
- `level_failed`
- `level_restarted`
- `move_valid`
- `move_invalid`
- `undo_used`
- `shuffle_opened`
- `shuffle_used`
- `extra_container_used`
- `gold_earned`
- `gold_spent`
- `daily_reward_claimed`
- `weekly_reward_completed`
- `achievement_unlocked`
- `life_consumed`
- `life_regenerated`
- `ad_requested`
- `ad_completed`
- `ad_failed`
- `shop_opened`
- `purchase_started`
- `purchase_completed`
- `leaderboard_unlocked`

Do not send personally identifying information in event payloads. Avoid excessive event volume by aggregating move-level events if needed.

Analytics should help answer:

- Where do players quit?
- Which levels cause repeated failures?
- How often do players use Shuffle?
- How much Gold is earned versus spent?
- How often do players reach zero lives?
- Does the player have a free way forward?
- Are rewarded ads useful without being intrusive?

---

# 24. Performance, Accessibility, and Mobile Requirements

## 24.1 Performance

- Target smooth 60 FPS on supported mid-range phones where practical.
- Avoid creating and destroying many GameObjects every frame.
- Pool repeated paint-layer visuals and particles where appropriate.
- Keep the level board responsive when the container count increases.
- Avoid expensive solver execution on the main gameplay thread.
- Run development audits outside normal player gameplay.

## 24.2 Input

- Support touch and mouse input.
- Use sufficiently large tap targets.
- Prevent double-tap race conditions during a pour.
- Handle app pause / resume without losing or duplicating a move.

## 24.3 Responsive layout

- Portrait-first.
- Support common phone aspect ratios.
- Respect notches, safe areas, and system navigation regions.
- Adapt the board grid as containers increase.
- Never shrink containers so far that colour layers cannot be distinguished or tapped.
- Use scroll / alternate board layout only when required and test it carefully.

## 24.4 Accessibility

- Colour-blind assistance option.
- Sound and haptic toggles.
- Readable text and contrast.
- Avoid using colour alone for status or selection feedback.
- Provide reduced-motion option if the animation system supports it.

---

# 25. Development Phases and Deliverables

Claude must implement in these phases and verify each phase before moving on.

## Phase 1 — Project audit and foundation

Deliver:

- Repository / Unity inspection report.
- Confirmed project structure.
- Core data types.
- Basic test scene.
- No unnecessary asset or package installation.

## Phase 2 — Core puzzle logic

Deliver:

- Tube state and puzzle state.
- Move validation.
- Contiguous top-group transfer.
- Partial pour logic.
- Completion detection.
- Undo.
- Restart.
- Automated unit tests for all basic rules.

Do not spend time polishing art until this phase is correct.

## Phase 3 — Playable placeholder board

Deliver:

- Container layout.
- Tap selection.
- Destination selection.
- Valid / invalid move feedback.
- Level win flow.
- Desktop mouse and mobile touch input.

## Phase 4 — Level system

Deliver:

- Level definition format.
- Seeded generator.
- Deterministic level catalog.
- Initial levels and difficulty labels.
- Witness-solution validation.

## Phase 5 — Obstacles

Deliver:

- Hidden `?` layers from Level 21.
- Covered containers from Level 31.
- Unlock-on-complete logic.
- Obstacle validation tests.

## Phase 6 — Economy and lives

Deliver:

- Starting 500 Gold.
- 20 Gold per first-time level win.
- Milestone bonuses.
- Double-gold rewarded-ad flow.
- 5 lives.
- 30-minute regeneration.
- Two-step failure / life-loss flow.

## Phase 7 — Power-ups

Deliver:

- Free Undo.
- 900-Gold targeted Shuffle.
- 900-Gold Add One Empty Container.
- Insufficient-Gold / rewarded-ad rescue flow.

## Phase 8 — Engagement features

Deliver:

- Weekly daily-login track.
- +1,000 Gold weekly completion bonus.
- Achievements.
- Leaderboard locked until Level 100.

## Phase 9 — Ads and shop scaffolding

Deliver:

- Persistent banner area.
- Central five-minute non-banner ad cooldown.
- Rewarded-ad interfaces.
- Shop screen and product data model.
- Real billing disabled until near closed testing.

## Phase 10 — Paint visual identity and polish

Deliver:

- Final acrylic paint-mixing pot visual.
- Paint layer rendering.
- Realistic-looking but performant paint pour.
- Selection / invalid / completion / obstacle animations.
- Paint-themed UI.
- Audio and haptics.

## Phase 11 — Full catalog and audit

Deliver:

- At least 1,000 actual level definitions (target 1,200).
- Test All Levels mode.
- CSV / JSON validation reports.
- Economy simulation report.
- All Hard / Super Hard levels validated.

## Phase 12 — Device testing and release preparation

Deliver:

- Android development build.
- Tested touch input.
- Save / restore verification.
- App pause / resume verification.
- Performance pass.
- Release build with debug menu excluded.
- Known-issues list.

---

# 26. Mandatory Test Checklist

## Core gameplay

- [ ] Empty source cannot pour.
- [ ] Source cannot pour into itself.
- [ ] Full destination cannot receive paint.
- [ ] Different colours cannot be combined.
- [ ] Matching colours can be combined.
- [ ] Empty destination accepts paint.
- [ ] Only the contiguous top-colour group moves.
- [ ] Partial pour works when destination space is limited.
- [ ] Capacity is never exceeded.
- [ ] Completed container is detected correctly.
- [ ] Win condition is correct.
- [ ] Invalid moves do not change the state.
- [ ] Undo restores the exact state.
- [ ] Multiple undos work.
- [ ] Restart restores the original board.
- [ ] App pause / resume does not duplicate a move.

## Special mechanics

- [ ] Hidden layers do not reveal too early.
- [ ] Hidden layer reveals when it becomes topmost.
- [ ] Hidden colour is not treated as a wildcard.
- [ ] Covered containers are inaccessible before unlock.
- [ ] One completed colour container unlocks one cover.
- [ ] Multiple covers unlock in the correct sequence.
- [ ] Restart restores the original covers.
- [ ] Undo correctly reverses an unlock if the triggering completion is undone.

## Progression

- [ ] Level IDs 1–1000 exist.
- [ ] All level data is deterministic.
- [ ] Levels 5, 15, 25, etc. are Hard.
- [ ] Levels 10, 20, 30, etc. are Super Hard.
- [ ] Super Hard takes precedence over Hard.
- [ ] Hidden layers first appear at Level 21.
- [ ] Covered containers first appear at Level 31.
- [ ] Container count increases according to progression.

## Gold

- [ ] New player receives 500 Gold once.
- [ ] First-time level win grants 20 Gold.
- [ ] Reopening the win panel does not grant duplicate Gold.
- [ ] Milestone bonus is correct through Level 50.
- [ ] Milestone bonus is 20 after Level 50.
- [ ] Double Gold doubles the current reward only once.
- [ ] Gold cannot go below zero.
- [ ] Failed power-up activation does not charge Gold.
- [ ] 900-Gold targeted Shuffle works.
- [ ] 900-Gold extra container works.

## Lives

- [ ] Maximum lives is 5.
- [ ] One life regenerates every 30 minutes.
- [ ] Regeneration persists while app is closed.
- [ ] Regeneration stops at 5.
- [ ] A failed attempt consumes exactly one life.
- [ ] Duplicate fail callback does not consume multiple lives.
- [ ] First rescue prompt works.
- [ ] Second life-loss confirmation works.
- [ ] Cancel / Keep Playing does not consume a life.

## Daily rewards / achievements / leaderboard

- [ ] Daily rewards are 50, 100, 200, 300, and 400 Gold.
- [ ] Each claim can be collected once per weekly cycle.
- [ ] Weekly +1,000 Gold bonus is granted once after all five claims.
- [ ] Weekly reset behaves correctly.
- [ ] Achievements unlock once.
- [ ] Achievement rewards are not duplicated.
- [ ] Leaderboard remains locked before Level 100.
- [ ] Leaderboard unlocks at Level 100.
- [ ] Offline play does not crash when leaderboard service is unavailable.

## Ads / shop

- [ ] Banner does not overlap controls.
- [ ] No more than one non-banner ad opportunity occurs in five minutes.
- [ ] Rewarded ad grants reward only after completed callback.
- [ ] Failed ad grants no reward and charges nothing.
- [ ] Double Gold can be collected without watching an ad.
- [ ] Shop is disabled until its feature flag is enabled.
- [ ] Fake purchase provider is excluded from release builds.

---

# 27. Acceptance Criteria

The first complete playable milestone is accepted only when:

1. The game opens and can be played from Level 1.
2. Core sorting rules work correctly.
3. Containers are paint-mixing pots, not test tubes or ordinary potion bottles.
4. Pour animation is visually synchronized with logical state.
5. Undo and restart work.
6. Levels are deterministic.
7. The level system supports the full initial catalog of at least 1,000 levels (target 1,200).
8. Hard / Super Hard labels follow the exact cadence.
9. Hidden layers start at Level 21.
10. Covered containers start at Level 31 and unlock after a completed colour container.
11. Gold rewards, milestones, daily login, and lives work.
12. Shuffle and Add One Empty Container each use the configured 900-Gold price.
13. Ads respect the five-minute non-banner cooldown.
14. Shop UI exists but real billing remains disabled until configured near closed testing.
15. Achievements work.
16. Leaderboard unlocks at Level 100.
17. Debug menu can exercise every system.
18. Test All Levels produces an auditable report for Levels 1–1200 (or the configured launch catalog count, never below 1,000).
19. Every level passes deterministic witness validation.
20. All Hard and Super Hard levels have a verified fair solution or an explicitly verified free rescue route.
21. Release builds do not contain the development debug menu or fake purchase provider.

---

# 28. Explicit Non-Goals for the First Version

Do not spend first-version development time on these unless the core game is already complete and verified:

- Multiplayer puzzle play.
- Teams / social life requests.
- User accounts.
- Cloud save backend.
- Seasonal events.
- Live operations calendar.
- Cauldron / ingredient crafting.
- Player-created levels.
- User-to-user gifts.
- Large cosmetic store.
- Complex story campaign.
- Full collection album.
- Ads during active pours.
- Any system that makes a level impossible without spending real money.

The architecture may leave room for these systems, but do not build them before the main game is stable.

---

# 29. Final Instructions to Claude

Implement Paint Sort in small, testable phases.

At the end of every phase, report:

- What files / systems were created or changed.
- What functionality is actually implemented.
- What tests were run and their results.
- What remains incomplete.
- Any decisions that required a reasonable assumption.

Do not simply say “done” if the feature has not been opened and tested.

Do not replace requested functionality with a placeholder without clearly reporting it.

Do not stop after Phase 1. Continue through the phases until there is a playable game and a documented audit result, subject to the available project tools and runtime.

The most important result is a reliable, satisfying sorting puzzle with at least 1,000 fair, validated levels (target 1,200 at launch). Monetization must support progression, not make the puzzle intentionally unwinnable.

---

# 30. Research References

The following references were checked while preparing this specification. Store listings and guides can change over time; use them as background reference rather than as a substitute for testing the actual installed game.

1. **Magic Sort! — Google Play listing, Grand Games A.Ş.**  
   https://play.google.com/store/apps/details?id=com.grandgames.magicsort

2. **Magic Sort! — Apple App Store listing, Grand Games A.Ş.**  
   https://apps.apple.com/in/app/magic-sort/id6499209744

3. **Magic Sort! Hard Levels Guide — Gaming Route.** Covers reported hidden layers, covered containers, keys / locks, three boosters, lives, ads, and player-reported systems.  
   https://gamingroute.com/magic-sort-hard-levels-tips/

4. **Magic Sort Level 341 walkthrough.** Example of reported key / curtain unlocking behaviour.  
   https://magicsortlevel.com/levels/341

5. **Magic Sort Level 338 walkthrough.** Example of hidden question-mark layers and curtain-covered containers.  
   https://magicsortlevel.com/levels/338

6. **Grand Games — official company site.**  
   https://trust.grand.gs/

---

**End of specification.**


## Copy-ready Claude Code kickoff instruction

After placing this document in the project root (or making it available to Claude Code), give Claude this instruction:

> Read `PAINT_SORT_GDD_CLAUDE_IMPLEMENTATION_SPEC.md` completely and treat it as the source of truth for Paint Sort. First inspect the current repository, Unity project, Git state, and available MCP tools. Then implement the game in the phases defined in the document. Do not stop after planning. Start with the core puzzle logic and automated tests, then build the playable board, level system, obstacles, economy, lives, rewards, power-ups, debug panel, and visual polish. Generate the full initial level catalog (target 1,200, minimum 1,000) and run Test All Levels. Do not claim completion unless the project builds and the relevant tests have passed. Report real blockers and incomplete work honestly.
