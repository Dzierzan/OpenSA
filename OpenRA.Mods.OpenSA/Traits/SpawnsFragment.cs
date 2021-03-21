#region Copyright & License Information
/*
 * Copyright 2019-2021 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenSA.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Spawns fragment weapons after a periodic interval.")]
	public class SpawnsFragmentInfo : PausableConditionalTraitInfo
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Delay between two spawns. Two values indicate a range.")]
		public readonly int[] Delay = { 50 };

		[Desc("Fragment spawn offset relative to actor's position.")]
		public readonly WVec LocalOffset = WVec.Zero;

		[Desc("Percentual chance the fragment is fired.")]
		public readonly int Chance = 100;

		[Desc("Target offsets relative to actor center position + LocalOffset.")]
		public readonly WVec[] Offsets = { new WVec(0, 0, 0) };

		[Desc("If set, Offset's Z value will be used as absolute height instead of explosion height.")]
		public readonly bool UseZOffsetAsAbsoluteHeight = false;

		[Desc("Rotate the fragment weapon based on the impact orientation.")]
		public readonly bool Rotate = false;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new SpawnsFragment(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weaponInfo;
		}
	}

	class SpawnsFragment : PausableConditionalTrait<SpawnsFragmentInfo>, ITick, ISync
	{
		readonly World world;
		readonly BodyOrientation body;

		[Sync]
		int ticks;

		WithSpawnsShrapnelAnimation[] animations;

		public SpawnsFragment(Actor self, SpawnsFragmentInfo info)
			: base(info)
		{
			world = self.World;
			body = self.TraitOrDefault<BodyOrientation>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			animations = self.TraitsImplementing<WithSpawnsShrapnelAnimation>().ToArray();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused || !self.IsInWorld || --ticks > 0)
				return;

			ticks = Info.Delay.Length == 2
					? world.SharedRandom.Next(Info.Delay[0], Info.Delay[1])
					: Info.Delay[0];

			var localoffset = body != null
					? body.LocalToWorld(Info.LocalOffset.Rotate(body.QuantizeOrientation(self, self.Orientation)))
					: Info.LocalOffset;

			var position = self.CenterPosition + localoffset;

			foreach (var offset in Info.Offsets)
			{
				var targetVector = offset;

				if (Info.Rotate && body != null)
					targetVector = targetVector.Rotate(body.QuantizeOrientation(self, self.Orientation));

				var fragmentTargetPosition = position + targetVector;

				if (Info.UseZOffsetAsAbsoluteHeight)
				{
					fragmentTargetPosition = new WPos(fragmentTargetPosition.X, fragmentTargetPosition.Y,
						world.Map.CenterOfCell(world.Map.CellContaining(fragmentTargetPosition)).Z + offset.Z);
				}

				var fragmentTarget = Target.FromPos(fragmentTargetPosition);
				var fragmentFacing = (fragmentTargetPosition - position).Yaw;

				var projectileArgs = new ProjectileArgs
				{
					Weapon = Info.WeaponInfo,
					Facing = fragmentFacing,
					CurrentMuzzleFacing = () => fragmentFacing,

					DamageModifiers = !self.IsDead ? self.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray() : new int[0],

					InaccuracyModifiers = !self.IsDead ? self.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray() : new int[0],

					RangeModifiers = !self.IsDead ? self.TraitsImplementing<IRangeModifier>()
						.Select(a => a.GetRangeModifier()).ToArray() : new int[0],

					Source = position,
					CurrentSource = () => position,
					SourceActor = self,
					GuidedTarget = fragmentTarget,
					PassiveTarget = fragmentTargetPosition
				};

				if (projectileArgs.Weapon.Projectile != null)
				{
					var projectile = projectileArgs.Weapon.Projectile.Create(projectileArgs);
					if (projectile != null)
						world.AddFrameEndTask(w => w.Add(projectile));

					if (projectileArgs.Weapon.Report != null && projectileArgs.Weapon.Report.Any())
						Game.Sound.Play(SoundType.World, projectileArgs.Weapon.Report.Random(world.SharedRandom), position);
				}
			}

			foreach (var animation in animations)
				animation.Trigger(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			ticks = Util.RandomDelay(self.World, Info.Delay);
		}
	}
}
