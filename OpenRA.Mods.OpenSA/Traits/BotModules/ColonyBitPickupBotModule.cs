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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenSA.Traits.Colony;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.BotModules
{
	[Desc("Put this on the Player actor. Manages colony bit collection.")]
	public class ColonyBitPickupBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that should not start hunting for colony bits.")]
		public readonly HashSet<string> ExcludedUnitTypes = new();

		[Desc("Only these actor types should start hunting for colony bits.")]
		public readonly HashSet<string> IncludedUnitTypes = new();

		[Desc("Interval (in ticks) between giving out orders to idle units.")]
		public readonly int ScanInterval = 50;

		[Desc("Only move this far away from current position. Disabled if set to zero.")]
		public readonly int MaxProximityRadius = 20;

		[Desc("Should visibility (Shroud, Fog, Cloak, etc) be considered when searching for colony bits?")]
		public readonly bool CheckTargetsForVisibility = true;

		public override object Create(ActorInitializer init) { return new ColonyBitPickupBotModule(init.Self, this); }
	}

	public class ColonyBitPickupBotModule : ConditionalTrait<ColonyBitPickupBotModuleInfo>, IBotTick
	{
		readonly OpenRA.World world;
		readonly OpenRA.Player player;

		readonly int maxProximity;

		int scanForBitsTicks;
		SquadManagerBotModule squadManagerBotModule;

		public ColonyBitPickupBotModule(Actor self, ColonyBitPickupBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			maxProximity = Info.MaxProximityRadius > 0 ? info.MaxProximityRadius : world.Map.Grid.MaximumTileSearchRange;
		}

		protected override void TraitEnabled(Actor self)
		{
			scanForBitsTicks = Info.ScanInterval;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--scanForBitsTicks > 0)
				return;

			scanForBitsTicks = Info.ScanInterval;

			var bits = world.ActorsHavingTrait<ColonyBit>().ToList();
			if (!bits.Any())
				return;

			if (Info.CheckTargetsForVisibility)
				bits.RemoveAll(c => !c.CanBeViewedByPlayer(player));

			var units = world.ActorsHavingTrait<Mobile>().Where(a => a.Owner == player && a.IsIdle
				&& (Info.IncludedUnitTypes.Contains(a.Info.Name) || (!Info.IncludedUnitTypes.Any() && !Info.ExcludedUnitTypes.Contains(a.Info.Name)))).ToList();

			if (!units.Any())
				return;

			foreach (var bit in bits)
			{
				var bitCollector = units.ClosestTo(bit);
				if (bitCollector == null)
					continue;

				if ((bit.Location - bitCollector.Location).Length > maxProximity)
					continue;

				units.Remove(bitCollector);

				if (squadManagerBotModule == null)
					squadManagerBotModule = bot.Player.PlayerActor.TraitsImplementing<SquadManagerBotModule>().FirstEnabledTraitOrDefault();

				if (squadManagerBotModule != null)
				{
					// You got ONE job!
					var squad = squadManagerBotModule.Squads.FirstOrDefault(s => s.Units.Contains(bitCollector));
					if (squad != null)
						squad.Units.Remove(bitCollector);
				}

				var target = Target.FromCell(world, bit.Location);
				AIUtils.BotDebug($"{player.PlayerName}: Ordering unit {bitCollector} to {target} for colony bit pick up.");
				bot.QueueOrder(new Order("Stop", bitCollector, false));
				bot.QueueOrder(new Order("Move", bitCollector, target, false));
			}
		}
	}
}
