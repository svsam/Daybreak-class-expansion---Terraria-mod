using Spears.Content.Common;
using Spears.Content.Systems;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class MonarchsSpear : ProgressionSpearItem
{
	internal override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.Monarch;

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe()
			.AddIngredient<GoldSpear>()
			.AddRecipeGroup(SpearProgressionSystem.EvilSpearRecipeGroup)
			.AddIngredient<Hellrend>()
			.AddIngredient<Mightpiercer>()
			.AddIngredient<GeminiGaze>()
			.AddIngredient<Frightsteel>()
			.AddIngredient<FlowerSpike>()
			.AddIngredient<Tepoztopilli>()
			.AddIngredient(ItemID.Spear)
			.AddIngredient(ItemID.DayBreak)
			.AddTile(TileID.WorkBenches);

		SpearRecipeConditions.RequireMoonLord(recipe);
		recipe.Register();
	}
}
