#region Copyright & License Information
/*
 * Copyright 2022 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenSA.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.OpenSA.Traits
{
	[TraitLocation(SystemActors.World)]
	class IndividualProductionQueueFromSelectionInfo : TraitInfo
	{
		public readonly string ProductionPaletteWidget = null;

		public override object Create(ActorInitializer init) { return new IndividualProductionQueueFromSelection(init.World, this); }
	}

	class IndividualProductionQueueFromSelection : INotifySelection
	{
		readonly World world;
		readonly Lazy<IndividualProductionPaletteWidget> paletteWidget;

		public IndividualProductionQueueFromSelection(World world, IndividualProductionQueueFromSelectionInfo info)
		{
			this.world = world;

			paletteWidget = Exts.Lazy(() => Ui.Root.GetOrNull(info.ProductionPaletteWidget) as IndividualProductionPaletteWidget);
		}

		void INotifySelection.SelectionChanged()
		{
			// Disable for spectators
			if (world.LocalPlayer == null)
				return;

			// Queue-per-actor
			var queue = world.Selection.Actors
				.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
				.SelectMany(a => a.TraitsImplementing<ProductionQueue>())
				.FirstOrDefault(q => q.Enabled);

			// Queue-per-player
			if (queue == null)
			{
				var types = world.Selection.Actors.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
					.SelectMany(a => a.TraitsImplementing<Production>().Where(p => !p.IsTraitDisabled))
					.SelectMany(t => t.Info.Produces);

				queue = world.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
					.FirstOrDefault(q => q.Enabled && types.Contains(q.Info.Type));
			}

			if (queue == null || !queue.BuildableItems().Any())
				return;

			if (paletteWidget.Value != null)
				paletteWidget.Value.CurrentQueue = queue;
		}
	}
}
