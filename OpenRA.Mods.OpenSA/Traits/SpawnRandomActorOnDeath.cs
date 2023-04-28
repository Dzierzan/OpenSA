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

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Spawn a random actor of the same owner immediately upon death.")]
	public class SpawnRandomActorOnDeathInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Possible actor to to choose from.")]
		public readonly string[] Actors = null;

		[Desc("Offset of the spawned actor relative to the dying actor's position.",
			"Warning: Spawning an actor outside the parent actor's footprint/influence might",
			"lead to unexpected behaviour.")]
		public readonly CVec Offset = CVec.Zero;

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

		// Don't add the new actor to the world before all RemovedFromWorld callbacks have run.
		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location + Info.Offset),
				new CenterPositionInit(self.CenterPosition),
				new OwnerInit(self.Owner.InternalName),
				new SkipMakeAnimsInit()
			};

			self.World.AddFrameEndTask(w => w.CreateActor(Info.Actors.Random(self.World.SharedRandom), td));
		}
	}
}
