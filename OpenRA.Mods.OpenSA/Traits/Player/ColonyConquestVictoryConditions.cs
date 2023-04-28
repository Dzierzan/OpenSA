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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenSA.Traits.Colony;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Player
{
	[TraitLocation(SystemActors.World)]
	public class ColonyConquestVictoryConditionsInfo : TraitInfo, Requires<MissionObjectivesInfo>
	{
		[Desc("Delay for the end game notification in milliseconds.")]
		public readonly int NotificationDelay = 1500;

		[Desc("Description of the objective.")]
		public readonly string ColonyObjective = "Conquer all colonies!";

		[Desc("Description of the objective.")]
		public readonly string ConquestObjective = "Destroy all opposition!";

		[Desc("Disable the win/loss messages and audio notifications?")]
		public readonly bool SuppressNotifications = false;

		public override object Create(ActorInitializer init) { return new ColonyConquestVictoryConditions(init.Self, this); }
	}

	public class ColonyConquestVictoryConditions : ITick, INotifyWinStateChanged, INotifyTimeLimit, IWorldLoaded
	{
		readonly ColonyConquestVictoryConditionsInfo info;
		readonly MissionObjectives missionObjectives;
		readonly bool shortGame;

		OpenRA.Player[] enemies;
		int colonyObjectiveID = -1;
		int conquestObjectiveID = -1;

		public ColonyConquestVictoryConditions(Actor self, ColonyConquestVictoryConditionsInfo info)
		{
			this.info = info;
			missionObjectives = self.Trait<MissionObjectives>();
			shortGame = self.Owner.World.WorldActor.Trait<MapOptions>().ShortGame;
		}

		void IWorldLoaded.WorldLoaded(OpenRA.World world, WorldRenderer wr)
		{
			// Players, NonCombatants, and IsAlliedWith are all fixed once the game starts, so we can cache the result.
			if (enemies == null)
				enemies = world.Players.Where(p => !p.NonCombatant && !p.IsAlliedWith(world.LocalPlayer)).ToArray();
		}

		void ITick.Tick(Actor self)
		{
			if (self.Owner.WinState != WinState.Undefined || self.Owner.NonCombatant)
				return;

			if (self.World.Actors.Any(a => a.Info.Name == "randomcolony"))
				return;

			if (conquestObjectiveID < 0)
				conquestObjectiveID = missionObjectives.Add(self.Owner, info.ConquestObjective, "Primary", inhibitAnnouncement: true);

			if (colonyObjectiveID < 0)
				colonyObjectiveID = missionObjectives.Add(self.Owner, info.ColonyObjective, "Primary", inhibitAnnouncement: true);

			// Require neutral colonies to get captured first.
			if (self.World.ActorsHavingTrait<DefeatedColony>().Any())
				return;

			var colonies = self.World.ActorsHavingTrait<Colony.Colony>().ToArray();

			// Nothing can be done in this case so don't frustrate human players and lose immediately.
			if (!self.Owner.NonCombatant && !self.Owner.IsBot && !colonies.Any(c => c.Owner == self.Owner) && self.Owner.HasNoRequiredUnits(shortGame))
			{
				missionObjectives.MarkFailed(self.Owner, conquestObjectiveID);
				missionObjectives.MarkFailed(self.Owner, colonyObjectiveID);

				return;
			}

			// Don't win the game on enemy colony defeat, but on re-capture.
			if (colonies.All(c => c.Owner == self.Owner || c.Owner.IsAlliedWith(self.Owner)))
				missionObjectives.MarkCompleted(self.Owner, colonyObjectiveID);
			else if (missionObjectives.Objectives[colonyObjectiveID].State == ObjectiveState.Completed)
				colonyObjectiveID = missionObjectives.Add(self.Owner, info.ColonyObjective, "Primary", inhibitAnnouncement: true);

			// Also require all units to be killed.
			if (enemies.All(o => o.HasNoRequiredUnits(shortGame)))
				missionObjectives.MarkCompleted(self.Owner, conquestObjectiveID);
			else if (missionObjectives.Objectives[conquestObjectiveID].State == ObjectiveState.Completed)
				conquestObjectiveID = missionObjectives.Add(self.Owner, info.ConquestObjective, "Primary", inhibitAnnouncement: true);
		}

		void INotifyTimeLimit.NotifyTimerExpired(Actor self)
		{
			if (colonyObjectiveID < 0 || conquestObjectiveID < 0)
				return;

			var myTeam = self.World.LobbyInfo.ClientWithIndex(self.Owner.ClientIndex).Team;
			var teams = self.World.Players.Where(p => !p.NonCombatant && p.Playable)
				.Select(p => (Player: p, PlayerStatistics: p.PlayerActor.TraitOrDefault<PlayerStatistics>()))
				.OrderByDescending(p => p.PlayerStatistics != null ? p.PlayerStatistics.Experience : 0)
				.GroupBy(p => (self.World.LobbyInfo.ClientWithIndex(p.Player.ClientIndex) ?? new Session.Client()).Team)
				.OrderByDescending(g => g.Sum(gg => gg.PlayerStatistics != null ? gg.PlayerStatistics.Experience : 0));

			if (teams.First().Key == myTeam && (myTeam != 0 || teams.First().First().Player == self.Owner))
			{
				missionObjectives.MarkCompleted(self.Owner, colonyObjectiveID);
				missionObjectives.MarkCompleted(self.Owner, conquestObjectiveID);
				return;
			}

			missionObjectives.MarkFailed(self.Owner, colonyObjectiveID);
			missionObjectives.MarkFailed(self.Owner, conquestObjectiveID);
		}

		void INotifyWinStateChanged.OnPlayerLost(OpenRA.Player player)
		{
			foreach (var a in player.World.ActorsWithTrait<INotifyOwnerLost>().Where(a => a.Actor.Owner == player))
				a.Trait.OnOwnerLost(a.Actor);

			if (info.SuppressNotifications)
				return;

			TextNotificationsManager.AddSystemLine(player.PlayerName + " are defeated.");
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", missionObjectives.Info.LoseNotification, player.Faction.InternalName);
			});
		}

		void INotifyWinStateChanged.OnPlayerWon(OpenRA.Player player)
		{
			if (info.SuppressNotifications)
				return;

			TextNotificationsManager.AddSystemLine(player.PlayerName + " are victorious.");
			Game.RunAfterDelay(info.NotificationDelay, () =>
			{
				if (Game.IsCurrentWorld(player.World) && player == player.World.LocalPlayer)
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", missionObjectives.Info.WinNotification, player.Faction.InternalName);
			});
		}
	}
}
