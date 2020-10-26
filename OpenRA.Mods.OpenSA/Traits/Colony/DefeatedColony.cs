using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	public enum ExplosionType { Footprint, CenterPosition }

	public class DefeatedColonyInfo : TraitInfo, IRulesetLoaded, Requires<TurretedInfo>
	{
		public readonly int BitFireDelay = 50;
		public readonly int NumberOfBits = 8;
		public readonly int MinRange = 5;
		public readonly int MaxRange = 10;

		[Desc("Value in percent of the maximum hit points.")]
		public readonly int ResurrectHealth = 10;

		[WeaponReference]
		public readonly string Weapon = "colony_bit";

		[WeaponReference]
		public readonly string Explode = "dieBuilding";

		public readonly string CaptureSound = "sounds|POWERUP.SDF";
		public readonly string ColonyExplosionSound = "sounds|COLONYEXPLODE.SDF";

		[ActorReference(typeof(ColonyInfo))]
		public readonly string SpawnsActor = "Colony";

		public readonly CVec Offset = new CVec(0, 0);

		public readonly string BackfallOwner = "Creeps";

		public override object Create(ActorInitializer init)
		{
			return new DefeatedColony(this, init.Self);
		}

		public WeaponInfo WeaponInfo { get; private set; }
		public WeaponInfo ExplodeWeaponInfo { get; private set; }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!string.IsNullOrEmpty(Weapon))
			{
				var weaponToLower = Weapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));
				WeaponInfo = weapon;
			}

			if (!string.IsNullOrEmpty(Explode))
			{
				var explodesWeaponToLower = Explode.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(explodesWeaponToLower, out var explodesInfo))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(explodesWeaponToLower));
				ExplodeWeaponInfo = explodesInfo;
			}
		}
	}

	public class DefeatedColony : ITick, INotifyKilled
	{
		readonly DefeatedColonyInfo info;
		readonly Dictionary<Player, int> bitPickers = new Dictionary<Player, int>();
		readonly IEnumerable<Turreted> turreted;

		int fireBitTimer;
		Player newOwner;

		public DefeatedColony(DefeatedColonyInfo info, Actor self)
		{
			this.info = info;
			turreted = self.TraitsImplementing<Turreted>();

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

			self.World.AddFrameEndTask(w => w.Add(projectile.Weapon.Projectile.Create(projectile)));
		}

		void Explodes(Actor self)
		{
			// Don't use Target.FromActor as the actor suicides.
			info.ExplodeWeaponInfo.Impact(Target.FromPos(self.CenterPosition), self);
			Game.Sound.Play(SoundType.World, info.ColonyExplosionSound, self.CenterPosition);
		}

		public void PickBit(Player player)
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

			if (bitPickers.Values.Sum() < info.NumberOfBits)
				return;

			var bits = bitPickers.Aggregate((a, b) => a.Value > b.Value ? a : b).Value;
			newOwner = bitPickers.First(entry => entry.Value == bits).Key;

			if (bitPickers.Values.Count(value => value == bits) > 1)
				newOwner = self.World.Players.First(player => player.InternalName == info.BackfallOwner);

			Game.Sound.Play(SoundType.World, info.CaptureSound, self.CenterPosition);
			bitPickers.Clear();
			self.Kill(self);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (newOwner == null)
				return;

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location + info.Offset),
				new CenterPositionInit(self.CenterPosition),
				new OwnerInit(newOwner),
				new HealthInit(info.ResurrectHealth),
			};

			foreach (var t in turreted)
				td.Add(new TurretFacingInit(t.Info, t.LocalOrientation.Yaw));

			self.World.AddFrameEndTask(w => w.CreateActor(info.SpawnsActor, td));
		}
	}
}
