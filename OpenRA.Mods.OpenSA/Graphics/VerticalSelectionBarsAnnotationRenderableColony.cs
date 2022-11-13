#region Copyright & License Information
/*
 * Copyright 2019-2022 The OpenSA Developers (see CREDITS)
 * This file is part of OpenSA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenSA.Graphics
{
	public struct VerticalSelectionBarsAnnotationRenderableColony : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly Actor actor;
		readonly bool displayHealth;
		readonly bool displayExtra;
		readonly Rectangle decorationBounds;

		public VerticalSelectionBarsAnnotationRenderableColony(Actor actor, Rectangle decorationBounds, bool displayHealth, bool displayExtra)
			: this(actor.CenterPosition, actor, decorationBounds)
		{
			this.displayHealth = displayHealth;
			this.displayExtra = displayExtra;
		}

		public VerticalSelectionBarsAnnotationRenderableColony(WPos pos, Actor actor, Rectangle decorationBounds)
			: this()
		{
			this.pos = pos;
			this.actor = actor;
			this.decorationBounds = decorationBounds;
		}

		public WPos Pos { get { return pos; } }
		public bool DisplayHealth { get { return displayHealth; } }
		public bool DisplayExtra { get { return displayExtra; } }

		public int ZOffset { get { return 0; } }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return new VerticalSelectionBarsAnnotationRenderableColony(pos + vec, actor, decorationBounds); }
		public IRenderable AsDecoration() { return this; }

		void DrawExtraBars(float2 start, float2 end)
		{
			foreach (var extraBar in actor.TraitsImplementing<ISelectionBar>())
			{
				var value = extraBar.GetValue();
				if (value != 0 || extraBar.DisplayWhenEmpty)
				{
					var offset = new float2(4, 0);
					start += offset;
					end += offset;
					DrawSelectionBar(start, end, extraBar.GetValue(), extraBar.GetColor());
				}
			}
		}

		static void DrawSelectionBar(float2 start, float2 end, float value, Color barColor)
		{
			var c = Color.FromArgb(128, 30, 30, 30);
			var c2 = Color.FromArgb(128, 10, 10, 10);
			var p = new float2(0, -4);
			var q = new float2(0, -3);
			var r = new float2(0, -2);

			var barColor2 = Color.FromArgb(255, barColor.R / 2, barColor.G / 2, barColor.B / 2);

			var z = float3.Lerp(start, end, value);
			var cr = Game.Renderer.RgbaColorRenderer;
			cr.DrawLine(start + p, end + p, 1, c);
			cr.DrawLine(start + q, end + q, 1, c2);
			cr.DrawLine(start + r, end + r, 1, c);

			cr.DrawLine(start + p, z + p, 1, barColor2);
			cr.DrawLine(start + q, z + q, 1, barColor);
			cr.DrawLine(start + r, z + r, 1, barColor2);
		}

		Color GetHealthColor(IHealth health)
		{
			if (Game.Settings.Game.UsePlayerStanceColors)
				return actor.Owner.PlayerRelationshipColor(actor);

			return health.DamageState == DamageState.Critical ? Color.FromArgb(255, 253, 2, 0) :
				health.DamageState == DamageState.Heavy ? Color.FromArgb(255, 254, 254, 1) : Color.FromArgb(255, 0, 254, 0);
		}

		void DrawHealthBar(IHealth health, float2 start, float2 end)
		{
			if (health == null || health.IsDead)
				return;

			var blackcolor = Color.FromArgb(255, 0, 0, 0);
			var greycolor = Color.FromArgb(255, 128, 128, 128);
			var p = new float2(-4, 0);
			var q = new float2(-3, 0);
			var r = new float2(-2, 0);
			var t = new float2(-1, 0);
			var f = new float2(0, 0);
			var g = new float2(1, 0);

			var up = new float2(0, -1);
			var down = new float2(0, 1);			

			var healthColor = GetHealthColor(health);
			var healthColor2 = Color.FromArgb(0, 0, 254, 0);

			var z = float3.Lerp(start, end, (float)health.HP / health.MaxHP);

			var cr = Game.Renderer.RgbaColorRenderer;

			// TOP
			cr.DrawLine(end + p + up, end + p, 1, blackcolor);
			cr.DrawLine(end + q + up, end + q, 1, blackcolor);
			cr.DrawLine(end + r + up, end + r, 1, blackcolor);
			cr.DrawLine(end + t + up, end + t, 1, blackcolor);			
			cr.DrawLine(end + f + up, end + f, 1, blackcolor);
			cr.DrawLine(end + g + up, end + g, 1, blackcolor);

			// MIDDLE
			cr.DrawLine(start + p, end + p, 1, blackcolor);
			cr.DrawLine(start + q, end + q, 1, greycolor);
			cr.DrawLine(start + r, end + r, 1, greycolor);
			cr.DrawLine(start + t, end + t, 1, greycolor);
			cr.DrawLine(start + f, end + f, 1, greycolor);			
			cr.DrawLine(start + g, end + g, 1, blackcolor);

			// BOTTOM
			cr.DrawLine(start + p + down, start + p, 1, blackcolor);
			cr.DrawLine(start + q + down, start + q, 1, blackcolor);
			cr.DrawLine(start + r + down, start + r, 1, blackcolor);
			cr.DrawLine(start + t + down, start + t, 1, blackcolor);			
			cr.DrawLine(start + f + down, start + f, 1, blackcolor);
			cr.DrawLine(start + g + down, start + g, 1, blackcolor);

			// HEALTH
			cr.DrawLine(start + p, z + p, 1, healthColor2);
			cr.DrawLine(start + q, z + q, 1, healthColor);
			cr.DrawLine(start + r, z + r, 1, healthColor);
			cr.DrawLine(start + t, z + t, 1, healthColor);
			cr.DrawLine(start + f, z + f, 1, healthColor);			
			cr.DrawLine(start + g, z + g, 1, healthColor2);

			if (health.DisplayHP != health.HP)
			{
				var deltaColor = Color.OrangeRed;
				var deltaColor2 = Color.FromArgb(
					255,
					deltaColor.R / 2,
					deltaColor.G / 2,
					deltaColor.B / 2);
				var zz = float3.Lerp(start, end, (float)health.DisplayHP / health.MaxHP);

				cr.DrawLine(z + p, zz + p, 1, deltaColor2);
				cr.DrawLine(z + q, zz + q, 1, deltaColor);
				cr.DrawLine(z + r, zz + r, 1, deltaColor);
				cr.DrawLine(z + t, zz + t, 1, deltaColor);
				cr.DrawLine(z + f, zz + f, 1, deltaColor);
				cr.DrawLine(z + g, zz + g, 1, deltaColor2);
			}
		}

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			if (!actor.IsInWorld || actor.IsDead)
				return;

			var health = actor.TraitOrDefault<IHealth>();
			var start = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Left, decorationBounds.Bottom - 1));
			var end = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Left, decorationBounds.Top + 1));

			if (DisplayHealth)
				DrawHealthBar(health, start, end);

			if (DisplayExtra)
				DrawExtraBars(start, end);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
