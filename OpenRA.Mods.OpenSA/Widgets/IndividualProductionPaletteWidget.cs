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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.OpenSA.Widgets
{
	public class ProductionIcon
	{
		public ActorInfo Actor;
		public string Name;
		public HotkeyReference Hotkey;
		public Sprite Sprite;
		public Sprite PressedSprite;
		public PaletteReference Palette;
		public PaletteReference ClockPalette;
		public PaletteReference DarkenPalette;
		public float2 Position;
		public List<ProductionItem> Queued;
		public ProductionQueue ProductionQueue;
	}

	public class IndividualProductionPaletteWidget : Widget
	{
		public enum ReadyTextStyleOptions { Solid, AlternatingColor, Blinking }
		public readonly ReadyTextStyleOptions ReadyTextStyle = ReadyTextStyleOptions.AlternatingColor;
		public readonly Color ReadyTextAltColor = Color.Gold;
		public readonly int Columns = 3;
		public readonly int2 IconSize = new int2(64, 48);
		public readonly int2 IconMargin = int2.Zero;
		public readonly int2 IconSpriteOffset = int2.Zero;

		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");
		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "PRODUCTION_TOOLTIP";

		// Note: LinterHotkeyNames assumes that these are disabled by default
		public readonly string HotkeyPrefix = null;
		public readonly int HotkeyCount = 0;
		public readonly HotkeyReference SelectProductionBuildingHotkey = new HotkeyReference();

		public readonly string ClockAnimation = "clock";
		public readonly string ClockSequence = "idle";
		public readonly string ClockPalette = "chrome";

		public readonly string NotBuildableAnimation = "clock";
		public readonly string NotBuildableSequence = "idle";
		public readonly string NotBuildablePalette = "chrome";

		public readonly string BuildableIconClickedSuffix = "-clicked";

		public readonly string OverlayFont = "TinyBold";
		public readonly string SymbolsFont = "Symbols";

		public readonly bool DrawTime = true;

		public readonly string ReadyText = "";
		public readonly string HoldText = "";

		public readonly string InfiniteSymbol = "\u221E";

		public int DisplayedIconCount { get; private set; }
		public int TotalIconCount { get; private set; }
		public event Action<int, int> OnIconCountChanged = (a, b) => { };

		public ProductionIcon TooltipIcon { get; private set; }
		public Func<ProductionIcon> GetTooltipIcon;
		public readonly World World;
		readonly ModData modData;
		readonly OrderManager orderManager;

		public int MinimumRows = 4;
		public int MaximumRows = int.MaxValue;

		public int IconRowOffset = 0;
		public int MaxIconRowOffset = int.MaxValue;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		ProductionQueue currentQueue;
		HotkeyReference[] hotkeys;

		public ProductionQueue CurrentQueue
		{
			get => currentQueue;
			set
			{
				currentQueue = value;
				if (currentQueue != null)
					UpdateCachedProductionIconOverlays();

				RefreshIcons();
			}
		}

		public override Rectangle EventBounds => eventBounds;
		Dictionary<Rectangle, ProductionIcon> icons = new Dictionary<Rectangle, ProductionIcon>();
		readonly Animation cantBuild;
		readonly Animation clock;
		Rectangle eventBounds = Rectangle.Empty;

		readonly WorldRenderer worldRenderer;

		SpriteFont overlayFont, symbolFont;
		float2 iconOffset, holdOffset, readyOffset, timeOffset, queuedOffset, infiniteOffset;

		Player cachedQueueOwner;
		IProductionIconOverlay[] productionOverlays;

		[CustomLintableHotkeyNames]
		public static IEnumerable<string> LinterHotkeyNames(MiniYamlNode widgetNode, Action<string> emitError)
		{
			var prefix = "";
			var prefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "HotkeyPrefix");
			if (prefixNode != null)
				prefix = prefixNode.Value.Value;

			var count = 0;
			var countNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "HotkeyCount");
			if (countNode != null)
				count = FieldLoader.GetValue<int>("HotkeyCount", countNode.Value.Value);

			if (count == 0)
				return Array.Empty<string>();

			if (string.IsNullOrEmpty(prefix))
				emitError($"{widgetNode.Location} must define HotkeyPrefix if HotkeyCount > 0.");

			return Exts.MakeArray(count, i => prefix + (i + 1).ToString("D2"));
		}

		[ObjectCreator.UseCtor]
		public IndividualProductionPaletteWidget(ModData modData, OrderManager orderManager, World world, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			this.orderManager = orderManager;
			World = world;
			this.worldRenderer = worldRenderer;
			GetTooltipIcon = () => TooltipIcon;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			cantBuild = new Animation(world, NotBuildableAnimation);
			cantBuild.PlayFetchIndex(NotBuildableSequence, () => 0);
			clock = new Animation(world, ClockAnimation);
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			hotkeys = Exts.MakeArray(HotkeyCount,
				i => modData.Hotkeys[HotkeyPrefix + (i + 1).ToString("D2")]);

			overlayFont = Game.Renderer.Fonts[OverlayFont];
			Game.Renderer.Fonts.TryGetValue(SymbolsFont, out symbolFont);

			iconOffset = 0.5f * IconSize.ToFloat2() + IconSpriteOffset;
			queuedOffset = new float2(4, 2);
			holdOffset = iconOffset - overlayFont.Measure(HoldText) / 2;
			readyOffset = iconOffset - overlayFont.Measure(ReadyText) / 2;

			if (ChromeMetrics.TryGet("InfiniteOffset", out infiniteOffset))
				infiniteOffset += queuedOffset;
			else
				infiniteOffset = queuedOffset;
		}

		public IEnumerable<ActorInfo> AllBuildables
		{
			get
			{
				if (CurrentQueue == null)
					return Enumerable.Empty<ActorInfo>();

				return CurrentQueue.AllItems().OrderBy(a => a.TraitInfo<BuildableInfo>().BuildPaletteOrder);
			}
		}

		public override void Tick()
		{
			TotalIconCount = AllBuildables.Count();

			if (CurrentQueue != null && !CurrentQueue.Actor.IsInWorld)
				CurrentQueue = null;

			if (CurrentQueue != null)
			{
				if (CurrentQueue.Actor.Owner != cachedQueueOwner)
					UpdateCachedProductionIconOverlays();

				RefreshIcons();
			}
		}

		public override void MouseEntered()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate,
					new WidgetArgs() { { "player", World.LocalPlayer }, { "getTooltipIcon", GetTooltipIcon }, { "world", World } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.RemoveTooltip();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			var icon = icons.Where(i => i.Key.Contains(mi.Location))
				.Select(i => i.Value).FirstOrDefault();

			if (mi.Event == MouseInputEvent.Move)
				TooltipIcon = icon;

			if (icon == null)
				return false;

			// Eat mouse-up events
			if (mi.Event != MouseInputEvent.Down)
				return true;

			return HandleEvent(icon, mi.Button, mi.Modifiers);
		}

		bool HandleLeftClick(ProductionItem item, ProductionIcon icon, int handleCount, Modifiers modifiers)
		{
			if (item != null && item.Paused)
			{
				// Resume a paused item
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.QueuedAudio, World.LocalPlayer.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(CurrentQueue.Info.QueuedTextNotification, World.LocalPlayer);

				World.IssueOrder(Order.PauseProduction(CurrentQueue.Actor, icon.Name, false));
				return true;
			}

			var buildable = CurrentQueue.BuildableItems().FirstOrDefault(a => a.Name == icon.Name);

			if (buildable != null)
			{
				// Queue a new item
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				var canQueue = CurrentQueue.CanQueue(buildable, out var notification, out var textNotification);

				if (!CurrentQueue.AllQueued().Any())
				{
					Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", notification, World.LocalPlayer.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(textNotification, World.LocalPlayer);
				}

				if (canQueue)
				{
					var queued = !modifiers.HasModifier(Modifiers.Ctrl);
					World.IssueOrder(Order.StartProduction(CurrentQueue.Actor, icon.Name, handleCount, queued));
					return true;
				}
			}

			return false;
		}

		bool HandleRightClick(ProductionItem item, ProductionIcon icon, int handleCount)
		{
			if (item == null)
				return false;

			Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);

			if (CurrentQueue.Info.DisallowPaused || item.Paused || item.Done || item.TotalCost == item.RemainingCost)
			{
				// Instantly cancel items that haven't started, have finished, or if the queue doesn't support pausing
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio, World.LocalPlayer.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(CurrentQueue.Info.CancelledTextNotification, World.LocalPlayer);

				World.IssueOrder(Order.CancelProduction(CurrentQueue.Actor, icon.Name, handleCount));
			}
			else
			{
				// Pause an existing item
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.OnHoldAudio, World.LocalPlayer.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(CurrentQueue.Info.OnHoldTextNotification, World.LocalPlayer);

				World.IssueOrder(Order.PauseProduction(CurrentQueue.Actor, icon.Name, true));
			}

			return true;
		}

		bool HandleMiddleClick(ProductionItem item, ProductionIcon icon, int handleCount)
		{
			if (item == null)
				return false;

			// Directly cancel, skipping "on-hold"
			Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
			Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Speech", CurrentQueue.Info.CancelledAudio, World.LocalPlayer.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(CurrentQueue.Info.CancelledTextNotification, World.LocalPlayer);

			World.IssueOrder(Order.CancelProduction(CurrentQueue.Actor, icon.Name, handleCount));

			return true;
		}

		bool HandleEvent(ProductionIcon icon, MouseButton btn, Modifiers modifiers)
		{
			var startCount = modifiers.HasModifier(Modifiers.Shift) ? 5 : 1;

			// PERF: avoid an unnecessary enumeration by casting back to its known type
			var cancelCount = modifiers.HasModifier(Modifiers.Ctrl) ? ((List<ProductionItem>)CurrentQueue.AllQueued()).Count : startCount;
			var item = icon.Queued.FirstOrDefault();
			var handled = btn == MouseButton.Left ? HandleLeftClick(item, icon, startCount, modifiers)
				: btn == MouseButton.Right ? HandleRightClick(item, icon, cancelCount)
				: btn == MouseButton.Middle ? HandleMiddleClick(item, icon, cancelCount)
				: false;

			if (!handled)
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickDisabledSound, null);

			return true;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Up || CurrentQueue == null)
				return false;

			if (SelectProductionBuildingHotkey.IsActivatedBy(e))
				return SelectProductionBuilding();

			var batchModifiers = e.Modifiers.HasModifier(Modifiers.Shift) ? Modifiers.Shift : Modifiers.None;

			// HACK: enable production if the shift key is pressed
			e.Modifiers &= ~Modifiers.Shift;
			var toBuild = icons.Values.FirstOrDefault(i => i.Hotkey != null && i.Hotkey.IsActivatedBy(e));
			return toBuild != null ? HandleEvent(toBuild, MouseButton.Left, batchModifiers) : false;
		}

		bool SelectProductionBuilding()
		{
			var viewport = worldRenderer.Viewport;
			var selection = World.Selection;

			if (CurrentQueue == null)
				return true;

			var facility = CurrentQueue.MostLikelyProducer().Actor;

			if (facility == null || facility.OccupiesSpace == null)
				return true;

			if (selection.Actors.Count() == 1 && selection.Contains(facility))
				viewport.Center(selection.Actors);
			else
				selection.Combine(World, new[] { facility }, false, true);

			Game.Sound.PlayNotification(World.Map.Rules, null, "Sounds", ClickSound, null);
			return true;
		}

		void UpdateCachedProductionIconOverlays()
		{
			cachedQueueOwner = CurrentQueue.Actor.Owner;
			productionOverlays = cachedQueueOwner.PlayerActor.TraitsImplementing<IProductionIconOverlay>().ToArray();
		}

		public void RefreshIcons()
		{
			icons = new Dictionary<Rectangle, ProductionIcon>();
			var producer = CurrentQueue != null ? CurrentQueue.MostLikelyProducer() : default;
			if (CurrentQueue == null || producer.Trait == null)
			{
				if (DisplayedIconCount != 0)
				{
					OnIconCountChanged(DisplayedIconCount, 0);
					DisplayedIconCount = 0;
				}

				return;
			}

			var oldIconCount = DisplayedIconCount;
			DisplayedIconCount = 0;

			var renderBounds = RenderBounds;
			var faction = producer.Trait.Faction;

			foreach (var item in AllBuildables.Skip(IconRowOffset * Columns).Take(MaxIconRowOffset * Columns))
			{
				var x = DisplayedIconCount % Columns;
				var y = DisplayedIconCount / Columns;
				var iconSize = new Rectangle(renderBounds.X + x * (IconSize.X + IconMargin.X), renderBounds.Y + y * (IconSize.Y + IconMargin.Y), IconSize.X, IconSize.Y);

				var renderSprites = item.TraitInfo<RenderSpritesInfo>();
				var icon = new Animation(World, renderSprites.GetImage(item, faction));
				var buildable = item.TraitInfo<BuildableInfo>();
				icon.Play(buildable.Icon);
				var pressedIcon = new Animation(World, renderSprites.GetImage(item, faction));
				pressedIcon.Play(buildable.Icon + BuildableIconClickedSuffix);

				var palette = buildable.IconPaletteIsPlayerPalette ? buildable.IconPalette + producer.Actor.Owner.InternalName : buildable.IconPalette;

				var productionIcon = new ProductionIcon()
				{
					Actor = item,
					Name = item.Name,
					Hotkey = DisplayedIconCount < HotkeyCount ? hotkeys[DisplayedIconCount] : null,
					Sprite = icon.Image,
					PressedSprite = pressedIcon.Image,
					Palette = worldRenderer.Palette(palette),
					ClockPalette = worldRenderer.Palette(ClockPalette),
					DarkenPalette = worldRenderer.Palette(NotBuildablePalette),
					Position = new float2(iconSize.Location),
					Queued = currentQueue.AllQueued().Where(a => a.Item == item.Name).ToList(),
					ProductionQueue = currentQueue
				};

				icons.Add(iconSize, productionIcon);
				DisplayedIconCount++;
			}

			eventBounds = icons.Keys.Union();

			if (oldIconCount != DisplayedIconCount)
				OnIconCountChanged(oldIconCount, DisplayedIconCount);
		}

		public override void Draw()
		{
			timeOffset = iconOffset - overlayFont.Measure(WidgetUtils.FormatTime(0, World.Timestep)) / 2;

			if (CurrentQueue == null)
				return;

			var buildableItems = CurrentQueue.BuildableItems();

			// Icons
			Game.Renderer.EnableAntialiasingFilter();
			foreach (var icon in icons.Values)
			{
				WidgetUtils.DrawSpriteCentered(icon.Sprite, icon.Palette, icon.Position + iconOffset);

				// Draw the ProductionIconOverlay's sprites
				foreach (var productionOverlay in productionOverlays.Where(p => p.IsOverlayActive(icon.Actor)))
					WidgetUtils.DrawSpriteCentered(productionOverlay.Sprite, worldRenderer.Palette(productionOverlay.Palette), icon.Position + iconOffset + productionOverlay.Offset(IconSize));

				// Build progress
				if (icon.Queued.Count > 0)
				{
					WidgetUtils.DrawSpriteCentered(icon.PressedSprite, icon.Palette, icon.Position + iconOffset);

					var first = icon.Queued[0];
					clock.PlayFetchIndex(ClockSequence,
						() => (first.TotalTime - first.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / first.TotalTime);
					clock.Tick();

					WidgetUtils.DrawSpriteCentered(clock.Image, icon.ClockPalette, icon.Position + iconOffset);
				}
				else if (!buildableItems.Any(a => a.Name == icon.Name))
					WidgetUtils.DrawSpriteCentered(cantBuild.Image, icon.DarkenPalette, icon.Position + iconOffset);
			}

			Game.Renderer.DisableAntialiasingFilter();

			// Overlays
			foreach (var icon in icons.Values)
			{
				var total = icon.Queued.Count;
				if (total > 0)
				{
					var first = icon.Queued[0];
					var waiting = !CurrentQueue.IsProducing(first) && !first.Done;
					if (first.Done)
					{
						if (ReadyTextStyle == ReadyTextStyleOptions.Solid || orderManager.LocalFrameNumber * worldRenderer.World.Timestep / 360 % 2 == 0)
							overlayFont.DrawTextWithContrast(ReadyText, icon.Position + readyOffset, Color.White, Color.Black, 1);
						else if (ReadyTextStyle == ReadyTextStyleOptions.AlternatingColor)
							overlayFont.DrawTextWithContrast(ReadyText, icon.Position + readyOffset, ReadyTextAltColor, Color.Black, 1);
					}
					else if (first.Paused)
						overlayFont.DrawTextWithContrast(HoldText,
							icon.Position + holdOffset,
							Color.White, Color.Black, 1);
					else if (!waiting && DrawTime)
						overlayFont.DrawTextWithContrast(WidgetUtils.FormatTime(first.Queue.RemainingTimeActual(first), World.Timestep),
							icon.Position + timeOffset,
							Color.White, Color.Black, 1);

					if (first.Infinite && symbolFont != null)
						symbolFont.DrawTextWithContrast(InfiniteSymbol,
							icon.Position + infiniteOffset,
							Color.White, Color.Black, 1);
					else if (total > 1 || waiting)
						overlayFont.DrawTextWithContrast(total.ToString(),
							icon.Position + queuedOffset,
							Color.White, Color.Black, 1);
				}
			}
		}

		public override string GetCursor(int2 pos)
		{
			var icon = icons.Where(i => i.Key.Contains(pos))
				.Select(i => i.Value).FirstOrDefault();

			return icon != null ? base.GetCursor(pos) : null;
		}
	}
}