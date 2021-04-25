#region Copyright & License Information
/*
 * Copyright 2019-2021 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Used by Mobile. Required for wasp actors. Attach these to the world actor. You can have multiple variants by adding @suffixes.")]
	public class WaspLocomotorInfo : LocomotorInfo
	{
		[Desc("Pathfinding cost for taking off or landing.")]
		public readonly int TransitionCost = 0;

		[Desc("The terrain types that this actor can transition on. Leave empty to allow any.")]
		public readonly HashSet<string> TransitionTerrainTypes = new HashSet<string>();

		public override bool DisableDomainPassabilityCheck { get { return true; } }

		public override object Create(ActorInitializer init) { return new WaspLocomotor(init.Self, this); }
	}

	public class WaspLocomotor : Locomotor
	{
		public WaspLocomotor(Actor self, WaspLocomotorInfo info)
			: base(self, info) { }
	}
}
