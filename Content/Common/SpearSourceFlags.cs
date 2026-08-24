using System;

namespace Spears.Content.Common;

[Flags]
internal enum SpearSourceFlags : byte
{
	None = 0,
	Main = 1 << 0,
	Auxiliary = 1 << 1,
	Orbital = 1 << 2,
	Copy = 1 << 3,
	Monarch = 1 << 4
}

internal enum SpearSecondaryKind : byte
{
	ShadowThorn,
	BloodNeedle,
	MightArc,
	RetinazerBeam,
	SpazmatismFlare,
	PrimeBlade,
	SeekingPetal,
	TempleShard,
	IdentityCopy,
	OrbitalBolt
}
