using Spears.Content.Common;
using Spears.Content.Systems;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class Mightpiercer : ProgressionSpearItem
{
	public override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.Mightpiercer;

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe()
			.AddIngredient<Hellrend>()
			.AddIngredient(ItemID.HallowedBar, 12)
			.AddIngredient(ItemID.SoulofMight, 15)
			.AddTile(TileID.MythrilAnvil);

		SpearRecipeConditions.RequireDestroyer(recipe);
		recipe.Register();
	}
}
