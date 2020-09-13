using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.OpenSA.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.SA.Widgets.Logic
{
	public enum ObserverStatsPanel { None, Basic, Economy, Production, Combat, Army, Graph, ArmyGraph }

	[ChromeLogicArgsHotkeys("StatisticsBasicKey", "StatisticsEconomyKey", "StatisticsProductionKey", "StatisticsCombatKey", "StatisticsArmyKey")]
	public class SwarmAssaultObserverStatsLogic : ChromeLogic
	{
		readonly ContainerWidget basicStatsHeaders;
		readonly ContainerWidget economyStatsHeaders;
		readonly ContainerWidget productionStatsHeaders;
		readonly ContainerWidget combatStatsHeaders;
		readonly ContainerWidget armyHeaders;
		readonly ScrollPanelWidget playerStatsPanel;
		readonly ScrollItemWidget basicPlayerTemplate;
		readonly ScrollItemWidget economyPlayerTemplate;
		readonly ScrollItemWidget productionPlayerTemplate;
		readonly ScrollItemWidget armyPlayerTemplate;
		readonly ScrollItemWidget combatPlayerTemplate;
		readonly ScrollItemWidget teamTemplate;
		readonly IEnumerable<Player> players;
		readonly IOrderedEnumerable<IGrouping<int, Player>> teams;
		readonly bool hasTeams;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		readonly string clickSound = ChromeMetrics.Get<string>("ClickSound");
		ObserverStatsPanel activePanel;

		[ObjectCreator.UseCtor]
		public SwarmAssaultObserverStatsLogic(World world, ModData modData, WorldRenderer worldRenderer, Widget widget, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			MiniYaml yaml;
			string[] keyNames = Enum.GetNames(typeof(ObserverStatsPanel));
			var statsHotkeys = new HotkeyReference[keyNames.Length];
			for (var i = 0; i < keyNames.Length; i++)
				statsHotkeys[i] = logicArgs.TryGetValue("Statistics" + keyNames[i] + "Key", out yaml) ? modData.Hotkeys[yaml.Value] : new HotkeyReference();

			players = world.Players.Where(p => !p.NonCombatant);
			teams = players.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team).OrderBy(g => g.Key);
			hasTeams = !(teams.Count() == 1 && teams.First().Key == 0);

			basicStatsHeaders = widget.Get<ContainerWidget>("BASIC_STATS_HEADERS");
			economyStatsHeaders = widget.Get<ContainerWidget>("ECONOMY_STATS_HEADERS");
			productionStatsHeaders = widget.Get<ContainerWidget>("PRODUCTION_STATS_HEADERS");
			armyHeaders = widget.Get<ContainerWidget>("ARMY_HEADERS");
			combatStatsHeaders = widget.Get<ContainerWidget>("COMBAT_STATS_HEADERS");

			playerStatsPanel = widget.Get<ScrollPanelWidget>("PLAYER_STATS_PANEL");
			playerStatsPanel.Layout = new GridLayout(playerStatsPanel);
			playerStatsPanel.IgnoreMouseOver = true;

			if (ShowScrollBar)
			{
				playerStatsPanel.ScrollBar = ScrollBar.Left;

				AdjustHeader(basicStatsHeaders);
				AdjustHeader(economyStatsHeaders);
				AdjustHeader(productionStatsHeaders);
				AdjustHeader(combatStatsHeaders);
				AdjustHeader(armyHeaders);
			}

			basicPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("BASIC_PLAYER_TEMPLATE");
			economyPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("ECONOMY_PLAYER_TEMPLATE");
			productionPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("PRODUCTION_PLAYER_TEMPLATE");
			armyPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("ARMY_PLAYER_TEMPLATE");
			combatPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("COMBAT_PLAYER_TEMPLATE");

			teamTemplate = playerStatsPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");

			var statsDropDown = widget.Get<DropDownButtonWidget>("STATS_DROPDOWN");
			Func<string, ObserverStatsPanel, ScrollItemWidget, Action, StatsDropDownOption> createStatsOption = (title, panel, template, a) =>
			{
				return new StatsDropDownOption
				{
					Title = title,
					IsSelected = () => activePanel == panel,
					OnClick = () =>
					{
						ClearStats();
						playerStatsPanel.Visible = true;
						statsDropDown.GetText = () => title;
						activePanel = panel;
						if (template != null)
							AdjustStatisticsPanel(template);

						a();
						Ui.ResetTooltips();
					}
				};
			};

			var statsDropDownOptions = new StatsDropDownOption[]
			{
				new StatsDropDownOption
				{
					Title = "Information: None",
					IsSelected = () => activePanel == ObserverStatsPanel.None,
					OnClick = () =>
					{
						statsDropDown.GetText = () => "Information: None";
						playerStatsPanel.Visible = false;
						ClearStats();
						activePanel = ObserverStatsPanel.None;
					}
				},
				createStatsOption("Basic", ObserverStatsPanel.Basic, basicPlayerTemplate, () => DisplayStats(BasicStats)),
				createStatsOption("Economy", ObserverStatsPanel.Economy, economyPlayerTemplate, () => DisplayStats(EconomyStats)),
				createStatsOption("Production", ObserverStatsPanel.Production, productionPlayerTemplate, () => DisplayStats(ProductionStats)),
				createStatsOption("Combat", ObserverStatsPanel.Combat, combatPlayerTemplate, () => DisplayStats(CombatStats)),
				createStatsOption("Army", ObserverStatsPanel.Army, armyPlayerTemplate, () => DisplayStats(ArmyStats)),
			};

			Func<StatsDropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
				item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
				return item;
			};

			var statsDropDownPanelTemplate = logicArgs.TryGetValue("StatsDropDownPanelTemplate", out yaml) ? yaml.Value : "LABEL_DROPDOWN_TEMPLATE";

			statsDropDown.OnMouseDown = _ => statsDropDown.ShowDropDown(statsDropDownPanelTemplate, 230, statsDropDownOptions, setupItem);
			statsDropDownOptions[0].OnClick();

			var keyListener = statsDropDown.Get<LogicKeyListenerWidget>("STATS_DROPDOWN_KEYHANDLER");
			keyListener.AddHandler(e =>
			{
				if (e.Event == KeyInputEvent.Down && !e.IsRepeat)
				{
					for (var i = 0; i < statsHotkeys.Length; i++)
					{
						if (statsHotkeys[i].IsActivatedBy(e))
						{
							Game.Sound.PlayNotification(modData.DefaultRules, null, "Sounds", clickSound, null);
							statsDropDownOptions[i].OnClick();
							return true;
						}
					}
				}

				return false;
			});

			if (logicArgs.TryGetValue("ClickSound", out yaml))
				clickSound = yaml.Value;
		}

		void ClearStats()
		{
			playerStatsPanel.Children.Clear();
			basicStatsHeaders.Visible = false;
			economyStatsHeaders.Visible = false;
			productionStatsHeaders.Visible = false;
			armyHeaders.Visible = false;
			combatStatsHeaders.Visible = false;
		}

		void DisplayStats(Func<Player, ScrollItemWidget> createItem)
		{
			foreach (var team in teams)
			{
				if (hasTeams)
				{
					var tt = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
					tt.IgnoreMouseOver = true;

					var teamLabel = tt.Get<LabelWidget>("TEAM");
					var teamText = team.Key == 0 ? "No Team" : "Team " + team.Key;
					teamLabel.GetText = () => teamText;
					tt.Bounds.Width = teamLabel.Bounds.Width = Game.Renderer.Fonts[tt.Font].Measure(tt.Get<LabelWidget>("TEAM").GetText()).X;

					var colorBlockWidget = tt.Get<ColorBlockWidget>("TEAM_COLOR");
					var scrollBarOffset = playerStatsPanel.ScrollBar != ScrollBar.Hidden
						? playerStatsPanel.ScrollbarWidth
						: 0;
					var boundsWidth = tt.Parent.Bounds.Width - scrollBarOffset;
					colorBlockWidget.Bounds.Width = boundsWidth - 200;

					var gradient = tt.Get<GradientColorBlockWidget>("TEAM_GRADIENT");
					gradient.Bounds.X = boundsWidth - 200;

					playerStatsPanel.AddChild(tt);
				}

				foreach (var p in team)
				{
					var player = p;
					playerStatsPanel.AddChild(createItem(player));
				}
			}
		}

		ScrollItemWidget CombatStats(Player player)
		{
			combatStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(combatPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return template;

			var unitsKilledText = new CachedTransform<int, string>(i => i.ToString());
			template.Get<LabelWidget>("UNITS_KILLED").GetText = () => unitsKilledText.Update(stats.UnitsKilled);

			var unitsDeadText = new CachedTransform<int, string>(i => i.ToString());
			template.Get<LabelWidget>("UNITS_DEAD").GetText = () => unitsDeadText.Update(stats.UnitsDead);

			return template;
		}

		ScrollItemWidget ProductionStats(Player player)
		{
			productionStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(productionPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			template.Get<ObserverProductionIconsWidget>("PRODUCTION_ICONS").GetPlayer = () => player;
			template.IgnoreChildMouseOver = false;

			return template;
		}

		ScrollItemWidget ArmyStats(Player player)
		{
			armyHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(armyPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			template.Get<ObserverArmyIconsWidget>("ARMY_ICONS").GetPlayer = () => player;
			template.IgnoreChildMouseOver = false;

			return template;
		}

		ScrollItemWidget EconomyStats(Player player)
		{
			economyStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(economyPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var res = player.PlayerActor.Trait<PlayerResources>();
			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return template;

			var colonies = template.Get<LabelWidget>("COLONIES");
			colonies.GetText = () => world.ActorsHavingTrait<Colony>().Count(a => a.Owner == player && !a.IsDead).ToString();

			return template;
		}

		ScrollItemWidget BasicStats(Player player)
		{
			basicStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(basicPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return template;

			var killsText = new CachedTransform<int, string>(i => i.ToString());
			template.Get<LabelWidget>("KILLS").GetText = () => killsText.Update(stats.UnitsKilled + stats.BuildingsKilled);

			var deathsText = new CachedTransform<int, string>(i => i.ToString());
			template.Get<LabelWidget>("DEATHS").GetText = () => deathsText.Update(stats.UnitsDead + stats.BuildingsDead);

			var experienceText = new CachedTransform<int, string>(i => i.ToString());
			template.Get<LabelWidget>("EXPERIENCE").GetText = () => experienceText.Update(stats.Experience);

			var actionsText = new CachedTransform<double, string>(d => AverageOrdersPerMinute(d));
			template.Get<LabelWidget>("ACTIONS_MIN").GetText = () => actionsText.Update(stats.OrderCount);

			return template;
		}

		void SetupPlayerColor(Player player, ScrollItemWidget template, ColorBlockWidget colorBlockWidget, GradientColorBlockWidget gradientColorBlockWidget)
		{
			var color = Color.FromArgb(128, player.Color.R, player.Color.G, player.Color.B);
			var hoverColor = Color.FromArgb(192, player.Color.R, player.Color.G, player.Color.B);

			var isMouseOver = new CachedTransform<Widget, bool>(w => w == template || template.Children.Contains(w));

			colorBlockWidget.GetColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : color;

			gradientColorBlockWidget.GetTopLeftColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : color;
			gradientColorBlockWidget.GetBottomLeftColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : color;
			gradientColorBlockWidget.GetTopRightColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : Color.Transparent;
			gradientColorBlockWidget.GetBottomRightColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : Color.Transparent;
		}

		ScrollItemWidget SetupPlayerScrollItemWidget(ScrollItemWidget template, Player player)
		{
			return ScrollItemWidget.Setup(template, () => false, () =>
			{
				var playerBase = world.ActorsHavingTrait<BaseBuilding>().FirstOrDefault(a => !a.IsDead && a.Owner == player);
				if (playerBase != null)
					worldRenderer.Viewport.Center(playerBase.CenterPosition);
			});
		}

		void AdjustStatisticsPanel(Widget itemTemplate)
		{
			var height = playerStatsPanel.Bounds.Height;

			var scrollbarWidth = playerStatsPanel.ScrollBar != ScrollBar.Hidden ? playerStatsPanel.ScrollbarWidth : 0;
			playerStatsPanel.Bounds.Width = itemTemplate.Bounds.Width + scrollbarWidth;

			if (playerStatsPanel.Bounds.Height < height)
				playerStatsPanel.ScrollToTop();
		}

		void AdjustHeader(ContainerWidget headerTemplate)
		{
			var offset = playerStatsPanel.ScrollbarWidth;

			headerTemplate.Get<ColorBlockWidget>("HEADER_COLOR").Bounds.Width += offset;
			headerTemplate.Get<GradientColorBlockWidget>("HEADER_GRADIENT").Bounds.X += offset;

			foreach (var headerLabel in headerTemplate.Children.OfType<LabelWidget>())
				headerLabel.Bounds.X += offset;
		}

		static void AddPlayerFlagAndName(ScrollItemWidget template, Player player)
		{
			var flag = template.Get<ImageWidget>("FLAG");
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => player.Faction.InternalName;

			var client = player.World.LobbyInfo.ClientWithIndex(player.ClientIndex);
			var playerName = template.Get<LabelWidget>("PLAYER");
			var playerNameFont = Game.Renderer.Fonts[playerName.Font];
			var suffixLength = new CachedTransform<string, int>(s => playerNameFont.Measure(s).X);
			var name = new CachedTransform<(string Name, int Width), string>(c =>
				WidgetUtils.TruncateText(c.Name, playerName.Bounds.Width - c.Width, playerNameFont));

			playerName.GetText = () =>
			{
				var suffix = player.WinState == WinState.Undefined ? "" : " (" + player.WinState + ")";
				if (client != null && client.State == Session.ClientState.Disconnected)
					suffix = " (Gone)";

				var sl = suffixLength.Update(suffix);
				return name.Update((player.PlayerName, sl)) + suffix;
			};

			playerName.GetColor = () => player.Color;
		}

		string AverageOrdersPerMinute(double orders)
		{
			return (world.WorldTick == 0 ? 0 : orders / (world.WorldTick / 1500.0)).ToString("F1");
		}

		static Color GetPowerColor(PowerState state)
		{
			if (state == PowerState.Critical)
				return Color.Red;

			if (state == PowerState.Low)
				return Color.Orange;

			return Color.LimeGreen;
		}

		// HACK The height of the templates and the scrollpanel needs to be kept in synch
		bool ShowScrollBar
		{
			get { return players.Count() + (hasTeams ? teams.Count() : 0) > 10; }
		}

		class StatsDropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
