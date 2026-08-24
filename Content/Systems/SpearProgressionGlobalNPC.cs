using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Spears.Content.Items.Weapons.Spears;

namespace Spears.Content.Systems;

public sealed class SpearProgressionGlobalNPC : GlobalNPC
{
	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		if (SpearProgressionSystem.IsEaterSegment(npc))
			SpearProgressionSystem.TrackEaterSpawn(npc, source);
	}

	public override void OnKill(NPC npc)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		if (npc.type == NPCID.BrainofCthulhu) {
			SpearProgressionSystem.SetDownedBrainOfCthulhu();
			return;
		}

		if (SpearProgressionSystem.IsEaterSegment(npc))
			SpearProgressionSystem.ScheduleEaterCompletionCheck(npc);
	}

	public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
	{
		if (npc.type == NPCID.WallofFlesh) {
			npcLoot.Add(ItemDropRule.ByCondition(
				new Conditions.NotExpert(),
				ModContent.ItemType<Hellrend>()));
		}
	}
}
