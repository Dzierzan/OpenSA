using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits
{
	[Desc("Controls the 'Flyer' checkbox in the lobby options.")]
	public class FlyerCreepsInfo : ITraitInfo, ILobbyOptions
	{
		[Translate]
		[Desc("Descriptive label for the plants checkbox in the lobby.")]
		public readonly string CheckboxLabel = "Flyers";

		[Translate]
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

		public object Create(ActorInitializer init) { return new FlyerCreeps(this); }
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
