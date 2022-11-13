#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenSA.Graphics
{
	public class CustomSelectionBoxAnnotationRenderableColony : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly Rectangle decorationBounds;
		readonly Color color;

		public CustomSelectionBoxAnnotationRenderableColony(Actor actor, Rectangle decorationBounds, Color color)
			: this(actor.CenterPosition, decorationBounds, color) { }

		public CustomSelectionBoxAnnotationRenderableColony(WPos pos, Rectangle decorationBounds, Color color)
		{
			this.pos = pos;
			this.decorationBounds = decorationBounds;
			this.color = color;
		}

		public WPos Pos => pos;

		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return new CustomSelectionBoxAnnotationRenderableColony(pos + vec, decorationBounds, color); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var tl = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Left, decorationBounds.Top)).ToFloat2();
			var br = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Right, decorationBounds.Bottom)).ToFloat2();
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);

			var a = new float2(4, 0);
			var c = new float2(13, 0);
			var d = new float2(1, 1);
			var e = new float2(1, -1);
			var f = new float2(0, 1);
			var g = new float2(0, -1);
			var moveleftboxtl = new float2(-3, -1);
			var moveleftboxbl = new float2(-3, 0);

			var cr = Game.Renderer.RgbaColorRenderer;
			
			cr.DrawLine(tl + moveleftboxtl, tl + a + moveleftboxtl, 1, color);
			cr.DrawLine(tl + e + moveleftboxtl, tl + e + a + moveleftboxtl, 1, color);
			cr.DrawLine(tl + e + e + moveleftboxtl, tl + e + e + c + moveleftboxtl, 1, color);

			cr.DrawLine(tl + f + moveleftboxtl, tl + a + f + moveleftboxtl, 1, color);

			cr.DrawLine(bl + moveleftboxbl, bl + a + moveleftboxbl, 1, color);
			cr.DrawLine(bl + d + moveleftboxbl, bl + d + a + moveleftboxbl, 1, color);
			cr.DrawLine(bl + d + d + moveleftboxbl, bl + d + d + c + moveleftboxbl, 1, color);

			cr.DrawLine(bl + g + moveleftboxbl, bl + a + g + moveleftboxbl, 1, color);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
