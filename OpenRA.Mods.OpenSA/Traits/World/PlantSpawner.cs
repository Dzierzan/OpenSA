#region Copyright & License Information
/*
 * Copyright The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.World
{
	[TraitLocation(SystemActors.World)]
	public class PlantSpawnerInfo : TraitInfo, Requires<MapCreepsInfo>
	{
		[Desc("Minimum number of plants on the map.")]
		public readonly int Minimum = 1;

		[Desc("Maximum number of plants on the map.")]
		public readonly int Maximum = 255;

		[Desc("Average time (ticks) between plant spawn.")]
		public readonly int[] SpawnInterval = { 180 * 25 };

		[Desc("Delay (in ticks) before the first plant spawns.")]
		public readonly int[] InitialSpawnDelay = { 0 };

		[Desc("Which terrain types can we drop on?")]
		[FieldLoader.Require]
		public readonly HashSet<string> ValidGround = null;

		[ActorReference(typeof(PlantInfo))]
		[FieldLoader.Require]
		[Desc("Plant actors to spawn.")]
		public readonly string[] PlantActors = null;

		[Desc("Chance of each actor spawning.")]
		[FieldLoader.Require]
		public readonly int[] PlantActorShares = null;

		[Desc("Only spawn on this tileset.")]
		public string Tileset = null;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Creeps";

		public override object Create(ActorInitializer init) { return new PlantSpawner(init.Self, this); }
	}

	public class PlantSpawner : ITick, INotifyCreated
	{
		readonly Actor self;
		readonly PlantSpawnerInfo info;
		bool enabled;
		int plants;
		int ticks;

		public PlantSpawner(Actor self, PlantSpawnerInfo info)
		{
			this.self = self;
			this.info = info;

			ticks = Util.RandomInRange(self.World.SharedRandom, info.InitialSpawnDelay);
		}

		void INotifyCreated.Created(Actor self)
		{
			enabled = self.Trait<PlantCreeps>().Enabled;
		}

		void ITick.Tick(Actor self)
		{
			if (!enabled)
				return;

			if (info.Tileset != null & self.World.Map.Tileset != info.Tileset)
				return;

			if (plants > info.Maximum)
					return;

			if (--ticks <= 0)
			{
				ticks = Util.RandomInRange(self.World.SharedRandom, info.SpawnInterval);

				var toSpawn = info.Minimum - plants;
				if (toSpawn <= 0)
					toSpawn = 1;

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
			var plantActor = ChoosePlantActor();

			self.World.AddFrameEndTask(w =>
			{
				w.CreateActor(plantActor, new TypeDictionary
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
				if (self.World.ActorMap.GetActorsAt(p).Any())
					continue;

				return p;
			}

			return null;
		}

		string ChoosePlantActor()
		{
			var plantShares = info.PlantActorShares;
			var n = self.World.SharedRandom.Next(plantShares.Sum());

			var cumulativeShares = 0;
			for (var i = 0; i < plantShares.Length; i++)
			{
				cumulativeShares += plantShares[i];
				if (n <= cumulativeShares)
					return info.PlantActors[i];
			}

			return null;
		}

		public void IncrementPlants()
		{
			plants++;
		}

		public void DecrementPlants()
		{
			plants--;
		}
	}
}
