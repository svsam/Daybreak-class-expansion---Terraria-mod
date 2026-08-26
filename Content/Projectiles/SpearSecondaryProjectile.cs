using System;
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

public sealed class SpearSecondaryProjectile : ModProjectile
{
	private bool _initialized;
	private bool _suppressVisualsWhileAwaitingRemoval;
	private bool _fromMonarch;
	private int _preferredTargetOrdinal;
	private int _nextTargetSearchUpdate;

	private SpearSecondaryKind SecondaryKind => (SpearSecondaryKind)(int)Projectile.ai[0];
	private int TargetIndex {
		get => (int)Projectile.ai[1] - 1;
		set => Projectile.ai[1] = value + 1;
	}
	private SpearKind Identity => (SpearKind)(int)Projectile.ai[2];
	private bool HasOwnerAuthority => Main.netMode == NetmodeID.SinglePlayer || Projectile.owner == Main.myPlayer;

	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetDefaults()
	{
		Projectile.width = 10;
		Projectile.height = 10;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.DamageType = DamageClass.MeleeNoSpeed;
		Projectile.penetrate = 1;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = 90;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.hide = false;
	}

	internal static int Spawn(
		IEntitySource source,
		Vector2 position,
		Vector2 velocity,
		int owner,
		int damage,
		float knockback,
		SpearSecondaryKind secondaryKind,
		SpearKind identity,
		int targetIndex = -1,
		int preferredTarget = 0,
		bool fromMonarch = false)
	{
		int index = Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<SpearSecondaryProjectile>(), damage, knockback, owner, (float)secondaryKind, targetIndex + 1, (float)identity);
		if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is SpearSecondaryProjectile secondary) {
			secondary._preferredTargetOrdinal = preferredTarget;
			secondary._fromMonarch = fromMonarch;
			Main.projectile[index].CritChance = 0;
			Main.projectile[index].netUpdate = true;
		}
		return index;
	}

	internal static int SpawnArc(IEntitySource source, Vector2 position, int owner, int damage, int targetIndex, SpearKind identity, bool fromMonarch = false)
		=> Spawn(source, position, Vector2.Zero, owner, damage, 0f, SpearSecondaryKind.MightArc, identity, targetIndex, fromMonarch: fromMonarch);

	public override void SendExtraAI(System.IO.BinaryWriter writer)
	{
		writer.Write((byte)_preferredTargetOrdinal);
		writer.Write(_fromMonarch);
	}

	public override void ReceiveExtraAI(System.IO.BinaryReader reader)
	{
		_preferredTargetOrdinal = reader.ReadByte();
		_fromMonarch = reader.ReadBoolean();
	}

	public override void AI()
	{
		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !Main.player[Projectile.owner].active) {
			_suppressVisualsWhileAwaitingRemoval = true;
			Projectile.alpha = 255;
			if (Main.netMode != NetmodeID.MultiplayerClient)
				Projectile.Kill();
			return;
		}
		_suppressVisualsWhileAwaitingRemoval = false;

		if (!_initialized)
			InitializeKind();

		Projectile.localAI[0]++;
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

		switch (SecondaryKind) {
			case SpearSecondaryKind.ShadowThorn:
				ShadowThornAI();
				break;
			case SpearSecondaryKind.MightArc:
				Projectile.velocity = Vector2.Zero;
				break;
			case SpearSecondaryKind.PrimeBlade:
				Projectile.rotation += Projectile.localAI[0] * 0.4f;
				break;
			case SpearSecondaryKind.SeekingPetal:
				SeekingPetalAI();
				break;
			case SpearSecondaryKind.TempleShard:
				Projectile.tileCollide = Projectile.localAI[0] > 12f;
				break;
			case SpearSecondaryKind.OrbitalBolt:
				OrbitalBoltAI();
				break;
		}

		SpearProfile profile = WeaponProfileRegistry.Get(Identity).Spear;
		Color color = GetVisualColor(profile);
		if (SpearVisualEffects.IsPrimaryUpdate(Projectile) && !_suppressVisualsWhileAwaitingRemoval) {
			SpearLightRole lightRole = SecondaryKind switch {
				SpearSecondaryKind.MightArc => SpearLightRole.MightArc,
				SpearSecondaryKind.OrbitalBolt => SpearLightRole.OrbitalBolt,
				SpearSecondaryKind.IdentityCopy => SpearLightRole.IdentityCopy,
				_ => _fromMonarch ? SpearLightRole.MonarchSecondary : SpearLightRole.Secondary
			};
			SpearVisualEffects.AddLight(Projectile.Center, color, profile.LightStrength, lightRole);

			bool allowDust = !_fromMonarch && SecondaryKind is not SpearSecondaryKind.MightArc and not SpearSecondaryKind.IdentityCopy and not SpearSecondaryKind.OrbitalBolt;
			if (!Main.dedServ && allowDust && Main.rand.NextBool(10))
				SpearVisualEffects.SpawnTintedDust(Projectile.Center, -Projectile.velocity * 0.03f, 160, color, 0.55f);
		}
	}

	private void InitializeKind()
	{
		_initialized = true;
		int size;
		int lifetime;
		bool tileCollide;

		switch (SecondaryKind) {
			case SpearSecondaryKind.ShadowThorn:
				size = 12; lifetime = 60; tileCollide = false;
				if (TargetIndex < 0 && HasOwnerAuthority)
					SetTargetIfChanged(SpearTargeting.FindClosestVisibleTarget(Projectile.Center, 240f));
				break;
			case SpearSecondaryKind.BloodNeedle:
				size = 8; lifetime = 45; tileCollide = true;
				break;
			case SpearSecondaryKind.MightArc:
				size = 2; lifetime = 1; tileCollide = false;
				break;
			case SpearSecondaryKind.RetinazerBeam:
				size = 8; lifetime = 30; tileCollide = true;
				break;
			case SpearSecondaryKind.SpazmatismFlare:
				size = 24; lifetime = 45; tileCollide = true;
				break;
			case SpearSecondaryKind.PrimeBlade:
				size = 14; lifetime = 60; tileCollide = true;
				break;
			case SpearSecondaryKind.SeekingPetal:
				size = 10; lifetime = 90; tileCollide = true;
				break;
			case SpearSecondaryKind.TempleShard:
				size = 12; lifetime = 60; tileCollide = false;
				break;
			case SpearSecondaryKind.IdentityCopy:
				size = 12; lifetime = 45; tileCollide = true;
				break;
			default:
				size = 10; lifetime = 60; tileCollide = false;
				break;
		}

		Projectile.Resize(size, size);
		// Preserve the remaining lifetime received over the network. Resetting it
		// here made delayed replicas replay an effect from the beginning.
		Projectile.timeLeft = Math.Min(Projectile.timeLeft, lifetime);
		Projectile.tileCollide = tileCollide;
	}

	private void ShadowThornAI()
	{
		if (!TryGetTarget(out NPC target, requireLineOfSight: false)) {
			if (ShouldSearchForTarget(6))
				SetTargetIfChanged(SpearTargeting.FindClosestVisibleTarget(Projectile.Center, 240f));
			return;
		}

		if (HasOwnerAuthority) {
			SpearTargeting.HomeTowards(Projectile, target.Center, 14f, MathHelper.ToRadians(12f));
			SyncHomingVelocity(6);
		}
	}

	private void SeekingPetalAI()
	{
		if (Projectile.localAI[0] <= 12f)
			return;

		if (!TryGetTarget(out NPC target, requireLineOfSight: false)) {
			if (ShouldSearchForTarget(10))
				SetTargetIfChanged(FindOrdinalVisibleTarget(Projectile.Center, 500f, _preferredTargetOrdinal));
			return;
		}

		if (HasOwnerAuthority) {
			SpearTargeting.HomeTowards(Projectile, target.Center, 8f, MathHelper.ToRadians(10f));
			SyncHomingVelocity(10);
		}
	}

	private void SetTargetIfChanged(int targetIndex)
	{
		if (TargetIndex == targetIndex)
			return;
		TargetIndex = targetIndex;
		Projectile.netUpdate = true;
	}

	private void OrbitalBoltAI()
	{
		if (!TryGetTarget(out NPC target, requireLineOfSight: false))
			return;

		if (HasOwnerAuthority) {
			SpearTargeting.HomeTowards(Projectile, target.Center, Math.Max(Projectile.velocity.Length(), 14f), MathHelper.ToRadians(8f));
			SyncHomingVelocity(6);
		}
	}

	private bool ShouldSearchForTarget(int interval)
	{
		if (!HasOwnerAuthority || Projectile.localAI[0] < _nextTargetSearchUpdate)
			return false;

		_nextTargetSearchUpdate = (int)Projectile.localAI[0] + interval;
		return true;
	}

	private void SyncHomingVelocity(int interval)
	{
		if ((int)Projectile.localAI[0] % interval == 0)
			Projectile.netUpdate = true;
	}

	private bool TryGetTarget(out NPC target, bool requireLineOfSight)
	{
		int index = TargetIndex;
		if (index >= 0 && index < Main.maxNPCs) {
			target = Main.npc[index];
			if (target.active && target.CanBeChasedBy(Projectile) && (!requireLineOfSight || Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height)))
				return true;
		}

		target = default;
		return false;
	}

	private static int FindOrdinalVisibleTarget(Vector2 origin, float range, int ordinal)
	{
		ordinal = Math.Clamp(ordinal, 0, 5);
		int capacity = ordinal + 1;
		Span<float> bestDistances = stackalloc float[6];
		Span<int> bestIndices = stackalloc int[6];
		for (int i = 0; i < capacity; i++) {
			bestDistances[i] = float.MaxValue;
			bestIndices[i] = -1;
		}

		int visibleTargetCount = 0;
		float rangeSquared = range * range;
		for (int i = 0; i < Main.maxNPCs; i++) {
			NPC npc = Main.npc[i];
			if (!npc.active || !npc.CanBeChasedBy())
				continue;

			float distanceSquared = Vector2.DistanceSquared(origin, npc.Center);
			if (distanceSquared > rangeSquared || !Collision.CanHitLine(origin, 1, 1, npc.position, npc.width, npc.height))
				continue;

			visibleTargetCount++;
			for (int insertAt = 0; insertAt < capacity; insertAt++) {
				if (distanceSquared > bestDistances[insertAt] || distanceSquared == bestDistances[insertAt] && i > bestIndices[insertAt])
					continue;

				for (int shift = capacity - 1; shift > insertAt; shift--) {
					bestDistances[shift] = bestDistances[shift - 1];
					bestIndices[shift] = bestIndices[shift - 1];
				}
				bestDistances[insertAt] = distanceSquared;
				bestIndices[insertAt] = i;
				break;
			}
		}

		return visibleTargetCount == 0 ? -1 : bestIndices[ordinal % visibleTargetCount];
	}

	public override bool? CanHitNPC(NPC target)
	{
		if (SecondaryKind == SpearSecondaryKind.MightArc)
			return target.whoAmI == TargetIndex ? null : false;
		return target.active && !target.friendly && !target.dontTakeDamage ? null : false;
	}

	public override bool? CanDamage() => _suppressVisualsWhileAwaitingRemoval ? false : null;

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (SecondaryKind != SpearSecondaryKind.MightArc || !TryGetTarget(out NPC target, requireLineOfSight: false))
			return base.Colliding(projHitbox, targetHitbox);

		float collisionPoint = 0f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, target.Center, 8f, ref collisionPoint);
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.DisableCrit();
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (SecondaryKind == SpearSecondaryKind.ShadowThorn && SpearGlobalNPC.CanCrowdControl(target))
			target.AddBuff(ModContent.BuffType<Stunned>(), 180);
		else if (SecondaryKind == SpearSecondaryKind.SpazmatismFlare)
			target.AddBuff(BuffID.CursedInferno, 240);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (_suppressVisualsWhileAwaitingRemoval)
			return false;

		Color color = GetVisualColor(WeaponProfileRegistry.Get(Identity).Spear);
		if (SecondaryKind == SpearSecondaryKind.IdentityCopy || SecondaryKind == SpearSecondaryKind.OrbitalBolt)
			return DrawIdentityTexture(color, lightColor, SecondaryKind == SpearSecondaryKind.OrbitalBolt ? 0.5f : 0.62f);

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		if (SecondaryKind == SpearSecondaryKind.MightArc && TryGetTarget(out NPC target, requireLineOfSight: false)) {
			Color arcColor = Color.Lerp(lightColor, color, 0.45f) * 0.8f;
			DrawLine(pixel, Projectile.Center, target.Center, arcColor, 4f);
			DrawLine(pixel, Projectile.Center + new Vector2(0f, 3f), target.Center + new Vector2(0f, -3f), lightColor * 0.55f, 1f);
			return false;
		}

		Vector2 scale = SecondaryKind switch {
			SpearSecondaryKind.BloodNeedle or SpearSecondaryKind.RetinazerBeam => new Vector2(10f, 2f),
			SpearSecondaryKind.SpazmatismFlare => new Vector2(18f, 10f),
			SpearSecondaryKind.PrimeBlade => new Vector2(18f, 4f),
			SpearSecondaryKind.SeekingPetal => new Vector2(9f, 5f),
			_ => new Vector2(12f, 5f)
		};
		Color drawColor = Color.Lerp(lightColor, color, 0.45f) * 0.8f;
		Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition, null, drawColor, Projectile.rotation, new Vector2(0.5f), scale, SpriteEffects.None);
		return false;
	}

	private Color GetVisualColor(SpearProfile profile) => SecondaryKind switch {
		SpearSecondaryKind.RetinazerBeam => new Color(238, 65, 65),
		SpearSecondaryKind.SpazmatismFlare => new Color(80, 235, 105),
		_ => profile.Color
	};

	private bool DrawIdentityTexture(Color color, Color lightColor, float scale)
	{
		Texture2D texture = ModContent.Request<Texture2D>(WeaponProfileRegistry.TexturePath(Identity), AssetRequestMode.ImmediateLoad).Value;
		Color drawColor = Color.Lerp(lightColor, color, 0.25f) * 0.85f;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor, Projectile.rotation, texture.Size() * 0.5f, scale, SpriteEffects.None);
		return false;
	}

	private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, float width)
	{
		Vector2 difference = end - start;
		Main.EntitySpriteDraw(texture, start - Main.screenPosition, null, color, difference.ToRotation(), new Vector2(0f, 0.5f), new Vector2(difference.Length(), width), SpriteEffects.None);
	}
}
