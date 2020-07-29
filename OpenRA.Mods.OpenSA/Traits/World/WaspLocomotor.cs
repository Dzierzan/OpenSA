using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.SA.Traits
{
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
