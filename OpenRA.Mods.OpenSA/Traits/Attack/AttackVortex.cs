#region Copyright & License Information
/*
 * Copyright 2021, 2022 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Random attack with a rotational movement.")]
	public class AttackVortexInfo : AttackOmniInfo, Requires<IFacingInfo>
	{
		public readonly int RotationRate = 1;

		public override object Create(ActorInitializer init) { return new AttackVortex(init.Self, this); }
	}

	public class AttackVortex : AttackOmni
	{
		public new readonly AttackVortexInfo Info;

		public AttackVortex(Actor self, AttackVortexInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override Activity GetAttackActivity(Actor self, AttackSource source, in Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			return new SwitchTargets(this, newTarget);
		}

		public class SwitchTargets : Activity
		{
			readonly AttackVortex attack;
			readonly int directionalOffset;
			readonly Target target;

			int rotationRate;

			public SwitchTargets(AttackVortex attack, in Target target)
			{
				this.target = target;
				this.attack = attack;

				directionalOffset = 360 / attack.Armaments.Count();
			}

			public override bool Tick(Actor self)
			{
				if (IsCanceling)
					return true;

				if (attack.IsTraitPaused)
					return false;

				if (!target.IsValidFor(self) || !attack.IsReachableTarget(target, false))
					return true;

				rotationRate += attack.Info.RotationRate;

				var d = 0;
				foreach (var armament in attack.Armaments)
				{
					var range = armament.Info.ModifiedRange.Length;
					var newTarget = self.CenterPosition + new WVec(range, 0, 0)
						.Rotate(WRot.FromFacing(rotationRate))
						.Rotate(WRot.FromYaw(WAngle.FromDegrees(d * directionalOffset)));

					var targetLocation = Target.FromPos(newTarget);
					armament.CheckFire(self, null, targetLocation);
					d++;
				}

				return false;
			}
		}
	}
}
