using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits.Conditions
{
	public class GrantConditionWhileProducingInfo : TraitInfo, Requires<ProductionQueueInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init)
		{
			return new GrantConditionWhileProducing(init.Self, this);
		}
	}

	public class GrantConditionWhileProducing : ITick
	{
		readonly GrantConditionWhileProducingInfo info;
		readonly IEnumerable<ProductionQueue> productionQueues;

		int token = Actor.InvalidConditionToken;

		public GrantConditionWhileProducing(Actor self, GrantConditionWhileProducingInfo info)
		{
			this.info = info;
			productionQueues = self.TraitsImplementing<ProductionQueue>();
		}

		void ITick.Tick(Actor self)
		{
			var hasCondition = token != Actor.InvalidConditionToken;
			var isProducing = productionQueues.Any(queue => queue.AllQueued().Any());

			if (!hasCondition && isProducing)
				token = self.GrantCondition(info.Condition);
			else if (hasCondition && !isProducing)
				token = self.RevokeCondition(token);
		}
	}
}
