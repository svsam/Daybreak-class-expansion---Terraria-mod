using System;
using Microsoft.Xna.Framework;
using Spears.Content.Players;
using Spears.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Common;

public abstract class ProgressionSpearItem : ModItem
{
	internal abstract SpearKind SpearKind { get; }

	public sealed override void SetStaticDefaults()
	{
		ItemID.Sets.Spears[Type] = true;
	}

	public sealed override void SetDefaults()
	{
		WeaponProfile profile = WeaponProfileRegistry.Get(SpearKind);
		Item.damage = profile.Damage;
		Item.DamageType = DamageClass.MeleeNoSpeed;
		Item.width = 40;
		Item.height = 40;
		Item.useTime = profile.UseTime;
		Item.useAnimation = profile.UseTime;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.knockBack = profile.Knockback;
		Item.value = profile.Value;
		Item.rare = profile.Rarity;
		Item.UseSound = SoundID.Item1;
		Item.autoReuse = true;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.shoot = ModContent.ProjectileType<ProgressionSpearProjectile>();
		Item.shootSpeed = profile.ShootSpeed;
		Item.crit += profile.ExtraCrit;
		Item.ArmorPenetration = profile.ArmorPenetration;
		Item.ResearchUnlockCount = 1;
	}

	public sealed override bool AltFunctionUse(Player player) => SpearKind == SpearKind.FlowerSpike;

	public sealed override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.whoAmI != Main.myPlayer)
			return false;

		switch (SpearKind) {
			case SpearKind.Hellrend:
				SpawnHellrendVolley(source, player, position, velocity, damage, knockback);
				break;
			case SpearKind.Gemini:
				SpawnGeminiVolley(source, player, position, velocity, damage, knockback);
				break;
			case SpearKind.Frightsteel:
				SpawnFrightsteelVolley(source, player, position, velocity, damage, knockback);
				break;
			case SpearKind.FlowerSpike:
				SpearAttackKind flowerAttack = player.altFunctionUse == 2 ? SpearAttackKind.FlowerThorn : SpearAttackKind.FlowerPrimary;
				ProgressionSpearProjectile.Spawn(source, position, velocity, damage, knockback, player.whoAmI, flowerAttack);
				break;
			case SpearKind.Monarch:
				SpawnMonarchAttack(source, player, position, velocity, damage, knockback);
				break;
			default:
				ProgressionSpearProjectile.Spawn(source, position, velocity, damage, knockback, player.whoAmI, WeaponProfileRegistry.PrimaryAttack(SpearKind));
				break;
		}

		return false;
	}

	private static void SpawnHellrendVolley(IEntitySource source, Player player, Vector2 position, Vector2 velocity, int damage, float knockback)
	{
		Vector2 perpendicular = velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * 12f;
		ProgressionSpearProjectile.Spawn(source, position, velocity, damage, knockback, player.whoAmI, SpearAttackKind.Hellrend);
		ProgressionSpearProjectile.Spawn(source, position + perpendicular, velocity.RotatedBy(MathHelper.ToRadians(24f)), ScaleDamage(damage, 0.65f), knockback, player.whoAmI, SpearAttackKind.Hellrend);
		ProgressionSpearProjectile.Spawn(source, position - perpendicular, velocity.RotatedBy(MathHelper.ToRadians(-24f)), ScaleDamage(damage, 0.65f), knockback, player.whoAmI, SpearAttackKind.Hellrend);
	}

	private static void SpawnGeminiVolley(IEntitySource source, Player player, Vector2 position, Vector2 velocity, int damage, float knockback)
	{
		int target = SpearTargeting.FindClosestVisibleTarget(position, 800f);
		ProgressionSpearProjectile.Spawn(source, position, velocity.RotatedBy(MathHelper.ToRadians(-4f)), damage, knockback, player.whoAmI, SpearAttackKind.GeminiInferno, initialTarget: target);
		ProgressionSpearProjectile.Spawn(source, position, velocity.RotatedBy(MathHelper.ToRadians(4f)), damage, knockback, player.whoAmI, SpearAttackKind.GeminiDamage, initialTarget: target);
	}

	private static void SpawnFrightsteelVolley(IEntitySource source, Player player, Vector2 position, Vector2 velocity, int damage, float knockback)
	{
		int target = SpearTargeting.FindClosestVisibleTarget(position, 800f);
		ProgressionSpearProjectile.Spawn(source, position, velocity.RotatedBy(MathHelper.ToRadians(-8f)), damage, knockback, player.whoAmI, SpearAttackKind.FrightDestroyer, initialTarget: target);
		ProgressionSpearProjectile.Spawn(source, position, velocity, damage, knockback, player.whoAmI, SpearAttackKind.FrightInferno, initialTarget: target);
		ProgressionSpearProjectile.Spawn(source, position, velocity.RotatedBy(MathHelper.ToRadians(8f)), damage, knockback, player.whoAmI, SpearAttackKind.PrimeSaw, initialTarget: target);
	}

	private static void SpawnMonarchAttack(IEntitySource source, Player player, Vector2 position, Vector2 velocity, int damage, float knockback)
	{
		if (player.GetModPlayer<SpearPlayer>().NextMonarchUseIsFinal()) {
			Vector2 finalVelocity = velocity.SafeNormalize(Vector2.UnitX) * 8f;
			int target = SpearTargeting.FindClosestVisibleTarget(position, 1200f);
			ProgressionSpearProjectile.Spawn(source, position, finalVelocity, damage, knockback, player.whoAmI, SpearAttackKind.MonarchFinal, initialTarget: target);
			return;
		}

		SpearAttackKind[] volley = WeaponProfileRegistry.MonarchVolley;
		int componentDamage = ScaleDamage(damage, 0.2f);
		int sharedTarget = SpearTargeting.FindClosestVisibleTarget(position, 1000f);
		for (int i = 0; i < volley.Length; i++) {
			float angle = MathHelper.Lerp(MathHelper.ToRadians(-18f), MathHelper.ToRadians(18f), i / (float)(volley.Length - 1));
			Vector2 componentVelocity = velocity.SafeNormalize(Vector2.UnitX).RotatedBy(angle) * 8f;
			ProgressionSpearProjectile.Spawn(source, position, componentVelocity, componentDamage, knockback, player.whoAmI, volley[i], SpearSourceFlags.MonarchVolley, sharedTarget);
		}

		int daybreakIndex = Projectile.NewProjectile(
			source,
			position,
			velocity.SafeNormalize(Vector2.UnitX) * 8f,
			ProjectileID.Daybreak,
			componentDamage,
			knockback,
			player.whoAmI);
		if (daybreakIndex >= 0 && daybreakIndex < Main.maxProjectiles)
			Main.projectile[daybreakIndex].CritChance = 0;
	}

	private static int ScaleDamage(int damage, float multiplier) => Math.Max(1, (int)MathF.Round(damage * multiplier));
}
