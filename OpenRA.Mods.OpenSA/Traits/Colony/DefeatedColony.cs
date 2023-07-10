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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Colony
{
	public class DefeatedColonyInfo : TraitInfo, IRulesetLoaded
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

		public readonly CVec Offset = new(0, 0);

		public readonly string BackfallOwner = "Creeps";

		public override object Create(ActorInitializer init)
		{
			return new DefeatedColony(this);
		}

		public WeaponInfo WeaponInfo { get; private set; }
		public WeaponInfo ExplodeWeaponInfo { get; private set; }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!string.IsNullOrEmpty(Weapon))
			{
				var weaponToLower = Weapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");
				WeaponInfo = weapon;
			}

			if (!string.IsNullOrEmpty(Explode))
			{
				var explodesWeaponToLower = Explode.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(explodesWeaponToLower, out var explodesInfo))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{explodesWeaponToLower}'");
				ExplodeWeaponInfo = explodesInfo;
			}
		}
	}

	public class DefeatedColony : ITick, INotifyKilled
	{
		readonly DefeatedColonyInfo info;
		readonly Dictionary<OpenRA.Player, int> bitPickers = new();

		int fireBitTimer;
		OpenRA.Player newOwner;

		public DefeatedColony(DefeatedColonyInfo info)
		{
			this.info = info;

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

		void LaunchBits(Actor self, CPos tile)
		{
			var projectile = new ProjectileArgs
			{
				Weapon = info.WeaponInfo,
				Source = self.CenterPosition,
				SourceActor = self,
				PassiveTarget = self.World.Map.CenterOfCell(tile),
				RangeModifiers = Array.Empty<int>(),
				DamageModifiers = Array.Empty<int>(),
				InaccuracyModifiers = Array.Empty<int>()
			};

			self.World.AddFrameEndTask(w => w.Add(projectile.Weapon.Projectile.Create(projectile)));
		}

		void Explodes(Actor self)
		{
			// Don't use Target.FromActor as the actor suicides.
			info.ExplodeWeaponInfo.Impact(Target.FromPos(self.CenterPosition), self);
			Game.Sound.Play(SoundType.World, info.ColonyExplosionSound, self.CenterPosition);
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

			foreach (var t in self.TraitsImplementing<Turreted>())
				td.Add(new TurretFacingInit(t.Info, t.LocalOrientation.Yaw));

			self.World.AddFrameEndTask(w => w.CreateActor(info.SpawnsActor, td));
		}
	}
}
