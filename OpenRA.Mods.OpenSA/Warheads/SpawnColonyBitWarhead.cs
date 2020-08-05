using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Warheads
{
	public class ColonyBitInit : IActorInit
	{
		public readonly Actor ColonyActor;

		public ColonyBitInit(Actor colonyActor)
		{
			ColonyActor = colonyActor;
		}
	}

	public class SpawnColonyBitWarhead : Warhead
	{
		public readonly string[] BitActors = { "colony_bit1", "colony_bit2", "colony_bit3", "colony_bit4" };

		public override void DoImpact(Target target, WarheadArgs args)
		{
			args.SourceActor.World.AddFrameEndTask(world =>
			{
				world.CreateActor(BitActors.Random(world.SharedRandom), new TypeDictionary
				{
					new LocationInit(world.Map.CellContaining(target.CenterPosition)),
					new ColonyBitInit(args.SourceActor),
					new OwnerInit(world.Players.First(player => player.InternalName == "Neutral"))
				});
			});
		}
	}
}
