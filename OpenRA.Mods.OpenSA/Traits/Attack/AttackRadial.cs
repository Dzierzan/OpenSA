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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Fire in all directions.")]
	public class AttackRadialInfo : AttackFollowInfo
	{
		public override object Create(ActorInitializer init) { return new AttackRadial(init.Self, this); }
	}

	public class AttackRadial : AttackFollow
	{
		public readonly new AttackRadialInfo Info;

		public AttackRadial(Actor self, AttackRadialInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override void DoAttack(Actor self, in Target target)
		{
			if (!CanAttack(self, target))
				return;

			var directionalOffset = 360 / Armaments.Count();
			var d = 0;
			foreach (var armament in Armaments)
			{
				if (armament.IsTraitDisabled)
					continue;

				var facing = armament.Actor.Trait<IFacing>();
				facing.Facing = WAngle.FromDegrees(d * directionalOffset);

				var barrel = armament.CheckFire(armament.Actor, facing, target);
				if (barrel == null)
					continue;

				foreach (var npa in self.TraitsImplementing<INotifyAttack>())
					npa.Attacking(self, target, armament, barrel);

				d++;
			}
		}
	}
}
