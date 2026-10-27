# Mr. Bard's Kingmaker Buff Planner 0.6.7

Owner-authorized stable update for limited-use source classification and casting
reports. This exact release has not been run in the game. In-game controller,
mouse, Animated casting and native acceptance remain unverified.

## Changes

- An ability without an ordinary ability resource is no longer assumed to be
  unlimited. Supported single-item charge sources receive finite budgets, with
  one shared budget for abilities that spend the same item.
- Instant and Animated casting check the selected source against its reservation
  before submitting a cast. Inconsistent, exhausted, unsupported or unread
  sources are refused before spending; known shortages under Require full plan
  prevent the routine from starting.
- Casting reports keep observed resource use and observed effects separate from
  the forecast and the final success/failure result. Unexpected spending still
  halts the run, and the affected attempt is never labelled free.
- Casting diagnostics identify the bound source, item charges, planned native
  cost, observed use and effect observation to help investigate uncertain cases.

Native casting, resource spending, targeting, materials and enhancements continue
to use the game's existing behavior. Controller navigation is unchanged.

## Saved plans and item-source limitation

Saved castings and targets are retained. If an item source was previously saved
as a free granted ability, explicitly choose its correctly priced item source.

Item-source selections are tied to the exact live item and must be reselected
after loading a save. The planner refuses an unresolved selection instead of
choosing another item with the same spell. Stacked consumables, combined item
and ability-resource costs, and other unverified finite contracts remain safely
unsupported.

The reported Aspect of the Falcon source has not been identified. The repair
reproduces the finite-without-ordinary-resource contract using installed game
methods and focused fixtures; it does not establish that the player's exact
configuration is fixed. See the
[repair checkpoint and R1-R7 acceptance matrix](LIMITED-USE-REPAIR-CHECKPOINT.md).

## Validation boundary

The owner-authorized pre-runtime release workflow runs source validation, the
complete offline C# behavior suite, publisher gate tests, two deterministic
release builds and strict package validation. Detached native-method fixtures
do not establish in-game effects or physical controller/mouse acceptance.

For a focused in-game check, use a backed-up disposable save: select a supported
finite source with enough uses for several legal castings, compare its remaining
uses and observed effects, then check a known shortage with Require full plan.
Also check an ordinary unlimited source and both Instant and Animated modes.
If a run stops unexpectedly, retain the full `[KBP-CF-RUN] casting` log line,
including the available-before/after and source details.
