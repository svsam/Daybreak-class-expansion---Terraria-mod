using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Spears.Content.Systems;

public sealed class SpearProgressionSystem : ModSystem
{
	internal const string EvilSpearRecipeGroup = "Spears:EvilSpear";

	public override void AddRecipeGroups()
	{
		RecipeGroup.RegisterGroup(
			EvilSpearRecipeGroup,
			new RecipeGroup(
				() => Language.GetTextValue("Mods.Spears.RecipeGroups.EvilSpear"),
				ModContent.ItemType<Items.Weapons.Spears.NightsSpine>(),
				ModContent.ItemType<Items.Weapons.Spears.BloodSpine>()));
	}
}
