using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
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

	private static readonly Dictionary<int, EaterEncounter> EaterEncounters = new();
	private static readonly Dictionary<int, int> EaterSegmentEncounterIds = new();
	private static readonly Dictionary<int, int> EaterRootEncounterIds = new();
	private static readonly HashSet<int> CurrentEaterSegments = new();
	private static readonly List<int> CompletedEaterEncounters = new();
	private static int nextEaterEncounterId;
	private static bool validEaterCompletionPending;

	public static bool DownedEaterOfWorlds { get; private set; }
	public static bool DownedBrainOfCthulhu { get; private set; }

	public override void ClearWorld()
	{
		DownedEaterOfWorlds = false;
		DownedBrainOfCthulhu = false;
		ResetEaterTracking();
	}

	public override void OnWorldUnload()
	{
		DownedEaterOfWorlds = false;
		DownedBrainOfCthulhu = false;
		ResetEaterTracking();
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
		ResetEaterTracking();
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
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;

		if (DownedEaterOfWorlds) {
			ResetEaterTracking();
			return;
		}

		foreach (EaterEncounter encounter in EaterEncounters.Values)
			encounter.SeenSegments.Clear();

		CurrentEaterSegments.Clear();
		for (int index = 0; index < Main.maxNPCs; index++) {
			NPC npc = Main.npc[index];
			if (!npc.active || !IsEaterSegment(npc))
				continue;

			CurrentEaterSegments.Add(index);
			if (!EaterSegmentEncounterIds.ContainsKey(index))
				TrackEaterSpawn(npc, null);

			if (EaterSegmentEncounterIds.TryGetValue(index, out int encounterId) && EaterEncounters.TryGetValue(encounterId, out EaterEncounter encounter))
				encounter.SeenSegments.Add(index);
		}

		CompletedEaterEncounters.Clear();
		foreach ((int encounterId, EaterEncounter encounter) in EaterEncounters) {
			foreach (int segmentId in encounter.ActiveSegments) {
				if (encounter.SeenSegments.Contains(segmentId))
					continue;

				if (!encounter.KilledSegments.Remove(segmentId))
					encounter.InvalidatedByDespawn = true;

				if (EaterSegmentEncounterIds.TryGetValue(segmentId, out int mappedEncounterId) && mappedEncounterId == encounterId)
					EaterSegmentEncounterIds.Remove(segmentId);
			}

			encounter.ActiveSegments.Clear();
			encounter.ActiveSegments.UnionWith(encounter.SeenSegments);

			if (encounter.ScanDelay > 0)
				encounter.ScanDelay--;

			if (encounter.ActiveSegments.Count == 0 && encounter.ScanDelay == 0) {
				if (encounter.SawKill && !encounter.InvalidatedByDespawn)
					validEaterCompletionPending = true;
				CompletedEaterEncounters.Add(encounterId);
			}
		}

		foreach (int encounterId in CompletedEaterEncounters) {
			if (EaterEncounters.Remove(encounterId, out EaterEncounter removedEncounter) && EaterRootEncounterIds.TryGetValue(removedEncounter.RootSegmentId, out int mappedEncounterId) && mappedEncounterId == encounterId)
				EaterRootEncounterIds.Remove(removedEncounter.RootSegmentId);
		}

		// A valid encounter can finish while an unrelated Eater still exists. Remember
		// that completion, but preserve the vanilla-style global no-segments check.
		if (validEaterCompletionPending && CurrentEaterSegments.Count == 0)
			SetDownedEaterOfWorlds();
	}

	internal static bool IsEaterSegment(NPC npc) =>
		npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;

	internal static void TrackEaterSpawn(NPC npc, IEntitySource source)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || DownedEaterOfWorlds || !IsEaterSegment(npc))
			return;

		int segmentId = npc.whoAmI;
		if (EaterSegmentEncounterIds.TryGetValue(segmentId, out int existingId) && EaterEncounters.TryGetValue(existingId, out EaterEncounter existingEncounter) && !existingEncounter.KilledSegments.Contains(segmentId)) {
			// Vanilla can change an Eater segment's defaults in place. The external slot
			// mapping deliberately survives that GlobalNPC recreation.
			existingEncounter.ActiveSegments.Add(segmentId);
			existingEncounter.KilledSegments.Remove(segmentId);
			return;
		}

		int rootSegmentId = npc.realLife >= 0 ? npc.realLife : segmentId;
		int encounterId = FindParentEncounter(npc, source);
		if (encounterId < 0) {
			encounterId = nextEaterEncounterId++;
			EaterEncounters.Add(encounterId, new EaterEncounter(rootSegmentId));
			EaterRootEncounterIds[rootSegmentId] = encounterId;
		}

		AssignSegmentToEncounter(segmentId, encounterId);
	}

	internal static void ScheduleEaterCompletionCheck(NPC npc)
	{
		if (DownedEaterOfWorlds)
			return;

		if (!EaterSegmentEncounterIds.TryGetValue(npc.whoAmI, out int encounterId) || !EaterEncounters.TryGetValue(encounterId, out EaterEncounter encounter)) {
			TrackEaterSpawn(npc, null);
			if (!EaterSegmentEncounterIds.TryGetValue(npc.whoAmI, out encounterId) || !EaterEncounters.TryGetValue(encounterId, out encounter))
				return;
		}

		encounter.KilledSegments.Add(npc.whoAmI);
		encounter.SawKill = true;
		encounter.ScanDelay = 2;
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

	private static void ResetEaterTracking()
	{
		EaterEncounters.Clear();
		EaterSegmentEncounterIds.Clear();
		EaterRootEncounterIds.Clear();
		CurrentEaterSegments.Clear();
		CompletedEaterEncounters.Clear();
		nextEaterEncounterId = 0;
		validEaterCompletionPending = false;
	}

	private static int FindParentEncounter(NPC npc, IEntitySource source)
	{
		if (source is EntitySource_Parent { Entity: NPC parent } && IsEaterSegment(parent) && EaterSegmentEncounterIds.TryGetValue(parent.whoAmI, out int parentEncounterId))
			return parentEncounterId;

		if (npc.realLife >= 0 && EaterSegmentEncounterIds.TryGetValue(npc.realLife, out int realLifeEncounterId))
			return realLifeEncounterId;

		int rootSegmentId = npc.realLife >= 0 ? npc.realLife : npc.whoAmI;
		if (EaterRootEncounterIds.TryGetValue(rootSegmentId, out int rootEncounterId) && EaterEncounters.TryGetValue(rootEncounterId, out EaterEncounter rootEncounter) && !rootEncounter.KilledSegments.Contains(rootSegmentId))
			return rootEncounterId;

		return -1;
	}

	private static void AssignSegmentToEncounter(int segmentId, int encounterId)
	{
		if (EaterSegmentEncounterIds.TryGetValue(segmentId, out int oldEncounterId) && oldEncounterId != encounterId && EaterEncounters.TryGetValue(oldEncounterId, out EaterEncounter oldEncounter)) {
			oldEncounter.ActiveSegments.Remove(segmentId);
			if (!oldEncounter.KilledSegments.Remove(segmentId))
				oldEncounter.InvalidatedByDespawn = true;
		}

		EaterSegmentEncounterIds[segmentId] = encounterId;
		EaterEncounter encounter = EaterEncounters[encounterId];
		encounter.ActiveSegments.Add(segmentId);
		encounter.KilledSegments.Remove(segmentId);
	}

	private static void SyncWorldData()
	{
		if (Main.netMode == NetmodeID.Server)
			NetMessage.SendData(MessageID.WorldData);
	}

	private sealed class EaterEncounter
	{
		internal EaterEncounter(int rootSegmentId)
		{
			RootSegmentId = rootSegmentId;
		}

		internal readonly HashSet<int> ActiveSegments = new();
		internal readonly HashSet<int> SeenSegments = new();
		internal readonly HashSet<int> KilledSegments = new();
		internal int RootSegmentId { get; }
		internal int ScanDelay;
		internal bool SawKill;
		internal bool InvalidatedByDespawn;
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
