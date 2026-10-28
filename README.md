# Chained Beasts: Divine Numbers

Research for a description-only mod that explains hidden ability numbers.

The first investigation covers **Proud Lion**, **Lethal Eagle**, and **Mighty Bear** on Steam build **26231777**. See [the first-pass findings](docs/first-pass-findings.md) for values, draft descriptions, implementation details, and remaining checks.

This repository currently contains research and proposed text. There is no installable mod or release yet. No game launch or gameplay testing was performed for this investigation.

The intended mod changes descriptions without changing combat balance. Each added number must be traced to the game's data or code, with its trigger, duration, recipients, and relevant exceptions checked.

- [Proposed description data](data/first-pass-descriptions.json)
- [Build fingerprint and evidence index](data/first-pass-evidence.json)
- [Read-only verification script](scripts/verify-first-pass.py)

Original game files, extracted localization catalogs, and decompiler output are excluded from Git. The existing MIT license applies to this project's original work, not the game or its assets.
