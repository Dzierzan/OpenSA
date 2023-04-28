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

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Colony
{
	public class TakeColonyBitOnIdleInfo : TraitInfo
	{
		public readonly int Radius = 10;
		public readonly int Delay = 50;
		public readonly int OnTick = 50;

		public override object Create(ActorInitializer init)
		{
			return new TakeColonyBitOnIdle(this);
		}
	}

	public class TakeColonyBitOnIdle : ITick
	{
		private readonly TakeColonyBitOnIdleInfo info;
		private int idleSince;

		public TakeColonyBitOnIdle(TakeColonyBitOnIdleInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsIdle)
				idleSince = 0;
			else if (idleSince < info.Delay)
				idleSince++;
			else if (self.World.WorldTick % info.OnTick == 0)
			{
				var actors = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(info.Radius))
					.OrderBy(actor => (actor.Location - self.Location).Length);

				foreach (var actor in actors)
				{
					if (!actor.Info.HasTraitInfo<ColonyBitInfo>())
						continue;

					self.QueueActivity(new Move(self, actor.Location));
					break;
				}
			}
		}
	}
}
