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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenSA.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Colony
{
	public class ColonyBitInfo : TraitInfo
	{
		public readonly int MinLifetime = 500;
		public readonly int MaxLifetime = 1000;
		public readonly string CrushClass = "colony_bit";
		public readonly string PickupSound = "sounds|COLONYPICKUP.SDF";
		public readonly string TimeoutSound = "sounds|COLONYPICKUPTIMEOUT.SDF";

		public override object Create(ActorInitializer init)
		{
			return new ColonyBit(init, this);
		}
	}

	public class ColonyBit : ITick, ICrushable, INotifyCrushed
	{
		readonly ColonyBitInfo info;
		readonly DefeatedColony colony;
		int lifetime;

		public ColonyBit(ActorInitializer init, ColonyBitInfo info)
		{
			this.info = info;

			var colonyBitInit = init.Get<ColonyBitInit>(info);
			colony = colonyBitInit.Value.Actor(init.World).Value.Trait<DefeatedColony>();

			lifetime = init.World.SharedRandom.Next(info.MinLifetime, info.MaxLifetime);
		}

		void ITick.Tick(Actor self)
		{
			if (--lifetime > 0)
				return;

			colony.PickBit(self.World.Players.First(player => player.InternalName == "Creeps"));
			Game.Sound.Play(SoundType.World, info.TimeoutSound, self.CenterPosition);
			self.Dispose();
		}

		bool ICrushable.CrushableBy(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			return self.IsAtGroundLevel() && crushClasses.Contains(info.CrushClass);
		}

		LongBitSet<PlayerBitMask> ICrushable.CrushableBy(Actor self, BitSet<CrushClass> crushClasses)
		{
			return self.World.AllPlayersMask;
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses) { }

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			colony.PickBit(crusher.Owner);
			Game.Sound.Play(SoundType.World, info.PickupSound, self.CenterPosition);
			self.Dispose();
		}
	}
}
