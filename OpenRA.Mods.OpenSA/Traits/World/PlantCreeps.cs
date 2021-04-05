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
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Controls the 'Plants' checkbox in the lobby options.")]
	public class PlantCreepsInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the plants checkbox in the lobby.")]
		public readonly string CheckboxLabel = "Plants";

		[Desc("Tooltip description for the plants checkbox in the lobby.")]
		public readonly string CheckboxDescription = "Random plants occur and attack the player.";

		[Desc("Default value of the plant checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = true;

		[Desc("Prevent the plant state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the plants checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the plants checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("plants", CheckboxLabel, CheckboxDescription, CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new PlantCreeps(this); }
	}

	public class PlantCreeps : INotifyCreated
	{
		readonly PlantCreepsInfo info;
		public bool Enabled { get; private set; }

		public PlantCreeps(PlantCreepsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Enabled = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("plants", info.CheckboxEnabled);
		}
	}
}
