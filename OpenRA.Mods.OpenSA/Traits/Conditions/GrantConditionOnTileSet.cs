using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits
{
	public class GrantConditionOnTileSetInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Tile set IDs to trigger the condition.")]
		public readonly string[] TileSets = { };

		public override object Create(ActorInitializer init) { return new GrantConditionOnTileSet(this); }
	}

	public class GrantConditionOnTileSet : INotifyCreated
	{
		readonly GrantConditionOnTileSetInfo info;

		public GrantConditionOnTileSet(GrantConditionOnTileSetInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.TileSets.Contains(self.World.Map.Rules.TileSet.Id))
				self.GrantCondition(info.Condition);
		}
	}
}
