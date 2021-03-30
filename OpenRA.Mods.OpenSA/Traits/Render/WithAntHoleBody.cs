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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Traits.Render
{
	[Desc("Renders ant holes with tileset specific variants.")]
	class WithAntHoleBodyInfo : TraitInfo, Requires<RenderSpritesInfo>, IRenderActorPreviewSpritesInfo
	{
		public readonly string IdleSequence = "idle";

		public override object Create(ActorInitializer init) { return new WithAntHoleBody(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var anim = new Animation(init.World, image);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), IdleSequence));
			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}
	}

	class WithAntHoleBody : IAutoMouseBounds
	{
		readonly WithAntHoleBodyInfo info;
		readonly Animation defaultAnimation;
		readonly Animation boundsAnimation;
		readonly RenderSprites renderSprites;

		public WithAntHoleBody(Actor self, WithAntHoleBodyInfo info)
		{
			this.info = info;
			renderSprites = self.Trait<RenderSprites>();
			var tileset = self.World.Map.Tileset.ToLowerInvariant();
			var image = "{0}_{1}".F(renderSprites.GetImage(self), tileset);

			defaultAnimation = new Animation(self.World, image);
			defaultAnimation.Play(info.IdleSequence);
			renderSprites.Add(defaultAnimation);

			// Cache the bounds from the default sequence to avoid flickering when the animation changes
			boundsAnimation = new Animation(self.World, image);
			boundsAnimation.PlayRepeating(info.IdleSequence);
		}

		public string NormalizeSequence(Actor self, string sequence)
		{
			return RenderSprites.NormalizeSequence(defaultAnimation, self.GetDamageState(), sequence);
		}

		public virtual void PlayCustomAnimation(Actor self, string name, Action after = null)
		{
			defaultAnimation.PlayThen(NormalizeSequence(self, name), () =>
			{
				CancelCustomAnimation(self);
				after?.Invoke();
			});
		}

		public virtual void CancelCustomAnimation(Actor self)
		{
			defaultAnimation.PlayRepeating(NormalizeSequence(self, info.IdleSequence));
		}

		Rectangle IAutoMouseBounds.AutoMouseoverBounds(Actor self, WorldRenderer wr)
		{
			return boundsAnimation.ScreenBounds(wr, self.CenterPosition, WVec.Zero, renderSprites.Info.Scale);
		}
	}
}
