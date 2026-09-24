# Casting-First Migration — GitHub Review Index

Branch: `codex/kingmaker-buff-planner-casting-first`
Reviewed baseline: `c182061354e9e761c09648ca779ab334588ba379`
(`codex/kingmaker-buff-planner-z-native-assignments`). The PR against
`main` additionally shows the earlier migration lineage
(`fd0e6dc..c182061`); this index covers the casting-first commits
`c182061..HEAD` (61 commits at first publication).

Status: **release candidate 0.2.0-rc5.** 0.2.0-rc4 (frozen at `863a182`,
receipt `docs/evidence/rc-0.2.0-rc4-receipt.md`, superseded after the
owner's review of its source), 0.2.0-rc3 (frozen at `ea2a027`,
receipt `docs/evidence/rc-0.2.0-rc3-receipt.md`, superseded after its final
review), 0.2.0-rc2 (frozen
at `ae0181d`, receipt `docs/evidence/rc-0.2.0-rc2-receipt.md`) and 0.2.0-rc1
(`f8562a6`, with the animated cantrip defect described below) keep their
receipts as history. Not a fully gameplay-qualified release. Casting-first is an opt-in planner mode (UMM setting, Classic
by default). In ordinary play every routine route reaches the production
dispatch boundary and the execution host; in an automated test session
both player routes (casting-first and classic) refuse, and native casts
happen only through the owner-authorized harness boundaries (single-cast
probe, casting qualification). First native evidence: the one-cast
Resistance probe confirmed its effect in game on
`320a1b6` (`casting-probe-cast-20260923-p2-02`), and the zero-cost
qualification passed in game on `d35b38f`
(`casting-qual-cast-20260923-q1-01`: stop, complete, repeat and a recast
after a close and reopen, each exactly as forecast). On the owner's
advanced seed (a disposable Beneath the Stolen Lands save) the finite,
group and enhanced qualifications followed; see the section below and the
rc5 receipt. Human usability and the native aesthetic pass remain open.

## The owner's rc4 source review and the advanced seed (2026-09-24)

The owner's review of the rc4 source (two findings) and the work for the
owner-designated `KBP_ADVANCED_SEED` (a disposable *Beneath the Stolen
Lands* save), each with its disposition:

| Finding or request | Disposition |
| --- | --- |
| Unreadable suppression or required strength information must not establish that an existing effect is sufficient | Fixed in `cb684f2`: the live snapshot records an unreadable suppression flag as unreadable (it read as "not suppressed"); an instance whose caster level cannot be read, or a planned casting whose caster level is unknown, is never "at least as strong"; the casting keeps its step and the card says why ("not provably as strong", "possibly suppressed"). Test fixtures now carry caster level 1, as every provider in the game reports |
| Reused migration archives must be verified against the exact original bytes, not trusted by file name | Fixed in `cb684f2`: `AtomicFile.WriteExactArchive` reuses a file only when its bytes are exactly the original's; a different file at the name is kept and the bytes go to the next numbered name; a new archive is read back and compared. Used by the casting migration's boundary archive, the Classic pre-schema archive (now raw bytes, byte-order mark included) and the unreadable-primary quarantine |
| A mixed-coverage group case: one recipient already has an adequate, longer-lasting buff while another lacks it | `cbdc2fd`, tests `48b5402`: the group casting still casts once for the others; recipients whose existing effect is provably sufficient from readable detail (skip-if-active only) are pre-covered in the step and its approved identity; confirmation accepts their kept coverage only when the complete effect was present and unsuppressed before the cast and still is after it; every other recipient needs a new or refreshed instance. Qualified live by the `group-mixed` recipe |
| Do not describe default skip-if-active as avoiding every unchanged-instance confirmation problem | `0c81925`: the release notes and the player guide now say it avoids the problem only when the casting is skipped, and name the cases where it does not |
| Advanced compatibility profile for the Gunslinger 0.0.136 installation (mission batch 3, section 5) | `a134f0a`: `advanced-gunslinger-0136`, pinned by a test to the six approved values and staged from an exact external copy; BagOfTricks resealed in both profiles as a frozen copy (its resource-altering toggles verified off); the launcher and the host refuse the advanced copy under another profile and the advanced profile without the advanced copy |
| Found live: the finite selection refused the advanced party (`casting-qual-select-20260924-adv-finite-01`, nothing cast) | `2e565bd`: every variant spell was rejected for its effect shape, because a variant's effect references the variant it casts while the plain-buff check accepted only the base spell. It now accepts either; rejections name the structure they saw; 160 rejections are kept (40 hid the second caster) |
| Group buffs (mission batch 3, section 8) | `2e565bd`: the `group-mixed` recipe (prime a recipient with a direct spell; the caster-centred communal form then casts once for the others with that recipient pre-covered; a target-anchored group casting where one exists), with per-recipient reads, one availability reading per casting, and its own step rules |
| Found live: the group recipe's repeat step failed (`casting-qual-cast-20260924-adv-group-anim-01`; its prime and mixed steps passed) | The game replaced the pre-covered fighter's longer direct instance with the communal form's shorter one, so at the repeat the direct casting was due again and its prepared slot was spent (`blocked-casting:qual-cast-1:prepared-slots-exhausted`). The recipe now ends after its mixed step (the direct recipes show that a repeat casts nothing), and a pre-covered recipient whose effect outlasts the cast gets its own note: "... lasting longer than this cast (the cast goes ahead for the others and may shorten it)". The player guide and release notes no longer say such a recipient keeps its effect. Qualified live on `0c03930`: selection `casting-qual-select-20260924-adv-group-02`, then `-adv-group-anim-02` and `-adv-group-inst-01` PASS (one invocation and one prepared slot for the communal casting reaching all four members, the fighter's instance replaced as before; the target-anchored casting one spontaneous level-3 use) |
| A supported non-rod per-casting enhancement exercised through the real UI, service and execution chain (mission batch 3, section 8) | `9d530bc`, tests `fefd5ec`: the `enhanced-direct` recipe casts one qualifying plain buff twice from the same source, plain on one recipient and then on another with Brown-Fur Powerful Change, chosen through the workspace's own option for the focused casting (the view the inspector draws and the command its chip calls). New native reads: the stat modifiers each effect instance gives, the enhancement's resource (the Arcane Reservoir) and every activatable ability of the caster. Rules: the plain step spends none of the reservoir; the enhanced recipient's modifiers are the plain one's with the enhanced stat raised by exactly 2, and the plain recipient's are unchanged; the reservoir drops by exactly one point; every activatable ability ends as it began. Share Transmutation (a targeting modifier) stays refused |
| Found while writing the enhanced recipe: in Instant mode the casting-first planner cast an enhanced casting by rule, a path the Brown-Fur provider's transaction never takes part in (its design notes: transactions enrol only from a native command or the provider's own version-1 handle), so Powerful Change would not have been applied while the cast was still confirmed | Fixed in `163a1bb`: the explicit compiler applies the classic planner's `CastEnhancementExecutionPolicy` to each casting's applied enhancements (provider-direct through `ProviderDirectRuleCast`, a native-command enhancement through `NativeCommandRequired`, otherwise the source's own strategy); the resolved casting carries the strategy and its reason into the step. A plain casting is never rerouted. Tests pin each route; the enhanced recipe's forecast pins the provider-direct route, and its live runs judge the result in both modes |

Independent read-only review of the unpushed work (`45ba833..f7e4b34`,
2026-09-24), eight findings:

| Finding | Disposition |
| --- | --- |
| Worn-item enchantments are recorded with no caster level, so under the rc4-review rule they are never sufficient: a skip-if-active Magic Weapon-type casting recasts every run, and stops the routine if the game keeps the enchantment unchanged | Kept as the owner's rule requires (a strength that cannot be read never proves sufficiency). Known limitation in the release notes; flagged for the owner, who may prefer reading a temporary enchantment's caster level and end time (the game exposes both) in a later version |
| The enhanced recipe checked the outcome but not the route that ran | Fixed in `3868c39`: the selection refuses an enhancement no route enrols and reports the forecast route; the enhanced step requires the route that ran (provider transaction in Instant, the game's own command otherwise) |
| Share Transmutation could be chosen as an enhancement (it changes targeting) and would have run armed on the caster's own target | Fixed: an enhancement that changes whom the spell reaches blocks the casting with its reason ("... such as Share Transmutation ... is not executed yet"); it stays selected and visible |
| Coverage given by an earlier casting in the same run is not pre-coverage | Known limitation (release notes and player guide): only a buff present before the routine starts counts; the generic rule then applies (a kept instance is unconfirmed and stops the routine) |
| The execution route is neither shown on the card nor part of the accepted plan | Known limitation: the route is logged (`[KBP-CF-ROUTE]`); the card and the review signature do not include it |
| The compiler stored a strategy only when it differed, while the converter fell back to the pre-modifier option | Fixed in `3868c39`: the effective strategy is always recorded and used (also by the probe refusal) |
| Archive names could run out: after ten distinct originals, a readable pre-schema settings file would load as unreadable and block saving | Fixed: after the numbered names the bytes go to a name keyed by their own hash (reused for the same bytes); only a keyed name holding other bytes refuses |
| The mixed-coverage notes named recipients by raw unit id | Fixed: the card names the character (every existing-effect note) |
| Found live in the enhanced runs (`casting-qual-cast-20260924-adv-enh-*`): for Bull's Strength the workspace offered Powerful Change for Constitution, Dexterity, Intelligence and Wisdom too, because an enhancement with an empty spell list read as "any spell" (right for rods, wrong for class features) | Fixed: the focused casting and the draft offer an enhancement exactly when the compiler would accept it for the casting's providers; with no provider to judge, a class feature only for a spell it lists. A test models the live case (Powerful Change for a score no known spell raises is never offered) |

Mutation of the new logic: 31 C# mutants. 28 were killed, after two
tests were added for real gaps (`48b5402`: a recipient suppressed before
the cast is not kept coverage; a covered recipient outside the required
coverage is pre-covered) and two mutants that did not compile were
re-formed. Three survive, each a second check of something already
enforced: the group selection's forecast check (the earlier caster-level,
duration and coverage rules leave no candidate it could refuse); "every
other recipient is newly covered" in the mixed step (the executor's own
confirmation already requires a new or refreshed instance, so the step
fails earlier); and the mixed step's submission count (implied by the
step's states, and the host never resubmits). PowerShell: the allowance
recipe list is killed by the new allowance test (`6c0d2a7`); the staging,
request and allowance-profile mutants are rerun when no live run is
active (see the incident below).

Incident (recorded, not a product defect): at 12:52 on 2026-09-24 the
runtime-harness test suite was run for PowerShell mutation while a live
run (`casting-qual-cast-20260924-adv-finite-anim-01`, game result PASS)
was restoring. The suite's process-detection test starts a fake
`Kingmaker.exe`; the launcher saw it, refused the restoration as
designed, and the run ended incomplete. The guarded `Restore-Local.ps1`
restored the Mods folder byte-exact and released the lock (transaction
`Restored`, verified; saves compared clean). The case was run again under
a new id. The harness suite and the gate are not run while a live run is
active.

## Findings from the last review of the targeted fixes (2026-09-24)

A final independent read-only review of `b783f60..1688af2` found no high
severity problem; its findings were fixed in the commit after `1688af2`:
a merge into castings added in the session writes the session's
execution settings only when the player changed them there (otherwise the
classic plan's own settings are imported); closing an impossible
comparison tries it once more after the game is known to be closed; the
archive holds exactly the bytes parsed; a result kept from before an area
change is dropped while the interruption it reports is kept; an
interrupted run reports how many of its casts were confirmed; unreadable
violation records and acknowledgements, a record without its blocking
list and a lost lock are named for the owner. The Classic planner's
groupings are used with its plan at import time whether or not the plan
itself is used: they describe the party's sources, not the plan's bytes.

## Findings from the targeted review of the focused fixes (2026-09-24)

One more independent read-only review of `f47f527..b783f60`. Fixed in the
commit after `7654b70` unless marked.

| Finding | Disposition |
| --- | --- |
| Reserving one unit for an unverified zero cost made it look verified, so the review-M1 refusals could no longer fire | Reverted; the rule is pinned by a test |
| "It had not cast anything" was decided on submitted casts, which the animated executor never records | Decided on any cast put to the game (queued, submitted, started, spent or confirmed), tested |
| An unreadable baseline was moved aside before the record and the closed baseline were written | Copied aside first, the closed baseline written last |
| Closing an impossible comparison never tried it or checked the game had exited | Tries the comparison first, refuses while the game runs, records why it failed |
| A merge into unsaved castings adopted the Classic plan's settings | The session's own settings are written |
| An area change dropped the interruption result | Kept (the campaign check covers another save) |
| Messages naming commands that cannot work (a malformed record, a missing run, an unreadable acknowledgement, a lost lock) | Named as malformed for the owner; an unreadable acknowledgement is superseded; the lost-lock message says the Mods folder needs the owner too |
| The hash was of a second read | The bytes parsed are the bytes hashed |
| A newer plan dropped the warnings before it | Kept, and every unusable file is named |
| Groupings captured when the session was built | Read with the Classic plan at import time |

## Findings from the focused review of the re-review fixes (2026-09-24)

Three more independent read-only reviews of `b1b3736..f47f527` (FA casting
and the Classic path, FB UI and persistence, FC harness). Their findings
were fixed in `2bb91da` and the harness commit after it, except the ones
marked as limitations (release notes, "Known limitations").

| Finding | Disposition | Commit |
| --- | --- | --- |
| FB1 (high): a Reload after the player repaired an unreadable classic file imported the stale empty default the Classic planner held for it, and wrote it | The in-memory plan is read when the import runs and used only while the classic file is still the bytes it came from (recorded by the load and each save) | `2bb91da` |
| FB2: a Reload with no plan file blocked saving (and toggled on each press) | Such a reload is a first open again: saving allowed, castings added in the session never discarded | `2bb91da` |
| FB3/FB4: castings added while blocked were lost on other routes; the notice told players to move readable backups too | Castings added while the import was blocked are imported into and saved; only the named unusable files are to be moved; a reload that can replace unsaved changes asks for a second press | `2bb91da` |
| FB5..FB7: "not saved" missing while a backup stands in for an unreadable file; both single/group switches always offered and dropped recipients unnamed; a missing Classic file with unreadable backups unannounced | Header uses whether saves are refused; only the buff's own mode is offered and dropped recipients are named; a non-blocking notice | `2bb91da` |
| FA1 (medium, Classic): the Classic screen closed itself when reopened mid-run | Decided once at the press (`ClassicRunScreenPolicy`, tested); the flag reset on every end | `2bb91da` |
| FA2/FA3: a kept result crossed campaigns and modes and hid the notice; a waiting run was not shown; "interrupted" for a run that never cast | Scoped to its campaign in Classic mode, dropped on area change and mode switch, shown beside the notice; waiting shown in the tooltip and screen; worded by what was cast | `2bb91da` |
| FA4: an unverified zero cost on a finite pool reserved nothing while the budget counted one | Kept by design (review M1): it reserves nothing, shows short by one and is refused before any cast. Reserving one unit (`2bb91da`) made the cost look known and was reverted after the targeted review | reverted |
| FA5 (unverified): effects that reach pets as well as the party, a target or an area | Limitation stated (no test party has pets; next iteration with the advanced seed) | — |
| FA6: tests pinned text | Pure policy, wrapped-expression confirmation cases; the world-gate text check follows the new rule | `2bb91da` |
| FA7: APPLY now closes the Classic planner | For the owner to confirm (release notes and handoff) | — |
| FC1 (medium): a comparison that can never be made kept the Mods folder moved and the lock held with no documented exit | `Restore-Local.ps1 -RunId <run> -CloseUnverifiableComparison` records it as unverifiable for the owner's review and restores; an unreadable baseline is kept aside and named in every refusal | harness commit |
| FC2: a comparison was made even when the run no longer held its lock | Made only while the run holds its lock; otherwise closed as unverifiable | harness commit |
| FC3: the owner's typed confirmation was skipped for any root that was not literally the lab's | Skipped only for a harness test root reached without a junction or link; redirected input refused; the fixture's production checks use the same rule | harness commit |
| FC4..FC9: no-save scenarios recorded as clean; a stale acknowledgement could not be replaced; missing fields gave StrictMode errors; the Advanced recover hint; an unguarded working-save repair script; two lab-root definitions; scenarios outside the stop rule; untested launcher decisions | Recorded as not known; superseded by a new review; clear messages; hint added; script removed; one lab root; every scenario obeys the stop rule; the restoration decision is one tested rule | harness commit |

## Findings from the re-review of the rc4 fixes (2026-09-24)

Three further independent read-only reviews of the rc4 fixes
(`ea2a027..29213f8`): R-A (casting and the Classic path), R-B (UI and
persistence) and R-C (harness). Their findings were fixed in
`b1b3736..f47f527` except the ones marked as limitations, which the
release notes and the guide state. Each fix has a mutant the tests
catch: 5 C# mutants for R-A, 24 for R-B (one was equivalent: the line it
removed was redundant and is gone) and 24 PowerShell mutants for
R-C.

| Finding | Disposition | Commit |
| --- | --- | --- |
| R-A1 (high/medium, Classic): the world gate wrapped the whole Classic run, so an APPLY from the open Classic screen ran none of its checks until the screen closed, dropped refusals and results, and left the screen editable | Checks and acceptance at the press; only the casting phase waits for the world (the casting-first host's rule); an accepted run closes the Classic screen; a result that arrives while it is closed is shown when it opens | `b1b3736` |
| R-A2: a cast whose effect the game keeps as an existing instance (non-replacing stacking, a prolonged buff that outlasts the new one, a permanent effect) is reported unconfirmed and halts the routine | Known limitation, stated in the release notes; next iteration: stacking-aware planning or a distinct non-halting status | — |
| R-A3: a new area instance of the same effect id confirmed a buff | Only an instance of an expected leaf kind and effect confirms | `b1b3736` |
| R-A4: a party effect that also reaches pets was planned as pet-only (caster not a legal origin) | Party before pet | `b1b3736` |
| R-A5: a funded casting whose finite cost reads 0 requested nothing | Requested = max(demand, reservation); an unfunded linked pair still records one (limitation) | `b1b3736` |
| R-A6..9: pending parity, the import comment, test gaps, frame-counted windows | Parity and comment fixed with R-A1; the Classic route has no press-again-to-stop and confirmation windows count frames (next iteration) | `b1b3736` |
| R-B1: the remedy the blocked-plan notice gave did not work in the session | Reload unblocks once the unreadable file and its backups are gone (castings added meanwhile kept and saved; an empty plan imports the classic one); notice names the remedy | `4555c9d` |
| R-B2: a provider change dropped the same caster's enhancements silently | Same caster keeps them; another caster keeps its own and names the dropped ones (Undo restores) | `4555c9d`, `f6e5e4c` |
| R-B3: no focused single/group switch, so an imported grouping-unknown casting could not become Ready | Focused **Make it a group casting** and **Or a single target** controls | `4555c9d` |
| R-B4: provider labels never named the spellbook or level | Spellbook name (game display name, else class), spell level, kind of slot | `4555c9d` |
| R-B5: two sources differing only by spell level in one spellbook could not be pinned | Refused as not pinnable with a player reason; never shown selected | `4555c9d`, `7523d88` |
| R-B6: "" and no spellbook compared unequal | Normalized | `4555c9d` |
| R-B7: a loaded plan's removed id could be reissued | Ids seen at load and reload stay below every issued id | `4555c9d`, `d315fcd` |
| R-B8: the footer counted blocking reasons (drafts, import notices) | Counts this routine's castings short of a resource only in one pass | `4555c9d`, `7523d88` |
| R-B9/10: notices misjudged a missing file, stayed stale after a save, or vanished | Missing and unreadable told apart; statuses current after a save; header indicator while saving is blocked; player-worded refusal | `4555c9d` |
| R-B11: the import read the unrebound classic file | Imports the classic planner's in-memory plan of the same campaign; the file must still be readable and is archived | `4555c9d` |
| R-B12/13: draft enhancements kept on a caster change; no out-of-combat control; raw review codes | Filtered; toggle; review items in words | `4555c9d` |
| R-C1/2: the save snapshot was taken before the lock; a pending comparison could finish after the lock was released | Snapshot and WORKING check under the lock; the lock is kept until the comparison is made; Restore-Local refuses to skip a pending comparison, keeps everything when it cannot compare, and records a released run's comparison as unverifiable | `292d261`, `f47f527` |
| R-C3/8: fixture and runtime could interleave; fixture changes ignored pending comparisons and violations | Each side re-checks the other's lock under its own; teardown re-checks before deleting; fixture changes wait for comparisons and reviews | `292d261` |
| R-C4: the acknowledgement was not owner-only and loosely matched | Typed by the owner on the lab root; own folder; bound to the run and the record's bytes | `292d261`, `f47f527` |
| R-C5..11: stale lock message, no-save scenarios, completion failures, stop-rule gaps, docs, rehearsal flag, combined failures | Fixed as listed in the commit messages | `292d261`, `f47f527` |

## Findings from the final review of rc3 (2026-09-24)

Three independent read-only reviews of `fd0e6dc..ea2a027` (the rc3
candidate): A (domain, planning, execution, game adapters), B (UI,
persistence, Main and lifecycle) and C (harness, installer, runtime
testing). They found real defects, so rc3 was superseded by rc4. Every
finding below was fixed, and each guard has a mutant the tests catch: 30
C# mutants for the product fixes and 8 for C3/C5 (one of these survives
as equivalent: the rehearsal flag on another scenario is already refused
by the exact parameter count), and 24 PowerShell mutants for the harness,
each verified to fail the intended check.

| Finding | Disposition | Commit |
| --- | --- | --- |
| A1 (high, Classic regression): a Classic routine run while the game was paused, or from the open Classic screen, used up its casts' frame-counted confirmation windows, and the halting runner abandoned the rest | The Classic run advances only while the world runs (`WorldGatedEnumerator`), like the casting-first host; the open Classic screen says casting starts once it is closed | `49afb00`, `6b8bd9d` |
| A2: an empty recipient set confirmed vacuously; a caster-centred group casting whose caster is no legal origin predicted every reachable unit | The compiler blocks `origin-caster-illegal` and `predicted-coverage-empty` with player reasons; the converter refuses a step with no recipient; an empty set never confirms | `49afb00`, `40948d6` |
| A3: a cast was confirmed by the effect's presence, so a recast over an old, insufficient or suppressed instance "confirmed" though nothing landed | Both live adapters read each recipient's expected effects before submission and confirm only a new or refreshed, unsuppressed instance (`AppliedEffectJudgement`); an unreadable read means nothing is cast | `49afb00` |
| A4: a linked prepared pair counted as one requested slot, hiding a later shortage | The budget records what a casting reserved | `49afb00` |
| B1 (high): the first-open import refused every Classic plan saved by 0.0.19 (schema 4) | Import reads through the Classic loader's own in-memory migration; the file is not rewritten; the live import seed is a genuine schema-4 file | `49afb00` |
| B2 (high, usability): a focused casting could not change caster, source, routine, order or recast policy, so imported automatic castings could never become Ready in place | Focused editor: caster and exact-source choices, routine and order, recast toggle; draft recast toggle; the plan's animated-fallback switch | `49afb00`, `7eb5eeb` |
| B3: Add was refused for a caster with two ways to cast a buff, with no picker | The draft's **Cast from** picker; the focused casting can switch source | `49afb00` |
| B4: load and save failures were never shown | Both planners show an unreadable or newer file, a loaded backup and a refused save | `49afb00` |
| B5: casting-first refreshes could rewrite the Classic file | No Classic save while casting-first is active (checked when the save happens) | `49afb00` |
| B6: the spellbook button waited for the Classic screen in casting-first mode | It waits for the workspace | `49afb00` |
| B7: the HUD tooltip could describe another campaign; a removed casting's id was reused (its card showed the old last run); the whole-plan forecast was never shown | Campaign-checked tooltip and press results; ids never reissued in a session; the footer shows a one-pass shortage | `49afb00` |
| C1: a protected-save violation was dropped when the run also failed, and later runs were not blocked | Every failure reported together; the violation recorded; later runs refused until the owner acknowledges it (`scripts/Confirm-KbpProtectedSaveReview.ps1`, owner-only) | `29213f8`, `edeffbd` |
| C2: the protected-save baseline lived only in launcher memory | Kept beside the transaction; compared before the lock is released; a pending comparison is an unresolved transaction that `Restore-Local.ps1 -RunId` finishes | `29213f8`, `edeffbd` |
| C3: the launcher could abandon a run the host was still inside its own limits for | The host stops at `-TimeoutSeconds` from the game's start less 10-45 s, and at once on the launcher's abort marker; the launcher then waits a bounded grace | `29213f8` |
| C4: the allowance writer could not produce a finite-direct-mixed allowance; the request document was stale | Recipe from the selection, a purpose per recipe, budget = the forecast castings; document corrected | `29213f8` |
| C5: a manual rehearsal looked like the owner's session in its records | Labelled in the request, orchestration and completion records, and the host's outcome | `29213f8` |
| C6: the launcher trusted the host's probe PASS | The launcher reads `probe-outcome.json` | `29213f8` |
| C7: the rollback's lock check was blocked by any game; `orchestration.json` kept a stale status; the launcher skipped host parser rules | Root-scoped check; status is the final verdict; the host's shape rules before launch | `29213f8` |
| C8: fixture lock and lease rechecks, held keys, a missing completion record, an unbound process-id list | All fixed | `29213f8`, `edeffbd` |

## Findings from the batch-3 review round (2026-09-24)

Three independent read-only reviews of `ae0181d..30c8483`, each given the
diff, its callers, the requirements and the evidence: A (discovery, source
resolution, budgets, native execution, effects), B (UI, lifecycle,
persistence, migration) and C (harness authorization, observations,
terminal state, restoration). Every finding below was fixed before rc3,
and each guard has a mutant that the tests catch. A focused re-review of
the fixes by two further independent read-only reviewers (source
resolution; harness and lifecycle) found the issues in the last rows,
which were fixed and mutation-tested the same way.

| Finding | Disposition | Commit |
| --- | --- | --- |
| The Classic route (the default mode) kept casting after a failed or uncertain cast; it had no owned terminal (a disable or area change stopped only the casting-first host, and teardown skipped the executor's cleanup); the test grant stayed armed when unused | Classic routines run through `HaltingPlanRunner` (one step at a time, stop at the first unconfirmed step, the rest reported as not attempted); the planner root owns the Classic run and ends it through its terminal on disable, area change and teardown; the grant is disarmed at every end | `6cc75d9` |
| Resolution chose free or paid by the entry, not the plan: a free cantrip whose ability was missing could be paid from a slot, and a paid level-0 casting could turn free | The step's reservation routes the cast: free only at will (a missing ability is refused), finite never at will, a free reservation for a slot entry refused; probes read the step's own source | `b3723c6` |
| A fact-granted ability could resolve to a source of the other cost kind or another pool | Resolution keeps the provider's kind, skips spellbook-bound abilities and, for a step, uses exactly the reserved pool (same key as discovery); provenance records pool, fact and equivalents | `b3723c6`, `09b349d` |
| An ambiguous at-will cantrip was repriced as a level-0 slot | It is offered as no provider; the refusal stays in the discovery trace | `b3723c6` |
| An unreadable cast count looked like the game's "unlimited" (-1) | Unread is null; a free casting whose counts are unread or finite fails as uncertainty | `b3723c6` |
| The at-will lookup asked every ability for availability, count and caster level | Only a cantrip of exactly the authored ability is judged | `b3723c6` |
| The plan repository overwrote a primary that parses but that its own load rejects; refused saves were silent | Such a file is kept byte-for-byte and the save refused; Save and Accept show the refusal in player words | `ce3311a` |
| The display-mode registry snapshot lived only in memory; restoration could move the Mods folder under the other lab's lease or delete a changed staged tree; entry gaps; a skipped save comparison could pass | Snapshot file restored under the lock, refused while unrestored; the lease is waited for; a changed staged tree is kept; entry gaps closed; a skipped comparison fails | `275fa59` |
| The physical scenario could pass with a programmatic open, typing into an unfocused field, "selecting" an already selected tile, or a wheel over a grid that cannot scroll | Physical open required; typing only into the focused field; the query names an unselected tile and the click must change the selection; wheel evidence labeled honestly | `b3ab18d` |
| The instant disable step was described as an in-flight interruption ("atomic within one pump") | Labeled disable-before-start in the driver, record rule and receipts; no in-flight instant interruption is claimed | `b03e16f` |
| The disable was enabled again in the same frame and the lifecycle comparison passed without a probe | The disable goes through the mod toggle's own path and is held for five updates (nothing may run or be accepted); a real probe line is required | `b03e16f` |
| The Classic allowance did not bind the profile, the WORKING save or a purpose | Classic schema 2 and qualification schema 5 bind profile, identity digest, WORKING save and purpose; the launcher checks them against what it resolved and the host re-checks; an in-repo writer produces them and is tested against those checks | `ec52531`, `f93f0ad` |
| The launcher trusted the host's PASS for Classic and physical runs | The launcher reads `classic-outcome.json` (digest, mode, grant used once, every step confirmed) and `physical-workspace.json` against its own acknowledgements | `953c511` |
| Re-review, high: after the A2 change the probe observer lost a consumed reserved prepared slot, so a finite qualification with a prepared caster could not pass | Observation reads the reserved slot even when consumed (it is still the source); execution still needs it available | `f3b778e` |
| Re-review: one cantrip granted by two classes also reached the planner as a fact-granted ability, bypassing the A4 refusal | One ability from one pool at two caster levels is ambiguous: discovery offers neither and execution refuses such equivalents; execution walks abilities in discovery's order and records the caster level | `f3b778e` |
| Re-review: unread counts on a paid source passed as "nothing spent"; the probe accepted any unchanged count as free; at-will provenance was thin | Both are uncertainty; one free rule everywhere; the provenance names the class ability, its caster level, the candidates and the reserved pool | `f3b778e` |
| Re-review: a failed disable rule still let the recover run submit; the probe allowance was unbound; a run ending during the hold left the planner disabled; the hold only read flags the disable itself set | The rules apply at the disable step; probe schema 3 carries the binding; the planner is enabled again at finish unless the mod manager disabled it; the hold observes the root's own ticks and the host's started runs | `31e56d7`, `af1b051`, `f903e69` |
| Re-review: the launcher accepted allowances the host refuses only after launch, took physical facts from the host, and left a stale PASS in `orchestration.json` | The host's format rules in the launcher; exactly the eight requests, the hotkey it sent, its own acknowledgements; the final verdict written after restoration; optional mods verified before any cast; every key the launcher reads pinned to the host | `31e56d7`, `9e55e68` |

## Findings from live qualification after rc1 (2026-09-23)

The animated-mode qualification (the player default) found a defect in the
frozen rc1 that source tests could not see. It is fixed at `70f135a`.

| Finding | Disposition | Evidence |
| --- | --- | --- |
| In animated mode, the game's own cast command failed every spontaneous caster's cantrip: the planner submitted the spellbook's level-0 entry, and the game casts a cantrip at will only through the ability its class grants for it (no spellbook, count -1); the spellbook entry needs a level-0 slot and these books have 0 per day. Instant mode cast the entry only because the cast rule skips the availability check | A level-0 entry executes through the caster's at-will cantrip ability first (both modes), exactly as the game's action bar; validation is the cast command's own `IsAvailable`; discovery prices level 0 as free only with that ability behind it (otherwise a level-0 slot or a consumable prepared slot) | Failure `casting-qual-cast-20260923-a1-anim-01` (halted after the failed cast: nothing spent, nothing else started); diagnostics `casting-qual-select-20260923-d1-01`; the game's `UnitUseAbility.OnAction`, `AbilityData.IsAvailable`, `Spellbook.SpendInternal` read from its assembly |
| The qualification's stop was a host cancel between castings, not the player's stop | The stop is the player's routine press through the HUD's routine entry while the first cast is in progress (the host's `RequestStop`); the cast completes and nothing after it starts | Source and mutation tests; in game on the fixed build |
| The qualification used a private host its driver pumped | It runs on the planner's own host, pumped by the planner root once per frame while the world runs; the root's tick precedes the test host in each mod update, so the press lands before the next pump | Source checks; in game on the fixed build |
| No in-game interruption of a cast in progress | Disable step: the planner's own disable and enable during a run; animated: while the cast is in progress (interrupted, cleaned up, nothing lands); instant: before the first step (nothing submitted); the host accepts runs again once enabled | Tests with an interrupted cast that lands anyway and a host that never resumes; in game on the fixed build |
| An allowance did not state the casting mode | Schema 4 names instant or animated; the launcher, request protocol, host and boundary each refuse any other mode | Tests; 21 mutants killed |

## Previous review dispositions — independent review of `1332ed8..542cd66` (answered 2026-09-23)

A read-only review of the in-game reload, the grid order, the version bump
and the docs found no P0. Both P1s concern the reload's save safety; no
run wrote a save (the WORKING hash stayed the same across the runs), but
the guarantee rested on the wrapper's counters alone.

| Finding | Disposition |
| --- | --- |
| P1-1: the reload restored the writable native saver after the header commit but before the load finished, with no write sentinels | The read-only saver stays until the reload's after-load callback; the first load's write sentinels (SaveRoutine, SaveStashedArea, DeleteSave, RemoveSaveFromList) are installed for the reload window together with a correlation hook on Game.LoadGame; any write, a foreign or repeated load, or a missing correlation fails the run; the hooks are removed on success and failure; rejected header updates and commits are recorded before they throw |
| P1-2: the harness still allowed the WORKING save to change in the reload scenario | The reload and import scenarios allow no save change and block new save files; the reload needs `-TimeoutSeconds` of at least 600 and has its own live budget |
| P2-1: three reload checks could not detect their failures | The saved plan is read back from disk as a fresh session would and compared with what was saved; exactly one area unload and one loading-complete delivery are required; HUD roots are counted including inactive ones; session reuse is reported |
| P2-2: the highlighted first buff was not the draft's buff | The first buff is committed as the selection, and a draft authored without a click uses it (it would otherwise have been saved as "unsourced") |
| P2-3: one version label, several builds | The candidate is frozen at one commit; its receipt pins the identities and repeats the runs on it |
| P2-4: release-note evidence errors | Corrected (the failed s1-01 run, per-run build checks, which frames showed what) |
| P2-5: tests weaker than claimed | The grid test uses three buffs whose ids sort against their names and adds without a click; the reload source check pins the callback gating and the sentinels; the family refusal asserts its reason |
| P3 | Failure evidence is flushed by the host's timeout and every loader failure path; the reload runs only after a passing authoring and reopen; grid names sort culture-aware; the scale evidence reads the native root canvas with invariant formatting |

## Previous review dispositions — independent review of `f7726c9..1332ed8` (history)

A second read-only review of the fixes above found no P0, P1 or P2 issue.
All ten P3 items are addressed:

| Finding | Disposition |
| --- | --- |
| P3-A: a restoration failure in a failed run was only a console warning; the completion record ignored it | `run-completion.json` carries `restorationFailure` and is never verified while it is set; the text is saved as `restoration-failure.txt`; the launcher holds the run's own error until the records are written and folds the restoration failure into it (a restoration failure and a save violation are reported together) |
| P3-B: the harness source check could not catch a `throw` in the restoration branch | The check extracts the restoration branch (no `throw`, no `Write-Error`, both paths assign the failure) and pins the save policy feeding the comparison, the failure reaching the record and the held run error |
| P3-C: the header-count test did not prove routine-wide counting | The test adds the same buff to another routine: Long counts 2 while the buff shows 3 cards |
| P3-D: enhancement and material footer rows read "resource"; labels collided; a shortage could be truncated | Rows are named by kind (enhancement pools by owner and pool name, material components, ability pools by their source); same-looking finite pools are numbered; the footer lists shortages first and merges free pools |
| P3-E: some refusals lost their advice | `targeting-requires-direct-target` and `targeting-invalid` are mapped; shape refusals are keyed on the mod's own messages |
| P3-F: the editing label fell back to "Editing one casting" after a buff switch | The focused casting is found in the whole plan |
| P3-G: the probe's `Pump` defaulted to a running world | The world state is a required argument |
| P3-H: the probe's time budget had no launcher minimum | Probe scenarios need `-TimeoutSeconds` of at least 600 |
| P3-I: the matrix overstated tab clicks, the player stop and the production pump | Worded as callback coverage, the host's immediate cancel and "not in game" for the production pump |
| P3-J: `ValidateSet` binds case-insensitively; `live-cast-probe` had the loose save policy | The launcher continues with each value's canonical spelling; the probe allows no save change |

## Previous review dispositions — independent review of `e7c5207..f7726c9` (history)

A read-only independent review of the RC1-RC4 fixes, the world-running
gate and the external Gunslinger fixture found no P0 or P1 issue. Fixed in
`548a90d` (each with a caught mutant):

| Finding | Disposition |
| --- | --- |
| P2-1: the probe checked the world once, a frame before its rule fired, and pumped its confirmation frames without a check; the wait was bounded by 300 updates | Every probe pump (the fire and each confirmation frame) waits while the world is held; a stop and the wall-clock deadline still apply. The record carries the world state at the first step and the held pump count; the wait before submitting is bounded in elapsed time (`ProbeWorldWaitSeconds`) |
| P3-1: the production world clock truncated each frame to whole milliseconds | `CastingWorldClock` accumulates double seconds; unit-tested at 60 fps and above 1000 updates a second |
| P3-2: the qualification host deadline counted held time although this index said it did not | The qualification host uses the same world clock as production; the run's own 240-second deadline stays wall-clock as the hard bound. A driver test holds a step for 200 s: it completes under the world clock and fails under a wall clock |
| P3-3: a failed restoration or a still-running Kingmaker skipped the protected-save comparison and `run-completion.json` | Restoration failures are captured and no longer skip `run-completion.json` or, once the game has exited, the protected-save comparison. Since the follow-up review (P3-A) the record carries the failure (`restorationFailure`), it is saved as `restoration-failure.txt`, and the launcher folds it into the run's own error |
| P3-4: an inspection that changed the WORKING save was recorded complete but could never qualify | The casting selection and every advanced-copy run, the inspection included, allow no save change |
| P3-5: several tests were weaker than their commits claimed | The launcher computes its completion record and save policy through tested functions; the held-world driver tests cover the Begin and Wait gates separately; the source checks test the gate blocks themselves |

## Previous review dispositions — Pro review of `e7c5207` (history)

| Finding | Disposition |
| --- | --- |
| RC1: native slot ids (`level-N\|type-T\|index-I`) contain the "\|" the token evidence was joined and split on | Fixed in `2a443c3`: typed readings (casting, opaque token id, before, after) and a list of castings read on one side only; the judge reads them directly and text is presentation only. One `PreparedSlotIds.Format` serves the snapshot and the cast adapter, and the tests build native ids with it: linked pairs, wrong-token spend, partial consumption, missing readings |
| RC2: a failed before-read did not stop the step before Apply | Fixed in `2a443c3`: every casting the step observes needs a succeeded read of its own target with effect, availability and every reserved token; otherwise the run ends before Apply with zero submissions. Driver tests: observer exception, failed read, null read, wrong target, missing prepared tokens |
| RC3: an in-game PASS could authorize advanced casting before restoration and the protected-save check | Fixed in `1af543c`: the launcher writes `run-completion.json` last (game result, harness success, owned exit, verified restoration, protected saves compared and clean, fixture files and hashes, campaign, binding manifest, compatibility identity); only a complete record of the same pair and identity qualifies. `harness-error.txt` is written only for failed runs. The 900-second floor is documented as a time budget with truthful recovery |
| RC4: zero-cost selection banned both casters as recipients and so needed four units | Fixed in `1af543c`: recipients are chosen per casting (never the casting's own caster); a three-unit party qualifies, end to end. The record now carries the roster the selection saw |

**Live finding (not from the review).** The first one-cast probe on the
current candidate (`casting-probe-cast-20260923-p1-01`, frozen `7664f2f`)
submitted its rule while the casting workspace was open. The workspace
lease holds the game in `FullScreenUi`, where queued ability execution
does not advance, so the Resistance effect never appeared
(`TimedOutUnconfirmed`). One invocation, no retry; restoration and the
protected-save check passed. Fixed in `320a1b6`: the probe closes the
planner and submits only once the world runs (Default mode, not
paused), the production host and the qualification driver advance only
while the world runs, and the host deadlines count only running time
(the qualification host since `548a90d`).

## Previous review dispositions — review of `54d330b..47caeef` (history)

A second independent read-only review covered the qualification core,
driver and scenarios, the run request and the RC review fixes. Every
finding is fixed in `af5f0ca` with a regression test and a mutant the
tests catch. Full gate at `af5f0ca`: source 42/42, protocol 286/286,
harness 33/33, deploy WhatIf 5/5, launcher WhatIf 11/11, fixture 3/3,
Restore-InstallLocal 16/16, publisher 3/3. No qualification run has
happened, so no native cast was affected.

| Finding | Disposition |
| --- | --- |
| P0: the driver judged the steps only at the end, so after a failed, uncertain or cancelled step (an unconfirmed first cast, a spend on a free source) it kept submitting | Fixed: each step is judged by the record's step rule the moment it ends, and repeat continues only on the exact no-op; anything else ends the run before another submission. Tests drive an unconfirmed cast, a spend on a free source and a refused repeat: each run fires exactly the casts before the failure |
| P2: the harness could time out and abandon a live qualification run with the Mods folder unrestored | Fixed: the launcher refuses a qualification scenario with less than 900 seconds; the documented commands carry it |
| P3: an exhausted rod could hide an incompatible enhancement set | Fixed: compatibility counts exhausted required enhancements |
| P3: the selection-only assertion was vacuous | Fixed: it requires zero qualification runs, the locked player routes, zero production runs and a closed workspace |
| P3: the reopen check accepted any stored acceptance | Fixed: the reopened session must restore exactly the digest just accepted |
| P3: judge and evidence gaps (cleanup failures, other targets on recast, interrupted steps, shutdown) | Fixed: step rules check cleanup failures and the other targets; interrupted steps keep their report and reads; shutdown publishes the record |
| P3: the workspace close result was ignored | Fixed: an unclosed workspace or a held input lease fails the run before any authoring |
| P3: guide wording ("more than two rounds") | Fixed: "at least two rounds" |
| P3: the protected-save check tolerated a changed WORKING save for the casting run | Fixed: a casting qualification must leave every save unchanged and create none |

After the fixes, `finite-direct-mixed` (`ebf2329`) adds the
advanced-copy recipe for finite prepared slots, spontaneous levels and
metamagic variants, judged per step down to the exact prepared tokens.

## Previous review dispositions — RC review of `ca0d636..325e4b3` (history)

An independent read-only review of RC slice 1 found no P0. Every finding
is fixed in `47caeef` with a regression test; the eight new mutants and
two scenario-drift mutants were caught. Full gate at `47caeef`: source
42/42, protocol 285/285, harness 33/33, deploy WhatIf 5/5, launcher
WhatIf 11/11, fixture 3/3, Restore-InstallLocal 16/16, publisher 3/3.

| Finding | Disposition |
| --- | --- |
| P1: `live-advanced-inspect` could not build its request (the request builder repeated an older scenario list) | Fixed: the builder accepts the three new scenarios; the harness builds a request for every launcher scenario and requires identical sets |
| P2: an exhausted rod blocked (required) or re-shaped (optional) a casting whose effect was already active | Fixed: an exhausted required enhancement is a resource shortage that a sufficient live effect waives; the skip keeps the would-be cost shape, so the digest is unchanged |
| P2: opening the planner revoked a stored acceptance on a temporary difference, and saved that | Fixed: presenting never revokes (an acceptance only ever authorizes its exact digest); viewing writes no review state |
| P2: casting-first refusals were invisible in game | Fixed in RC slice 1 (`7e864e7`): refusals are kept per routine and shown on the HUD tooltip and planner footer |
| P2: nearly expired effects counted as sufficient when durations were not comparable | Fixed: under two rounds left is never sufficient, whatever the duration text |
| P3: the stop tooltip promised a graceful stop the host did not provide | Fixed: the player stop now lands after the cast in progress; area change, disable and teardown still stop at once |
| P3: lock comments overstated coverage (the classic path still cast in automation) | Fixed: the lock also refuses the classic routine execution; `live-ui-bootstrap` asserts that refusal |
| P3: the HUD tooltip called a stale stored digest "accepted"; execution settings outside the signature were undocumented | Fixed: the tooltip states current / on file / changed; the signature comment and player guide say why the Animated/Instant choice is not signed |
| P3: coverage gaps | Closed by the tests listed above |

## Previous review dispositions — O1 at `e964d2f` (history)

The frozen probe checkout and artifact (`e964d2f`,
`runtime-backups/probe-frozen/e964d2f…/`) are unchanged. Development now
continues in a separate worktree on the same PR branch; the original
checkout is detached at `e964d2f`.

**O1 (P2) — a refused fixture retry deleted prior evidence before preflight.**
Pro found this by source inspection; it was not reproduced under Windows
PowerShell.

- **Location:** `scripts/New-KbpAutomationFixture.ps1`, in the bootstrap
  branch that handles an existing `$runRoot`.
- **Behaviour at `e964d2f`:** when the prior transaction was
  `RolledBack`, the script ran `Remove-Item -LiteralPath $runRoot -Recurse -Force`.
  That happened before `Assert-KbpFixturePreconditions`, before the seed
  lookup and validation, and before the outer `$PSCmdlet.ShouldProcess`
  decision.
- **Failure sequence:** a real, non-WhatIf retry reusing a rolled-back
  RunId deleted the old run directory. A prerequisite then failed (for
  example, a missing `KBP_ADVANCED_SEED`, a running game or an active
  deployment), so the retry was refused but the previous
  transaction and evidence were gone.
- **Scope:** the path is inherited and is now shared by `-Family Advanced`;
  the family parameter did not introduce it. The affected data were the
  old fixture transaction and evidence, not ordinary saves. `-WhatIf`
  itself did not delete these files; the finding is the mutation during a
  real retry, ahead of the prerequisites and the outer decision.

**Disposition (the preferred repair):** the script never removes an
existing run directory, whatever its status. A same-ID retry is refused
before anything else happens, the prior attempt is kept as recovery
history, and a new attempt needs a fresh `-RunId`. Interrupted-run,
`-Recover` and lock guards are unchanged.

**Isolated regressions** (`Test-RuntimeHarness.ps1`, the real script
against temporary roots, with byte snapshots of the save, state and
archive roots):

| Case | Result |
| --- | --- |
| A | A same-ID retry, both the Advanced family without an advanced seed and the automation family, is refused; the snapshots are unchanged. |
| B | With a Kingmaker-named process running, a same-ID attempt and a valid fresh attempt are both refused. The fresh one is refused by the game-running precondition itself. No state, evidence, archive, save or lock change. |
| C | `-WhatIf` and a genuinely declined confirmation ("N" on the prompt) on a fresh ID both reach the outer decision and mutate nothing. |
| D | A fresh attempt completes. The prior history is byte-identical, the seed and the unrelated and other-family saves are unchanged, and teardown removes exactly the owned pair. |
| E | The rolled-back attempt's retained evidence and an unexpected nested file survive every path. |

Three existing harness tests had asserted the old "same-ID retry after
rollback works" contract. They now assert both halves of the new one:
the same-ID retry is refused with the record byte-identical, and a fresh
ID succeeds. A mutant restoring the old delete-on-RolledBack behaviour
fails case A.

## Previous review dispositions — N-series at `7379dc8` (history)

No cast was run; normal dispatch stays disabled. Source and recording-runtime evidence only.

| Finding | Disposition | Commit | Tests (mutants caught) |
| --- | --- | --- | --- |
| N1 — approval did not bind the frozen artifact before execution | Allowance schema 2 binds commit, package, DLL and MVID; the launcher refuses a manifest mismatch before deploy; the owner runs the boundary preflight first, which measures the LOADED commit/package/DLL/MVID and refuses any mismatch with no observation, executor construction or submission | `b9721c3` | `probe-requires-frozen-artifact-identity` (3/3), 6 launcher binding cases |
| N2 — shutdown was not terminal before the owner existed | The owner is created with the runtime host; after Shutdown, BeginSelection/Submit refuse and Update completes once as a shut-down failure before any work | `b9721c3` | `probe-shutdown-before-selection-is-terminal` (3/3) |
| N3 — prepared-slot observation required a spendable slot | `ProbeSourceSlots.ReservedExactly` reads exactly the reserved tokens, consumed or not, never substituting; a prepared cast is judged by those tokens going available to consumed | `b9721c3` | `prepared-slot-observation-reads-exact-source` (3/3) |

Advanced-copy tooling (`bdac45f`): `New-KbpAutomationFixture.ps1 -Family Advanced`, isolated test only.

## Previous review dispositions — M-series at `1e3c95b` (history)

Normal dispatch stays disabled; no cast was run. Evidence is source and
recording-runtime tests plus selection-only live runs (no boundary
constructed).

| Finding | Disposition | Commit | Tests (mutants caught) |
| --- | --- | --- | --- |
| M1 — zero-cost native sources could not be projected | A known Unlimited pool is one zero-unit native demand reserved by the existing ledger and flagged verified; flag carried by cost line, budget line (shown "unlimited"), converter, projection identity v3 and both executors; unknown/stripped/missing costs refused; spend on a free source fails; feature costs still count | `a023293` | `zero-cost-native-source-integration` (5/5); four legacy fixtures moved to finite pools because their runtimes report a spend |
| M2 — probe after-state came from cached UI discovery | Fresh native reads (ability availability, target buff instances + end time) stamped from one sequence; failed reads recorded, never replaced; before < submission < after; new instance or verified refresh; free = zero change, finite = exactly one; missing ≠ zero; target without the effect preferred | `0a4c382` | `probe-observations-are-authoritative` (6/6) |
| M3 — host did not own probe cancellation | `SingleCastProbeRunOwner`: one idempotent terminal (dispose boundary, fresh after-read, close workspace, release lease, record cleanup, publish once) for completion, stop, deadline, host exception, disable and unload | `0a4c382` | `probe-owner-terminal-cleanup` (3/3), `probe-owner-wiring-in-host-and-main` (1/1) |
| Selection finding | Discovery wraps effects in a self-reference; accepted only for the cast ability itself | `ea45807` | `probe-scope-enforces-whole-subset` (1/1) |

Concrete, unapproved cast proposal: `docs/LIVE-CAST-PROBE-REQUEST.md`.
Advanced-copy inspection request: `docs/ADVANCED-SAVE-COPY-INSPECTION-REQUEST.md`.

## Previous review dispositions — L-review at `475d2b9` (history)

Native dispatch remains disabled in the production workspace. Every row
below is **source/isolated-test evidence only** (protocol suite, isolated
PowerShell fixtures, mutation checks); no row claims gameplay. The
single-cast probe is prepared but has **not** cast anything.

| Finding | Disposition | Commit | Tests (mutants caught) |
| --- | --- | --- | --- |
| L1 — imported constraints bypassable (Ready Casts Only; generic Mark Ready) | Pending plan-wide notices refuse EVERY apply mode in the gate and the session; a refused gate decision never reaches the boundary; unresolved per-casting review items block compiler readiness whatever the state; Mark Ready/Add/Update refuse Ready with unresolved review; content edits cannot change provenance; explicit, undoable, disclosed ResolveImportReview / AcknowledgeImportNotices record resolution beside the kept history (persisted); inspector controls added | `58a1aaf` | `import-requirements-stay-enforced` (6/6) |
| L2 — cancellation did not dispose the active executor | The coordinator owns the nested iterator: disposed exactly once on exhaustion, failure and OUTER disposal; acquisition/MoveNext/Current/Dispose failures halt without replacing the first failure; cancelled runs report once; Processed vs NativeSubmissionReported. Animated executor now reports abandonment and cancel-cleanup failures (reporting only) | `2929985` | `explicit-run-cancellation-disposes-executor` (6/6) |
| L3 — ProjectionId omitted executable fields | Versioned (identityVersion 2) canonical JSON of every step field in order, incl. token ids, enhancement pool usage, source id, expected effect tree, omitted enhancements; set-like values normalized; unrepresentable effect refuses; canonical text exposed | `cd5bc4a` | `projection-identity-is-complete` (6/7; the survivor is equivalent — CastStep already sorts recipients) |
| L4 — partial swap could release an Installed record | Each destructive substep tracked; only a verified byte-identical return yields Installed/not-applied; a failed second move with failed reversal records RollbackRecoveryNeeded with paths/identities and keeps the lock; lock released only after a clean state is recorded | `aa3b95c` | `Test-RestoreInstallLocal.ps1` 16/16 (2/2 script mutants) |
| L5 — file-name scan is not format compatibility | Schema-6 format revisions (1/2/3); writer stamps formatRevision 3; repository refuses newer revisions; the assembly declares `KingmakerBuffPlanner.CandidateProfileFormat = 6.3`, read from the restored binary in a separate reflection-only process; candidates kept only if every member belongs to a revision the target reads; undeclared targets keep none; byte-exact archives | `aa3b95c` | `Test-RestoreInstallLocal.ps1` (4/4 script mutants), `rollback-candidate-format-members-match-model` |
| L6 — probe scope admitted excluded categories | Converter refuses non-spellbook/special source, metamagic, provider/caster mismatch, self-target, unverified target, non-DirectRuleCast strategy, non-plain effect shape; undefined scope enum refused | `cd5bc4a` | `probe-scope-enforces-whole-subset` (5/5) |
| Probe preparation | Dormant selector, strict owner allowance, default-refusing one-shot boundary, gated `live-cast-probe-select` / `live-cast-probe` scenarios; request `docs/LIVE-CAST-PROBE-REQUEST.md` | `6f22a78`, `2117438` | `single-cast-probe-is-dormant-and-one-shot`, `probe-scenario-request-validation`, `single-cast-probe-run-record-rules` (8/8), launcher gating 4 cases |

The first live cast is **not** ready: two selection-only live runs found no
eligible casting on the WORKING fixture (cantrips only). See
`docs/LIVE-CAST-PROBE-REQUEST.md`.

## Previous review dispositions — K-review at `258a1d0` (history)

Native dispatch remains disabled. Evidence for every row below is
**source tests only** (protocol suite / isolated script fixtures); no row
claims gameplay.

| Finding | Disposition | Commit | Tests |
| --- | --- | --- | --- |
| K1 — a failed legacy import activated an empty plan / default replacement | A failed import blocks the workspace with a visible reason: no candidate written, Save and Apply refused, old plan not replaced; legacy `ProfileRepository.Save` quarantines an unresolved primary instead of replacing it (explicit `ReplaceUnresolvedPrimary` recovery) | `e0509fe` | malformed primary, newer primary + valid backup, archive/candidate write failures, retry after repair, legacy save refusal/recovery |
| K2 — import ids could collide | Ids `m5:<routine>:<child>:<recipient key>`; reuse only on exact persisted provenance; any other collision refuses the import (K1 blocks) | `0c7b760` | duplicate child ids across routines, repeated import, unrelated collision, earlier-format reuse without renumbering |
| K3 — unresolved legacy constraints were dropped | Unknown grouping, provider pins, enhancement requiredness, automatic/missing casters and target-less children become Draft castings with durable review items; bans/caps/priorities become plan-wide import notices that block Apply until acknowledged (undoable) | `0c7b760` | unknown grouping, pin, optional enhancement, priority/cap, target-less child, save/reopen, Apply refusal + acknowledgement |
| K4 — converter could drop contracts it cannot carry | Standard scope refuses the whole projection for an enabled targeting modifier, an exact-source enhancement, or incomplete required group coverage; `SingleCastProbe` scope admits one plain direct casting only; deterministic `ProjectionId`; the dispatch boundary receives the exact projection | `b69f873` + probe-case tests | `converter-refuses-unsupported-contracts` (every probe refusal mutation-checked) |
| K5 — a failed/uncertain cast must stop later submissions | `ExplicitCastingRunCoordinator` halts after any casting not positively confirmed, marks the rest NotAttempted, never retries; optional submission limit | `20bbe82` | `explicit-run-stops-after-failure-both-modes` (instant + animated, five failure kinds each; limit 1) |
| K6 — install rollback could mis-record state after a post-swap failure | Prior build prepared and verified in staging (backup never modified); RollingBack + phase recorded before each transition; post-swap failure reverses the swap; unrecoverable reversal records RollbackRecoveryNeeded and keeps the lock; candidate compatibility read from the restored binary; candidates from both sides archived | `b243b02` | `Test-RestoreInstallLocal.ps1` 12 isolated cases incl. injected failures at candidate-archive, settings-merge, identity, verify, record and reversal |
| K7 — selected-buff coverage depended on the lane scope | Coverage computed from every casting of the selected buff, independent of the this-buff/whole-routine toggle | `93408bd` | scope toggle changes cards only, never coverage |

First live cast: **request prepared, not executed** —
`docs/LIVE-CAST-PROBE-REQUEST.md`.

## Previous review dispositions — J-review at `19ecbe8` (history)

| Finding | Disposition | Code | Tests |
| --- | --- | --- | --- |
| J1 — automatic evidence producer/consumer adjacency mismatch | Repaired: structured record; the host fills it through `WorkspaceCastStepEvaluator.Evaluate` and accepts it only through `WorkspaceInteractionRecord.Violations()`; `saved` is observed via `!IsDirty` | `src/KingmakerBuffPlanner/RuntimeTesting/WorkspaceScenarioContracts.cs`, `RuntimeTestHost.cs` (`UpdateWorkspaceInteraction`, workspace result block) | `workspace-interaction-evidence-contract`, `workspace-cast-step-evaluator-real-session`, `runtime-host-scenario-contract-wiring` |
| J2 — manual terminal accepted capture by filename, ignoring failure/restoration | Repaired: `ManualTerminalCoordinator` — bounded 20 s capture wait, failure + restoration verdict consumed, close postcondition (view + input lease) always recorded, four separate manual assertions; camera routine now always reports `RestorationClean` | `WorkspaceScenarioContracts.cs`, `RuntimeTestHost.cs` (phases 31/32, manual result block), `MenuRenderDiagnostic.cs` | `manual-terminal-policy-done-stop-deadline`, `manual-terminal-capture-restoration-cleanup`, `runtime-host-scenario-contract-wiring` |
| C1 — stale current-state summaries | Updated: tracker CURRENT STATE section, this index, `AUTONOMOUS-RESUME.md`, PR body; history kept | `planning/CASTING-FIRST-MIGRATION-STATUS.md` | — |
| C2 — rehearsal-6 evidence distinctions | Raw records re-verified; sanitized receipt published | `docs/evidence/casting-ws-manual-rehearsal-6-receipt.md` | — |
| C3 — manual hold numeric boundary | Protocol reader matches launcher 30–1200 s and range-checks before narrowing | `RuntimeTestProtocol.ReadManualHoldSeconds` | `runtime-manual-scenario-validation` (29, 2^32+300 long, -1 rejected) |

Every new regression was mutation-checked: re-introducing the defect
(already-ready rejected, capture failure ignored, unclean restoration
ignored, unbounded capture wait, lease ignored, sibling change ignored,
done preferred over stop) makes the suite fail.

## What to review, by area

### Casting contract core (domain/compiler/budgets)

- `src/KingmakerBuffPlanner/Domain/Authoring/CastingIntentModels.cs` —
  one saved casting record = one invocation; Draft/Ready/Disabled;
  explicit order.
- `src/KingmakerBuffPlanner/Planning/ExplicitCastingCompiler.cs` —
  deterministic allocation with spell-scoped atomic budgets.
- `src/KingmakerBuffPlanner/Planning/CastingReviewCoordinator.cs` —
  presented-plan review with material-content signatures.
- `src/KingmakerBuffPlanner/Planning/CastingExecutionGate.cs`,
  `CastingForecast.cs`, `CastingTargetingModifiers.cs`.
- Regression tests: `tests/KingmakerBuffPlanner.Tests/Program.cs`
  (custom runner; 231 protocol tests at the J-review repair, incl.
  `casting-workspace-mixed-caster-flow`,
  `casting-workspace-review-and-apply-policy`,
  `casting-workspace-save-reopen-and-protection`,
  `casting-workspace-sibling-intent-vs-derived-changes`,
  `casting-workspace-disabled-dispatch-attempt-only`).

### Workspace UI (session + view)

- `src/KingmakerBuffPlanner/UI/CastingWorkspaceSession.cs` — session
  controller: browse-never-mutates, explicit commands with Undo,
  review integration, `DisabledCastingDispatchBoundary` (records
  attempts, never claims gameplay).
- `src/KingmakerBuffPlanner/UI/CastingWorkspaceScreenView.cs` —
  parchment workspace view (caster lane, casting cards, inspector,
  Review & Apply footer) built against installed Unity 2018.4
  contracts.
- `src/KingmakerBuffPlanner/UI/CastingWorkspaceDevSelection.cs` —
  session-scoped selection; while selected, every legacy quick-exec
  route refuses (`BuffPlannerUiRoot.ExecuteLegacyRoutine`).

### Harness repairs this session (each with a regression test or a fail-closed gate)

- **Launcher `-File` crash** — `scripts/Invoke-KingmakerRuntimeTest.ps1`:
  a top-level `$PSCmdlet.ShouldProcess` throws NullReferenceException
  under `powershell.exe -File` on both the WhatIf and confirmation paths
  (reproduced minimally; works under `-Command`/direct invocation).
  Guard contract: a decision that cannot be evaluated is a REFUSED
  decision for real runs (error propagation, nothing staged); a `-WhatIf`
  request is honored because its outcome is deterministically negative
  and can never create permission. Regression:
  `scripts/Test-RuntimeLauncherFileWhatIf.ps1` (four layers: pattern
  refusal, guard-shape source check, `-File -WhatIf` purity, and a REAL
  `-File` run proving non-zero exit plus zero mutation of every
  protected root; wired into `Test-SourceOnly.ps1`).
- **Workspace routing** —
  `src/KingmakerBuffPlanner/RuntimeTesting/RuntimeTestHost.cs`: the
  workspace dev-selection enable sat dead twice (inside the non-live
  UI-smoke gate; then after `Update()`'s live-UI return); now at the
  top of `Update()`.
- **Honest visual gates** — the `live-workspace-qual` scenario now
  requires `workspaceRoot=active;legacyScreen=closed`
  (`IsCastingWorkspaceOpen`, not the legacy `IsScreenOpen`), dual-path
  frame capture (end-of-frame ReadPixels + luma stats + engine second
  path), `workspace-frame-nonblack` (blackFraction<0.98), and
  `workspace-hierarchy-presents` (active hierarchy, alpha, renderable
  texts). A persistent black frame FAILs the run at
  `workspace-visual-validation`.

## Evidence layer status (honest)

Current (0.2.0-rc1 work, 2026-09-23):

| Layer | Status |
| --- | --- |
| Source tests | Full gate at every pushed source commit (Windows PowerShell 5.1, non-interactive); 294 protocol tests at `542cd66`. Every guard added in the RC iteration has a caught mutant. |
| Native gameplay | One-cast probe (`casting-probe-cast-20260923-p2-02`) and the zero-cost qualification (`casting-qual-cast-20260923-q1-01`): real casts through the production Apply, stop, skip, repeat and a recast after a reopen, each exactly as forecast. Finite resources, group, metamagic, rods and animated mode not yet cast in game. |
| In-game persistence | Save, close and reopen (`casting-ws-qual-*`); an in-game reload of the exact test save with the saved plan and one set of handlers after it (`casting-ws-reload-20260923-s2-01`). |
| Workspace presentation | Game frames at 1920×1200 (scale 1.000, like the native UI); other resolutions untested. |
| Restoration | Every live run restored the Mods folder byte-exact and compared every save. |
| Manual acceptance | Not yet; the supervised session is prepared. |

History:

| Layer | Status |
| --- | --- |
| Source/protocol tests | 231/231 protocol PASS locally at the J-review repair (the full `Test-SourceOnly.ps1` gate counts for the pushed HEAD are recorded in `AUTONOMOUS-RESUME.md`). Historical: 217/217 at checkpoint 12. |
| Live campaign qualification | Campaign load, UMM close, workspace open through the production hotkey path: PROVEN (run `casting-ws-root-200200`, game log) |
| Visual capture | Backbuffer presentation dies with a disconnected session (classified by matched control/open/closed bisection); the camera-render capture lane bypasses the display path and produced rendered workspace frames including the live authoring run. Display-path acceptance still pending a connected session. |
| Interaction/execution | **Corrected control-path LIVE-PASSED** (run `casting-ws-gseries-081000`; supersedes the `casting-ws-controls-051500` claim, whose assertions could not detect refused Adds): every action through real ACTIVE buttons — source, caster, recipient, state, Add (×3 with one-record growth, distinct identities, exact authored fields, unchanged siblings), a deliberately refused Add leaving the document untouched, Edit, focused retarget, Undo restoring the exact pre-edit canonical document, Done clearing focus, Save, and production-route reopen with the full canonical signature preserved and a clean dirty state. Protocol layer additionally proves group transitions, focused enhancements, campaign binding, and a fresh-session persisted round trip (223/223). Native submission remains disabled; physical-input (real pointer/keyboard) acceptance and the native aesthetic pass remain OPEN. |

## Published evidence images (game-window-only, authorized runs)

| File | Original run artifact (identical SHA-256) | What it shows |
| --- | --- | --- |
| `docs/evidence/casting-ws-gseries-081000-full-acceptance.png` | `runtime-evidence/casting-ws-gseries-081000/workspace-frame.png` (`38fb627f…`) | **Corrected full-acceptance run, display path**: the open workspace (mean 0.73 vs control 0.14, changed 0.95) from the run whose sequence requires real active controls, record-level Add assertions, a refused-Add negative, and intent-exact Undo. The earlier `casting-ws-controls-051500` frames are superseded by this run (its assertions could recycle a last-record ID). |
| `docs/evidence/casting-ws-controls-051500-control-backbuffer.png` | `runtime-evidence/casting-ws-controls-051500/workspace-control-frame.png` (`0ea31e28…`) | Matched control frame (workspace closed) — retained for the superseded 051500 run. |
| `docs/evidence/casting-ws-author-033000-interact-authored.png` | `runtime-evidence/casting-ws-author-033000/ws-interact-authored.png` (`62c2a167…`) | The authoring workspace LIVE with three authored casting cards (cast-1/2/3, two casters), draft editor in the inspector — captured after the scripted interaction sequence (browse → 3 casts → edit), through the camera lane. The run's behavior assertions PASSED: `workspace-interaction-sequence` (browseNoMutation=True; capableCasters=3; cast1/2/3=applied; edit=applied; undo=True; saved=True) and `workspace-reopen-preserves-intent` (exact IDs/order, loadStatus=Loaded). |
| `docs/evidence/casting-ws-author-033000-interact-reopened.png` | `runtime-evidence/casting-ws-author-033000/ws-interact-reopened.png` (`45e0cdeb…`) | The workspace after close + production-route reopen with the saved candidate intact (cast-1,cast-2,cast-3 preserved in order). |
| `docs/evidence/casting-ws-layer-030000-workspace-camera-frame.png` | `runtime-evidence/casting-ws-layer-030000/workspace-camera-frame.png` (`0a9a2fc9…`) | **The casting workspace rendered** — parchment panel, header "Casting Workspace", caster lane with three casters + Focus buttons, casting cards, inspector, footer Undo/Accept Plan/Review & Apply. Captured through the camera-render lane (display-independent) in a session whose backbuffer was black. Known defects visible: caster names show unit GUIDs (display names not reaching the row model); lane/card label truncation. |
| `docs/evidence/casting-ws-layer-030000-camera-control.png` | `runtime-evidence/casting-ws-layer-030000/workspace-camera-control.png` (`ce4b468c…`) | Matched control frame, same session/build/resolution, workspace closed: game scene + HUD (mean luma 0.41 vs the open frame's 0.73). |
| `docs/evidence/casting-ws-visual-183000-legacy-misroute-frame.png` | `runtime-evidence/casting-ws-visual-183000/workspace-frame.png` (`3c54fe28…`) | The run that PASSED identity checks while showing the LEGACY catalog screen — the misroute that the routing fixes address. Non-black dual-path capture (blackFraction 0.0285). |
| `docs/evidence/casting-ws-root-200200-hud-only-open-workspace.png` | `runtime-evidence/casting-ws-root-200200/workspace-frame.png` (`f6aa382a…`) | The run where the REAL workspace opened (log-proven, `workspaceRoot=active`, legacy closed) yet the frame shows the plain game HUD — later explained by the layer-culling defect fixed at `72209fc`. |

No image edits: the published files are byte-identical copies of the
run-time captures. Black-frame and missing-content checks were never
relaxed to obtain a PASS; the two runs above are recorded failures of
the visual gate on their own terms.

## Durable records for the full chain

`planning/CASTING-FIRST-MIGRATION-STATUS.md` (CURRENT STATE section),
`AUTONOMOUS-RESUME.md`, `AUTONOMOUS-BLOCKERS.md`,
`KINGMAKER-BUFF-PLANNER-JOURNAL.md`.
