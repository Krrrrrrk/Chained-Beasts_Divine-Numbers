# Chained Beasts: Divine Numbers

A display-only BepInEx mod that adds a separate numbers box while you hold **Ctrl**.

The first build covers **Proud Lion**, **Lethal Eagle**, and **Mighty Bear** on Steam build **26231777**. See [the first-pass findings](docs/first-pass-findings.md) for the researched values and their limits.

On **character select**, highlight a supported beast variant and hold either **left or right Ctrl**. This also works in the character-info tooltip, on hovered skill rows in the lobby character-details list, and on selected/hovered trainer skill cards. Release Ctrl to hide the box. **Shift keeps its normal status explanations**; both keys can be held together.

Original names and descriptions are preserved. The extra box copies the game's native status-panel style and uses smaller 22-point text with explicit padding. Unsupported skills, purchases, statuses, locked variants, and afflictions do not receive guessed numbers. Codex pages are not included yet.

Version **0.1.1** adds character-select support and more compact text. The user's screenshot confirms that the 0.1.0 in-game Ctrl box rendered; the new character-select behavior and revised layout still need manual testing. Compilation and offline checks pass. No GitHub release has been published.

Build with the .NET SDK and the installed game's BepInEx IL2CPP interop files:

```powershell
dotnet build src/ChainedBeasts.DivineNumbers/ChainedBeasts.DivineNumbers.csproj -c Release
dotnet run --project tests/LogicChecks/LogicChecks.csproj -c Release
```

The output is `src/ChainedBeasts.DivineNumbers/bin/Release/net6.0/ChainedBeasts.DivineNumbers.dll`. With the game closed, place that DLL in `BepInEx/plugins/ChainedBeasts.DivineNumbers/`. Only this mod's DLL is needed; there is no Foundation dependency. Remove the DLL to uninstall. The mod refuses an uninspected game binary after an update until its values are re-audited.

- [Numbers box data](data/first-pass-details.json)
- [Build fingerprint and evidence index](data/first-pass-evidence.json)
- [Read-only verification script](scripts/verify-first-pass.py)
- [UI implementation and manual checks](docs/ctrl-details.md)

Original game files, extracted localization catalogs, and decompiler output are excluded from Git. The existing MIT license applies to this project's original work, not the game or its assets.
