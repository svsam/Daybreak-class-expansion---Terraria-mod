using Spears.Content.Common;
using Spears.Content.Systems;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class FlowerSpike : ProgressionSpearItem
{
	public override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.FlowerSpike;

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe()
			.AddIngredient<Frightsteel>()
			.AddIngredient(ItemID.ChlorophyteBar, 18)
			.AddIngredient(ItemID.Ectoplasm, 10)
			.AddTile(TileID.MythrilAnvil);

		SpearRecipeConditions.RequirePlantera(recipe);
		recipe.Register();
	}
}
