# Javelin Expansion

Javelin Expansion is a Terraria mod that adds ten throwable, Daybreak-style javelins across the full game progression. Each weapon has a unique effect, including lodging, debuffs, homing attacks, and explosions.

The current source targets Terraria 1.4.4.9 through tModLoader 2026.06, .NET 8, and C# 12.

## Weapon progression

| Stage | Weapon | Main mechanic |
| --- | --- | --- |
| Pre-boss | Gilded Oath | Tags ordinary enemies for a weighted bonus coin drop |
| Post-Eater of Worlds | Night's Spine | Lodges and maintains Shadowflame |
| Post-Brain of Cthulhu | Crimson Vow | Lodges and maintains Ichor |
| Post-Wall of Flesh | Hellrend | Three contact-exploding javelins |
| Post-Destroyer | Mightpiercer | Homes through a target, overshoots, and strikes again |
| Post-Twins | Gemini Gaze | Paired Cursed Inferno and delayed-explosion javelins |
| Post-Skeletron Prime | Frightsteel | Returning javelin, inferno javelin, and lodged saw |
| Post-Plantera | Venom Bloom | Venom primary fire and Poisoned/Thorned alternate fire |
| Post-Golem | Tepoztopilli | Immediate fiery contact explosion |
| Post-Moon Lord | Monarch's Ascension | Five full-lineage volleys followed by a homing final strike |

## Artwork and compatibility

Version 0.3.0 replaces every 40×40 weapon icon with its exact original transparent source illustration. Inventory art is drawn at a 64-pixel tip-to-tail length (about a 45-pixel diagonal footprint), and thrown javelins at 128 pixels. The Monarch final strike keeps its 1.65× visual size. Each source has its own pivot and angle so the blade follows the throw and stays at the existing damage hitbox. Softer trails keep the main weapon readable.

The `Spears` internal mod ID, item IDs, recipe keys, damage, and attack behaviour are retained for existing saves. The public mod name is **Javelin Expansion**; these are reusable thrown melee weapons in the style of Daybreak.

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

Copyright © 2026 SvSam. Project code, documentation, original artwork, and runtime textures are released under the [MIT License](LICENSE.txt), except for material identified in [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt). Artwork provenance and the use of unchanged source illustrations are recorded in [ASSET_PROVENANCE.md](ASSET_PROVENANCE.md).

Terraria is a trademark of Re-Logic, Inc. Javelin Expansion is an independent fan project and is not endorsed by, sponsored by, or affiliated with Re-Logic or the tModLoader team. Terraria and its original assets are not included under this project's MIT license.
