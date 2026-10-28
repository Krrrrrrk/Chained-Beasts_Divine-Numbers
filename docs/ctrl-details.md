# Ctrl details: 0.2.0 test build

Hold either Ctrl key to see researched numbers in a separate native-style panel. Original descriptions stay intact and Shift retains its normal explanations. The user confirmed the 0.1.1 character-select support and smaller text work well. Expanded 0.2.0 interactions still need manual verification.

## Where to use it

- Character select: highlight an unlocked supported variant. Hover another visible player's description to inspect theirs.
- Character info: inspect an audited skill, affliction severity, purchase or status.
- Trainer/character-details lists: select or hover the supported skill as before.
- Shop, surgery and Codex: hover the displayed description text while holding Ctrl.
- Vanilla Shift status boxes: hover an audited Vigor, Weak, Fire or Backstab explanation while holding Ctrl.

New descriptions are discovered immediately when Ctrl is first held, then at most twice per second while it stays held. A pooled label's current key is checked again before display. Locked choices, unknown keys, invalid severities and unsupported custom-mod descriptions receive no guessed card. Release both Ctrl keys, close the menu or lose focus to hide the overlay.

There are 206 distinct cards: 50 skills/variants, 134 affliction severity entries, 18 purchases and four statuses. Some cards supplement a named status rather than restating an already numbered trigger. See [the audit report](audit-report.md), [complete coverage](../data/coverage.json) and [card data](../data/numeric-details.json).

## Implementation and limits

Seven postfix hooks observe the same native UI methods as 0.1.1: the four UIPlayerInfoTooltip.Initialise overloads, UILobbyTrainerUpgradePage.SetSkill, UIGameLevelLobbyCharacterSkillInfo.Initialise and UIGameServerLobbyPage.Refresh. Injury, purchase and status overloads now bind their own namespaces/severities, so their integer IDs cannot collide with skills. Native methods run normally and native-entry alias guards remain.

Additional contexts use visible I2.Loc.Localize.Term values discovered while Ctrl is held. Localization, translation resolution and input actions are not patched. Discovery has a half-second interval; resource scanning does not run every frame. Current keys are rechecked before showing pooled labels. Hovered description text has priority over selected/background contexts; the clone's own labels are excluded.

The private, non-interactive UIStatusTooltip clone keeps 22-point type, 24-pixel horizontal/20-pixel vertical padding and measured preferred height, with smaller type if needed. Placement tries right/left/below/above and clamps to the viewport. Known native Shift boxes are included in occupied bounds. Other screens and simultaneous Shift/Ctrl placement still require manual review where space is limited.

Values are usually audited bases, not live player totals. Backstab reads the audited GladiatorConsts.BackstabMultiplierBase getter because a setting selects 2x or 1.5x. Target resistance can further change the bonus. The overlay patches no combat method and writes no saves or network messages. A UI exception disables it and logs once while native UI continues.

Four input hashes gate loading: GameAssembly, metadata, level1 and sharedassets1. A changed input disables the mod for re-audit, including the asset-backed values. No Unity session or gameplay was launched/automated for this update. The user's confirmation applies to 0.1.1, not all new screens or mechanics.

## Completed checks

- Release compilation: zero warnings/errors against installed interop files.
- All 206 typed cards/exact keys; namespace/severity isolation; unknown/locked-key clearing; invalid-data rejection; shared-card isolation.
- Evidence-sensitive examples: pickup disarm 16/32%, stamina drawback 25% per severity, fired arrows versus thrown bows, implemented stun chance versus localization.
- Four placement directions and 1,575 viewport/edge combinations.
- Four game hashes, 157 native literals, 35 native dump hashes and all active card keys checked. Input/literal identity checks do not independently prove every inferred semantic relationship.

## Manual checks

1. Hold left/right/both Ctrl keys and release individually. Original descriptions must stay unchanged.
2. Switch supported, unsupported and locked variants while holding Ctrl. Check matching cards and old-card clearing.
3. Inspect 1/2/3-skull afflictions and purchases/skills sharing integer IDs. Check severity-specific numbers.
4. Open shop, surgery and Codex. Hover description text with Ctrl; allow up to half a second for newly opened labels. Change pooled selections.
5. Try Shift, Ctrl and both. Hover native status text. Confirm the overlay cannot steal clicks/hover.
6. Close/reopen menus, change scenes and application focus. No stale card should remain.
7. Check smaller windows, wide screens, UI scaling, multiplayer/local-player pages and other installed mods.
8. Review long cards and Backstab's setting-dependent value. Compare selected mechanics in controlled gameplay before treating them as measured final damage.
