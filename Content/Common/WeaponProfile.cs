using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Common;

internal enum SpearImpactDebuff : byte
{
	None,
	Midas,
	Hellfire,
	CursedInferno,
	Venom,
	OnFire
}

internal enum SpearSecondaryEffect : byte
{
	None,
	ShadowThorn,
	BloodNeedles,
	MightArcs,
	Gemini,
	PrimeBlades,
	SeekingPetals,
	TempleShards
}

internal sealed class SpearProfile
{
	internal SpearProfile(
		SpearKind kind,
		string texturePath,
		int dotDps,
		float burstDamageMultiplier,
		int burstRadius,
		float burstKnockback,
		Color color,
		float lightStrength,
		bool directHitCanCrit,
		SpearImpactDebuff impactDebuff,
		int debuffChancePercent,
		int debuffDurationTicks,
		SpearSecondaryEffect secondaryEffect)
	{
		Kind = kind;
		TexturePath = texturePath;
		DotDps = dotDps;
		BurstDamageMultiplier = burstDamageMultiplier;
		BurstRadius = burstRadius;
		BurstKnockback = burstKnockback;
		Color = color;
		LightStrength = lightStrength;
		DirectHitCanCrit = directHitCanCrit;
		ImpactDebuff = impactDebuff;
		DebuffChancePercent = debuffChancePercent;
		DebuffDurationTicks = debuffDurationTicks;
		SecondaryEffect = secondaryEffect;
	}

	internal SpearKind Kind { get; }
	internal string TexturePath { get; }
	internal int DotDps { get; }
	internal float BurstDamageMultiplier { get; }
	internal int BurstRadius { get; }
	internal float BurstKnockback { get; }
	internal Color Color { get; }
	internal float LightStrength { get; }
	internal bool DirectHitCanCrit { get; }
	internal SpearImpactDebuff ImpactDebuff { get; }
	internal int DebuffChancePercent { get; }
	internal int DebuffDurationTicks { get; }
	internal SpearSecondaryEffect SecondaryEffect { get; }

	private int DebuffType => ImpactDebuff switch {
		SpearImpactDebuff.Midas => BuffID.Midas,
		SpearImpactDebuff.Hellfire => BuffID.OnFire3,
		SpearImpactDebuff.CursedInferno => BuffID.CursedInferno,
		SpearImpactDebuff.Venom => BuffID.Venom,
		SpearImpactDebuff.OnFire => BuffID.OnFire,
		_ => 0
	};

	internal void TryApplyImpactDebuff(NPC target)
	{
		if (DebuffType > 0 && DebuffDurationTicks > 0 && Main.rand.Next(100) < DebuffChancePercent)
			target.AddBuff(DebuffType, DebuffDurationTicks);
	}
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
		int dotDps,
		float burstDamageMultiplier,
		int burstRadius,
		float burstKnockback,
		Color color,
		float lightStrength = 0.35f,
		int armorPenetration = 0,
		int extraCrit = 0,
		bool directHitCanCrit = true,
		SpearImpactDebuff impactDebuff = SpearImpactDebuff.None,
		int debuffChancePercent = 100,
		int debuffDurationTicks = 0,
		SpearSecondaryEffect secondaryEffect = SpearSecondaryEffect.None)
	{
		Kind = kind;
		Damage = damage;
		UseTime = useTime;
		Knockback = knockback;
		ShootSpeed = shootSpeed;
		Rarity = rarity;
		Value = value;
		ArmorPenetration = armorPenetration;
		ExtraCrit = extraCrit;
		Spear = new SpearProfile(kind, texturePath, dotDps, burstDamageMultiplier, burstRadius, burstKnockback, color, lightStrength, directHitCanCrit, impactDebuff, debuffChancePercent, debuffDurationTicks, secondaryEffect);
	}

	public SpearKind Kind { get; }
	public int Damage { get; }
	public int UseTime { get; }
	public float Knockback { get; }
	public float ShootSpeed { get; }
	public int Rarity { get; }
	public int Value { get; }
	public int ArmorPenetration { get; }
	public int ExtraCrit { get; }
	public SpearProfile Spear { get; }
}
