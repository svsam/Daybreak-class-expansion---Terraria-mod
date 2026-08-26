using Spears.Content.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Buffs;

public sealed class ThornedDebuff : ModBuff
{
	public override string Texture => "Terraria/Images/Buff_" + BuffID.Webbed;

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.pvpBuff[Type] = false;
	}

	public override void Update(NPC npc, ref int buffIndex)
	{
		if (SpearGlobalNPC.CanCrowdControl(npc))
			npc.GetGlobalNPC<SpearGlobalNPC>().Thorned = true;
		else if (Main.netMode != NetmodeID.MultiplayerClient)
			npc.DelBuff(buffIndex--);
	}
}
