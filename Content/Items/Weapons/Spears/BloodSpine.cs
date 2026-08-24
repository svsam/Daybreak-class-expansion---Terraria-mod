using Spears.Content.Common;
using Spears.Content.Systems;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class BloodSpine : ProgressionSpearItem
{
	internal override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.Crimson;

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe()
			.AddIngredient<GoldSpear>()
			.AddIngredient(ItemID.CrimtaneBar, 12)
			.AddIngredient(ItemID.TissueSample, 8)
			.AddTile(TileID.Anvils);

		SpearRecipeConditions.RequireBrainOfCthulhu(recipe);
		recipe.Register();
	}
}
