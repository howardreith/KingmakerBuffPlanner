# Third-Party Notices

Kingmaker Buff Planner is being implemented originally. The following MIT-licensed projects were inspected as design and API-behavior references. No third-party binary, source tree, UI asset, or game asset is included in this repository or release package.

- BubbleBuffs — copyright 2021 Sean Petrie (Vek17), MIT License.
- BuffIt2TheLimit / Wrath Epic Buffing — copyright 2021 Sean Petrie, factubsio, and Gh05d, MIT License.
- PathfinderAutoBuff — copyright 2021 Ilkar, Holic92, Narria, and Hsinyu, MIT License.
- KingmakerRebalance / Call of the Wild — copyright 2021 Holic75, MIT License.

If substantial code is later adapted, the applicable complete MIT notice and file-level provenance will be added before packaging. Kingmaker Buff Bot code and assets are explicitly excluded.

For the 0.4.0 scroll paper (WP7), the owner's own MIT-licensed Kingmaker Gunslinger Teleport modal and Kingmaker Dice Roller "Roll for Stats" panel were studied as visual references (see `docs/VISUAL-THEME.md`). The parchment sheet and scroll rule the planner shows are the running game's own sprites, borrowed at runtime from its UI and displayed only; no game image or extracted asset is included in this repository or the release package.

For the 0.0.7 and 0.0.8 presentation studies, BubbleBuffs was inspected at commit `f4871f763a23251284422ef0945a85e9f3fb788e`. Only general information-design ideas were reimplemented: icon-first cards, portrait-centered targeting, routine readiness, search-first filtering, and progressive disclosure of caster details. No BubbleBuffs code, binary, shader, material, icon, texture, mesh, localization, or other asset is included.

## Native controller menu contract reference

The native card/VM/navigation integration pattern was adapted with reference to
Sergey Usaev's `kingmaker-time-control/src/MenuCards.cs` in `sergusaev/pathfinder-mods`,
commit `37742d2da64b4915c2621cf15db330f243c2ad53`. The planner uses Harmony12,
owned passive presentation rather than a bound-card clone, and strict source-release
handoff rather than its timeout. No reference-project binary or artwork is bundled.

MIT License

Copyright (c) 2026 Sergey Usaev

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
