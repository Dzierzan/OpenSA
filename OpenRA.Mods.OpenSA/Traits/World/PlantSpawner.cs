using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits
{
	public class PlantSpawnerInfo : ITraitInfo, ILobbyOptions
	{
		[Translate]
		[Desc("Descriptive label for the plants checkbox in the lobby.")]
		public readonly string CheckboxLabel = "Plants";

		[Translate]
		[Desc("Tooltip description for the plants checkbox in the lobby.")]
		public readonly string CheckboxDescription = "Random plants occur and attack the player.";

		[Desc("Default value of the plants checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = true;

		[Desc("Prevent the plants state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the plants checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the plants checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 0;

		[Desc("Minimum number of plants on the map.")]
		public readonly int Minimum = 1;

		[Desc("Maximum number of plants on the map.")]
		public readonly int Maximum = 255;

		[Desc("Average time (ticks) between crate spawn.")]
		public readonly int SpawnInterval = 180 * 25;

		[Desc("Delay (in ticks) before the first crate spawns.")]
		public readonly int InitialSpawnDelay = 0;

		[Desc("Which terrain types can we drop on?")]
		[FieldLoader.Require]
		public readonly HashSet<string> ValidGround = null;

		[ActorReference]
		[FieldLoader.Require]
		[Desc("Plant actors to spawn.")]
		public readonly string[] PlantActors = null;

		[Desc("Chance of each crate actor spawning.")]
		[FieldLoader.Require]
		public readonly int[] PlantActorShares = null;

		[Desc("Only spawn on this tileset.")]
		public string Tileset = null;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Creeps";

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("crates", CheckboxLabel, CheckboxDescription, CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public object Create(ActorInitializer init) { return new PlantSpawner(init.Self, this); }
	}

	public class PlantSpawner : ITick, INotifyCreated
	{
		readonly Actor self;
		readonly PlantSpawnerInfo info;
		bool enabled;
		int crates;
		int ticks;

		public PlantSpawner(Actor self, PlantSpawnerInfo info)
		{
			this.self = self;
			this.info = info;

			ticks = info.InitialSpawnDelay;
		}

		void INotifyCreated.Created(Actor self)
		{
			enabled = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("plants", info.CheckboxEnabled);
		}

		void ITick.Tick(Actor self)
		{
			if (!enabled)
				return;

			if (info.Tileset != null & self.World.Map.Tileset != info.Tileset)
				return;

			if (--ticks <= 0)
			{
				ticks = info.SpawnInterval;

				var toSpawn = Math.Max(0, info.Minimum - crates)
					+ (crates < info.Maximum && info.Maximum > info.Minimum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnPlant(self);
			}
		}

		void SpawnPlant(Actor self)
		{
			var spawn = ChooseSpawnCell(self, 100);
			if (spawn == null)
				return;

			var position = spawn.Value;
			var crateActor = ChooseCrateActor();

			self.World.AddFrameEndTask(w =>
			{
				w.CreateActor(crateActor, new TypeDictionary
				{
					new OwnerInit(w.Players.First(p => p.InternalName == info.InternalOwner)),
					new LocationInit(position)
				});
			});
		}

		CPos? ChooseSpawnCell(Actor self, int maxTries)
		{
			for (var n = 0; n < maxTries; n++)
			{
				var p = self.World.Map.ChooseRandomCell(self.World.SharedRandom);

				// Is this valid terrain?
				var terrainType = self.World.Map.GetTerrainInfo(p).Type;
				if (!info.ValidGround.Contains(terrainType))
					continue;

				// Don't drop on any actors
				if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(p) != null
					|| self.World.ActorMap.GetActorsAt(p).Any())
						continue;

				return p;
			}

			return null;
		}

		string ChooseCrateActor()
		{
			var crateShares = info.PlantActorShares;
			var n = self.World.SharedRandom.Next(crateShares.Sum());

			var cumulativeShares = 0;
			for (var i = 0; i < crateShares.Length; i++)
			{
				cumulativeShares += crateShares[i];
				if (n <= cumulativeShares)
					return info.PlantActors[i];
			}

			return null;
		}

		public void IncrementPlants()
		{
			crates++;
		}

		public void DecrementPlants()
		{
			crates--;
		}
	}
}
