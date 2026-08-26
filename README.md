# Spears

Spears is a Terraria content mod that adds a ten-weapon progression of thrown, Daybreak-style spears. Every tier has a distinct job: money utility, lodged debuffs, contact explosions, target-return attacks, paired mechanical weapons, crowd control, and a post-Moon-Lord arsenal combo.

The current source targets Terraria 1.4.4.9 through tModLoader 2026.06, .NET 8, and C# 12. Multiplayer-safe code paths are implemented for single-player, host-and-play, and dedicated servers; the full gameplay acceptance matrix must pass before a public release is tagged.

## Weapon progression

| Stage | Weapon | Theme |
| --- | --- | --- |
| Pre-boss | Gold Spear | Tags ordinary enemies for a weighted bonus coin drop |
| Post-Eater of Worlds | Post-Eater of Worlds Spear | Lodges and maintains Shadowflame |
| Post-Brain of Cthulhu | Bloodspine | Lodges and maintains Ichor |
| Post-Wall of Flesh | Hellrend | Three contact-exploding spears |
| Post-Destroyer | Mightpiercer | Homes through a target, overshoots, and strikes again |
| Post-Twins | Post-Twins Spear | Paired Cursed Inferno and delayed-explosion spears |
| Post-Skeletron Prime | Post-Skeletron Prime Spear | Returning spear, inferno spear, and lodged saw |
| Post-Plantera | FlowerSpike | Venom primary fire and Poisoned/Thorned alternate fire |
| Post-Golem | Tepoztopilli | Immediate fiery contact explosion |
| Post-Moon Lord | Monarch's Spear | Five full-lineage volleys followed by a homing final strike |

The Eater of Worlds and Brain of Cthulhu paths are separate. Upgrade recipes consume their listed predecessor, and every boss-associated recipe also checks that the relevant boss has been defeated.

## Installation

For normal play, install the published mod through tModLoader's Steam Workshop browser and enable **Spears** before entering a world. Both clients and the server need the same mod version for multiplayer.

For source builds, place this repository in `Terraria/tModLoader/ModSources/Spears`, open tModLoader's Workshop menu, and choose **Develop Mods > Build + Reload**. The project is configured for tModLoader 2026.06 and its .NET 8 toolchain.

## Artwork and replacement workflow

High-resolution source illustrations live in `spearsart/`. Game-ready 40×40 transparent item icons live in `Content/Items/Weapons/Spears/` and are currently reused as projectile textures.

To replace an item or projectile illustration:

1. Preserve the internal item filename listed in `DESIGN_MANIFEST.yaml`.
2. Export a 40×40 PNG with transparent background, crisp nearest-neighbour pixels, and no unused opaque border.
3. Replace the corresponding file under `Content/Items/Weapons/Spears/`.
4. Build and inspect the item in inventory, in flight, and against both light and dark backgrounds.
5. Update `ASSET_PROVENANCE.md` if the source, artist, or transformation method changed.

Procedural trails and secondary effects intentionally have no committed raster assets. Runtime references to Terraria dusts, sounds, and buff icons do not redistribute Terraria files.

## Development and design

`DESIGN_MANIFEST.yaml` version 4 is the approved design reference. Gameplay values are compiled into the mod; the YAML file is not loaded at runtime and is excluded from the distributed `.tmod` package. The mod deliberately remains on Terraria 1.4.4.9, so FlowerSpike uses Venom instead of the later Spored debuff.

A quick compile check can be run with:

```powershell
dotnet build Spears.csproj --no-restore -p:BuildMod=false -p:TargetFramework=net8.0 -p:LangVersion=12.0
```

Use tModLoader's **Build + Reload** for the authoritative content and packaging test.

## License and attribution

Copyright © 2026 SvSam. Project code, documentation, original artwork, and derived item icons are released under the [MIT License](LICENSE.txt), except for material identified in [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt). Artwork provenance, including OpenAI-assisted icon conversion, is recorded in [ASSET_PROVENANCE.md](ASSET_PROVENANCE.md).

Terraria is a trademark of Re-Logic, Inc. Spears is an independent fan project and is not endorsed by, sponsored by, or affiliated with Re-Logic or the tModLoader team. Terraria and its original assets are not included under this project's MIT license.
