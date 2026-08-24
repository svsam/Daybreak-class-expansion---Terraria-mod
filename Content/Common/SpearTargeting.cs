using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace Spears.Content.Common;

internal static class SpearTargeting
{
	public static int FindClosestVisibleTargets(Vector2 origin, float maxDistance, Span<int> targetIndices)
	{
		targetIndices.Fill(-1);
		int targetCount = 0;

		for (int resultIndex = 0; resultIndex < targetIndices.Length; resultIndex++) {
			int bestIndex = -1;
			float bestDistanceSquared = maxDistance * maxDistance;

			for (int npcIndex = 0; npcIndex < Main.maxNPCs; npcIndex++) {
				NPC npc = Main.npc[npcIndex];
				if (!npc.active || !npc.CanBeChasedBy() || Contains(targetIndices, resultIndex, npcIndex))
					continue;

				float distanceSquared = Vector2.DistanceSquared(origin, npc.Center);
				if (distanceSquared > bestDistanceSquared || !Collision.CanHitLine(origin, 1, 1, npc.position, npc.width, npc.height))
					continue;

				if (distanceSquared < bestDistanceSquared || distanceSquared == bestDistanceSquared && (bestIndex < 0 || npcIndex < bestIndex)) {
					bestDistanceSquared = distanceSquared;
					bestIndex = npcIndex;
				}
			}

			if (bestIndex < 0)
				break;

			targetIndices[resultIndex] = bestIndex;
			targetCount++;
		}

		return targetCount;
	}

	public static int FindClosestVisibleTarget(Vector2 origin, float maxDistance, int excludedTarget = -1)
	{
		int bestIndex = -1;
		float bestDistanceSquared = maxDistance * maxDistance;

		for (int i = 0; i < Main.maxNPCs; i++) {
			if (i == excludedTarget)
				continue;

			NPC npc = Main.npc[i];
			if (!npc.active || !npc.CanBeChasedBy())
				continue;

			float distanceSquared = Vector2.DistanceSquared(origin, npc.Center);
			if (distanceSquared > bestDistanceSquared || !Collision.CanHitLine(origin, 1, 1, npc.position, npc.width, npc.height))
				continue;

			if (distanceSquared < bestDistanceSquared || distanceSquared == bestDistanceSquared && (bestIndex < 0 || i < bestIndex)) {
				bestDistanceSquared = distanceSquared;
				bestIndex = i;
			}
}
		return bestIndex;
	}

	public static void HomeTowards(Projectile projectile, Vector2 destination, float speed, float maximumTurnRadians)
	{
		Vector2 desired = projectile.DirectionTo(destination) * speed;
		float currentAngle = projectile.velocity.ToRotation();
		float desiredAngle = desired.ToRotation();
		float nextAngle = currentAngle.AngleTowards(desiredAngle, maximumTurnRadians);
		projectile.velocity = nextAngle.ToRotationVector2() * speed;
	}

	private static bool Contains(ReadOnlySpan<int> values, int count, int value)
	{
		for (int index = 0; index < count; index++) {
			if (values[index] == value)
				return true;
		}
		return false;
	}
}
