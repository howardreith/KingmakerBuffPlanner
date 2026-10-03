# E01–E27 acceptance matrix — everyday-use v1.2 private preview

Rows and required observations are the ORIGINAL definitions of the v1.2
handoff (`KBP_Everyday_Use_v1_2/03_ACCEPTANCE_MATRIX.md`). The previous
version of this file (written at 3c1c5d4) renumbered several rows and marked
E05/E06 PASS from a run whose frame shows no description panel (D11); it is
superseded by this one.

**Delivered candidate:** source commit `8d7681d03752f3f7170f25f7d45029f71c46a884`,
package `5b24e2eafee69b18898894b6e9a5cfb9b2c8e625a561494cb2234766097d8d17`,
DLL `deee7b1de54daee9e890f6680c8e0d4db09674b12c394e2d2d6c2d3e460cf65b`,
MVID `1e762004-8200-430e-853e-d61456a627ae` (version string 0.2.0-rc6).

**Evidence types:** *source* = assembly-backed protocol/source test at the
candidate (protocol suite 398, source validation 42, full gate
`artifacts/gate-8d7681d.log`); *native-UI* = guarded in-game run reading the
real UI; *physical* = guarded in-game run driven by operating-system input
(mouse/keyboard) through the game window; *native-cast* = guarded in-game run
that cast on a disposable fixture and observed effects and resources;
*prior-build* = native evidence from an earlier candidate, labelled with its
own commit. Every run restored the lab exactly (transaction Restored,
protected saves clean) unless stated. Run evidence lives under
`C:\Dev\KingmakerBuffPlannerLab\runtime-evidence\<run-id>\`.

**Display:** every 1920x1080 run is FULLSCREEN (Unity `Screen.fullScreen=True`;
the launcher's "windowed" transaction does not produce a window - H2).
Windowed presentation is not qualified. Physical runs use the owner's own
display settings; in r15 the desktop and the game surface were both
1920x1080 (every acknowledgement records `bars=0,0`). The letterbox mapping
repair (H1) is proven by its WhatIf regression against the geometry observed
on a 1920x1200 desktop in r13, not live in r15. The session type (console or
RDP) is not recorded per run; the owner's session was the console when r15 ended.

| ID | Required observation (original) | Verdict | Evidence type | Runs / tests | Limitation |
| --- | --- | --- | --- | --- | --- |
| E01 | Fresh/unset profile: every normal editor route opens casting-first; Instant default; no Save or Accept Plan | PASS | source + native-UI + physical | source: planner-mode default and route wiring (hotkey, HUD gear, spellbook entry -> casting-first), `EverydayUseWordingTests` (settings page, footer and HUD say no ceremony); native: r15 w1080-01, adv-01, reload-01 opened casting-first in Instant mode; phys-sel/phys-cast-01 opened it through the physical hotkey; frames show "Mode: Instant", a "Saved" status and no Save/Accept control | The UMM settings-page route is source-tested only |
| E02 | Upgrade existing graph/legacy settings: IDs preserved, graph preferred over stale legacy, explicit Animated kept, legacy ambiguity visible | PASS | source + native-UI | source: real serialized migration cases (graph preferred, Animated kept, IDs preserved); native r15 imp-01: classic plan migrated with 2 Automatic-caster castings kept as Drafts with no caster (`ambiguityKept=True; drafts=2; unresolvedCasters=2`), classic file byte-unchanged | Animated retention and graph-over-legacy preference are source-only |
| E03 | Classic retirement: no prominent Classic switch or competing writer; importer/archive/rollback retained; routes mapped | PASS | source + native-UI | settings page offers only a one-way switch from a stored Classic choice (D1); route tests; import keeps the byte-exact `.orig` archive (imp-01 `archives=1`); `Restore-InstallLocal` 16/16 (rollback keeps prior and trial plans) | Settings page source-only |
| E04 | Continuous scroll: no fold/gutter beneath connections; labels, inspector and footer legible and reachable | PASS | physical + native-UI frames | r15 phys-sel-01 / phys-cast-01: the Short tab was clicked physically (14 castings, graph overflows) and the physical wheel scrolled it 1.0 -> 0.771; frames `physical-cf-graph.png`, `physical-cf-graph-scrolled.png` show one continuous parchment with the ink lines following the chips; `page=continuous-scroll;book-art-retired` in w1080/reload hierarchy evidence | 1920x1080 fullscreen only (H2); legibility judged from frames by me, the owner's visual verdict is pending |
| E05 | Right-click inspect: full native concrete-spell description from row/icon/header; no mutation | PASS | physical | r15 phys-sel-01 / phys-cast-01: physical right-click on a casting card opened a 760x520 panel titled "#1 Resistance" whose body equals the source's native description (`inspectBodyNative=True`, `inspectTitleNative=True`); document signature unchanged | Row and header use the same handler (source); only the card was right-clicked physically. r11/r13 frames show the panel never rendered then (D11) - those earlier "PASS" claims were wrong |
| E06 | Description lifecycle: long content scrolls; Escape closes description first; later clicks work; nothing leaks | PASS | physical | a labelled long-native-text probe in the same panel overflowed (1129 px) and the physical wheel scrolled it (1.0 -> 0.975); the wheel over the description never moved the graph (0.771 -> 0.771); Escape closed the description, then the planner; the game menu stayed closed (`escMenuOpenAfterClose=False`); input lease released; game mode back to Default; world-input probe clean | Long content is a labelled probe (joined native descriptions) because single fixture descriptions fit the panel |
| E07 | Autosave commands persist current intent without Save | PASS | source + native-UI | autosave boundary tests; r15 w1080-01 / reload-01 interaction (3 castings, retarget, Undo, close, reopen `preserved=True`) with "Saved" in the footer; phys-cast cold seed stored by an earlier session and run without any save step | |
| E08 | Undo + rapid edits: latest wins, no stale completion marks Saved | PASS | source | controlled write-order/failure tests | Not exercised in game (by design: pure ordering policy) |
| E09 | Close/restart: a new session loads the last revision exactly | PASS | native-UI | r15 reload-01: save reload in the running game, `diskPreserved=True; sessionReused=True; subscriptions=1->1; hudRoots=1->1`; imp-01 fresh production session from disk equals the imported intent (`freshEqual=True`) after the planner closed; phys-cast-01 ran a plan stored by an earlier session | A full process restart (quit to desktop) is not automated |
| E10 | Save failure / protected loads keep last good files and in-memory edits; Run does not use a stale disk plan | PASS | source | real isolated-file tests (read-only, corrupt, newer schema, failed replace, bad campaign identity); footer shows Not saved + Retry save / Reload (D6/D7) | Not induced in game |
| E11 | Read-only navigation writes nothing | PASS | source + physical | browse/hover/compile tests; w1080 `browseNoMutation=True`; phys runs: document signature equal across browse, routine tab, wheel, right-click, description | |
| E12 | Physical moon cold start: Long only, editor stays closed | PASS | physical + native-cast | r15 phys-cast-01: no session and editor never opened before the press; one HUD moon click; allowance `valid` (cap exactly 1); grant consumed, 1 attempt; exactly 1 run, routine long, completed, 1 submission, `seed-long-1=EffectConfirmed`; Long effect false -> true, Important false -> false; editor stayed closed; free source 99 -> 99 | Seeded with a free cantrip source: the cost check is "nothing spent" (paid costs are proven by the qualification chains) |
| E13 | Other routines/editor use the same preflight; no crossover; no hidden acceptance | PASS | source + native | route/wiring tests (HUD Long/Important/Short and editor Run share one run service); phys-cast: Important and the 14 Short castings stayed un-run (`moon:routine-crossover` judged); the planner's Run caption follows the selected routine (frames) | Important/Short HUD buttons not physically clicked |
| E14 | Repeat/busy/invalid: harmless repeat, no overlap/double spend, no silent subset | PASS | native-cast + source | r15 cast-finite-instant / -animated: stop pressed during a cast in flight (cast 1 confirmed, cast 2 not processed), complete skipped the active effect and cast the other (slot 1 -> 0), the repeat submitted nothing, Always recast spent exactly one slot (5 -> 4); cast-ability-pool-instant: the single-use pool spent once (1 -> 0), repeat and exhausted submitted nothing; repeat step casts nothing; Share shortage step refused the routine whole with zero submissions and zero spend (r15 shared runs, 4 shortage castings); single active-run guard tests | |
| E15 | Active-run revision / Stop | PASS | native-cast + source | r15 cast-finite-instant / -animated: a routine press during the first cast stopped the run (`stopPressedInFlight=True`, terminal player-stopped): the cast in flight finished and was charged honestly (6 -> 5), the next casting was not processed and spent nothing (1 -> 1); coordinator snapshot tests | |
| E16 | Share before target: exposed after caster/source, before recipient; off = self only; on = legal allies | PASS | native-cast + source | r15 sel-shared-personal / sel-shared-powerful: exact Brown-Fur caster and spontaneous source, Share armed before the ally target, legal non-self ally; preview left caster state unchanged; inspector ordering and reasons source-tested | The "Share with an ally" control itself was driven through the production authoring API, not physically clicked |
| E17 | Share intent round trip | PASS | source + native | persisted per-casting intent read back from disk (`persisted=True`), disarming the draft kept the existing casting (`draftDisarmKeptCasting=True`); two-ally, switch-off and Undo/reload round trips source-tested | |
| E18 | Native shared cast once, observed costs; Instant/hybrid and Animated | PASS | native-cast | r15 cast-shared-personal-instant / -animated: the personal transmutation landed on the ally (AC natural +4, Str +4, Dex -2, speed +10) through the verified Brown-Fur provider transaction once; slot spontaneous-4 4 -> 3; Arcane Reservoir 16 -> 15 | Supported provider: Brown-Fur Transmuter (KingmakerGunslinger 0.0.136) on the Advanced fixture |
| E19 | Combined Share costs exact; shortage gives no partial reservation or submission | PASS | native-cast | r15 cast-shared-powerful-instant / -animated: Share + Powerful Change charged once each (reservoir 16 -> 14, provider reservoir-cost 2), Str +6 instead of +4; shortage step refused whole, reservoir and slot unchanged | |
| E20 | Share isolation: no live toggle in preview; no leaked state; next plain cast normal | PASS | native-cast + source | caster toggles identical before/after every step; the plain witness cast by the same caster spent a slot (6 -> 5) and no reservoir, with the shared casting omitted as disabled; cancel/exception paths source-tested | |
| E21 | Relevance filtering of enhancements | PASS | source + native-UI | relevance policy tests (Good Hope: no Piercing/Persistent/Selective; Extend/Powerful Change when applicable); r15 adv-01 (Advanced fixture) enhancement phase: an Extend rod offered and its uses pool budgeted across castings (Uses 3-2-2-3) | The Good Hope inspector was not rendered natively |
| E22 | Previously selected option survives as explained/removable intent | PASS | source | saved-plan migration + compile tests | |
| E23 | Beneficial catalog | PASS | source + native catalog | structural classifier tests; catalog discovery in every guided run | |
| E24 | Existing excluded spell visible, blocked, not silently deleted | PASS | source | persist/load/compile regressions | |
| E25 | Global accounting preserved | PASS | native-UI + source | r15 adv-01: cross-buff spontaneous pool proved (adding buff B moved the shared spontaneous-2 capacity 6 > 5, Undo restored 6), shared Extend-rod uses pool 3-2-2-3, complete-cost atomic refusal observed (a blocked prepared casting reserved nothing: prepared 25 > 25, rod 2 > 2), cleanup verified; w1080-01 (Automation fixture) records cross-buff/enhancement as not-run:unsupported (no shared pool there); production ledger tests | |
| E26 | Recovery and performance | PASS | source + native | Restore-InstallLocal 16/16 (archive-first upgrade/downgrade keeps prior and trial plans); imp-01 one byte-exact archive; reload `subscriptions=1->1; hudRoots=1->1`; w1080-01 / adv-01: one planner root after reopen, graph selectables stable, hover single-owner (35/37 samples) | No new same-scene performance baseline comparison was run in this round |
| E27 | Delivery identity: UMM ZIP hash matches the private destination; source/hashes/MVID/guide agree; limitations named; old beta untouched | PASS | delivery | draft release 399014941 asset `KingmakerBuffPlanner-0.2.0-rc6-everyday-use-v1.2+8d7681d0.zip` (id 607817987): uploaded from the frozen bytes, downloaded back and verified (701341 bytes; package 5b24e2ea..., DLL deee7b1d..., MVID 1e762004-..., embedded commit 8d7681d0, exact entries); prior beta asset 597866119 (214b795b..., 661250 bytes) re-downloaded identical before and after; release still a draft; receipt `docs/evidence/everyday-use-v1.2-delivery-receipt.md` | Owner acceptance pending (self-paced trial) |

## r15 batch (all 41 jobs done and verified on 8d7681d, 2026-10-03)

Dispatched by the existing lab dispatcher (`dispatch\jobs.r15-final.json`);
allowances written by `scripts/New-KbpRunAllowance.ps1` from each selection
run's recorded evidence (`C:\Dev\KingmakerBuffPlannerLab\approvals`). Every
run: transaction Restored, Kingmaker exited, protected saves compared and
clean. Run evidence directories:

- `beta-8d7681d0r15-adv-01`
- `beta-8d7681d0r15-cast-ability-pool-instant`
- `beta-8d7681d0r15-cast-enhanced-instant`
- `beta-8d7681d0r15-cast-finite-animated`
- `beta-8d7681d0r15-cast-finite-instant`
- `beta-8d7681d0r15-cast-group-instant`
- `beta-8d7681d0r15-cast-rod-extend-instant`
- `beta-8d7681d0r15-cast-shared-personal-animated`
- `beta-8d7681d0r15-cast-shared-personal-instant`
- `beta-8d7681d0r15-cast-shared-powerful-animated`
- `beta-8d7681d0r15-cast-shared-powerful-instant`
- `beta-8d7681d0r15-imp-01`
- `beta-8d7681d0r15-insp-ability-pool`
- `beta-8d7681d0r15-insp-enhanced`
- `beta-8d7681d0r15-insp-finite`
- `beta-8d7681d0r15-insp-group`
- `beta-8d7681d0r15-insp-rod-extend`
- `beta-8d7681d0r15-insp-shared-personal`
- `beta-8d7681d0r15-insp-shared-powerful`
- `beta-8d7681d0r15-phys-cast-01`
- `beta-8d7681d0r15-phys-sel-01`
- `beta-8d7681d0r15-reload-01`
- `beta-8d7681d0r15-sel-ability-pool`
- `beta-8d7681d0r15-sel-enhanced`
- `beta-8d7681d0r15-sel-finite`
- `beta-8d7681d0r15-sel-group`
- `beta-8d7681d0r15-sel-rod-extend`
- `beta-8d7681d0r15-sel-shared-personal`
- `beta-8d7681d0r15-sel-shared-powerful`
- `beta-8d7681d0r15-w1080-01`

## Repairs that changed verdicts in this round

Each repair has a regression test; native evidence is quoted only from runs on the delivered candidate.

| Ref | What was wrong | Found by | Repair |
| --- | --- | --- | --- |
| D1-D10 | Settings Classic toggle "experimental"; Accept/Save ceremony wording; stale raw footer save status, no recovery action; HUD gear ran Long; "Run Long" caption; planner Escape also opened the game menu | source and frame reading during r13 | e185cc4, d49f15a |
| D11 | The right-click description panel never rendered (point anchor with edge insets: negative width, zero height); the old record judged only `activeSelf`, so E05/E06 were marked PASS wrongly | r11/r13 `physical-cf-inspect.png` | 268e291 |
| D12 | Description titled "#1 heighten-0" (provider-key fragment) | r14 phys-sel frame | 648608f |
| D13 | The graph never overflowed in any physical run (`not-applicable:no-overflow` in r11, r13, r14): physical graph scrolling had never been demonstrated | r14 record | 648608f |
| Grant cap | The cold-moon digest record counted every routine's castings, so the single-use grant's cap was looser than Long | r14 cf-plan-digest.json | 648608f |
| H1 | Launcher stretched Unity coordinates over a letterboxed client (1920x1080 surface on a 1920x1200 desktop) | r13 w1080 hover samples | 9b788e8 |
| H2 | The "windowed" transaction writes Unity's ExclusiveFullScreen; every 1920x1080 run is fullscreen | r10-r15 `fullScreen=True` | not repaired: windowed is not claimed |
| H3 | The cf-physical allowance writer demanded the pre-v1.2 seed note | r14 allow-phys | 648608f |
| H4 | The shared-personal allowance purpose (441 chars) exceeded the launcher's 400 bound | r14 cast-shared-personal-instant (refused in preflight) | 8d7681d |
| H5 | Allowance run ids were not batch-unique, so stale claims blocked a new batch | r15 dispatch | dispatch\build-jobs.ps1 (lab tooling, outside the repo) |

Evidence of earlier candidates (r11 f0cf4f16, r13 3c1c5d4, r14 bff840a) is kept under its own run ids and is not used for any verdict above.
