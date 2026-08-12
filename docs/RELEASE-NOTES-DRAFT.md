# Kingmaker Buff Planner 0.0.6 — Draft Release Notes

Publication status: local qualification build only. Not a public release.

Version 0.0.6 recovers actual player-visible catalog rows and details after direct human testing showed 0.0.5's internal row/geometry reports did not correspond to pixels.

Highlights:

- fixed both masked scroll panes by keeping their visually hidden Mask source opaque enough to write the Unity UI stencil;
- used a temporary high-contrast same-Content canary and actual campaign screenshots to isolate the shared viewport path, then removed the canary;
- retained the simple programmatic Unity row renderer and enforced explicit readable row/detail heights;
- replaced “shown” claims with matched and bound-row counts;
- added first-five names/rectangles, selected details, CanvasRenderer/font/material/mask evidence, screenshot hashes, and independent pixel-region contrast checks;
- corrected generic material validation to query component sufficiency only when Kingmaker reports a consumable component requirement;
- confirmed native Bless submission, start, expected effect, and one prepared-slot spend in two fresh processes;
- preserved the accepted HUD icons, tooltips, pointer isolation, F10 bootstrap, opaque modal, input restoration, persistence, native discovery, and Call of the Wild support.

Qualified source: `e656812572adea8bc312419372b61ee8c4834e5a`. Package SHA-256: `ce7492b262f01a9afb5a7666fe7e4bda9be1821395eb00244f5898b6882208e9`. DLL SHA-256: `6144256c6a0623e908c3d9e821a1b87ee5800195759fbfabb1e587eaf9be1d9b`. MVID: `bff11809-aa53-42c2-8ab7-ef3564450e61`.

Final campaign runs `row-render-0.0.6-production-3` and `row-render-0.0.6-production-4` passed 71/71 each and produced the same screenshot SHA-256 `cb2343683ebc4d3dfbb066de4b030c1745c518063354a6357a331a6d53d75c19`. Native passed 12/12 and Call of the Wild passed 26/26. The guarded installation preserved settings and every other mod. Publication requires separate authorization.
