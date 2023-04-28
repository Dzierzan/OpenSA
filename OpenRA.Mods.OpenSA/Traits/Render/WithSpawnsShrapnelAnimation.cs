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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Render
{
	[Desc("Periodically plays an idle animation, replacing the default body animation.")]
	public class WithSpawnsShrapnelAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference]
		[Desc("Sequence names to use.")]
		public readonly string[] Sequences = { "active" };

		public readonly int Interval = 750;

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithSpawnsShrapnelAnimation(init.Self, this); }
	}

	public class WithSpawnsShrapnelAnimation : ConditionalTrait<WithSpawnsShrapnelAnimationInfo>
	{
		readonly WithSpriteBody wsb;

		public WithSpawnsShrapnelAnimation(Actor self, WithSpawnsShrapnelAnimationInfo info)
			: base(info)
		{
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		public void Trigger(Actor self)
		{
			if (IsTraitDisabled)
				return;

			wsb.PlayCustomAnimation(self, Info.Sequences.Random(Game.CosmeticRandom));
		}

		protected override void TraitDisabled(Actor self)
		{
			wsb.CancelCustomAnimation(self);
		}
	}
}
