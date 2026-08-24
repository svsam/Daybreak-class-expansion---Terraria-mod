using Spears.Content.Common;
using Spears.Content.Systems;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class Tepoztopilli : ProgressionSpearItem
{
	public override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.Tepoztopilli;

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe()
			.AddIngredient(ItemID.BeetleHusk, 8)
			.AddIngredient(ItemID.LunarTabletFragment, 12)
			.AddTile(TileID.MythrilAnvil);

		SpearRecipeConditions.RequireGolem(recipe);
		recipe.Register();
	}
}
