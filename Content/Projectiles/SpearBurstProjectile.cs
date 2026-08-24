using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Spears.Content.Buffs;
using Spears.Content.Common;
using Spears.Content.NPCs;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Projectiles;

public sealed class SpearBurstProjectile : ModProjectile
{
	private SpearKind Kind => (SpearKind)(int)Projectile.ai[0];
	private float Radius => Math.Max(1f, Projectile.ai[1]);
	private bool AppliesFear => Projectile.ai[2] > 0f;

	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.DamageType = DamageClass.MeleeNoSpeed;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = 3;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.hide = false;
	}

	internal static int Spawn(IEntitySource source, Vector2 center, int owner, int damage, float knockback, SpearKind kind, int radius, bool appliesFear)
	{
		int index = Projectile.NewProjectile(source, center, Vector2.Zero, ModContent.ProjectileType<SpearBurstProjectile>(), damage, knockback, owner, (float)kind, radius, appliesFear ? 1f : 0f);
		if (index >= 0 && index < Main.maxProjectiles)
			Main.projectile[index].CritChance = 0;
		return index;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !Main.player[Projectile.owner].active) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.Kill();
			return;
		}

		WeaponProfile profile = WeaponProfileRegistry.Get(Kind);
		if (!Main.dedServ)
			Lighting.AddLight(Projectile.Center, profile.Spear.Color.ToVector3() * profile.Spear.LightStrength);
		if (!Main.dedServ && Projectile.timeLeft == 3) {
			for (int i = 0; i < 16; i++) {
				Vector2 velocity = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 4f);
				Dust dust = Dust.NewDustPerfect(Projectile.Center + velocity.SafeNormalize(Vector2.UnitX) * Radius * 0.4f, DustID.TintableDustLighted, velocity, 100, profile.Spear.Color, 1f);
				dust.noGravity = true;
			}
		}
	}

	public override bool? CanHitNPC(NPC target) => target.active && !target.friendly && !target.dontTakeDamage ? null : false;

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
		return Vector2.DistanceSquared(Projectile.Center, nearest) <= Radius * Radius;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.DisableCrit();
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		WeaponProfileRegistry.Get(Kind).Spear.TryApplyImpactDebuff(target);
		if (AppliesFear && SpearGlobalNPC.CanCrowdControl(target))
			target.AddBuff(ModContent.BuffType<TheKingsOfKings>(), 300);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color color = WeaponProfileRegistry.Get(Kind).Spear.Color * (Projectile.timeLeft / 3f) * 0.75f;
		const int segmentCount = 24;
		for (int i = 0; i < segmentCount; i++) {
			float angle = MathHelper.TwoPi * i / segmentCount;
			Vector2 point = Projectile.Center + angle.ToRotationVector2() * Radius - Main.screenPosition;
			Main.EntitySpriteDraw(pixel, point, null, color, angle + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(2f, 5f), SpriteEffects.None);
		}
		return false;
	}
}
