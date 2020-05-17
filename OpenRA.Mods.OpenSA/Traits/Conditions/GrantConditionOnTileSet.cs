using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	public class GrantConditionOnTileSetInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Tile set IDs to trigger the condition.")]
		public readonly string[] TileSets = { };

		public object Create(ActorInitializer init) { return new GrantConditionOnTileSet(init, this); }
	}

	public class GrantConditionOnTileSet : INotifyCreated
	{
		readonly GrantConditionOnTileSetInfo info;
		readonly TileSet tileSet;

		public GrantConditionOnTileSet(ActorInitializer init, GrantConditionOnTileSetInfo info)
		{
			this.info = info;
			tileSet = init.World.Map.Rules.TileSet;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.TileSets.Contains(tileSet.Id))
				self.GrantCondition(info.Condition);
		}
	}
}
