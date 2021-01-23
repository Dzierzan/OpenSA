#region Copyright & License Information
/*
 * Copyright 2021 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Radar
{
	public class RadarColorFromFactionInfo : TraitInfo
	{
		public readonly Color NeutralCreepsOverride = Color.FromArgb(227, 217, 205);

		public readonly Color AntsOverride = Color.FromArgb(0, 254, 0);

		public readonly Color BeetlesOverride = Color.FromArgb(0, 254, 254);

		public readonly Color WaspsOverride = Color.FromArgb(239, 206, 37);

		public readonly Color SpidersOverride = Color.FromArgb(253, 2, 0);

		public readonly Color ScorpionsOverride = Color.FromArgb(227, 217, 205);

		public override object Create(ActorInitializer init) { return new RadarColorFromFaction(this); }
	}

	public class RadarColorFromFaction : IRadarColorModifier
	{
		readonly RadarColorFromFactionInfo info;

		public RadarColorFromFaction(RadarColorFromFactionInfo info)
		{
			this.info = info;
		}

		Color IRadarColorModifier.RadarColorOverride(Actor self, Color color)
		{
			if (self.Owner.InternalName == "Neutral" || self.Owner.InternalName == "Creeps")
				return info.NeutralCreepsOverride;

			if (self.Owner.InternalName.StartsWith("Multi"))
				return color;

			if (self.Owner.Faction.InternalName == "ants")
				return info.AntsOverride;

			if (self.Owner.Faction.InternalName == "beetles")
				return info.BeetlesOverride;

			if (self.Owner.Faction.InternalName == "wasps")
				return info.WaspsOverride;

			if (self.Owner.Faction.InternalName == "spiders")
				return info.SpidersOverride;

			if (self.Owner.Faction.InternalName == "scorpions")
				return info.ScorpionsOverride;

			return color;
		}
	}
}
