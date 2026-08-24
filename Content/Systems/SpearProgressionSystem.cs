using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Spears.Content.Systems;

/// <summary>
/// Stores the evil-biome boss completions separately. Terraria's downedBoss2 flag
/// deliberately combines the Eater of Worlds and Brain of Cthulhu, which is not
/// precise enough for the two progression recipes in this mod.
/// </summary>
public sealed class SpearProgressionSystem : ModSystem
{
	internal const string EvilSpearRecipeGroup = "Spears:EvilSpear";

	private const string EaterSaveKey = "downedEaterOfWorlds";
	private const string BrainSaveKey = "downedBrainOfCthulhu";

	private static int eaterScanDelay;

	public static bool DownedEaterOfWorlds { get; private set; }
	public static bool DownedBrainOfCthulhu { get; private set; }

	public override void ClearWorld()
	{
		DownedEaterOfWorlds = false;
		DownedBrainOfCthulhu = false;
		eaterScanDelay = 0;
	}

	public override void OnWorldUnload()
	{
		DownedEaterOfWorlds = false;
		DownedBrainOfCthulhu = false;
		eaterScanDelay = 0;
	}

	public override void SaveWorldData(TagCompound tag)
	{
		if (DownedEaterOfWorlds)
			tag[EaterSaveKey] = true;

		if (DownedBrainOfCthulhu)
			tag[BrainSaveKey] = true;
	}

	public override void LoadWorldData(TagCompound tag)
	{
		DownedEaterOfWorlds = tag.ContainsKey(EaterSaveKey);
		DownedBrainOfCthulhu = tag.ContainsKey(BrainSaveKey);
		eaterScanDelay = 0;
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(DownedEaterOfWorlds);
		writer.Write(DownedBrainOfCthulhu);
	}

	public override void NetReceive(BinaryReader reader)
	{
		DownedEaterOfWorlds = reader.ReadBoolean();
		DownedBrainOfCthulhu = reader.ReadBoolean();
	}

	public override void AddRecipeGroups()
	{
		RecipeGroup.RegisterGroup(
			EvilSpearRecipeGroup,
			new RecipeGroup(
				() => Language.GetTextValue("Mods.Spears.RecipeGroups.EvilSpear"),
				ModContent.ItemType<Items.Weapons.Spears.NightsSpine>(),
				ModContent.ItemType<Items.Weapons.Spears.BloodSpine>()));
	}

	public override void PostUpdateWorld()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || DownedEaterOfWorlds || eaterScanDelay <= 0)
			return;

		if (--eaterScanDelay > 0)
			return;

		if (!AnyEaterSegmentAlive())
			SetDownedEaterOfWorlds();
	}

	internal static void ScheduleEaterCompletionCheck()
	{
		if (!DownedEaterOfWorlds)
			eaterScanDelay = 2;
	}

	internal static void SetDownedBrainOfCthulhu()
	{
		if (DownedBrainOfCthulhu)
			return;

		DownedBrainOfCthulhu = true;
		SyncWorldData();
	}

	private static void SetDownedEaterOfWorlds()
	{
		if (DownedEaterOfWorlds)
			return;

		DownedEaterOfWorlds = true;
		SyncWorldData();
	}

	private static bool AnyEaterSegmentAlive()
	{
		for (int index = 0; index < Main.maxNPCs; index++) {
			NPC npc = Main.npc[index];
			if (!npc.active)
				continue;

			if (npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail)
				return true;
		}

		return false;
	}

	private static void SyncWorldData()
	{
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.WorldData);
	}
}

internal static class SpearRecipeConditions
{
	internal static void RequireEaterOfWorlds(Recipe recipe) =>
		Add(recipe, "DownedEaterOfWorlds", () => SpearProgressionSystem.DownedEaterOfWorlds);

	internal static void RequireBrainOfCthulhu(Recipe recipe) =>
		Add(recipe, "DownedBrainOfCthulhu", () => SpearProgressionSystem.DownedBrainOfCthulhu);

	internal static void RequireDestroyer(Recipe recipe) =>
		Add(recipe, "DownedDestroyer", () => NPC.downedMechBoss1);

	internal static void RequireTwins(Recipe recipe) =>
		Add(recipe, "DownedTwins", () => NPC.downedMechBoss2);

	internal static void RequireSkeletronPrime(Recipe recipe) =>
		Add(recipe, "DownedSkeletronPrime", () => NPC.downedMechBoss3);

	internal static void RequirePlantera(Recipe recipe) =>
		Add(recipe, "DownedPlantera", () => NPC.downedPlantBoss);

	internal static void RequireGolem(Recipe recipe) =>
		Add(recipe, "DownedGolem", () => NPC.downedGolemBoss);

	internal static void RequireMoonLord(Recipe recipe) =>
		Add(recipe, "DownedMoonLord", () => NPC.downedMoonlord);

	private static void Add(Recipe recipe, string key, Func<bool> predicate)
	{
		recipe.AddCondition(Language.GetText($"Mods.Spears.Conditions.{key}"), predicate);
	}
}
