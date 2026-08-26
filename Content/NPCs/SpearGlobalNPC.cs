using System;
using System.IO;
using Microsoft.Xna.Framework;
using Spears.Content.Buffs;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Spears.Content.NPCs;

public sealed class SpearGlobalNPC : GlobalNPC
{
	private static int _nextSpawnSerial;
	private int _goldCurseBonusPercent;
	private int _goldCurseTicks;
	private int _thornCooldownTicks;

	public override bool InstancePerEntity => true;

	internal bool GoldCursed;
	internal bool Thorned;
	internal int SpawnSerial { get; private set; }

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		_nextSpawnSerial = unchecked(_nextSpawnSerial + 1);
		if (_nextSpawnSerial == 0)
			_nextSpawnSerial = 1;
		SpawnSerial = _nextSpawnSerial;
		_goldCurseBonusPercent = 0;
		_goldCurseTicks = 0;
		_thornCooldownTicks = 0;
	}

	public override void ResetEffects(NPC npc)
	{
		GoldCursed = false;
		Thorned = false;
		if (Main.netMode != NetmodeID.MultiplayerClient && _goldCurseTicks > 0) {
			if (!npc.HasBuff(ModContent.BuffType<GoldCurseDebuff>())) {
				_goldCurseTicks = 0;
				_goldCurseBonusPercent = 0;
			}
			else {
				_goldCurseTicks--;
			}
		}
		if (Main.netMode != NetmodeID.MultiplayerClient && _thornCooldownTicks > 0) {
			_thornCooldownTicks--;
			if (_thornCooldownTicks == 0)
				npc.netUpdate = true;
		}
	}

	internal void TryApplyGoldCurse(NPC npc)
	{
		if (!CanReceiveGoldCurse(npc))
			return;

		int buffType = ModContent.BuffType<GoldCurseDebuff>();
		npc.AddBuff(buffType, 300);
		if (Main.netMode != NetmodeID.MultiplayerClient)
			UpdateGoldCurseState(npc, 300);
	}

	internal void UpdateGoldCurseState(NPC npc, int remainingTicks)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (_goldCurseBonusPercent <= 0)
			_goldCurseBonusPercent = RollGoldBonusPercent();
		_goldCurseTicks = Math.Max(_goldCurseTicks, remainingTicks);
		npc.netUpdate = true;
	}

	internal bool TryApplyThorned(NPC npc)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || _thornCooldownTicks > 0 || !CanCrowdControl(npc))
			return false;

		_thornCooldownTicks = 1800;
		npc.AddBuff(ModContent.BuffType<ThornedDebuff>(), 180);
		npc.netUpdate = true;
		return true;
	}

	public override bool PreAI(NPC npc)
	{
		if (!Thorned || !CanCrowdControl(npc))
			return true;

		npc.velocity = Vector2.Zero;
		if (Main.netMode != NetmodeID.MultiplayerClient && Main.GameUpdateCount % 10 == 0)
			npc.netUpdate = true;
		return false;
	}

	public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) => !Thorned;
	public override bool CanHitNPC(NPC npc, NPC target) => !Thorned;

	public override void DrawEffects(NPC npc, ref Color drawColor)
	{
		if (Thorned)
			drawColor = Color.Lerp(drawColor, new Color(102, 116, 56), 0.55f);
		else if (GoldCursed)
			drawColor = Color.Lerp(drawColor, new Color(255, 210, 55), 0.35f);
	}

	public override void OnKill(NPC npc)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || _goldCurseBonusPercent <= 0 || _goldCurseTicks <= 0 || npc.value <= 0f)
			return;

		long extraValue = (long)Math.Round(npc.value * _goldCurseBonusPercent / 100d, MidpointRounding.AwayFromZero);
		if (extraValue <= 0)
			return;

		int[] coinCounts = Utils.CoinsSplit(extraValue);
		int[] coinTypes = { ItemID.CopperCoin, ItemID.SilverCoin, ItemID.GoldCoin, ItemID.PlatinumCoin };
		for (int i = 0; i < coinTypes.Length; i++) {
			int remaining = coinCounts[i];
			while (remaining > 0) {
				int stack = Math.Min(remaining, Item.CommonMaxStack);
				Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, coinTypes[i], stack);
				remaining -= stack;
			}
		}
	}

	public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		binaryWriter.Write((byte)_goldCurseBonusPercent);
		binaryWriter.Write((short)_goldCurseTicks);
		binaryWriter.Write((short)_thornCooldownTicks);
	}

	public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
	{
		_goldCurseBonusPercent = binaryReader.ReadByte();
		_goldCurseTicks = binaryReader.ReadInt16();
		_thornCooldownTicks = binaryReader.ReadInt16();
	}

	internal static bool CanReceiveGoldCurse(NPC npc) =>
		npc.active
		&& !npc.friendly
		&& !npc.townNPC
		&& !npc.isLikeATownNPC
		&& !npc.boss
		&& !NPCID.Sets.ShouldBeCountedAsBoss[npc.type]
		&& npc.value > 0f;

	internal static bool CanCrowdControl(NPC npc)
	{
		if (!npc.active || npc.friendly || npc.townNPC || npc.isLikeATownNPC || npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])
			return false;
		if (npc.knockBackResist <= 0f || npc.realLife >= 0 || npc.aiStyle == NPCAIStyleID.Worm)
			return false;
		return npc.lifeMax > 5 && npc.CanBeChasedBy();
	}

	private static int RollGoldBonusPercent()
	{
		int roll = Main.rand.Next(100);
		if (roll < 50)
			return 25;
		if (roll < 80)
			return 50;
		if (roll < 95)
			return 75;
		return 100;
	}
}
