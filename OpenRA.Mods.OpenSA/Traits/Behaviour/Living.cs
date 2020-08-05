using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Behaviour
{
	[Desc("Makes infantry feel more alive by randomly rotating or playing an animation when idle.")]
	class LivingInfo : TraitInfo, Requires<MobileInfo>
	{
		[Desc("Chance per tick the actor rotates to a random direction.")]
		public readonly int RotationChance = 1000;

		public override object Create(ActorInitializer init) { return new Living(init, this); }
	}

	class Living : ITick
	{
		private readonly LivingInfo info;
		private readonly Mobile mobile;

		public Living(ActorInitializer init, LivingInfo info)
		{
			this.info = info;
			mobile = init.Self.Trait<Mobile>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.CurrentActivity != null)
				return;

			if (info.RotationChance > 0 && self.World.SharedRandom.Next(1, info.RotationChance) == 1)
				mobile.Facing = self.World.SharedRandom.Next(0x00, 0xff);
		}
	}
}
