# AUTONOMOUS BLOCKERS — updated 2026-10-02

None blocking the code path. Open items, none of which need a decision:

1. r13 evidence batch built but not dispatched (owner-requested stopping
   point). One command to resume; see AUTONOMOUS-RESUME.md.
2. E12's cast half (the allowance-armed cold-moon run) is implemented,
   source-tested and built as ui-phys-cast; its native evidence comes
   from the r13 batch. The selection half is already proven live
   (beta-f0cf4f16r11-phys-sel-01, zero violations).
3. Hover-probe jobs may need the physical console even after the
   owner-authorized activation change; if so they are last in the queue
   by design and the concrete reason is recorded in the dispatcher and
   the journal.
4. Private delivery not yet sent; the prior beta draft-release asset
   (214b795b...8b20564) is untouched and must stay so.
