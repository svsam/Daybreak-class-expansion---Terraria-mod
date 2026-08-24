using Spears.Content.Common;
using Spears.Content.Systems;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class NightsSpine : ProgressionSpearItem
{
	public override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.Corruption;

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe()
			.AddIngredient<GoldSpear>()
			.AddIngredient(ItemID.DemoniteBar, 12)
			.AddIngredient(ItemID.ShadowScale, 8)
			.AddTile(TileID.Anvils);

		SpearRecipeConditions.RequireEaterOfWorlds(recipe);
		recipe.Register();
	}
}
