using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Swarm_Assault.Traits.Conditions
{
    public class GrantConditionWhileProducingInfo : ITraitInfo, Requires<ProductionQueueInfo>
    {
        [FieldLoader.Require] [GrantedConditionReference]
        public readonly string Condition = null;

        public object Create(ActorInitializer init)
        {
            return new GrantConditionWhileProducing(init.Self, this);
        }
    }

    public class GrantConditionWhileProducing : ITick, INotifyCreated
    {
        private readonly GrantConditionWhileProducingInfo info;
        private ConditionManager conditionManager;
        private IEnumerable<ProductionQueue> productionQueues;

        int token = ConditionManager.InvalidConditionToken;

        public GrantConditionWhileProducing(Actor self, GrantConditionWhileProducingInfo info)
        {
            this.info = info;
            productionQueues = self.TraitsImplementing<ProductionQueue>();
        }

        void INotifyCreated.Created(Actor self)
        {
            conditionManager = self.TraitOrDefault<ConditionManager>();
        }

        void ITick.Tick(Actor self)
        {
            var hasCondition = token != ConditionManager.InvalidConditionToken;
            var isProducing = productionQueues.Any(queue => queue.AllQueued().Any());

            if (!hasCondition && isProducing)
                token = conditionManager.GrantCondition(self, info.Condition);
            else if (hasCondition && !isProducing)
                token = conditionManager.RevokeCondition(self, token);
        }
    }
}