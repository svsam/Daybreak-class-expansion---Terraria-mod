using Microsoft.Xna.Framework;
using Terraria;

namespace Spears.Content.Common;

internal static class SpearTargeting
{
	public static int FindClosestVisibleTarget(Vector2 origin, float maxDistance, int excludedTarget = -1)
	{
		int bestIndex = -1;
		float bestDistanceSquared = maxDistance * maxDistance;

		for (int i = 0; i < Main.maxNPCs; i++) {
			if (i == excludedTarget)
				continue;

			NPC npc = Main.npc[i];
			if (!npc.active || !npc.CanBeChasedBy() || !Collision.CanHitLine(origin, 1, 1, npc.position, npc.width, npc.height))
				continue;

			float distanceSquared = Vector2.DistanceSquared(origin, npc.Center);
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
}

