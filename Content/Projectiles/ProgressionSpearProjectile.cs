using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Spears.Content.Buffs;
using Spears.Content.Common;
using Spears.Content.NPCs;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Projectiles;

public sealed class ProgressionSpearProjectile : ModProjectile
{
	private const int FlightGravityDelayUpdates = 45;
	private const int MaximumFlightUpdates = 3600;
	private const int EmbeddedLifetimeUpdates = 600;
	private const int EmbeddedCapPerOwner = 8;

	private Vector2 _embedOffset;
	private Vector2 _impactDirection = Vector2.UnitX;
	private SpearKind _identity;
	private SpearSourceFlags _sourceFlags = SpearSourceFlags.Main;
	private bool _geminiSpazmatismMode;
	private bool _detonated;
	private int _homingTargetIndex = -1;
	private int _launchDamage;

	private bool IsFlying => Projectile.ai[1] == 0f;
	private bool IsEmbedded => Projectile.ai[1] > 0f;
	private int EmbeddedTargetIndex => (int)Projectile.ai[1] - 1;
	private bool HasOwnerAuthority => Main.netMode == NetmodeID.SinglePlayer || Projectile.owner == Main.myPlayer;
	internal SpearKind EffectiveKind => (_sourceFlags & SpearSourceFlags.Monarch) != 0 ? _identity : (SpearKind)(int)Projectile.ai[0];

	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 5;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.DamageType = DamageClass.MeleeNoSpeed;
		Projectile.penetrate = -1;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.extraUpdates = 1;
		Projectile.timeLeft = 7200;
		Projectile.alpha = 255;
		Projectile.netImportant = true;
		Projectile.hide = false;
	}

	public override void OnSpawn(IEntitySource source)
	{
		_identity = (SpearKind)(int)Projectile.ai[0];
		_impactDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		_launchDamage = Projectile.damage;
	}

	internal static int Spawn(
		IEntitySource source,
		Vector2 position,
		Vector2 velocity,
		int damage,
		float knockback,
		int owner,
		SpearKind kind,
		SpearSourceFlags sourceFlags = SpearSourceFlags.Main,
		SpearKind? identity = null,
		bool geminiSpazmatismMode = false)
	{
		int index = Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ProgressionSpearProjectile>(), damage, knockback, owner, (float)kind);
		if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is ProgressionSpearProjectile spear) {
			spear._sourceFlags = sourceFlags;
			spear._identity = identity ?? kind;
			spear._geminiSpazmatismMode = geminiSpazmatismMode;
			spear._impactDirection = velocity.SafeNormalize(Vector2.UnitX);
			spear._launchDamage = damage;
			Main.projectile[index].netUpdate = true;
		}

		return index;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write((byte)_identity);
		writer.Write((byte)_sourceFlags);
		writer.Write(_geminiSpazmatismMode);
		writer.Write(_detonated);
		writer.Write(_embedOffset.X);
		writer.Write(_embedOffset.Y);
		writer.Write(_impactDirection.X);
		writer.Write(_impactDirection.Y);
		writer.Write((short)_homingTargetIndex);
		writer.Write(_launchDamage);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		_identity = (SpearKind)reader.ReadByte();
		_sourceFlags = (SpearSourceFlags)reader.ReadByte();
		_geminiSpazmatismMode = reader.ReadBoolean();
		_detonated = reader.ReadBoolean();
		_embedOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
		_impactDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
		_homingTargetIndex = reader.ReadInt16();
		_launchDamage = reader.ReadInt32();
	}

	public override void AI()
	{
		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !Main.player[Projectile.owner].active) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.Kill();
			return;
		}

		if (IsEmbedded)
			EmbeddedAI();
		else
			FlyingAI();
	}

	private void FlyingAI()
	{
		Projectile.ai[2]++;
		Projectile.alpha = Math.Max(0, Projectile.alpha - 25);
		_impactDirection = Projectile.velocity.SafeNormalize(_impactDirection);
		Projectile.rotation = _impactDirection.ToRotation() + MathHelper.PiOver4;

		if ((SpearKind)(int)Projectile.ai[0] == SpearKind.Monarch && Projectile.ai[2] >= 30f) {
			if (HasOwnerAuthority && !IsValidHomingTarget(_homingTargetIndex) && (int)Projectile.ai[2] % 10 == 0) {
				int nextTarget = SpearTargeting.FindClosestVisibleTarget(Projectile.Center, 800f);
				if (nextTarget != _homingTargetIndex) {
					_homingTargetIndex = nextTarget;
					Projectile.netUpdate = true;
				}
			}
			if (IsValidHomingTarget(_homingTargetIndex))
				SpearTargeting.HomeTowards(Projectile, Main.npc[_homingTargetIndex].Center, Math.Max(Projectile.velocity.Length(), 1f), MathHelper.ToRadians(4f));
		}
		else if (Projectile.ai[2] > FlightGravityDelayUpdates) {
			Projectile.velocity.X *= 0.995f;
			Projectile.velocity.Y += 0.15f;
		}

		WeaponProfile profile = WeaponProfileRegistry.Get(EffectiveKind);
		Lighting.AddLight(Projectile.Center, profile.Color.ToVector3() * 0.35f);
		if (Main.rand.NextBool(4)) {
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, -Projectile.velocity * 0.04f, 120, profile.Color, 0.8f);
			dust.noGravity = true;
		}

		if (Projectile.ai[2] >= MaximumFlightUpdates && HasOwnerAuthority)
			Detonate();
	}

	private static bool IsValidHomingTarget(int index)
	{
		return index >= 0 && index < Main.maxNPCs && Main.npc[index].active && Main.npc[index].CanBeChasedBy();
	}

	private void EmbeddedAI()
	{
		Projectile.ai[2]++;
		Projectile.damage = 0;
		Projectile.velocity = Vector2.Zero;
		Projectile.tileCollide = false;
		Projectile.alpha = 0;

		int targetIndex = EmbeddedTargetIndex;
		if (targetIndex < 0 || targetIndex >= Main.maxNPCs || !Main.npc[targetIndex].active || Main.npc[targetIndex].life <= 0) {
			if (HasOwnerAuthority)
				Detonate();
			return;
		}

		NPC target = Main.npc[targetIndex];
		target.GetGlobalNPC<SpearGlobalNPC>().RegisterEmbedded(Projectile.whoAmI);
		Projectile.Center = target.Center + _embedOffset;
		Projectile.gfxOffY = target.gfxOffY;
		Projectile.rotation = _impactDirection.ToRotation() + MathHelper.PiOver4;

		WeaponProfile profile = WeaponProfileRegistry.Get(EffectiveKind);
		Lighting.AddLight(Projectile.Center, profile.Color.ToVector3() * 0.25f);
		if (Main.rand.NextBool(12)) {
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, Vector2.Zero, 140, profile.Color, 0.65f);
			dust.noGravity = true;
		}

		if (Projectile.ai[2] >= EmbeddedLifetimeUpdates && HasOwnerAuthority)
			Detonate();
	}

	public override bool? CanDamage() => IsFlying && !_detonated ? null : false;

	public override bool? CanHitNPC(NPC target) => IsFlying && target.active && !target.friendly && !target.dontTakeDamage ? null : false;

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		WeaponProfile attackProfile = WeaponProfileRegistry.Get((SpearKind)(int)Projectile.ai[0]);
		if (!attackProfile.DirectHitCanCrit)
			modifiers.DisableCrit();
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (!IsFlying || _detonated)
			return;

		WeaponProfileRegistry.Get(EffectiveKind).TryApplyImpactDebuff(target);
		_embedOffset = Projectile.Center - target.Center;
		_impactDirection = Projectile.velocity.SafeNormalize(_impactDirection);
		Projectile.ai[1] = target.whoAmI + 1;
		Projectile.ai[2] = 0f;
		Projectile.velocity = Vector2.Zero;
		Projectile.tileCollide = false;
		Projectile.netUpdate = true;
		target.GetGlobalNPC<SpearGlobalNPC>().RegisterEmbedded(Projectile.whoAmI);

		if (HasOwnerAuthority)
			EnforceEmbeddedCap(target.whoAmI);
	}

	private void EnforceEmbeddedCap(int targetIndex)
	{
		List<ProgressionSpearProjectile> embedded = new();
		for (int i = 0; i < Main.maxProjectiles; i++) {
			Projectile candidate = Main.projectile[i];
			if (!candidate.active || candidate.owner != Projectile.owner || candidate.type != Type)
				continue;

			if (candidate.ModProjectile is ProgressionSpearProjectile spear && spear.IsEmbedded && spear.EmbeddedTargetIndex == targetIndex)
				embedded.Add(spear);
		}

		while (embedded.Count > EmbeddedCapPerOwner) {
			ProgressionSpearProjectile oldest = embedded[0];
			for (int i = 1; i < embedded.Count; i++) {
				ProgressionSpearProjectile candidate = embedded[i];
				if (candidate.Projectile.ai[2] > oldest.Projectile.ai[2] || candidate.Projectile.ai[2] == oldest.Projectile.ai[2] && candidate.Projectile.identity < oldest.Projectile.identity)
					oldest = candidate;
			}

			embedded.Remove(oldest);
			oldest.Detonate();
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (HasOwnerAuthority)
			Detonate();
		return false;
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		width = 10;
		height = 10;
		return true;
	}

	internal bool IsEmbeddedIn(int npcIndex) => IsEmbedded && EmbeddedTargetIndex == npcIndex && !_detonated;

	internal int EmbeddedDotDps => IsEmbedded && !_detonated ? WeaponProfileRegistry.Get(EffectiveKind).DotDps : 0;

	private void Detonate()
	{
		if (_detonated || !Projectile.active)
			return;

		_detonated = true;
		Projectile.netUpdate = true;
		if (HasOwnerAuthority)
			CreateDetonationEffects();

		Projectile.Kill();
	}

	private void CreateDetonationEffects()
	{
		WeaponProfile selected = WeaponProfileRegistry.Get(EffectiveKind);
		SpawnBurst(selected, false);
		SpawnSelectedSecondary(selected);

		if ((_sourceFlags & SpearSourceFlags.Monarch) != 0) {
			SpawnBurst(WeaponProfileRegistry.Get(SpearKind.Monarch), true);
			SpawnIdentityCopies();
		}
	}

	private void SpawnBurst(WeaponProfile profile, bool appliesFear)
	{
		int damage = Math.Max(1, (int)(_launchDamage * profile.BurstDamageMultiplier));
		SpearBurstProjectile.Spawn(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, damage, profile.BurstKnockback, profile.Kind, profile.BurstRadius, appliesFear);
	}

	private void SpawnSelectedSecondary(WeaponProfile profile)
	{
		Vector2 direction = _impactDirection.SafeNormalize(Vector2.UnitX);
		IEntitySource source = Projectile.GetSource_FromThis();

		switch (profile.SecondaryEffect) {
			case SpearSecondaryEffect.ShadowThorn:
				SpearSecondaryProjectile.Spawn(source, Projectile.Center, direction * 14f, Projectile.owner, ScaleDamage(0.35f), 0f, SpearSecondaryKind.ShadowThorn, profile.Kind);
				break;

			case SpearSecondaryEffect.BloodNeedles:
				foreach (float degrees in new[] { -30f, -10f, 10f, 30f })
					SpearSecondaryProjectile.Spawn(source, Projectile.Center, direction.RotatedBy(MathHelper.ToRadians(degrees)) * 10f, Projectile.owner, ScaleDamage(0.2f), 0f, SpearSecondaryKind.BloodNeedle, profile.Kind);
				break;

			case SpearSecondaryEffect.MightArcs:
				int firstTarget = SpearTargeting.FindClosestVisibleTarget(Projectile.Center, 180f);
				if (firstTarget >= 0)
					SpearSecondaryProjectile.SpawnArc(source, Projectile.Center, Projectile.owner, ScaleDamage(0.4f), firstTarget, profile.Kind);
				int secondTarget = SpearTargeting.FindClosestVisibleTarget(Projectile.Center, 180f, firstTarget);
				if (secondTarget >= 0)
					SpearSecondaryProjectile.SpawnArc(source, Projectile.Center, Projectile.owner, ScaleDamage(0.4f), secondTarget, profile.Kind);
				break;

			case SpearSecondaryEffect.Gemini:
				SpearSecondaryKind geminiKind = _geminiSpazmatismMode ? SpearSecondaryKind.SpazmatismFlare : SpearSecondaryKind.RetinazerBeam;
				float geminiSpeed = _geminiSpazmatismMode ? 12f : 24f;
				SpearSecondaryProjectile.Spawn(source, Projectile.Center, direction * geminiSpeed, Projectile.owner, ScaleDamage(0.5f), 0f, geminiKind, profile.Kind);
				break;

			case SpearSecondaryEffect.PrimeBlades:
				for (int i = 0; i < 4; i++)
					SpearSecondaryProjectile.Spawn(source, Projectile.Center, (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 10f, Projectile.owner, ScaleDamage(0.35f), 0f, SpearSecondaryKind.PrimeBlade, profile.Kind);
				break;

			case SpearSecondaryEffect.SeekingPetals:
				for (int i = 0; i < 6; i++) {
					float angle = MathHelper.Lerp(MathHelper.ToRadians(-50f), MathHelper.ToRadians(50f), i / 5f);
					SpearSecondaryProjectile.Spawn(source, Projectile.Center, direction.RotatedBy(angle) * 8f, Projectile.owner, ScaleDamage(0.25f), 0f, SpearSecondaryKind.SeekingPetal, profile.Kind, preferredTarget: i);
				}
				break;

			case SpearSecondaryEffect.TempleShards:
				for (int i = 0; i < 6; i++)
					SpearSecondaryProjectile.Spawn(source, Projectile.Center, (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 10f, Projectile.owner, ScaleDamage(0.3f), 0f, SpearSecondaryKind.TempleShard, profile.Kind);
				break;
		}
	}

	private void SpawnIdentityCopies()
	{
		IEntitySource source = Projectile.GetSource_FromThis();
		SpearKind[] cycle = WeaponProfileRegistry.MonarchCycle;
		for (int i = 0; i < cycle.Length; i++) {
			Vector2 velocity = (MathHelper.TwoPi * i / cycle.Length).ToRotationVector2() * 12f;
			SpearSecondaryProjectile.Spawn(source, Projectile.Center, velocity, Projectile.owner, ScaleDamage(0.25f), 0f, SpearSecondaryKind.IdentityCopy, cycle[i]);
		}
	}

	private int ScaleDamage(float multiplier) => Math.Max(1, (int)(_launchDamage * multiplier));

	public override bool PreDraw(ref Color lightColor)
	{
		WeaponProfile profile = WeaponProfileRegistry.Get(EffectiveKind);
		Texture2D texture = ModContent.Request<Texture2D>(profile.TexturePath, AssetRequestMode.ImmediateLoad).Value;
		Vector2 origin = texture.Size() * 0.5f;
		SpriteEffects effects = SpriteEffects.None;
		float opacity = Projectile.Opacity;

		if (IsFlying) {
			for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
				if (Projectile.oldPos[i] == Vector2.Zero)
					continue;
				float fade = (Projectile.oldPos.Length - i) / (float)(Projectile.oldPos.Length + 1) * 0.35f;
				Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
				Main.EntitySpriteDraw(texture, drawPosition, null, profile.Color * (fade * opacity), Projectile.rotation, origin, 1f, effects);
			}
		}

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * opacity, Projectile.rotation, origin, 1f, effects);
		return false;
	}
}
