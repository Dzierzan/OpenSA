using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Put this on the Player actor. Manages colony healing by halting production.")]
	public class HealColonyBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Act upon this damage taken.")]
		public readonly DamageState DamageState = DamageState.Critical;

		public override object Create(ActorInitializer init) { return new HealColonyBotModule(init.Self, this); }
	}

	public class HealColonyBotModule : ConditionalTrait<HealColonyBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;

		public HealColonyBotModule(Actor self, HealColonyBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		void IBotTick.BotTick(IBot bot)
		{
			var colonies = AIUtils.GetActorsWithTrait<Colony>(world).Where(c => c.Owner == player).ToArray();
			foreach (var colony in colonies)
			{
				var health = colony.Trait<IHealth>();
				if (health.DamageState == Info.DamageState)
					colony.Trait<Colony>().CancelProductions(colony);
			}
		}
	}
}
