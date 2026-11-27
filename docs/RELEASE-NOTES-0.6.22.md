# Kingmaker Buff Planner 0.6.22

Greater Magic Fang can be planned and cast on several pets in one routine again, including pets of other party members.

- **What was wrong.** The planner expected all five Greater Magic Fang enhancement tiers to appear after a single cast. The game applies the duration buff and one tier, so the first cast was never confirmed and every later casting in the run was stopped. A routine that started with the Druid's own pet therefore looked as if the spell only worked on that pet.
- **Now.** Each cast expects the duration buff plus the one tier its caster's level gives: +1 up to caster level 7, +2 from 8, +3 from 12, +4 from 16 and +5 from 20. Call of the Wild's replacement tiers follow the same rule.
- Magic Fang on unarmed strikes, extra natural weapons (such as a bite) and polymorph natural weapons can now be confirmed.
- If a caster's level cannot be read, Greater Magic Fang is not planned for that caster.
- An existing Magic Fang is still recast rather than skipped, as before. The bonuses do not stack, but the recast spends a slot or spell.

Saved routines and assignments keep working. The Unity Mod Manager baseline is unchanged from 0.6.21 (0.31.1 or later).

This is an owner-authorized pre-runtime release, published on the owner's authorization of November 26, 2026 for version 0.6.22. This exact release has not been run in the game; the repair was verified with offline tests against the native 2.1.7b blueprints only.

[Owner in-game checklist](https://github.com/howardreith/KingmakerBuffPlanner/blob/v0.6.22/docs/MANUAL-ACCEPTANCE.md#greater-magic-fang--owner-in-game-acceptance-pending)
