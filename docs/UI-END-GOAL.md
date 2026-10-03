# Planner UI end goal — casting-first, Bubble Buffs–like

Recorded 2026-09-22 from Howie's direction after the first supervised
manual session (`casting-ws-claude-manual-20260922-200244`). This page steers
UI work; the adopted migration charter (§3, §6) remains the detailed
contract.

## Owner direction

1. **Look broadly like Bubble Buffs** (Wrath of the Righteous mod,
   nexusmods `pathfinderwrathoftherighteous/mods/195`). This is a
   long-term visual target, rendered with Kingmaker's own campaign art —
   never Wrath assets.
2. **One individual casting of a buff is the atomic unit** — not the
   caster, not the recipient. Bubble Buffs is buff × recipient-set
   centric, which makes it hard to change one specific casting (its
   caster, source, enhancements, target). Every casting must be
   independently selectable and editable.
3. The current three-column workspace (Casters / Castings / Inspector
   with checkbox-style text buttons) works functionally (Add, Undo) but
   is disliked ("I really hate this UI"). It is scaffolding, not the
   design.

## Reference catalogue (local only, not published)

Owner-supplied screenshots of Bubble Buffs, kept in
`C:\Dev\KingmakerBuffPlannerLab\handoff\` (third-party mod screenshots;
not committed to this public repository).

| File | SHA-256 prefix | What it shows |
|---|---|---|
| `Bubblebuffs1.webp` (= `Bubblebuffs8.webp`, identical) | `4e91716066b85d1d` | Full buff setup inside the native Spellbook service window: a 4-column scrolling grid of icon-first buff cards ("Name — Variant", then `casting: n/m + available: a+b`); left rail with Search, Show Hidden / Show Short / Only Requested toggles and Spells / Abilities / Items / Consumables source tabs; a selected-buff detail panel with its caster portrait(s) and per-caster counts (`4+0`); Add To All / Remove From All; a bottom strip of full party portraits tinted by state. |
| `Bubblebuffs2.webp` | `020a5c90c54a3c1f` | Tutorial: choose the ability type via the Spells / Abilities / Items / Consumables tabs. |
| `Bubblebuffs3.webp` | `8ec05fc03863b4e0` | Tutorial: select the buff card to configure (highlighted card). |
| `Bubblebuffs4.webp` | `c64182eaf70cce81` | Tutorial: toggle recipients by clicking portraits. Colour legend — gray: not wanted; green: wanted, casters available; yellow: wanted, no caster available; red: invalid target. |
| `Bubblebuffs5.webp` | `2f224bda41884de0` | One HUD button runs the routine; the combat log reports "Buffed! Applied 86/86 (skipped: 2)". |
| `Bubblebuffs6.webp` | `0303186c326a12ff` | Results window: applied buffs with icon and count (`Blur x8`), plus a "Buffs Skipped" section (already applied). |
| `Bubblebuffs7.webp` | `7642c210e892b717` | Per-caster popover on a caster portrait: limit casts (`4/4` with steppers), Ban from casting, Use 'Powerful Change', Allow 'Share Transmutation', resource-tracking note; caption shows `2+2 Arcanist`. |

## What to keep from Bubble Buffs

- Lives in (or looks like) a native campaign page: parchment, framed
  icons, serif type, native tabs and checkboxes.
- **Icon-first buff grid** with honest counts on every card; search and a
  few plain filters; source-type tabs.
- **Portrait-first targeting** with a clear state colour legend
  (not wanted / wanted+coverable / wanted+uncovered / invalid).
- Caster-level options in a small popover (limits, ban, Powerful Change,
  Share Transmutation).
- One-click routine buttons and a readable results window with skipped
  reasons.

## What changes for casting-first

- Selecting a buff shows its **castings as cards** in the detail panel
  — each card = one invocation: caster portrait + source/variant,
  recipient portrait (or origin + coverage for group spells),
  enhancement chips (Extend, rods, metamagic), cost, readiness, routine.
- Clicking a recipient portrait with a caster chosen **adds a casting
  card** (single-target: one card per recipient); clicking a card
  focuses it for editing — change caster, source, recipient,
  enhancements, routine — without touching sibling cards.
- The recipient strip shows, per portrait, which castings cover them
  (count badge), using the Bubble Buffs colour legend.
- "Add To All" becomes an explicit, previewed batch that creates N
  visible cards; nothing is created silently.
- Per-casting settings replace per-buff/per-caster settings wherever they
  change an individual invocation; caster-wide limits remain on the
  caster popover.
- Long / Important / Short are routine tabs; each card shows and can
  change its own routine.

## Gaps observed in the current workspace (session 2026-09-22)

- Focus buttons and routine tabs give no visible feedback.
- No starting guidance; the empty Castings lane does not say what to do.
- Buff list is text buttons with `[ ]` markers, no icons; duplicate names
  ("Use Heal Skill" ×2, "Aid Another" ×2) are indistinguishable; skill
  actions appear as buffs.
- No recipient portraits, no coverage colours, no catalogue grid.
- Routine tab "long" clipped at the frame edge; footer diagnostic text
  overlaps the Casters lane.

## Next UI slice (proposed)

1. Replace the inspector's buff buttons with an icon-first, searchable
   buff grid (discovered icons and exact variant labels, duplicates
   disambiguated by source).
2. Replace caster/target text buttons with portrait strips using the
   colour legend; clicking a recipient with a chosen caster adds a card.
3. Casting cards with caster/recipient portraits and enhancement chips;
   card click = focus/edit; visible selected state.
4. Then the native parchment pass (charter §6) and per-caster popover.
