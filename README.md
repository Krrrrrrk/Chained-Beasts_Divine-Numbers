# Chained Beasts: Divine Numbers

Ever wish the game would just tell you what a skill *actually* does?

Divine Numbers adds a small info panel with the numbers behind skills, afflictions, potions, and other effects in **Chained Beasts**. Hold **Ctrl** while looking at something, or press **R3** on a controller, to see the details.

It doesn't replace the game's descriptions or change how anything works. It's just extra information when you want it. **Shift still works normally** for the game's built-in status explanations.

**Current version:** 0.2.4 · **Supported Steam build:** 26231777

**[Download Divine Numbers v0.2.4](https://github.com/Krrrrrrk/Chained-Beasts_Divine-Numbers/releases/tag/v0.2.4)**

## What does it show?

There are currently **206 entries** covering:

- 50 skills and beast variants
- 134 affliction entries (including different severity levels)
- 18 shop purchases
- 4 statuses

The panel shows things like actual stat changes, effect strengths, timers, and other values that aren't always obvious from the normal descriptions.

I haven't filled in numbers I couldn't verify. Locked or unknown descriptions won't get a made-up info card. There's also one training-related description whose trip-duration behavior I haven't pinned down yet. The [audit notes](docs/audit-report.md) have the full breakdown if you're curious.

## How to use it

### Keyboard and mouse

Hold **either Ctrl key** while inspecting something. Let go to hide the panel.

It works with:

- Unlocked, supported beast variants on the character-select screen
- Character-info tooltips and hovered detail rows
- Selected or hovered trainer cards
- Healer potions and shop offers
- Surgery and Codex descriptions

For healer potions, you don't need to hover over one tiny spot. The whole sign/potion preview area counts, including the header, price, border, pedestal, and empty space.

Sometimes a new description takes a moment to register. If the panel doesn't pop up immediately, keep Ctrl held for about half a second.

### Controller

Click **R3 (right stick)** to turn the numbers on or off. The panel follows whatever the game currently has highlighted, so you don't have to move a mouse around. Older v0.2.0 releases only support Ctrl; use v0.2.4 for controller support.

On the collapsed bottom HUD, R3 shows the first visible skill. Hold **RB** to use the game's normal character-info navigation to inspect other entries.

The toggle resets if you leave all inspection contexts, alt-tab away from the game, or disconnect the controller. If you're returning to a visible HUD, it can stay on. Ctrl works separately, so you can still use the keyboard whenever you want.

A few things that don't have native controller selection still need a mouse and Ctrl.

**Want to use L3 instead?** After launching the mod once, open:

`BepInEx/config/local.chainedbeasts.divinenumbers.cfg`

Under `[Input]`, set `ControllerToggleButton` to one of these:

- `RightStickClick` - default (R3)
- `LeftStickClick` - L3
- `Disabled` - no controller toggle

Restart the game after changing it. The default game controls don't use either stick click in the build I checked, but keep your own custom bindings in mind.

## Screenshots

These screenshots are from v0.2.0, showing the keyboard/Ctrl panel. Controller support and the larger potion hover area were added later.

**Character select - Hulking Bear**

![Hulking Bear's character-select description beside a Divine Numbers card showing health, movement speed and invulnerability values.](docs/screenshots/character-select-bear.png)

**Character info - Agile Ibex**

![The Agile Ibex character-info tooltip beside its movement-speed and dodge-stamina numbers.](docs/screenshots/character-info-ibex.png)

**Trainer - Arm Fatigue III**

![The trainer's Arm Fatigue III description beside a Divine Numbers card explaining its delay, slowing strength and timer reset.](docs/screenshots/trainer-arm-fatigue.png)

**Healer - Tunnel Vision II**

![Healer potion offers with a Divine Numbers card explaining Tunnel Vision II's mask blend strength.](docs/screenshots/healer-tunnel-vision.png)

## Installation

1. Install **BepInEx 6 (IL2CPP)** for Chained Beasts if you haven't already.
2. Download the ZIP from the [latest release](https://github.com/Krrrrrrk/Chained-Beasts_Divine-Numbers/releases/latest).
3. Close the game and extract the ZIP into your **Chained Beasts** game folder. Merge the included `BepInEx` folder with the one that's already there.
4. Start the game. That's it.

You **don't** need my Mod Foundation project for this one. Divine Numbers runs on its own.

To uninstall it, delete `BepInEx/plugins/ChainedBeasts.DivineNumbers/ChainedBeasts.DivineNumbers.dll`.

**Important:** The mod checks that the game's executable, metadata, and two asset files match the build it was made for. If an update changes those files, Divine Numbers will disable its UI hooks rather than risk showing incorrect information. If an update breaks it, check back for a newer release.

## Building it yourself

You'll need the **.NET SDK** and the BepInEx IL2CPP interop files from your installed game.

Build the plugin:

```powershell
dotnet build src/ChainedBeasts.DivineNumbers/ChainedBeasts.DivineNumbers.csproj -c Release
```

Run the logic checks:

```powershell
dotnet run --project tests/LogicChecks/LogicChecks.csproj -c Release
```

The finished DLL will be at:

`src/ChainedBeasts.DivineNumbers/bin/Release/net6.0/ChainedBeasts.DivineNumbers.dll`

Copy it into `BepInEx/plugins/ChainedBeasts.DivineNumbers/`. That's the only DLL this mod needs.

If you want to build, verify, and package a release ZIP in one go, use `scripts/build.ps1`. The verification scripts require **Python 3.11+**.

## Technical stuff

Divine Numbers is **display-only**. It doesn't change combat calculations, save data, or network traffic. The values come from checking the game's code and assets, not from guessing based on the descriptions. That also means this isn't a promise that every damage outcome or every multiplayer/controller situation has been tested.

If you want to dig into how the numbers were found or how the UI works, here's the relevant stuff:

- [Numbers and source references](data/numeric-details.json)
- [Coverage list](data/coverage.json)
- [Build and asset evidence](data/expanded-evidence.json)
- [Catalog verification script](scripts/verify-catalog.py)
- [UI behavior and testing notes](docs/ctrl-details.md)
- [Original research notes](docs/first-pass-findings.md)
- [Full audit and known limitations](docs/audit-report.md)

Game files, localization catalogs, and decompiler output aren't included in this repository. The MIT license covers the original code and other work in this project, not Chained Beasts or its assets.
