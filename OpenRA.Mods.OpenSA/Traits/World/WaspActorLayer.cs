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

using System.Linq;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.World
{
	[TraitLocation(SystemActors.World)]
	public class WaspActorLayerInfo : TraitInfo, ICustomMovementLayerInfo
	{
		[Desc("Terrain type of the airborne layer.")]
		public readonly string TerrainType = "Air";

		public override object Create(ActorInitializer init) { return new WaspActorLayer(init.Self, this); }
	}

	public class WaspActorLayer : ICustomMovementLayer
	{
		readonly OpenRA.World world;

		readonly byte terrainIndex;

		public WaspActorLayer(Actor self, WaspActorLayerInfo info)
		{
			world = self.World;

			terrainIndex = self.World.Map.Rules.TerrainInfo.GetTerrainIndex(info.TerrainType);
		}

		bool ICustomMovementLayer.EnabledForLocomotor(LocomotorInfo li) { return li is WaspLocomotorInfo; }
		byte ICustomMovementLayer.Index { get { return CustomMovementLayerType.Jumpjet; } }
		bool ICustomMovementLayer.InteractsWithDefaultLayer { get { return false; } }
		bool ICustomMovementLayer.ReturnToGroundLayerOnIdle { get { return true; } }

		WPos ICustomMovementLayer.CenterOfCell(CPos cell)
		{
			return world.Map.CenterOfCell(cell); // height is only fake
		}

		bool ValidTransitionCell(CPos cell, LocomotorInfo li)
		{
			var terrainType = world.Map.GetTerrainInfo(cell).Type;
			var wli = (WaspLocomotorInfo)li;
			if (!wli.TransitionTerrainTypes.Contains(terrainType) && wli.TransitionTerrainTypes.Any())
				return false;

			return true;
		}

		short ICustomMovementLayer.EntryMovementCost(LocomotorInfo li, CPos cell)
		{
			var wli = (WaspLocomotorInfo)li;
			return ValidTransitionCell(cell, wli) ? wli.TransitionCost : PathGraph.MovementCostForUnreachableCell;
		}

		short ICustomMovementLayer.ExitMovementCost(LocomotorInfo li, CPos cell)
		{
			var wli = (WaspLocomotorInfo)li;
			return ValidTransitionCell(cell, wli) ? wli.TransitionCost : PathGraph.MovementCostForUnreachableCell;
		}

		byte ICustomMovementLayer.GetTerrainIndex(CPos cell)
		{
			if (world.ActorMap.GetActorsAt(cell).Any(a => a.TraitOrDefault<Colony.Colony>() != null))
				return byte.MaxValue;

			return terrainIndex;
		}
	}
}
