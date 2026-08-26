using Spears.Content.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Buffs;

public sealed class GoldCurseDebuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_" + BuffID.Midas;

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.pvpBuff[Type] = false;
	}

	public override void Update(NPC npc, ref int buffIndex)
	{
		if (SpearGlobalNPC.CanReceiveGoldCurse(npc)) {
			SpearGlobalNPC spearState = npc.GetGlobalNPC<SpearGlobalNPC>();
			spearState.GoldCursed = true;
			spearState.UpdateGoldCurseState(npc, npc.buffTime[buffIndex]);
		}
		else if (Main.netMode != NetmodeID.MultiplayerClient)
			npc.DelBuff(buffIndex--);
	}
}
