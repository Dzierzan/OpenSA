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

namespace OpenRA.Mods.OpenSA.Traits.Colony
{
	public class ColonyInfo : TraitInfo
	{
		public readonly string LostSound = "sounds|POWERDOWN.SDF";

		[ActorReference(typeof(DefeatedColonyInfo))]
		public readonly string SpawnsActor = "UnclaimedColony";

		public readonly string NewOwner = "Neutral";

		public readonly CVec Offset = new(0, 0);

		public override object Create(ActorInitializer init)
		{
			return new Colony(this);
		}
	}

	public class Colony : INotifyKilled
	{
		readonly ColonyInfo info;

		public Colony(ColonyInfo info)
		{
			this.info = info;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			Game.Sound.Play(SoundType.World, info.LostSound, self.CenterPosition);

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location + info.Offset),
				new CenterPositionInit(self.CenterPosition),
				new OwnerInit(info.NewOwner),
			};

			foreach (var t in self.TraitsImplementing<Turreted>())
				td.Add(new TurretFacingInit(t.Info, t.LocalOrientation.Yaw));

			self.World.AddFrameEndTask(w => w.CreateActor(info.SpawnsActor, td));
		}
	}
}
