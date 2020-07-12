using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits
{
	public class PlantInfo : TraitInfo
	{
		[Desc("Length of time (in seconds) until the crate gets removed automatically. " +
			"A value of zero disables auto-removal.")]
		public readonly int Lifetime = 0;

		[Desc("Allowed to land on.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new Plant(this); }
	}

	public class Plant : ITick, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly PlantInfo info;

		int ticks;

		public Plant(PlantInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (info.Lifetime != 0 && self.IsInWorld && ++ticks >= info.Lifetime * 25)
				self.Dispose();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var spawner = self.World.WorldActor.TraitsImplementing<PlantSpawner>().FirstEnabledTraitOrDefault();
			if (spawner != null)
				spawner.IncrementPlants();
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			var spawner = self.World.WorldActor.TraitsImplementing<PlantSpawner>().FirstEnabledTraitOrDefault();
			if (spawner != null)
				spawner.DecrementPlants();
		}
	}
}
