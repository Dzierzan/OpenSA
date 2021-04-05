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
	[Desc("Controls the 'Flyer' checkbox in the lobby options.")]
	public class FlyerCreepsInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the plants checkbox in the lobby.")]
		public readonly string CheckboxLabel = "Flyers";

		[Desc("Tooltip description for the flyer checkbox in the lobby.")]
		public readonly string CheckboxDescription = "Random flyers occur and drop bombs.";

		[Desc("Default value of the flyer checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = true;

		[Desc("Prevent the flyer state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the flyers checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the flyers checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("flyers", CheckboxLabel, CheckboxDescription, CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new FlyerCreeps(this); }
	}

	public class FlyerCreeps : INotifyCreated
	{
		readonly FlyerCreepsInfo info;
		public bool Enabled { get; private set; }

		public FlyerCreeps(FlyerCreepsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Enabled = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("flyers", info.CheckboxEnabled);
		}
	}
}
