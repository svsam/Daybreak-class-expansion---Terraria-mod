using System;
using Microsoft.Xna.Framework;

namespace Spears.Content.Common;

// Landmarks are in the untouched source illustrations' pixel coordinates.
// Their different canvas sizes and shaft angles must not change gameplay size or aim.
internal readonly record struct JavelinArtwork(Vector2 Tail, Vector2 Tip)
{
	internal const float InventoryLength = 64f;
	internal const float DroppedLength = 64f;
	internal const float FlightLength = 128f;
	internal const float MonarchScale = 1.65f;

	internal Vector2 Direction => Vector2.Normalize(Tip - Tail);
	internal float Length => Vector2.Distance(Tail, Tip);
	internal Vector2 Center => (Tail + Tip) * 0.5f;
	internal float RotationOffset => -MathHelper.PiOver4 - MathF.Atan2(Direction.Y, Direction.X);

	// Keep the blade tip inside the existing 16px damage hitbox, with the shaft behind it.
	internal Vector2 FlightOrigin(float scale) => Tip - Direction * (8f / scale);

	internal static JavelinArtwork Get(SpearKind kind) => kind switch {
		SpearKind.Gold => new(new(7f, 507f), new(499f, 20f)),
		SpearKind.Corruption => new(new(92f, 1130f), new(1160f, 85f)),
		SpearKind.Crimson => new(new(304f, 864f), new(1488f, 65f)),
		SpearKind.Hellrend => new(new(194f, 1210f), new(1160f, 24f)),
		SpearKind.Mightpiercer => new(new(195f, 1438f), new(894f, 127f)),
		SpearKind.Gemini => new(new(136f, 1208f), new(1209f, 26f)),
		SpearKind.Frightsteel => new(new(162f, 1462f), new(831f, 79f)),
		SpearKind.FlowerSpike => new(new(234f, 1132f), new(1135f, 127f)),
		SpearKind.Tepoztopilli => new(new(88f, 1259f), new(958f, 203f)),
		SpearKind.Monarch => new(new(76f, 1493f), new(983f, 30f)),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown javelin artwork.")
	};
}
