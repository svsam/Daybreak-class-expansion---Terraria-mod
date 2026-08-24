using Microsoft.Xna.Framework;
using Spears.Content.Players;
using Spears.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Common;

/// <summary>
/// Thin concrete spear items only need to select a profile and register acquisition.
/// All throwing and shared defaults live here so the progression stays mechanically consistent.
/// </summary>
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

	public sealed override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.whoAmI != Main.myPlayer)
			return false;

		if (SpearKind == SpearKind.Hellrend) {
			Vector2 perpendicular = velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * 12f;
			ProgressionSpearProjectile.Spawn(source, position, velocity, damage, knockback, player.whoAmI, SpearKind.Hellrend);
			ProgressionSpearProjectile.Spawn(source, position + perpendicular, velocity.RotatedBy(MathHelper.ToRadians(24f)), (int)(damage * 0.5f), knockback, player.whoAmI, SpearKind.Hellrend);
			ProgressionSpearProjectile.Spawn(source, position - perpendicular, velocity.RotatedBy(MathHelper.ToRadians(-24f)), (int)(damage * 0.5f), knockback, player.whoAmI, SpearKind.Hellrend);
			return false;
		}

		SpearPlayer spearPlayer = player.GetModPlayer<SpearPlayer>();
		SpearKind identity = SpearKind;
		SpearSourceFlags flags = SpearSourceFlags.Main;
		bool geminiMode = false;

		if (SpearKind == SpearKind.Monarch) {
			identity = spearPlayer.NextMonarchIdentity();
			flags |= SpearSourceFlags.Monarch;
		}

		if (identity == SpearKind.Gemini)
			geminiMode = spearPlayer.NextGeminiMode(SpearKind == SpearKind.Monarch);

		ProgressionSpearProjectile.Spawn(source, position, velocity, damage, knockback, player.whoAmI, SpearKind, flags, identity, geminiMode);
		return false;
	}
}
