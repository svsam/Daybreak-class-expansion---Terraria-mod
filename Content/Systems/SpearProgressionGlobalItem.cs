using Spears.Content.Items.Weapons.Spears;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Spears.Content.Systems;

public sealed class SpearProgressionGlobalItem : GlobalItem
{
	public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
	{
		if (item.type == ItemID.WallOfFleshBossBag)
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<Hellrend>()));
	}
}
