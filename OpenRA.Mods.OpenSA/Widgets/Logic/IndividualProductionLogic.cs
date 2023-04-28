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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.OpenSA.Widgets.Logic
{
	public class IndividualProductionLogic : ChromeLogic
	{
		readonly IndividualProductionPaletteWidget palette;
		readonly World world;

		void SetupProductionGroupButton(ProductionTypeButtonWidget button)
		{
			if (button == null)
				return;

			// Classic production queues are initialized at game start, and then never change.
			var queues = world.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
				.Where(q => (q.Info.Group ?? q.Info.Type) == button.ProductionGroup)
				.ToArray();

			button.IsDisabled = () => !queues.Any(q => q.BuildableItems().Any());
			button.IsHighlighted = () => queues.Contains(palette.CurrentQueue);

			var chromeName = button.ProductionGroup.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? chromeName + "-disabled" :
				queues.Any(q => q.AllQueued().Any(i => i.Done)) ? chromeName + "-alert" : chromeName;
		}

		[ObjectCreator.UseCtor]
		public IndividualProductionLogic(Widget widget, World world)
		{
			this.world = world;
			palette = widget.Get<IndividualProductionPaletteWidget>("PRODUCTION_PALETTE");

			var background = widget.GetOrNull("PALETTE_BACKGROUND");
			var foreground = widget.GetOrNull("PALETTE_FOREGROUND");
			if (background != null || foreground != null)
			{
				Widget backgroundTemplate = null;
				Widget backgroundBottom = null;
				Widget foregroundTemplate = null;

				if (background != null)
				{
					backgroundTemplate = background.Get("ROW_TEMPLATE");
					backgroundBottom = background.GetOrNull("BOTTOM_CAP");
				}

				if (foreground != null)
					foregroundTemplate = foreground.Get("ROW_TEMPLATE");

				Action<int, int> updateBackground = (_, icons) =>
				{
					var rows = Math.Max(palette.MinimumRows, (icons + palette.Columns - 1) / palette.Columns);
					rows = Math.Min(rows, palette.MaximumRows);

					if (background != null)
					{
						background.RemoveChildren();

						var rowHeight = backgroundTemplate.Bounds.Height;
						for (var i = 0; i < rows; i++)
						{
							var row = backgroundTemplate.Clone();
							row.Bounds.Y = i * rowHeight;
							background.AddChild(row);
						}

						if (backgroundBottom == null)
							return;

						backgroundBottom.Bounds.Y = rows * rowHeight;
						background.AddChild(backgroundBottom);
					}

					if (foreground != null)
					{
						foreground.RemoveChildren();

						var rowHeight = foregroundTemplate.Bounds.Height;
						for (var i = 0; i < rows; i++)
						{
							var row = foregroundTemplate.Clone();
							row.Bounds.Y = i * rowHeight;
							foreground.AddChild(row);
						}
					}
				};

				palette.OnIconCountChanged += updateBackground;

				// Set the initial palette state
				updateBackground(0, 0);
			}

			var typesContainer = widget.Get("PRODUCTION_TYPES");
			foreach (var i in typesContainer.Children)
				SetupProductionGroupButton(i as ProductionTypeButtonWidget);

			var ticker = widget.Get<LogicTickerWidget>("PRODUCTION_TICKER");
			ticker.OnTick = () =>
			{
				if (palette.CurrentQueue == null || palette.DisplayedIconCount == 0)
				{
					// Select the first active tab
					foreach (var b in typesContainer.Children)
					{
						if (!(b is ProductionTypeButtonWidget button) || button.IsDisabled())
							continue;

						button.OnClick();
						break;
					}
				}
			};
		}
	}
}
