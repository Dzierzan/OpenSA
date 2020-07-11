using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.SA.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits.Colony
{
	public class ColonyBitInfo : TraitInfo
	{
		public readonly int MinLifetime = 500;
		public readonly int MaxLifetime = 1000;
		public readonly int AutoPickupRadius = 5;
		public readonly string CrushClass = "colony_bit";
		public readonly string PickupSound = "sounds|COLONYPICKUP.SDF";
		public readonly string TimeoutSound = "sounds|COLONYPICKUPTIMEOUT.SDF";

		public override object Create(ActorInitializer init)
		{
			return new ColonyBit(init, this);
		}
	}

	public class ColonyBit : ITick, ICrushable, INotifyCrushed
	{
		private readonly ColonyBitInfo info;
		private readonly Colony colony;
		private int lifetime;

		public ColonyBit(ActorInitializer init, ColonyBitInfo info)
		{
			this.info = info;
			colony = init.Get<ColonyBitInit>().ColonyActor.Trait<Colony>();
			lifetime = init.World.WorldTick + init.World.SharedRandom.Next(info.MinLifetime, info.MaxLifetime);
		}

		void ITick.Tick(Actor self)
		{
			if (--lifetime > 0)
			{
				var actors = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(info.AutoPickupRadius));
				foreach (var actor in actors)
				{
					if (!actor.IsIdle || !actor.Info.HasTraitInfo<MobileInfo>())
						continue;

					actor.QueueActivity(new Move(actor, Target.FromPos(self.CenterPosition), WDist.Zero));
					break;
				}

				return;
			}

			colony.PickBit(self.World.Players.First(player => player.InternalName == "Creeps"));
			Game.Sound.Play(SoundType.World, info.TimeoutSound, self.CenterPosition);
			self.Dispose();
		}

		bool ICrushable.CrushableBy(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			return self.IsAtGroundLevel() && crushClasses.Contains(info.CrushClass);
		}

		LongBitSet<PlayerBitMask> ICrushable.CrushableBy(Actor self, BitSet<CrushClass> crushClasses)
		{
			return self.World.AllPlayersMask;
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			colony.PickBit(crusher.Owner);
			Game.Sound.Play(SoundType.World, info.PickupSound, self.CenterPosition);
			self.Dispose();
		}
	}
}
