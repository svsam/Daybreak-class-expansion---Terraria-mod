using Spears.Content.Common;
using Terraria;
using Terraria.ID;

namespace Spears.Content.Items.Weapons.Spears;

public sealed class Frightsteel : ProgressionSpearItem
{
	internal override SpearKind SpearKind => global::Spears.Content.Common.SpearKind.Frightsteel;

	public override void AddRecipes()
	{
		Recipe recipe = CreateRecipe()
			.AddIngredient<GeminiGaze>()
			.AddIngredient(ItemID.HallowedBar, 12)
			.AddIngredient(ItemID.SoulofFright, 15)
			.AddTile(TileID.MythrilAnvil);

		recipe.Register();
	}
}
