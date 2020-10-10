using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	public class CreepFlyerSpawnerInfo : TraitInfo
	{
		[Desc("Average time (ticks) between creep spawn.")]
		public readonly int[] SpawnInterval = { 10 * 25 };

		[Desc("Delay (in ticks) before the first creep spawns.")]
		public readonly int[] InitialSpawnDelay = { 0 };

		[ActorReference(typeof(AircraftInfo))]
		[FieldLoader.Require]
		[Desc("The actors to spawn.")]
		public readonly string[] ActorTypes = null;

		[Desc("Number of facings that the creep may approach from.")]
		public readonly int QuantizedFacings = 8;

		[Desc("Spawn and remove the creeps this far outside the map.")]
		public readonly WDist Cordon = new WDist(7680);

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Creeps";

		[Desc("Only spawn on this tileset.")]
		public string Tileset = null;

		public override object Create(ActorInitializer init) { return new CreepFlyerSpawner(init.Self, this); }
	}

	public class CreepFlyerSpawner : ITick, INotifyCreated
	{
		readonly CreepFlyerSpawnerInfo info;

		bool enabled;
		int ticks;

		public CreepFlyerSpawner(Actor self, CreepFlyerSpawnerInfo info)
		{
			this.info = info;

			ticks = Util.RandomDelay(self.World, info.InitialSpawnDelay);
		}

		void INotifyCreated.Created(Actor self)
		{
			enabled = self.Trait<FlyerCreeps>().Enabled;
		}

		void ITick.Tick(Actor self)
		{
			if (!enabled)
				return;

			if (info.Tileset != null & self.World.Map.Tileset != info.Tileset)
				return;

			if (--ticks <= 0)
			{
				ticks = Util.RandomDelay(self.World, info.SpawnInterval);

				SpawnCreeps(self);
			}
		}

		void SpawnCreeps(Actor self)
		{
			var position = self.World.Map.ChooseRandomCell(self.World.SharedRandom);

			self.World.AddFrameEndTask(w =>
			{
				var actorType = info.ActorTypes.Random(self.World.SharedRandom);
				var actor = self.World.Map.Rules.Actors[actorType];
				var facing = new WAngle(self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);
				var delta = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));

				var altitude = actor.TraitInfo<AircraftInfo>().CruiseAltitude.Length;
				var target = self.World.Map.CenterOfCell(position) + new WVec(0, 0, altitude);
				var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;
				var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;

				var flyer = w.CreateActor(actorType, new TypeDictionary
				{
					new CenterPositionInit(startEdge),
					new OwnerInit(self.World.Players.First(p => p.InternalName == info.InternalOwner)),
					new FacingInit(facing),
				});

				flyer.QueueActivity(false, new Fly(flyer, Target.FromPos(finishEdge)));
				flyer.QueueActivity(new RemoveSelf());
			});
		}
	}
}
