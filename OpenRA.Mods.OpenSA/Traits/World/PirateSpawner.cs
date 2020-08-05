using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Controls the spawning of specified actor types. Attach this to the world actor.")]
	public class PirateSpawnerInfo : ConditionalTraitInfo, Requires<MapCreepsInfo>
	{
		[Desc("Minimum number of actors.")]
		public readonly int Minimum = 0;

		[Desc("Maximum number of actors.")]
		public readonly int Maximum = 4;

		[Desc("Time (in ticks) between actor spawn.")]
		public readonly int[] SpawnInterval = { 6000 };

		[Desc("Delay (in ticks) before the first actor spawns.")]
		public readonly int[] InitialSpawnDelay = { 0 };

		[FieldLoader.Require]
		[ActorReference]
		[Desc("Name of the actor that will be randomly picked to spawn.")]
		public readonly string[] Actors = { };

		[Desc("Which terrain types can we drop on?")]
		[FieldLoader.Require]
		public readonly HashSet<string> ValidGround = null;

		public readonly string Owner = "Creeps";

		public override object Create(ActorInitializer init) { return new PirateSpawner(init.Self, this); }
	}

	public class PirateSpawner : ConditionalTrait<PirateSpawnerInfo>, ITick
	{
		readonly PirateSpawnerInfo info;

		bool enabled;
		int spawnCountdown;
		float actorsPresent;

		public PirateSpawner(Actor self, PirateSpawnerInfo info)
			: base(info)
		{
			this.info = info;

			spawnCountdown = Util.RandomDelay(self.World, info.InitialSpawnDelay);
		}

		protected override void Created(Actor self)
		{
			enabled = self.Trait<MapCreeps>().Enabled;
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !enabled)
				return;

			if (info.Maximum < 1 || actorsPresent >= info.Maximum)
				return;

			if (--spawnCountdown > 0)
				return;

			var spawnPoint = ChooseSpawnCell(self, 100);

			if (spawnPoint == null)
				return;

			spawnCountdown = Util.RandomDelay(self.World, info.SpawnInterval);

			do
			{
				// Always spawn at least one actor, plus
				// however many needed to reach the minimum.
				SpawnActor(self, spawnPoint);
			}
			while (actorsPresent < info.Minimum);
		}

		void SpawnActor(Actor self, CPos? spawnPoint)
		{
			self.World.AddFrameEndTask(w => w.CreateActor(info.Actors.Random(self.World.SharedRandom), new TypeDictionary
			{
				new OwnerInit(w.Players.First(x => x.PlayerName == info.Owner)),
				new LocationInit(spawnPoint.Value)
			}));

			actorsPresent++;
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

		public void DecreaseActorCount(float amount)
		{
			actorsPresent -= amount;
		}
	}
}
