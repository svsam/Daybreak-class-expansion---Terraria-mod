using Spears.Content.Common;
using Spears.Content.Systems;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class GeminiGaze : ProgressionSpearItem
{
	internal override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.Gemini;

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe()
			.AddIngredient<Mightpiercer>()
			.AddIngredient(ItemID.HallowedBar, 12)
			.AddIngredient(ItemID.SoulofSight, 15)
			.AddTile(TileID.MythrilAnvil);

		SpearRecipeConditions.RequireTwins(recipe);
		recipe.Register();
	}
}
