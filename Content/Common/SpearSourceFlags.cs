using System;

namespace Spears.Content.Common;

[Flags]
internal enum SpearSourceFlags : byte
{
	None = 0,
	MonarchVolley = 1 << 0
}

internal enum SpearAttackKind : byte
{
	Gold,
	Corruption,
	Crimson,
	Hellrend,
	Mightpiercer,
	GeminiInferno,
	GeminiDamage,
	FrightDestroyer,
	FrightInferno,
	PrimeSaw,
	FlowerPrimary,
	FlowerThorn,
	Tepoztopilli,
	MonarchFinal
}

internal enum SpearProjectileState : byte
{
	Flying,
	Lodged,
	Penetrating,
	Overshooting,
	Returning,
	Sawing,
	Terminal
}
