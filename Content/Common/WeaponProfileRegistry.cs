using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Common;

internal static class WeaponProfileRegistry
{
	private const string ItemTextureRoot = "Spears/Content/Items/Weapons/Spears/";

	private static readonly IReadOnlyDictionary<SpearKind, WeaponProfile> Weapons =
		new Dictionary<SpearKind, WeaponProfile> {
			[SpearKind.Gold] = new(SpearKind.Gold, ItemTextureRoot + "GoldSpear", 18, 28, 5f, 5f, ItemRarityID.White, Item.sellPrice(silver: 2), new Color(255, 205, 55), 0.25f),
			[SpearKind.Corruption] = new(SpearKind.Corruption, ItemTextureRoot + "NightsSpine", 36, 25, 5.5f, 5.5f, ItemRarityID.Blue, Item.sellPrice(silver: 50), new Color(122, 62, 188)),
			[SpearKind.Crimson] = new(SpearKind.Crimson, ItemTextureRoot + "BloodSpine", 36, 25, 5.5f, 5.5f, ItemRarityID.Blue, Item.sellPrice(silver: 50), new Color(205, 45, 65), 0.3f),
			[SpearKind.Hellrend] = new(SpearKind.Hellrend, ItemTextureRoot + "Hellrend", 60, 24, 6f, 6f, ItemRarityID.LightRed, Item.sellPrice(gold: 2), new Color(255, 92, 34), 0.65f),
			[SpearKind.Mightpiercer] = new(SpearKind.Mightpiercer, ItemTextureRoot + "Mightpiercer", 88, 22, 6.25f, 7f, ItemRarityID.Pink, Item.sellPrice(gold: 3), new Color(190, 38, 52), 0.45f, 8),
			[SpearKind.Gemini] = new(SpearKind.Gemini, ItemTextureRoot + "GeminiGaze", 108, 21, 6.5f, 7.5f, ItemRarityID.Pink, Item.sellPrice(gold: 4), new Color(111, 230, 129), 0.55f, 8),
			[SpearKind.Frightsteel] = new(SpearKind.Frightsteel, ItemTextureRoot + "Frightsteel", 132, 20, 7f, 8f, ItemRarityID.LightPurple, Item.sellPrice(gold: 5), new Color(220, 226, 238), 0.55f, 12),
			[SpearKind.FlowerSpike] = new(SpearKind.FlowerSpike, ItemTextureRoot + "FlowerSpike", 150, 19, 6.75f, 8.5f, ItemRarityID.Lime, Item.sellPrice(gold: 6), new Color(246, 93, 164), 0.5f, 10),
			[SpearKind.Tepoztopilli] = new(SpearKind.Tepoztopilli, ItemTextureRoot + "Tepoztopilli", 170, 18, 7.25f, 9f, ItemRarityID.Yellow, Item.sellPrice(gold: 8), new Color(255, 184, 42), 0.75f, 20),
			[SpearKind.Monarch] = new(SpearKind.Monarch, ItemTextureRoot + "MonarchsSpear", 500, 16, 8f, 10f, ItemRarityID.Red, Item.sellPrice(gold: 20), new Color(255, 245, 193), 0.35f, 30, 10)
		};

	private static readonly IReadOnlyDictionary<SpearAttackKind, SpearAttackProfile> Attacks =
		new Dictionary<SpearAttackKind, SpearAttackProfile> {
			[SpearAttackKind.Gold] = new(SpearAttackKind.Gold, SpearKind.Gold, SpearAttackBehavior.Contact, debuff: SpearImpactDebuff.GoldCurse, debuffDurationTicks: 300, canCrit: false),
			[SpearAttackKind.Corruption] = new(SpearAttackKind.Corruption, SpearKind.Corruption, SpearAttackBehavior.LodgeDebuff, debuff: SpearImpactDebuff.ShadowFlame, lodgedDurationTicks: 240, lingeringDebuffTicks: 120),
			[SpearAttackKind.Crimson] = new(SpearAttackKind.Crimson, SpearKind.Crimson, SpearAttackBehavior.LodgeDebuff, debuff: SpearImpactDebuff.Ichor, lodgedDurationTicks: 240, lingeringDebuffTicks: 120),
			[SpearAttackKind.Hellrend] = new(SpearAttackKind.Hellrend, SpearKind.Hellrend, SpearAttackBehavior.ExplodeOnContact, explosionDamageMultiplier: 0.75f, explosionRadius: 48, debuff: SpearImpactDebuff.Hellfire, debuffDurationTicks: 180, tileExplodes: true),
			[SpearAttackKind.Mightpiercer] = new(SpearAttackKind.Mightpiercer, SpearKind.Mightpiercer, SpearAttackBehavior.PierceReturn, returnDamageMultiplier: 1.25f, homingRange: 800f, homingTurnDegrees: 8f),
			[SpearAttackKind.GeminiInferno] = new(SpearAttackKind.GeminiInferno, SpearKind.Gemini, SpearAttackBehavior.DebuffOnly, debuff: SpearImpactDebuff.CursedInferno, debuffDurationTicks: 240, homingRange: 800f, homingTurnDegrees: 10f, canCrit: false),
			[SpearAttackKind.GeminiDamage] = new(SpearAttackKind.GeminiDamage, SpearKind.Gemini, SpearAttackBehavior.LodgeExplode, explosionDamageMultiplier: 0.85f, explosionRadius: 56, lodgedDurationTicks: 240, homingRange: 800f, homingTurnDegrees: 10f, tileExplodes: true),
			[SpearAttackKind.FrightDestroyer] = new(SpearAttackKind.FrightDestroyer, SpearKind.Frightsteel, SpearAttackBehavior.PierceReturn, initialDamageMultiplier: 0.65f, returnDamageMultiplier: 0.8f, homingRange: 800f, homingTurnDegrees: 8f),
			[SpearAttackKind.FrightInferno] = new(SpearAttackKind.FrightInferno, SpearKind.Frightsteel, SpearAttackBehavior.DebuffOnly, debuff: SpearImpactDebuff.CursedInferno, debuffDurationTicks: 240, homingRange: 800f, homingTurnDegrees: 10f, canCrit: false),
			[SpearAttackKind.PrimeSaw] = new(SpearAttackKind.PrimeSaw, SpearKind.Frightsteel, SpearAttackBehavior.Saw, initialDamageMultiplier: 0.75f, pulseDamageMultiplier: 0.15f, debuff: SpearImpactDebuff.Bleeding, debuffDurationTicks: 600, lodgedDurationTicks: 240, homingRange: 800f, homingTurnDegrees: 10f),
			[SpearAttackKind.FlowerPrimary] = new(SpearAttackKind.FlowerPrimary, SpearKind.FlowerSpike, SpearAttackBehavior.LodgeDebuff, debuff: SpearImpactDebuff.Venom, lodgedDurationTicks: 240, lingeringDebuffTicks: 120),
			[SpearAttackKind.FlowerThorn] = new(SpearAttackKind.FlowerThorn, SpearKind.FlowerSpike, SpearAttackBehavior.FlowerThorn, debuff: SpearImpactDebuff.Poisoned, debuffDurationTicks: 480, canCrit: false),
			[SpearAttackKind.Tepoztopilli] = new(SpearAttackKind.Tepoztopilli, SpearKind.Tepoztopilli, SpearAttackBehavior.ExplodeOnContact, explosionDamageMultiplier: 0.8f, explosionRadius: 72, debuff: SpearImpactDebuff.OnFire, debuffDurationTicks: 480, tileExplodes: true),
			[SpearAttackKind.MonarchFinal] = new(SpearAttackKind.MonarchFinal, SpearKind.Monarch, SpearAttackBehavior.MonarchFinal, homingRange: 1200f, homingTurnDegrees: 12f)
		};

	internal static readonly SpearAttackKind[] MonarchVolley = {
		SpearAttackKind.Gold,
		SpearAttackKind.Corruption,
		SpearAttackKind.Crimson,
		SpearAttackKind.Hellrend,
		SpearAttackKind.Mightpiercer,
		SpearAttackKind.GeminiInferno,
		SpearAttackKind.GeminiDamage,
		SpearAttackKind.PrimeSaw,
		SpearAttackKind.FlowerPrimary,
		SpearAttackKind.Tepoztopilli
	};

	internal static WeaponProfile Get(SpearKind kind) =>
		Weapons.TryGetValue(kind, out WeaponProfile profile)
			? profile
			: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown spear identity.");

	internal static SpearAttackProfile GetAttack(SpearAttackKind kind) =>
		Attacks.TryGetValue(kind, out SpearAttackProfile profile)
			? profile
			: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown spear attack.");

	internal static string TexturePath(SpearKind kind) => Get(kind).TexturePath;

	internal static SpearAttackKind PrimaryAttack(SpearKind kind) => kind switch {
		SpearKind.Gold => SpearAttackKind.Gold,
		SpearKind.Corruption => SpearAttackKind.Corruption,
		SpearKind.Crimson => SpearAttackKind.Crimson,
		SpearKind.Hellrend => SpearAttackKind.Hellrend,
		SpearKind.Mightpiercer => SpearAttackKind.Mightpiercer,
		SpearKind.Gemini => SpearAttackKind.GeminiDamage,
		SpearKind.Frightsteel => SpearAttackKind.PrimeSaw,
		SpearKind.FlowerSpike => SpearAttackKind.FlowerPrimary,
		SpearKind.Tepoztopilli => SpearAttackKind.Tepoztopilli,
		SpearKind.Monarch => SpearAttackKind.MonarchFinal,
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No primary attack for spear identity.")
	};
}
