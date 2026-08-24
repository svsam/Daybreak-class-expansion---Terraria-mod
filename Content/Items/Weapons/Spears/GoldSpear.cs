using Spears.Content.Common;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class GoldSpear : ProgressionSpearItem
{
	internal override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.Gold;

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.GoldBar, 10)
			.AddIngredient(ItemID.Wood, 5)
			.AddTile(TileID.WorkBenches)
			.Register();
	}
}
