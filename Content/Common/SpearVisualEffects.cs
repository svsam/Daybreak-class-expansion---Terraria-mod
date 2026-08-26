using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Common;

internal enum SpearLightRole : byte
{
	MainFlight,
	MonarchMainFlight,
	Embedded,
	MonarchEmbedded,
	HellrendEmbedded,
	Burst,
	MonarchBurst,
	Secondary,
	MonarchSecondary
}

/// <summary>
/// Keeps the many overlapping spear visuals inside a shared light budget. In
/// particular, Monarch detonations can have dozens of live projectiles at once,
/// so profile light values must never be applied to every child at full strength.
/// </summary>
internal static class SpearVisualEffects
{
	// Terraria decrements numUpdates before each AI pass. The final (and only,
	// when extraUpdates is zero) pass for a game tick is therefore -1.
	internal static bool IsPrimaryUpdate(Projectile projectile) => projectile.numUpdates == -1;

	internal static void AddLight(Vector2 position, Color color, float profileStrength, SpearLightRole role)
	{
		if (Main.dedServ)
			return;

		(float multiplier, float maximum) = role switch {
			SpearLightRole.MainFlight => (0.4f, 0.24f),
			SpearLightRole.MonarchMainFlight => (0.25f, 0.14f),
			SpearLightRole.Embedded => (0.22f, 0.14f),
			SpearLightRole.MonarchEmbedded => (0.12f, 0.08f),
			SpearLightRole.HellrendEmbedded => (0.3f, 0.22f),
			SpearLightRole.Burst => (0.25f, 0.16f),
			SpearLightRole.MonarchBurst => (0.12f, 0.08f),
			SpearLightRole.Secondary => (0.25f, 0.14f),
			SpearLightRole.MonarchSecondary => (0.12f, 0.07f),
			_ => (0f, 0f)
		};

		float strength = Math.Min(profileStrength * multiplier, maximum);
		if (strength > 0f)
			Lighting.AddLight(position, color.ToVector3() * strength);
	}

	internal static Dust SpawnTintedDust(Vector2 position, Vector2 velocity, int alpha, Color color, float scale)
	{
		// TintableDustLighted was the source of the long-lived glow after its
		// projectile died. This variant preserves the colored particle without
		// adding another independent light source.
		Dust dust = Dust.NewDustPerfect(position, DustID.TintableDust, velocity, alpha, color, scale);
		dust.noGravity = true;
		dust.noLightEmittence = true;
		return dust;
	}
}
