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

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenSA.Traits.World;

namespace OpenRA.Mods.OpenSA.Traits.Conditions
{
	public class GrantConditionOnWaspLayerInfo : GrantConditionOnLayerInfo
	{
		public override object Create(ActorInitializer init) { return new GrantConditionOnJumpjetLayer(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var mobileInfo = ai.TraitInfoOrDefault<MobileInfo>();
			if (mobileInfo == null || mobileInfo.LocomotorInfo is not WaspLocomotorInfo)
				throw new YamlException("GrantConditionOnWaspLayer requires Mobile to be linked to a WaspLocomotor!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class GrantConditionOnJumpjetLayer : GrantConditionOnLayer<GrantConditionOnWaspLayerInfo>, INotifyFinishedMoving
	{
		bool airborne;

		public GrantConditionOnJumpjetLayer(GrantConditionOnWaspLayerInfo info)
			: base(info, CustomMovementLayerType.Jumpjet) { }

		void INotifyFinishedMoving.FinishedMoving(Actor self, byte oldLayer, byte newLayer)
		{
			if (airborne && oldLayer != ValidLayerType && newLayer != ValidLayerType)
				UpdateConditions(self, oldLayer, newLayer);
		}

		protected override void UpdateConditions(Actor self, byte oldLayer, byte newLayer)
		{
			if (!airborne && newLayer == ValidLayerType && oldLayer != ValidLayerType && conditionToken == Actor.InvalidConditionToken)
			{
				conditionToken = self.GrantCondition(Info.Condition);
				airborne = true;
			}

			// By the time the condition is meant to be revoked, the 'oldLayer' is already no longer the Jumpjet layer, either
			if (airborne && newLayer != ValidLayerType && oldLayer != ValidLayerType && conditionToken != Actor.InvalidConditionToken)
			{
				conditionToken = self.RevokeCondition(conditionToken);
				airborne = false;
			}
		}
	}
}
