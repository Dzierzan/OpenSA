using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Swarm_Assault.Warheads
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
        public readonly string[] BitActors = {"colony_bit1", "colony_bit2", "colony_bit3", "colony_bit4"};

        public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
        {
            firedBy.World.AddFrameEndTask(world =>
            {
                world.CreateActor(BitActors[world.SharedRandom.Next(0, BitActors.Length)], new TypeDictionary
                {
                    new LocationInit(world.Map.CellContaining(target.CenterPosition)),
                    new ColonyBitInit(firedBy),
                    new OwnerInit(world.Players.First(player => player.InternalName == "Neutral"))
                });
            });
        }
    }
}