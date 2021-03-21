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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Conditions
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
