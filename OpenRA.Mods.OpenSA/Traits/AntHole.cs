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
using OpenRA.Effects;
using OpenRA.Mods.OpenSA.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits
{
	[Desc("Animates the anthole and spawns actors.")]
	public class AntHoleInfo : TraitInfo, Requires<WithAntHoleBodyInfo>
	{
		[FieldLoader.Require]
		[ActorReference(typeof(PirateAntInfo))]
		public readonly string[] Actors = null;

		[Desc("Chance of each actor spawning.")]
		[FieldLoader.Require]
		public readonly int[] ActorShares = null;

		[Desc("Minimum and Maximum number of actors spawning.")]
		public readonly int2 Amount = new(1, 5);

		[Desc("Time in ticks between each ant crawling out of the hole.")]
		public readonly int Delay = 40;

		public readonly string OpenSequence = "open";

		public readonly string OpenSound = "ANTHOLEOPEN.SDF";

		public readonly string CloseSequence = "close";

		public readonly string CloseSound = "ANTHOLECLOSE.SDF";

		public readonly string Owner = "Creeps";

		public override object Create(ActorInitializer init) { return new AntHole(init, this); }
	}

	public class AntHole : INotifyCreated
	{
		readonly AntHoleInfo info;
		readonly WithAntHoleBody body;

		public AntHole(ActorInitializer init, AntHoleInfo info)
		{
			this.info = info;
			var self = init.Self;
			body = self.Trait<WithAntHoleBody>();
		}

		string ChooseActor(Actor self)
		{
			var shares = info.ActorShares;
			var n = self.World.SharedRandom.Next(shares.Sum());

			var cumulativeShares = 0;
			for (var i = 0; i < shares.Length; i++)
			{
				cumulativeShares += shares[i];
				if (n <= cumulativeShares)
					return info.Actors[i];
			}

			return null;
		}

		void INotifyCreated.Created(Actor self)
		{
			Game.Sound.Play(SoundType.World, info.OpenSound, self.CenterPosition);

			body.PlayCustomAnimation(self, info.OpenSequence, () =>
			{
				var amount = self.World.SharedRandom.Next(info.Amount.X, info.Amount.Y);
				for (var i = 0; i < amount; i++)
				{
					self.World.Add(new DelayedAction(info.Delay * i, () =>
					{
						var actor = ChooseActor(self);
						var ant = self.World.CreateActor(true, actor.ToLowerInvariant(), new TypeDictionary
						{
							new OwnerInit(self.World.Players.First(x => x.PlayerName == info.Owner)),
							new LocationInit(self.Location)
						});
						ant.Trait<PirateAnt>().AntHoleAmount = amount;
					}));
				}

				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(info.Delay * amount, () =>
				{
					Game.Sound.Play(SoundType.World, info.CloseSound, self.CenterPosition);

					body.PlayCustomAnimation(self, info.CloseSequence, () => { self.Dispose(); });
				})));
			});
		}
	}
}
