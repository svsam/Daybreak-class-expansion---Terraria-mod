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
		int armorPenetration = 0,
		int extraCrit = 0,
		bool directHitCanCrit = true,
		SpearImpactDebuff impactDebuff = SpearImpactDebuff.None,
		int debuffChancePercent = 100,
		int debuffDurationTicks = 0,
		SpearSecondaryEffect secondaryEffect = SpearSecondaryEffect.None)
	{
		Kind = kind;
		TexturePath = texturePath;
		Damage = damage;
		UseTime = useTime;
		Knockback = knockback;
		ShootSpeed = shootSpeed;
		Rarity = rarity;
		Value = value;
		DotDps = dotDps;
		BurstDamageMultiplier = burstDamageMultiplier;
		BurstRadius = burstRadius;
		BurstKnockback = burstKnockback;
		Color = color;
		ArmorPenetration = armorPenetration;
		ExtraCrit = extraCrit;
		DirectHitCanCrit = directHitCanCrit;
		ImpactDebuff = impactDebuff;
		DebuffChancePercent = debuffChancePercent;
		DebuffDurationTicks = debuffDurationTicks;
		SecondaryEffect = secondaryEffect;
	}

	public SpearKind Kind { get; }
	public string TexturePath { get; }
	public int Damage { get; }
	public int UseTime { get; }
	public float Knockback { get; }
	public float ShootSpeed { get; }
	public int Rarity { get; }
	public int Value { get; }
	public int DotDps { get; }
	public float BurstDamageMultiplier { get; }
	public int BurstRadius { get; }
	public float BurstKnockback { get; }
	public Color Color { get; }
	public int ArmorPenetration { get; }
	public int ExtraCrit { get; }
	public bool DirectHitCanCrit { get; }
	public SpearImpactDebuff ImpactDebuff { get; }
	public int DebuffChancePercent { get; }
	public int DebuffDurationTicks { get; }
	public SpearSecondaryEffect SecondaryEffect { get; }

	public int DebuffType => ImpactDebuff switch {
		SpearImpactDebuff.Midas => BuffID.Midas,
		SpearImpactDebuff.Hellfire => BuffID.OnFire3,
		SpearImpactDebuff.CursedInferno => BuffID.CursedInferno,
		SpearImpactDebuff.Venom => BuffID.Venom,
		SpearImpactDebuff.OnFire => BuffID.OnFire,
		_ => 0
	};

	public void TryApplyImpactDebuff(NPC target)
	{
		if (DebuffType > 0 && DebuffDurationTicks > 0 && Main.rand.Next(100) < DebuffChancePercent)
			target.AddBuff(DebuffType, DebuffDurationTicks);
	}
}
