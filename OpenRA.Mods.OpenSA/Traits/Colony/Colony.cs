#region Copyright & License Information
/*
 * Copyright 2019-2022 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	public class ColonyInfo : TraitInfo, Requires<TurretedInfo>
	{
		public readonly string LostSound = "sounds|POWERDOWN.SDF";

		[ActorReference(typeof(DefeatedColonyInfo))]
		public readonly string SpawnsActor = "UnclaimedColony";

		public readonly string NewOwner = "Neutral";

		public readonly CVec Offset = new CVec(0, 0);

		public override object Create(ActorInitializer init)
		{
			return new Colony(this, init.Self);
		}
	}

	public class Colony : INotifyKilled
	{
		readonly ColonyInfo info;
		readonly IEnumerable<Turreted> turreted;

		public Colony(ColonyInfo info, Actor self)
		{
			this.info = info;
			turreted = self.TraitsImplementing<Turreted>();
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

			foreach (var t in turreted)
				td.Add(new TurretFacingInit(t.Info, t.LocalOrientation.Yaw));

			self.World.AddFrameEndTask(w => w.CreateActor(info.SpawnsActor, td));
		}
	}
}
