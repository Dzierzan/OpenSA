#region Copyright & License Information
/*
 * Copyright 2019-2020 The OpenSA Developers (see CREDITS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Put this on the Player actor. Manages colony healing by halting production.")]
	public class HealColonyBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Act upon this damage taken.")]
		public readonly DamageState DamageState = DamageState.Heavy;

		public override object Create(ActorInitializer init) { return new HealColonyBotModule(init.Self, this); }
	}

	public class HealColonyBotModule : ConditionalTrait<HealColonyBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;

		public HealColonyBotModule(Actor self, HealColonyBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		void IBotTick.BotTick(IBot bot)
		{
			var colonies = AIUtils.GetActorsWithTrait<Colony>(world).Where(c => c.Owner == player).ToArray();
			foreach (var colony in colonies)
			{
				var health = colony.Trait<IHealth>();
				if (health.DamageState == Info.DamageState)
				{
					var queue = colony.TraitOrDefault<ProductionQueue>(); // Turrets don't produce
					if (queue == null)
						continue;

					foreach (var current in queue.AllQueued())
					{
						bot.QueueOrder(Order.CancelProduction(queue.Actor, current.Item, 1));
						AIUtils.BotDebug("{0}: Stopping production of {1} at {2} to heal.".F(player.PlayerName, current.Item, colony));
					}
				}
			}
		}
	}
}
