using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Spears.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.NPCs;

public sealed class SpearGlobalNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	internal bool Stunned;
	internal bool KingsOfKings;
	private readonly HashSet<int> _embeddedProjectileIndices = new();

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		_embeddedProjectileIndices.Clear();
	}

	internal void RegisterEmbedded(int projectileIndex)
	{
		if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
			_embeddedProjectileIndices.Add(projectileIndex);
	}

	public override void ResetEffects(NPC npc)
	{
		Stunned = false;
		KingsOfKings = false;
	}

	public override bool PreAI(NPC npc)
	{
		if (!Stunned || !CanCrowdControl(npc))
			return true;

		npc.velocity = Vector2.Zero;
		if (Main.netMode != NetmodeID.MultiplayerClient && Main.GameUpdateCount % 10 == 0)
			npc.netUpdate = true;
		return false;
	}

	public override void PostAI(NPC npc)
	{
		if (Stunned || !KingsOfKings || !CanCrowdControl(npc) || Main.netMode == NetmodeID.MultiplayerClient)
			return;

		Player nearest = FindNearestPlayer(npc.Center);
		if (nearest is null)
			return;

		Vector2 away = npc.Center - nearest.Center;
		if (away == Vector2.Zero)
			away = Vector2.UnitX * (npc.whoAmI % 2 == 0 ? 1f : -1f);

		if (npc.noGravity || npc.noTileCollide) {
			Vector2 desiredVelocity = away.SafeNormalize(Vector2.UnitX) * 6f;
			npc.velocity = Vector2.Lerp(npc.velocity, desiredVelocity, 0.12f);
			if (npc.velocity.LengthSquared() > 36f)
				npc.velocity = Vector2.Normalize(npc.velocity) * 6f;
		}
		else {
			float direction = Math.Sign(away.X);
			npc.velocity.X = MathHelper.Clamp(npc.velocity.X + direction * 0.22f, -4f, 4f);
			if (npc.collideX)
				npc.velocity.Y = Math.Min(npc.velocity.Y, -6f);
		}

		if (Main.GameUpdateCount % 10 == 0)
			npc.netUpdate = true;
	}

	public override void UpdateLifeRegen(NPC npc, ref int damage)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		int totalDps = 0;
		int spearType = ModContent.ProjectileType<ProgressionSpearProjectile>();
		List<int> staleIndices = null;
		foreach (int projectileIndex in _embeddedProjectileIndices) {
			Projectile projectile = Main.projectile[projectileIndex];
			if (projectile.active && projectile.type == spearType && projectile.ModProjectile is ProgressionSpearProjectile spear && spear.IsEmbeddedIn(npc.whoAmI)) {
				totalDps += spear.EmbeddedDotDps;
				continue;
			}

			staleIndices ??= new List<int>();
			staleIndices.Add(projectileIndex);
		}

		if (staleIndices is not null)
			foreach (int projectileIndex in staleIndices)
				_embeddedProjectileIndices.Remove(projectileIndex);

		if (totalDps <= 0)
			return;

		if (npc.lifeRegen > 0)
			npc.lifeRegen = 0;
		npc.lifeRegen -= totalDps * 2;
		damage = Math.Max(damage, totalDps);
	}

	internal static bool CanCrowdControl(NPC npc)
	{
		if (!npc.active || npc.friendly || npc.townNPC || npc.isLikeATownNPC || npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
			return false;
		if (npc.knockBackResist <= 0f || npc.realLife >= 0 || npc.aiStyle == NPCAIStyleID.Worm)
			return false;
		return npc.lifeMax > 5 && npc.CanBeChasedBy();
	}

	private static Player FindNearestPlayer(Vector2 origin)
	{
		Player nearest = null;
		float nearestDistance = float.MaxValue;
		for (int i = 0; i < Main.maxPlayers; i++) {
			Player player = Main.player[i];
			if (!player.active || player.dead)
				continue;

			float distance = Vector2.DistanceSquared(origin, player.Center);
			if (distance < nearestDistance) {
				nearestDistance = distance;
				nearest = player;
			}
		}
		return nearest;
	}
}
