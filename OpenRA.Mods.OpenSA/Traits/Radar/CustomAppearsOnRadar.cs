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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenSA.Traits.Colony;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Radar
{
	[Desc("Used to draw a clock animation during production.")]
	public class CustomAppearsOnRadarInfo : TraitInfo
	{
		public readonly CVec Offset = CVec.Zero;

		[Desc("'Speed' at which the clock rotates.")]
		public readonly float TimeStep = .5f;

		public override object Create(ActorInitializer init)
		{
			return new CustomAppearsOnRadar(init.Self, this);
		}
	}

	public class CustomAppearsOnRadar : INotifyCreated, ITick, IRadarSignature
	{
		readonly IEnumerable<ProductionQueue> productionQueues;

		readonly CustomAppearsOnRadarInfo info;

		readonly List<CVec> clockCells = new List<CVec>();

		IRadarColorModifier modifier;

		float t = 0;
		bool isLarge;

		public CustomAppearsOnRadar(Actor self, CustomAppearsOnRadarInfo info)
		{
			productionQueues = self.TraitsImplementing<ProductionQueue>();

			this.info = info;

			clockCells.Add(new CVec(1, 1));
			clockCells.Add(new CVec(2, 1));
			clockCells.Add(new CVec(3, 1));
			clockCells.Add(new CVec(4, 1));
			clockCells.Add(new CVec(4, 2));
			clockCells.Add(new CVec(4, 3));
			clockCells.Add(new CVec(4, 4));
			clockCells.Add(new CVec(3, 4));
			clockCells.Add(new CVec(2, 4));
			clockCells.Add(new CVec(1, 4));
			clockCells.Add(new CVec(1, 3));
			clockCells.Add(new CVec(1, 2));
		}

		void INotifyCreated.Created(Actor self)
		{
			modifier = self.TraitsImplementing<IRadarColorModifier>().FirstOrDefault();
			var dim = self.TraitOrDefault<Building>()?.Info.Dimensions ?? new CVec(0, 0);
			isLarge = dim.X == 6 && dim.Y == 6;
		}

		void ITick.Tick(Actor self)
		{
			t += info.TimeStep;
		}

		void IRadarSignature.PopulateRadarSignatureCells(Actor self, List<(CPos, Color)> destinationBuffer)
		{
			var isProducing = productionQueues.Any(queue => queue.AllQueued().Any());

			var color = Game.Settings.Game.UsePlayerStanceColors ? self.Owner.PlayerRelationshipColor(self) : self.Owner.Color;
			if (modifier != null)
				color = modifier.RadarColorOverride(self, color);

			var rotate = (int)t % 12;

			if (isLarge)
			{
				for (var y = 0; y < 6; y++)
				{
					for (var x = 0; x < 6; x++)
					{
						var coordinate = new CVec(x, y);

						if (!isProducing && clockCells.Contains(coordinate))
							continue;

						if (isProducing && clockCells.Contains(coordinate) && coordinate != clockCells[rotate])
							continue;

						destinationBuffer.Add((self.Location + new CVec(x, y) + info.Offset, color));
					}
				}
			}
			else
				destinationBuffer.Add((self.Location, color));
		}
	}
}
