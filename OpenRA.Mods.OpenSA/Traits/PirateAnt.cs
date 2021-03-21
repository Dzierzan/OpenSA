#region Copyright & License Information
/*
 * Copyright 2019-2021 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	class PirateAntInfo : TraitInfo, Requires<MobileInfo>
	{
		public override object Create(ActorInitializer init) { return new PirateAnt(init.Self); }
	}

	class PirateAnt : INotifyCreated, INotifyActorDisposing
	{
		readonly PirateSpawner spawner;
		readonly Mobile mobile;
		bool disposed;

		public int AntHoleAmount;

		public PirateAnt(Actor self)
		{
			mobile = self.Trait<Mobile>();
			spawner = self.World.WorldActor.Trait<PirateSpawner>();
		}

		void INotifyCreated.Created(Actor self)
		{
			for (var i = 0; i < 3; i++)
				mobile.Nudge(self);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			if (AntHoleAmount == 0)
				return;

			spawner.DecreaseActorCount(1 / AntHoleAmount);
			disposed = true;
		}
	}
}
