# Expanded effect audit: 0.2.0

The inventory reviews 113 registered skills, 101 afflictions and 63 purchases, plus localized training-only skills and 11 shared status explanations. Affliction severities are separate descriptions. The resulting coverage file has 434 rows, including unreleased and intentionally concealed entries.

| Classification | Description rows |
| --- | ---: |
| Numeric card or researched shared-status supplement | 206 |
| Existing concrete quantity or behavior; no extra claim selected | 136 |
| Unreleased/testing/abandoned definition | 68 |
| No active description for this severity | 14 |
| Intentionally concealed random-affliction outcome | 6 |
| Qualitative visual effect without a selected numeric gameplay claim | 3 |
| Unresolved localized training-only entry | 1 |

Cards comprise 50 skills/variants, 134 affliction severity entries, 18 purchases and four statuses. A card can supply omitted shared-status numbers without claiming every possible interaction has been exhaustively measured. Original descriptions are retained, including discrepancies.

## Selected findings

| Effect | Confirmed omitted information |
| --- | --- |
| Bear variants | +25% base health, -20% movement; Bear I base invulnerability/Fury 1.75 seconds |
| Panther variants | +25% attack damage, -25% base health; Panther III repeated-hit bonus +12.5% per repeat, capped at +50% |
| Stag variants | +20% movement; Stag III +25% chain length per relevant end and +5 base chain damage per relevant end |
| Wolf pickups | Wolf II +4 coins; Wolf III heals 3% maximum health, rounded up |
| Vigor | +40% movement and a separate extra 6 stamina/second, including during normal recovery delay |
| Alacrity | At rank 1, Vigor becomes +60% movement and 15 extra stamina/second |
| Weak | -25% attack damage, movement and normal stamina recovery; no maximum-stamina penalty from Weak itself |
| Follow Through | Combat code uses 20% melee-stun chance per rank; localization says 15% |
| Pickup disarm | 16% / 32% by severity |
| Melee stamina affliction | Fixed +15% melee damage; stamina cost +25% / +50% / +75% |
| Critical-stamina affliction | Adds 0.3 / 0.6 to the crit multiplier; drains current stamina |
| Gambling | 1% chance of +100 coins; otherwise -5 or -10 |
| Wirecutters | Checks a chain segment within one world unit of the attack point; no random success roll in the cut check |
| Surgery healing | 7 / 10 / 15 HP by act of the last completed arena, subject to healing limits |
| Bow quiver | Fired ordinary arrows have 12 base damage; throwing the bow itself has a different 5-damage projectile |
| Explosive arrow | 5 base impact damage, 9 base explosion damage and a 4-unit explosion radius; separate damage sources |
| More pots/weapons | Enables arena-designated extra spawn points; no universal count or percentage |

See the curated card JSON for the other probabilities, durations, severity formulas, vision timings, distance thresholds and weapon bases. Each entry includes references to traced methods or serialized fields. Native dump files and full asset/localization exports remain local.

## Qualifications and remaining uncertainty

- Base damage is not measured final HP loss. Charge, the player's level, day scaling, random variation, target resistance and other skills can apply. Arrow effects must be traced through the fired arrow prefab rather than the throwable bow prefab.
- Backstab uses the audited native getter at display time because a setting selects 2x or 1.5x. The target can resist the bonus. The card does not assume a setting.
- Wolf's extra dismemberment roll is conditional and varies with waves; the card gives the confirmed upper limit rather than claiming a flat chance on every hit.
- Visual curves and texture masks do not imply a fixed world-space sight radius, viewing angle or percentage of the screen lost. Cards label relative strength, blend strength and animation periods accordingly. Trippy Vision and the red Fury overlay have no selected additional gameplay number.
- Hook Spear's card supplies weapon bases. It does not invent a health cutoff for the original phrase about weaker enemies or claim exhaustive shield-removal eligibility.
- Localized training-only **Trip From Behind** (skill 10006) has no confirmed native trip-duration branch in the inspected output. It remains unresolved in the inventory and receives no guessed card. This ID is distinct from affliction 10006, whose stamina-regeneration branch must not be mistaken for this skill.
- Some registered descriptions already give an exact quantity or behavior, so they intentionally have no duplicate numeric card. Concealed random afflictions stay concealed. Unreleased definitions are excluded.

## Verification boundary

Compilation and offline checks pass. The read-only verifier checks four game hashes, 157 native float literals, 35 native dump hashes, catalog/coverage consistency and real localization keys. These establish source identity and literal values; semantic interpretation still comes from the static audit. The user's confirmation establishes the previous 0.1.1 UI worked well. The expanded 0.2.0 UI paths and all numeric mechanics still need manual runtime/gameplay checks. No game was launched or automated for this audit.

```powershell
python scripts/verify-catalog.py --game-dir 'C:\Program Files (x86)\Steam\steamapps\common\Chained Beasts'
```

Optional `--dump-dir` and `--localization-json` arguments verify locally available Cpp2IL dumps and the AssetRipper localization export. The standalone original three-card verifier is retained for its initial evidence anchors.
