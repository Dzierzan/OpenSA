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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Render
{
	[Desc("Replaces the default animation when actor is created and grants a condition when finished.")]
	public class WithGrowAnimationInfo : TraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "grow";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		[GrantedConditionReference]
		[Desc("The condition to grant when fully grown.")]
		public readonly string Condition = "grown";

		public override object Create(ActorInitializer init) { return new WithGrowAnimation(init.Self, this); }
	}

	public class WithGrowAnimation : INotifyCreated
	{
		readonly WithGrowAnimationInfo info;
		readonly WithSpriteBody wsb;

		public WithGrowAnimation(Actor self, WithGrowAnimationInfo info)
		{
			this.info = info;
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
		}

		void INotifyCreated.Created(Actor self)
		{
			wsb.PlayCustomAnimation(self, info.Sequence, () => self.GrantCondition(info.Condition));
		}
	}
}
