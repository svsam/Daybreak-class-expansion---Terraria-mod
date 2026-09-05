using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
	private const int FlightTimeoutTicks = 600;
	private const int EmbeddedCapPerOwner = 8;
	private const float MaximumTrailGap = 160f;
	private const float PierceOvershootDistance = 96f;

	private SpearSourceFlags _sourceFlags;
	private Vector2 _embedOffset;
	private Vector2 _impactDirection = Vector2.UnitX;
	private Vector2 _stateOrigin;
	private int _launchDamage;
	private int _stateTimer;
	private int _lockedTargetType = -1;
	private int _localTargetIndex = -1;
	private int _localTargetSpawnSerial;
	private int _nextTargetSearchTick;
	private bool _terminal;
	private bool _pulseDamageActive;
	private bool _suppressVisuals;
	private bool _awaitingOwnerRemoval;

	private SpearAttackKind AttackKind => (SpearAttackKind)(int)Projectile.ai[0];
	private SpearAttackProfile Profile => WeaponProfileRegistry.GetAttack(AttackKind);
	private SpearProjectileState State {
		get => (SpearProjectileState)(int)Projectile.ai[1];
		set => Projectile.ai[1] = (float)value;
	}
	private int TargetIndex {
		get => (int)Projectile.ai[2] - 1;
		set => Projectile.ai[2] = value + 1;
	}
	private bool HasOwnerAuthority => Main.netMode == NetmodeID.SinglePlayer || Projectile.owner == Main.myPlayer;
	private bool IsMonarchVolley => (_sourceFlags & SpearSourceFlags.MonarchVolley) != 0;

	public override string Texture => "Terraria/Images/Projectile_0";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 8;
		ProjectileID.Sets.TrailingMode[Type] = 2;
		// Include the entire enlarged Monarch sprite when its tip leaves the screen.
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 256;
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
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 10;
	}

	public override void OnSpawn(IEntitySource source)
	{
		_launchDamage = Math.Max(1, Projectile.damage);
		_impactDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		Projectile.friendly = !Profile.IsDebuffOnly;
		Projectile.tileCollide = Profile.Behavior != SpearAttackBehavior.MonarchFinal;
		if (Profile.Behavior is SpearAttackBehavior.Contact or SpearAttackBehavior.ExplodeOnContact or SpearAttackBehavior.MonarchFinal)
			Projectile.penetrate = 1;
		if (!Profile.CanCrit)
			Projectile.CritChance = 0;
		ResetTrailHistory();
	}

	internal static int Spawn(
		IEntitySource source,
		Vector2 position,
		Vector2 velocity,
		int damage,
		float knockback,
		int owner,
		SpearAttackKind attackKind,
		SpearSourceFlags sourceFlags = SpearSourceFlags.None,
		int initialTarget = -1)
	{
		int index = Projectile.NewProjectile(
			source,
			position,
			velocity,
			ModContent.ProjectileType<ProgressionSpearProjectile>(),
			Math.Max(1, damage),
			knockback,
			owner,
			(float)attackKind,
			(float)SpearProjectileState.Flying,
			initialTarget + 1);

		if (index < 0 || index >= Main.maxProjectiles || Main.projectile[index].ModProjectile is not ProgressionSpearProjectile spear)
			return index;

		spear._sourceFlags = sourceFlags;
		spear._launchDamage = Math.Max(1, damage);
		spear._impactDirection = velocity.SafeNormalize(Vector2.UnitX);
		SpearAttackProfile profile = WeaponProfileRegistry.GetAttack(attackKind);
		if (sourceFlags.HasFlag(SpearSourceFlags.MonarchVolley) || !profile.CanCrit)
			Main.projectile[index].CritChance = 0;

		float acquisitionRange = sourceFlags.HasFlag(SpearSourceFlags.MonarchVolley) ? 1000f : profile.HomingRange;
		if (initialTarget < 0 && acquisitionRange > 0f && (Main.netMode == NetmodeID.SinglePlayer || owner == Main.myPlayer))
			spear.TargetIndex = SpearTargeting.FindClosestVisibleTarget(position, acquisitionRange);

		Main.projectile[index].friendly = !profile.IsDebuffOnly;
		Main.projectile[index].netUpdate = true;
		return index;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write((byte)_sourceFlags);
		writer.Write(_embedOffset.X);
		writer.Write(_embedOffset.Y);
		writer.Write(_impactDirection.X);
		writer.Write(_impactDirection.Y);
		writer.Write(_stateOrigin.X);
		writer.Write(_stateOrigin.Y);
		writer.Write(_launchDamage);
		writer.Write(_stateTimer);
		writer.Write(_lockedTargetType);
		writer.Write(_terminal);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		_sourceFlags = (SpearSourceFlags)reader.ReadByte();
		_embedOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
		_impactDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle());
		_stateOrigin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
		_launchDamage = reader.ReadInt32();
		_stateTimer = reader.ReadInt32();
		_lockedTargetType = reader.ReadInt32();
		_terminal = reader.ReadBoolean();
		_suppressVisuals = _terminal;
	}

	public override void AI()
	{
		if (_terminal) {
			SuppressAndRemoveOnServer();
			return;
		}

		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers || !Main.player[Projectile.owner].active) {
			SuppressAndRemoveOnServer();
			return;
		}
		if (_awaitingOwnerRemoval) {
			_suppressVisuals = true;
			return;
		}

		_suppressVisuals = false;
		if (SpearVisualEffects.IsPrimaryUpdate(Projectile))
			_stateTimer++;

		switch (State) {
			case SpearProjectileState.Flying:
				FlyingAI();
				break;
			case SpearProjectileState.Lodged:
				LodgedAI();
				break;
			case SpearProjectileState.Penetrating:
				PenetratingAI();
				break;
			case SpearProjectileState.Overshooting:
				OvershootingAI();
				break;
			case SpearProjectileState.Returning:
				ReturningAI();
				break;
			case SpearProjectileState.Sawing:
				SawingAI();
				break;
			default:
				SuppressAndRemoveOnServer();
				break;
		}
	}

	private void FlyingAI()
	{
		Projectile.alpha = Math.Max(0, Projectile.alpha - 25);
		_impactDirection = Projectile.velocity.SafeNormalize(_impactDirection);
		Projectile.rotation = _impactDirection.ToRotation() + MathHelper.PiOver4;

		if (Profile.IsDebuffOnly)
			CheckDebuffOnlyCollision();

		if (SpearVisualEffects.IsPrimaryUpdate(Projectile)) {
			if (!_terminal)
				UpdateFlightMovement();

			if (_stateTimer >= FlightTimeoutTicks && HasOwnerAuthority)
				Terminate();
		}

		EmitFlightVisuals();
	}

	private void UpdateFlightMovement()
	{
		float homingRange = IsMonarchVolley ? 1000f : Profile.HomingRange;
		if (homingRange > 0f) {
			UpdateHoming(homingRange, IsMonarchVolley ? 10f : Profile.HomingTurnDegrees);
			return;
		}

		if (_stateTimer > 23) {
			Projectile.velocity.X *= 0.995f;
			Projectile.velocity.Y += 0.15f;
		}
	}

	private void UpdateHoming(float range, float turnDegrees)
	{
		if (!HasOwnerAuthority)
			return;

		if (!IsValidTarget(TargetIndex) && _stateTimer >= _nextTargetSearchTick) {
			_nextTargetSearchTick = _stateTimer + 10;
			SetTarget(SpearTargeting.FindClosestVisibleTarget(Projectile.Center, range));
		}

		if (!IsValidTarget(TargetIndex))
			return;

		float rawSpeed = AttackKind == SpearAttackKind.MonarchFinal || IsMonarchVolley
			? 8f
			: WeaponProfileRegistry.Get(Profile.TextureKind).ShootSpeed;
		SpearTargeting.HomeTowards(Projectile, Main.npc[TargetIndex].Center, rawSpeed, MathHelper.ToRadians(turnDegrees));
		_impactDirection = Projectile.velocity.SafeNormalize(_impactDirection);
		if (_stateTimer % 10 == 0)
			Projectile.netUpdate = true;
	}

	private void CheckDebuffOnlyCollision()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		int targetIndex = FindCollidingTarget();
		if (targetIndex < 0)
			return;

		NPC target = Main.npc[targetIndex];
		if (Profile.Behavior == SpearAttackBehavior.FlowerThorn) {
			target.AddBuff(BuffID.Poisoned, 480);
			target.GetGlobalNPC<SpearGlobalNPC>().TryApplyThorned(target);
		}
		else if (Profile.DebuffType > 0) {
			target.AddBuff(Profile.DebuffType, Profile.DebuffDurationTicks);
		}

		Terminate();
	}

	private int FindCollidingTarget()
	{
		if (IsValidTarget(TargetIndex) && Projectile.Hitbox.Intersects(Main.npc[TargetIndex].Hitbox))
			return TargetIndex;

		for (int i = 0; i < Main.maxNPCs; i++) {
			NPC npc = Main.npc[i];
			if (npc.active && !npc.friendly && !npc.dontTakeDamage && Projectile.Hitbox.Intersects(npc.Hitbox))
				return i;
		}
		return -1;
	}

	private void LodgedAI()
	{
		if (!TryGetLockedTarget(out NPC target)) {
			if (HasOwnerAuthority) {
				if (Profile.Behavior == SpearAttackBehavior.LodgeExplode)
					Detonate();
				else
					Terminate();
			}
			else {
				_suppressVisuals = true;
			}
			return;
		}

		AttachToTarget(target);
		if (Profile.Behavior == SpearAttackBehavior.LodgeDebuff && Profile.DebuffType > 0 && SpearVisualEffects.IsPrimaryUpdate(Projectile))
			target.AddBuff(Profile.DebuffType, Profile.LingeringDebuffTicks);

		if (_stateTimer >= Profile.LodgedDurationTicks && HasOwnerAuthority) {
			if (Profile.Behavior == SpearAttackBehavior.LodgeExplode)
				Detonate();
			else
				Terminate();
			return;
		}

		EmitAttachedVisuals();
	}

	private void SawingAI()
	{
		if (!TryGetLockedTarget(out NPC target)) {
			if (HasOwnerAuthority)
				Terminate();
			else
				_suppressVisuals = true;
			return;
		}

		AttachToTarget(target);
		Projectile.rotation += 0.45f;
		if (SpearVisualEffects.IsPrimaryUpdate(Projectile) && _stateTimer > 0 && _stateTimer % 30 == 0 && _stateTimer <= 240 && HasOwnerAuthority)
			DealSawPulse();

		if (_stateTimer >= Profile.LodgedDurationTicks && HasOwnerAuthority) {
			Terminate();
			return;
		}

		EmitAttachedVisuals();
	}

	private void DealSawPulse()
	{
		Projectile.damage = _launchDamage;
		Projectile.friendly = true;
		_pulseDamageActive = true;
		Projectile.Damage();
		_pulseDamageActive = false;
		Projectile.friendly = false;
		Projectile.damage = 0;
	}

	private void PenetratingAI()
	{
		if (!TryGetLockedTarget(out NPC target)) {
			if (HasOwnerAuthority)
				Terminate();
			return;
		}

		float rawSpeed = WeaponProfileRegistry.Get(Profile.TextureKind).ShootSpeed;
		Projectile.velocity = _impactDirection * rawSpeed;
		Projectile.rotation = _impactDirection.ToRotation() + MathHelper.PiOver4;
		float projectedHalfExtent = MathF.Abs(_impactDirection.X) * target.width * 0.5f + MathF.Abs(_impactDirection.Y) * target.height * 0.5f;
		float distancePastCenter = Vector2.Dot(Projectile.Center - target.Center, _impactDirection);
		if (distancePastCenter >= projectedHalfExtent + 8f || _stateTimer >= 60)
			TransitionTo(SpearProjectileState.Overshooting, Projectile.Center);
	}

	private void OvershootingAI()
	{
		if (!TryGetLockedTarget(out NPC target)) {
			if (HasOwnerAuthority)
				Terminate();
			return;
		}

		Projectile.velocity = _impactDirection * WeaponProfileRegistry.Get(Profile.TextureKind).ShootSpeed;
		if (Vector2.DistanceSquared(Projectile.Center, _stateOrigin) < PierceOvershootDistance * PierceOvershootDistance && _stateTimer < 30)
			return;

		TransitionTo(SpearProjectileState.Returning, Projectile.Center);
		float returnSpeed = WeaponProfileRegistry.Get(Profile.TextureKind).ShootSpeed * 1.15f;
		Projectile.velocity = Projectile.DirectionTo(target.Center) * returnSpeed;
	}

	private void ReturningAI()
	{
		if (!TryGetLockedTarget(out NPC target)) {
			if (HasOwnerAuthority)
				Terminate();
			return;
		}

		if (SpearVisualEffects.IsPrimaryUpdate(Projectile) && HasOwnerAuthority) {
			float speed = WeaponProfileRegistry.Get(Profile.TextureKind).ShootSpeed * 1.15f;
			SpearTargeting.HomeTowards(Projectile, target.Center, speed, MathHelper.ToRadians(12f));
			if (_stateTimer % 8 == 0)
				Projectile.netUpdate = true;
		}
		_impactDirection = Projectile.velocity.SafeNormalize(_impactDirection);
		Projectile.rotation = _impactDirection.ToRotation() + MathHelper.PiOver4;
		if (_stateTimer >= 180 && HasOwnerAuthority)
			Terminate();

		EmitFlightVisuals();
	}

	private void AttachToTarget(NPC target)
	{
		Projectile.Center = target.Center + _embedOffset;
		Projectile.gfxOffY = target.gfxOffY;
		Projectile.velocity = Vector2.Zero;
		Projectile.damage = 0;
		Projectile.tileCollide = false;
		Projectile.alpha = 0;
	}

	public override bool? CanDamage()
	{
		if (_terminal || _suppressVisuals || Profile.IsDebuffOnly)
			return false;
		if (_pulseDamageActive)
			return true;
		return State is SpearProjectileState.Flying or SpearProjectileState.Returning ? null : false;
	}

	public override bool? CanHitNPC(NPC target)
	{
		if (Profile.IsDebuffOnly || _terminal || _suppressVisuals)
			return false;
		if (!target.active || target.friendly || target.dontTakeDamage)
			return false;

		if (_pulseDamageActive || State == SpearProjectileState.Returning)
			return target.whoAmI == TargetIndex ? null : false;

		if (State != SpearProjectileState.Flying)
			return false;

		bool targetedAttack = IsMonarchVolley || Profile.HasHoming;
		return targetedAttack && IsValidTarget(TargetIndex) && target.whoAmI != TargetIndex ? false : null;
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (!Profile.CanCrit || IsMonarchVolley || _pulseDamageActive)
			modifiers.DisableCrit();

		float multiplier = _pulseDamageActive
			? Profile.PulseDamageMultiplier
			: State == SpearProjectileState.Returning
				? Profile.ReturnDamageMultiplier
				: Profile.InitialDamageMultiplier;
		modifiers.SourceDamage *= multiplier;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (_pulseDamageActive || _terminal)
			return;

		if (State == SpearProjectileState.Returning) {
			if (HasOwnerAuthority)
				Terminate();
			return;
		}

		if (State != SpearProjectileState.Flying)
			return;

		switch (Profile.Behavior) {
			case SpearAttackBehavior.Contact:
				ApplyImpactDebuff(target);
				if (HasOwnerAuthority)
					Terminate();
				break;
			case SpearAttackBehavior.MonarchFinal:
				if (HasOwnerAuthority)
					Terminate();
				break;
			case SpearAttackBehavior.ExplodeOnContact:
				ApplyImpactDebuff(target);
				if (HasOwnerAuthority)
					Detonate();
				break;
			case SpearAttackBehavior.LodgeDebuff:
				BeginAttached(target, SpearProjectileState.Lodged);
				break;
			case SpearAttackBehavior.LodgeExplode:
				BeginAttached(target, SpearProjectileState.Lodged);
				break;
			case SpearAttackBehavior.PierceReturn:
				BeginPiercing(target);
				break;
			case SpearAttackBehavior.Saw:
				ApplyImpactDebuff(target);
				BeginAttached(target, SpearProjectileState.Sawing);
				break;
		}
	}

	private void ApplyImpactDebuff(NPC target)
	{
		if (Profile.Debuff == SpearImpactDebuff.GoldCurse) {
			target.GetGlobalNPC<SpearGlobalNPC>().TryApplyGoldCurse(target);
			return;
		}

		if (Profile.DebuffType > 0 && Profile.DebuffDurationTicks > 0)
			target.AddBuff(Profile.DebuffType, Profile.DebuffDurationTicks);
	}

	private void BeginAttached(NPC target, SpearProjectileState attachedState)
	{
		_embedOffset = Projectile.Center - target.Center;
		_impactDirection = Projectile.velocity.SafeNormalize(_impactDirection);
		SetLockedTarget(target);
		TransitionTo(attachedState, Projectile.Center);
		Projectile.velocity = Vector2.Zero;
		Projectile.damage = 0;
		Projectile.friendly = attachedState == SpearProjectileState.Sawing;
		Projectile.tileCollide = false;
		Projectile.netUpdate = true;
		if (HasOwnerAuthority)
			EnforceEmbeddedCap(target.whoAmI);
	}

	private void BeginPiercing(NPC target)
	{
		_impactDirection = Projectile.velocity.SafeNormalize(_impactDirection);
		SetLockedTarget(target);
		TransitionTo(SpearProjectileState.Penetrating, Projectile.Center);
		Projectile.tileCollide = false;
		Projectile.netUpdate = true;
	}

	private void SetLockedTarget(NPC target)
	{
		TargetIndex = target.whoAmI;
		_lockedTargetType = target.type;
		_localTargetIndex = target.whoAmI;
		_localTargetSpawnSerial = target.GetGlobalNPC<SpearGlobalNPC>().SpawnSerial;
	}

	private void EnforceEmbeddedCap(int targetIndex)
	{
		List<ProgressionSpearProjectile> attached = new();
		for (int i = 0; i < Main.maxProjectiles; i++) {
			Projectile candidate = Main.projectile[i];
			if (!candidate.active || candidate.owner != Projectile.owner || candidate.type != Type)
				continue;
			if (candidate.ModProjectile is ProgressionSpearProjectile spear && spear.IsAttachedTo(targetIndex))
				attached.Add(spear);
		}

		while (attached.Count > EmbeddedCapPerOwner) {
			ProgressionSpearProjectile oldest = attached[0];
			for (int i = 1; i < attached.Count; i++) {
				ProgressionSpearProjectile candidate = attached[i];
				if (candidate._stateTimer > oldest._stateTimer || candidate._stateTimer == oldest._stateTimer && candidate.Projectile.identity < oldest.Projectile.identity)
					oldest = candidate;
			}
			attached.Remove(oldest);
			oldest.EndFromCap();
		}
	}

	private bool IsAttachedTo(int targetIndex) =>
		!_terminal
		&& TargetIndex == targetIndex
		&& (State is SpearProjectileState.Lodged or SpearProjectileState.Sawing)
		&& TryGetLockedTarget(out _);

	private void EndFromCap()
	{
		if (Profile.Behavior == SpearAttackBehavior.LodgeExplode)
			Detonate();
		else
			Terminate();
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (HasOwnerAuthority) {
			if (Profile.TileExplodes)
				Detonate();
			else
				Terminate();
		}
		else {
			_suppressVisuals = true;
			_awaitingOwnerRemoval = true;
		}
		return false;
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		width = 10;
		height = 10;
		return true;
	}

	private void Detonate()
	{
		if (_terminal)
			return;
		// Gold is contact-only. Keep shared terminal paths from ever producing a burst for it.
		if (AttackKind == SpearAttackKind.Gold) {
			Terminate();
			return;
		}

		if (HasOwnerAuthority && Profile.ExplosionDamageMultiplier > 0f && Profile.ExplosionRadius > 0) {
			int burstDamage = Math.Max(1, (int)MathF.Round(_launchDamage * Profile.ExplosionDamageMultiplier));
			if (AttackKind is SpearAttackKind.Hellrend or SpearAttackKind.Tepoztopilli) {
				int explosionIndex = Projectile.NewProjectile(
					Projectile.GetSource_FromThis(),
					Projectile.Center,
					Vector2.Zero,
					ProjectileID.DaybreakExplosion,
					burstDamage,
					Projectile.knockBack,
					Projectile.owner);
				if (explosionIndex >= 0 && explosionIndex < Main.maxProjectiles)
					Main.projectile[explosionIndex].CritChance = 0;
			}
			else {
				SpearBurstProjectile.Spawn(
					Projectile.GetSource_FromThis(),
					Projectile.Center,
					Projectile.owner,
					burstDamage,
					Projectile.knockBack,
					Profile.TextureKind,
					Profile.ExplosionRadius,
					IsMonarchVolley);
			}
		}
		Terminate();
	}

	private void Terminate()
	{
		if (_terminal)
			return;
		_terminal = true;
		State = SpearProjectileState.Terminal;
		_suppressVisuals = true;
		Projectile.alpha = 255;
		Projectile.netUpdate = true;
		Projectile.Kill();
	}

	private void SuppressAndRemoveOnServer()
	{
		_suppressVisuals = true;
		Projectile.alpha = 255;
		if (Main.netMode != NetmodeID.MultiplayerClient)
			Projectile.Kill();
	}

	private void TransitionTo(SpearProjectileState state, Vector2 stateOrigin)
	{
		State = state;
		_stateTimer = 0;
		_stateOrigin = stateOrigin;
		ResetTrailHistory();
		Projectile.netUpdate = true;
	}

	private void SetTarget(int targetIndex)
	{
		if (TargetIndex == targetIndex)
			return;
		TargetIndex = targetIndex;
		Projectile.netUpdate = true;
	}

	private static bool IsValidTarget(int index) => index >= 0 && index < Main.maxNPCs && Main.npc[index].active && Main.npc[index].CanBeChasedBy();

	private bool TryGetLockedTarget(out NPC target)
	{
		if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs) {
			target = default;
			return false;
		}

		target = Main.npc[TargetIndex];
		if (!target.active || target.life <= 0)
			return false;
		if (_lockedTargetType >= 0 && target.type != _lockedTargetType)
			return false;

		SpearGlobalNPC targetState = target.GetGlobalNPC<SpearGlobalNPC>();
		if (_localTargetIndex != target.whoAmI) {
			_localTargetIndex = target.whoAmI;
			_localTargetSpawnSerial = targetState.SpawnSerial;
		}
		return _localTargetSpawnSerial == targetState.SpawnSerial;
	}

	private void ResetTrailHistory()
	{
		if (Projectile.oldPos is null)
			return;
		for (int i = 0; i < Projectile.oldPos.Length; i++) {
			Projectile.oldPos[i] = Projectile.position;
			Projectile.oldRot[i] = Projectile.rotation;
		}
	}

	private void EmitFlightVisuals()
	{
		if (!SpearVisualEffects.IsPrimaryUpdate(Projectile) || _suppressVisuals)
			return;
		WeaponProfile weapon = WeaponProfileRegistry.Get(Profile.TextureKind);
		SpearVisualEffects.AddLight(Projectile.Center, GetVisualColor(), weapon.LightStrength, IsMonarchVolley || AttackKind == SpearAttackKind.MonarchFinal ? SpearLightRole.MonarchMainFlight : SpearLightRole.MainFlight);
		if (!Main.dedServ && Main.rand.NextBool(IsMonarchVolley ? 12 : 8))
			SpearVisualEffects.SpawnTintedDust(Projectile.Center, -Projectile.velocity * 0.04f, 140, GetVisualColor(), 0.65f);
	}

	private void EmitAttachedVisuals()
	{
		if (!SpearVisualEffects.IsPrimaryUpdate(Projectile) || _suppressVisuals)
			return;
		WeaponProfile weapon = WeaponProfileRegistry.Get(Profile.TextureKind);
		SpearVisualEffects.AddLight(Projectile.Center, GetVisualColor(), weapon.LightStrength, IsMonarchVolley ? SpearLightRole.MonarchEmbedded : SpearLightRole.Embedded);
		if (!Main.dedServ && Main.rand.NextBool(18))
			SpearVisualEffects.SpawnTintedDust(Projectile.Center, Vector2.Zero, 160, GetVisualColor(), 0.55f);
	}

	private Color GetVisualColor() => AttackKind switch {
		SpearAttackKind.GeminiInferno or SpearAttackKind.FrightInferno => new Color(80, 235, 105),
		SpearAttackKind.GeminiDamage => new Color(238, 65, 65),
		SpearAttackKind.FlowerThorn => new Color(106, 132, 62),
		_ => WeaponProfileRegistry.Get(Profile.TextureKind).Color
	};

	public override bool PreDraw(ref Color lightColor)
	{
		if (_terminal || _suppressVisuals)
			return false;

		Texture2D texture = ModContent.Request<Texture2D>(WeaponProfileRegistry.TexturePath(Profile.TextureKind), AssetRequestMode.ImmediateLoad).Value;
		JavelinArtwork artwork = JavelinArtwork.Get(Profile.TextureKind);
		float scale = JavelinArtwork.FlightLength / artwork.Length;
		if (AttackKind == SpearAttackKind.MonarchFinal)
			scale *= JavelinArtwork.MonarchScale;
		Vector2 origin = artwork.FlightOrigin(scale);
		Vector2 previousWorld = Projectile.Center;

		if (State is SpearProjectileState.Flying or SpearProjectileState.Returning) {
			for (int i = 0; i < Projectile.oldPos.Length; i++) {
				Vector2 stored = Projectile.oldPos[i];
				if (stored == Vector2.Zero || !float.IsFinite(stored.X) || !float.IsFinite(stored.Y))
					continue;
				Vector2 world = stored + Projectile.Size * 0.5f;
				if (Vector2.DistanceSquared(previousWorld, world) > MaximumTrailGap * MaximumTrailGap)
					continue;

				float fade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length * 0.12f;
				Color trailColor = AttackKind == SpearAttackKind.MonarchFinal
					? Main.hslToRgb(((float)Main.GameUpdateCount * 0.01f + i / (float)Projectile.oldPos.Length) % 1f, 0.85f, 0.62f) * fade
					: Color.Lerp(lightColor, GetVisualColor(), 0.45f) * fade;
				Main.EntitySpriteDraw(texture, world - Main.screenPosition, null, trailColor, Projectile.oldRot[i] + artwork.RotationOffset, origin, scale, SpriteEffects.None);
				previousWorld = world;
			}
		}

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation + artwork.RotationOffset, origin, scale, SpriteEffects.None);
		return false;
	}
}
