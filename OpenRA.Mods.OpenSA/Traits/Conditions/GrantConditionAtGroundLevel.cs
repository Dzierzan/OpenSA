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

using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Conditions
{
	[Desc("Grants a condition above a certain altitude.")]
	public class GrantConditionAtGroundLevelInfo : TraitInfo
	{
		[GrantedConditionReference]
		[FieldLoader.Require]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionAtGroundLevel(this); }
	}

	public class GrantConditionAtGroundLevel : ITick, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly GrantConditionAtGroundLevelInfo info;

		int token = Actor.InvalidConditionToken;

		public GrantConditionAtGroundLevel(GrantConditionAtGroundLevelInfo info)
		{
			this.info = info;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			if (altitude == WDist.Zero)
				token = self.GrantCondition(info.Condition);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			if (altitude == WDist.Zero)
			{
				if (token == Actor.InvalidConditionToken)
					token = self.GrantCondition(info.Condition);
			}
			else
			{
				if (token != Actor.InvalidConditionToken)
					token = self.RevokeCondition(token);
			}
		}
	}
}
