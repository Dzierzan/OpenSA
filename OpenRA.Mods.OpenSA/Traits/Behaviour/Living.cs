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

namespace OpenRA.Mods.OpenSA.Traits.Behaviour
{
	[Desc("Makes infantry feel more alive by randomly rotating or playing an animation when idle.")]
	class LivingInfo : TraitInfo, Requires<MobileInfo>
	{
		[Desc("Chance per tick the actor rotates to a random direction.")]
		public readonly int RotationChance = 1000;

		public override object Create(ActorInitializer init) { return new Living(init, this); }
	}

	class Living : ITick
	{
		private readonly LivingInfo info;
		private readonly Mobile mobile;

		public Living(ActorInitializer init, LivingInfo info)
		{
			this.info = info;
			mobile = init.Self.Trait<Mobile>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.CurrentActivity != null)
				return;

			if (info.RotationChance > 0 && self.World.SharedRandom.Next(1, info.RotationChance) == 1)
				mobile.Facing = new WAngle(self.World.SharedRandom.Next(1024));
		}
	}
}
