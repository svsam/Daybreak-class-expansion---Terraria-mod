# Spears

Spears is an unofficial Terraria content mod for tModLoader. It turns the idea
behind Daybreak into a ten-weapon path running from the first bosses to a
post-Moon Lord finale, with lodging, debuffs, returning attacks, homing volleys,
and contact explosions.

<p align="center">
  <img src="spearsart/Post%20Moon%20Lord%20Spear.png" alt="Pixel-art illustration of the endgame Monarch's Spear" width="360">
</p>

## The problem

Daybreak has a distinctive thrown-spear mechanic, but it arrives at the very end
of Terraria. I wanted to find out whether that style could support an entire
progression without making ten copies of the same weapon or turning multiplayer
behaviour into a collection of special cases.

## The approach

The mod uses shared weapon profiles and one synchronized projectile state machine
for flying, lodging, penetrating, overshooting, returning, sawing, and terminal
states. Individual weapons combine those behaviours with data-driven balance
values and small pieces of specialised logic.

Multiplayer authority is divided deliberately: owning clients launch and steer
their projectiles, while the server owns NPC debuffs, Gold Curse rewards, and
Thorned cooldowns. Embedded spears are capped per player and target, and gameplay
timers advance once per game tick rather than once per projectile sub-update.

## Weapon progression

| Stage | Weapon | Main mechanic |
| --- | --- | --- |
| Pre-boss | Gold Spear | Tags ordinary enemies for a weighted bonus coin drop. |
| Post-Eater of Worlds | Post-Eater of Worlds Spear | Lodges and maintains Shadowflame. |
| Post-Brain of Cthulhu | Bloodspine | Lodges and maintains Ichor. |
| Post-Wall of Flesh | Hellrend | Throws three contact-exploding spears. |
| Post-Destroyer | Mightpiercer | Passes through one target, overshoots, and returns for a second hit. |
| Post-Twins | Post-Twins Spear | Pairs a debuff-only spear with a lodged explosive spear. |
| Post-Skeletron Prime | Post-Skeletron Prime Spear | Combines a returning spear, inferno spear, and pulsing saw. |
| Post-Plantera | FlowerSpike | Uses Venom primary fire and a Poisoned/Thorned alternate fire. |
| Post-Golem | Tepoztopilli | Produces an immediate fiery contact explosion. |
| Post-Moon Lord | Monarch's Spear | Builds five lineage volleys into a sixth-use homing final strike. |

## What I found

A shared projectile core was flexible enough to express all ten weapons while
keeping common networking, trail, targeting, and embed rules in one place. The
harder design problem was not projectile movement; it was deciding which machine
owns each side effect so a coin reward, debuff, or secondary projectile is not
created twice in multiplayer.

The repository now contains all ten weapon classes, game-ready 40x40 icons, the
source illustrations, recipes, English localisation, and a versioned design
manifest. Static repository validation checks that those pieces still agree.

## Current status

Version `0.2.0` targets Terraria `1.4.4.9`, tModLoader `2026.06`, .NET 8, and
C# 12. The source is implemented for single-player, host-and-play, and dedicated
server use, but the project is still in development. The included validator can
check files, declared balance values, artwork dimensions, packaging rules, and
sensitive-data leaks; it is not a substitute for live balance and multiplayer
play-testing.

## Install and build

The player build is published through the Steam Workshop; it can be found from
[my Steam profile](https://steamcommunity.com/id/SvSam/).

For source development, place the repository in a tModLoader Mod Sources setup
where `Spears.csproj` can import the adjacent `tModLoader.targets`, then build and
reload it through tModLoader's **Develop Mods** menu. A normal `dotnet build`
also works when that target file and the matching tModLoader installation are
available.

Run the repository-level checks separately with:

```powershell
python -m pip install -r tools/requirements-validation.txt
python tools/validate_repository.py
```

## Repository guide

| Path | Purpose |
| --- | --- |
| `Content/Items/Weapons/Spears/` | The ten craftable or dropped weapon items. |
| `Content/Projectiles/` | Shared projectile state machine and burst effects. |
| `Content/Common/` | Weapon profiles, targeting, source flags, and visual budgets. |
| `Content/NPCs/`, `Content/Buffs/` | Gold Curse, Thorned, and NPC-side state. |
| `DESIGN_MANIFEST.yaml` | Versioned mechanics, balance, networking, and validation contract. |
| `spearsart/` | High-resolution project-owned source illustrations. |
| `tools/validate_repository.py` | Static release and asset validation. |

## License and attribution

Copyright © 2026 SvSam. Project code, documentation, original artwork, and
derived item icons are released under the [MIT License](LICENSE.txt), except for
material identified in [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt).
Artwork provenance, including OpenAI-assisted icon conversion, is recorded in
[ASSET_PROVENANCE.md](ASSET_PROVENANCE.md).

Terraria is a trademark of Re-Logic, Inc. Spears is an independent fan project
and is not endorsed by, sponsored by, or affiliated with Re-Logic or the
tModLoader team.
