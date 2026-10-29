# Ctrl and controller details: 0.2.1

Hold either Ctrl key to see researched numbers in a separate native-style panel. Original descriptions stay intact and Shift retains its normal explanations. The user confirmed the 0.1.1 character-select support and smaller text work well. Expanded 0.2.0 interactions still need manual verification.

Controller: click the **right stick (R3)** while a supported info/menu screen is open. The box stays on and follows the native selected item or that controller's character-select description. Click again to turn it off. The heading shows `[R3: On]` while latched. It resets when all bound info menus close, application focus is lost or the owning pad disconnects. Holding the button does not repeat the toggle or arm it when entering a menu/returning focus. Ctrl remains independent. Unsupported focused descriptions clear the old card. Non-focusable character-detail rows still require mouse/Ctrl.

The generated `BepInEx/config/local.chainedbeasts.divinenumbers.cfg` exposes `[Input] ControllerToggleButton`: `RightStickClick` (default), `LeftStickClick`, or `Disabled`. Restart to apply. No key binding or input action is replaced.

Controller troubleshooting: the 0.2.0 DLL has no R3 support. The user's reported R3 failure was checked against the installed assembly and game log, both of which showed 0.2.0; the 0.2.1 controller update had been left packaged rather than installed. A working Ctrl panel alone does not establish that the controller update is loaded. Check the startup version in `BepInEx/LogOutput.log` after installing/restarting. Hardware controller behavior remains a manual check.

## Where to use it

- Character select: highlight an unlocked supported variant. Hover another visible player's description to inspect theirs.
- Character info: inspect an audited skill, affliction severity, purchase or status.
- Trainer/character-details lists: select or hover the supported skill as before.
- Potion/healer shop: hold Ctrl over any part of the offer's native interaction rectangle, including the header, border, blank space and price.
- Surgery and Codex: hover the displayed description text while holding Ctrl.
- Vanilla Shift status boxes: hover an audited Vigor, Weak, Fire or Backstab explanation while holding Ctrl.

New descriptions are discovered immediately when Ctrl is first held or the controller toggle is requested, then at most twice per second while requested. A pooled label's current key is checked again before display. Locked choices, unknown keys, invalid severities and unsupported custom-mod descriptions receive no guessed card. In keyboard-only use, release both Ctrl keys to hide the overlay. With the controller toggle latched, release Ctrl to return to controller selection.

There are 206 distinct cards: 50 skills/variants, 134 affliction severity entries, 18 purchases and four statuses. Some cards supplement a named status rather than restating an already numbered trigger. See [the audit report](audit-report.md), [complete coverage](../data/coverage.json) and [card data](../data/numeric-details.json).

## Implementation and limits

Seven postfix hooks observe the same native UI methods as 0.1.1: the four UIPlayerInfoTooltip.Initialise overloads, UILobbyTrainerUpgradePage.SetSkill, UIGameLevelLobbyCharacterSkillInfo.Initialise and UIGameServerLobbyPage.Refresh. Injury, purchase and status overloads now bind their own namespaces/severities, so their integer IDs cannot collide with skills. Native methods run normally and native-entry alias guards remain.

Additional contexts use visible I2.Loc.Localize.Term values discovered while Ctrl is held or the controller toggle is requested. Localization, translation resolution and input actions are not patched. Discovery has a half-second interval; resource scanning does not run every frame. Current keys are rechecked before showing pooled labels. In keyboard mode hovered description text has priority; controller mode uses native focus and excludes mouse hover. The clone's own labels are excluded.

The toggle samples Unity Input System gamepads even when no keyboard is present. Device IDs select the owning pad; the game's `GameInput.GetClientDevice` mapping restricts player-specific tooltip and character-select pages. Native trainer navigation flags, shop focused-player lists and Codex gamepad highlights supply selection. Shared menus can retain shared native focus. One overlay is active at a time; another pad's click takes ownership. No new navigation is added to rows the game cannot focus.

The audited `InputActions` asset embedded in the native constructor contains no `leftStickPress` or `rightStickPress` binding. UI right shoulder is CharacterInfo and left shoulder is Tooltip, so both are preserved. This is a default-binding check, not proof about custom remaps. Local Cpp2IL `UnityEngine/InputSystem/InputActions.txt` SHA256: `f1a873f155d5100f5154fa458f18ad3ae71d0ed557517e103d384228e30c5a38`; the shipped game's binary hash gate covers that constructor.

Potion hover uses `UILobbyShopSelectionPage.navigationButton.InteractionAnchor`, with the card root as a fallback, for both hit testing and occupied bounds. The localized main description remains the data source and visibility gate, so pooled or unrevealed offers cannot inherit another card's numbers. Nested status explanations retain their own hitboxes. Native shop Shift/LB status panels are included in occupied bounds and the shop's own tooltip prefab can supply the frame. No purchase/click behavior is changed.

The private, non-interactive UIStatusTooltip clone keeps 22-point type, 24-pixel horizontal/20-pixel vertical padding and measured preferred height, with smaller type if needed. Placement tries right/left/below/above and clamps to the viewport. Known native Shift boxes are included in occupied bounds. Other screens and simultaneous Shift/Ctrl placement still require manual review where space is limited.

Values are usually audited bases, not live player totals. Backstab reads the audited GladiatorConsts.BackstabMultiplierBase getter because a setting selects 2x or 1.5x. Target resistance can further change the bonus. The overlay patches no combat method and writes no saves or network messages. A UI exception disables it and logs once while native UI continues.

Four input hashes gate loading: GameAssembly, metadata, level1 and sharedassets1. A changed input disables the mod for re-audit, including the asset-backed values. No Unity session or gameplay was launched/automated for this update. The user's confirmation applies to 0.1.1, not all new screens or mechanics.

## Completed checks

- Release compilation: zero warnings/errors against installed interop files.
- All 206 typed cards/exact keys; namespace/severity isolation; unknown/locked-key clearing; invalid-data rejection; shared-card isolation.
- Evidence-sensitive examples: pickup disarm 16/32%, stamina drawback 25% per severity, fired arrows versus thrown bows, implemented stun chance versus localization.
- Four placement directions and 1,575 viewport/edge combinations.
- Controller input sequences: press/hold/release, second-click off, menu/focus reset, held-on-entry suppression, disconnect, another controller taking ownership and Ctrl coexistence. These exercise the production state machine without Unity.
- Four game hashes, 157 native literals, 35 native dump hashes and all active card keys checked. Input/literal identity checks do not independently prove every inferred semantic relationship.

## Manual checks

1. Hold left/right/both Ctrl keys and release individually. Original descriptions must stay unchanged.
2. Switch supported, unsupported and locked variants while holding Ctrl. Check matching cards and old-card clearing.
3. Inspect 1/2/3-skull afflictions and purchases/skills sharing integer IDs. Check severity-specific numbers.
4. Open the potion/healer shop. Hold Ctrl over each card's header, border, blank area, price and description; verify the same matching card appears throughout. Move between supported, unknown and unrevealed offers and confirm old-card clearing. Check native Shift/LB panels and the numbers panel fit beside the whole offer. Open surgery and Codex and hover their descriptions. Allow up to half a second for new labels and change pooled selections.
5. Try Shift, Ctrl and both. Hover native status text. Confirm the overlay cannot steal clicks/hover.
6. Close/reopen menus, change scenes and application focus. No stale card should remain.
7. Check smaller windows, wide screens, UI scaling, multiplayer/local-player pages and other installed mods.
8. Review long cards and Backstab's setting-dependent value. Compare selected mechanics in controlled gameplay before treating them as measured final damage.
9. With a controller, highlight supported/unsupported/locked choices in character select, character info, trainer, shop, surgery and Codex. Click R3; verify the card follows native focus without mouse movement, disappears for unsupported choices and a second click turns it off. Confirm native shoulder-button tooltips still work.
10. Hold R3 across several frames, menu close/reopen and focus loss/return. Disconnect the pad while latched; reconnect. Check there is no repeated or stale toggle. Test without a keyboard and with two local controllers inspecting different players.
11. Try `LeftStickClick` and `Disabled` after restarting. With a controller latched, hold/release Ctrl and toggle the controller off while Ctrl remains held.
