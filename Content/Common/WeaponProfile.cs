using Microsoft.Xna.Framework;
using Terraria.ID;

namespace Spears.Content.Common;

internal enum SpearImpactDebuff : byte
{
	None,
	GoldCurse,
	ShadowFlame,
	Ichor,
	Hellfire,
	CursedInferno,
	Venom,
	Poisoned,
	OnFire,
	Bleeding
}

internal enum SpearAttackBehavior : byte
{
	Contact,
	LodgeDebuff,
	ExplodeOnContact,
	PierceReturn,
	DebuffOnly,
	LodgeExplode,
	Saw,
	FlowerThorn,
	MonarchFinal
}

internal sealed class SpearAttackProfile
{
	internal SpearAttackProfile(
		SpearAttackKind kind,
		SpearKind textureKind,
		SpearAttackBehavior behavior,
		float initialDamageMultiplier = 1f,
		float returnDamageMultiplier = 0f,
		float pulseDamageMultiplier = 0f,
		float explosionDamageMultiplier = 0f,
		int explosionRadius = 0,
		SpearImpactDebuff debuff = SpearImpactDebuff.None,
		int debuffDurationTicks = 0,
		int lodgedDurationTicks = 0,
		int lingeringDebuffTicks = 0,
		float homingRange = 0f,
		float homingTurnDegrees = 0f,
		bool canCrit = true,
		bool tileExplodes = false)
	{
		Kind = kind;
		TextureKind = textureKind;
		Behavior = behavior;
		InitialDamageMultiplier = initialDamageMultiplier;
		ReturnDamageMultiplier = returnDamageMultiplier;
		PulseDamageMultiplier = pulseDamageMultiplier;
		ExplosionDamageMultiplier = explosionDamageMultiplier;
		ExplosionRadius = explosionRadius;
		Debuff = debuff;
		DebuffDurationTicks = debuffDurationTicks;
		LodgedDurationTicks = lodgedDurationTicks;
		LingeringDebuffTicks = lingeringDebuffTicks;
		HomingRange = homingRange;
		HomingTurnDegrees = homingTurnDegrees;
		CanCrit = canCrit;
		TileExplodes = tileExplodes;
	}

	internal SpearAttackKind Kind { get; }
	internal SpearKind TextureKind { get; }
	internal SpearAttackBehavior Behavior { get; }
	internal float InitialDamageMultiplier { get; }
	internal float ReturnDamageMultiplier { get; }
	internal float PulseDamageMultiplier { get; }
	internal float ExplosionDamageMultiplier { get; }
	internal int ExplosionRadius { get; }
	internal SpearImpactDebuff Debuff { get; }
	internal int DebuffDurationTicks { get; }
	internal int LodgedDurationTicks { get; }
	internal int LingeringDebuffTicks { get; }
	internal float HomingRange { get; }
	internal float HomingTurnDegrees { get; }
	internal bool CanCrit { get; }
	internal bool TileExplodes { get; }
	internal bool HasHoming => HomingRange > 0f;
	internal bool IsDebuffOnly => Behavior is SpearAttackBehavior.DebuffOnly or SpearAttackBehavior.FlowerThorn;

	internal int DebuffType => Debuff switch {
		SpearImpactDebuff.ShadowFlame => BuffID.ShadowFlame,
		SpearImpactDebuff.Ichor => BuffID.Ichor,
		SpearImpactDebuff.Hellfire => BuffID.OnFire3,
		SpearImpactDebuff.CursedInferno => BuffID.CursedInferno,
		SpearImpactDebuff.Venom => BuffID.Venom,
		SpearImpactDebuff.Poisoned => BuffID.Poisoned,
		SpearImpactDebuff.OnFire => BuffID.OnFire,
		SpearImpactDebuff.Bleeding => BuffID.Bleeding,
		_ => 0
	};
}

internal sealed class WeaponProfile
{
	public WeaponProfile(
		SpearKind kind,
		string texturePath,
		int damage,
		int useTime,
		float knockback,
		float shootSpeed,
		int rarity,
		int value,
		Color color,
		float lightStrength = 0.35f,
		int armorPenetration = 0,
		int extraCrit = 0)
	{
		Kind = kind;
		TexturePath = texturePath;
		Damage = damage;
		UseTime = useTime;
		Knockback = knockback;
		ShootSpeed = shootSpeed;
		Rarity = rarity;
		Value = value;
		Color = color;
		LightStrength = lightStrength;
		ArmorPenetration = armorPenetration;
		ExtraCrit = extraCrit;
	}

	public SpearKind Kind { get; }
	public string TexturePath { get; }
	public int Damage { get; }
	public int UseTime { get; }
	public float Knockback { get; }
	public float ShootSpeed { get; }
	public int Rarity { get; }
	public int Value { get; }
	public Color Color { get; }
	public float LightStrength { get; }
	public int ArmorPenetration { get; }
	public int ExtraCrit { get; }
}
