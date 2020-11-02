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

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.OpenSA.Widgets.Logic
{
	public class SimpleProductionTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SimpleProductionTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, Player player, Func<ProductionIcon> getTooltipIcon)
		{
			var world = player.World;
			var mapRules = world.Map.Rules;

			widget.IsVisible = () => getTooltipIcon() != null && getTooltipIcon().Actor != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var requiresLabel = widget.Get<LabelWidget>("REQUIRES");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var timeIcon = widget.Get<ImageWidget>("TIME_ICON");
			var descLabel = widget.Get<LabelWidget>("DESC");

			var iconMargin = timeIcon.Bounds.X;

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			var formatBuildTime = new CachedTransform<int, string>(time => WidgetUtils.FormatTime(time, world.Timestep));
			var requiresFormat = requiresLabel.Text;

			ActorInfo lastActor = null;
			Hotkey lastHotkey = Hotkey.Invalid;
			var descLabelY = descLabel.Bounds.Y;
			var descLabelPadding = descLabel.Bounds.Height;

			tooltipContainer.BeforeRender = () =>
			{
				var tooltipIcon = getTooltipIcon();
				if (tooltipIcon == null)
					return;

				var actor = tooltipIcon.Actor;
				if (actor == null)
					return;

				var hotkey = tooltipIcon.Hotkey != null ? tooltipIcon.Hotkey.GetValue() : Hotkey.Invalid;
				if (actor == lastActor && hotkey == lastHotkey)
					return;

				var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				var name = tooltip != null ? tooltip.Name : actor.Name;
				var buildable = actor.TraitInfo<BuildableInfo>();

				var cost = 0;
				if (tooltipIcon.ProductionQueue != null)
					cost = tooltipIcon.ProductionQueue.GetProductionCost(actor);
				else
				{
					var valued = actor.TraitInfoOrDefault<ValuedInfo>();
					if (valued != null)
						cost = valued.Cost;
				}

				nameLabel.Text = name;

				var nameSize = font.Measure(name);
				var hotkeyWidth = 0;
				hotkeyLabel.Visible = hotkey.IsValid();

				if (hotkeyLabel.Visible)
				{
					var hotkeyText = "({0})".F(hotkey.DisplayString());

					hotkeyWidth = font.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X;
					hotkeyLabel.Text = hotkeyText;
					hotkeyLabel.Bounds.X = nameSize.X + 2 * nameLabel.Bounds.X;
				}

				var prereqs = buildable.Prerequisites.Select(a => ActorName(mapRules, a))
					.Where(s => !s.StartsWith("~", StringComparison.Ordinal) && !s.StartsWith("!", StringComparison.Ordinal));

				var requiresSize = int2.Zero;
				if (prereqs.Any())
				{
					requiresLabel.Text = requiresFormat.F(prereqs.JoinWith(", "));
					requiresSize = requiresFont.Measure(requiresLabel.Text);
					requiresLabel.Visible = true;
					descLabel.Bounds.Y = descLabelY + requiresLabel.Bounds.Height;
				}
				else
				{
					requiresLabel.Visible = false;
					descLabel.Bounds.Y = descLabelY;
				}

				var buildTime = tooltipIcon.ProductionQueue == null ? 0 : tooltipIcon.ProductionQueue.GetBuildTime(actor, buildable);
				var timeModifier = tooltipIcon.ProductionQueue.Info.LowPowerModifier;

				timeLabel.Text = formatBuildTime.Update(buildTime * timeModifier / 100);
				timeLabel.TextColor = tooltipIcon.ProductionQueue.Info.LowPowerModifier > 100 ? Color.Red : Color.White;
				var timeSize = font.Measure(timeLabel.Text);

				descLabel.Text = buildable.Description.Replace("\\n", "\n");
				var descSize = descFont.Measure(descLabel.Text);
				descLabel.Bounds.Width = descSize.X;
				descLabel.Bounds.Height = descSize.Y + descLabelPadding;

				var leftWidth = new[] { nameSize.X + hotkeyWidth, requiresSize.X, descSize.X }.Aggregate(Math.Max);
				var rightWidth = new[] { timeSize.X }.Aggregate(Math.Max);

				timeIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
				timeLabel.Bounds.X = timeIcon.Bounds.Right + iconMargin;
				widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + timeIcon.Bounds.Width + iconMargin;

				// Set the bottom margin to match the left margin
				var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;

				// Set the bottom margin to match the top margin
				var rightHeight = timeIcon.Bounds.Bottom;

				widget.Bounds.Height = Math.Max(leftHeight, rightHeight);

				lastActor = actor;
				lastHotkey = hotkey;
			};
		}

		static string ActorName(Ruleset rules, string a)
		{
			if (rules.Actors.TryGetValue(a.ToLowerInvariant(), out var ai))
			{
				var actorTooltip = ai.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				if (actorTooltip != null)
					return actorTooltip.Name;
			}

			return a;
		}
	}
}
