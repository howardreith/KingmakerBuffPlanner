# AUTONOMOUS RESUME — updated 2026-10-03 (Claude takeover: v1.2 preview delivered)

## Where the work stands

- Delivered (tested) source commit: `8d7681d03752f3f7170f25f7d45029f71c46a884`
  on `codex/kingmaker-buff-planner-everyday-use` (pushed through the guarded
  helper). Records commits on top of it are documentation only.
- Package `5b24e2eafee69b18898894b6e9a5cfb9b2c8e625a561494cb2234766097d8d17`,
  DLL `deee7b1de54daee9e890f6680c8e0d4db09674b12c394e2d2d6c2d3e460cf65b`,
  MVID `1e762004-8200-430e-853e-d61456a627ae`; frozen under
  `runtime-backups/qualification-frozen/8d7681d03752f3f7170f25f7d45029f71c46a884/`.
- Full gate PASS at that tree: `artifacts/gate-8d7681d.log` (42 / 398 / 38 /
  4 / 5 / 13 / 3 / 16 / 3).
- r15: 41/41 jobs done and verified on the delivered candidate
  (`dispatch\jobs.r15-final.json`, run ids `beta-8d7681d0r15-*`).
- Delivery: private draft release 399014941 (still a draft), asset
  `KingmakerBuffPlanner-0.2.0-rc6-everyday-use-v1.2+8d7681d0.zip` (id
  607817987), downloaded back and verified; the prior beta asset
  (214b795b...8b20564) unchanged. Receipt:
  `docs/evidence/everyday-use-v1.2-delivery-receipt.md`.
- Lab: dispatcher stopped, no claims in state `claimed`, transactions all
  Restored, no locks, no Kingmaker; `owner-pause` and `stop` flags absent.

## The exact next action

Wait for the owner's self-paced trial (`docs/MANUAL-ACCEPTANCE.md`,
everyday-use section). Do not merge, tag, publish or promote without the
owner's explicit authorization. If the owner reports a defect: reproduce from
the report, smallest repair + regression, full gate (~4 h; validate the
launcher WhatIf logic first with the fast copy described in the journal),
Build-Local, freeze, rebuild jobs with a fresh suffix (allowance run ids are
batch-unique since H5), rerun the affected chains, and add a NEW uniquely
named asset to the same draft (never replace an existing asset).
