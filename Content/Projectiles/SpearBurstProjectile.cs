using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Spears.Content.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Projectiles;

public sealed class SpearBurstProjectile : ModProjectile
{
	private bool _fromMonarch;
	private bool _suppressVisuals;

	private SpearKind VisualKind => (SpearKind)(int)Projectile.ai[0];
	private float Radius => Math.Max(1f, Projectile.ai[1]);

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
	}

	internal static int Spawn(IEntitySource source, Vector2 center, int owner, int damage, float knockback, SpearKind visualKind, int radius, bool fromMonarch)
	{
		int index = Projectile.NewProjectile(source, center, Vector2.Zero, ModContent.ProjectileType<SpearBurstProjectile>(), damage, knockback, owner, (float)visualKind, radius);
		if (index >= 0 && index < Main.maxProjectiles) {
			Main.projectile[index].CritChance = 0;
			if (Main.projectile[index].ModProjectile is SpearBurstProjectile burst)
				burst._fromMonarch = fromMonarch;
			Main.projectile[index].netUpdate = true;
		}
		return index;
	}

	public override void SendExtraAI(BinaryWriter writer) => writer.Write(_fromMonarch);
	public override void ReceiveExtraAI(BinaryReader reader) => _fromMonarch = reader.ReadBoolean();
	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !Main.player[Projectile.owner].active) {
			_suppressVisuals = true;
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.Kill();
			return;
		}

		WeaponProfile weapon = WeaponProfileRegistry.Get(VisualKind);
		if (SpearVisualEffects.IsPrimaryUpdate(Projectile))
			SpearVisualEffects.AddLight(Projectile.Center, weapon.Color, weapon.LightStrength, _fromMonarch ? SpearLightRole.MonarchBurst : SpearLightRole.Burst);
	}

	public override bool? CanDamage() => _suppressVisuals ? false : null;
	public override bool? CanHitNPC(NPC target) => target.active && !target.friendly && !target.dontTakeDamage ? null : false;

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
		return Vector2.DistanceSquared(Projectile.Center, nearest) <= Radius * Radius;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.DisableCrit();

	public override bool PreDraw(ref Color lightColor)
	{
		if (_suppressVisuals)
			return false;
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color color = Color.Lerp(lightColor, WeaponProfileRegistry.Get(VisualKind).Color, 0.4f) * (Projectile.timeLeft / 3f) * 0.45f;
		for (int i = 0; i < 24; i++) {
			float angle = MathHelper.TwoPi * i / 24f;
			Vector2 point = Projectile.Center + angle.ToRotationVector2() * Radius - Main.screenPosition;
			Main.EntitySpriteDraw(pixel, point, null, color, angle + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(2f, 5f), SpriteEffects.None);
		}
		return false;
	}
}
