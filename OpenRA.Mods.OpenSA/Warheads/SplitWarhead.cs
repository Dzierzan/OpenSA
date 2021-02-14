#region Copyright & License Information
/*
 * Copyright 2021 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Warheads
{
	[Desc("A warhead that fires another weapon on impact.")]
	public class SplitWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Terrain where the projectile explodes instead of bouncing.")]
		public readonly HashSet<string> InvalidBounceTerrain = new HashSet<string>();

		[Desc("Modify distance of each bounce by this percentage of previous distance.")]
		public readonly int BounceRangeModifier = 100;

		[Desc("At which angle to divide the split projectiles.")]
		public readonly WAngle[] SplitAngles = { WAngle.FromDegrees(-45), WAngle.FromDegrees(45) };

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;

			var world = firedBy.World;
			var map = world.Map;

			var position = args.ImpactPosition;
			var source = args.Source.Value;

			var cell = world.Map.CellContaining(position);
			if (!world.Map.Contains(cell))
				return;

			if (InvalidBounceTerrain.Contains(world.Map.GetTerrainInfo(cell).Type))
				return;

			var targetPosition = target.CenterPosition;

			foreach (var splitAngle in SplitAngles)
			{
				targetPosition += (position - source).Rotate(WRot.FromYaw(splitAngle)) * BounceRangeModifier / 100;

				var dat = world.Map.DistanceAboveTerrain(targetPosition);
				targetPosition += new WVec(0, 0, -dat.Length);

				var newTarget = Target.FromPos(targetPosition);

				var facing = (targetPosition - target.CenterPosition).Yaw;

				var projectileArgs = new ProjectileArgs
				{
					Weapon = weapon,
					Facing = facing,
					CurrentMuzzleFacing = () => facing,

					DamageModifiers = args.DamageModifiers,

					InaccuracyModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray() : new int[0],

					RangeModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IRangeModifier>()
						.Select(a => a.GetRangeModifier()).ToArray() : new int[0],

					Source = position,
					CurrentSource = () => position,
					SourceActor = firedBy,
					GuidedTarget = newTarget,
					PassiveTarget = targetPosition
				};

				if (projectileArgs.Weapon.Projectile != null)
				{
					var projectile = projectileArgs.Weapon.Projectile.Create(projectileArgs);
					if (projectile != null)
						firedBy.World.AddFrameEndTask(w => w.Add(projectile));

					if (projectileArgs.Weapon.Report != null && projectileArgs.Weapon.Report.Any())
						Game.Sound.Play(SoundType.World, projectileArgs.Weapon.Report.Random(firedBy.World.SharedRandom), target.CenterPosition);
				}
			}
		}
	}
}
