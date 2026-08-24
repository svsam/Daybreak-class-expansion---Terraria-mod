using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Spears.Content.Common;
using Terraria;
using Terraria.ModLoader;

namespace Spears.Content.Projectiles;

public sealed class MonarchOrbitalProjectile : ModProjectile
{
	private int Slot => (int)Projectile.ai[0];
	private SpearKind Identity => (SpearKind)(int)Projectile.ai[1];

	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.friendly = false;
		Projectile.hostile = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = 2;
		Projectile.netImportant = true;
		Projectile.hide = false;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool? CanDamage() => false;

	public override void AI()
	{
		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) {
			Projectile.Kill();
			return;
		}

		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead || owner.HeldItem?.ModItem is not ProgressionSpearItem spearItem || spearItem.SpearKind != SpearKind.Monarch) {
			Projectile.Kill();
			return;
		}

		Projectile.timeLeft = 2;
		float angle = (float)(Main.GameUpdateCount * 0.025f) + MathHelper.TwoPi * Slot / 3f;
		Vector2 offset = new(MathF.Cos(angle) * 48f, -42f + MathF.Sin(angle) * 10f);
		Projectile.Center = owner.MountedCenter + offset;
		Projectile.rotation = angle + MathHelper.PiOver4;
		Lighting.AddLight(Projectile.Center, WeaponProfileRegistry.Get(Identity).Color.ToVector3() * 0.35f);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = ModContent.Request<Texture2D>(WeaponProfileRegistry.TexturePath(Identity), AssetRequestMode.ImmediateLoad).Value;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, 0.65f, SpriteEffects.None);
		return false;
	}
}
