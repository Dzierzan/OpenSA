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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Warheads
{
	public class ColonyBitInit : ValueActorInit<ActorInitActorReference>
	{
		public ColonyBitInit(Actor value)
			: base(value) { }
	}

	public class SpawnColonyBitWarhead : Warhead
	{
		public readonly string[] BitActors = { "colony_bit1", "colony_bit2", "colony_bit3", "colony_bit4" };

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var targetPostion = target.CenterPosition;
			args.SourceActor.World.AddFrameEndTask(world =>
			{
				world.CreateActor(BitActors.Random(world.SharedRandom), new TypeDictionary
				{
					new LocationInit(world.Map.CellContaining(targetPostion)),
					new ColonyBitInit(args.SourceActor),
					new OwnerInit(world.Players.First(player => player.InternalName == "Neutral"))
				});
			});
		}
	}
}
