using System;
using Microsoft.Xna.Framework;
using Spears.Content.Common;
using Spears.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Players;

public sealed class SpearPlayer : ModPlayer
{
	private int _monarchCycleIndex;
	private bool _nextStandaloneGeminiSpazmatism;
	private bool _nextMonarchGeminiSpazmatism;
	private bool _orbitalsInitialized;
	private readonly int[] _orbitalCooldowns = new int[3];
	private readonly SpearKind[] _orbitalIdentities = new SpearKind[3];

	internal SpearKind NextMonarchIdentity()
	{
		SpearKind[] cycle = WeaponProfileRegistry.MonarchCycle;
		SpearKind result = cycle[_monarchCycleIndex % cycle.Length];
		_monarchCycleIndex = (_monarchCycleIndex + 1) % cycle.Length;
		return result;
	}

	internal bool NextGeminiMode(bool monarchCycle)
	{
		bool result = monarchCycle ? _nextMonarchGeminiSpazmatism : _nextStandaloneGeminiSpazmatism;
		if (monarchCycle)
			_nextMonarchGeminiSpazmatism = !_nextMonarchGeminiSpazmatism;
		else
			_nextStandaloneGeminiSpazmatism = !_nextStandaloneGeminiSpazmatism;
		return result;
	}

	public override void PostUpdate()
	{
		bool holdingMonarch = !Player.dead && Player.HeldItem?.ModItem is ProgressionSpearItem spearItem && spearItem.SpearKind == SpearKind.Monarch;
		if (!holdingMonarch) {
			if (_orbitalsInitialized)
				RemoveOrbitals();
			_orbitalsInitialized = false;
			return;
		}

		if (Player.whoAmI != Main.myPlayer)
			return;

		if (!_orbitalsInitialized)
			InitializeOrbitals();

		EnsureOrbitalProjectiles();
		UpdateOrbitalAttacks();
	}

	private void InitializeOrbitals()
	{
		_orbitalsInitialized = true;
		for (int i = 0; i < 3; i++) {
			_orbitalCooldowns[i] = (i + 1) * 15;
			_orbitalIdentities[i] = RollInitialDistinctIdentity(i);
		}
	}

	private SpearKind RollInitialDistinctIdentity(int slot)
	{
		SpearKind[] cycle = WeaponProfileRegistry.MonarchCycle;
		int start = Main.rand.Next(cycle.Length);
		for (int offset = 0; offset < cycle.Length; offset++) {
			SpearKind candidate = cycle[(start + offset) % cycle.Length];
			bool duplicate = false;
			for (int previous = 0; previous < slot; previous++)
				duplicate |= _orbitalIdentities[previous] == candidate;
			if (!duplicate)
				return candidate;
		}
		return cycle[slot];
	}

	private void EnsureOrbitalProjectiles()
	{
		int projectileType = ModContent.ProjectileType<MonarchOrbitalProjectile>();
		for (int slot = 0; slot < 3; slot++) {
			Projectile existing = FindOrbital(projectileType, slot);
			if (existing is null) {
				int index = Projectile.NewProjectile(Player.GetSource_Misc("MonarchOrbitals"), Player.Center, Vector2.Zero, projectileType, 0, 0f, Player.whoAmI, slot, (float)_orbitalIdentities[slot]);
				if (index >= 0 && index < Main.maxProjectiles)
					Main.projectile[index].netUpdate = true;
			}
			else if ((SpearKind)(int)existing.ai[1] != _orbitalIdentities[slot]) {
				existing.ai[1] = (float)_orbitalIdentities[slot];
				existing.netUpdate = true;
			}
		}
	}

	private void UpdateOrbitalAttacks()
	{
		for (int slot = 0; slot < 3; slot++) {
			if (_orbitalCooldowns[slot] > 0)
				_orbitalCooldowns[slot]--;
			if (_orbitalCooldowns[slot] > 0)
				continue;

			Projectile orbital = FindOrbital(ModContent.ProjectileType<MonarchOrbitalProjectile>(), slot);
			Vector2 origin = orbital?.Center ?? Player.Center;
			int targetIndex = SpearTargeting.FindClosestVisibleTarget(origin, 800f);
			if (targetIndex < 0) {
				_orbitalCooldowns[slot] = 45;
				continue;
			}

			Vector2 velocity = origin.DirectionTo(Main.npc[targetIndex].Center) * 16f;
			int damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * 0.35f));
			SpearSecondaryProjectile.Spawn(Player.GetSource_ItemUse(Player.HeldItem, "MonarchOrbitalShot"), origin, velocity, Player.whoAmI, damage, 0f, SpearSecondaryKind.OrbitalBolt, _orbitalIdentities[slot], targetIndex);

			_orbitalCooldowns[slot] = 45;
			_orbitalIdentities[slot] = RollDistinctIdentity(slot);
		}
	}

	private SpearKind RollDistinctIdentity(int slot)
	{
		SpearKind[] cycle = WeaponProfileRegistry.MonarchCycle;
		int start = Main.rand.Next(cycle.Length);
		for (int offset = 0; offset < cycle.Length; offset++) {
			SpearKind candidate = cycle[(start + offset) % cycle.Length];
			bool duplicate = false;
			for (int other = 0; other < 3; other++) {
				if (other != slot && _orbitalIdentities[other] == candidate) {
					duplicate = true;
					break;
				}
			}
			if (!duplicate)
				return candidate;
		}
		return cycle[slot];
	}

	private Projectile FindOrbital(int projectileType, int slot)
	{
		for (int i = 0; i < Main.maxProjectiles; i++) {
			Projectile projectile = Main.projectile[i];
			if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == projectileType && (int)projectile.ai[0] == slot)
				return projectile;
		}
		return null;
	}

	private void RemoveOrbitals()
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		int projectileType = ModContent.ProjectileType<MonarchOrbitalProjectile>();
		for (int i = 0; i < Main.maxProjectiles; i++) {
			Projectile projectile = Main.projectile[i];
			if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == projectileType)
				projectile.Kill();
		}
	}
}
