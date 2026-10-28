# Ctrl numbers box: test build 0.1.1

Version 0.1.1 adds character-select support and reduces the type size from 26 to 22. It keeps the independent box introduced in 0.1.0 and never replaces the game's description or intercepts its translation lookups. Shift continues to show the game's ordinary status explanations.

## Supported interaction

- Character-info tooltips: inspect a supported skill and hold either Ctrl key.
- Character select: highlight a supported variant and hold Ctrl. The local player's displayed description supplies the exact skill key. Hovering another visible player's description can inspect its card too.
- Lobby character-details list: hover a supported skill row and hold Ctrl.
- Trainer skill cards: select or hover a supported skill card and hold Ctrl. The hooks are present even though the initial three beast variants are not ordinary trainer offers.
- Release Ctrl, close the menu, or inspect an unsupported item to hide the box. Losing application focus also hides it.

Only Proud Lion, Lethal Eagle, and Mighty Bear have researched entries. This version supplies no affliction numbers or Codex boxes. Locked variants display native unlock requirements and receive no numeric card.

## Implementation

Seven postfix hooks observe native UI binding: four `UIPlayerInfoTooltip.Initialise` overloads, `UILobbyTrainerUpgradePage.SetSkill`, `UIGameLevelLobbyCharacterSkillInfo.Initialise`, and `UIGameServerLobbyPage.Refresh`. The original methods always run. No combat method, input action, description, or localization resolver is patched. Character-select lookup uses `characterDescription.Term` after native refresh, rather than treating beast enum values or progression levels as skill IDs. Native `StringManager.GetBeastDescription` produces `Skills/Beast_{BeastType}_Lv{variant+1}_Desc`; locked choices instead use unlock-requirement keys. This was traced in the installed build's native dump.

The controller reads the current Ctrl keys in `LateUpdate` without consuming input. It chooses the visible hovered/selected context; unsupported contexts block previously selected supported cards. Hidden or destroyed widgets are excluded, including menus hidden by an ancestor CanvasGroup. A single private clone of `UIStatusTooltip` renders the current entry on a separate overlay canvas. Only the clone's localization components are disabled. Its graphics cannot receive raycasts. Native frames, fonts, and materials are borrowed without modifying the originals.

The panel uses 22-point type, a smaller variant-name line, and no empty line between the heading and body. The copied root stretches to the box; text has explicit 24-pixel horizontal and 20-pixel vertical padding, zero inherited TMP margins, and controlled line/paragraph spacing. Preferred height is measured using that exact inner width, with an additional 8 pixels of slack. Placement tries right, left, below, then above and clamps to the viewport. Visible Shift panels are included in the occupied bounds. On crowded screens where no free region can fit the panel, clamping can still overlap other UI; exact placement needs gameplay review.

The plugin uses an independent ID (`local.chainedbeasts.divinenumbers`), gates the recorded numeric data on the inspected game binary's SHA-256, checks for aliased native patch entries, and removes its hooks/overlay on unload. A UI exception disables this overlay and logs once while the native UI continues. It creates no save data or network messages.

## Completed verification

- Release build against installed BepInEx/game interop assemblies: zero warnings and errors.
- Offline checks: the three entries map to their audited IDs; unsupported IDs return no numeric details; the minimum-stun and ready-window qualifications are preserved.
- Character-select lookup checks: exact description terms resolve to the right entry; locked, unsupported, name-only, malformed, and translated-text lookups clear the previous card.
- Offline geometry checks: four placement directions and 1,575 viewport/edge combinations keep a fitting card on-screen.
- Source provenance: game hashes, eight native float constants, existing code-dump hashes, and original English descriptions verified with `scripts/verify-first-pass.py`.

The user supplied a 0.1.0 screenshot showing the in-game Ctrl card, with crowded text and a last line touching the lower frame. The 0.1.1 changes address that report; neither the new character-select hook nor the updated fit has been agent-tested in Unity.

## Manual checks still required

1. Open character info for each of the three supported variants. Hold left Ctrl, then right Ctrl. Confirm the original text is unchanged and the extra card contains the matching numbers.
2. Hold both Ctrl keys; release one, then the other. The card should remain until both are released.
3. Use Shift alone, Ctrl alone, then Shift+Ctrl. Confirm vanilla status descriptions continue to work and the two boxes remain readable.
4. Move to an unsupported skill, an affliction, equipment, and an empty slot while holding Ctrl. Confirm the old numeric card disappears.
5. Close/reopen the menu, change scene, and switch application focus while holding Ctrl. Confirm no old card stays on-screen.
6. Hover skill rows in character details. Check that the card follows the hovered row. Trainer contexts need a future audited trainer skill to exercise a positive entry; unsupported cards should show no card now.
7. Check smaller windows, wide screens, UI scaling, multiplayer clients/local player sheets, and coexistence with other installed mods.
8. On character select, switch between supported, unsupported, and locked variants while holding Ctrl. Confirm matching cards appear only for supported displayed descriptions, then hide when leaving the screen.
9. Confirm the smaller text stays within the frame with space below the final line on all three cards.
