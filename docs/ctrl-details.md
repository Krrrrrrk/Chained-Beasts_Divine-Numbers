# Ctrl and controller details: 0.2.4

Hold either Ctrl key to see researched numbers in a separate native-style panel. Original descriptions stay intact and Shift retains its normal explanations. The user confirmed the 0.1.1 character-select support and smaller text work well. Expanded 0.2.0 interactions still need manual verification.

Controller: click the **right stick (R3)** to inspect the native selected item or that controller's character-select description. From the collapsed bottom HUD, it shows the first actual visible skill; native focused tooltips take priority when you hold RB and navigate normally. The mod does not force the native held-RB interface open. Click again to turn the numbers off. The heading shows `[R3: On]` while latched. It resets when all inspection contexts disappear, application focus is lost or the pad disconnects. It can remain latched when returning from a menu to a visible HUD. Holding the button does not repeat the toggle or arm it when entering a context/returning focus. Ctrl remains independent. Unsupported focused descriptions clear the old card.

The generated `BepInEx/config/local.chainedbeasts.divinenumbers.cfg` exposes `[Input] ControllerToggleButton`: `RightStickClick` (default), `LeftStickClick`, or `Disabled`. Restart to apply. No key binding or input action is replaced.

Controller troubleshooting: use 0.2.4 for merchant and trainer menus. The user confirmed R3 potion selection in 0.2.3, but merchant cards, the trainer affliction and post-training skill choices failed. Merchant selection now reads native per-player menu state and resolves the current typed purchase ID for controller mode. Training afflictions bind `UILobbyTrainerProgress`, while skill choices read the native selected skill-card index rather than the separate card navigation flags. Menu contexts suppress the HUD fallback even with no focused/supported card. Logs include the selected offer index, typed ID or description term. The new paths need manual runtime verification.

## Where to use it

- Character select: highlight an unlocked supported variant. Hover another visible player's description to inspect theirs.
- Character info: inspect an audited skill, affliction severity, purchase or status.
- Trainer/character-details lists: select or hover the supported skill as before.
- Potion/healer shop: hold Ctrl over the combined sign and separate potion preview, including the header, border, blank space, price, potion and pedestal.
- Surgery and Codex: hover the displayed description text while holding Ctrl.
- Vanilla Shift status boxes: hover an audited Vigor, Weak, Fire or Backstab explanation while holding Ctrl.

New descriptions are discovered immediately when Ctrl is first held or the controller toggle is requested, then at most twice per second while requested. A pooled label's current key is checked again before display. Locked choices, unknown keys, invalid severities and unsupported custom-mod descriptions receive no guessed card. In keyboard-only use, release both Ctrl keys to hide the overlay. With the controller toggle latched, release Ctrl to return to controller selection.

There are 206 distinct cards: 50 skills/variants, 134 affliction severity entries, 18 purchases and four statuses. Some cards supplement a named status rather than restating an already numbered trigger. See [the audit report](audit-report.md), [complete coverage](../data/coverage.json) and [card data](../data/numeric-details.json).

## Implementation and limits

Seven postfix hooks observe the same native UI methods as 0.1.1: the four UIPlayerInfoTooltip.Initialise overloads, UILobbyTrainerUpgradePage.SetSkill, UIGameLevelLobbyCharacterSkillInfo.Initialise and UIGameServerLobbyPage.Refresh. Injury, purchase and status overloads now bind their own namespaces/severities, so their integer IDs cannot collide with skills. Native methods run normally and native-entry alias guards remain.

Additional contexts use visible I2.Loc.Localize.Term values discovered while Ctrl is held or the controller toggle is requested. Localization, translation resolution and input actions are not patched. Discovery has a half-second interval; resource scanning does not run every frame. Current keys are rechecked before showing pooled labels. In keyboard mode hovered description text has priority; controller mode uses native focus and excludes mouse hover. The clone's own labels are excluded.

The toggle samples Unity Input System gamepads by index even when no keyboard is present. Device IDs resolve through `InputSystem.GetDeviceById` and native `GameInput.GetDeviceClient`, including the no-explicit-assignment single-player fallback. `GetClientDevice` alone is not its inverse. Native healer focus flags, merchant/trainer menu indices and Codex highlights supply selection. A low-priority HUD binding reads the first native `visualSkillList` entry and anchors to its visible slot; it never chooses another supported entry when that slot is unsupported. Visible offer/training contexts suppress this fallback even without a supported focused card. Shared menus can retain shared native focus. Another pad's click takes ownership of the one overlay.

Merchant controller focus uses `UILobbyShop.state.navigatePurchase[player]` and `navigatePurchaseIndex[player] == page.Index`; its detail resolves through `PurchaseType` after the title is visible. Keyboard inspection retains its existing term lookup. Training previews bind the current visible `UILobbyTrainerProgress.injuryDescription` and its `playerIndex`, preserving native severity terms. Skill-choice focus uses `UILobbyTrainer.uiState.upgradeSelectionIndex[player] == UILobbyTrainerUpgradePage.Index`; `navPage.lastFocusedState` and `isSelected` do not identify the separate Learn/Trade action widgets' current controller choice. Native arrays are bounds checked before reading, and current skill IDs are re-resolved every frame.

The audited `InputActions` asset embedded in the native constructor contains no `leftStickPress` or `rightStickPress` binding. UI right shoulder is CharacterInfo and left shoulder is Tooltip, so both are preserved. This is a default-binding check, not proof about custom remaps. Local Cpp2IL `UnityEngine/InputSystem/InputActions.txt` SHA256: `f1a873f155d5100f5154fa458f18ad3ae71d0ed557517e103d384228e30c5a38`; the shipped game's binary hash gate covers that constructor.

Healer potion offers bind directly from visible `UILobbyDoctorSelectionPage` objects. Hover and placement use a screen-space union of the native navigation anchor, remedy/water name, remedy description, healing value, backing images, potion icon and `ui3DView`. The healer prefab has a 368x406 sign at (-145,35) and a separate 250x350 preview centered at (0,-80). Focus reads `focusedPlayerIndices[playerIndex]`, a boolean array. General purchases retain their separate `UILobbyShopSelectionPage` binding, whose focus field is a list of player indices and whose sign/preview positions differ. The two widget types must not be interchanged.

The current visible description term supplies the numeric detail each frame. Concealed descriptions yield no card; their visible offers still block stale beast numbers. A pure selection policy suppresses the collapsed-HUD fallback whenever an offer menu is visible. Unknown/concealed focused offers are retained as blockers, not skipped in favor of a supported HUD entry. Nested status explanations retain their own hitboxes, and native Shift/LB panels contribute to occupied bounds. No purchase/click behavior changes.

The private, non-interactive UIStatusTooltip clone keeps 22-point type, 24-pixel horizontal/20-pixel vertical padding and measured preferred height, with smaller type if needed. Placement tries right/left/below/above and clamps to the viewport. Known native Shift boxes are included in occupied bounds. Other screens and simultaneous Shift/Ctrl placement still require manual review where space is limited.

Values are usually audited bases, not live player totals. Backstab reads the audited GladiatorConsts.BackstabMultiplierBase getter because a setting selects 2x or 1.5x. Target resistance can further change the bonus. The overlay patches no combat method and writes no saves or network messages. A UI exception disables it and logs once while native UI continues.

Four input hashes gate loading: GameAssembly, metadata, level1 and sharedassets1. A changed input disables the mod for re-audit, including the asset-backed values. No Unity session or gameplay was launched/automated for this update. The user's confirmation applies to 0.1.1, not all new screens or mechanics.

## Completed checks

- Release compilation: zero warnings/errors against installed interop files.
- All 206 typed cards/exact keys; namespace/severity isolation; unknown/locked-key clearing; invalid-data rejection; shared-card isolation.
- Evidence-sensitive examples: pickup disarm 16/32%, stamina drawback 25% per severity, fired arrows versus thrown bows, implemented stun chance versus localization.
- Four placement directions and 1,575 viewport/edge combinations.
- Sign-plus-potion hover regression using the serialized prefab's dimensions/offsets, at two scales; header, blank area, price and preview points, plus outside rejection.
- Separate healer-prefab geometry checks, plus the reported Ibex-HUD-versus-potion selection scenario, concealed/no-card offers, no focused offer and per-player candidate priority.
- Merchant/trainer modal-selection regressions suppressing the Desperate Ibex HUD, plus Wooden Club purchase ID 3000, Color Blindness I's exact severity term and No Mercy skill ID 400 lookup checks.
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
9. With a controller, press R3 from the collapsed bottom HUD; check the first visible skill's numbers. Hold RB and navigate other entries. In the healer, move focus among supported potions and verify their names/severities replace the HUD card. Move to water or a concealed random offer: no unrelated beast card should appear. Check the other menus and a second R3 click. Repeat in single-player and with two assigned local controllers. Inspect the visible-card count/selection logs if a menu fails to bind.
10. Hold R3 across several frames, menu close/reopen and focus loss/return. Disconnect the pad while latched; reconnect. Check there is no repeated or stale toggle. Test without a keyboard and with two local controllers inspecting different players.
11. Try `LeftStickClick` and `Disabled` after restarting. With a controller latched, hold/release Ctrl and toggle the controller off while Ctrl remains held.
