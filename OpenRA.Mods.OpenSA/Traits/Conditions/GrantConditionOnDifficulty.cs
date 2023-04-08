#region Copyright & License Information
/*
 * Copyright 2023 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Grants a condition to this actor when it is owned by an AI bot.")]
	public class GrantConditionOnDifficultyInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Map difficulty that triggers the condition.")]
		public readonly string Difficulty = "normal";

		public readonly bool AppliesToPlayer = true;

		public override object Create(ActorInitializer init) { return new GrantConditionOnDifficulty(this, init.Self); }
	}

	public class GrantConditionOnDifficulty : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionOnDifficultyInfo info;
		readonly string difficulty;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnDifficulty(GrantConditionOnDifficultyInfo info, Actor self)
		{
			this.info = info;

			var mapDifficulty = self.World.WorldActor.TraitsImplementing<ScriptLobbyDropdown>()
					.FirstOrDefault(sld => sld.Info.ID == "difficulty");

			if (mapDifficulty != null)
				difficulty = mapDifficulty.Value;
		}

		void INotifyCreated.Created(Actor self)
		{
			GrantCondition(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			GrantCondition(self);
		}

		void GrantCondition(Actor self)
		{
			if (info.AppliesToPlayer && !self.Owner.IsBot && info.Difficulty == difficulty)
				conditionToken = self.GrantCondition(info.Condition);
		}
	}
}
