using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.SA.Render
{
	public class WithFlyAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<IMoveInfo>
	{
		[SequenceReference]
		[Desc("Displayed while moving.")]
		public readonly string FlySequence = "move";

		[Desc("Which sprite body to modify.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithFlyAnimation(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var matches = ai.TraitInfos<WithSpriteBodyInfo>().Count(w => w.Name == Body);
			if (matches != 1)
				throw new YamlException("WithFlyAnimation needs exactly one sprite body with matching name.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class WithFlyAnimation : ConditionalTrait<WithFlyAnimationInfo>
	{
		readonly WithSpriteBody wsb;

		public WithFlyAnimation(ActorInitializer init, WithFlyAnimationInfo info)
			: base(info)
		{
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		void UpdateAnimation(Actor self)
		{
			var playAnim = false;
			if (!IsTraitDisabled)
				playAnim = true;

			if (!playAnim && wsb.DefaultAnimation.CurrentSequence.Name == Info.FlySequence)
			{
				wsb.CancelCustomAnimation(self);
				return;
			}

			if (playAnim && wsb.DefaultAnimation.CurrentSequence.Name != Info.FlySequence)
				wsb.PlayCustomAnimationRepeating(self, Info.FlySequence);
		}

		protected override void TraitEnabled(Actor self)
		{
			// HACK: Use a FrameEndTask to avoid construction order issues with WithSpriteBody
			self.World.AddFrameEndTask(w => UpdateAnimation(self));
		}

		protected override void TraitDisabled(Actor self)
		{
			UpdateAnimation(self);
		}
	}
}
