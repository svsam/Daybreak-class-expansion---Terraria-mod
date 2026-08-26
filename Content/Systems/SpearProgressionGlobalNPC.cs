using Spears.Content.Items.Weapons.Spears;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Systems;

public sealed class SpearProgressionGlobalNPC : GlobalNPC
{
	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		if (npc.type == NPCID.WallofFlesh) {
			npcLoot.Add(ItemDropRule.ByCondition(
				new Conditions.NotExpert(),
				ModContent.ItemType<Hellrend>()));
		}
	}
}
