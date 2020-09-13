using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	public class GrantConditionOnWaspLayerInfo : GrantConditionOnLayerInfo
	{
		public override object Create(ActorInitializer init) { return new GrantConditionOnJumpjetLayer(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var mobileInfo = ai.TraitInfoOrDefault<MobileInfo>();
			if (mobileInfo == null || !(mobileInfo.LocomotorInfo is WaspLocomotorInfo))
				throw new YamlException("GrantConditionOnJumpjetLayer requires Mobile to be linked to a JumpjetLocomotor!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class GrantConditionOnJumpjetLayer : GrantConditionOnLayer<GrantConditionOnWaspLayerInfo>, INotifyFinishedMoving
	{
		bool airborne;

		public GrantConditionOnJumpjetLayer(Actor self, GrantConditionOnWaspLayerInfo info)
			: base(self, info, CustomMovementLayerType.Jumpjet) { }

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
