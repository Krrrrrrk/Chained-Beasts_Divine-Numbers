# First pass: three beast abilities

Investigated 2026-10-28 against installed Steam build **26231777**. Results come from Unity assets and native-code inspection, with the installed binaries checked against the existing Cpp2IL dumps. No game launch, combat measurements, multiplayer session, save edits, or mod installation were performed.

| Ability | Numbers found | Important detail |
| --- | --- | --- |
| **Proud Lion** (Lion variant I) | **+40% attack damage, +20% movement speed, 10 seconds** | Pose after a qualifying kill while your crowd is wild. The buff includes yourself and teammates. |
| **Lethal Eagle** (Eagle variant I) | **2x direct-impact damage on the next throw; 8-second opportunity** | A melee kill readies Power Throw. Launching a projectile consumes it. |
| **Mighty Bear** (Bear variant II) | **+25% base health, -20% movement speed; blunt melee attacks impose a 0.3-second minimum base stun** | The existing description already states +20% blunt-weapon damage. Stun is a minimum, not an extra 0.3 seconds added to every hit. |

“Lv1/Lv2/Lv3” are internal variant identifiers, not necessarily cumulative ranks. These findings should not be presented as three abilities that a beast automatically stacks by leveling up.

## Proud Lion

Current description says that posing after kills with a wild crowd gives allies a damage and speed boost, without either percentage or duration.

Proposed description:

> Posing to a wild crowd after kills gives you and all allies +40% damage and +20% movement speed for 10s.

The code checks Lion skill ID 0 when the pose input succeeds, then calls `Player.ApplyDamageBoostRpc(10)`. That RPC traverses `PlayerManager.LocalPlayers` on each receiving peer and applies effect **27 / GloatBoost**. There is no exclusion of the posing Lion and no distance test in that application loop. This is evidence for the intended recipient scope, not a completed multiplayer test.

`Player.ModifyAttackDamage` multiplies outgoing attack damage by **1.4** while GloatBoost is active. `Player.GetModifiedPlayerVelocity` multiplies the movement vector by **1.2** for that same effect. It is a separate status from Vigor; the Vigor tooltip's 40% movement bonus is not the Lion's speed bonus.

A nominal 10-damage attack becomes 14 at this modifier's stage of the calculation. Other multipliers, random damage variation, target defenses, and rounding can still change final HP loss. This finding does not establish that every independent damage source, such as arena hazards or explosions, receives the bonus.

Still to check: repeated poses and multiple Lions, refresh/stacking rules, death/revive interactions, and actual UI display on host and clients.

## Lethal Eagle

Current description: “Powerful throw after melee kills.”

Proposed description:

> Melee kills ready your next powerful throw for 8s. That throw deals +100% direct-impact damage; launching a projectile consumes the effect.

`Player.OnBattleEventActor` checks skill ID **3** and a qualifying melee kill, then applies effect **18 / PowerThrow** for **8 seconds**. An embedded debug message claims “4s,” but its immediately following duration load resolves to **8.0** in the installed binary. The executable value takes precedence over that message.

`Projectile.Launch` records Power Throw on the projectile and removes it from a player thrower. `Projectile.DealDamage` then multiplies the direct-hit damage by **2.0**, before the projectile's ordinary damage scale is applied. The branch for AOE damage is separate. “Double damage for eight seconds” would be misleading: the player has up to eight seconds to launch the next powered projectile, and the direct-impact bonus is attached to that projectile.

This is not an assertion that Power Throw only changes damage. The throw preparation path also checks this status; animation/timing behavior, unblockability, special projectiles, and other interactions need a separate audit before promising a complete mechanical description. A missed throw can still consume the effect because consumption happens on launch.

Still to check: multiple/projectile-producing weapons, refresh rules, shield throws, AOE exceptions, host/client behavior, and final displayed damage.

## Mighty Bear

The current description gives +20% blunt damage, but leaves “More health, less movement speed” and “briefly stun” unquantified.

Proposed description:

> Base health +25%, movement speed -20%. Blunt weapons deal +20% damage; blunt melee hits impose at least 0.3s base stun.

`PlayerData.MaxHealth` checks Bear skill IDs **6, 7, or 8** and multiplies the starting health component by **1.25**. With the ordinary 60 base HP and neutral progression/other modifiers, that component becomes **75 HP**. This is not a permanent flat +15 HP at every progression level. `Player.GetModifiedPlayerVelocity` uses **0.8** for the Bear beast check.

For skill ID **7**, `Player.ModifyAttackDamage` uses **1.2** when the held primary weapon's damage type is **3 / Bludgeon**. The implementation reads the held weapon, so do not automatically promise this bonus for every thrown object or alternate damage source without tracing their paths.

In `Weapon.AttackEffect`, a Bludgeon hit with skill 7 raises the prepared stun duration to **0.3 seconds** if it was shorter. An existing longer stun is preserved. The duration then passes through the normal stunner multiplier and target-side stun handling. Therefore this is a **minimum base stun**, not +0.3 seconds on top of an existing stun, and not a guarantee that every enemy is disabled for exactly 0.3 seconds.

Still to check: susceptible versus resistant enemies/bosses, hits interrupted or blocked before application, thrown weapons, charged attacks that change damage type, and layout of the expanded text.

## Mod feasibility and next scope

The game's `StringManager.GetSkillDescTerm` resolves descriptions using `Skills/{SkillType}_Desc`. The three exact keys are included in [the proposed text data](../data/first-pass-descriptions.json). A small BepInEx localization patch can target these descriptions without changing ability calculations. In-game display and compatibility remain untested; no plugin has been built in this first pass.

The registered catalog contains **113 skill definitions**, including **24 beast variants** and records with different release statuses. Registration alone does not prove an entry is obtainable. A full audit should separate beast variants, ordinary skills, surgeries/purchases, injuries, and shared statuses, and trace each vague phrase to its owning mechanic. Only the three samples above have been investigated here.

## Evidence and reproduction

[The evidence index](../data/first-pass-evidence.json) records the exact game hashes, float addresses and decoded values, input-dump hashes, and relevant method/instruction references. Full extracted localization and native dumps remain in the ignored `local/` directory or the existing parent analysis workspace.

Run the read-only verifier with Python 3:

```powershell
python scripts/verify-first-pass.py --game-dir "C:\Program Files (x86)\Steam\steamapps\common\Chained Beasts" --dump-dir "..\analysis\update-20261027\isil\IsilDump\Assembly-CSharp" --localization-json "local\I2Languages.json"
```

Only `--game-dir` is required. Optional inputs validate the recorded dumps and source English descriptions as well. The verifier checks provenance, constants, and data consistency; it does not prove the interpretation of native control flow or replace gameplay tests. It fails if the installed game differs from the recorded build, so the fixed addresses are not silently reused after an update.
