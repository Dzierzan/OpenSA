using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits
{
	public enum OwnerType { Victim, Killer, InternalName }

	[Desc("Spawn another actor immediately upon death.")]
	public class SpawnRandomActorOnDeathInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn on death.")]
		public readonly string[] Actor = null;

		[Desc("Map player to use.")]
		public readonly string Owner = "Neutral";

		[Desc("Skips the spawned actor's make animations if true.")]
		public readonly bool SkipMakeAnimations = true;

		public override object Create(ActorInitializer init) { return new SpawnRandomActorOnDeath(this); }
	}

	public class SpawnRandomActorOnDeath : ConditionalTrait<SpawnRandomActorOnDeathInfo>, INotifyKilled, INotifyRemovedFromWorld
	{
		public SpawnRandomActorOnDeath(SpawnRandomActorOnDeathInfo info)
			: base(info) { }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (!self.IsInWorld)
				return;
		}

		// Don't add the new actor to the world before all RemovedFromWorld callbacks have run
		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location),
				new CenterPositionInit(self.CenterPosition),
				new FactionInit(self.Owner.Faction.InternalName)
			};

			td.Add(new OwnerInit(self.World.Players.First(p => p.InternalName == Info.Owner)));

			if (Info.SkipMakeAnimations)
				td.Add(new SkipMakeAnimsInit());

			foreach (var modifier in self.TraitsImplementing<IDeathActorInitModifier>())
				modifier.ModifyDeathActorInit(self, td);

			self.World.AddFrameEndTask(w => w.CreateActor(Info.Actor.Random(self.World.SharedRandom), td));
		}
	}
}
