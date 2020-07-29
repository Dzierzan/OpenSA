using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits
{
	public class WaspActorLayerInfo : TraitInfo
	{
		[Desc("Terrain type of the airborne layer.")]
		public readonly string TerrainType = "Air";

		[Desc("Height offset relative to the terrain for movement.")]
		public readonly WDist HeightOffset = new WDist(1);

		public override object Create(ActorInitializer init) { return new WaspActorLayer(init.Self, this); }
	}

	public class WaspActorLayer : ICustomMovementLayer
	{
		readonly World world;
		readonly WaspActorLayerInfo info;

		readonly byte terrainIndex;

		public WaspActorLayer(Actor self, WaspActorLayerInfo info)
		{
			world = self.World;
			this.info = info;

			terrainIndex = self.World.Map.Rules.TileSet.GetTerrainIndex(info.TerrainType);
		}

		bool ICustomMovementLayer.EnabledForActor(ActorInfo a, LocomotorInfo li) { return li is WaspLocomotorInfo; }
		byte ICustomMovementLayer.Index { get { return CustomMovementLayerType.Jumpjet; } }
		bool ICustomMovementLayer.InteractsWithDefaultLayer { get { return false; } }
		bool ICustomMovementLayer.ReturnToGroundLayerOnIdle { get { return true; } }

		WPos ICustomMovementLayer.CenterOfCell(CPos cell)
		{
			var pos = world.Map.CenterOfCell(cell);
			return pos + new WVec(0, 0, info.HeightOffset.Length - pos.Z);
		}

		bool ValidTransitionCell(CPos cell, LocomotorInfo li)
		{
			var terrainType = world.Map.GetTerrainInfo(cell).Type;
			var jli = (WaspLocomotorInfo)li;
			if (!jli.TransitionTerrainTypes.Contains(terrainType) && jli.TransitionTerrainTypes.Any())
				return false;

			return true;
		}

		int ICustomMovementLayer.EntryMovementCost(ActorInfo a, LocomotorInfo li, CPos cell)
		{
			var jli = (WaspLocomotorInfo)li;
			return ValidTransitionCell(cell, jli) ? jli.TransitionCost : int.MaxValue;
		}

		int ICustomMovementLayer.ExitMovementCost(ActorInfo a, LocomotorInfo li, CPos cell)
		{
			var jli = (WaspLocomotorInfo)li;
			return ValidTransitionCell(cell, jli) ? jli.TransitionCost : int.MaxValue;
		}

		byte ICustomMovementLayer.GetTerrainIndex(CPos cell)
		{
			if (world.ActorMap.GetActorsAt(cell).Any(a => a.TraitOrDefault<Colony>() != null))
				return byte.MaxValue;

			return terrainIndex;
		}
	}
}
