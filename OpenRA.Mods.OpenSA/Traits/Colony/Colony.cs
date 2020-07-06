using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.SA.Traits.Colony
{
	public enum ExplosionType { Footprint, CenterPosition }

	public class ColonyInfo : TraitInfo, Requires<HealthInfo>, IRulesetLoaded
	{
		public readonly int BitFireDelay = 50;
		public readonly int NumberOfBits = 8;
		public readonly int MinRange = 5;
		public readonly int MaxRange = 10;
		public readonly int ResurrectHealth = 10;

		[WeaponReference]
		public readonly string Weapon = "colony_bit";

		[WeaponReference]
		public readonly string Explode = "dieBuilding";

		public readonly string CaptureSound = "sounds|POWERUP.SDF";
		public readonly string LostSound = "sounds|POWERDOWN.SDF";
		public readonly string ColonyExplosionSound = "sounds|COLONYEXPLODE.SDF";

		public override object Create(ActorInitializer init)
		{
			return new Colony(init, this);
		}

		public WeaponInfo WeaponInfo { get; private set; }
		public WeaponInfo ExplodeWeaponInfo { get; private set; }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!string.IsNullOrEmpty(Weapon))
			{
				WeaponInfo weapon;
				var weaponToLower = Weapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));
				WeaponInfo = weapon;
			}

			if (!string.IsNullOrEmpty(Explode))
			{
				WeaponInfo explodesInfo;
				var explodesWeaponToLower = Explode.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(explodesWeaponToLower, out explodesInfo))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(explodesWeaponToLower));
				ExplodeWeaponInfo = explodesInfo;
			}
		}
	}

	public class Colony : INotifyKilled, ITick
	{
		readonly ColonyInfo info;
		readonly Health health;
		readonly Dictionary<OpenRA.Player, int> bitPickers = new Dictionary<OpenRA.Player, int>();
		int fireBitTimer;

		public Colony(ActorInitializer init, ColonyInfo info)
		{
			this.info = info;
			health = init.Self.Trait<Health>();
			health.RemoveOnDeath = false;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			Game.Sound.Play(SoundType.World, info.LostSound, self.CenterPosition);
			self.ChangeOwner(self.World.Players.First(player => player.InternalName == "Creeps"));
			CancelProductions(self);
			fireBitTimer = info.BitFireDelay;
		}

		List<CPos> GetBitTargetTiles(Actor self)
		{
			var cell = self.World.Map.CellContaining(self.CenterPosition);
			var tiles = new List<CPos>();
			for (var i = 0; tiles.Count < info.NumberOfBits; i++)
			{
				tiles = self.World.Map
					.FindTilesInAnnulus(cell, info.MinRange, info.MaxRange + i)
					.Where(tilePosition =>
					{
						if (!self.World.Map.Contains(tilePosition))
							return false;

						if (self.World.Map.GetTerrainInfo(tilePosition).Type == "Water")
							return false;

						if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(tilePosition) != null)
							return false;

						return !self.World.ActorMap.AnyActorsAt(tilePosition);
					}).ToList();
			}

			return tiles;
		}

		private void LaunchBits(Actor self, CPos tile)
		{
			var projectile = new ProjectileArgs
			{
				Weapon = info.WeaponInfo,
				Source = self.CenterPosition,
				SourceActor = self,
				PassiveTarget = self.World.Map.CenterOfCell(tile),
				RangeModifiers = new int[0],
				DamageModifiers = new int[0],
				InaccuracyModifiers = new int[0]
			};
			self.World.AddFrameEndTask(world => world.Add(projectile.Weapon.Projectile.Create(projectile)));
		}

		void Explodes(Actor self)
		{
			// Don't use Target.FromActor as the actor suicides.
			info.ExplodeWeaponInfo.Impact(Target.FromPos(self.CenterPosition), self);
			Game.Sound.Play(SoundType.World, info.ColonyExplosionSound, self.CenterPosition);
		}

		void CancelProductions(Actor self)
		{
			foreach (var productionQueue in self.TraitsImplementing<ProductionQueue>())
			{
				while (true)
				{
					var producing = productionQueue.AllQueued();

					if (!producing.Any())
						break;

					foreach (var productionItem in producing)
						productionQueue.EndProduction(productionItem);
				}
			}
		}

		public void PickBit(OpenRA.Player player)
		{
			if (!bitPickers.ContainsKey(player))
				bitPickers.Add(player, 0);

			bitPickers[player]++;
		}

		void ITick.Tick(Actor self)
		{
			if (fireBitTimer > 0)
			{
				if (--fireBitTimer == 0)
				{
					var tiles = GetBitTargetTiles(self);

					for (var i = 0; i < info.NumberOfBits; i++)
					{
						var tile = tiles[self.World.SharedRandom.Next(0, tiles.Count)];
						tiles.Remove(tile);
						LaunchBits(self, tile);
						Explodes(self);
					}
				}
			}

			if (!self.IsDead)
				return;

			if (bitPickers.Values.Sum() < info.NumberOfBits)
				return;

			var bits = bitPickers.Aggregate((a, b) => a.Value > b.Value ? a : b).Value;
			var newOwner = bitPickers.First(entry => entry.Value == bits).Key;

			if (bitPickers.Values.Count(value => value == bits) > 1)
				newOwner = self.World.Players.First(player => player.InternalName == "Creeps");

			self.ChangeOwner(newOwner);
			Game.Sound.Play(SoundType.World, info.CaptureSound, self.CenterPosition);
			bitPickers.Clear();
			health.Resurrect(self, self);
			health.InflictDamage(self, self, new Damage(health.MaxHP - info.ResurrectHealth), true);
		}
	}
}
