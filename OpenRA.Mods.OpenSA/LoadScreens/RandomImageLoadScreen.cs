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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenSA.LoadScreens
{
	public sealed class RandomImageLoadScreen : RandomSheetLoadScreen
	{
		float2 logoPos;
		Sprite logo;

		Sheet lastSheet;
		int lastDensity;

		string message = "Loading...";

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);

			if (info.ContainsKey("Text"))
				message = info["Text"];
		}

		public override void DisplayInner(Renderer r, Sheet s, int density)
		{
			if (s != lastSheet || density != lastDensity)
			{
				lastSheet = s;
				Rectangle rect = new Rectangle(0, 0, s.Size.Width, s.Size.Height);

				lastDensity = density;

				logo = new Sprite(s, rect, TextureChannel.RGBA);
			}

			if (logo != null)
			{
				logoPos = new float2((r.Resolution.Width - s.Size.Width) / 2, (-s.Size.Height + r.Resolution.Height) / 2);
				r.RgbaSpriteRenderer.DrawSprite(logo, logoPos);
			}

			if (r.Fonts != null)
			{
				var textSize = r.Fonts["Bold"].Measure(message);
				r.Fonts["Bold"].DrawText(message, new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20), Color.White);
			}
		}
	}
}
