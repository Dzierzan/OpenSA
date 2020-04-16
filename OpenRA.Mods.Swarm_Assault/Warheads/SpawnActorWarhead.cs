using System.Linq;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Swarm_Assault.Warheads
{
	[Desc("Spawn actors upon explosion. Don't use this with buildings.")]
	public class SpawnActorWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("The cell range to try placing the actors within.")]
		public readonly int Range = 10;

		[Desc("Actors to spawn.")]
		[ActorReference]
		public readonly string[] Actors = { };

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		[Desc("Defines the image of an optional animation played at the spawning location.")]
		public readonly string Image = null;

		[SequenceReference("Image")]
		[Desc("Defines the sequence of an optional animation played at the spawning location.")]
		public readonly string Sequence = null;

		[PaletteReference("UsePlayerPalette")]
		[Desc("Defines the palette of an optional animation played at the spawning location.")]
		public readonly string Palette = "effect";

		[Desc("List of sounds that can be played at the spawning location.")]
		public readonly string[] Sounds = new string[0];

		[FieldLoader.Require]
		[Desc("The terrain types that the actor is allowed to spawn.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		public readonly bool UsePlayerPalette = false;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			foreach (var a in Actors)
			{
				var actorInfo = rules.Actors[a.ToLowerInvariant()];
				var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();

				if (buildingInfo != null)
					throw new YamlException("SpawnActorWarhead cannot be used to spawn building actor '{0}'!".F(a));
			}
		}

		public override void DoImpact(Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var map = firedBy.World.Map;
			var targetCell = map.CellContaining(target.CenterPosition);

			var targetCells = map.FindTilesInCircle(targetCell, Range);
			var cell = targetCells.GetEnumerator();

			foreach (var a in Actors)
			{
				var td = new TypeDictionary();
				var ai = map.Rules.Actors[a.ToLowerInvariant()];

				td.Add(new OwnerInit(firedBy.World.Players.First(p => p.InternalName == InternalOwner)));

				while (cell.MoveNext())
				{
					if (!firedBy.World.ActorMap.GetActorsAt(cell.Current).Any() && TerrainTypes.Contains(firedBy.World.Map.GetTerrainInfo(cell.Current).Type))
					{
						td.Add(new LocationInit(cell.Current));
						var unit = firedBy.World.CreateActor(false, a.ToLowerInvariant(), td);

						firedBy.World.AddFrameEndTask(w =>
						{
							w.Add(unit);

							var palette = Palette;
							if (UsePlayerPalette)
								palette += unit.Owner.InternalName;

							var spawn = firedBy.World.Map.CenterOfCell(cell.Current);

							if (Image != null)
								w.Add(new SpriteEffect(spawn, w, Image, Sequence, palette));

							var sound = Sounds.RandomOrDefault(firedBy.World.LocalRandom);
							if (sound != null)
								Game.Sound.Play(SoundType.World, sound, spawn);
						});

						break;
					}
				}
			}
		}
	}
}
