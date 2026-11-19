# Simplified Chinese localization sprint

Start from verified origin/main 639b5d2a3119efa2d2db9323f663d6aa69cd1d65 (0.6.14). No open PRs at baseline. The original checkout is a clean older 0.6.11 branch. Use the isolated KBP-ZHCN worktree, branch codex/kingmaker-buff-planner-zhcn-localization. Preserve other worktrees, the normal installation and protected saves.

1. Inventory planner-owned text, verify installed locale, renderer and duration contracts.
2. Add Automatic, English and 简体中文 settings and independently reviewable embedded catalogs. Keep native names and all plan identities separate.
3. Integrate presentation, fallback fonts, safe refresh and input ownership across settings, workspace, controller, HUD, spellbook, retained Classic and notifications.
4. Replace prose-based duration decisions with conservative structural evidence, retaining fixed/per-level and Extend policy.
5. Run focused L01–L06 and D01–D05 checks, freeze and validate candidate, attempt guarded R01–R08, record observations and remaining limits, then publish a draft PR for owner review. Only KBP_AUTOMATION_WORKING may change during live tests. No merge or release.

Initial text inventory from source inspection: Main and PlannerSettingsText own UMM controls. CastingWorkspaceScreenView and its ClearPlan/Tooltips partials own workspace chrome and dialogues. WorkspaceFooterModels, CastingGraphModels, CastingWorkspaceSession, CastingRunPresentation and CastingWorkspaceResources supply computed labels, notices and summaries. ControllerRoutineView, controller menu integrations, HUD and spellbook entry controllers supply entry and quick-run text. BuffPlannerScreenView, PlannerViews and presentation models serve retained Classic. PersistenceMessages and service outcomes feed visible errors. Exclude object names, stable IDs, profile tokens, log prefixes, probe outputs and reason comparisons. Further inspection and verification are recorded in the localization document.
