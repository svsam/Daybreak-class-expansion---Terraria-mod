using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Common;

internal static class WeaponProfileRegistry
{
	private const string ItemTextureRoot = "Spears/Content/Items/Weapons/Spears/";

	private static readonly IReadOnlyDictionary<SpearKind, WeaponProfile> Profiles =
		new Dictionary<SpearKind, WeaponProfile> {
			[SpearKind.Gold] = new(SpearKind.Gold, ItemTextureRoot + "GoldSpear", 15, 32, 5f, 5f, ItemRarityID.White, Item.sellPrice(silver: 2), 0, 0.5f, 32, 3f, new Color(255, 205, 55), lightStrength: 0.25f, directHitCanCrit: false, impactDebuff: SpearImpactDebuff.Midas, debuffChancePercent: 50, debuffDurationTicks: 300),
			[SpearKind.Corruption] = new(SpearKind.Corruption, ItemTextureRoot + "NightsSpine", 28, 28, 5.5f, 5.5f, ItemRarityID.Blue, Item.sellPrice(silver: 50), 6, 0.65f, 40, 4f, new Color(122, 62, 188), secondaryEffect: SpearSecondaryEffect.ShadowThorn),
			[SpearKind.Crimson] = new(SpearKind.Crimson, ItemTextureRoot + "BloodSpine", 28, 28, 5.5f, 5.5f, ItemRarityID.Blue, Item.sellPrice(silver: 50), 6, 0.65f, 40, 4f, new Color(205, 45, 65), lightStrength: 0.3f, secondaryEffect: SpearSecondaryEffect.BloodNeedles),
			[SpearKind.Hellrend] = new(SpearKind.Hellrend, ItemTextureRoot + "Hellrend", 52, 25, 6f, 6f, ItemRarityID.LightRed, Item.sellPrice(gold: 2), 12, 0.75f, 48, 5f, new Color(255, 92, 34), lightStrength: 0.65f, impactDebuff: SpearImpactDebuff.Hellfire, debuffDurationTicks: 300),
			[SpearKind.Mightpiercer] = new(SpearKind.Mightpiercer, ItemTextureRoot + "Mightpiercer", 66, 24, 6.25f, 6.5f, ItemRarityID.Pink, Item.sellPrice(gold: 3), 18, 0.8f, 52, 5.5f, new Color(190, 38, 52), lightStrength: 0.45f, armorPenetration: 4, secondaryEffect: SpearSecondaryEffect.MightArcs),
			[SpearKind.Gemini] = new(SpearKind.Gemini, ItemTextureRoot + "GeminiGaze", 78, 22, 6.5f, 7f, ItemRarityID.Pink, Item.sellPrice(gold: 4), 24, 0.85f, 56, 6f, new Color(111, 230, 129), lightStrength: 0.55f, secondaryEffect: SpearSecondaryEffect.Gemini),
			[SpearKind.Frightsteel] = new(SpearKind.Frightsteel, ItemTextureRoot + "Frightsteel", 92, 20, 7f, 7.5f, ItemRarityID.LightPurple, Item.sellPrice(gold: 5), 30, 0.9f, 60, 6.5f, new Color(220, 226, 238), lightStrength: 0.55f, armorPenetration: 8, secondaryEffect: SpearSecondaryEffect.PrimeBlades),
			[SpearKind.FlowerSpike] = new(SpearKind.FlowerSpike, ItemTextureRoot + "FlowerSpike", 110, 19, 6.75f, 8f, ItemRarityID.Lime, Item.sellPrice(gold: 6), 40, 1f, 64, 6f, new Color(246, 93, 164), lightStrength: 0.5f, impactDebuff: SpearImpactDebuff.Venom, debuffDurationTicks: 360, secondaryEffect: SpearSecondaryEffect.SeekingPetals),
			[SpearKind.Tepoztopilli] = new(SpearKind.Tepoztopilli, ItemTextureRoot + "Tepoztopilli", 135, 18, 7.25f, 8.5f, ItemRarityID.Yellow, Item.sellPrice(gold: 8), 55, 1.1f, 72, 7f, new Color(255, 184, 42), lightStrength: 0.75f, armorPenetration: 15, impactDebuff: SpearImpactDebuff.OnFire, debuffDurationTicks: 360, secondaryEffect: SpearSecondaryEffect.TempleShards),
			[SpearKind.Monarch] = new(SpearKind.Monarch, ItemTextureRoot + "MonarchsSpear", 260, 10, 8f, 10f, ItemRarityID.Red, Item.sellPrice(gold: 20), 0, 1.35f, 88, 8f, new Color(255, 245, 193), lightStrength: 1f, armorPenetration: 20, extraCrit: 6),
			[SpearKind.VanillaSpear] = new(SpearKind.VanillaSpear, "Terraria/Images/Item_" + ItemID.Spear, 260, 10, 8f, 10f, ItemRarityID.Red, 0, 0, 0.5f, 32, 3f, new Color(190, 190, 190)),
			[SpearKind.Daybreak] = new(SpearKind.Daybreak, "Terraria/Images/Item_" + ItemID.DayBreak, 260, 10, 8f, 10f, ItemRarityID.Red, 0, 100, 1f, 72, 7f, new Color(255, 137, 38))
		};

	public static WeaponProfile Get(SpearKind kind)
	{
		if (Profiles.TryGetValue(kind, out WeaponProfile profile))
			return profile;

		throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown spear identity.");
	}

	public static string TexturePath(SpearKind kind) => Get(kind).Spear.TexturePath;

	public static SpearKind[] MonarchCycle { get; } = {
		SpearKind.Gold,
		SpearKind.Corruption,
		SpearKind.Crimson,
		SpearKind.Hellrend,
		SpearKind.Mightpiercer,
		SpearKind.Gemini,
		SpearKind.Frightsteel,
		SpearKind.FlowerSpike,
		SpearKind.Tepoztopilli,
		SpearKind.VanillaSpear,
		SpearKind.Daybreak
	};
}
