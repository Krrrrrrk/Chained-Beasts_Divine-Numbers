# Chained Beasts: Divine Numbers

A display-only BepInEx mod that adds a separate numbers box while you hold **Ctrl**.

The first build covers **Proud Lion**, **Lethal Eagle**, and **Mighty Bear** on Steam build **26231777**. See [the first-pass findings](docs/first-pass-findings.md) for the researched values and their limits.

Inspect a supported skill in the character-info tooltip, hover its row in the lobby character-details list, or select/hover a trainer skill card, then hold either **left or right Ctrl**. A separate box shows the numeric details. Release Ctrl to hide it. **Shift keeps its normal status explanations**; both keys can be held together.

Original names and descriptions are preserved. The extra box copies the game's native status-panel style and font. Unsupported skills, purchases, statuses, and afflictions do not receive guessed numbers. Beast selection and Codex pages are not included in this initial UI pass.

This is a **first test build**: compilation and offline checks pass, but the game has not been launched by the agent. In-game layout, input, and compatibility still require manual testing. No GitHub release has been published.

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
