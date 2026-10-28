# Chained Beasts: Divine Numbers

A display-only BepInEx mod that shows researched numbers in a separate box while you hold **Ctrl**. Original descriptions stay intact; **Shift keeps its normal status explanations**.

Version **0.2.0** expands the three-card prototype to **206 cards**: 50 skills/beast variants, 134 affliction severity entries, 18 purchases and four statuses. It targets Steam build **26231777**. [Audit findings and limits](docs/audit-report.md) explain coverage and base values.

On **character select**, highlight an unlocked supported variant and hold either Ctrl key. Character-info tooltips, hovered character-detail rows and selected/hovered trainer cards also work. In **shop, surgery and Codex screens**, hover the displayed description text while holding Ctrl. New labels may take up to half a second to register while Ctrl stays held. Release Ctrl to hide the box.

The panel retains the smaller 22-point text and padding confirmed working by the user in 0.1.1. New 0.2.0 screens/mechanics have build, static and offline verification; they still need manual gameplay review. Unknown/locked descriptions receive no guessed card. One training-only description has no confirmed native trip-duration branch and is recorded as unresolved.

## Build and install

Use the .NET SDK and the installed game's BepInEx IL2CPP interop files:

```powershell
dotnet build src/ChainedBeasts.DivineNumbers/ChainedBeasts.DivineNumbers.csproj -c Release
dotnet run --project tests/LogicChecks/LogicChecks.csproj -c Release
```

With the game closed, copy `src/ChainedBeasts.DivineNumbers/bin/Release/net6.0/ChainedBeasts.DivineNumbers.dll` into `BepInEx/plugins/ChainedBeasts.DivineNumbers/`. Only this DLL is needed; there is no Foundation dependency. Remove it to uninstall. `scripts/build.ps1` builds, verifies and packages a test ZIP (Python 3.11+ is required for provenance checks). No GitHub release has been published for this update.

The mod checks the audited binary, metadata and two asset files before enabling UI hooks. A changed build disables it until re-audited. It changes no combat, saves or network traffic.

- [Card data and source references](data/numeric-details.json)
- [Coverage of registered definitions and localized training/status entries](data/coverage.json)
- [Build fingerprints, literals and asset facts](data/expanded-evidence.json)
- [Read-only verification](scripts/verify-catalog.py)
- [UI behavior and manual checks](docs/ctrl-details.md)
- [Original three-card research](docs/first-pass-findings.md)

Original game files, full localization catalogs and decompiler output are excluded from Git. The MIT license applies to this project's original work, not the game or its assets.
