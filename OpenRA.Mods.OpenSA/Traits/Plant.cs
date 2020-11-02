#region Copyright & License Information
/*
 * Copyright 2019-2020 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
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
